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
}
