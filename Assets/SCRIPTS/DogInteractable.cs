using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class DogInteractable : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("=== СПРАЙТЫ СОБАКИ ===")]
    [Tooltip("Спрайт, когда собака спит (по умолчанию)")]
    public Sprite spriteSleep;

    [Tooltip("Спрайт, когда собака проснулась (держится 6 секунд)")]
    public Sprite spriteAwake;

    [Tooltip("Спрайт, когда собака гавкает (держится 0.4 секунды)")]
    public Sprite spriteBark;

    [Header("=== ССЫЛКИ ===")]
    [Tooltip("Image, на котором висит спрайт собаки")]
    public Image dogImage;

    [Header("=== АНИМАЦИЯ НАЖАТИЯ ===")]
    [Tooltip("Насколько уменьшать собаку при нажатии (0.1 = на 10%)")]
    public float pressScale = 0.9f;

    [Tooltip("Длительность анимации нажатия/отпускания")]
    public float pressAnimDuration = 0.08f;

    [Header("=== КУЛДАУН ===")]
    [Tooltip("Минимальный интервал между 'гавками' (сек)")]
    public float barkCooldown = 1.2f;

    [Header("=== ЗВУКИ ЛАЯ ===")]
    public AudioSource barkAudioSource;
    public AudioClip[] barkClips;

    [Tooltip("Минимальный и максимальный Pitch при гавке (рандомизация)")]
    public float pitchMin = 0.85f;
    public float pitchMax = 1.15f;

    [Header("=== СЕРИЯ ГАВКОВ ===")]
    [Tooltip("Сколько гавков за одну серию (мин и макс, включительно)")]
    public int barksInSeriesMin = 2;
    public int barksInSeriesMax = 3;

    [Tooltip("Минимальная и максимальная пауза между гавками в серии")]
    public float barkGapMin = 0.18f;
    public float barkGapMax = 0.32f;

    [Tooltip("Длительность показа спрайта гавканья в серии")]
    public float barkSpriteDuration = 0.4f;

    [Header("=== ПАНЕЛЬ СОВЕТОВ ===")]
    public GameObject advicePanel;
    public TextMeshProUGUI adviceText;
    public float adviceShowDuration = 4f;

    [TextArea(2, 6)]
    public string[] advices =
    {
        "Совет: не забудь сохраниться!",
        "Совет: нажми Esc, чтобы открыть меню.",
        "Совет: исследуй каждый уголок — там может быть что-то интересное.",
        "Совет: не доверяй тишине...",
        "Совет: если станет страшно — просто закрой глаза. Но не сейчас.",
        "Гав! (перевод: ты справишься)",
        "Совет: иногда лучше отступить и вернуться позже."
    };

    [Header("=== ОСОБЫЕ ФРАЗЫ (ДОЛГО КЛИКАЕМ) ===")]
    [Tooltip("Через сколько секунд после последнего клика сбрасывается счётчик спама")]
    public float spamResetTime = 4f;

    [Tooltip("Сколько кликов нужно, чтобы собака начала выдавать особые фразы")]
    public int spamClickThreshold = 6;

    [TextArea(2, 6)]
    public string[] specialPhrases =
    {
        "Эй! Хватит меня тыкать!",
        "Гав! Я тебе не игрушка!",
        "Ты вообще спишь? Я — нет, благодаря тебе.",
        "Ещё раз кликнешь — укушу.",
        "Гав-гав-гав! (перевод: отстань)",
        "Может, хватит? У меня лапы устали от твоих кликов.",
        "Ты серьёзно? Я тут пытаюсь быть милой.",
        "Ладно, ты победил. Гав.",
        "Гав! Хватит! У меня уже голова болит от твоего курсора."
    };

    [Header("=== НАСТРОЙКИ ===")]
    public float awakeDuration = 6f;

    // === ВНУТРЕННИЕ ===
    private enum DogState { Sleeping, Awake, Barking }
    private DogState state = DogState.Sleeping;

    private Coroutine awakeCoroutine;
    private Coroutine barkSeriesCoroutine;
    private Coroutine adviceCoroutine;
    private Coroutine pressAnimCoroutine;

    private Vector3 originalScale;
    private float lastBarkTime = -999f;

    // === СЧЁТЧИК СПАМА ===
    private int spamClickCount = 0;
    private float lastSpamClickTime = -999f;
    private bool isSpamMode = false;
    private int specialPhraseIndex = 0;

    void Start()
    {
        if (dogImage == null)
            dogImage = GetComponent<Image>();

        if (dogImage == null)
            Debug.LogWarning("[DogInteractable] Не найден Image! Повесь скрипт на UI Image собаки.");

        if (advicePanel != null)
            advicePanel.SetActive(false);

        originalScale = transform.localScale;

        state = DogState.Sleeping;
        if (dogImage != null && spriteSleep != null)
            dogImage.sprite = spriteSleep;
    }

    // === КЛИК ПО СОБАКЕ ===
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        OnDogClicked();
    }

    // === АНИМАЦИЯ НАЖАТИЯ ===
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        StartPressAnim(originalScale * pressScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        StartPressAnim(originalScale);
    }

    void StartPressAnim(Vector3 targetScale)
    {
        if (pressAnimCoroutine != null) StopCoroutine(pressAnimCoroutine);
        pressAnimCoroutine = StartCoroutine(PressAnim(targetScale));
    }

    IEnumerator PressAnim(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < pressAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / pressAnimDuration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, k);
            yield return null;
        }

        transform.localScale = targetScale;
        pressAnimCoroutine = null;
    }

    // === ЛОГИКА КЛИКА ===
    void OnDogClicked()
    {
        RegisterSpamClick();

        if (state == DogState.Sleeping)
        {
            WakeUp();
            if (isSpamMode)
                ShowSpecialPhrase();
            return;
        }

        if (state == DogState.Awake || state == DogState.Barking)
        {
            // Кулдаун — чтобы не спамить серии гавков
            if (Time.unscaledTime - lastBarkTime < barkCooldown)
                return;

            lastBarkTime = Time.unscaledTime;

            // Запускаем серию гавков (2-3) со сменой спрайта
            StartBarkSeries();

            // Совет показываем ТОЛЬКО в обычном режиме (не в спам-режиме)
            if (!isSpamMode)
                ShowAdvice();
            else
                ShowSpecialPhrase();
        }
    }

    // === СЕРИЯ ГАВКОВ ===
    void StartBarkSeries()
    {
        if (barkSeriesCoroutine != null)
            StopCoroutine(barkSeriesCoroutine);

        barkSeriesCoroutine = StartCoroutine(BarkSeries());
    }

    IEnumerator BarkSeries()
    {
        state = DogState.Barking;

        int count = Random.Range(barksInSeriesMin, barksInSeriesMax + 1);

        for (int i = 0; i < count; i++)
        {
            // Спрайт гавканья
            if (dogImage != null && spriteBark != null)
                dogImage.sprite = spriteBark;

            // Случайный гавк с рандомным питчем
            PlayRandomBark();

            // Держим спрайт гавканья
            yield return new WaitForSecondsRealtime(barkSpriteDuration);

            // Между гавками (кроме последнего) — короткая пауза со спрайтом "проснулась"
            if (i < count - 1)
            {
                if (dogImage != null && spriteAwake != null)
                    dogImage.sprite = spriteAwake;

                float gap = Random.Range(barkGapMin, barkGapMax);
                yield return new WaitForSecondsRealtime(gap);
            }
        }

        // Возвращаемся в состояние "проснулась"
        if (dogImage != null && spriteAwake != null)
            dogImage.sprite = spriteAwake;

        state = DogState.Awake;

        // Перезапускаем таймер сна
        if (awakeCoroutine != null) StopCoroutine(awakeCoroutine);
        awakeCoroutine = StartCoroutine(AwakeTimer());

        barkSeriesCoroutine = null;
    }

    // === СЧЁТЧИК СПАМА ===
    void RegisterSpamClick()
    {
        float now = Time.unscaledTime;

        if (now - lastSpamClickTime > spamResetTime)
        {
            spamClickCount = 0;
            isSpamMode = false;
        }

        lastSpamClickTime = now;
        spamClickCount++;

        if (spamClickCount >= spamClickThreshold)
            isSpamMode = true;
    }

    void ResetSpamModeIfIdle()
    {
        if (!isSpamMode) return;

        if (Time.unscaledTime - lastSpamClickTime > spamResetTime)
        {
            isSpamMode = false;
            spamClickCount = 0;
        }
    }

    void WakeUp()
    {
        if (dogImage != null && spriteAwake != null)
            dogImage.sprite = spriteAwake;

        state = DogState.Awake;

        if (awakeCoroutine != null) StopCoroutine(awakeCoroutine);
        awakeCoroutine = StartCoroutine(AwakeTimer());
    }

    IEnumerator AwakeTimer()
    {
        yield return new WaitForSecondsRealtime(awakeDuration);

        if (state == DogState.Awake)
        {
            state = DogState.Sleeping;
            if (dogImage != null && spriteSleep != null)
                dogImage.sprite = spriteSleep;
        }

        awakeCoroutine = null;
    }

    void PlayRandomBark()
    {
        if (barkAudioSource == null || barkClips == null || barkClips.Length == 0)
            return;

        AudioClip clip = barkClips[Random.Range(0, barkClips.Length)];
        barkAudioSource.pitch = Random.Range(pitchMin, pitchMax);
        barkAudioSource.PlayOneShot(clip);
    }

    void ShowAdvice()
    {
        if (advicePanel == null) return;

        if (adviceCoroutine != null) StopCoroutine(adviceCoroutine);

        if (adviceText != null && advices != null && advices.Length > 0)
            adviceText.text = advices[Random.Range(0, advices.Length)];

        advicePanel.SetActive(true);
        adviceCoroutine = StartCoroutine(HideAdviceTimer());
    }

    IEnumerator HideAdviceTimer()
    {
        yield return new WaitForSecondsRealtime(adviceShowDuration);

        if (advicePanel != null)
            advicePanel.SetActive(false);

        adviceCoroutine = null;
    }

    // === ОСОБЫЕ ФРАЗЫ ===
    void ShowSpecialPhrase()
    {
        if (advicePanel == null) return;

        if (adviceCoroutine != null) StopCoroutine(adviceCoroutine);

        string phrase = GetNextSpecialPhrase();

        if (adviceText != null)
            adviceText.text = phrase;

        advicePanel.SetActive(true);
        adviceCoroutine = StartCoroutine(HideAdviceTimer());
    }

    string GetNextSpecialPhrase()
    {
        if (specialPhrases == null || specialPhrases.Length == 0)
            return "Гав!";

        if (specialPhrases.Length == 1)
            return specialPhrases[0];

        int newIndex = specialPhraseIndex;

        for (int i = 0; i < 10; i++)
        {
            int candidate = Random.Range(0, specialPhrases.Length);
            if (candidate != specialPhraseIndex)
            {
                newIndex = candidate;
                break;
            }
        }

        specialPhraseIndex = newIndex;
        return specialPhrases[newIndex];
    }

    void Update()
    {
        ResetSpamModeIfIdle();
    }
}