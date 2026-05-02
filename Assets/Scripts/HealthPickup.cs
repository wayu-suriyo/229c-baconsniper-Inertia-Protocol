using UnityEngine;

public class HealthPickup : PickupBase
{
    [Header("Heal Settings")]
    public float healAmount = 30f;
    [Tooltip("If true, restores to full health regardless of healAmount")]
    public bool fullRestore = false;

    protected override void ApplyEffect(Collider2D player)
    {
        DroneHealth health = player.GetComponent<DroneHealth>();
        if (health == null) return;

        float amount = fullRestore ? health.maxHealth : healAmount;
        health.currentHealth = Mathf.Min(health.currentHealth + amount, health.maxHealth);
    }
}
