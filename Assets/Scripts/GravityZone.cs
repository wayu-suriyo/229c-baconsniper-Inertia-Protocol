using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GravityZone : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Gravity scale applied to the drone while inside. Use -1 to fully invert.")]
    public float invertedGravityScale = -1f;
    public float transitionSpeed = 5f;

    [Header("Visual")]
    public Color zoneColor = new Color(0.4f, 0f, 1f, 0.25f);
    public Color activeColor = new Color(1f, 0.2f, 0.8f, 0.4f);

    private Rigidbody2D droneRb;
    private float originalGravityScale = 1f;
    private bool droneInside = false;
    private bool isZoneActive = true;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        SetVisual(isZoneActive);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        DroneController dc = other.GetComponent<DroneController>();
        if (rb == null || dc == null) return;

        droneRb = rb;
        originalGravityScale = rb.gravityScale;
        droneInside = true;

        if (isZoneActive)
            ApplyInvertedGravity();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<DroneController>() == null) return;
        RestoreGravity();
        droneInside = false;
        droneRb = null;
    }

    private void ApplyInvertedGravity()
    {
        if (droneRb != null)
            droneRb.gravityScale = invertedGravityScale;
    }

    private void RestoreGravity()
    {
        if (droneRb != null)
            droneRb.gravityScale = originalGravityScale;
    }

    public void Activate()
    {
        isZoneActive = true;
        SetVisual(true);
        if (droneInside) ApplyInvertedGravity();
    }

    public void Deactivate()
    {
        isZoneActive = false;
        SetVisual(false);
        if (droneInside) RestoreGravity();
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

        Gizmos.color = new Color(0.6f, 0f, 1f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(
            transform.TransformPoint(col.offset),
            transform.rotation,
            transform.lossyScale
        );
        Gizmos.DrawCube(Vector3.zero, col.size);
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.8f);
        Vector3 center = transform.position;
        Gizmos.DrawLine(center + Vector3.down * 0.5f, center + Vector3.up * 0.5f);
        Gizmos.DrawLine(center + Vector3.up * 0.5f, center + new Vector3(-0.2f, 0.2f, 0));
        Gizmos.DrawLine(center + Vector3.up * 0.5f, center + new Vector3(0.2f, 0.2f, 0));
    }
}
