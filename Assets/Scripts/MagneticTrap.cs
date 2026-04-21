using UnityEngine;

public class MagneticTrap : MonoBehaviour
{
    [Header("Magnetic Trap Settings")]
    [Tooltip("ค่าคงที่แรงโน้มถ่วง (G)")]
    public float gravitationalConstant = 10f;
    [Tooltip("มวลของกับดัก (m1)")]
    public float trapMass = 50f;
    [Tooltip("Maximum distance the trap can reach")]
    public float maxInfluenceRadius = 15f;
    [Tooltip("ระยะห่างน้อยที่สุดที่จะนำมาคำนวณ (ป้องกันแรงดูดเป็นอนันต์เวลาเข้าใกล้จุดกึ่งกลาง)")]
    public float minSafeDistance = 2f;
    [Tooltip("แรงดูดสูงสุดที่จะกระทำกับโดรน (ควรน้อยกว่าแรงพ่นเทอร์โบของโดรน เพื่อให้เร่งเครื่องสู้หนีออกมาได้)")]
    public float maxPullForce = 25f;

    private Transform targetDrone;
    private Rigidbody2D droneRb;

    void Start()
    {
        // For foundation purposes, we simply find the DroneController in the scene.
        DroneController drone = FindAnyObjectByType<DroneController>();
        if (drone != null)
        {
            targetDrone = drone.transform;
            droneRb = drone.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("MagneticTrap could not find the DroneController in the scene!");
        }
    }

    void FixedUpdate()
    {
        if (targetDrone == null || droneRb == null) return;

        // Calculate distance between Trap and Drone
        Vector2 forceDirection = (Vector2)transform.position - (Vector2)targetDrone.position;
        float distance = forceDirection.magnitude;

        if (distance > 0f && distance <= maxInfluenceRadius)
        {
            // จำกัดระยะห่างต่ำสุดในการคำนวณ เพื่อป้องกันบั๊กหารด้วยศูนย์ (Division by Zero) ที่ทำให้แรงดูดมหาศาลทะลุจอ
            float mathDistance = Mathf.Max(distance, minSafeDistance);

            // Newton's Law of Universal Gravitation: F = G * (m1*m2)/r^2
            float forceMagnitude = gravitationalConstant * (trapMass * droneRb.mass) / (mathDistance * mathDistance);
            
            // พระเอกของการกู้ชีพ: จำกัดแรงดูดรวมไม่ให้เกินลิมิต โดรนจะได้พอมีลุ้นเร่งเครื่องหนีได้!
            forceMagnitude = Mathf.Clamp(forceMagnitude, 0f, maxPullForce);

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
