using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class GravityZone : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("The acceleration vector applied to the drone in this zone. Standard gravity is (0, -9.81). Up is (0, 9.81).")]
    public Vector2 gravityVector = new Vector2(0f, 9.81f);
    public float transitionSpeed = 5f;

    [Header("Visual")]
    public Color zoneColor = new Color(0.4f, 0f, 1f, 0.25f);
    public Color activeColor = new Color(1f, 0.2f, 0.8f, 0.4f);

    [Header("Audio")]
    [Tooltip("Looping sound played while gravity zone is active")]
    public AudioClip activeLoopClip;
    [Range(0f, 1f)] public float loopVolume = 0.5f;
    public float audioMinDistance = 2f;
    public float audioMaxDistance = 15f;

    [Header("Chevron Animation")]
    public int chevronCount = 4;
    public float chevronSize = 0.4f;
    public float chevronSpacing = 0.8f;
    public float animationSpeed = 2.0f;
    public float chevronThickness = 0.1f;

    // Dictionary to keep track of affected objects and their original gravity scale
    private Dictionary<Rigidbody2D, float> affectedBodies = new Dictionary<Rigidbody2D, float>();
    
    private bool isZoneActive = true;
    private SpriteRenderer sr;
    private AudioSource loopSource;
    private LineRenderer[] chevrons;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Setup the animated chevrons
        chevrons = new LineRenderer[chevronCount];
        for (int i = 0; i < chevronCount; i++)
        {
            GameObject child = new GameObject("Chevron_" + i);
            child.transform.SetParent(transform);
            LineRenderer lr = child.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = chevronThickness;
            lr.endWidth = chevronThickness;
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            lr.numCornerVertices = 4; // Makes the chevron joint sharp and clean
            chevrons[i] = lr;
        }
        
        SetVisual(isZoneActive);

        if (activeLoopClip != null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.clip = activeLoopClip;
            loopSource.loop = true;
            loopSource.spatialBlend = 1f;
            loopSource.rolloffMode = AudioRolloffMode.Linear;
            loopSource.minDistance = audioMinDistance;
            loopSource.maxDistance = audioMaxDistance;
            loopSource.volume = loopVolume;
            loopSource.playOnAwake = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DroneController>() == null && other.GetComponent<FlyingEnemyAI>() == null) return;

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        if (!affectedBodies.ContainsKey(rb))
        {
            affectedBodies[rb] = rb.gravityScale;
            if (isZoneActive) rb.gravityScale = 0f;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null && affectedBodies.ContainsKey(rb))
        {
            rb.gravityScale = affectedBodies[rb];
            affectedBodies.Remove(rb);
        }
    }

    void Update()
    {
        if (chevrons == null || chevrons.Length == 0) return;

        Vector3 dir = gravityVector == Vector2.zero ? Vector3.up : new Vector3(gravityVector.x, gravityVector.y, 0).normalized;
        Vector3 right = Vector3.Cross(dir, Vector3.forward).normalized;
        Vector3 center = transform.position;

        float totalLength = chevronSpacing * chevronCount;
        float startOffset = -totalLength / 2f;

        // Loop the offset seamlessly
        float timeOffset = (Time.time * animationSpeed) % chevronSpacing;
        
        Color baseColor = isZoneActive ? activeColor : zoneColor;
        float baseAlpha = isZoneActive ? 1f : 0.2f;

        for (int i = 0; i < chevronCount; i++)
        {
            float posOffset = startOffset + (i * chevronSpacing) + timeOffset;
            Vector3 chevronCenter = center + dir * posOffset;

            // Define points for a chevron ">>>"
            Vector3 p0 = chevronCenter - dir * (chevronSize * 0.5f) + right * chevronSize;
            Vector3 p1 = chevronCenter + dir * (chevronSize * 0.5f);
            Vector3 p2 = chevronCenter - dir * (chevronSize * 0.5f) - right * chevronSize;

            LineRenderer lr = chevrons[i];
            lr.SetPosition(0, p0);
            lr.SetPosition(1, p1);
            lr.SetPosition(2, p2);

            // Smoothly fade out at the very edges so they don't pop in/out abruptly
            float alphaMult = 1.0f - Mathf.Clamp01(Mathf.Abs(posOffset) / (totalLength / 2f));
            Color fadedColor = baseColor;
            fadedColor.a = baseAlpha * alphaMult;
            lr.startColor = fadedColor;
            lr.endColor = fadedColor;
        }
        
        // Handle Audio
        if (loopSource != null)
        {
            bool hasValidTargets = false;
            foreach (var kvp in affectedBodies)
            {
                if (kvp.Key != null)
                {
                    hasValidTargets = true;
                    break;
                }
            }

            if (isZoneActive && hasValidTargets && !loopSource.isPlaying)
                loopSource.Play();
            else if ((!isZoneActive || !hasValidTargets) && loopSource.isPlaying)
                loopSource.Stop();
        }
    }

    void FixedUpdate()
    {
        if (isZoneActive)
        {
            List<Rigidbody2D> keysToRemove = new List<Rigidbody2D>();
            
            foreach (var kvp in affectedBodies)
            {
                Rigidbody2D rb = kvp.Key;
                if (rb == null)
                {
                    keysToRemove.Add(rb);
                    continue;
                }
                
                rb.AddForce(gravityVector * rb.mass, ForceMode2D.Force);
            }
            
            foreach (var k in keysToRemove)
            {
                affectedBodies.Remove(k);
            }
        }
    }

    private void ApplyInvertedGravity()
    {
        foreach (var kvp in affectedBodies)
        {
            if (kvp.Key != null) kvp.Key.gravityScale = 0f;
        }
    }

    private void RestoreGravity()
    {
        foreach (var kvp in affectedBodies)
        {
            if (kvp.Key != null) kvp.Key.gravityScale = kvp.Value;
        }
    }

    public void Activate()
    {
        isZoneActive = true;
        SetVisual(true);
        ApplyInvertedGravity();
    }

    public void Deactivate()
    {
        isZoneActive = false;
        SetVisual(false);
        RestoreGravity();
    }

    private void SetVisual(bool active)
    {
        if (sr != null)
            sr.color = active ? activeColor : zoneColor;
    }

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0.6f, 0f, 1f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(
            transform.TransformPoint(col.offset),
            transform.rotation,
            transform.lossyScale
        );
        Gizmos.DrawCube(Vector3.zero, col.size);
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.8f);
        Vector3 center = transform.position;
        Vector3 dir = gravityVector == Vector2.zero ? Vector3.up : new Vector3(gravityVector.x, gravityVector.y, 0).normalized;
        float arrowLength = 0.5f;
        
        Gizmos.DrawLine(center - dir * arrowLength, center + dir * arrowLength);
        
        Vector3 right = Vector3.Cross(dir, Vector3.forward).normalized;
        Gizmos.DrawLine(center + dir * arrowLength, center + dir * arrowLength * 0.5f + right * 0.2f);
        Gizmos.DrawLine(center + dir * arrowLength, center + dir * arrowLength * 0.5f - right * 0.2f);
    }
}
