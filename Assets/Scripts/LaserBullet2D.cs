using UnityEngine;
using System.Collections;

public class LaserBullet2D : MonoBehaviour
{
    public float lifeTime = 3f;
    public float damage = 25f;

    [Header("Hit Audio")]
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    void OnEnable()
    {
        StartCoroutine(AutoReturn());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator AutoReturn()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
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
