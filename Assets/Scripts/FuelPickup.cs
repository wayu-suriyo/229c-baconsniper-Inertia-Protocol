using UnityEngine;

public class FuelPickup : MonoBehaviour
{
    [Header("Fuel Settings")]
    [Tooltip("ปริมาณน้ำมันที่จะเติมให้ยานเมื่อเก็บไอเทมนี้")]
    public float refuelAmount = 50f;

    [Header("Audio Settings")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    void OnTriggerEnter2D(Collider2D other)
    {
        // เช็คว่าคนที่มาเก็บคือโดรนหรือไม่
        if (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null)
        {
            // ดึงสคริปต์ FuelSystem ออกมาจากโดรน
            FuelSystem fuelSys = other.GetComponent<FuelSystem>();
            
            if (fuelSys != null)
            {
                // เติมน้ำมันให้โดรน
                fuelSys.AddFuel(refuelAmount);

                // เล่นเสียงเอฟเฟกต์ (ใช้ PlayClipAtPoint เพื่อให้เสียงยังดังอยู่แม้ Object จะถูกทำลายไปแล้วทันที)
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, volume);
                }

                // ทำลายถังน้ำมันทิ้ง
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("โดรนลำนี้เก็บน้ำมันได้ แต่ดันไม่มีสคริปต์ FuelSystem ติดอยู่!");
            }
        }
    }
}
