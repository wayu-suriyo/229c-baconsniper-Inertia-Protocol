using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FlyingEnemyAI : MonoBehaviour, IDamageable
{
    public enum FacingDirection { Up, Down, Left, Right }
    private enum EnemyState { Patrol, Tracking, Charging, Dashing, Dead }
    
    private EnemyState currentState = EnemyState.Patrol;

    [Header("Targeting & Vision")]
    public Transform playerTarget;
    public float detectionRadius = 15f;
    public float attackRange = 8f;
    public LayerMask obstacleLayer;
    [Tooltip("How long it can lose sight of player before teleporting back to spawn")]
    public float loseSightTimeLimit = 2.5f;

    [Header("Patrol")]
    public float patrolRadius = 3f;
    public float patrolWaitTime = 2f;

    [Header("Movement (Tracking)")]
    public float trackingSpeed = 10f;
    public float maxTrackingVelocity = 4f;
    
    [Header("Dash Attack")]
    public float chargeTime = 1.2f;
    [Tooltip("How fast the enemy can turn to track the player while charging before a dash.")]
    public float chargeRotationSpeed = 2f;
    public float dashForce = 35f;
    public float damageToPlayer = 30f;
    [Tooltip("Controlled knockback force to prevent launching the player to space")]
    public float playerKnockbackForce = 15f;
    [Tooltip("Velocity required to explode when hitting a wall. High enough to ignore normal flying, low enough that Gravity crushes it.")]
    public float crashThreshold = 10f;

    [Header("Visuals & VFX")]
    public SpriteRenderer spriteRenderer;
    public ParticleSystem chargeParticles;
    public GameObject explosionPrefab;
    public FacingDirection spriteFacing = FacingDirection.Left;
    
    [Header("Colors")]
    public Color trackingColor = Color.white;
    public Color chargingColor = Color.yellow;
    public Color dashingColor = Color.red;

    [Header("Audio")]
    public AudioClip chargeSound;
    public AudioClip dashSound;
    public AudioClip crashSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private Rigidbody2D rb;
    private float stateTimer = 0f;
    private float outOfSightTimer = 0f;
    private Vector2 currentPatrolTarget;
    private float patrolWaitTimer = 0f;
    private bool isWaitingInPatrol = false;
    private Vector2 spawnPoint;
    private Vector2 dashDirection;
    private Vector2 currentAimDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; 
        spawnPoint = transform.position;
        currentPatrolTarget = spawnPoint;
        
        if (spriteRenderer != null)
            spriteRenderer.color = trackingColor;
            
        if (chargeParticles != null)
            chargeParticles.Stop();

        if (playerTarget == null)
        {
            DroneController dc = FindFirstObjectByType<DroneController>();
            if (dc != null) playerTarget = dc.transform;
        }
    }

    void FixedUpdate()
    {
        if (currentState == EnemyState.Dead || playerTarget == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        // Only do the expensive linecast if player is within detection range
        bool hasLOS = distanceToPlayer <= detectionRadius && CanSeePlayer();

        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrol();
                
                if (hasLOS)
                {
                    outOfSightTimer = 0f;
                    currentState = EnemyState.Tracking;
                }
                break;

            case EnemyState.Tracking:
                if (!hasLOS)
                {
                    outOfSightTimer += Time.fixedDeltaTime;
                    if (outOfSightTimer >= loseSightTimeLimit)
                    {
                        // Teleport back to spawn
                        transform.position = spawnPoint;
                        rb.linearVelocity = Vector2.zero;
                        currentState = EnemyState.Patrol;
                    }
                }
                else
                {
                    outOfSightTimer = 0f; // Reset timer while seeing player
                    
                    if (distanceToPlayer <= attackRange)
                    {
                        StartCharging();
                    }
                    else
                    {
                        Vector2 dir = (playerTarget.position - transform.position).normalized;
                        
                        float speedInDir = Vector2.Dot(rb.linearVelocity, dir);
                        if (speedInDir < maxTrackingVelocity)
                        {
                            rb.AddForce(dir * trackingSpeed);
                        }
                        ResetRotation();
                    }
                }
                break;

            case EnemyState.Charging:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
                Vector2 targetDir = (playerTarget.position - transform.position).normalized;
                currentAimDirection = Vector3.Slerp(currentAimDirection, targetDir, Time.fixedDeltaTime * chargeRotationSpeed).normalized;
                RotateSprite(currentAimDirection, 50f); // Fast visual rotation to match the strictly controlled aim vector
                break;
                
            case EnemyState.Dashing:
                // Rotation remains locked to the dash direction
                RotateSprite(dashDirection);
                break;
        }
    }

    void Update()
    {
        if (currentState == EnemyState.Dead) return;

        if (currentState == EnemyState.Charging)
        {
            stateTimer -= Time.deltaTime;
            
            if (spriteRenderer != null)
            {
                float blink = Mathf.PingPong(Time.time * 15f, 1f);
                spriteRenderer.color = Color.Lerp(chargingColor, Color.white, blink);
            }

            if (stateTimer <= 0f)
            {
                StartDashing();
            }
        }
    }
    
    private void HandlePatrol()
    {
        if (isWaitingInPatrol)
        {
            patrolWaitTimer -= Time.fixedDeltaTime;
            // Slowly drift to a stop while waiting
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 3f);
            
            if (patrolWaitTimer <= 0f)
            {
                Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
                Vector2 potentialTarget = spawnPoint + randomOffset;
                
                // Ensure the point is not behind a wall relative to our current position
                RaycastHit2D hit = Physics2D.Linecast(transform.position, potentialTarget, obstacleLayer);
                if (hit.collider != null)
                {
                    // Target slightly in front of the wall
                    Vector2 dir = (potentialTarget - (Vector2)transform.position).normalized;
                    currentPatrolTarget = hit.point - dir * 0.5f;
                }
                else
                {
                    currentPatrolTarget = potentialTarget;
                }
                
                isWaitingInPatrol = false;
            }
        }
        else
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget);
            
            if (distanceToTarget < 0.5f)
            {
                isWaitingInPatrol = true;
                patrolWaitTimer = patrolWaitTime;
            }
            else
            {
                Vector2 dir = (currentPatrolTarget - (Vector2)transform.position).normalized;
                
                float speedInDir = Vector2.Dot(rb.linearVelocity, dir);
                if (speedInDir < maxTrackingVelocity * 0.5f)
                {
                    rb.AddForce(dir * trackingSpeed * 0.5f);
                }
                    
                ResetRotation();
            }
        }
    }

    private bool CanSeePlayer()
    {
        if (playerTarget == null) return false;
        
        // Cast a line from enemy to player. If it hits an obstacle, we can't see them.
        RaycastHit2D hit = Physics2D.Linecast(transform.position, playerTarget.position, obstacleLayer);
        return hit.collider == null;
    }

    private void ResetRotation()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.fixedDeltaTime * 5f);
        FaceDirection(rb.linearVelocity);
    }

    private void FaceDirection(Vector2 dir)
    {
        if (spriteRenderer == null || Mathf.Abs(dir.x) < 0.05f) return;

        bool movingRight = dir.x > 0;
        
        if (spriteFacing == FacingDirection.Left)
        {
            spriteRenderer.flipX = movingRight;
        }
        else if (spriteFacing == FacingDirection.Right)
        {
            spriteRenderer.flipX = !movingRight;
        }
    }

    private void RotateSprite(Vector2 direction, float turnSpeed = 10f)
    {
        if (direction == Vector2.zero) return;
        
        FaceDirection(direction);
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        FacingDirection currentFacing = spriteFacing;
        if (spriteRenderer != null && spriteRenderer.flipX)
        {
            if (spriteFacing == FacingDirection.Left) currentFacing = FacingDirection.Right;
            else if (spriteFacing == FacingDirection.Right) currentFacing = FacingDirection.Left;
        }
        
        // Adjust angle based on which way the sprite is currently facing
        switch (currentFacing)
        {
            case FacingDirection.Right:
                // Atan2 expects right as 0 degrees, so no offset needed
                break;
            case FacingDirection.Up:
                angle -= 90f;
                break;
            case FacingDirection.Left:
                angle += 180f;
                break;
            case FacingDirection.Down:
                angle += 90f;
                break;
        }
        
        // Apply rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.fixedDeltaTime * turnSpeed);
    }

    private void StartCharging()
    {
        currentState = EnemyState.Charging;
        stateTimer = chargeTime;
        currentAimDirection = (playerTarget.position - transform.position).normalized;
        
        if (spriteRenderer != null) spriteRenderer.color = chargingColor;
        if (chargeParticles != null) chargeParticles.Play();
        if (chargeSound != null) AudioManager.PlaySFXAt(chargeSound, transform.position, volume);
    }

    private void StartDashing()
    {
        currentState = EnemyState.Dashing;
        
        if (spriteRenderer != null) spriteRenderer.color = dashingColor;
        if (chargeParticles != null) chargeParticles.Stop();
        if (dashSound != null) AudioManager.PlaySFXAt(dashSound, transform.position, volume);
        
        dashDirection = currentAimDirection.normalized;
        rb.AddForce(dashDirection * dashForce, ForceMode2D.Impulse);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == EnemyState.Dead) return;

        if (collision.gameObject.TryGetComponent<DroneHealth>(out var health))
        {
            if (currentState == EnemyState.Dashing)
            {
                // Apply controlled knockback instead of raw momentum transfer
                Rigidbody2D droneRb = collision.rigidbody; // Free cached property
                if (droneRb != null)
                {
                    // Partially dampen the velocity so it isn't completely stopped, but prevents launching to space
                    droneRb.linearVelocity = Vector2.Lerp(droneRb.linearVelocity, Vector2.zero, 0.5f);
                    
                    // Apply a sensible knockback
                    Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                    droneRb.AddForce(knockbackDir * playerKnockbackForce, ForceMode2D.Impulse);
                }
                
                health.TakeDamage(damageToPlayer);
                Die();
            }
            // If it hits the player while Tracking or Charging, it just physically bumps them without exploding.
        }
        else 
        {
            float impactForce = collision.relativeVelocity.magnitude;
            if (currentState == EnemyState.Dashing || impactForce >= crashThreshold)
            {
                // Crashed into a wall or obstacle hard enough, or while dashing!
                Die();
            }
        }
    }

    private void Die()
    {
        currentState = EnemyState.Dead;
        
        if (crashSound != null) 
            AudioManager.PlaySFXAt(crashSound, transform.position, volume);
            
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
    
    public void TakeDamage(float amount)
    {
        Die(); // Instant kill regardless of damage amount
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
        }
        else
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }
    }
}
