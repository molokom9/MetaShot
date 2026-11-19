using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    [Header("Панели (CanvasGroup)")]
    [SerializeField] private CanvasGroup blackPanel;
    [SerializeField] private CanvasGroup warningPanel;
    [SerializeField] private CanvasGroup splashPanel;

    [Header("Анимация заставки")]
    [SerializeField] private RectTransform imageFromRight;
    [SerializeField] private RectTransform imageFromLeft;
    [SerializeField] private TMP_Text blinkText;
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float slideOffset = 800f;
    [SerializeField] private float blinkInterval = 0.5f;

    [Header("Аудио")]
    [Tooltip("Фоновая музыка. Играет с заставки и не останавливается после интро.")]
    [SerializeField] private AudioSource splashMusic;
    [Tooltip("Звук при нажатии любой кнопки на заставке.")]
    [SerializeField] private AudioSource pressSound;
    [Range(0f, 1f)]
    [SerializeField] private float pressSoundVolume = 0.8f;

    [Header("Настройки")]
    [SerializeField] private float initialBlackHold = 1f;
    [SerializeField] private float fadeInSpeed = 0.6f;
    [SerializeField] private float warningHold = 4f;
    [SerializeField] private float fadeToSplash = 0.6f;
    [SerializeField] private float warningFadeOut = 0.6f;
    [SerializeField] private float splashInputDelay = 1f;
    [SerializeField] private float fastFadeOut = 0.3f;

    private bool anyKeyPressed;
    private bool inputEnabled;
    private bool skipWarningPressed;   // флаг пропуска дисклеймера
    private Coroutine blinkRoutine;

    private void Awake()
    {
        if (blackPanel) blackPanel.gameObject.SetActive(false);
        if (warningPanel) warningPanel.gameObject.SetActive(false);
        if (splashPanel) splashPanel.gameObject.SetActive(false);

        if (splashMusic)
        {
            splashMusic.playOnAwake = false;
            splashMusic.loop = true;
            splashMusic.Stop();
        }

        if (pressSound)
        {
            pressSound.playOnAwake = false;
            pressSound.loop = false;
        }

        inputEnabled = false;
    }

    private void Start() => StartCoroutine(PlayIntro());

    private void Update()
    {
        // Пропуск дисклеймера по Escape / Space / Enter
        if (!skipWarningPressed && warningPanel != null
            && warningPanel.gameObject.activeInHierarchy
            && warningPanel.alpha > 0.05f) // только когда реально виден
        {
            if (Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                skipWarningPressed = true;
            }
        }

        // Нажатие для заставки — только когда ввод разрешён
        if (inputEnabled && Input.anyKeyDown) anyKeyPressed = true;
    }

    private IEnumerator PlayIntro()
    {
        // 1. Чёрный экран
        if (blackPanel)
        {
            blackPanel.gameObject.SetActive(true);
            blackPanel.alpha = 1f;
        }
        yield return new WaitForSecondsRealtime(initialBlackHold);

        // 2. Дисклеймер
        if (warningPanel)
        {
            warningPanel.gameObject.SetActive(true);
            warningPanel.alpha = 0f;
        }
        yield return Fade(warningPanel, 0f, 1f, fadeInSpeed);

        // 3. Держим дисклеймер — с возможностью пропуска
        float t = 0f;
        skipWarningPressed = false;
        while (t < warningHold && !skipWarningPressed)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        skipWarningPressed = false;

        // 4. Заставка + музыка + выезд картинок
        if (splashPanel)
        {
            splashPanel.gameObject.SetActive(true);
            splashPanel.alpha = 1f;
        }

        // Музыка стартует и БОЛЬШЕ НЕ ОСТАНАВЛИВАЕТСЯ — продолжит играть в меню
        if (splashMusic && splashMusic.clip != null && !splashMusic.isPlaying)
            splashMusic.Play();

        // Картинки за кадром
        Vector2 rightTarget = Vector2.zero;
        Vector2 leftTarget = Vector2.zero;
        if (imageFromRight) { rightTarget = imageFromRight.anchoredPosition; imageFromRight.anchoredPosition = rightTarget + new Vector2(slideOffset, 0f); }
        if (imageFromLeft) { leftTarget = imageFromLeft.anchoredPosition; imageFromLeft.anchoredPosition = leftTarget + new Vector2(-slideOffset, 0f); }

        if (imageFromRight) StartCoroutine(SlideIn(imageFromRight, rightTarget, +1f, slideDuration));
        if (imageFromLeft) StartCoroutine(SlideIn(imageFromLeft, leftTarget, -1f, slideDuration));

        if (blinkText)
        {
            blinkText.gameObject.SetActive(true);
            blinkRoutine = StartCoroutine(BlinkRoutine());
        }

        StartCoroutine(Fade(warningPanel, 1f, 0f, warningFadeOut));
        yield return Fade(blackPanel, 1f, 0f, fadeToSplash);

        yield return new WaitForSecondsRealtime(warningFadeOut);

        if (blackPanel) { blackPanel.alpha = 0f; blackPanel.gameObject.SetActive(false); }
        if (warningPanel) { warningPanel.alpha = 0f; warningPanel.gameObject.SetActive(false); }

        // 5. Пауза без ввода
        anyKeyPressed = false;
        yield return new WaitForSecondsRealtime(splashInputDelay);

        // 6. Включаем ввод
        anyKeyPressed = false;
        inputEnabled = true;

        // 7. Ждём нажатия
        yield return new WaitUntil(() => anyKeyPressed);
        inputEnabled = false;

        // 7.1 Звук нажатия
        if (pressSound && pressSound.clip != null)
            pressSound.PlayOneShot(pressSound.clip, pressSoundVolume);

        // 8. Финальное затухание
        if (blinkRoutine != null) { StopCoroutine(blinkRoutine); blinkRoutine = null; }
        yield return Fade(splashPanel, 1f, 0f, fastFadeOut);

        // 9. Выключаем панели. Музыку НЕ трогаем — она уходит в меню.
        if (splashPanel) splashPanel.gameObject.SetActive(false);
        if (blackPanel) blackPanel.gameObject.SetActive(false);
        if (warningPanel) warningPanel.gameObject.SetActive(false);
    }

    private IEnumerator SlideIn(RectTransform rt, Vector2 target, float dir, float duration)
    {
        if (rt == null) yield break;
        Vector2 start = target + new Vector2(slideOffset * dir, 0f);
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / duration);
            k = 1f - (1f - k) * (1f - k);
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    private IEnumerator BlinkRoutine()
    {
        var c = blinkText.color;
        c.a = 1f;
        blinkText.color = c;

        while (true)
        {
            yield return new WaitForSecondsRealtime(blinkInterval);
            c.a = (c.a > 0.5f) ? 0f : 1f;
            blinkText.color = c;
        }
    }

    private IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null || !cg.gameObject.activeInHierarchy) yield break;
        cg.alpha = from;
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}