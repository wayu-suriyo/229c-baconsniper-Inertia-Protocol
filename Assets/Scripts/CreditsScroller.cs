using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class CreditsScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("The RectTransform containing all the credits text (parent panel that moves)")]
    public RectTransform creditsContent;
    [Tooltip("Scroll speed in pixels per second")]
    public float scrollSpeed = 40f;
    [Tooltip("How much faster scroll goes when player holds Space or clicks")]
    public float fastScrollMultiplier = 3f;

    [Header("Timing")]
    [Tooltip("Seconds to wait before scrolling begins")]
    public float startDelay = 1.5f;
    [Tooltip("Seconds to wait after scroll finishes before auto-returning to menu")]
    public float endDelay = 3f;

    [Header("Navigation")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    [Tooltip("Background music for the credits scene")]
    public AudioClip creditsMusic;
    [Range(0f, 1f)] public float creditsMusicVolume = 0.5f;

    [Header("Fade Out")]
    [Tooltip("Optional CanvasGroup on the credits content for fade-out at the end")]
    public CanvasGroup contentCanvasGroup;
    public float fadeOutDuration = 2f;

    private float startY;
    private float endY;
    private bool scrollStarted = false;
    private bool scrollFinished = false;
    private float delayTimer;
    private float endTimer;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (creditsMusic != null && AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicVolume(creditsMusicVolume);
            AudioManager.instance.PlayMusic(creditsMusic);
        }

        if (creditsContent != null)
        {
            // Starting position: credits text sits just below the visible screen
            startY = creditsContent.anchoredPosition.y;

            // Calculate how far we need to scroll:
            // The total height of the credits text content
            float contentHeight = creditsContent.rect.height;

            // The visible viewport height (the parent's rect, i.e. the screen)
            RectTransform viewport = creditsContent.parent as RectTransform;
            float viewportHeight = viewport != null ? viewport.rect.height : Screen.height;

            // We need to scroll the entire content height + one viewport height
            // so text starts from below screen and ends above screen
            endY = startY + contentHeight + viewportHeight;
        }

        delayTimer = startDelay;
        if (contentCanvasGroup != null)
            contentCanvasGroup.alpha = 1f;
    }

    void Update()
    {
        // Allow skipping back to menu at any time with Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMenu();
            return;
        }

        if (scrollFinished)
        {
            // Fade out
            if (contentCanvasGroup != null && contentCanvasGroup.alpha > 0f)
            {
                contentCanvasGroup.alpha -= Time.deltaTime / fadeOutDuration;
            }

            endTimer -= Time.deltaTime;
            if (endTimer <= 0f)
            {
                ReturnToMenu();
            }
            return;
        }

        // Start delay
        if (!scrollStarted)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer > 0f) return;
            scrollStarted = true;
        }

        // Scroll the content upward
        if (creditsContent != null)
        {
            float speed = scrollSpeed;

            // Fast-forward when holding Space or left mouse button
            bool holdingFast = (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                            || (Mouse.current != null && Mouse.current.leftButton.isPressed);
            if (holdingFast)
                speed *= fastScrollMultiplier;

            Vector2 pos = creditsContent.anchoredPosition;
            pos.y += speed * Time.deltaTime;
            creditsContent.anchoredPosition = pos;

            // Check if we've scrolled past the end
            if (pos.y >= endY)
            {
                scrollFinished = true;
                endTimer = endDelay;
            }
        }
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
