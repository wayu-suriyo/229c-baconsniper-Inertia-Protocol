using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WindZone : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector2 windDirection = Vector2.up;
    public float windForce = 15f;

    [Header("Gust Settings")]
    public bool isGusting = false;
    public float gustMinForce = 5f;
    public float gustMaxForce = 25f;
    public float gustSpeed = 2f;

    [Header("Visuals")]
    public ParticleSystem windParticles;

    [Header("Audio")]
    [Tooltip("Looping wind sound — volume increases as drone gets closer (3D spatial)")]
    public AudioClip windLoopClip;
    [Range(0f, 1f)] public float windVolume = 0.8f;
    [Tooltip("Distance at which wind is fully audible")]
    public float audioMinDistance = 2f;
    [Tooltip("Distance at which wind becomes inaudible")]
    public float audioMaxDistance = 20f;

    private float currentForce;
    private AudioSource windAudioSource;

    void Start()
    {
        currentForce = windForce;
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
        if (isGusting)
        {
            currentForce = Mathf.Lerp(gustMinForce, gustMaxForce, Mathf.PingPong(Time.time * gustSpeed, 1f));
        }
        else
        {
            currentForce = windForce;
        }

        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateParticleDirection();
        }
        #endif
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<DroneController>() != null)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(windDirection.normalized * currentForce, ForceMode2D.Force);
            }
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
        float particleSpeed = windForce * 0.5f;
        main.startSpeed = particleSpeed;

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
            if (particleSpeed > 0f)
            {
                main.startLifetime = travelLength / particleSpeed;
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
