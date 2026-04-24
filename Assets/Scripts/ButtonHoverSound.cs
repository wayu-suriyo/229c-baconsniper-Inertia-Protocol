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
            // สร้าง GameObject สำหรับเล่นเสียง 2D โดยเฉพาะ
            GameObject soundGo = new GameObject("HoverSound2D");
            AudioSource source = soundGo.AddComponent<AudioSource>();
            
            source.clip = hoverClip;
            source.volume = volume;
            source.spatialBlend = 0f; // 2D Sound (เสียงดังเท่ากันหมดไม่ต้องสนระยะห่าง)
            source.ignoreListenerPause = true; // ไม่ถูกหยุดแม้เกมหยุด
            
            source.Play();
            Destroy(soundGo, hoverClip.length + 0.1f);
        }
    }
}