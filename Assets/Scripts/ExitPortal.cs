using UnityEngine;

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

    private bool isOpen = false;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.RegisterExitPortal(this);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Show the closed sprite at start, preserve natural sprite color
        if (spriteRenderer != null && closedSprite != null)
        {
            spriteRenderer.sprite = closedSprite;
        }

        // Particles should be stopped at start
        if (openParticles != null)
        {
            openParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void OpenPortal()
    {
        if (isOpen) return;

        isOpen = true;

        // Swap to open sprite
        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        // Play particle effect
        if (openParticles != null)
        {
            openParticles.Play();
        }

        if (openSound != null)
        {
            AudioManager.PlaySFX(openSound, volume);
        }

        Debug.Log("Exit Portal Opened!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen && (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null))
        {
            AudioManager.PlaySFXAt(enterSound, transform.position, volume);

            if (GameUIManager.instance != null)
            {
                GameUIManager.instance.ShowWinScreen();
            }
        }
    }
}
