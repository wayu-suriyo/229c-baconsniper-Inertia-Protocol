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
    public TextMeshProUGUI deathCountHUDText;
    
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
    private bool isGoalOpen = false;
    private bool isLevelCompleted = false;
    private bool isGameOver = false;
    private int lastDisplayedSeconds = -1;
    private int lastDisplayedDeaths = -1;

    [Header("Game Over Settings")]
    public GameObject gameOverPanel;
    public string mainMenuSceneName = "MainMenu";

    [Header("Win Settings")]
    public GameObject winPanel;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI deathCountText;
    public string nextLevelSceneName = "Level2";
    public AudioClip winSound;
    [Range(0f, 1f)] public float winSoundVolume = 1f;

    [Header("Pause Settings")]
    public GameObject pausePanel;
    private bool isPaused = false;

    private FuelSystem playerFuel;
    private DroneHealth playerHealth;
    private DroneController playerController;
    private ExitPortal currentExitPortal;

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
        bool isAnyEndPanelOpen = (gameOverPanel != null && gameOverPanel.activeSelf) || 
                                 (winPanel != null && winPanel.activeSelf);

        if (!isAnyEndPanelOpen)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        if (!isLevelCompleted && !isGameOver)
        {
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
        if (deathCountHUDText != null)
        {
            UpdateDeathCountUI();
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
        if (isGoalOpen || isLevelCompleted) return;

        currentDrives++;
        UpdateScoreUI();

        if (currentDrives >= requiredDrives)
        {
            isGoalOpen = true;
            if (currentExitPortal != null)
            {
                currentExitPortal.OpenPortal();
            }
            else
            {
                Debug.LogWarning("Goal opened, but no ExitPortal registered!");
                ShowWinScreen();
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Data : {currentDrives} / {requiredDrives}";
        }
    }

    private void UpdateTimeUI()
    {
        if (timeText == null) return;

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        if (totalSeconds == lastDisplayedSeconds) return;
        lastDisplayedSeconds = totalSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

    private void UpdateDeathCountUI()
    {
        if (deathCountHUDText == null) return;

        int deaths = (CheckpointManager.instance != null) ? CheckpointManager.instance.DeathCount : 0;
        if (deaths == lastDisplayedDeaths) return;
        lastDisplayedDeaths = deaths;

        deathCountHUDText.text = $"Deaths: {deaths}";
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

        // Skip if fill is already at target (within tolerance)
        if (Mathf.Abs(healthBarFill.fillAmount - healthPercent) < 0.001f) return;
        
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

        // If a checkpoint exists, reload scene but respawn at checkpoint
        if (CheckpointManager.instance != null &&
            CheckpointManager.instance.HasCheckpointForCurrentScene())
        {
            pendingRespawn = true;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;

        // Clear checkpoint when returning to menu
        if (CheckpointManager.instance != null)
            CheckpointManager.instance.ClearCheckpoint();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // --- Checkpoint Respawn System ---
    private static bool pendingRespawn = false;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!pendingRespawn) return;
        pendingRespawn = false;

        if (CheckpointManager.instance == null ||
            !CheckpointManager.instance.HasCheckpointForCurrentScene()) return;

        // Find the drone fresh after scene reload
        DroneController drone = FindAnyObjectByType<DroneController>();
        if (drone == null) return;

        // Reposition drone at checkpoint
        Vector3 respawnPos = CheckpointManager.instance.RespawnPosition;
        Rigidbody2D droneRb = drone.GetComponent<Rigidbody2D>();
        if (droneRb != null)
        {
            droneRb.linearVelocity = Vector2.zero;
            droneRb.angularVelocity = 0f;
            droneRb.rotation = 0f;
        }
        drone.transform.position = respawnPos;

        Debug.Log($"Respawned at checkpoint: {respawnPos}");
    }

    public void ShowWinScreen()
    {
        if (isGameOver || isLevelCompleted) return;
        isLevelCompleted = true;

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

        if (deathCountText != null)
        {
            int deaths = (CheckpointManager.instance != null) ? CheckpointManager.instance.DeathCount : 0;
            deathCountText.text = deaths == 0 ? "Deaths: 0" : $"Deaths: {deaths}";
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
        bool isAnyEndPanelOpen = (gameOverPanel != null && gameOverPanel.activeSelf) || 
                                 (winPanel != null && winPanel.activeSelf);
                                 
        if (isAnyEndPanelOpen) return;

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

    public void RegisterExitPortal(ExitPortal portal)
    {
        currentExitPortal = portal;
    }
}
