using UnityEngine;

public class LaserBullet2D : MonoBehaviour
{
    public float lifeTime = 3f;
    public float damage = 25f;

    [Header("Hit Audio")]
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    private float timer;

    void OnEnable()
    {
        timer = lifeTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<CameraFocusZone2D>() != null)
        {
            return; 
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            AudioManager.PlaySFXAt(hitSound, transform.position, soundVolume);
            damageable.TakeDamage(damage);
        }

        if (other.GetComponent<MagneticTrap>() == null)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (BulletPool.instance != null)
            BulletPool.instance.Return(gameObject);
        else
            Destroy(gameObject);
    }
}
