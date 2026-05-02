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
    [Tooltip("Looping sound played while the magnet is actively pulling")]
    public AudioClip activeLoopClip;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;
    [Range(0f, 1f)] public float loopVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    [Tooltip("Time in seconds for the loop audio to fade out when deactivated")]
    public float audioFadeOutTime = 0.3f;
    public float audioMinDistance = 5f;
    public float audioMaxDistance = 25f;

    [Header("Visual Sprites")]
    [Tooltip("Sprite shown while the magnet is actively pulling.")]
    public Sprite activeSprite;
    [Tooltip("Sprite shown while the magnet is resting/inactive.")]
    public Sprite inactiveSprite;

    [Header("VFX")]
    [Tooltip("Particle system that plays while the magnet is active. Will stop when resting.")]
    public ParticleSystem activeParticles;

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
    private Coroutine loopFadeCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = audioMinDistance;
        audioSource.maxDistance = audioMaxDistance;

        if (activeLoopClip != null)
        {
            audioSource.clip = activeLoopClip;
            audioSource.loop = true;
            audioSource.volume = loopVolume;
        }

        radiusLine = GetComponent<LineRenderer>();
        if (radiusLine == null)
        {
            radiusLine = gameObject.AddComponent<LineRenderer>();
            radiusLine.material = new Material(Shader.Find("Sprites/Default"));
            radiusLine.useWorldSpace = false;
        }
        DrawRadiusCircle();

        // Initialize visuals to match starting state
        SetVisual(isActive);
        
        StartCoroutine(DelayedAudioStart());
    }

    private System.Collections.IEnumerator DelayedAudioStart()
    {
        yield return new WaitForSeconds(0.1f); // Wait for camera to snap to player
        if (isActive && activeLoopClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        if (isPulseMode)
        {
            pulseTimer += Time.deltaTime;
            if (isActive && pulseTimer >= activeTime)
            {
                Deactivate();
                pulseTimer = 0f;
            }
            else if (!isActive && pulseTimer >= restTime)
            {
                Activate();
                pulseTimer = 0f;
            }
        }
        else
        {
            if (!isActive) // Only set once if it wasn't already active
            {
                Activate();
            }
        }

        if (radiusLine != null)
        {
            Color targetColor = isActive ? activeColor : inactiveColor;
            radiusLine.startColor = targetColor;
            radiusLine.endColor = targetColor;
        }
    }

    private void Activate()
    {
        isActive = true;
        SetVisual(true);

        if (turnOnSound != null) AudioManager.PlaySFXAt(turnOnSound, transform.position, sfxVolume, audioMinDistance, audioMaxDistance);

        if (activeLoopClip != null)
        {
            if (loopFadeCoroutine != null)
            {
                StopCoroutine(loopFadeCoroutine);
                loopFadeCoroutine = null;
            }
            audioSource.volume = loopVolume;
            if (!audioSource.isPlaying) audioSource.Play();
        }
    }

    private void Deactivate()
    {
        isActive = false;
        SetVisual(false);

        if (turnOffSound != null) AudioManager.PlaySFXAt(turnOffSound, transform.position, sfxVolume, audioMinDistance, audioMaxDistance);

        if (activeLoopClip != null && audioSource.isPlaying && loopFadeCoroutine == null)
        {
            loopFadeCoroutine = StartCoroutine(FadeLoopOut());
        }
    }

    private System.Collections.IEnumerator FadeLoopOut()
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < audioFadeOutTime)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / audioFadeOutTime);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = loopVolume;
        loopFadeCoroutine = null;
    }

    private void SetVisual(bool active)
    {
        // Swap sprite
        if (spriteRenderer != null)
        {
            Sprite target = active ? activeSprite : inactiveSprite;
            if (target != null) spriteRenderer.sprite = target;
        }

        // Control particles
        if (activeParticles != null)
        {
            if (active)
                activeParticles.Play();
            else
                activeParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
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
