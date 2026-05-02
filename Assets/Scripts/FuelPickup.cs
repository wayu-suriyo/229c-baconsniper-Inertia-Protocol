using UnityEngine;

public class FuelPickup : PickupBase
{
    [Header("Fuel Settings")]
    public float refuelAmount = 50f;

    protected override void ApplyEffect(Collider2D player)
    {
        FuelSystem fuelSys = player.GetComponent<FuelSystem>();

        if (fuelSys != null)
        {
            fuelSys.AddFuel(refuelAmount);
        }
        else
        {
            Debug.LogWarning("Player is missing FuelSystem component.");
        }
    }
}
