using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("UI Text Elements")]
    public TextMeshProUGUI scoreText; 
    public TextMeshProUGUI timeText; 
    
    [Header("Game State")]
    [HideInInspector] public int requiredDrives = 0; 
    private int currentDrives = 0;
    private float elapsedTime = 0f;
    private bool isLevelCompleted = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        DataDrive[] allDrivesInScene = Object.FindObjectsByType<DataDrive>(FindObjectsSortMode.None);
        requiredDrives = allDrivesInScene.Length;

        UpdateScoreUI();
    }

    void Update()
    {
        if (!isLevelCompleted)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimeUI();
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

    private void LevelComplete()
    {
        isLevelCompleted = true;
        Debug.Log(" Mission Complete! ");
    }
}
