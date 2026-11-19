using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("=== ЗВУКИ ===")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("=== МАСШТАБ ===")]
    public float normalScale = 1f;
    public float hoverScale = 1.08f;
    public float clickScale = 0.94f;

    [Tooltip("Скорость анимации масштаба")]
    public float scaleSpeed = 12f;

    private float targetScale;
    private Vector3 baseScale;
    private bool wasHovered = false;

    void Start()
    {
        baseScale = transform.localScale;
        targetScale = normalScale;
    }

    void Update()
    {
        float current = baseScale.x > 0f ? transform.localScale.x / baseScale.x : 1f;
        float next = Mathf.Lerp(current, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        transform.localScale = baseScale * next;
    }

    // === МЫШЬ ===
    public void OnPointerEnter(PointerEventData e)
    {
        SetHover();
        if (!wasHovered)
        {
            PlaySound(hoverSound);
            wasHovered = true;
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        SetNormal();
        wasHovered = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        SetClick();
        PlaySound(clickSound);
    }

    public void OnPointerUp(PointerEventData e)
    {
        SetHover();
    }

    // === КЛАВИАТУРА ===
    public void OnSelect(BaseEventData e)
    {
        SetHover();
        if (!wasHovered)
        {
            PlaySound(hoverSound);
            wasHovered = true;
        }
    }

    public void OnDeselect(BaseEventData e)
    {
        SetNormal();
        wasHovered = false;
    }

    void SetNormal() { targetScale = normalScale; }
    void SetHover() { targetScale = hoverScale; }
    void SetClick() { targetScale = clickScale; }

    // === 2D ЗВУК ===
    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        GameObject go = new GameObject("MenuSoundTemp");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = soundVolume;
        src.spatialBlend = 0f;      // 2D — не зависит от позиции
        src.playOnAwake = false;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }
}