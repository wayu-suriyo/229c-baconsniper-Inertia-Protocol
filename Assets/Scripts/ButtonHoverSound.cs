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
        AudioManager.PlaySFX(hoverClip, volume);
    }
}