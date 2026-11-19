using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("=== HP ===")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("=== БОЖЕСТВЕННЫЙ РЕЖИМ ===")]
    public bool godMode = false; // <--- ДОБАВЛЕНО

    [Header("=== UI ЗДОРОВЬЯ ===")]
    public TextMeshProUGUI healthText;

    [Header("=== СЕРДЦЕ ===")]
    public RectTransform heartImage;
    public float maxHeartShake = 20f;
    public float minHeartShake = 2f;
    public float heartShakeFrequency = 25f;
    [Range(0f, 1f)] public float heartShakeThreshold = 0.6f;

    [Header("=== РЕГЕН ===")]
    public float regenDelay = 40f;
    public float regenAmount = 1f;
    public float regenInterval = 1f;
    public float regenMaxHealth = 75f;

    [Header("=== КРАСНЫЙ ЭКРАН ПРИ НИЗКОМ HP ===")]
    public GameObject lowHealthOverlayObject;
    public float lowHealthThreshold = 25f;
    [Range(0f, 1f)] public float lowHealthMaxAlpha = 0.55f;
    public float lowHealthLerpSpeed = 4f;
    public bool lowHealthPulse = true;
    public float lowHealthPulseSpeed = 6f;
    [Range(0f, 0.5f)] public float lowHealthPulseAmount = 0.15f;

    [Header("=== ПОЛУЧЕНИЕ УРОНА ===")]
    public Color damageFlashColor = new Color(1f, 0f, 0f, 1f);
    public AudioClip damageSound;
    [Range(0f, 1f)] public float damageSoundVolume = 1f;

    [Header("=== СМЕРТЬ ===")]
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    public GameObject[] objectsToDisableOnDeath;

    public float deathFallDuration = 1.5f;
    public float deathRollAngle = 75f;
    public float deathDropDistance = 1f;
    public float deathShakeStrength = 0.1f;
    public float deathShakeDuration = 0.5f;

    public float gameOverPanelDelay = 2f;

    [Header("=== ОТКЛЮЧЕНИЕ ФИЗИКИ ===")]
    public bool disablePhysicsOnDeath = true;

    [Header("=== КУРСОР ===")]
    public bool showCursorOnDeath = true;

    [Header("=== БЛОКИРОВКА УПРАВЛЕНИЯ ОРУЖИЕМ ===")]
    public MonoBehaviour weaponSwitcher;
    public List<MonoBehaviour> additionalScriptsToDisable = new List<MonoBehaviour>();

    [Header("=== ФОН СМЕРТИ ===")]
    public GameObject deathScreenObject;

    [Header("=== ПАНЕЛЬ GAME OVER ===")]
    public GameObject gameOverPanel;
    public RectTransform gameOverPanelRect;
    public float gameOverShakeStrength = 25f;
    public float gameOverShakeFrequency = 60f;
    public float gameOverShakeDuration = 0.6f;

    public Button restartButton;
    public Button menuButton;

    [Header("=== СЦЕНА МЕНЮ ===")]
    public string menuSceneName = "MENU";

    [Header("=== ЦИТАТЫ ПОСЛЕ СМЕРТИ ===")]
    [TextArea(2, 4)]
    public string[] deathQuotes = new string[]
    {
        "Ты не первый, кто здесь пал...",
        "Смерть — это лишь начало.",
        "Ошибки делают нас сильнее.",
        "Попробуй ещё раз.",
    };

    public TextMeshProUGUI quoteText;
    public float quoteTypingSpeed = 25f;
    public float quoteStartDelay = 0.3f;
    public bool rememberQuote = false;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    [Header("=== ССЫЛКИ ===")]
    public PlayerController playerController;
    public Camera playerCamera;
    public PauseMenu pauseMenu;

    // === ВНУТРЕННИЕ ===
    private Vector2 heartStartPos;
    private float heartShakeTimer = 0f;
    private bool isDead = false;
    private Transform cameraTransform;
    private Vector3 cameraStartLocalPos;
    private Quaternion cameraStartLocalRot;
    private bool deathAnimDone = false;
    private float deathTimer = 0f;

    private Rigidbody rb;

    private bool showGameOver = false;
    private float gameOverTimer = 0f;
    private bool gameOverShakeDone = false;

    private Vector2 gameOverPanelStartPos;
    private bool gameOverPanelStartPosCaptured = false;

    private float lastDamageTime = -999f;
    private float regenTimer = 0f;

    private string currentQuote = "";
    private float quoteTimer = 0f;
    private int quoteCharIndex = 0;
    private bool quoteDone = false;
    private bool quoteStarted = false;
    private static string lastQuote = "";

    private readonly List<MonoBehaviour> disabledByDeath = new List<MonoBehaviour>();

    private Image lowHealthOverlay;
    private bool lowHealthOverlayActive = false;
    private float currentOverlayAlpha = 0f;

    void Start()
    {
        // ✅ ЗАГРУЗКА HP ИЗ СОХРАНЕНИЯ
        SaveData save = GameSave.Load();
        if (save != null)
        {
            currentHealth = Mathf.Clamp(save.health, 1f, maxHealth);
        }
        else
        {
            currentHealth = maxHealth;
        }

        isDead = false;

        if (healthText != null) UpdateHealthUI();

        if (heartImage != null)
            heartStartPos = heartImage.anchoredPosition;

        rb = GetComponent<Rigidbody>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            cameraStartLocalPos = cameraTransform.localPosition;
            cameraStartLocalRot = cameraTransform.localRotation;
        }

        if (pauseMenu == null)
            pauseMenu = FindObjectOfType<PauseMenu>();

        if (deathScreenObject != null)
            deathScreenObject.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (lowHealthOverlayObject != null)
        {
            lowHealthOverlay = lowHealthOverlayObject.GetComponent<Image>();
            lowHealthOverlayObject.SetActive(false);
            lowHealthOverlayActive = false;

            if (lowHealthOverlay != null)
            {
                Color c = lowHealthOverlay.color;
                c.a = 0f;
                lowHealthOverlay.color = c;
            }
        }

        if (gameOverPanelRect != null)
        {
            gameOverPanelStartPos = gameOverPanelRect.anchoredPosition;
            gameOverPanelStartPosCaptured = true;
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButton);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuButton);

        if (quoteText != null)
            quoteText.text = "";
    }

    void Update()
    {
        if (isDead)
        {
            UpdateDeathAnimation();
            UpdateGameOverPanel();
            UpdateQuoteTyping();
            return;
        }

        UpdateHeartShake();
        UpdateRegen();
        UpdateLowHealthOverlay();
    }

    public void TakeDamage(float amount)
    {
        // <--- ДОБАВЛЕНО: Проверка на бессмертие --->
        if (isDead || godMode) return;
        // <----------------------------------------->

        if (amount <= 0f) return;

        float before = currentHealth;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, Mathf.Round(currentHealth));

        lastDamageTime = Time.time;
        regenTimer = 0f;

        UpdateHealthUI();

        if (currentHealth < before)
        {
            ScreenFlash.Flash(damageFlashColor);

            if (damageSound != null)
                PlaySound2D(damageSound, damageSoundVolume);
        }

        if (debugLogs)
            Debug.Log($"[PlayerHealth] TakeDamage({amount}) → HP: {before} → {currentHealth} / {maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        float before = currentHealth;

        currentHealth += amount;
        currentHealth = Mathf.Min(Mathf.Round(currentHealth), maxHealth);
        if (currentHealth < 0f) currentHealth = 0f;

        UpdateHealthUI();

        if (debugLogs)
            Debug.Log($"[PlayerHealth] Heal({amount}) → HP: {before} → {currentHealth} / {maxHealth}");
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = Mathf.RoundToInt(currentHealth).ToString();
    }

    void UpdateRegen()
    {
        if (currentHealth >= regenMaxHealth) return;

        if (Time.time < lastDamageTime + regenDelay)
        {
            regenTimer = 0f;
            return;
        }

        regenTimer += Time.deltaTime;

        if (regenTimer >= regenInterval)
        {
            regenTimer = 0f;

            currentHealth += regenAmount;
            if (currentHealth > regenMaxHealth)
                currentHealth = regenMaxHealth;

            UpdateHealthUI();
        }
    }

    void UpdateHeartShake()
    {
        if (heartImage == null) return;

        float hpRatio = maxHealth > 0f ? currentHealth / maxHealth : 0f;

        if (hpRatio >= heartShakeThreshold)
        {
            heartImage.anchoredPosition = Vector2.Lerp(
                heartImage.anchoredPosition, heartStartPos, Time.deltaTime * 10f);
            return;
        }

        float t = 1f - (hpRatio / heartShakeThreshold);
        float strength = Mathf.Lerp(minHeartShake, maxHeartShake, t);

        heartShakeTimer += Time.deltaTime * heartShakeFrequency;

        float offsetX = Mathf.Sin(heartShakeTimer) * strength;
        float offsetY = Mathf.Cos(heartShakeTimer * 1.4f) * strength;

        heartImage.anchoredPosition = heartStartPos + new Vector2(offsetX, offsetY);
    }

    void UpdateLowHealthOverlay()
    {
        if (lowHealthOverlayObject == null || lowHealthOverlay == null) return;

        float targetAlpha = 0f;

        if (currentHealth < lowHealthThreshold && lowHealthThreshold > 0f)
        {
            float t = 1f - (currentHealth / lowHealthThreshold);
            targetAlpha = Mathf.Clamp01(t) * lowHealthMaxAlpha;

            if (lowHealthPulse && targetAlpha > 0.01f)
            {
                float pulse = 1f + Mathf.Sin(Time.time * lowHealthPulseSpeed) * lowHealthPulseAmount;
                targetAlpha *= pulse;
                targetAlpha = Mathf.Clamp(targetAlpha, 0f, 1f);
            }
        }

        bool shouldBeActive = targetAlpha > 0.001f || currentOverlayAlpha > 0.001f;

        if (shouldBeActive && !lowHealthOverlayActive)
        {
            lowHealthOverlayObject.SetActive(true);
            lowHealthOverlayActive = true;
        }

        currentOverlayAlpha = Mathf.Lerp(
            currentOverlayAlpha, targetAlpha, Time.deltaTime * lowHealthLerpSpeed);

        if (!shouldBeActive && lowHealthOverlayActive && currentOverlayAlpha <= 0.001f)
        {
            currentOverlayAlpha = 0f;

            Color cc = lowHealthOverlay.color;
            cc.a = 0f;
            lowHealthOverlay.color = cc;

            lowHealthOverlayObject.SetActive(false);
            lowHealthOverlayActive = false;
            return;
        }

        Color c = lowHealthOverlay.color;
        c.a = currentOverlayAlpha;
        lowHealthOverlay.color = c;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathScreenObject != null)
            deathScreenObject.SetActive(true);

        if (deathSound != null)
            PlaySound2D(deathSound, deathSoundVolume);

        if (heartImage != null)
            heartImage.anchoredPosition = heartStartPos;

        if (playerController != null)
            playerController.enabled = false;

        DisableWeaponControls();

        if (disablePhysicsOnDeath && rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (objectsToDisableOnDeath != null)
        {
            for (int i = 0; i < objectsToDisableOnDeath.Length; i++)
            {
                if (objectsToDisableOnDeath[i] != null)
                    objectsToDisableOnDeath[i].SetActive(false);
            }
        }

        if (pauseMenu != null)
            pauseMenu.enabled = false;

        gameObject.tag = "Untagged";

        if (showCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (lowHealthOverlayObject != null)
        {
            currentOverlayAlpha = 0f;

            if (lowHealthOverlay != null)
            {
                Color c = lowHealthOverlay.color;
                c.a = 0f;
                lowHealthOverlay.color = c;
            }

            lowHealthOverlayObject.SetActive(false);
            lowHealthOverlayActive = false;
        }

        deathTimer = 0f;
        deathAnimDone = false;
        showGameOver = false;
        gameOverTimer = 0f;
        gameOverShakeDone = false;
        quoteStarted = false;
    }

    void DisableWeaponControls()
    {
        disabledByDeath.Clear();

        if (weaponSwitcher != null && weaponSwitcher.enabled)
        {
            weaponSwitcher.enabled = false;
            disabledByDeath.Add(weaponSwitcher);
        }

        if (additionalScriptsToDisable != null)
        {
            for (int i = 0; i < additionalScriptsToDisable.Count; i++)
            {
                var mb = additionalScriptsToDisable[i];
                if (mb != null && mb.enabled)
                {
                    mb.enabled = false;
                    disabledByDeath.Add(mb);
                }
            }
        }

        additionalScriptsToDisable.RemoveAll(item => item == null);
    }

    public void RegisterWeaponScript(MonoBehaviour script)
    {
        if (script == null) return;
        if (additionalScriptsToDisable == null)
            additionalScriptsToDisable = new List<MonoBehaviour>();

        if (!additionalScriptsToDisable.Contains(script))
            additionalScriptsToDisable.Add(script);
    }

    public void UnregisterWeaponScript(MonoBehaviour script)
    {
        if (script == null || additionalScriptsToDisable == null) return;
        additionalScriptsToDisable.Remove(script);
    }

    void UpdateDeathAnimation()
    {
        if (cameraTransform == null) return;
        if (deathAnimDone) return;

        deathTimer += Time.deltaTime;
        float t = Mathf.Clamp01(deathTimer / deathFallDuration);

        Vector3 targetPos = cameraStartLocalPos + new Vector3(0f, -deathDropDistance, 0f);
        Quaternion targetRot = cameraStartLocalRot * Quaternion.Euler(0f, 0f, -deathRollAngle);

        float shakeAmount = 0f;
        if (deathTimer < deathShakeDuration)
        {
            float shakeT = 1f - (deathTimer / deathShakeDuration);
            shakeAmount = deathShakeStrength * shakeT;
        }

        Vector3 shakeOffset = new Vector3(
            Random.Range(-shakeAmount, shakeAmount),
            Random.Range(-shakeAmount, shakeAmount),
            0f
        );

        cameraTransform.localPosition = Vector3.Lerp(cameraStartLocalPos, targetPos, t) + shakeOffset;
        cameraTransform.localRotation = Quaternion.Slerp(cameraStartLocalRot, targetRot, t);

        if (t >= 1f)
            deathAnimDone = true;
    }

    void UpdateGameOverPanel()
    {
        if (gameOverPanel == null) return;

        if (!showGameOver)
        {
            gameOverTimer += Time.unscaledDeltaTime;
            if (gameOverTimer >= gameOverPanelDelay)
            {
                showGameOver = true;

                gameOverPanel.SetActive(true);

                if (gameOverPanelRect != null)
                {
                    if (!gameOverPanelStartPosCaptured)
                    {
                        gameOverPanelStartPos = gameOverPanelRect.anchoredPosition;
                        gameOverPanelStartPosCaptured = true;
                    }
                }

                if (!quoteStarted)
                {
                    quoteStarted = true;
                    StartQuote();
                }
            }
            return;
        }

        if (!gameOverShakeDone && gameOverPanelRect != null)
        {
            float shakeTime = gameOverTimer - gameOverPanelDelay;

            if (shakeTime < gameOverShakeDuration)
            {
                float progress = shakeTime / gameOverShakeDuration;
                float falloff = 1f - progress;
                float shakeAmount = gameOverShakeStrength * falloff;

                float phase = Time.unscaledTime * gameOverShakeFrequency;
                float offsetX = Mathf.Sin(phase * 1.7f) * shakeAmount;
                float offsetY = Mathf.Cos(phase * 2.3f) * shakeAmount;

                gameOverPanelRect.anchoredPosition = gameOverPanelStartPos + new Vector2(offsetX, offsetY);
            }
            else
            {
                gameOverPanelRect.anchoredPosition = gameOverPanelStartPos;
                gameOverShakeDone = true;
            }

            gameOverTimer += Time.unscaledDeltaTime;
        }
    }

    void StartQuote()
    {
        if (quoteText == null) return;
        if (deathQuotes == null || deathQuotes.Length == 0)
        {
            quoteText.text = "";
            quoteDone = true;
            return;
        }

        string picked = deathQuotes[Random.Range(0, deathQuotes.Length)];
        if (deathQuotes.Length > 1 && rememberQuote == false && picked == lastQuote)
        {
            int safety = 0;
            while (picked == lastQuote && safety < 20)
            {
                picked = deathQuotes[Random.Range(0, deathQuotes.Length)];
                safety++;
            }
        }

        currentQuote = picked;
        lastQuote = picked;

        quoteTimer = 0f;
        quoteCharIndex = 0;
        quoteDone = false;

        if (quoteTypingSpeed <= 0f)
        {
            quoteText.text = currentQuote;
            quoteDone = true;
        }
        else
        {
            quoteText.text = "";
        }
    }

    void UpdateQuoteTyping()
    {
        if (!quoteStarted) return;
        if (quoteText == null) return;
        if (quoteDone) return;
        if (quoteTypingSpeed <= 0f) return;

        quoteTimer += Time.unscaledDeltaTime;

        if (quoteTimer < quoteStartDelay) return;

        float typingTime = quoteTimer - quoteStartDelay;
        int targetChars = Mathf.FloorToInt(typingTime * quoteTypingSpeed);

        if (targetChars <= quoteCharIndex) return;

        quoteCharIndex = Mathf.Min(targetChars, currentQuote.Length);
        quoteText.text = currentQuote.Substring(0, quoteCharIndex);

        if (quoteCharIndex >= currentQuote.Length)
            quoteDone = true;
    }

    void OnRestartButton()
    {
        Time.timeScale = 1f;
        // ✅ Сброс кэша, чтобы сцена перечитала сохранение
        GameSave.ResetCache();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    void PlaySound2D(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameObject go = new GameObject("PlayerSoundTemp");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }

    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;

    [ContextMenu("Тест: +10 HP")]
    void DebugHeal10()
    {
        Heal(10f);
        Debug.Log($"[PlayerHealth] Тест Heal(10). HP = {currentHealth} / {maxHealth}");
    }

    [ContextMenu("Тест: -10 HP")]
    void DebugDamage10()
    {
        TakeDamage(10f);
        Debug.Log($"[PlayerHealth] Тест TakeDamage(10). HP = {currentHealth} / {maxHealth}");
    }
}