using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicCamera2D : MonoBehaviour
{
    public static DynamicCamera2D instance;
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

    [Header("Camera Shake")]
    public float shakeDecaySpeed = 5f;

    [HideInInspector]
    public BoxCollider2D currentFocusZone;

    private Camera cam;
    private float lastGoodFloorY;
    private float lastGoodCeilingY;
    private float floorTimer;
    private float ceilingTimer;

    private float shakeIntensity = 0f;
    private Vector3 shakeOffset = Vector3.zero;
    private RaycastHit2D[] rayBuffer = new RaycastHit2D[8];

    void Awake()
    {
        instance = this;
    }

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

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        float dt = Time.deltaTime;
        float lowestFloorY = target.position.y;
        float highestCeilingY = target.position.y;

        if (currentFocusZone != null)
        {
            lowestFloorY = currentFocusZone.bounds.min.y;
            highestCeilingY = currentFocusZone.bounds.max.y;
        }
        else
        {
            int downCount = Physics2D.RaycastNonAlloc(target.position, Vector2.down, rayBuffer, rayDistance, groundLayer);
            bool foundFloor = false;

            for (int i = 0; i < downCount; i++)
            {
                RaycastHit2D hit = rayBuffer[i];
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
                floorTimer += dt;
                if (floorTimer <= memoryTime)
                {
                    lowestFloorY = lastGoodFloorY;
                }
                else
                {
                    // Smoothly drift toward the fallback rather than snapping to target.position.y
                    float softFallback = target.position.y - fallbackDistance;
                    lowestFloorY = Mathf.Lerp(lastGoodFloorY, softFallback, (floorTimer - memoryTime) * 0.5f);
                    lastGoodFloorY = lowestFloorY;
                }
            }

            int upCount = Physics2D.RaycastNonAlloc(target.position, Vector2.up, rayBuffer, rayDistance, groundLayer);
            bool foundCeiling = false;

            for (int i = 0; i < upCount; i++)
            {
                RaycastHit2D hit = rayBuffer[i];
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
                ceilingTimer += dt;
                if (ceilingTimer <= memoryTime)
                {
                    highestCeilingY = lastGoodCeilingY;
                }
                else
                {
                    // Smoothly drift toward the fallback rather than snapping to target.position.y
                    float softFallback = target.position.y + fallbackDistance;
                    highestCeilingY = Mathf.Lerp(lastGoodCeilingY, softFallback, (ceilingTimer - memoryTime) * 0.5f);
                    lastGoodCeilingY = highestCeilingY;
                }
            }
        }

        float currentLevelHeight = highestCeilingY - lowestFloorY;
        
        float desiredOrthoSize = (currentLevelHeight / 2f) * zoomPaddingMultiplier;
        desiredOrthoSize = Mathf.Clamp(desiredOrthoSize, minOrthoSize, maxOrthoSize);

        float targetY = (lowestFloorY + highestCeilingY) / 2f;

        Vector3 targetPos = new Vector3(
            target.position.x,
            Mathf.Lerp(transform.position.y, targetY, dt * smoothSpeed),
            transform.position.z 
        );

        transform.position = targetPos;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredOrthoSize, dt * zoomSpeed);

        // Camera shake
        if (shakeIntensity > 0f)
        {
            shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeOffset.z = 0f;
            transform.position += shakeOffset;
            shakeIntensity = Mathf.MoveTowards(shakeIntensity, 0f, shakeDecaySpeed * Time.unscaledDeltaTime);
        }
    }

    public static void Shake(float intensity)
    {
        if (instance != null)
            instance.shakeIntensity = Mathf.Max(instance.shakeIntensity, intensity);
    }

    public static void StopShake()
    {
        if (instance != null)
            instance.shakeIntensity = 0f;
    }
}
