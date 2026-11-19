using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Титры: чёрный экран -> пауза -> высветление -> пауза ->
/// ЖЁСТКАЯ тряска + мгновенная смена спрайта + punch-scale -> титры -> меню.
/// Текст ВЫКЛЮЧЕН и включается когда нужно показать первое имя.
/// Курсор скрыт на время титров.
/// </summary>
public class CreditsRoll : MonoBehaviour
{
    public enum TextType { TextMeshProUGUI, LegacyText }

    // ============================================================
    // === ULTRA FAST MODE ===
    // ============================================================

    [Tooltip("Включить: тряска становится жёсткой и быстрой. НЕ влияет на тайминги титров.")]
    public bool ultraFastMode = true;

    [Tooltip("Тряхнуть ЕЩЁ РАЗ после смены спрайта (двойной удар: бах-бах)")]
    public bool doubleShake = true;

    [Tooltip("Резко раздуть спрайт и вернуть при смене (punch scale)")]
    public bool scalePunchOnSwap = true;

    // ============================================================
    // === КУРСОР ===
    // ============================================================

    [Tooltip("Скрыть и заблокировать курсор на время титров")]
    public bool hideCursor = true;

    [Tooltip("Показать курсор перед загрузкой меню (если меню использует мышь)")]
    public bool restoreCursorBeforeMenu = true;

    // ============================================================
    // === ЗАТЕМНЕНИЕ ЭКРАНА ===
    // ============================================================

    [Tooltip("UI Image чёрного цвета на весь экран (перекрывает всё)")]
    public Image fadePanel;

    [Tooltip("Сколько секунд держать экран ПОЛНОСТЬЮ ЧЁРНЫМ до начала высветления")]
    public float delayBeforeScreenFade = 0f;

    [Tooltip("Сколько секунд чёрный экран плавно исчезает (высветление)")]
    public float screenFadeInDuration = 1.5f;

    [Tooltip("Сколько секунд ждать после высветления ПЕРЕД началом тряски")]
    public float delayAfterFade = 3f;

    // ============================================================
    // === ТРЯС КАРТИНКИ ===
    // ============================================================

    [Tooltip("UI Image, который нужно тряхнуть и сменить спрайт")]
    public Image shakeImage;

    [Tooltip("Новый спрайт, который установится ПОСЛЕ тряски (можно оставить пустым)")]
    public Sprite newSpriteAfterShake;

    [Tooltip("Сколько секунд длится тряска")]
    public float shakeDuration = 0.15f;

    [Tooltip("Амплитуда тряски в пикселях (больше = сильнее)")]
    public float shakeStrength = 30f;

    [Tooltip("Как часто меняется направление тряски (сек). Меньше = злее")]
    public float shakeFrequency = 0.015f;

    [Tooltip("Пауза ПОСЛЕ тряски перед показом первого имени")]
    public float delayAfterShake = 0.05f;

    // ============================================================
    // === ТЕКСТ ===
    // ============================================================

    [Tooltip("Тип текста: TextMeshProUGUI (TMP) или LegacyText (обычный UI Text)")]
    public TextType textType = TextType.TextMeshProUGUI;

    [Tooltip("Компонент TextMeshProUGUI (если textType = TMP)")]
    public TextMeshProUGUI tmpText;

    [Tooltip("Компонент обычного UI Text (если textType = LegacyText)")]
    public Text legacyText;

    // ============================================================
    // === ИМЕНА ===
    // ============================================================

    [Tooltip("Список имён/строк, которые показываются по порядку")]
    public List<string> names = new List<string>();

    // ============================================================
    // === ТАЙМИНГИ ТИТРОВ ===
    // ============================================================

    [Tooltip("Сколько секунд имя плавно ПРОЯВЛЯЕТСЯ")]
    public float fadeInDuration = 0.3f;

