using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


public class PauseMenu : MonoBehaviour
{
    [Header("=== ПАНЕЛИ ===")]
    public GameObject menuPanel;
    public GameObject settingsPanel;

    [Header("=== КНОПКИ ===")]
    public Button continueButton;
    public Button settingsButton;
    public Button exitButton;
    public Button settingsBackButton;

    [Header("=== ИГРОК ===")]
    public PlayerController playerController;

    [Header("=== ОБЪЕКТЫ ДЛЯ ОТКЛЮЧЕНИЯ ===")]
    public GameObject[] objectsToDisable;

    [Header("=== СЦЕНА ===")]
    public string mainMenuSceneName = "MainMenu";

    [Header("=== НАСТРОЙКИ ===")]
    public bool pauseGame = true;
    public KeyCode menuKey = KeyCode.Escape;

    [Header("=== ТРЯСКА ===")]
    public bool enableShake = true;
    public float shakeDuration = 0.3f;
    public float shakeStrength = 8f;
    public float shakeFrequency = 30f;

    [Header("=== UI-НАВИГАЦИЯ ГЕЙМПАДОМ ===")]
    [Tooltip("Как часто (сек) можно переключать пункты стиком/крестовиной")]
    public float uiNavigateRepeatDelay = 0.25f;

    private bool isMenuOpen = false;
    private bool isSettingsOpen = false;

    private RectTransform shakeTarget;
    private Vector2 shakeOrigin;
    private float shakeTimer = 0f;

    private List<GameObject> activeObjects = new List<GameObject>();

    // UI-навигация
    private List<Selectable> currentSelectables = new List<Selectable>();
    private int currentSelectableIndex = 0;
    private float uiNavTimer = 0f;

    void Start()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueButton);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButton);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitToMenuButton);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnSettingsBackButton);

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        isMenuOpen = false;
        isSettingsOpen = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pauseGame) Time.timeScale = 1f;
    }

    void Update()
    {
        // === Пауза с клавиатуры или Start на геймпаде ===
        if (Input.GetKeyDown(menuKey)) HandleEscape();
        if (GamepadInput.PadAvailable && GamepadInput.Start_Pressed) HandleEscape();

        // === UI-навигация геймпадом (работает только когда меню открыто) ===
        if (isMenuOpen && GamepadInput.PadAvailable)
            HandleGamepadUINavigation();

        UpdateShake();
    }

    void HandleGamepadUINavigation()
    {
        if (Time.unscaledDeltaTime <= 0f) return;

        // Навигация
        Vector2 nav = GamepadInput.UINavigate;
        if (nav.magnitude > 0.5f)
        {
            uiNavTimer -= Time.unscaledDeltaTime;
            if (uiNavTimer <= 0f)
            {
                uiNavTimer = uiNavigateRepeatDelay;
                MoveSelection(nav.y > 0.5f ? -1 : (nav.y < -0.5f ? 1 : 0));
            }
        }
        else uiNavTimer = 0f;

        // Крестовина — отдельно (быстрее)
        if (GamepadInput.DPadDown_Pressed) MoveSelection(1);
        if (GamepadInput.DPadUp_Pressed) MoveSelection(-1);

        // A — подтвердить
        if (GamepadInput.UISubmit && EventSystem.current != null)
        {
            GameObject sel = EventSystem.current.currentSelectedGameObject;
            if (sel != null)
            {
                Button btn = sel.GetComponent<Button>();
                if (btn != null && btn.interactable) btn.onClick.Invoke();
            }
        }

        // B — назад
        if (GamepadInput.UICancel)
        {
            if (isSettingsOpen) CloseSettings();
            else if (isMenuOpen) CloseMenu();
        }
    }

    void MoveSelection(int dir)
    {
        if (dir == 0 || currentSelectables.Count == 0) return;
        if (EventSystem.current == null) return;

        currentSelectableIndex = (currentSelectableIndex + dir + currentSelectables.Count) % currentSelectables.Count;
        EventSystem.current.SetSelectedGameObject(currentSelectables[currentSelectableIndex].gameObject);
    }

    void RebuildSelectables(GameObject panel)
    {
        currentSelectables.Clear();
        currentSelectableIndex = 0;
        if (panel == null) return;

        Selectable[] all = panel.GetComponentsInChildren<Selectable>(true);
        foreach (var s in all)
            if (s != null && s.interactable && s.gameObject.activeInHierarchy)
                currentSelectables.Add(s);

        if (currentSelectables.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(currentSelectables[0].gameObject);
    }

    void HandleEscape()
    {
        if (isSettingsOpen) { CloseSettings(); return; }
        if (isMenuOpen) { CloseMenu(); return; }
        OpenMenu();
    }

    public void OpenMenu()
    {
        isMenuOpen = true;

        if (menuPanel != null) menuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        isSettingsOpen = false;

        if (pauseGame) Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayerEnabled(false);
        DisableObjects();

        RebuildSelectables(menuPanel);

        if (enableShake && menuPanel != null)
            StartShake(menuPanel.GetComponent<RectTransform>());
    }

    public void CloseMenu()
    {
        isMenuOpen = false;

        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        isSettingsOpen = false;

        StopShake();

        if (pauseGame) Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetPlayerEnabled(true);
        EnableObjects();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OpenSettings()
    {
        isSettingsOpen = true;

        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        RebuildSelectables(settingsPanel);

        if (enableShake && settingsPanel != null)
            StartShake(settingsPanel.GetComponent<RectTransform>());
    }

    public void CloseSettings()
    {
        isSettingsOpen = false;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);

        RebuildSelectables(menuPanel);

        if (enableShake && menuPanel != null)
            StartShake(menuPanel.GetComponent<RectTransform>());
    }

    void SetPlayerEnabled(bool state)
    {
        if (playerController != null) playerController.enabled = state;
    }

    void DisableObjects()
    {
        activeObjects.Clear();
        if (objectsToDisable == null) return;

        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            GameObject go = objectsToDisable[i];
            if (go == null) continue;
            if (go.activeSelf) { activeObjects.Add(go); go.SetActive(false); }
        }
    }

    void EnableObjects()
    {
        for (int i = 0; i < activeObjects.Count; i++)
            if (activeObjects[i] != null) activeObjects[i].SetActive(true);
        activeObjects.Clear();
    }

    void StartShake(RectTransform target)
    {
        if (target == null) return;
        shakeTarget = target;
        shakeOrigin = target.anchoredPosition;
        shakeTimer = shakeDuration;
    }

    void StopShake()
    {
        if (shakeTarget != null) shakeTarget.anchoredPosition = shakeOrigin;
        shakeTarget = null;
        shakeTimer = 0f;
    }

    void UpdateShake()
    {
        if (shakeTarget == null || shakeTimer <= 0f) return;
        shakeTimer -= Time.unscaledDeltaTime;
        if (shakeTimer <= 0f)
        {
            shakeTarget.anchoredPosition = shakeOrigin;
            shakeTarget = null;
            return;
        }
        float t = shakeTimer / shakeDuration;
        float strength = shakeStrength * t;
        float offsetX = Mathf.Sin(Time.unscaledTime * shakeFrequency) * strength;
        float offsetY = Mathf.Cos(Time.unscaledTime * shakeFrequency * 1.3f) * strength;
        shakeTarget.anchoredPosition = shakeOrigin + new Vector2(offsetX, offsetY);
    }

    void OnContinueButton() { CloseMenu(); }
    void OnSettingsButton() { OpenSettings(); }
    void OnSettingsBackButton() { CloseSettings(); }

    void OnExitToMenuButton()
    {
        if (pauseGame) Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}