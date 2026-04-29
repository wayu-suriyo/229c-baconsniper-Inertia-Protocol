using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float currentFuel;
    public float fuelDrainRate = 15f;

    [Header("Low Fuel Warning")]
    public AudioClip lowFuelWarningClip;
    [Range(0f, 1f)] public float warningVolume = 0.9f;
    [Range(0f, 1f)] public float lowFuelThreshold = 0.25f;

    public bool IsOutOfFuel => currentFuel <= 0f;

    private float emptyFuelTimer = 0f;
    private bool isGameOverTriggered = false;
    private bool lowFuelWarningPlayed = false;

    void Start()
    {
        currentFuel = maxFuel;
    }

    void Update()
    {
        if (IsOutOfFuel && !isGameOverTriggered)
        {
            emptyFuelTimer += Time.deltaTime;
            if (emptyFuelTimer >= 2f)
            {
                isGameOverTriggered = true;
                Debug.Log("Fuel depleted for 2 seconds. Game Over.");
                
                if (GameUIManager.instance != null)
                {
                    GameUIManager.instance.ShowGameOver();
                }
            }
        }
        else if (!IsOutOfFuel)
        {
            emptyFuelTimer = 0f;
        }

        float fuelPercent = currentFuel / maxFuel;
        if (!lowFuelWarningPlayed && fuelPercent <= lowFuelThreshold && fuelPercent > 0f)
        {
            lowFuelWarningPlayed = true;
            AudioManager.PlaySFX(lowFuelWarningClip, warningVolume);
        }
        else if (fuelPercent > lowFuelThreshold)
        {
            lowFuelWarningPlayed = false;
        }
    }

    public void ConsumeFuel(float multiplier = 1f)
    {
        if (currentFuel > 0f)
        {
            currentFuel -= fuelDrainRate * multiplier * Time.fixedDeltaTime;
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
        
        isGameOverTriggered = false;
        emptyFuelTimer = 0f;

        Debug.Log($"Fuel refilled by {amount}. Current fuel: {currentFuel:F1} / {maxFuel}");
    }
}
