using UnityEngine;

public class SmashPlatform2D : MonoBehaviour
{
    [Header("Smash Settings")]
    public float breakVelocityThreshold = 8f;
    public float droneDamageThreshold = 25f; 

    [Header("Audio Settings")]
    public AudioClip smashSound;
    [Range(0f, 1f)]
    public float volume = 0.8f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<DroneHealth>() != null)
        {
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce >= breakVelocityThreshold)
            {
                if (smashSound != null)
                {
                    AudioSource.PlayClipAtPoint(smashSound, transform.position, volume);
                }

                GetComponent<Collider2D>().enabled = false;
                
                DroneHealth playerHealth = collision.gameObject.GetComponent<DroneHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(droneDamageThreshold);
                }

                Destroy(gameObject);
            }
        }
    }
}
