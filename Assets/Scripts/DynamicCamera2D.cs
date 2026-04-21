using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicCamera2D : MonoBehaviour
{
    public Transform target;

    [Header("Camera Speeds")]
    public float smoothSpeed = 5f;
    public float zoomSpeed = 3f;

    [Header("Auto Zoom (Orthographic)")]
    public float zoomPaddingMultiplier = 1.3f;
    public float minOrthoSize = 4f;
    public float maxOrthoSize = 10f;

    [Header("Raycast Penetration")]
    public float rayDistance = 50f;
    public LayerMask groundLayer;

    [Header("Anti-Jerk")]
    public float fallbackDistance = 5f;
    public float memoryTime = 0.5f;

    [HideInInspector]
    public BoxCollider2D currentFocusZone; 

    private Camera cam;
    private float lastGoodFloorY;
    private float lastGoodCeilingY;
    private float floorTimer;
    private float ceilingTimer;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        if (target != null)
        {
            lastGoodFloorY = target.position.y - fallbackDistance;
            lastGoodCeilingY = target.position.y + fallbackDistance;
        }
    }

    void FixedUpdate()
    {
        if (target == null || cam == null) return;

        float lowestFloorY = target.position.y;
        float highestCeilingY = target.position.y;

        if (currentFocusZone != null)
        {
            lowestFloorY = currentFocusZone.bounds.min.y;
            highestCeilingY = currentFocusZone.bounds.max.y;
        }
        else
        {
            RaycastHit2D[] downHits = Physics2D.RaycastAll(target.position, Vector2.down, rayDistance, groundLayer);
            bool foundFloor = false;

            foreach (RaycastHit2D hit in downHits)
            {
                if (!hit.collider.isTrigger && (!foundFloor || hit.point.y < lowestFloorY))
                {
                    lowestFloorY = hit.point.y;
                    foundFloor = true;
                }
            }

            if (foundFloor)
            {
                lastGoodFloorY = lowestFloorY;
                floorTimer = 0f;
            }
            else
            {
                floorTimer += Time.fixedDeltaTime;
                if (floorTimer <= memoryTime) lowestFloorY = lastGoodFloorY;
                else lowestFloorY = target.position.y - fallbackDistance;
            }

            RaycastHit2D[] upHits = Physics2D.RaycastAll(target.position, Vector2.up, rayDistance, groundLayer);
            bool foundCeiling = false;

            foreach (RaycastHit2D hit in upHits)
            {
                if (!hit.collider.isTrigger && (!foundCeiling || hit.point.y > highestCeilingY))
                {
                    highestCeilingY = hit.point.y;
                    foundCeiling = true;
                }
            }

            if (foundCeiling)
            {
                lastGoodCeilingY = highestCeilingY;
                ceilingTimer = 0f;
            }
            else
            {
                ceilingTimer += Time.fixedDeltaTime;
                if (ceilingTimer <= memoryTime) highestCeilingY = lastGoodCeilingY;
                else highestCeilingY = target.position.y + fallbackDistance;
            }
        }

        float currentLevelHeight = highestCeilingY - lowestFloorY;
        
        float desiredOrthoSize = (currentLevelHeight / 2f) * zoomPaddingMultiplier;
        desiredOrthoSize = Mathf.Clamp(desiredOrthoSize, minOrthoSize, maxOrthoSize);

        float targetY = (lowestFloorY + highestCeilingY) / 2f;

        Vector3 targetPos = new Vector3(
            target.position.x,
            Mathf.Lerp(transform.position.y, targetY, Time.fixedDeltaTime * smoothSpeed),
            transform.position.z 
        );

        transform.position = targetPos;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredOrthoSize, Time.fixedDeltaTime * zoomSpeed);
    }
}
