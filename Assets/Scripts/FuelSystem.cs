using UnityEngine;
using System.Collections;

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
    [Tooltip("Seconds for the warning sound to fade out after refuelling above threshold")]
    public float warningFadeOutTime = 0.5f;

    public bool IsOutOfFuel => currentFuel <= 0f;

    private float emptyFuelTimer = 0f;
    private bool isGameOverTriggered = false;

    private AudioSource warningSource;
    private bool isWarningActive = false;
    private Coroutine warningFadeCoroutine;

    void Start()
    {
        currentFuel = maxFuel;

        if (lowFuelWarningClip != null)
        {
            warningSource = gameObject.AddComponent<AudioSource>();
            warningSource.clip = lowFuelWarningClip;
            warningSource.loop = true;
            warningSource.volume = warningVolume;
            warningSource.spatialBlend = 0f;
            warningSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (IsOutOfFuel && !isGameOverTriggered)
        {
            emptyFuelTimer += Time.deltaTime;
            if (emptyFuelTimer >= 2f)
            {
                isGameOverTriggered = true;
                StopWarning();

                if (GameUIManager.instance != null)
                    GameUIManager.instance.ShowGameOver();
            }
        }
        else if (!IsOutOfFuel)
        {
            emptyFuelTimer = 0f;
        }

        float fuelPercent = currentFuel / maxFuel;

        if (!isWarningActive && fuelPercent <= lowFuelThreshold && fuelPercent > 0f)
        {
            StartWarning();
        }
        else if (isWarningActive && fuelPercent > lowFuelThreshold)
        {
            StopWarning();
        }
    }

    private void StartWarning()
    {
        if (warningSource == null) return;

        isWarningActive = true;

        // Cancel any in-progress fade and restore volume
        if (warningFadeCoroutine != null)
        {
            StopCoroutine(warningFadeCoroutine);
            warningFadeCoroutine = null;
        }

        warningSource.volume = warningVolume;
        if (!warningSource.isPlaying) warningSource.Play();
    }

    private void StopWarning()
    {
        if (warningSource == null || !warningSource.isPlaying) return;

        isWarningActive = false;

        if (warningFadeCoroutine == null)
            warningFadeCoroutine = StartCoroutine(FadeWarningOut());
    }

    private IEnumerator FadeWarningOut()
    {
        float startVolume = warningSource.volume;
        float elapsed = 0f;

        while (elapsed < warningFadeOutTime)
        {
            elapsed += Time.deltaTime;
            warningSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / warningFadeOutTime);
            yield return null;
        }

        warningSource.Stop();
        warningSource.volume = warningVolume; // Restore for next time
        warningFadeCoroutine = null;
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
            currentFuel = maxFuel;

        isGameOverTriggered = false;
        emptyFuelTimer = 0f;

        Debug.Log($"Fuel refilled by {amount}. Current fuel: {currentFuel:F1} / {maxFuel}");
    }
}

