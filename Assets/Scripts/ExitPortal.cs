using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public Color closedColor = Color.gray;
    public Color openColor = Color.cyan;
    
    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip enterSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private bool isOpen = false;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = closedColor;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
    }

    public void OpenPortal()
    {
        if (isOpen) return;
        
        isOpen = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = openColor;
        }

        if (openSound != null)
        {
            audioSource.PlayOneShot(openSound, volume);
        }
        
        Debug.Log("🚪 Exit Portal Opened!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen && (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null))
        {
            // ผู้เล่นเข้าประตูที่เปิดแล้ว
            if (enterSound != null)
            {
                AudioSource.PlayClipAtPoint(enterSound, transform.position, volume);
            }

            if (GameUIManager.instance != null)
            {
                GameUIManager.instance.ShowWinScreen();
            }
        }
    }
}