    [Tooltip("Сколько секунд имя ПОЛНОСТЬЮ ВИДНО (между появлением и исчезновением)")]
    public float holdDuration = 2f;

    [Tooltip("Сколько секунд имя плавно ИСЧЕЗАЕТ")]
    public float fadeOutDuration = 0.3f;

    [Tooltip("Пауза между именами (после исчезновения одного, до появления следующего)")]
    public float gapBetweenNames = 0.2f;

    // ============================================================
    // === ФИНИШ ===
    // ============================================================

    [Tooltip("Сколько секунд ждать после последнего имени перед загрузкой сцены")]
    public float delayAfterCredits = 5f;

    [Tooltip("Имя сцены главного меню для загрузки в конце (пусто = не грузить)")]
    public string mainMenuSceneName = "MainMenu";

    // ============================================================
    // === ЗВУК ===
    // ============================================================

    [Tooltip("AudioSource для звуков титров (если пусто — найдёт/добавит на этом объекте)")]
    public AudioSource audioSource;

    [Tooltip("Звук при появлении каждого имени (опционально)")]
    public AudioClip nameAppearSound;

    [Range(0f, 1f)]
    [Tooltip("Общая громкость звуков титров")]
    public float soundVolume = 1f;

    [Tooltip("Звук при тряске картинки (опционально)")]
    public AudioClip shakeSound;

    // ============================================================
    // === ОТЛАДКА ===
    // ============================================================

    [Tooltip("Писать в консоль что происходит (для отладки)")]
    public bool debugLogs = false;

    // ============================================================
    // === ВНУТРЕННИЕ (не трогать в инспекторе) ===
    // ============================================================

    // Текущая корутина последовательности (чтобы можно было остановить)
    private Coroutine routine;

    // GameObject, на котором висит текст (TMP или Legacy)
    private GameObject textGameObject;

    // Исходная позиция картинки (чтобы вернуть после тряски)
    private Vector2 shakeImageOriginalPos;

    // Исходный масштаб картинки (чтобы вернуть после punch-scale)
    private Vector3 shakeImageOriginalScale;

    // Флаг, что пресет уже применён (чтобы не применять повторно)
    private bool presetApplied;

    void Awake()
    {
        ApplyUltraFastPreset();
    }

    void Start()
    {
        // Автопоиск компонентов, если не назначены в инспекторе
        if (tmpText == null && textType == TextType.TextMeshProUGUI)
            tmpText = GetComponent<TextMeshProUGUI>();

        if (legacyText == null && textType == TextType.LegacyText)
            legacyText = GetComponent<Text>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Скрытие курсора
        if (hideCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Определяем GameObject с текстом (НЕ включаем — включим когда надо)
        textGameObject = GetTextGameObject();
        if (textGameObject == null)
            Debug.LogWarning("[CreditsRoll] Не найден компонент текста!");

        // Запоминаем исходную позицию и масштаб картинки
        if (shakeImage != null)
        {
            RectTransform rt = shakeImage.rectTransform;
            if (rt != null)
            {
                shakeImageOriginalPos = rt.anchoredPosition;
                shakeImageOriginalScale = rt.localScale;
            }
        }

        // Изначально текст скрыт
        SetTextAlpha(0f);

        // Чёрный экран — полностью чёрный в начале
        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 1f;
            fadePanel.color = c;
            fadePanel.gameObject.SetActive(true);
        }

        routine = StartCoroutine(RunSequence());
    }

    /// <summary>
    /// Применяет жёсткие пресеты ТОЛЬКО для тряски (не для титров).
    /// </summary>
    void ApplyUltraFastPreset()
    {
        if (!ultraFastMode || presetApplied) return;
        presetApplied = true;

        // Тряска — жёсткая и быстрая
        shakeDuration = 0.15f;
        shakeStrength = 30f;
        shakeFrequency = 0.015f;
        delayAfterShake = 0.05f;

        // ⚠️ Тайминги титров НЕ трогаем — они настраиваются в инспекторе
    }

