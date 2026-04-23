using UnityEngine;

public class MagneticTrap : MonoBehaviour
{
    [Header("Magnetic Trap Settings")]
    [Tooltip("ค่าคงที่แรงโน้มถ่วง (G)")]
    public float gravitationalConstant = 10f;
    [Tooltip("มวลของกับดัก (m1)")]
    public float trapMass = 50f;
    [Tooltip("Maximum distance the trap can reach")]
    public float maxInfluenceRadius = 15f;
    [Tooltip("ระยะห่างน้อยที่สุดที่จะนำมาคำนวณ (ป้องกันแรงดูดเป็นอนันต์เวลาเข้าใกล้จุดกึ่งกลาง)")]
    public float minSafeDistance = 2f;
    [Tooltip("แรงดูดสูงสุดที่จะกระทำกับโดรน (ควรน้อยกว่าแรงพ่นเทอร์โบของโดรน เพื่อให้เร่งเครื่องสู้หนีออกมาได้)")]
    public float maxPullForce = 25f;

    [Header("Pulse Settings")]
    [Tooltip("เปิดระบบให้แม่เหล็กทำงานสลับกับหยุดพัก")]
    public bool isPulseMode = true;
    [Tooltip("ระยะเวลาที่แม่เหล็กปล่อยพลังดูด (วินาที)")]
    public float activeTime = 2f;
    [Tooltip("ระยะเวลาที่แม่เหล็กหยุดพักให้โดรนหนี (วินาที)")]
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

    private Transform targetDrone;
    private Rigidbody2D droneRb;
    private SpriteRenderer spriteRenderer;
    private LineRenderer radiusLine;
    private AudioSource audioSource;

    private bool isActive = true;
    private float pulseTimer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; 

        radiusLine = GetComponent<LineRenderer>();
        if (radiusLine == null)
        {
            radiusLine = gameObject.AddComponent<LineRenderer>();
            radiusLine.material = new Material(Shader.Find("Sprites/Default"));
            radiusLine.useWorldSpace = false;
        }
        DrawRadiusCircle();

        DroneController drone = FindAnyObjectByType<DroneController>();
        if (drone != null)
        {
            targetDrone = drone.transform;
            droneRb = drone.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("MagneticTrap could not find the DroneController in the scene!");
        }
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
        if (targetDrone == null || droneRb == null || !isActive) return;

        Vector2 forceDirection = (Vector2)transform.position - (Vector2)targetDrone.position;
        float distance = forceDirection.magnitude;

        if (distance > 0f && distance <= maxInfluenceRadius)
        {
            float mathDistance = Mathf.Max(distance, minSafeDistance);

            float forceMagnitude = gravitationalConstant * (trapMass * droneRb.mass) / (mathDistance * mathDistance);
            
            forceMagnitude = Mathf.Clamp(forceMagnitude, 0f, maxPullForce);

            Vector2 appliedForce = forceDirection.normalized * forceMagnitude;

            droneRb.WakeUp();

            if (forceDirection.y > 0)
            {
                appliedForce.y += 9.81f * droneRb.mass * forceDirection.normalized.y;
            }

            droneRb.AddForce(appliedForce, ForceMode2D.Force);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxInfluenceRadius);
    }
}
