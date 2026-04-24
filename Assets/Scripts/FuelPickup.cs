using UnityEngine;

public class FuelPickup : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float refuelAmount = 50f;

    [Header("Audio Settings")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null)
        {
            FuelSystem fuelSys = other.GetComponent<FuelSystem>();
            
            if (fuelSys != null)
            {
                fuelSys.AddFuel(refuelAmount);

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, volume);
                }

                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("Player is missing FuelSystem component.");
            }
        }
    }
}
