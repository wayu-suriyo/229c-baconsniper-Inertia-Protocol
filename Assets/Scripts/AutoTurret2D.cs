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

    [Header("Rotation Pivot")]
    [Tooltip("Optional: assign a child Transform (e.g. 'TurretHead') to rotate around that point instead of this object's origin. Leave empty to rotate the whole turret as before.")]
    public Transform rotationPivot;

    [Header("Audio")]
    public AudioClip shootSound;
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private Transform target;
    private Vector2 defaultForward;
    private bool canSeePlayer = false;
    private float nextFireTime = 0f;

    // The transform we actually rotate — pivot if set, otherwise self
    private Transform RotatingPart => rotationPivot != null ? rotationPivot : transform;

    void Start()
    {
        DroneController drone = FindAnyObjectByType<DroneController>();
        if (drone != null) target = drone.transform;

        // Capture the initial forward direction of whichever part rotates
        defaultForward = RotatingPart.up;
    }

    void Update()
    {
        canSeePlayer = false;

        if (target != null)
        {
            // Direction is always measured from the rotating part's position
            Vector2 directionToPlayer = (Vector2)target.position - (Vector2)RotatingPart.position;
            float distance = directionToPlayer.magnitude;

            if (distance <= detectRange)
            {
                float angle = Vector2.Angle(defaultForward, directionToPlayer.normalized);

                if (angle <= viewAngle / 2f)
                {
                    RaycastHit2D hit = Physics2D.Raycast(RotatingPart.position, directionToPlayer.normalized, distance, obstacleLayer);

                    if (hit.collider == null || hit.collider.CompareTag("Player") ||
                        hit.collider.GetComponent<DroneController>() != null ||
                        hit.collider.GetComponent<SmashPlatform2D>() != null)
                    {
                        canSeePlayer = true;

                        float targetRotZ = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
                        Quaternion lookRotation = Quaternion.Euler(0, 0, targetRotZ);
                        RotatingPart.rotation = Quaternion.Slerp(RotatingPart.rotation, lookRotation, Time.deltaTime * turnSpeed);

                        // Check aim angle against the rotating part's up direction
                        float aimAngle = Vector2.Angle(RotatingPart.up, directionToPlayer.normalized);

                        if (aimAngle <= 5f && Time.time >= nextFireTime)
                        {
                            Shoot();
                            nextFireTime = Time.time + fireRate;
                        }
                    }
                }
            }
        }

        // Return to default forward when player is lost
        if (!canSeePlayer && defaultForward != Vector2.zero)
        {
            float targetRotZ = Mathf.Atan2(defaultForward.y, defaultForward.x) * Mathf.Rad2Deg - 90f;
            Quaternion resetRotation = Quaternion.Euler(0, 0, targetRotZ);
            RotatingPart.rotation = Quaternion.Slerp(RotatingPart.rotation, resetRotation, Time.deltaTime * turnSpeed);
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
        // Detection range sphere (from the rotating part's origin)
        Vector3 detectionOrigin = RotatingPart != null ? RotatingPart.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionOrigin, detectRange);

        // Show view cone
        if (Application.isPlaying && defaultForward != Vector2.zero)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Vector3 fwd = new Vector3(defaultForward.x, defaultForward.y, 0f);
            float halfAngle = viewAngle / 2f;
            Vector3 leftBound  = Quaternion.Euler(0, 0,  halfAngle) * fwd * detectRange;
            Vector3 rightBound = Quaternion.Euler(0, 0, -halfAngle) * fwd * detectRange;
            Gizmos.DrawLine(detectionOrigin, detectionOrigin + leftBound);
            Gizmos.DrawLine(detectionOrigin, detectionOrigin + rightBound);
        }

        // Draw pivot marker if assigned
        if (rotationPivot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rotationPivot.position, 0.15f);
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawLine(transform.position, rotationPivot.position);
        }
    }
}

