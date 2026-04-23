using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    [Tooltip("ปริมาณน้ำมันสูงสุด")]
    public float maxFuel = 100f;
    [Tooltip("ปริมาณน้ำมันปัจจุบัน")]
    public float currentFuel;
    [Tooltip("อัตราการใช้เชื้อเพลิงต่อวินาที")]
    public float fuelDrainRate = 15f; 

    public bool IsOutOfFuel => currentFuel <= 0f;

    void Start()
    {
        currentFuel = maxFuel;
    }

    public void ConsumeFuel()
    {
        if (currentFuel > 0f)
        {
            currentFuel -= fuelDrainRate * Time.fixedDeltaTime;
            if (currentFuel <= 0f)
            {
                currentFuel = 0f;
                Debug.LogWarning("Fuel Depleted! Thrust disabled.");
            }
        }
    }

    public void AddFuel(float amount)
    {
        currentFuel += amount;
        if (currentFuel > maxFuel)
        {
            currentFuel = maxFuel;
        }
        Debug.Log($"⛽ เติมน้ำมันเพิ่ม {amount} หน่วย! น้ำมันตอนนี้: {currentFuel:F1} / {maxFuel}");
    }
}
