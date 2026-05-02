using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Name of the first gameplay scene to load when Play is pressed")]
    public string firstLevelSceneName = "Level1";
    [Tooltip("Name of the credits scene")]
    public string creditsSceneName = "Credits";

    [Header("Audio")]
    [Tooltip("Optional click sound for buttons")]
    public AudioClip buttonClickSound;
    [Range(0f, 1f)] public float clickVolume = 0.5f;

    [Header("Music")]
    [Tooltip("Background music for the main menu (leave empty to keep AudioManager's default)")]
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float menuMusicVolume = 0.5f;

    void Start()
    {
        // Ensure clean state coming from gameplay
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Play menu music if assigned
        if (menuMusic != null && AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicVolume(menuMusicVolume);
            AudioManager.instance.PlayMusic(menuMusic);
        }
    }

    public void PlayGame()
    {
        PlayClickSound();
        SceneManager.LoadScene(firstLevelSceneName);
    }

    public void OpenCredits()
    {
        PlayClickSound();
        SceneManager.LoadScene(creditsSceneName);
    }

    public void QuitGame()
    {
        PlayClickSound();
        Debug.Log("Quit Game requested.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayClickSound()
    {
        if (buttonClickSound != null)
            AudioManager.PlaySFX(buttonClickSound, clickVolume);
    }
}
