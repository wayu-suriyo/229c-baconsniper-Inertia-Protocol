using UnityEngine;

public class DataDrive : PickupBase
{
    protected override void ApplyEffect(Collider2D player)
    {
        if (GameUIManager.instance != null)
            GameUIManager.instance.AddDataDrive();
    }
}
