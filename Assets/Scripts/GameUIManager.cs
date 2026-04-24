using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using TMPro;

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

    private FuelSystem playerFuel;
    private DroneHealth playerHealth;

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

        UpdateScoreUI();
    }

    void Update()
    {
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
            ShowWinScreen(); // Fallback if no portal exists
        }
    }

    public void ShowGameOver()
    {
        if (isGameOver || isLevelCompleted) return;
        
        isGameOver = true;
        
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
}
