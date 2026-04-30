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
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateParticleDirection();
        }
        #endif
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
        
        DroneController dc = other.GetComponent<DroneController>();
        if (dc != null) dc.ApplyWindOverride(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            affectedBodies.Remove(rb);
        }
        
        DroneController dc = other.GetComponent<DroneController>();
        if (dc != null) dc.RemoveWindOverride(this);
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
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
        Gizmos.DrawCube(transform.position, GetComponent<BoxCollider2D>() != null ? GetComponent<BoxCollider2D>().size : Vector3.one);
        
        Gizmos.color = Color.cyan;
        Vector3 direction = new Vector3(windDirection.x, windDirection.y, 0).normalized;
        Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
        Gizmos.DrawSphere(transform.position + direction * 2f, 0.2f);
    }
}
