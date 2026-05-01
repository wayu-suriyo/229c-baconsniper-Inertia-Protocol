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
    [Range(0f, 1f)] public float damageSoundVolume = 0.8f;
    public AudioClip deathSoundClip;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    [Header("Death Settings")]
    public GameObject explosionPrefab;
    public float explosionDuration = 1f;

    private bool isInvincible = false;
    private float iFrameTimer = 0f;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
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
        AudioManager.PlaySFX(damageSoundClip, damageSoundVolume);

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
            
            TakeDamage(damageToTake);
        }
        else if (impactForce > 2f)
        {
            float minorShake = Mathf.Clamp(impactForce / 20f, 0f, 0.15f);
            DynamicCamera2D.Shake(minorShake);
            Debug.Log($"Bumped! Minor Impact Force: {impactForce:F2}");
        }
    }
}
