using UnityEngine;

public class SpikeTrap2D : MonoBehaviour
{
    [Header("Trap Settings")]
    public float damage = 50f;
    public float knockbackForce = 20f;

    [Header("Audio Settings")]
    public AudioClip hitSound;
    [Range(0f, 1f)] public float minSoundVolume = 0.2f;
    [Range(0f, 1f)] public float maxSoundVolume = 0.9f;
    [Tooltip("Impact velocity that maps to minimum volume (very slow grazes)")]
    public float minVelocityForSound = 2f;
    [Tooltip("Impact velocity at which hit sound reaches maximum volume")]
    public float maxVelocityForSound = 18f;
    [Tooltip("Random pitch range for hit sound")]
    public float minPitch = 0.85f;
    public float maxPitch = 1.15f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        DroneHealth health = collision.gameObject.GetComponent<DroneHealth>();
        if (health == null) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        float t = Mathf.Clamp01((impactSpeed - minVelocityForSound) / (maxVelocityForSound - minVelocityForSound));
        float scaledVolume = Mathf.Lerp(minSoundVolume, maxSoundVolume, t);
        float pitch = Random.Range(minPitch, maxPitch);
        AudioManager.PlaySFXAt(hitSound, transform.position, scaledVolume, pitch);

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
