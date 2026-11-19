using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    public Image flashImage;

    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Длительность вспышки")]
    public float flashDuration = 0.15f;

    [Tooltip("Максимальная прозрачность вспышки (0-1). 1 = максимально ярко")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    private static ScreenFlash instance;
    private float timer = 0f;

    void Awake()
    {
        instance = this;

        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
            flashImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.unscaledDeltaTime;

        float t = Mathf.Clamp01(timer / flashDuration);
        Color c = flashImage.color;
        c.a = maxAlpha * t;
        flashImage.color = c;

        if (timer <= 0f)
        {
            c.a = 0f;
            flashImage.color = c;
            flashImage.gameObject.SetActive(false);
        }
    }

    public static void Flash(Color color)
    {
        if (instance == null) return;
        if (instance.flashImage == null) return;

        instance.flashImage.gameObject.SetActive(true);

        // сохраняем цвет переданный, alpha берём из maxAlpha
        color.a = instance.maxAlpha;
        instance.flashImage.color = color;
        instance.timer = instance.flashDuration;
    }
}