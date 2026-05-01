using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Portal sprite shown before the checkpoint is activated.")]
    public Sprite inactiveSprite;
    [Tooltip("Portal sprite shown after the checkpoint is activated.")]
    public Sprite activeSprite;

    [Header("Transparency")]
    [Tooltip("Opacity of the portal when inactive (0 = invisible, 1 = fully opaque).")]
    [Range(0f, 1f)] public float inactiveAlpha = 0.6f;
    [Tooltip("Opacity of the portal when active.")]
    [Range(0f, 1f)] public float activeAlpha = 1f;

    [Header("Audio")]
    public AudioClip activateSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Respawn Offset")]
    [Tooltip("Offset from this object's position where the drone will spawn.")]
    public Vector2 respawnOffset = Vector2.zero;

    private SpriteRenderer sr;
    private bool isActivated = false;

    /// <summary>Reference to the currently active checkpoint in this scene.</summary>
    private static Checkpoint currentActive;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void Start()
    {
        // If we just reloaded the scene and this checkpoint was the saved one,
        // mark it as active visually
        if (CheckpointManager.instance != null &&
            CheckpointManager.instance.HasCheckpointForCurrentScene())
        {
            Vector3 savedPos = CheckpointManager.instance.RespawnPosition;
            float dist = Vector2.Distance(savedPos, (Vector2)transform.position + respawnOffset);
            if (dist < 0.5f)
            {
                MarkActive();
            }
        }

        if (!isActivated)
        {
            SetVisual(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;
        if (!other.CompareTag("Player")) return;

        // Deactivate the previous checkpoint
        if (currentActive != null && currentActive != this)
        {
            currentActive.Deactivate();
        }

        MarkActive();

        // Save to manager
        Vector3 spawnPos = transform.position + (Vector3)respawnOffset;
        string sceneName = SceneManager.GetActiveScene().name;

        if (CheckpointManager.instance != null)
        {
            CheckpointManager.instance.SetCheckpoint(spawnPos, sceneName);
        }

        AudioManager.PlaySFX(activateSound, volume);
    }

    private void MarkActive()
    {
        isActivated = true;
        currentActive = this;
        SetVisual(true);
    }

    private void Deactivate()
    {
        isActivated = false;
        SetVisual(false);
    }

    private void SetVisual(bool active)
    {
        if (sr == null) return;

        sr.sprite = active ? activeSprite : inactiveSprite;
        float alpha = active ? activeAlpha : inactiveAlpha;
        sr.color = new Color(1f, 1f, 1f, alpha);
    }

    void OnDrawGizmos()
    {
        // Draw respawn point
        Vector3 spawnPos = transform.position + (Vector3)respawnOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPos, 0.3f);
        Gizmos.DrawLine(transform.position, spawnPos);

        // Draw the trigger zone
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.6f, 0.25f);
            Gizmos.matrix = Matrix4x4.TRS(
                transform.TransformPoint(col.offset),
                transform.rotation,
                transform.lossyScale
            );
            Gizmos.DrawCube(Vector3.zero, col.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
