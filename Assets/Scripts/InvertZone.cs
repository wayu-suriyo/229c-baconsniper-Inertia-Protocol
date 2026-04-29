using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InvertZone : MonoBehaviour
{
    [Header("Physics")]
    [Tooltip("Multiplier for the drone's auto-thrust while inside the zone (e.g. 0.5 = half speed)")]
    public float thrustMultiplier = 0.5f;
    [Tooltip("Multiplier for fuel consumption while inside the zone (e.g. 0 = no fuel cost)")]
    public float fuelMultiplier = 0f;

    [Header("Visual")]
    public Color zoneColor = new Color(1f, 0f, 0f, 0.25f);
    public Color activeColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    [Header("Audio")]
    public AudioClip enterSound;
    public AudioClip exitSound;
    [Range(0f, 1f)] public float volume = 0.7f;

    private SpriteRenderer sr;
    private DroneController droneInZone;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        SetVisual(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        DroneController dc = other.GetComponent<DroneController>();
        if (dc == null) return;

        droneInZone = dc;
        dc.invertControls = true;
        dc.invertThrustMultiplier = thrustMultiplier;
        dc.invertFuelMultiplier = fuelMultiplier;
        SetVisual(true);

        if (enterSound != null)
        {
            AudioManager.PlaySFX(enterSound, volume);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<DroneController>() == null) return;
        
        if (droneInZone != null)
        {
            droneInZone.invertControls = false;
            droneInZone.invertThrustMultiplier = 1f;
            droneInZone.invertFuelMultiplier = 1f;
            droneInZone = null;
        }
        
        SetVisual(false);

        if (exitSound != null)
        {
            AudioManager.PlaySFX(exitSound, volume);
        }
    }

    private void SetVisual(bool active)
    {
        if (sr != null)
            sr.color = active ? activeColor : zoneColor;
    }

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(
            transform.TransformPoint(col.offset),
            transform.rotation,
            transform.lossyScale
        );
        Gizmos.DrawCube(Vector3.zero, col.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
