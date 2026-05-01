using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    /// <summary>Whether a checkpoint has been activated in the current level.</summary>
    public bool HasCheckpoint { get; private set; } = false;

    /// <summary>World position of the last activated checkpoint.</summary>
    public Vector3 RespawnPosition { get; private set; }

    /// <summary>Total deaths since the level was started or last fully restarted.</summary>
    public int DeathCount { get; private set; } = 0;

    public void IncrementDeathCount()
    {
        DeathCount++;
        Debug.Log($"Death count: {DeathCount}");
    }

    public void ResetDeathCount()
    {
        DeathCount = 0;
    }

    /// <summary>Name of the scene the checkpoint belongs to (prevents cross-level bleed).</summary>
    public string CheckpointSceneName { get; private set; } = "";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by Checkpoint trigger zones when the drone passes through.
    /// </summary>
    public void SetCheckpoint(Vector3 position, string sceneName)
    {
        RespawnPosition = position;
        CheckpointSceneName = sceneName;
        HasCheckpoint = true;
        Debug.Log($"Checkpoint saved at {position} in scene '{sceneName}'");
    }

    /// <summary>
    /// Returns true if there's a valid checkpoint for the current scene.
    /// </summary>
    public bool HasCheckpointForCurrentScene()
    {
        return HasCheckpoint &&
               CheckpointSceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Clears the saved checkpoint (e.g. when the level is completed or exiting to menu).
    /// Also resets the death count.
    /// </summary>
    public void ClearCheckpoint()
    {
        HasCheckpoint = false;
        CheckpointSceneName = "";
        DeathCount = 0;
    }
}
