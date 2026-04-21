using UnityEngine;
using UnityEngine.SceneManagement;

public class DroneHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Impact Settings")]
    [Tooltip("แรงที่ต้องการเพื่อให้รับดาเมจ")]
    public float crashThreshold = 10f;
    [Tooltip("ดาเมจที่ได้รับต่อ 1 หน่วยของแรงที่เกินขีดจำกัด")]
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
        Debug.LogError("🚨 Drone Destroyed! 💥 (HP = 0)");
        // รีสตาร์ทด่าน
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
