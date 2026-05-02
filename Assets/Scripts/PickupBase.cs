using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public abstract class PickupBase : MonoBehaviour
{
    [Header("Idle Animation")]
    [Tooltip("Vertical bob amplitude in world units")]
    public float bobAmplitude = 0.15f;
    [Tooltip("Bob cycles per second")]
    public float bobFrequency = 2f;
    [Tooltip("Z-axis rotation speed in degrees/sec (0 = no spin)")]
    public float spinSpeed = 0f;

    [Header("Glow Pulse")]
    [Tooltip("Identity color for this pickup type")]
    public Color glowColor = Color.white;
    [Tooltip("Pulse oscillation speed")]
    public float pulseSpeed = 3f;
    [Tooltip("Minimum brightness multiplier at pulse trough")]
    [Range(0f, 1f)] public float pulseMin = 0.7f;

    [Header("Magnet Pull")]
    [Tooltip("Radius at which the pickup starts drifting toward the player")]
    public float magnetRadius = 2f;
    [Tooltip("Maximum magnet pull speed")]
    public float magnetSpeed = 8f;

    [Header("Collection VFX")]
    [Tooltip("Looping ambient particle aura (child object, optional)")]
    public ParticleSystem ambientParticles;
    [Tooltip("One-shot burst prefab spawned on collection (optional)")]
    public ParticleSystem collectBurstPrefab;
    [Tooltip("Scale multiplier during the pop animation")]
    public float collectScalePop = 1.3f;
    [Tooltip("Duration of the scale-pop / fade-out before destroy")]
    public float collectPopDuration = 0.1f;
    [Tooltip("Micro screen-shake intensity on collection (0 = none)")]
    public float collectShakeIntensity = 0.06f;

    [Header("Audio")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    // --- Internal state ---
    protected SpriteRenderer sr;
    private Vector3 anchorPos;
    private float phase;
    private bool collected = false;
    private Transform playerTransform;

    protected virtual void Start()
    {
        anchorPos = transform.position;
        phase = Random.Range(0f, Mathf.PI * 2f);
        sr = GetComponent<SpriteRenderer>();

        // Cache player reference once
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    protected virtual void Update()
    {
        if (collected) return;

        // --- Sine bob ---
        float yOffset = Mathf.Sin(Time.time * bobFrequency + phase) * bobAmplitude;
        transform.position = anchorPos + Vector3.up * yOffset;

        // --- Spin ---
        if (spinSpeed != 0f)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        // --- Glow pulse ---
        if (sr != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed + phase) + 1f) / 2f;
            float brightness = Mathf.Lerp(pulseMin, 1f, t);
            sr.color = glowColor * brightness;
            // Preserve full alpha during idle
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        // --- Proximity magnet ---
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(anchorPos, playerTransform.position);
            if (dist < magnetRadius && dist > 0.01f)
            {
                float pull = 1f - (dist / magnetRadius);
                anchorPos = Vector2.MoveTowards(
                    anchorPos,
                    playerTransform.position,
                    magnetSpeed * pull * Time.deltaTime
                );
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player") && other.GetComponent<DroneHealth>() == null) return;

        collected = true;
        ApplyEffect(other);
        StartCoroutine(CollectSequence());
    }

    protected abstract void ApplyEffect(Collider2D player);

    private IEnumerator CollectSequence()
    {
        // Disable collider immediately to prevent double-trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Stop ambient aura
        if (ambientParticles != null)
            ambientParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Spawn one-shot burst (detached so it outlives this object)
        if (collectBurstPrefab != null)
        {
            ParticleSystem burst = Instantiate(collectBurstPrefab, transform.position, Quaternion.identity);
            burst.Play();
            var main = burst.main;
            Destroy(burst.gameObject, main.duration + main.startLifetime.constantMax);
        }

        // Play collection sound
        AudioManager.PlaySFXAt(pickupSound, transform.position, volume);

        // Micro screen shake
        if (collectShakeIntensity > 0f)
            DynamicCamera2D.Shake(collectShakeIntensity);

        // Scale pop + alpha fade
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < collectPopDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectPopDuration;

            // Ease-out scale pop
            transform.localScale = originalScale * Mathf.Lerp(1f, collectScalePop, t);

            // Fade sprite alpha
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - t;
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
