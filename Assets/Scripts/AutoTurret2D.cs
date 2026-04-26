using UnityEngine;

public class AutoTurret2D : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireForce = 15f;
    public float fireRate = 2f;

    [Header("Detection Settings")]
    public float detectRange = 15f;
    public float viewAngle = 90f;
    public LayerMask obstacleLayer;

    [Header("Tracking Settings")]
    public float turnSpeed = 5f;
    private Transform target;
    private Vector2 defaultForward;

    [Header("Audio")]
    public AudioClip shootSound;
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private bool canSeePlayer = false;
    private float nextFireTime = 0f;

    void Start()
    {
        DroneController drone = FindAnyObjectByType<DroneController>();
        if (drone != null) target = drone.transform;

        defaultForward = transform.up; 
    }

    void Update()
    {
        canSeePlayer = false;

        if (target != null)
        {
            Vector2 directionToPlayer = target.position - transform.position;
            float distance = directionToPlayer.magnitude;

            if (distance <= detectRange)
            {
                float angle = Vector2.Angle(defaultForward, directionToPlayer.normalized);

                if (angle <= viewAngle / 2f)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, distance, obstacleLayer);
                    
                    if (hit.collider == null || hit.collider.CompareTag("Player") || hit.collider.GetComponent<DroneController>() != null || hit.collider.GetComponent<SmashPlatform2D>() != null)
                    {
                        canSeePlayer = true;

                        float targetRotZ = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
                        Quaternion lookRotation = Quaternion.Euler(0, 0, targetRotZ);
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);

                        float aimAngle = Vector2.Angle(transform.up, directionToPlayer.normalized);

                        if (aimAngle <= 5f && Time.time >= nextFireTime)
                        {
                            Shoot();
                            nextFireTime = Time.time + fireRate;
                        }
                    }
                }
            }
        }

        if (!canSeePlayer && defaultForward != Vector2.zero)
        {
            float targetRotZ = Mathf.Atan2(defaultForward.y, defaultForward.x) * Mathf.Rad2Deg - 90f;
            Quaternion resetRotation = Quaternion.Euler(0, 0, targetRotZ);
            transform.rotation = Quaternion.Slerp(transform.rotation, resetRotation, Time.deltaTime * turnSpeed);
        }
    }

    void Shoot()
    {
        if (firePoint == null) return;

        GameObject bullet = BulletPool.instance != null
            ? BulletPool.instance.Get(firePoint.position, firePoint.rotation)
            : null;

        if (bullet == null) return;

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(firePoint.up * fireForce, ForceMode2D.Impulse);
        }

        if (shootSound != null) AudioManager.PlaySFXAt(shootSound, transform.position, volume);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
