using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;
    public bool HasCheckpoint { get; private set; } = false;
    public Vector3 RespawnPosition { get; private set; }
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

    public void SetCheckpoint(Vector3 position, string sceneName)
    {
        RespawnPosition = position;
        CheckpointSceneName = sceneName;
        HasCheckpoint = true;
        Debug.Log($"Checkpoint saved at {position} in scene '{sceneName}'");
    }

    public bool HasCheckpointForCurrentScene()
    {
        return HasCheckpoint &&
               CheckpointSceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    public void ClearCheckpoint()
    {
        HasCheckpoint = false;
        CheckpointSceneName = "";
        DeathCount = 0;
    }
}
