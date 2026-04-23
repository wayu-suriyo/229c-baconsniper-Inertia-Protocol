using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("UI Text Elements")]
    public TextMeshProUGUI scoreText; 
    public TextMeshProUGUI timeText; 
    
    [Header("Fuel UI Elements")]
    public Image fuelImage;
    [Tooltip("ใส่รูประดับน้ำมัน 5 รูปเรียงจาก: หมด(0), 1 ขีด(1), 2 ขีด(2), 3 ขีด(3), เต็ม(4)")]
    public Sprite[] fuelSprites;
    
    [Header("Health UI Elements")]
    [Tooltip("ลาก UI Image ที่ตั้งค่า Image Type เป็น Filled สำหรับหลอดเลือดมาใส่")]
    public Image healthBarFill;
    public Color healthFullColor = Color.green;
    public Color healthLowColor = Color.red;
    
    [Header("Game State")]
    [HideInInspector] public int requiredDrives = 0; 
    private int currentDrives = 0;
    private float elapsedTime = 0f;
    private bool isLevelCompleted = false;

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
        if (!isLevelCompleted)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimeUI();
        }

        // อัปเดตภาพหลอดน้ำมัน
        if (playerFuel != null && fuelImage != null)
        {
            UpdateFuelUI();
        }

        // อัปเดตหลอดเลือด
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
            
            // แบ่งเกณฑ์การโชว์รูปภาพออกเป็น 5 ระดับ
            if (fuelPercent >= 0.75f) spriteIndex = 4;      // 75% - 100% (รูป 4 ขีดเต็ม)
            else if (fuelPercent >= 0.50f) spriteIndex = 3; // 50% - 74% (รูป 3 ขีด)
            else if (fuelPercent >= 0.25f) spriteIndex = 2; // 25% - 49% (รูป 2 ขีด)
            else if (fuelPercent > 0f) spriteIndex = 1;     // 1% - 24% (รูป 1 ขีด)
            else spriteIndex = 0;                           // 0% (หลอดเปล่า)
            
            fuelImage.sprite = fuelSprites[spriteIndex];
            fuelImage.color = Color.white; // เคลียร์สีเผื่อของเก่าทำไว้
        }
    }

    private void UpdateHealthUI()
    {
        float healthPercent = playerHealth.currentHealth / playerHealth.maxHealth;
        
        // ทีเด็ด: ใช้ Lerp ทำให้หลอดเลือดลดลงแบบสมูทๆ แทนที่จะกระตุกฮวบหายไป
        healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, healthPercent, Time.deltaTime * 5f);
        
        // เปลียนสีหลอดไล่เฉด (ถ้าเต็มจะเขียว ถ้าเจ็บหนักจะค่อยๆ แดง)
        healthBarFill.color = Color.Lerp(healthLowColor, healthFullColor, healthPercent);
    }

    private void LevelComplete()
    {
        isLevelCompleted = true;
        Debug.Log(" Mission Complete! ");
    }
}
