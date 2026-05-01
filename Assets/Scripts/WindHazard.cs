using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class WindZone : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector2 windDirection = Vector2.up;
    [Tooltip("The acceleration force of the wind.")]
    public float windForce = 15f;

    [Header("Drone Overrides")]
    [Tooltip("Distance outside the wind zone (opposite to wind direction) where overrides begin applying early.")]
    public float overridePreZoneDistance = 3f;
    [Tooltip("Overrides the drone's Thrust Force while in this specific wind zone.")]
    public float droneThrustOverride = 35f;
    [Tooltip("Overrides the drone's Torque Force while in this specific wind zone.")]
    public float droneTorqueOverride = 15f;
    [Tooltip("Overrides the drone's Max Tilt Angle while in this specific wind zone.")]
    public float droneTiltOverride = 60f;

    [Header("Visuals")]
    public ParticleSystem windParticles;
    [Tooltip("Manual speed for the particle visuals. Does not affect physics.")]
    public float particleVisualSpeed = 5f;

    [Header("Audio")]
    [Tooltip("Looping wind sound — volume increases as drone gets closer (3D spatial)")]
    public AudioClip windLoopClip;
    [Range(0f, 1f)] public float windVolume = 0.8f;
    [Tooltip("Distance at which wind is fully audible")]
    public float audioMinDistance = 2f;
    [Tooltip("Distance at which wind becomes inaudible")]
    public float audioMaxDistance = 20f;

    private AudioSource windAudioSource;
    private HashSet<Rigidbody2D> affectedBodies = new HashSet<Rigidbody2D>();
    private HashSet<DroneController> dronesInExpandedZone = new HashSet<DroneController>();

    void Start()
    {
        UpdateParticleDirection();

        if (windLoopClip != null)
        {
            windAudioSource = gameObject.AddComponent<AudioSource>();
            windAudioSource.clip = windLoopClip;
            windAudioSource.loop = true;
            windAudioSource.spatialBlend = 1f;
            windAudioSource.rolloffMode = AudioRolloffMode.Linear;
            windAudioSource.minDistance = audioMinDistance;
            windAudioSource.maxDistance = audioMaxDistance;
            windAudioSource.volume = windVolume;
            windAudioSource.Play();
        }
    }

    void Update()
    {
        UpdateParticleDirection();
    }

    void FixedUpdate()
    {
        List<Rigidbody2D> keysToRemove = new List<Rigidbody2D>();
        
        foreach (Rigidbody2D rb in affectedBodies)
        {
            if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                keysToRemove.Add(rb);
                continue;
            }
            
            // Use raw force. Heavy objects will naturally resist wind better.
            rb.AddForce(windDirection.normalized * windForce, ForceMode2D.Force);
        }
        
        foreach (var k in keysToRemove)
        {
            affectedBodies.Remove(k);
        }

        // Handle Expanded Override Zone for Drones
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Vector2 expandDir = windDirection.normalized;
            Vector2 worldSize = new Vector2(col.size.x * Mathf.Abs(transform.lossyScale.x), col.size.y * Mathf.Abs(transform.lossyScale.y));
            
            Vector2 expandedSize = worldSize;
            expandedSize.x += Mathf.Abs(expandDir.x) * overridePreZoneDistance;
            expandedSize.y += Mathf.Abs(expandDir.y) * overridePreZoneDistance;
            
            Vector2 centerWorld = (Vector2)transform.TransformPoint(col.offset) + (expandDir * (overridePreZoneDistance / 2f));
            
            Collider2D[] hits = Physics2D.OverlapBoxAll(centerWorld, expandedSize, transform.eulerAngles.z);
            HashSet<DroneController> currentHits = new HashSet<DroneController>();
            
            foreach (var hit in hits)
            {
                DroneController dc = hit.GetComponent<DroneController>();
                if (dc != null)
                {
                    currentHits.Add(dc);
                    if (!dronesInExpandedZone.Contains(dc))
                    {
                        dronesInExpandedZone.Add(dc);
                        dc.ApplyWindOverride(this);
                    }
                }
            }
            
            List<DroneController> toRemove = new List<DroneController>();
            foreach (var dc in dronesInExpandedZone)
            {
                if (dc == null) toRemove.Add(dc);
                else if (!currentHits.Contains(dc))
                {
                    dc.RemoveWindOverride(this);
                    toRemove.Add(dc);
                }
            }
            foreach (var dc in toRemove) dronesInExpandedZone.Remove(dc);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DroneController>() != null || other.GetComponent<FlyingEnemyAI>() != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                affectedBodies.Add(rb);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            affectedBodies.Remove(rb);
        }
    }

    private void UpdateParticleDirection()
    {
        if (windParticles == null) return;

        // Rotate particle system to emit along wind direction
        Vector3 dir3D = new Vector3(windDirection.x, windDirection.y, 0).normalized;
        if (dir3D != Vector3.zero)
        {
            windParticles.transform.rotation = Quaternion.LookRotation(dir3D, Vector3.back);
        }

        var main = windParticles.main;
        main.startSpeed = particleVisualSpeed;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            var shape = windParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;

            // The axis of travel is determined by whichever wind component dominates
            bool isVertical = Mathf.Abs(windDirection.y) > Mathf.Abs(windDirection.x);

            // Spawn width = the collider's cross-axis (perpendicular to travel direction)
            // Travel length = the collider's on-axis dimension (parallel to wind)
            float spawnWidth  = isVertical ? col.size.x : col.size.y;
            float travelLength = isVertical ? col.size.y : col.size.x;

            shape.scale = new Vector3(spawnWidth, 0.1f, 1f);

            // lifetime = distance / speed — particle dies exactly at the far boundary
            if (particleVisualSpeed > 0f)
            {
                main.startLifetime = travelLength / particleVisualSpeed;
            }
        }
    }

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(col.offset, col.size);
            Gizmos.matrix = Matrix4x4.identity;

            // Draw pre-zone (separate visual box)
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
            Vector2 expandDir = windDirection.normalized;
            Vector2 worldSize = new Vector2(col.size.x * Mathf.Abs(transform.lossyScale.x), col.size.y * Mathf.Abs(transform.lossyScale.y));
            
            // Calculate size of just the extended portion
            Vector2 extensionSize = worldSize;
            if (Mathf.Abs(expandDir.x) > 0.5f) extensionSize.x = overridePreZoneDistance;
            if (Mathf.Abs(expandDir.y) > 0.5f) extensionSize.y = overridePreZoneDistance;
            
            // Calculate center of just the extended portion
            Vector2 edgeOffset = new Vector2(worldSize.x / 2f * expandDir.x, worldSize.y / 2f * expandDir.y);
            Vector2 centerWorld = (Vector2)transform.TransformPoint(col.offset) + edgeOffset + (expandDir * (overridePreZoneDistance / 2f));
            
            Gizmos.matrix = Matrix4x4.TRS(centerWorld, Quaternion.Euler(0, 0, transform.eulerAngles.z), Vector3.one);
            Gizmos.DrawCube(Vector3.zero, extensionSize);
            Gizmos.DrawWireCube(Vector3.zero, extensionSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        Gizmos.color = Color.cyan;
        Vector3 direction = new Vector3(windDirection.x, windDirection.y, 0).normalized;
        Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
        Gizmos.DrawSphere(transform.position + direction * 2f, 0.2f);
    }
}
