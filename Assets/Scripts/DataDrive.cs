using UnityEngine;

public class DataDrive : MonoBehaviour
{
    [Header("Effects")]
    public AudioClip collectSound;
    [Range(0f, 1f)]
    public float volume = 0.8f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<DroneHealth>() != null)
        {
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, volume);
            }

            if (GameUIManager.instance != null)
            {
                GameUIManager.instance.AddDataDrive();
            }
            
            Destroy(gameObject);
        }
    }
}
