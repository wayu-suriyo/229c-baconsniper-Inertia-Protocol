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
            DynamicCamera2D camFollow = Camera.main.GetComponent<DynamicCamera2D>();
            if (camFollow != null)
            {
                camFollow.currentFocusZone = zoneCollider;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DynamicCamera2D camFollow = Camera.main.GetComponent<DynamicCamera2D>();
            if (camFollow != null && camFollow.currentFocusZone == zoneCollider)
            {
                camFollow.currentFocusZone = null;
            }
        }
    }
}
