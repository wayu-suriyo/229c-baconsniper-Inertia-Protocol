using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("UI Text Elements")]
    public TextMeshProUGUI scoreText; 
    public TextMeshProUGUI timeText; 
    
    [Header("Fuel UI Elements")]
    public Image fuelImage;
    public Sprite[] fuelSprites;
    
    [Header("Health UI Elements")]
    public Image healthBarFill;
    public Color healthFullColor = Color.green;
    public Color healthLowColor = Color.red;

    [Header("Damage Flash")]
    [Tooltip("Fullscreen Image set to red with low alpha — fades out on hit")]
    public Image damageFlashImage;
    public float flashDuration = 0.3f;
    
    [Header("Game State")]
    [HideInInspector] public int requiredDrives = 0; 
    private int currentDrives = 0;
    private float elapsedTime = 0f;
    private bool isLevelCompleted = false;
    private bool isGameOver = false;

    [Header("Game Over Settings")]
    public GameObject gameOverPanel;
    public string mainMenuSceneName = "MainMenu";

    [Header("Win Settings")]
    public GameObject winPanel;
    public TextMeshProUGUI finalTimeText;
    public string nextLevelSceneName = "Level2";
    public AudioClip winSound;
    [Range(0f, 1f)] public float winSoundVolume = 1f;

    [Header("Pause Settings")]
    public GameObject pausePanel;
    private bool isPaused = false;

    private FuelSystem playerFuel;
    private DroneHealth playerHealth;
    private DroneController playerController;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        DataDrive[] allDrivesInScene = Object.FindObjectsByType<DataDrive>(FindObjectsSortMode.None);
        requiredDrives = allDrivesInScene.Length;

        playerFuel = Object.FindAnyObjectByType<FuelSystem>();
        playerHealth = Object.FindAnyObjectByType<DroneHealth>();
        playerController = Object.FindAnyObjectByType<DroneController>();

        UpdateScoreUI();
    }

    void Update()
    {
        if (!isLevelCompleted && !isGameOver)
        {
            // Check for pause input
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }

            elapsedTime += Time.deltaTime;
            UpdateTimeUI();
        }

        if (playerFuel != null && fuelImage != null)
        {
            UpdateFuelUI();
        }
        if (playerHealth != null && healthBarFill != null)
        {
            UpdateHealthUI();
        }
    }

    public void TriggerDamageFlash()
    {
        if (damageFlashImage != null)
            StartCoroutine(FlashDamage());
    }

    private IEnumerator FlashDamage()
    {
        damageFlashImage.color = new Color(1f, 0f, 0f, 0.35f);
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.35f, 0f, elapsed / flashDuration);
            damageFlashImage.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }
        damageFlashImage.color = Color.clear;
    }

    public void AddDataDrive()
    {
        if (isLevelCompleted) return;

        currentDrives++;
        UpdateScoreUI();

        if (currentDrives >= requiredDrives)
        {
            LevelComplete();
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Data Drives: {currentDrives} / {requiredDrives}";
        }
    }

    private void UpdateTimeUI()
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60F);
            int seconds = Mathf.FloorToInt(elapsedTime - minutes * 60);
            timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }
    }

    private void UpdateFuelUI()
    {
        float fuelPercent = playerFuel.currentFuel / playerFuel.maxFuel;
        
        if (fuelSprites != null && fuelSprites.Length >= 5)
        {
            int spriteIndex = 0;
            
            if (fuelPercent >= 0.75f) spriteIndex = 4;      
            else if (fuelPercent >= 0.50f) spriteIndex = 3; 
            else if (fuelPercent >= 0.25f) spriteIndex = 2; 
            else if (fuelPercent > 0f) spriteIndex = 1;     
            else spriteIndex = 0;                           
            
            fuelImage.sprite = fuelSprites[spriteIndex];
            fuelImage.color = Color.white; 
        }
    }

    private void UpdateHealthUI()
    {
        float healthPercent = playerHealth.currentHealth / playerHealth.maxHealth;
        
        healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, healthPercent, Time.deltaTime * 5f);
        healthBarFill.color = Color.Lerp(healthLowColor, healthFullColor, healthPercent);
    }

    public void ForceHealthToZero()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 0f;
            healthBarFill.color = healthLowColor;
        }
    }

    private void LevelComplete()
    {
        isLevelCompleted = true;
        
        ExitPortal portal = Object.FindAnyObjectByType<ExitPortal>();
        if (portal != null)
        {
            portal.OpenPortal();
        }
        else
        {
            Debug.LogWarning("Level Complete, but no ExitPortal found in scene!");
            ShowWinScreen(); 
        }
    }

    public void ShowGameOver()
    {
        if (isGameOver || isLevelCompleted) return;
        
        isGameOver = true;
        DisableDroneController();
        DynamicCamera2D.StopShake();
        ForceHealthToZero();
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        Time.timeScale = 0f;
        Debug.Log("Game Over Triggered!");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowWinScreen()
    {
        if (isGameOver) return;

        DisableDroneController();
        DynamicCamera2D.StopShake();
        AudioManager.PlaySFX(winSound, winSoundVolume);

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (finalTimeText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60F);
            int seconds = Mathf.FloorToInt(elapsedTime - minutes * 60);
            finalTimeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }

        Time.timeScale = 0f;
        Debug.Log("Stage Cleared! Win Screen Triggered!");
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelSceneName);
    }

    public void TogglePause()
    {
        if (isGameOver || isLevelCompleted) return;

        isPaused = !isPaused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }

    private void DisableDroneController()
    {
        if (playerController != null) playerController.enabled = false;
    }
}
