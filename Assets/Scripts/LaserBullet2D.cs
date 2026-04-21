using UnityEngine;

public class LaserBullet2D : MonoBehaviour
{
    public float lifeTime = 3f;
    public float damage = 25f;

    [Header("Hit Audio")]
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<CameraFocusZone2D>() != null)
        {
            return; 
        }

        if (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null)
        {
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
            }

            DroneHealth health = other.GetComponent<DroneHealth>();
            if (health != null) health.TakeDamage(damage);
        }
        else if (other.GetComponent<SmashPlatform2D>() != null)
        {
            Destroy(other.gameObject);
        }

        if (other.GetComponent<MagneticTrap>() == null)
        {
            Destroy(gameObject);
        }
    }
}
