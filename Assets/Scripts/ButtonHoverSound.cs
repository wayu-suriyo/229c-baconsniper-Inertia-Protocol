using UnityEngine;
using UnityEngine.EventSystems; 

public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("Hover Sound")]
    public AudioClip hoverClip;
    [Range(0f, 1f)]
    public float volume = 0.3f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[ButtonHoverSound] Hovered over {gameObject.name}");
        if (hoverClip != null)
        {
            GameObject soundGo = new GameObject("HoverSound2D");
            AudioSource source = soundGo.AddComponent<AudioSource>();
            
            source.clip = hoverClip;
            source.volume = volume;
            source.spatialBlend = 0f; 
            source.ignoreListenerPause = true; 
            
            source.Play();
            Destroy(soundGo, hoverClip.length + 0.1f);
        }
    }
}