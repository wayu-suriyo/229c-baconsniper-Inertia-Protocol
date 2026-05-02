using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ExitPortal : MonoBehaviour
{
    [Header("Portal Sprites")]
    [Tooltip("Sprite shown when the portal is closed (waiting for all data drives).")]
    public Sprite closedSprite;
    [Tooltip("Sprite shown when the portal is open and ready to enter.")]
    public Sprite openSprite;

    [Header("VFX")]
    [Tooltip("Particle system to play when the portal opens. Assign a child GameObject with a ParticleSystem.")]
    public ParticleSystem openParticles;

    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip enterSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Entry Cinematic")]
    [Tooltip("Time scale multiplier during the slow-motion effect (e.g. 0.15 = very slow)")]
    public float slowMoTimeScale = 0.15f;
    [Tooltip("How long (real seconds) to stay in slow-motion before showing the win panel")]
    public float slowMoDuration = 0.4f;

    private bool isOpen = false;
    private bool isEntering = false;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        if (GameUIManager.instance != null)
            GameUIManager.instance.RegisterExitPortal(this);

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && closedSprite != null)
            spriteRenderer.sprite = closedSprite;

        if (openParticles != null)
            openParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void OpenPortal()
    {
        if (isOpen) return;
        isOpen = true;

        if (spriteRenderer != null && openSprite != null)
            spriteRenderer.sprite = openSprite;

        if (openParticles != null)
            openParticles.Play();

        if (openSound != null)
            AudioManager.PlaySFX(openSound, volume);

        Debug.Log("Exit Portal Opened!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen || isEntering) return;
        if (!other.CompareTag("Player") && other.GetComponent<DroneHealth>() == null) return;

        isEntering = true;
        StartCoroutine(EntryCinematic(other.gameObject));
    }

    private IEnumerator EntryCinematic(GameObject drone)
    {
        // --- 1. Slow time ---
        Time.timeScale = slowMoTimeScale;

        // --- 2. Hide drone ---
        SpriteRenderer droneSr = drone.GetComponent<SpriteRenderer>();
        DroneController droneCtrl = drone.GetComponent<DroneController>();

        if (droneSr != null) droneSr.enabled = false;
        if (droneCtrl != null) droneCtrl.enabled = false;

        // Freeze drone physics so it doesn't drift while hidden
        Rigidbody2D droneRb = drone.GetComponent<Rigidbody2D>();
        if (droneRb != null) droneRb.linearVelocity = Vector2.zero;

        // --- 3. Play enter sound (unscaled so it isn't affected by slow-mo) ---
        float soundLength = 0f;
        if (enterSound != null)
        {
            // Create a dedicated source with ignoreListenerPause so it survives
            // even if AudioListener is paused by ShowWinScreen later
            GameObject sfxGo = new GameObject("SFX_PortalEnter");
            sfxGo.transform.SetParent(AudioManager.instance != null ? AudioManager.instance.transform : transform);
            AudioSource src = sfxGo.AddComponent<AudioSource>();
            src.clip = enterSound;
            src.volume = volume;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = true;
            src.pitch = 1f;
            src.Play();
            soundLength = enterSound.length;
            Destroy(sfxGo, soundLength + 0.2f);
        }

        // Wait the slow-mo beat using unscaled time
        float elapsed = 0f;
        while (elapsed < slowMoDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Restore time before handing off to ShowWinScreen (which will set timeScale 0)
        Time.timeScale = 1f;

        // --- 4. Wait for the remaining sound to finish ---
        float remainingSound = soundLength - slowMoDuration;
        if (remainingSound > 0f)
        {
            elapsed = 0f;
            while (elapsed < remainingSound)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // --- 5. Show Stage Clear panel ---
        if (GameUIManager.instance != null)
            GameUIManager.instance.ShowWinScreen();
    }
}

