using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Heal Settings")]
    public float healAmount = 30f;
    [Tooltip("If true, restores to full health regardless of healAmount")]
    public bool fullRestore = false;

    [Header("Audio")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    void OnTriggerEnter2D(Collider2D other)
    {
        DroneHealth health = other.GetComponent<DroneHealth>();
        if (health == null) return;


        float amount = fullRestore ? health.maxHealth : healAmount;
        health.currentHealth = Mathf.Min(health.currentHealth + amount, health.maxHealth);

        AudioManager.PlaySFXAt(pickupSound, transform.position, volume);
        Destroy(gameObject);
    }
}
