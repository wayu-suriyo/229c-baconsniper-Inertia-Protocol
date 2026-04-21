using UnityEngine;

public class SpikeTrap2D : MonoBehaviour
{
    [Header("Trap Settings")]
    public float damage = 50f; 

    [Header("Audio Settings")]
    public AudioClip deathSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null)
        {
            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position, soundVolume);
            }

            DroneHealth health = other.GetComponent<DroneHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
}
