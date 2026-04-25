using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public float waitTimeAtWaypoint = 1f;

    [Header("Waypoints")]
    public Transform[] waypoints;

    private Rigidbody2D rb;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        if (isWaiting)
        {
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= waitTimeAtWaypoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                SetNextWaypoint();
            }
            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        Vector2 newPosition = Vector2.MoveTowards(rb.position, target.position, speed * Time.fixedDeltaTime);
        
        rb.MovePosition(newPosition);

        if (Vector2.Distance(rb.position, target.position) < 0.05f)
        {
            isWaiting = true;
        }
    }

    private void SetNextWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = 0;
        }
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                if (i < waypoints.Length - 1 && waypoints[i+1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
                }
            }
        }
        
        if (waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }
    }
}
