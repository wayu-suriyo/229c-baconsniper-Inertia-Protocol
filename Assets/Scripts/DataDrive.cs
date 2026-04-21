using UnityEngine;

public class DataDrive : MonoBehaviour
{
    [Header("Effects")]
    public AudioClip collectSound;
    [Range(0f, 1f)]
    public float volume = 0.8f;

    void OnTriggerEnter2D(Collider2D other)
    {
        // เช็คว่าคนที่มาชนคือผู้เล่น (โดรน)
        if (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null)
        {
            // เล่นเสียงตอนเก็บ
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, volume);
            }

            // แจ้ง UI Manager ว่าเก็บของได้แล้ว 1 ชิ้น
            if (GameUIManager.instance != null)
            {
                GameUIManager.instance.AddDataDrive();
            }
            
            // ลบตัวเองทิ้ง (ทำเป็นว่าถูกเก็บไปแล้ว)
            Destroy(gameObject);
        }
    }
}
