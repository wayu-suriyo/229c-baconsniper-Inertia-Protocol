using UnityEngine;

public enum ObstaclePatrolMode { Loop, PingPong }

[RequireComponent(typeof(Rigidbody2D))]
public class MovingObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public float waitTimeAtWaypoint = 0.5f;
    public ObstaclePatrolMode patrolMode = ObstaclePatrolMode.PingPong;

    [Header("Waypoints")]
    public Vector2[] waypointOffsets = { new Vector2(0, 3f), new Vector2(0, -3f) };

    [Header("Activation")]
    public bool startActive = true;

    private Rigidbody2D rb;
    private Vector2[] worldWaypoints;
    private int currentIndex = 0;
    private int direction = 1;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isMoving = false;
    private Vector2 startPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        startPosition = rb.position;
        BakeWorldWaypoints();
        isMoving = startActive;
    }

    private void BakeWorldWaypoints()
    {
        worldWaypoints = new Vector2[waypointOffsets.Length];
        for (int i = 0; i < waypointOffsets.Length; i++)
        {
            worldWaypoints[i] = startPosition + waypointOffsets[i];
        }
    }

    void FixedUpdate()
    {
        if (!isMoving || worldWaypoints == null || worldWaypoints.Length < 2) return;

        if (isWaiting)
        {
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= waitTimeAtWaypoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                AdvanceWaypoint();
            }
            return;
        }

        Vector2 target = worldWaypoints[currentIndex];
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (Vector2.Distance(rb.position, target) < 0.05f)
        {
            isWaiting = true;
        }
    }

    private void AdvanceWaypoint()
    {
        if (patrolMode == ObstaclePatrolMode.Loop)
        {
            currentIndex = (currentIndex + 1) % worldWaypoints.Length;
        }
        else
        {
            currentIndex += direction;
            if (currentIndex >= worldWaypoints.Length || currentIndex < 0)
            {
                direction *= -1;
                currentIndex += direction * 2;
            }
        }
    }

    public void Activate()   => isMoving = true;
    public void Deactivate() => isMoving = false;

    void OnDrawGizmos()
    {
        if (waypointOffsets == null || waypointOffsets.Length < 2) return;

        Vector2 origin = Application.isPlaying ? startPosition : (Vector2)transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f);

        for (int i = 0; i < waypointOffsets.Length; i++)
        {
            Vector2 pointA = origin + waypointOffsets[i];
            Vector2 pointB = origin + waypointOffsets[(i + 1) % waypointOffsets.Length];

            Gizmos.DrawWireSphere(pointA, 0.25f);

            if (patrolMode == ObstaclePatrolMode.Loop || i < waypointOffsets.Length - 1)
            {
                Gizmos.DrawLine(pointA, pointB);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + waypointOffsets[0]);
    }
}
