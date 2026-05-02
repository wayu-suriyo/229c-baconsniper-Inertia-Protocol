using UnityEngine;

public class DroneHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Impact & Landing Settings")]
    [Tooltip("Minimum impact velocity to trigger damage on hard landings or crashes.")]
    public float minimumImpactVelocity = 8f;
    public float damagePerVelocityUnit = 3f;
    [Tooltip("Maximum damage taken from a single physical collision")]
    public float maxDamagePerCollision = 40f;

    [Header("Invincibility Frames")]
    [Tooltip("Seconds of invincibility after taking damage (prevents multi-source instant kill)")]
    public float iFrameDuration = 0.5f;

    [Header("Audio")]
    public AudioClip damageSoundClip;
    [Range(0f, 1f)] public float damageSoundMinVolume = 0.3f;
    [Range(0f, 1f)] public float damageSoundMaxVolume = 0.9f;
    [Tooltip("Impact velocity at which damage sound reaches maximum volume")]
    public float damageSoundMaxVelocity = 20f;

    [Tooltip("Sound played on light bumps (below damage threshold)")]
    public AudioClip bumpSoundClip;
    [Range(0f, 1f)] public float bumpSoundMinVolume = 0.05f;
    [Range(0f, 1f)] public float bumpSoundMaxVolume = 0.45f;
    [Tooltip("Impact velocity that maps to maximum bump volume (= minimumImpactVelocity)")]
    public float bumpSoundMaxVelocity = 8f;

    [Tooltip("Random pitch range for collision sounds (adds variety per crash)")]
    public float minPitch = 0.85f;
    public float maxPitch = 1.15f;

    public AudioClip deathSoundClip;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    [Header("Death Settings")]
    public GameObject explosionPrefab;
    public float explosionDuration = 1f;

    private bool isInvincible = false;
    private float iFrameTimer = 0f;
    private bool isDead = false;
    // Set by OnCollisionEnter2D before calling TakeDamage; non-physics callers use damageSoundMaxVolume
    private float damageSoundVolume;
    private float damageSoundPitch;

    void Start()
    {
        currentHealth = maxHealth;
        damageSoundVolume = damageSoundMaxVolume;
        damageSoundPitch = 1f; // Default for non-physics damage sources
    }

    void Update()
    {
        if (isInvincible)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0f)
            {
                isInvincible = false;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible) return;

        currentHealth -= amount;
        Debug.Log($"💥 Drone took {amount:F1} damage. HP left: {currentHealth:F1}");

        float shakeAmount = Mathf.Clamp(amount / 50f, 0.1f, 0.6f);
        DynamicCamera2D.Shake(shakeAmount);
        AudioManager.PlaySFX(damageSoundClip, damageSoundVolume, damageSoundPitch);

        if (GameUIManager.instance != null)
            GameUIManager.instance.TriggerDamageFlash();
        
        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            isInvincible = true;
            iFrameTimer = iFrameDuration;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Track deaths for end-of-level summary
        if (CheckpointManager.instance != null)
            CheckpointManager.instance.IncrementDeathCount();

        Debug.Log("Drone Destroyed!");

        DynamicCamera2D.Shake(0.8f);
        AudioManager.PlaySFX(deathSoundClip, deathSoundVolume);

        DroneController controller = GetComponent<DroneController>();
        if (controller != null) controller.enabled = false;
        
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach(var r in renderers) r.enabled = false;

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach(var col in colliders) col.enabled = false;

        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, explosionDuration);
        }

        StartCoroutine(GameOverRoutine());
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(explosionDuration);

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.ShowGameOver();
        }

        gameObject.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce >= minimumImpactVelocity)
        {
            float excessForce = impactForce - minimumImpactVelocity;
            float damageToTake = 5f + (excessForce * damagePerVelocityUnit);
            damageToTake = Mathf.Min(damageToTake, maxDamagePerCollision);

            // Scale damage sound volume: louder the harder the hit
            float t = Mathf.Clamp01((impactForce - minimumImpactVelocity) / (damageSoundMaxVelocity - minimumImpactVelocity));
            damageSoundVolume = Mathf.Lerp(damageSoundMinVolume, damageSoundMaxVolume, t);
            damageSoundPitch = Random.Range(minPitch, maxPitch);

            TakeDamage(damageToTake);
        }
        else if (impactForce > 2f)
        {
            // Minor bump: scale both shake and sound by velocity
            float minorShake = Mathf.Clamp(impactForce / 20f, 0f, 0.15f);
            DynamicCamera2D.Shake(minorShake);

            if (bumpSoundClip != null)
            {
                float t = Mathf.Clamp01((impactForce - 2f) / (bumpSoundMaxVelocity - 2f));
                float bumpVol = Mathf.Lerp(bumpSoundMinVolume, bumpSoundMaxVolume, t);
                float bumpPitch = Random.Range(minPitch, maxPitch);
                AudioManager.PlaySFXAt(bumpSoundClip, transform.position, bumpVol, bumpPitch);
            }
        }
    }
}
