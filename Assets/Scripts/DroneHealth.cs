using UnityEngine;

public class DroneHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Impact Settings")]
    public float crashThreshold = 10f;
    public float damagePerForceUnit = 3f;

    [Header("Invincibility Frames")]
    [Tooltip("Seconds of invincibility after taking damage (prevents multi-source instant kill)")]
    public float iFrameDuration = 0.5f;

    private bool isInvincible = false;
    private float iFrameTimer = 0f;

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
        Debug.LogError("Drone Destroyed!");

        DroneController controller = GetComponent<DroneController>();
        if (controller != null) controller.enabled = false;
        
        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.ShowGameOver();
        }

        gameObject.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce >= crashThreshold)
        {
            float excessForce = impactForce - crashThreshold;
            float damageToTake = 5f + (excessForce * damagePerForceUnit);
            TakeDamage(damageToTake);
        }
        else if (impactForce > 2f)
        {
            Debug.Log($"Bumped! Minor Impact Force: {impactForce:F2}");
        }
    }
}
