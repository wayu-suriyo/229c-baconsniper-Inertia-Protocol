using UnityEngine;

public class SmashPlatform2D : MonoBehaviour, IDamageable
{
    [Header("Smash Settings")]
    public float breakVelocityThreshold = 8f;
    public float droneDamageThreshold = 25f;

    [Header("Audio Settings")]
    public AudioClip smashSound;
    [Range(0f, 1f)] public float minVolume = 0.3f;
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Tooltip("Impact velocity at which smash sound reaches max volume")]
    public float maxVelocityForVolume = 20f;
    [Tooltip("Random pitch range for smash sound")]
    public float minPitch = 0.85f;
    public float maxPitch = 1.15f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<DroneHealth>() != null)
        {
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce >= breakVelocityThreshold)
            {
                // Scale volume: breakVelocityThreshold = quietest, maxVelocityForVolume = loudest
                float t = Mathf.Clamp01((impactForce - breakVelocityThreshold) / (maxVelocityForVolume - breakVelocityThreshold));
                float scaledVolume = Mathf.Lerp(minVolume, maxVolume, t);
                float pitch = Random.Range(minPitch, maxPitch);
                AudioManager.PlaySFXAt(smashSound, transform.position, scaledVolume, pitch);
                GetComponent<Collider2D>().enabled = false;

                DroneHealth playerHealth = collision.gameObject.GetComponent<DroneHealth>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(droneDamageThreshold);

                Destroy(gameObject);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        float pitch = Random.Range(minPitch, maxPitch);
        AudioManager.PlaySFXAt(smashSound, transform.position, maxVolume, pitch);
        Destroy(gameObject);
    }
}