    void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        RestoreCursor();
    }

    void RestoreCursor()
    {
        if (!hideCursor) return;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    GameObject GetTextGameObject()
    {
        if (textType == TextType.TextMeshProUGUI && tmpText != null)
            return tmpText.gameObject;
        if (textType == TextType.LegacyText && legacyText != null)
            return legacyText.gameObject;
        return null;
    }

    void EnsureTextObjectActive()
    {
        if (textGameObject == null) return;
        if (!textGameObject.activeSelf)
        {
            textGameObject.SetActive(true);
            if (debugLogs) Debug.Log($"[CreditsRoll] Текст '{textGameObject.name}' включён.");
        }
    }

    IEnumerator RunSequence()
    {
        // 0. Пауза чёрным экраном
        if (delayBeforeScreenFade > 0f)
        {
            if (fadePanel != null)
            {
                Color c0 = fadePanel.color;
                c0.a = 1f;
                fadePanel.color = c0;
                if (!fadePanel.gameObject.activeSelf)
                    fadePanel.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(delayBeforeScreenFade);
        }

        // 1. Выцветание чёрного экрана
        if (fadePanel != null && screenFadeInDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < screenFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / screenFadeInDuration);
                Color c = fadePanel.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                fadePanel.color = c;
                yield return null;
            }
            Color finalColor = fadePanel.color;
            finalColor.a = 0f;
            fadePanel.color = finalColor;
            fadePanel.gameObject.SetActive(false);
        }

        // 2. Пауза после высветления
        if (delayAfterFade > 0f)
            yield return new WaitForSeconds(delayAfterFade);

        // 3. ЖЁСТКАЯ ТРЯСКА + СМЕНА СПРАЙТА + PUNCH SCALE
        yield return ShakeAndSwapImage();

        // 3.1 (опционально) Второй удар после смены спрайта
        if (doubleShake)
        {
            yield return new WaitForSeconds(0.02f);
            yield return ShakeOnly(0.08f, shakeStrength * 1.2f, 0.015f);
        }

        // 4. Пауза после тряски
        if (delayAfterShake > 0f)
            yield return new WaitForSeconds(delayAfterShake);

        // 5. Титры (по очереди)
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            if (string.IsNullOrEmpty(name))
                continue;

            EnsureTextObjectActive();

            SetText(name);
            SetTextAlpha(0f);

            if (nameAppearSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.98f, 1.02f);
                audioSource.PlayOneShot(nameAppearSound, soundVolume);
            }

            // Появление
            yield return FadeTextAlpha(0f, 1f, fadeInDuration);

            // Держим
            if (holdDuration > 0f)
                yield return new WaitForSeconds(holdDuration);

            // Исчезновение
            yield return FadeTextAlpha(1f, 0f, fadeOutDuration);

            // Пауза между именами
            if (gapBetweenNames > 0f)
                yield return new WaitForSeconds(gapBetweenNames);
        }

        SetTextAlpha(0f);

        // 6. Пауза перед меню
        if (delayAfterCredits > 0f)
            yield return new WaitForSeconds(delayAfterCredits);

        if (restoreCursorBeforeMenu)
            RestoreCursor();

        // 7. Загрузка сцены
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            if (debugLogs) Debug.Log($"[CreditsRoll] Загружаю сцену: '{mainMenuSceneName}'");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    /// <summary>
    /// Тряска + смена спрайта + punch scale.
    /// </summary>
    IEnumerator ShakeAndSwapImage()
    {
        if (shakeImage == null) yield break;
        RectTransform rt = shakeImage.rectTransform;
        if (rt == null) yield break;

        if (!shakeImage.gameObject.activeSelf)
            shakeImage.gameObject.SetActive(true);

        // Звук тряски
        if (shakeSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(shakeSound, soundVolume);
        }

        if (debugLogs) Debug.Log($"[CreditsRoll] Тряска '{shakeImage.name}' — СТАРТ");

        // 1) ТРЯСКА
        yield return ShakeOnly(shakeDuration, shakeStrength, shakeFrequency);
        rt.anchoredPosition = shakeImageOriginalPos;

        // 2) МГНОВЕННАЯ СМЕНА СПРАЙТА
        if (newSpriteAfterShake != null)
        {
            shakeImage.sprite = newSpriteAfterShake;
            if (debugLogs) Debug.Log($"[CreditsRoll] Спрайт '{shakeImage.name}' СМЕНЁН!");
        }

        // 3) PUNCH SCALE
        if (scalePunchOnSwap)
            yield return PunchScale(rt, 1f, 1.25f, 0.10f);

        if (debugLogs) Debug.Log($"[CreditsRoll] Тряска '{shakeImage.name}' — END");
    }

    /// <summary>
    /// Только тряска. В конце возвращает позицию в исходную.
    /// </summary>
    IEnumerator ShakeOnly(float duration, float strength, float frequency)
    {
        if (shakeImage == null) yield break;
        RectTransform rt = shakeImage.rectTransform;
        if (rt == null) yield break;

        float elapsed = 0f;
        float freqTimer = 0f;
        Vector2 offset = Vector2.zero;

        float freq = Mathf.Max(0.01f, frequency);
        float dur = Mathf.Max(0.01f, duration);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            freqTimer -= Time.deltaTime;

            if (freqTimer <= 0f)
            {
                freqTimer = freq;
                offset = new Vector2(
                    Random.Range(-strength, strength),
                    Random.Range(-strength, strength));
            }

            // Затухание к концу (последние 30% — плавно к 0)
            float fade = 1f;
            float tailStart = dur * 0.7f;
            if (elapsed > tailStart)
            {
                float k = (elapsed - tailStart) / Mathf.Max(0.0001f, dur - tailStart);
                fade = 1f - Mathf.Clamp01(k);
            }

            rt.anchoredPosition = shakeImageOriginalPos + offset * fade;
            yield return null;
        }

        rt.anchoredPosition = shakeImageOriginalPos;
    }

    /// <summary>
    /// Punch-scale: резко раздувается до to и возвращается.
    /// </summary>
    IEnumerator PunchScale(RectTransform rt, float from, float to, float duration)
    {
        if (rt == null) yield break;
        Vector3 baseScale = shakeImageOriginalScale;

        // Раздуваемся
        float half = duration * 0.35f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            float s = Mathf.Lerp(from, to, t);
            rt.localScale = baseScale * s;
            yield return null;
        }

        // Возвращаемся
        elapsed = 0f;
        float half2 = duration - half;
        while (elapsed < half2)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half2);
            float s = Mathf.Lerp(to, 1f, t);
            rt.localScale = baseScale * s;
            yield return null;
        }

        rt.localScale = baseScale;
    }

    IEnumerator FadeTextAlpha(float from, float to, float duration)
    {
        if (duration <= 0f) { SetTextAlpha(to); yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTextAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetTextAlpha(to);
    }

    void SetText(string s)
    {
        if (textType == TextType.TextMeshProUGUI && tmpText != null)
            tmpText.text = s;
        else if (textType == TextType.LegacyText && legacyText != null)
            legacyText.text = s;
    }

    void SetTextAlpha(float a)
    {
        if (textType == TextType.TextMeshProUGUI && tmpText != null)
        {
            Color c = tmpText.color;
            c.a = a;
            tmpText.color = c;
        }
        else if (textType == TextType.LegacyText && legacyText != null)
        {
            Color c = legacyText.color;
            c.a = a;
            legacyText.color = c;
        }
    }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);

        if (hideCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        SetTextAlpha(0f);
        routine = StartCoroutine(RunSequence());
    }

    public void Stop()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        SetTextAlpha(0f);
        RestoreCursor();
    }
}