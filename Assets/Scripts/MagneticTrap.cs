using UnityEngine;

public class MagneticTrap : MonoBehaviour
{
    [Header("Magnetic Trap Settings")]
    [Tooltip("ค่าคงที่แรงโน้มถ่วง (G)")]
    public float gravitationalConstant = 10f;
    [Tooltip("มวลของกับดัก (m1)")]
    public float trapMass = 50f;
    [Tooltip("รัศมีอิทธิพลสูงสุด")]
    public float maxInfluenceRadius = 15f;

    private Transform targetDrone;
    private Rigidbody2D droneRb;

    void Start()
    {
        DroneController drone = FindAnyObjectByType<DroneController>();
        if (drone != null)
        {
            targetDrone = drone.transform;
            droneRb = drone.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("MagneticTrap could not find the DroneController");
        }
    }

    void FixedUpdate()
    {
        if (targetDrone == null || droneRb == null) return;

        Vector2 forceDirection = (Vector2)transform.position - (Vector2)targetDrone.position;
        float distance = forceDirection.magnitude;

        if (distance > 0f && distance <= maxInfluenceRadius)
        {
            float forceMagnitude = gravitationalConstant * (trapMass * droneRb.mass) / (distance * distance);
            Vector2 appliedForce = forceDirection.normalized * forceMagnitude;

            droneRb.AddForce(appliedForce, ForceMode2D.Force);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxInfluenceRadius);
    }
}
