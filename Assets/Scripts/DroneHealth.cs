using UnityEngine;
using UnityEngine.SceneManagement;

public class DroneHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Impact Settings")]
    public float crashThreshold = 10f;
    public float damagePerForceUnit = 3f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"💥 Drone took {amount:F1} damage. HP left: {currentHealth:F1}");
        
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.LogError("Drone Destroyed!");
        
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
