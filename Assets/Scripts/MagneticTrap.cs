using UnityEngine;

public class MagneticTrap : MonoBehaviour
{
    [Header("Magnetic Trap Settings")]
    public float gravitationalConstant = 10f;
    public float trapMass = 50f;
    public float maxInfluenceRadius = 15f;
    [Tooltip("ระยะห่างน้อยที่สุดที่จะนำมาคำนวณ (ป้องกันแรงดูดเป็นอนันต์เวลาเข้าใกล้จุดกึ่งกลาง)")]
    public float minSafeDistance = 2f;
    [Tooltip("แรงดูดสูงสุดที่จะกระทำกับโดรน (ควรน้อยกว่าแรงพ่นเทอร์โบของโดรน เพื่อให้เร่งเครื่องสู้หนีออกมาได้)")]
    public float maxPullForce = 25f;

    [Header("Pulse Settings")]
    public bool isPulseMode = true;
    public float activeTime = 2f;
    public float restTime = 3f;

    [Header("Audio Settings")]
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Header("Visual Radius")]
    public int circleSegments = 50;
    public float lineWidth = 0.05f;
    public Color activeColor = new Color(1f, 0f, 0f, 0.5f);
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);

    [Header("Detection")]
    [Tooltip("Set to Player + Enemy layers to avoid scanning walls/triggers")]
    public LayerMask targetLayer;

    private SpriteRenderer spriteRenderer;
    private LineRenderer radiusLine;
    private AudioSource audioSource;

    private bool isActive = true;
    private float pulseTimer = 0f;
    private Collider2D[] hitBuffer = new Collider2D[16];

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = maxInfluenceRadius;

        radiusLine = GetComponent<LineRenderer>();
        if (radiusLine == null)
        {
            radiusLine = gameObject.AddComponent<LineRenderer>();
            radiusLine.material = new Material(Shader.Find("Sprites/Default"));
            radiusLine.useWorldSpace = false;
        }
        DrawRadiusCircle();
    }

    void Update()
    {
        if (isPulseMode)
        {
            pulseTimer += Time.deltaTime;
            if (isActive && pulseTimer >= activeTime)
            {
                isActive = false;
                pulseTimer = 0f;
                if (turnOffSound != null) audioSource.PlayOneShot(turnOffSound, volume);
            }
            else if (!isActive && pulseTimer >= restTime)
            {
                isActive = true;
                pulseTimer = 0f;
                if (turnOnSound != null) audioSource.PlayOneShot(turnOnSound, volume);
            }
        }
        else
        {
            isActive = true;
        }

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = isActive ? 1f : 0.3f;
            spriteRenderer.color = c;
        }

        if (radiusLine != null)
        {
            Color targetColor = isActive ? activeColor : inactiveColor;
            radiusLine.startColor = targetColor;
            radiusLine.endColor = targetColor;
        }
    }

    void DrawRadiusCircle()
    {
        if (radiusLine == null) return;
        
        radiusLine.positionCount = circleSegments + 1;
        radiusLine.startWidth = lineWidth;
        radiusLine.endWidth = lineWidth;

        float angle = 0f;
        for (int i = 0; i < (circleSegments + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * maxInfluenceRadius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * maxInfluenceRadius;

            radiusLine.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / circleSegments);
        }
    }

    void FixedUpdate()
    {
        if (!isActive) return;

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, maxInfluenceRadius, hitBuffer, targetLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Rigidbody2D rb = hitBuffer[i].attachedRigidbody;
            if (rb == null || !hitBuffer[i].gameObject.activeInHierarchy) continue;

            Vector2 forceDirection = (Vector2)transform.position - (Vector2)hitBuffer[i].transform.position;
            float distance = forceDirection.magnitude;

            if (distance > 0f && distance <= maxInfluenceRadius)
            {
                float mathDistance = Mathf.Max(distance, minSafeDistance);
                float forceMagnitude = gravitationalConstant * (trapMass * rb.mass) / (mathDistance * mathDistance);
                forceMagnitude = Mathf.Clamp(forceMagnitude, 0f, maxPullForce);
                Vector2 appliedForce = forceDirection.normalized * forceMagnitude;

                rb.WakeUp();

                if (forceDirection.y > 0)
                {
                    float gravityCounterForce = -Physics2D.gravity.y * rb.gravityScale * rb.mass;
                    if (gravityCounterForce > 0)
                    {
                        appliedForce.y += gravityCounterForce * forceDirection.normalized.y;
                    }
                }

                rb.AddForce(appliedForce, ForceMode2D.Force);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxInfluenceRadius);
    }
}
