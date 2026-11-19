using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    private enum ConfirmAction
    {
        None,
        NewGame,
        Quit,
        Credits
    }

    // ✅ Класс для привязки Кнопки и Ссылки
    [System.Serializable]
    public class ButtonLink
    {
        [Tooltip("Кнопка из UI")]
        public Button button;
        [Tooltip("Ссылка, которая откроется при клике")]
        public string url = "https://example.com";
    }

    [Header("=== ПАНЕЛЬ ГЛАВНОГО МЕНЮ ===")]
    public GameObject mainPanel;

    [Header("=== КНОПКИ ГЛАВНОГО МЕНЮ ===")]
    public Button continueButton;
    public Button newGameButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("=== ВНЕШНИЕ ССЫЛКИ (КНОПКИ) ===")]
    [Tooltip("Добавь сюда кнопки и ссылки. Сколько добавишь — столько и будет работать.")]
    public List<ButtonLink> linkButtons = new List<ButtonLink>();

    [Header("=== ВИД КНОПКИ CONTINUE (ОПЦИОНАЛЬНО) ===")]
    public TextMeshProUGUI continueButtonText;
    public Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Серый цвет
    private Color normalTextColor;

    [Header("=== ИМЯ СЦЕНЫ ИГРЫ ===")]
    public string gameSceneName = "GAME";

    [Header("=== ПАНЕЛЬ ПОДТВЕРЖДЕНИЯ ===")]
    public GameObject confirmPanel;

    [TextArea(2, 4)]
    public string confirmNewGameMessage = "Ты уверен, что хочешь начать новую игру?\n<b>Всё сохранение будет удалено!</b>";

    [TextArea(2, 4)]
    public string confirmQuitMessage = "Ты уверен, что хочешь выйти из игры?";

    [TextArea(4, 8)]
    public string creditsText =
        "Игра сделана: <b>Твоё Имя</b>\n" +
        "Программирование: Ты\n" +
        "Арт: Ты\n" +
        "Музыка: Кто-то\n" +
        "\nСпасибо за игру!";

    public TextMeshProUGUI confirmText;

    public Button confirmYesButton;
    public Button confirmNoButton;
    public Button confirmCloseButton;

    [Header("=== ГАММА ===")]
    public string gammaPrefsKey = "GammaValue";
    public Slider gammaSlider;
    public bool applyGammaOnStart = true;

    [Header("=== НАСТРОЙКИ МЕНЮ ===")]
    public bool showCursor = true;
    public bool resetTimeScale = true;

    [Header("=== ВСТУПЛЕНИЕ (INTRO) ===")]
    public GameObject introPanel;
    public TextMeshProUGUI introText;
    public GameObject pressAnyKeyText;

    [Tooltip("Объект, который появляется вместе с панелью предыстории (после полного затемнения и паузы)")]
    public GameObject introExtraObject;

    [TextArea(5, 20)]
    public string introTextRU = "Здесь твоя предыстория на русском...\nОна будет печататься по буквам.";

    [TextArea(5, 20)]
    public string introTextEN = "Here is your backstory in English...\nIt will be typed letter by letter.";

    public Button languageToggleButton;
    public TextMeshProUGUI languageToggleLabel;

    public float typeSpeed = 0.04f;
    public float punctuationPause = 0.25f;
    public float fadeDuration = 1.0f;

    public GameObject fadePanel;

    [Header("=== МУЗЫКА ===")]
    public AudioSource musicSource;
    public float musicFadeDuration = 0.5f;
    public bool stopMusicOnIntro = true;

    [Header("=== ПЕРЕХОД К ВСТУПЛЕНИЮ (NEW GAME) ===")]
    public float introFadeInDuration = 1.5f;
    public float introBlackHoldDuration = 2.0f;
    public float introFadeInRevealDuration = 1.0f;

    // === ВНУТРЕННИЕ ===
    private bool isConfirmOpen = false;
    private ConfirmAction currentConfirmAction = ConfirmAction.None;

    private bool isIntroActive = false;
    private bool isTyping = false;
    private bool isIntroLanguageRU = true;
    private Coroutine introCoroutine;
    private Coroutine musicFadeCoroutine;
    private CanvasGroup fadeCanvasGroup;
    private CanvasGroup introCanvasGroup;

    void Start()
    {
        if (resetTimeScale)
            Time.timeScale = 1f;

        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ApplySavedGamma();

        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (introPanel != null) introPanel.SetActive(false);
        if (pressAnyKeyText != null) pressAnyKeyText.SetActive(false);
        if (introExtraObject != null) introExtraObject.SetActive(false);

        if (fadePanel != null)
        {
            fadeCanvasGroup = fadePanel.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
                fadeCanvasGroup = fadePanel.AddComponent<CanvasGroup>();

            fadeCanvasGroup.alpha = 1f;
            fadePanel.SetActive(false);
        }

        if (introPanel != null)
        {
            introCanvasGroup = introPanel.GetComponent<CanvasGroup>();
            if (introCanvasGroup == null)
                introCanvasGroup = introPanel.AddComponent<CanvasGroup>();
            introCanvasGroup.alpha = 1f;
        }

        if (continueButtonText != null)
            normalTextColor = continueButtonText.color;

        // Настройка кнопок
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCredits);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

        // ✅ НАСТРОЙКА ВНЕШНИХ ССЫЛОК
        SetupLinkButtons();

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNo);
        if (confirmCloseButton != null) confirmCloseButton.onClick.AddListener(OnConfirmClose);

        if (gammaSlider != null)
            gammaSlider.onValueChanged.AddListener(OnGammaSliderChanged);

        if (languageToggleButton != null)
            languageToggleButton.onClick.AddListener(ToggleIntroLanguage);

        UpdateLanguageLabel();

        UpdateContinueButtonState();
    }

    // ✅ МЕТОД ДЛЯ ПРИВЯЗКИ ССЫЛОК К КНОПКАМ
    void SetupLinkButtons()
    {
        foreach (var link in linkButtons)
        {
            if (link != null && link.button != null && !string.IsNullOrEmpty(link.url))
            {
                string url = link.url; // Локальная переменная, чтобы не замыкание
                link.button.onClick.AddListener(() => OnLinkButtonClicked(url));
            }
        }
    }

    void OnLinkButtonClicked(string url)
    {
        Debug.Log($"[MainMenu] Открываю ссылку: {url}");
        Application.OpenURL(url);
    }

    void UpdateContinueButtonState()
    {
        if (continueButton == null) return;

        bool hasSave = GameSave.HasSave();

        continueButton.interactable = hasSave;

        if (continueButtonText != null)
        {
            continueButtonText.color = hasSave ? normalTextColor : disabledTextColor;
        }
    }

    void ApplySavedGamma()
    {
        if (!applyGammaOnStart) return;
        float savedGamma = PlayerPrefs.GetFloat(gammaPrefsKey, 1f);
        if (gammaSlider != null)
            gammaSlider.SetValueWithoutNotify(savedGamma);
    }

    void OnGammaSliderChanged(float value)
    {
        PlayerPrefs.SetFloat(gammaPrefsKey, value);
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (isConfirmOpen)
        {
            if (currentConfirmAction == ConfirmAction.NewGame ||
                currentConfirmAction == ConfirmAction.Quit)
            {
                if (Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnConfirmYes();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentConfirmAction == ConfirmAction.Credits)
                    OnConfirmClose();
                else
                    OnConfirmNo();
                return;
            }
        }

        if (isIntroActive)
        {
            if (isTyping)
            {
                if (Input.anyKeyDown &&
                    !Input.GetMouseButtonDown(0) &&
                    !Input.GetMouseButtonDown(1) &&
                    !Input.GetMouseButtonDown(2))
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) return;
                    SkipTyping();
                }
            }
            else
            {
                if (Input.anyKeyDown)
                {
                    StartIntroFadeOut();
                }
            }
        }
    }

    void OnContinue()
    {
        if (!GameSave.HasSave()) return;

        if (mainPanel != null) mainPanel.SetActive(false);

        if (musicSource != null)
        {
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }

        StartCoroutine(FadeOutAndLoad());
    }

    void OnNewGame() { OpenConfirm(ConfirmAction.NewGame, confirmNewGameMessage); }
    void OnCredits() { OpenConfirm(ConfirmAction.Credits, creditsText); }
    void OnQuit() { OpenConfirm(ConfirmAction.Quit, confirmQuitMessage); }

    void OpenConfirm(ConfirmAction action, string message)
    {
        if (isConfirmOpen) return;
        isConfirmOpen = true;
        currentConfirmAction = action;

        if (confirmText != null && !string.IsNullOrEmpty(message))
            confirmText.text = message;

        if (confirmPanel != null) confirmPanel.SetActive(true);

        bool isCreditsMode = (action == ConfirmAction.Credits);

        if (confirmYesButton != null) confirmYesButton.gameObject.SetActive(!isCreditsMode);
        if (confirmNoButton != null) confirmNoButton.gameObject.SetActive(!isCreditsMode);
        if (confirmCloseButton != null) confirmCloseButton.gameObject.SetActive(isCreditsMode);

        if (isCreditsMode)
        {
            if (confirmCloseButton != null) confirmCloseButton.Select();
        }
        else
        {
            if (confirmYesButton != null) confirmYesButton.Select();
        }
    }

    void OnConfirmYes()
    {
        if (!isConfirmOpen) return;

        isConfirmOpen = false;
        if (confirmPanel != null) confirmPanel.SetActive(false);

        ConfirmAction action = currentConfirmAction;
        currentConfirmAction = ConfirmAction.None;

        switch (action)
        {
            case ConfirmAction.NewGame:
                GameSave.Delete();
                UpdateContinueButtonState();
                StartCoroutine(NewGameTransition());
                break;

            case ConfirmAction.Quit:
                DoQuit();
                break;
        }
    }

    void OnConfirmNo()
    {
        if (!isConfirmOpen) return;
        isConfirmOpen = false;
        currentConfirmAction = ConfirmAction.None;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    void OnConfirmClose()
    {
        if (!isConfirmOpen) return;
        isConfirmOpen = false;
        currentConfirmAction = ConfirmAction.None;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    void DoQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void LoadGameScene()
    {
        Time.timeScale = 1f;
        GameSave.ResetCache();
        SceneManager.LoadScene(gameSceneName);
    }

    IEnumerator NewGameTransition()
    {
        if (musicSource != null)
        {
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }

        if (mainPanel != null) mainPanel.SetActive(false);

        if (fadePanel != null)
        {
            fadePanel.SetActive(true);

            if (fadeCanvasGroup == null)
                fadeCanvasGroup = fadePanel.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
                fadeCanvasGroup = fadePanel.AddComponent<CanvasGroup>();

            fadeCanvasGroup.alpha = 0f;

            float t = 0f;
            while (t < introFadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(t / introFadeInDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            yield return new WaitForSecondsRealtime(introFadeInDuration);
        }

        yield return new WaitForSecondsRealtime(introBlackHoldDuration);

        if (introPanel != null)
        {
            introPanel.SetActive(true);

            if (introCanvasGroup == null)
                introCanvasGroup = introPanel.GetComponent<CanvasGroup>();
            if (introCanvasGroup == null)
                introCanvasGroup = introPanel.AddComponent<CanvasGroup>();
            introCanvasGroup.alpha = 0f;
        }

        if (introExtraObject != null)
            introExtraObject.SetActive(true);

        if (pressAnyKeyText != null) pressAnyKeyText.SetActive(false);

        yield return new WaitForSecondsRealtime(0.05f);

        if (fadePanel != null && fadeCanvasGroup != null)
        {
            float t = 0f;
            while (t < introFadeInRevealDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / introFadeInRevealDuration);
                fadeCanvasGroup.alpha = 1f - k;
                if (introCanvasGroup != null)
                    introCanvasGroup.alpha = k;
                yield return null;
            }

            fadeCanvasGroup.alpha = 0f;
            if (introCanvasGroup != null) introCanvasGroup.alpha = 1f;
            fadePanel.SetActive(false);
        }
        else
        {
            if (introCanvasGroup != null) introCanvasGroup.alpha = 1f;
        }

        isIntroActive = true;
        isTyping = true;

        if (introCoroutine != null) StopCoroutine(introCoroutine);
        introCoroutine = StartCoroutine(TypeIntroText());
    }

    string GetCurrentIntroText()
    {
        return isIntroLanguageRU ? introTextRU : introTextEN;
    }

    IEnumerator TypeIntroText()
    {
        if (introText == null) yield break;

        introText.text = "";
        string fullText = GetCurrentIntroText();

        foreach (char c in fullText)
        {
            if (!isTyping) break;

            introText.text += c;

            if (c == '.' || c == '!' || c == '?' || c == ',' || c == ';' || c == ':')
                yield return new WaitForSeconds(punctuationPause);
            else
                yield return new WaitForSeconds(typeSpeed);
        }

        introText.text = fullText;

        isTyping = false;
        introCoroutine = null;

        if (pressAnyKeyText != null) pressAnyKeyText.SetActive(true);
    }

    void SkipTyping()
    {
        if (!isTyping) return;

        isTyping = false;

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        if (introText != null)
            introText.text = GetCurrentIntroText();

        if (pressAnyKeyText != null) pressAnyKeyText.SetActive(true);
    }

    void StartIntroFadeOut()
    {
        if (!isIntroActive) return;
        isIntroActive = false;

        if (pressAnyKeyText != null) pressAnyKeyText.SetActive(false);

        StartCoroutine(FadeOutAndLoad());
    }

    IEnumerator FadeOutAndLoad()
    {
        if (fadePanel == null)
        {
            LoadGameScene();
            yield break;
        }

        fadePanel.SetActive(true);

        if (fadeCanvasGroup == null)
            fadeCanvasGroup = fadePanel.GetComponent<CanvasGroup>();
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = fadePanel.AddComponent<CanvasGroup>();

        fadeCanvasGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(0.15f);

        LoadGameScene();
    }

    IEnumerator FadeOutMusic()
    {
        if (musicSource == null) yield break;

        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicFadeCoroutine = null;
    }

    void ToggleIntroLanguage()
    {
        if (fadePanel != null && fadePanel.activeSelf) return;

        isIntroLanguageRU = !isIntroLanguageRU;
        UpdateLanguageLabel();

        if (isIntroActive)
        {
            if (introCoroutine != null)
            {
                StopCoroutine(introCoroutine);
                introCoroutine = null;
            }

            if (introText != null) introText.text = "";
            isTyping = true;
            if (pressAnyKeyText != null) pressAnyKeyText.SetActive(false);
            introCoroutine = StartCoroutine(TypeIntroText());
        }
    }

    void UpdateLanguageLabel()
    {
        if (languageToggleLabel != null)
            languageToggleLabel.text = isIntroLanguageRU ? "EN" : "RU";
    }
}