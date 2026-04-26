using UnityEngine;

public class SpikeTrap2D : MonoBehaviour
{
    [Header("Trap Settings")]
    public float damage = 50f;
    public float knockbackForce = 20f;

    [Header("Audio Settings")]
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        DroneHealth health = collision.gameObject.GetComponent<DroneHealth>();
        if (health == null) return;

        AudioManager.PlaySFXAt(hitSound, transform.position, soundVolume);

        health.TakeDamage(damage);

        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }
    }
}
