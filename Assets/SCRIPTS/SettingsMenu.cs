using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [System.Serializable]
    public class ObjectToggleSetting
    {
        public string toggleId = "NewToggle";
        public Toggle toggleUI;
        public List<GameObject> targetObjects = new List<GameObject>();
        [Tooltip("Состояние при ПЕРВОМ запуске (если нет сохранения)")]
        public bool defaultState = true;
    }

    [Header("=== СЛАЙДЕРЫ ===")]
    public Slider sensitivitySlider;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider brightnessSlider;
    public Slider fpsSlider;
    public Slider renderDistanceSlider;

    [Header("=== ТЕКСТЫ ===")]
    public TextMeshProUGUI sensitivityText;
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI musicVolumeText;
    public TextMeshProUGUI brightnessText;
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI renderDistanceText;

    [Header("=== ЯРКОСТЬ ===")]
    public Image brightnessOverlay;

    [Header("=== СБРОС ===")]
    public Button resetButton;

    [Header("=== FPS ===")]
    public int[] fpsOptions = { 30, 60, 120, 144, 240, -1 };

    [Header("=== ТРИГГЕРЫ МУЗЫКИ ===")]
    public MusicTrigger[] musicTriggers;

    [Header("=== ГРАНИЦЫ ===")]
    public float sensitivityMin = 0.5f;
    public float sensitivityMax = 10f;
    public float renderDistanceMin = 50f;
    public float renderDistanceMax = 1000f;

    [Header("=== КАЧЕСТВО ===")]
    public TMP_Dropdown qualityDropdownTMP;
    public bool syncDropdownOnEnable = true;

    [Header("=== РАЗРЕШЕНИЕ ЭКРАНА ===")]
    public TMP_Dropdown resolutionDropdown;

    [Header("=== ГАЛОЧКИ ===")]
    public List<ObjectToggleSetting> objectToggles = new List<ObjectToggleSetting>();

    [Header("=== ПОКАЗАТЕЛИ ПК (FPS) ===")]
    public Toggle showStatsToggle;
    public bool defaultShowStats = false;

    private const float DEFAULT_SENSITIVITY = 2f;
    private const float DEFAULT_MASTER_VOLUME = 0.7f;
    private const float DEFAULT_MUSIC_VOLUME = 0.7f;
    private const float DEFAULT_BRIGHTNESS = 0f;
    private const int DEFAULT_FPS_INDEX = 5; // Unlimited (-1)
    private const float DEFAULT_RENDER_DISTANCE = 100f;
    private const string QUALITY_PREFS_KEY = "ConsoleSave_QualityLevel";
    private const int DEFAULT_QUALITY_INDEX = 4;
    private const string STATS_PREFS_KEY = "ShowPerformanceStats";

    private int currentQualityIndex = DEFAULT_QUALITY_INDEX;
    private bool isSyncingDropdown = false;
    private bool initialized = false;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions = new List<Resolution>();
    private int nativeResolutionIndex = 0;

    void Awake()
    {
        CreateBrightnessOverlayIfNeeded();
        SetupQualityDropdowns();
        SetupResolutionDropdowns();
    }

    void OnEnable()
    {
        if (!initialized)
        {
            InitSliders();
            InitToggles();
            InitStatsToggle();
            initialized = true;
        }

        LoadSettings();
        LoadAndApplyQuality();
        LoadToggles();
        LoadStatsToggle();
        LoadResolutionSettings();
    }

    // ==========================================
    // === РАЗРЕШЕНИЕ ЭКРАНА ===
    // ==========================================

    void SetupResolutionDropdowns()
    {
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            filteredResolutions.Clear();

            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResIndex = 0;
            int maxWidth = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                if (!options.Contains(option))
                {
                    options.Add(option);
                    filteredResolutions.Add(resolutions[i]);

                    if (resolutions[i].width > maxWidth)
                    {
                        maxWidth = resolutions[i].width;
                        nativeResolutionIndex = options.Count - 1;
                    }

                    if (resolutions[i].width == Screen.currentResolution.width &&
                        resolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResIndex = options.Count - 1;
                    }
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
    }

    void LoadResolutionSettings()
    {
        if (resolutionDropdown != null && filteredResolutions.Count > 0)
        {
            int savedWidth = PlayerPrefs.GetInt("Res_Width", Screen.currentResolution.width);
            int savedHeight = PlayerPrefs.GetInt("Res_Height", Screen.currentResolution.height);

            int index = filteredResolutions.FindIndex(r => r.width == savedWidth && r.height == savedHeight);
            if (index >= 0)
            {
                resolutionDropdown.SetValueWithoutNotify(index);
                resolutionDropdown.RefreshShownValue();
            }
        }
    }

    void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= filteredResolutions.Count) return;

        Resolution res = filteredResolutions[index];

        if (ResolutionManager.Instance != null)
            ResolutionManager.Instance.SetResolution(res.width, res.height);
        else
            Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
    }

    // ==========================================
    // === ПОКАЗАТЕЛИ ПК (FPS) ===
    // ==========================================

    [ContextMenu("Сбросить галочки (тест)")]
    public void ResetAllTogglePrefs()
    {
        PlayerPrefs.DeleteKey(STATS_PREFS_KEY);
        foreach (var setting in objectToggles)
        {
            PlayerPrefs.DeleteKey("Toggle_" + setting.toggleId);
        }
        PlayerPrefs.Save();
    }

    void InitStatsToggle()
    {
        if (showStatsToggle != null)
        {
            showStatsToggle.onValueChanged.RemoveAllListeners();
            showStatsToggle.onValueChanged.AddListener(OnStatsToggleChanged);
        }
    }

    void LoadStatsToggle()
    {
        if (showStatsToggle != null)
        {
            bool isEnabled = PlayerPrefs.GetInt(STATS_PREFS_KEY, defaultShowStats ? 1 : 0) == 1;
            showStatsToggle.SetIsOnWithoutNotify(isEnabled);
        }
    }

    void OnStatsToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(STATS_PREFS_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ==========================================
    // === ГАЛОЧКИ ===
    // ==========================================

    void InitToggles()
    {
        foreach (var setting in objectToggles)
        {
            if (setting.toggleUI != null)
            {
                setting.toggleUI.onValueChanged.RemoveAllListeners();
                setting.toggleUI.onValueChanged.AddListener((isOn) => OnToggleChanged(setting, isOn));
            }
        }
    }

    void LoadToggles()
    {
        foreach (var setting in objectToggles)
        {
            if (setting.toggleUI != null)
            {
                string key = "Toggle_" + setting.toggleId;
                bool savedState = PlayerPrefs.HasKey(key)
                    ? (PlayerPrefs.GetInt(key) == 1)
                    : setting.defaultState;

                setting.toggleUI.SetIsOnWithoutNotify(savedState);
                ApplyToggleState(setting, savedState);
            }
        }
    }

    void OnToggleChanged(ObjectToggleSetting setting, bool isOn)
    {
        ApplyToggleState(setting, isOn);
        string key = "Toggle_" + setting.toggleId;
        PlayerPrefs.SetInt(key, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyToggleState(ObjectToggleSetting setting, bool isOn)
    {
        if (setting.targetObjects == null) return;
        foreach (var obj in setting.targetObjects)
            if (obj != null) obj.SetActive(isOn);
    }

    // ==========================================
    // === ИНИЦИАЛИЗАЦИЯ ===
    // ==========================================

    void InitSliders()
    {
        if (sensitivitySlider != null) { sensitivitySlider.minValue = sensitivityMin; sensitivitySlider.maxValue = sensitivityMax; }
        if (masterVolumeSlider != null) { masterVolumeSlider.minValue = 0f; masterVolumeSlider.maxValue = 1f; }
        if (musicVolumeSlider != null) { musicVolumeSlider.minValue = 0f; musicVolumeSlider.maxValue = 1f; }
        if (brightnessSlider != null) { brightnessSlider.minValue = -1f; brightnessSlider.maxValue = 1f; }
        if (fpsSlider != null) { fpsSlider.minValue = 0; fpsSlider.maxValue = fpsOptions.Length - 1; fpsSlider.wholeNumbers = true; }
        if (renderDistanceSlider != null) { renderDistanceSlider.minValue = renderDistanceMin; renderDistanceSlider.maxValue = renderDistanceMax; renderDistanceSlider.wholeNumbers = true; }

        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        if (fpsSlider != null) fpsSlider.onValueChanged.AddListener(OnFPSChanged);
        if (renderDistanceSlider != null) renderDistanceSlider.onValueChanged.AddListener(OnRenderDistanceChanged);

        if (resetButton != null) resetButton.onClick.AddListener(OnResetButton);
    }

    // ==========================================
    // === КАЧЕСТВО ===
    // ==========================================

    void SetupQualityDropdowns()
    {
        List<string> qualityNames = new List<string> { "VERY LOW", "LOW", "MEDIUM", "HIGH", "ULTRA" };

        if (qualityDropdownTMP != null)
        {
            qualityDropdownTMP.ClearOptions();
            qualityDropdownTMP.AddOptions(qualityNames);
            qualityDropdownTMP.onValueChanged.RemoveAllListeners();
            qualityDropdownTMP.onValueChanged.AddListener(OnTMPQualityChanged);
        }
    }

    void OnTMPQualityChanged(int index)
    {
        if (isSyncingDropdown) return;
        ApplyQuality(index, true);
        SyncDropdowns(index);
    }

    void SyncDropdowns(int index)
    {
        isSyncingDropdown = true;
        if (qualityDropdownTMP != null) { qualityDropdownTMP.value = index; qualityDropdownTMP.RefreshShownValue(); }
        isSyncingDropdown = false;
    }

    void LoadAndApplyQuality()
    {
        int savedLevel = PlayerPrefs.HasKey(QUALITY_PREFS_KEY)
            ? PlayerPrefs.GetInt(QUALITY_PREFS_KEY)
            : DEFAULT_QUALITY_INDEX;

        savedLevel = Mathf.Clamp(savedLevel, 0, 4);
        currentQualityIndex = savedLevel;
        ApplyQualityToUnity(savedLevel);
        if (syncDropdownOnEnable) SyncDropdowns(savedLevel);
    }

    void ApplyQuality(int level, bool save)
    {
        level = Mathf.Clamp(level, 0, 4);
        currentQualityIndex = level;
        ApplyQualityToUnity(level);
        if (save) { PlayerPrefs.SetInt(QUALITY_PREFS_KEY, level); PlayerPrefs.Save(); }
    }

    void ApplyQualityToUnity(int level)
    {
        if (QualitySettings.names == null || QualitySettings.names.Length == 0) return;
        int unityLevel = MapToUnityQualityLevel(level);
        unityLevel = Mathf.Clamp(unityLevel, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(unityLevel, true);
        ApplyQualitySettings(level);
    }

    void ApplyQualitySettings(int level)
    {
        switch (level)
        {
            case 4:
                QualitySettings.antiAliasing = 4;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = 150f; QualitySettings.shadowCascades = 4;
                QualitySettings.softParticles = true; QualitySettings.softVegetation = true;
                QualitySettings.realtimeReflectionProbes = true; QualitySettings.billboardsFaceCameraPosition = true;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.lodBias = 2.0f; QualitySettings.skinWeights = SkinWeights.FourBones;
                QualitySettings.pixelLightCount = 8; QualitySettings.particleRaycastBudget = 4096;
                RenderSettings.fog = true;
                break;
            case 3:
                QualitySettings.antiAliasing = 2;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.shadowDistance = 100f; QualitySettings.shadowCascades = 2;
                QualitySettings.softParticles = true; QualitySettings.softVegetation = true;
                QualitySettings.realtimeReflectionProbes = true; QualitySettings.billboardsFaceCameraPosition = true;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.lodBias = 1.5f; QualitySettings.skinWeights = SkinWeights.FourBones;
                QualitySettings.pixelLightCount = 6; QualitySettings.particleRaycastBudget = 2048;
                RenderSettings.fog = true;
                break;
            case 2:
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowDistance = 60f; QualitySettings.shadowCascades = 1;
                QualitySettings.softParticles = false; QualitySettings.softVegetation = false;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.lodBias = 1.0f; QualitySettings.skinWeights = SkinWeights.TwoBones;
                QualitySettings.pixelLightCount = 4; QualitySettings.particleRaycastBudget = 1024;
                RenderSettings.fog = true;
                break;
            case 1:
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = 30f; QualitySettings.shadowCascades = 0;
                QualitySettings.softParticles = false; QualitySettings.softVegetation = false;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.lodBias = 0.7f; QualitySettings.skinWeights = SkinWeights.TwoBones;
                QualitySettings.pixelLightCount = 2; QualitySettings.particleRaycastBudget = 256;
                RenderSettings.fog = false;
                break;
            case 0:
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = 0f; QualitySettings.shadowCascades = 0;
                QualitySettings.softParticles = false; QualitySettings.softVegetation = false;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.lodBias = 0.5f; QualitySettings.skinWeights = SkinWeights.OneBone;
                QualitySettings.pixelLightCount = 1; QualitySettings.particleRaycastBudget = 64;
                RenderSettings.fog = false;
                break;
        }

        // При смене качества мы НЕ трогаем V-Sync и FPS, 
        // чтобы не сбить настройки, которые выбрал игрок.
        ApplyFPSAndVSync();
    }

    int MapToUnityQualityLevel(int level)
    {
        int max = QualitySettings.names.Length - 1;
        if (max <= 0) return 0;
        if (max == 4) return level;
        float ratio = (float)level / 4f;
        return Mathf.RoundToInt(ratio * max);
    }

    // ==========================================
    // === ОСТАЛЬНОЕ ===
    // ==========================================

    void CreateBrightnessOverlayIfNeeded()
    {
        if (brightnessOverlay != null) return;

        GameObject canvasGO = new GameObject("BrightnessCanvas (auto)");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        GameObject imgGO = new GameObject("BrightnessOverlay (auto)");
        imgGO.transform.SetParent(canvasGO.transform, false);

        Image img = imgGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        brightnessOverlay = img;
    }

    void LoadSettings()
    {
        if (sensitivitySlider != null) sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", DEFAULT_SENSITIVITY);
        if (masterVolumeSlider != null) masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", DEFAULT_MASTER_VOLUME);
        if (musicVolumeSlider != null) musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", DEFAULT_MUSIC_VOLUME);
        if (brightnessSlider != null) brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", DEFAULT_BRIGHTNESS);
        if (fpsSlider != null) fpsSlider.value = PlayerPrefs.GetInt("FPSIndex", DEFAULT_FPS_INDEX);
        if (renderDistanceSlider != null) renderDistanceSlider.value = PlayerPrefs.GetFloat("RenderDistance", DEFAULT_RENDER_DISTANCE);
        RefreshAllTexts();
    }

    void RefreshAllTexts()
    {
        if (sensitivitySlider != null) OnSensitivityChanged(sensitivitySlider.value);
        if (masterVolumeSlider != null) OnMasterVolumeChanged(masterVolumeSlider.value);
        if (musicVolumeSlider != null) OnMusicVolumeChanged(musicVolumeSlider.value);
        if (brightnessSlider != null) OnBrightnessChanged(brightnessSlider.value);
        if (fpsSlider != null) OnFPSChanged(fpsSlider.value);
        if (renderDistanceSlider != null) OnRenderDistanceChanged(renderDistanceSlider.value);
    }

    public void OnResetButton()
    {
        PlayerPrefs.DeleteKey("Sensitivity");
        PlayerPrefs.DeleteKey("MasterVolume");
        PlayerPrefs.DeleteKey("MusicVolume");
        PlayerPrefs.DeleteKey("Brightness");
        PlayerPrefs.DeleteKey("FPSIndex");
        PlayerPrefs.DeleteKey("RenderDistance");
        PlayerPrefs.DeleteKey(QUALITY_PREFS_KEY);
        PlayerPrefs.DeleteKey(STATS_PREFS_KEY);
        PlayerPrefs.DeleteKey("Res_Width");
        PlayerPrefs.DeleteKey("Res_Height");

        foreach (var setting in objectToggles)
        {
            PlayerPrefs.DeleteKey("Toggle_" + setting.toggleId);
            if (setting.toggleUI != null)
            {
                setting.toggleUI.SetIsOnWithoutNotify(setting.defaultState);
                ApplyToggleState(setting, setting.defaultState);
            }
        }

        if (showStatsToggle != null) showStatsToggle.SetIsOnWithoutNotify(defaultShowStats);
        PlayerPrefs.Save();

        if (sensitivitySlider != null) sensitivitySlider.value = DEFAULT_SENSITIVITY;
        if (masterVolumeSlider != null) masterVolumeSlider.value = DEFAULT_MASTER_VOLUME;
        if (musicVolumeSlider != null) musicVolumeSlider.value = DEFAULT_MUSIC_VOLUME;
        if (brightnessSlider != null) brightnessSlider.value = DEFAULT_BRIGHTNESS;
        if (fpsSlider != null) fpsSlider.value = DEFAULT_FPS_INDEX;
        if (renderDistanceSlider != null) renderDistanceSlider.value = DEFAULT_RENDER_DISTANCE;

        ApplyQuality(DEFAULT_QUALITY_INDEX, true);
        SyncDropdowns(DEFAULT_QUALITY_INDEX);
        RefreshAllTexts();

        if (resolutionDropdown != null && filteredResolutions.Count > 0)
        {
            int defIndex = filteredResolutions.FindIndex(r => r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height);
            if (defIndex < 0) defIndex = nativeResolutionIndex;

            resolutionDropdown.SetValueWithoutNotify(defIndex);

            Resolution defRes = filteredResolutions[defIndex];
            if (Screen.width != defRes.width || Screen.height != defRes.height)
            {
                OnResolutionChanged(defIndex);
            }
        }
    }

    void OnSensitivityChanged(float v)
    {
        PlayerPrefs.SetFloat("Sensitivity", v);
        if (sensitivityText != null) sensitivityText.text = v.ToString("0.0");
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            pc.mouseSensitivity = v;
            pc.gamepadLookSensitivity = v * 75f;
        }
    }

    void OnMasterVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat("MasterVolume", v);
        AudioListener.volume = v;
        if (masterVolumeText != null) masterVolumeText.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    void OnMusicVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat("MusicVolume", v);
        if (musicVolumeText != null) musicVolumeText.text = Mathf.RoundToInt(v * 100f) + "%";
        ApplyMusicVolume(v);
    }

    void ApplyMusicVolume(float v)
    {
        if (musicTriggers == null) return;
        for (int i = 0; i < musicTriggers.Length; i++)
        {
            MusicTrigger mt = musicTriggers[i];
            if (mt == null) continue;
            mt.ApplyMusicVolume(v);
        }
    }

    void OnBrightnessChanged(float v)
    {
        PlayerPrefs.SetFloat("Brightness", v);
        if (brightnessText != null) brightnessText.text = Mathf.RoundToInt(v * 100f) + "%";
        if (brightnessOverlay != null)
        {
            if (v < 0f) brightnessOverlay.color = new Color(0f, 0f, 0f, Mathf.Abs(v) * 0.8f);
            else brightnessOverlay.color = new Color(1f, 1f, 1f, v * 0.5f);
        }
    }

    // === ФИКС FPS ===
    void OnFPSChanged(float v)
    {
        int idx = Mathf.Clamp(Mathf.RoundToInt(v), 0, fpsOptions.Length - 1);
        PlayerPrefs.SetInt("FPSIndex", idx);
        ApplyFPSAndVSync();
    }

    void ApplyFPSAndVSync()
    {
        int idx = Mathf.Clamp(PlayerPrefs.GetInt("FPSIndex", DEFAULT_FPS_INDEX), 0, fpsOptions.Length - 1);
        int fps = fpsOptions[idx];

        Application.targetFrameRate = fps;

        // Если выбрано "Unlimited", V-Sync обязан быть выключен, иначе он будет резать FPS по герцовке монитора.
        // Если выбрано конкретное число, V-Sync тоже выключаем, чтобы он не перебивал наш лимит.
        QualitySettings.vSyncCount = 0;

        if (fpsText != null)
            fpsText.text = fps > 0 ? fps.ToString() : "Unlimited";
    }

    void OnRenderDistanceChanged(float v)
    {
        PlayerPrefs.SetFloat("RenderDistance", v);
        if (renderDistanceText != null) renderDistanceText.text = Mathf.RoundToInt(v).ToString() + "m";
        Camera mainCam = Camera.main;
        if (mainCam != null) mainCam.farClipPlane = v;
    }

    public void RefreshMusicVolume()
    {
        float v = PlayerPrefs.GetFloat("MusicVolume", DEFAULT_MUSIC_VOLUME);
        ApplyMusicVolume(v);
    }
}