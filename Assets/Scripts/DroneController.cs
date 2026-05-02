using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class DroneController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float thrustForce = 35f;
    public float torqueForce = 15f;
    public float autoLevelForce = 10f;

    [Header("Physics Limits")]
    public float maxSpeed = 15f;
    public float maxTiltAngle = 70f;

    [Header("Forward Flight (Hold Shift)")]
    [Tooltip("Maximum tilt angle while Shift is held (allows steeper forward flight).")]
    public float forwardFlightMaxTilt = 60f;
    [Tooltip("How much tilt compensation is removed. 1.0 = fully removed (raw directional thrust), 0.0 = no change.")]
    [Range(0f, 1f)]
    public float forwardFlightCompReduction = 0.8f;

    [Header("Air Resistance (Physics Topic E)")]
    [Tooltip("ความหนาแน่นของอากาศ (rho)")]
    public float airDensity = 1.25f;
    [Tooltip("พื้นที่ต้านทานลมของหน้าตัดโดรน (A) - ลดลงถ้าหนืดไป")]
    public float surfaceArea = 0.2f;
    [Tooltip("สัมประสิทธิ์แรงต้าน (Cd) - ลดลงถ้าหนืดไป")]
    public float dragCoefficient = 0.5f;

    [Header("Visuals & VFX")]
    public ParticleSystem exhaustParticles;
    
    [Header("Animation Settings")]
    public Animator undersideAnimator;
    public float baseAnimSpeed = 1f;
    public float maxAnimSpeedMultiplier = 3f;
    public float speedDecayRate = 2f;

    [Header("Audio")]
    [Tooltip("Looping engine hum while drone is active (mid-air idle)")]
    public AudioClip engineHumClip;
    [Tooltip("Looping thruster burst played only while Space is held")]
    public AudioClip thrusterClip;
    [Range(0f, 1f)] public float engineVolume = 0.4f;
    [Range(0f, 1f)] public float thrusterVolume = 0.7f;
    [Tooltip("Time in seconds for thruster audio to fade out after releasing thrust")]
    public float thrusterFadeOutTime = 0.25f;

    private Rigidbody2D rb;
    private float tiltInput = 0f;
    private bool isThrusting = false;
    
    private float currentAnimMultiplier = 1f;
    private float lastThrustTime = 0f;
    private bool wasEmitting = true; // Start true so first frame forces sync to off
    private static readonly int AnimIsOpen = Animator.StringToHash("IsOpen");
    private bool isForwardFlight = false;
    
    [HideInInspector]
    public bool invertControls = false;
    [HideInInspector]
    public float invertThrustMultiplier = 1f;
    [HideInInspector]
    public float invertFuelMultiplier = 1f;

    private WindZone activeWindZone = null;

    private FuelSystem fuelSystem;
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private InputAction moveAction;
    private AudioSource engineSource;
    private AudioSource thrusterSource;
    private Coroutine thrusterFadeCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        fuelSystem = GetComponent<FuelSystem>();
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput != null)
        {
            jumpAction = playerInput.actions["Jump"];
            moveAction = playerInput.actions["Move"];
        }

        rb.angularDamping = 3f;
        rb.linearDamping = 0.5f;

        engineSource = CreateLoopingAudioSource(engineHumClip, engineVolume);
        thrusterSource = CreateLoopingAudioSource(thrusterClip, thrusterVolume);

        if (engineSource != null) engineSource.Play();
    }

    private AudioSource CreateLoopingAudioSource(AudioClip clip, float vol)
    {
        if (clip == null) return null;
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.volume = vol;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        return src;
    }

    void OnMove(InputValue value)
    {
        var obj = value.Get();
        if (obj is Vector2 v2) {
            tiltInput = -v2.x; 
        } else if (obj is float f) {
            tiltInput = -f; 
        } else {
            tiltInput = 0f;
        }
    }

    void OnJump(InputValue value)
    {
        isThrusting = value.isPressed;
        
        if (isThrusting)
        {
            float timeSinceLast = Time.time - lastThrustTime;
            lastThrustTime = Time.time;
            
            if (timeSinceLast < 0.4f)
            {
                currentAnimMultiplier += 0.6f;
                currentAnimMultiplier = Mathf.Min(currentAnimMultiplier, maxAnimSpeedMultiplier);
            }
        }
    }

    void Update()
    {
        if (currentAnimMultiplier > 1f)
        {
            currentAnimMultiplier -= Time.deltaTime * speedDecayRate;
            currentAnimMultiplier = Mathf.Max(1f, currentAnimMultiplier);
        }

        if (undersideAnimator != null)
        {
            undersideAnimator.speed = baseAnimSpeed * currentAnimMultiplier;
        }
    }

    void FixedUpdate()
    {
        if (jumpAction != null) isThrusting = jumpAction.IsPressed();
        if (moveAction != null) {
            var v2 = moveAction.ReadValue<Vector2>();
            tiltInput = -v2.x;
        }

        bool activeThrust = invertControls ? !isThrusting : isThrusting;
        float activeTilt = invertControls ? -tiltInput : tiltInput;

        // Forward Flight: hold Shift to unlock steeper tilt
        isForwardFlight = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        HandleTilt(activeTilt, activeThrust);
        HandleThrust(activeThrust);
        CalculateAirResistance(); 
        ClampVelocity();
    }

    public void ApplyWindOverride(WindZone zone)
    {
        activeWindZone = zone;
    }

    public void RemoveWindOverride(WindZone zone)
    {
        if (activeWindZone == zone)
        {
            activeWindZone = null;
        }
    }

    private void CalculateAirResistance()
    {
        Vector2 velocity = rb.linearVelocity;
        float speedSqr = velocity.sqrMagnitude;

        if (speedSqr > 0.01f)
        {
            float speed = Mathf.Sqrt(speedSqr);
            float dragForceMagnitude = 0.5f * airDensity * speedSqr * dragCoefficient * surfaceArea;
            
            Vector2 dragVector = -(velocity / speed) * dragForceMagnitude;
            
            rb.AddForce(dragVector, ForceMode2D.Force);
        }
    }

    private void HandleTilt(float activeTilt, bool activeThrust)
    {
        float currentAngle = rb.rotation;
        currentAngle = Mathf.DeltaAngle(0, currentAngle);

        float currentTorque = (activeWindZone != null) ? activeWindZone.droneTorqueOverride : torqueForce;
        float baseTilt = (activeWindZone != null) ? activeWindZone.droneTiltOverride : maxTiltAngle;

        // Forward Flight: use wider tilt angle while Shift is held
        float currentMaxTilt = (isForwardFlight && Mathf.Abs(activeTilt) > 0.1f)
            ? forwardFlightMaxTilt
            : baseTilt;

        if (Mathf.Abs(activeTilt) > 0.1f)
        {
            float targetAngle = activeTilt * currentMaxTilt;
            
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            rb.AddTorque(angleDifference * currentTorque * Time.fixedDeltaTime);
        }
        else if (autoLevelForce > 0)
        {
            float levelMultiplier = activeThrust ? 0.5f : 1.5f;
            float angleDifference = Mathf.DeltaAngle(currentAngle, 0f); 
            rb.AddTorque(angleDifference * (autoLevelForce * levelMultiplier) * Time.fixedDeltaTime);
        }
    }

    private void HandleThrust(bool activeThrust)
    {
        bool hasFuel = fuelSystem == null || !fuelSystem.IsOutOfFuel;
        bool shouldEmit = activeThrust && hasFuel;

        if (shouldEmit)
        {
            float angleInRad = Mathf.Abs(rb.rotation) * Mathf.Deg2Rad;
            float fullCompensation = 1f / Mathf.Max(Mathf.Cos(angleInRad), 0.5f);

            // In Forward Flight, reduce compensation so tilting redirects thrust sideways
            float tiltCompensation = fullCompensation;
            if (isForwardFlight && Mathf.Abs(rb.rotation) > 5f)
            {
                float reducedCompensation = Mathf.Lerp(fullCompensation, 1f, forwardFlightCompReduction);
                tiltCompensation = reducedCompensation;
            }

            float baseThrust = (activeWindZone != null) ? activeWindZone.droneThrustOverride : thrustForce;
            float currentThrust = invertControls ? baseThrust * invertThrustMultiplier : baseThrust;
            rb.AddForce(transform.up * (currentThrust * tiltCompensation), ForceMode2D.Force);

            if (fuelSystem != null)
            {
                float currentFuelMult = invertControls ? invertFuelMultiplier : 1f;
                fuelSystem.ConsumeFuel(currentFuelMult);
            }
        }

        if (exhaustParticles != null && shouldEmit != wasEmitting)
        {
            var emission = exhaustParticles.emission;
            emission.enabled = shouldEmit;
            wasEmitting = shouldEmit;
        }

        if (undersideAnimator != null)
        {
            undersideAnimator.SetBool(AnimIsOpen, shouldEmit);
        }

        if (thrusterSource != null)
        {
            if (shouldEmit)
            {
                // Cancel any in-progress fade and restore full volume instantly
                if (thrusterFadeCoroutine != null)
                {
                    StopCoroutine(thrusterFadeCoroutine);
                    thrusterFadeCoroutine = null;
                }
                thrusterSource.volume = thrusterVolume;
                if (!thrusterSource.isPlaying) thrusterSource.Play();
            }
            else if (thrusterSource.isPlaying && thrusterFadeCoroutine == null)
            {
                thrusterFadeCoroutine = StartCoroutine(FadeThrusterOut());
            }
        }
    }

    private System.Collections.IEnumerator FadeThrusterOut()
    {
        float startVolume = thrusterSource.volume;
        float elapsed = 0f;

        while (elapsed < thrusterFadeOutTime)
        {
            elapsed += Time.deltaTime;
            thrusterSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / thrusterFadeOutTime);
            yield return null;
        }

        thrusterSource.Stop();
        thrusterSource.volume = thrusterVolume; // Restore for next time
        thrusterFadeCoroutine = null;
    }

    private void ClampVelocity()
    {
        Vector2 clampedVelocity = rb.linearVelocity;
        
        if (Mathf.Abs(clampedVelocity.x) > maxSpeed)
        {
            clampedVelocity.x = Mathf.Sign(clampedVelocity.x) * maxSpeed;
        }
        
        if (Mathf.Abs(clampedVelocity.y) > maxSpeed * 1.5f)
        {
            clampedVelocity.y = Mathf.Sign(clampedVelocity.y) * maxSpeed * 1.5f;
        }

        rb.linearVelocity = clampedVelocity;
    }
}