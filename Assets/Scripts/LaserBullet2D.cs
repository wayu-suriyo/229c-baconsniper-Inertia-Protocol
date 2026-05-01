using UnityEngine;

public class LaserBullet2D : MonoBehaviour
{
    public float lifeTime = 3f;
    public float damage = 25f;

    [Header("Hit Audio")]
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    [Header("Motion Blur Trail")]
    [Tooltip("If unchecked, no trail is created or shown.")]
    public bool enableTrail = true;
    [Tooltip("How long the trail lingers behind the bullet (seconds).")]
    public float trailTime = 0.12f;
    [Tooltip("Width of the trail at the bullet.")]
    public float trailStartWidth = 0.25f;
    [Tooltip("Width of the trail at the tail end.")]
    public float trailEndWidth = 0.02f;
    [Tooltip("Trail color at the bullet (tip).")]
    public Color trailStartColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    [Tooltip("Trail color at the tail (fades out).")]
    public Color trailEndColor  = new Color(1f, 0.6f, 0.2f, 0f);
    [Tooltip("Assign a custom trail material. Leave empty to auto-create a Sprites/Default material.")]
    public Material trailMaterial;

    private float timer;
    private TrailRenderer trail;

    void Awake()
    {
        SetupTrail();
    }

    void OnEnable()
    {
        timer = lifeTime;

        // Clear stale trail positions from the previous pool cycle
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = enableTrail;
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<CameraFocusZone2D>() != null)
        {
            return; 
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            AudioManager.PlaySFXAt(hitSound, transform.position, soundVolume);
            damageable.TakeDamage(damage);
        }

        if (other.GetComponent<MagneticTrap>() == null)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        // Stop emitting before returning so the trail doesn't render at the pool origin
        if (trail != null) trail.emitting = false;

        if (BulletPool.instance != null)
            BulletPool.instance.Return(gameObject);
        else
            Destroy(gameObject);
    }

    private void SetupTrail()
    {
        if (!enableTrail) return;

        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.numCapVertices = 3;
        trail.numCornerVertices = 3;
        trail.minVertexDistance = 0.05f;

        // Colors
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailStartColor, 0f),
                new GradientColorKey(trailEndColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(trailStartColor.a, 0f),
                new GradientAlphaKey(trailEndColor.a, 1f)
            }
        );
        trail.colorGradient = gradient;

        // Material
        if (trailMaterial != null)
        {
            trail.material = trailMaterial;
        }
        else
        {
            trail.material = new Material(Shader.Find("Sprites/Default"));
        }

        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
    }
}

