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

    [Header("Air Resistance (Physics Topic E)")]
    [Tooltip("ความหนาแน่นของอากาศ (rho)")]
    public float airDensity = 1.25f;
    [Tooltip("พื้นที่ต้านทานลมของหน้าตัดโดรน (A) - ลดลงถ้าหนืดไป")]
    public float surfaceArea = 0.2f;
    [Tooltip("สัมประสิทธิ์แรงต้าน (Cd) - ลดลงถ้าหนืดไป")]
    public float dragCoefficient = 0.5f;

    [Header("VFX")]
    public ParticleSystem exhaustParticles;

    [Header("Audio")]
    [Tooltip("Looping engine hum while drone is active (mid-air idle)")]
    public AudioClip engineHumClip;
    [Tooltip("Looping thruster burst played only while Space is held")]
    public AudioClip thrusterClip;
    [Range(0f, 1f)] public float engineVolume = 0.4f;
    [Range(0f, 1f)] public float thrusterVolume = 0.7f;

    private Rigidbody2D rb;
    private float tiltInput = 0f;
    private bool isThrusting = false;
    private FuelSystem fuelSystem;
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private InputAction moveAction;
    private AudioSource engineSource;
    private AudioSource thrusterSource;

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
    }

    void FixedUpdate()
    {
        if (jumpAction != null) isThrusting = jumpAction.IsPressed();
        if (moveAction != null) {
            var v2 = moveAction.ReadValue<Vector2>();
            tiltInput = -v2.x;
        }

        HandleTilt();
        HandleThrust();
        CalculateAirResistance(); 
        ClampVelocity();
    }

    private void CalculateAirResistance()
    {
        Vector2 velocity = rb.linearVelocity;
        float speedSqr = velocity.sqrMagnitude;

        if (speedSqr > 0.01f)
        {
            float dragForceMagnitude = 0.5f * airDensity * speedSqr * dragCoefficient * surfaceArea;
            
            Vector2 dragVector = -velocity.normalized * dragForceMagnitude;
            
            rb.AddForce(dragVector, ForceMode2D.Force);
        }
    }

    private void HandleTilt()
    {
        float currentAngle = rb.rotation;
        currentAngle = Mathf.DeltaAngle(0, currentAngle);

        if (Mathf.Abs(tiltInput) > 0.1f)
        {
            float targetAngle = tiltInput * maxTiltAngle;
            
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            rb.AddTorque(angleDifference * torqueForce * Time.fixedDeltaTime);
        }
        else if (autoLevelForce > 0)
        {
            float levelMultiplier = isThrusting ? 0.5f : 1.5f;
            float angleDifference = Mathf.DeltaAngle(currentAngle, 0f); 
            rb.AddTorque(angleDifference * (autoLevelForce * levelMultiplier) * Time.fixedDeltaTime);
        }
    }

    private void HandleThrust()
    {
        bool hasFuel = fuelSystem == null || !fuelSystem.IsOutOfFuel;
        bool shouldEmit = isThrusting && hasFuel;

        if (shouldEmit)
        {
            float angleInRad = Mathf.Abs(rb.rotation) * Mathf.Deg2Rad;
            float tiltCompensation = 1f / Mathf.Max(Mathf.Cos(angleInRad), 0.5f);

            rb.AddForce(transform.up * (thrustForce * tiltCompensation), ForceMode2D.Force);

            if (fuelSystem != null)
                fuelSystem.ConsumeFuel();
        }

        if (exhaustParticles != null)
        {
            var emission = exhaustParticles.emission;
            emission.enabled = shouldEmit;
        }

        if (thrusterSource != null)
        {
            if (shouldEmit && !thrusterSource.isPlaying) thrusterSource.Play();
            else if (!shouldEmit && thrusterSource.isPlaying) thrusterSource.Stop();
        }
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