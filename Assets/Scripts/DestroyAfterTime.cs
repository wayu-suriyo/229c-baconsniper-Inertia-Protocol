using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [Tooltip("How long in seconds before this object is automatically destroyed")]
    public float lifetime = 2f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
