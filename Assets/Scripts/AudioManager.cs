using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Master Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Music Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    public AudioClip backgroundMusic;

    private AudioSource musicSource;
    private Dictionary<AudioClip, float> lastPlayedTime = new Dictionary<AudioClip, float>();
    [Tooltip("Minimum seconds between identical sound effects (prevents audio spam)")]
    public float deduplicateWindow = 0.05f;

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
            return;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.playOnAwake = false;
    }

    void Start()
    {
        PlayMusic(backgroundMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public static void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (instance == null || clip == null) return;

        float finalVolume = volume * instance.sfxVolume * instance.masterVolume;

        if (instance.lastPlayedTime.TryGetValue(clip, out float lastTime))
        {
            if (Time.unscaledTime - lastTime < instance.deduplicateWindow) return;
        }

        instance.lastPlayedTime[clip] = Time.unscaledTime;

        GameObject sfxGo = new GameObject($"SFX_{clip.name}");
        sfxGo.transform.SetParent(instance.transform);
        AudioSource src = sfxGo.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = finalVolume;
        src.spatialBlend = 0f;
        src.ignoreListenerPause = true;
        src.Play();

        Destroy(sfxGo, clip.length + 0.1f);
    }

    /// <summary>
    /// Play a non-positional SFX with custom pitch.
    /// </summary>
    public static void PlaySFX(AudioClip clip, float volume, float pitch)
    {
        if (instance == null || clip == null) return;

        float finalVolume = volume * instance.sfxVolume * instance.masterVolume;

        if (instance.lastPlayedTime.TryGetValue(clip, out float lastTime))
        {
            if (Time.unscaledTime - lastTime < instance.deduplicateWindow) return;
        }

        instance.lastPlayedTime[clip] = Time.unscaledTime;

        GameObject sfxGo = new GameObject($"SFX_{clip.name}");
        sfxGo.transform.SetParent(instance.transform);
        AudioSource src = sfxGo.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = finalVolume;
        src.pitch = pitch;
        src.spatialBlend = 0f;
        src.ignoreListenerPause = true;
        src.Play();

        Destroy(sfxGo, clip.length / Mathf.Abs(pitch) + 0.1f);
    }

    public static void PlaySFXAt(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (instance == null || clip == null) return;

        float finalVolume = volume * instance.sfxVolume * instance.masterVolume;
        AudioSource.PlayClipAtPoint(clip, position, finalVolume);
    }

    /// <summary>
    /// Play a spatial SFX with custom pitch. Useful for collision sound variety.
    /// </summary>
    public static void PlaySFXAt(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        if (instance == null || clip == null) return;

        float finalVolume = volume * instance.sfxVolume * instance.masterVolume;

        GameObject sfxGo = new GameObject($"SFX_{clip.name}");
        sfxGo.transform.position = position;
        sfxGo.transform.SetParent(instance.transform);
        AudioSource src = sfxGo.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = finalVolume;
        src.pitch = pitch;
        src.spatialBlend = 1f;
        src.Play();

        Destroy(sfxGo, clip.length / Mathf.Abs(pitch) + 0.1f);
    }

    /// <summary>
    /// Play a spatial SFX with custom 3D distance and rolloff settings.
    /// </summary>
    public static void PlaySFXAt(AudioClip clip, Vector3 position, float volume, float minDistance, float maxDistance, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, float pitch = 1f)
    {
        if (instance == null || clip == null) return;

        float finalVolume = volume * instance.sfxVolume * instance.masterVolume;

        GameObject sfxGo = new GameObject($"SFX_{clip.name}");
        sfxGo.transform.position = position;
        sfxGo.transform.SetParent(instance.transform);
        AudioSource src = sfxGo.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = finalVolume;
        src.pitch = pitch;
        src.spatialBlend = 1f;
        src.rolloffMode = rolloffMode;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.Play();

        Destroy(sfxGo, clip.length / Mathf.Abs(pitch) + 0.1f);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        musicSource.volume = musicVolume * masterVolume;
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        musicSource.volume = musicVolume * masterVolume;
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }
}
