using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PhysicsDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public Vector2 openOffset = new Vector2(0f, 3f);
    public float moveSpeed = 5f;
    public bool startOpen = false;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float volume = 0.6f;

    private Rigidbody2D rb;
    private Vector2 closedPosition;
    private Vector2 openPosition;
    private bool isOpen;
    private bool isMoving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        closedPosition = rb.position;
        openPosition = closedPosition + openOffset;
        isOpen = startOpen;
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        Vector2 target = isOpen ? openPosition : closedPosition;
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (Vector2.Distance(rb.position, target) < 0.02f)
        {
            rb.MovePosition(target);
            isMoving = false;
        }
    }

    public void Activate()
    {
        if (isOpen) return;
        isOpen = true;
        isMoving = true;
        AudioManager.PlaySFX(openSound, volume);
    }

    public void Deactivate()
    {
        if (!isOpen) return;
        isOpen = false;
        isMoving = true;
        AudioManager.PlaySFX(closeSound, volume);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying
            ? (Vector2)transform.position
            : (Vector2)transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + openOffset);
        Gizmos.DrawWireCube(origin + openOffset, Vector3.one * 0.4f);
    }
}
