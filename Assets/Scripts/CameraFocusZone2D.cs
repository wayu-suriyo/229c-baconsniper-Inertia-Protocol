using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraFocusZone2D : MonoBehaviour
{
    private BoxCollider2D zoneCollider;

    void Start()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (DynamicCamera2D.instance != null)
            {
                DynamicCamera2D.instance.currentFocusZone = zoneCollider;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (DynamicCamera2D.instance != null && DynamicCamera2D.instance.currentFocusZone == zoneCollider)
            {
                DynamicCamera2D.instance.currentFocusZone = null;
            }
        }
    }
}
