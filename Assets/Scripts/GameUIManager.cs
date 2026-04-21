using UnityEngine;
using TMPro; // จำเป็นต้องใช้ TextMeshPro สำหรับ UI ใน Unity ยุคใหม่

public class GameUIManager : MonoBehaviour
{
    // สร้างเป็น Singleton เพื่อให้ Script อื่นๆ เรียกใช้ได้ง่ายๆ
    public static GameUIManager instance;

    [Header("UI Text Elements")]
    public TextMeshProUGUI scoreText; // แสดงจำนวนที่เก็บได้ เช่น Data Drives: 0 / 5
    public TextMeshProUGUI timeText;  // แสดงเวลา เช่น Time: 00:00
    
    [Header("Game State")]
    // ตัวแปรนี้จะถูกนับอัตโนมัติตอนเริ่มเกมส์ ไม่ต้องตั้งค่าเอง
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
        // ไฮเทคกว่าตอนที่ทำ 3D: เรานับจาก Component "DataDrive" ได้เลย ไม่ต้องเสียเวลาไปสร้าง Tag ใหม่ ป้องกันบั๊กพิมพ์ชื่อพิมพ์ Tag ผิดด้วยครับ!
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
        Debug.Log("🚪 ภารกิจสำเร็จ! เก็บข้อมูลครบแล้ว ประตูทางออกเปิดออก!");
        // (เดี๋ยวเราค่อยมาโค้ดเชื่อมกับประตูที่นี่หลังจาก mechanic หลักเสร็จ)
    }
}
