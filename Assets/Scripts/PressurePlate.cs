using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Detection Settings")]
    public float hoverVelocityThreshold = 2f;

    [Header("Press Visual")]
    public float pressDepth = 0.15f;

    [Header("Targets")]
    public MovingObstacle movingObstacleTarget;
    public PhysicsDoor doorTarget;
    public GravityZone gravityZoneTarget;

    [Header("Timer (Door & Gravity Zone only — counts after drone leaves)")]
    public bool useTimer = true;
    public float activeDuration = 4f;

    [Header("Visual & Audio")]
    public SpriteRenderer plateRenderer;
    [Tooltip("How fast the plate sinks and rises (higher = snappier)")]
    public float pressSmoothSpeed = 8f;
    public Color unpressedColor = new Color(0.6f, 0.6f, 0.6f);
    public Color pressedColor = new Color(0.2f, 1f, 0.4f);
    public AudioClip pressSound;
    public AudioClip releaseSound;
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("Contact Debounce")]
    [Tooltip("Grace period (seconds) after the drone breaks contact before the plate counts it as 'left'. Prevents micro-bounce jitter.")]
    public float exitGraceTime = 0.15f;

    private bool isDroneOnPlate = false;
    private bool targetsActivated = false;
    private bool timerRunning = false;
    private bool movingObstacleActivated = false;
    private float timerRemaining = 0f;
    private float exitGraceTimer = 0f;
    private bool pendingExit = false;
    private Vector3 originalRendererLocalPos;
    private Vector3 targetLocalPos;
    private DroneController cachedDrone;

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().isTrigger = false;

        if (plateRenderer != null)
        {
            originalRendererLocalPos = plateRenderer.transform.localPosition;
            targetLocalPos = originalRendererLocalPos;
            plateRenderer.color = unpressedColor;

            // Make the button render BEHIND the base sprite so the base frame appears on top,
            // creating a recessed/inset visual. Set button Order in Layer lower than its parent.
            SpriteRenderer parentSr = transform.parent != null
                ? transform.parent.GetComponent<SpriteRenderer>()
                : null;
            if (parentSr != null)
                plateRenderer.sortingOrder = parentSr.sortingOrder - 1;
        }

        movingObstacleTarget?.Deactivate();
        doorTarget?.Deactivate();
        gravityZoneTarget?.Deactivate();
    }

    void Update()
    {
        // Smooth sink/rise animation
        if (plateRenderer != null)
        {
            plateRenderer.transform.localPosition = Vector3.Lerp(
                plateRenderer.transform.localPosition,
                targetLocalPos,
                Time.deltaTime * pressSmoothSpeed
            );
        }

        // --- Exit grace period: absorb micro-bounces ---
        if (pendingExit)
        {
            exitGraceTimer -= Time.deltaTime;
            if (exitGraceTimer <= 0f)
            {
                // Drone genuinely left the plate
                pendingExit = false;
                isDroneOnPlate = false;
                cachedDrone = null;
                HandleDroneExit();
            }
        }

        // --- Timed release (door/gravity zone) ---
        if (!timerRunning) return;

        // Timer only counts while drone is OFF the plate
        if (isDroneOnPlate)
        {
            timerRemaining = activeDuration;
            return;
        }

        timerRemaining -= Time.deltaTime;
        if (timerRemaining <= 0f)
        {
            timerRunning = false;
            targetsActivated = false;
            SetVisual(false);
            ReleaseTimedTargets();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        DroneController drone = collision.gameObject.GetComponent<DroneController>();
        if (drone == null) return;

        cachedDrone = drone;
        isDroneOnPlate = true;
        pendingExit = false;  // Cancel any pending exit — drone is back
        timerRunning = false;

        TryActivate(collision.rigidbody);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (cachedDrone == null) return;
        isDroneOnPlate = true;
        pendingExit = false;  // Still in contact — cancel any pending exit

        if (!targetsActivated)
            TryActivate(collision.rigidbody);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (cachedDrone == null) return;

        // Don't immediately count as "left" — start a grace period
        // to absorb physics micro-bounces
        pendingExit = true;
        exitGraceTimer = exitGraceTime;
    }

    /// <summary>
    /// Called after the exit grace period expires — the drone genuinely left.
    /// </summary>
    private void HandleDroneExit()
    {
        if (!targetsActivated) return;

        bool hasTimedTargets = doorTarget != null || gravityZoneTarget != null;
        if (useTimer && hasTimedTargets)
        {
            timerRemaining = activeDuration;
            timerRunning = true;
        }
        else
        {
            targetsActivated = false;
            SetVisual(false);
            ReleaseTimedTargets();
        }
    }

    private void TryActivate(Rigidbody2D droneRb)
    {
        if (droneRb == null) return;
        if (droneRb.linearVelocity.magnitude > hoverVelocityThreshold) return;

        targetsActivated = true;
        SetVisual(true);
        AudioManager.PlaySFX(pressSound, volume);

        if (movingObstacleTarget != null && !movingObstacleActivated)
        {
            movingObstacleTarget.Activate();
            movingObstacleActivated = true;
        }

        doorTarget?.Activate();
        gravityZoneTarget?.Activate();
    }

    private void ReleaseTimedTargets()
    {
        doorTarget?.Deactivate();
        gravityZoneTarget?.Deactivate();
        AudioManager.PlaySFX(releaseSound, volume);
    }

    private void SetVisual(bool pressed)
    {
        if (plateRenderer == null) return;
        plateRenderer.color = pressed ? pressedColor : unpressedColor;
        targetLocalPos = pressed
            ? originalRendererLocalPos + Vector3.down * pressDepth
            : originalRendererLocalPos;
    }

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(
                transform.TransformPoint(col.offset),
                transform.rotation,
                transform.lossyScale
            );
            Gizmos.DrawCube(Vector3.zero, col.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        // Draw connecting lines to linked objects in Edit Mode
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f); // Yellowish connecting line

        if (movingObstacleTarget != null)
        {
            Gizmos.DrawLine(transform.position, movingObstacleTarget.transform.position);
            Gizmos.DrawWireSphere(movingObstacleTarget.transform.position, 0.3f);
        }

        if (doorTarget != null)
        {
            Gizmos.DrawLine(transform.position, doorTarget.transform.position);
            Gizmos.DrawWireSphere(doorTarget.transform.position, 0.3f);
        }

        if (gravityZoneTarget != null)
        {
            Gizmos.DrawLine(transform.position, gravityZoneTarget.transform.position);
            Gizmos.DrawWireSphere(gravityZoneTarget.transform.position, 0.3f);
        }
    }
}
