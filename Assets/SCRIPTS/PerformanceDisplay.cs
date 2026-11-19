using UnityEngine;
using TMPro;

public class PerformanceDisplay : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Показывать FPS при запуске игры")]
    public bool showByDefault = true;

    private TextMeshProUGUI statsText;
    private float deltaTime = 0.0f;
    private float updateTimer = 0.0f;

    void Start()
    {
        statsText = GetComponent<TextMeshProUGUI>();

        // При первом запуске игры включаем по умолчанию
        if (!PlayerPrefs.HasKey("ShowPerformanceStats"))
        {
            PlayerPrefs.SetInt("ShowPerformanceStats", showByDefault ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        bool isEnabled = PlayerPrefs.GetInt("ShowPerformanceStats", 1) == 1;

        if (statsText != null)
        {
            if (statsText.gameObject.activeSelf != isEnabled)
                statsText.gameObject.SetActive(isEnabled);
        }

        if (!isEnabled) return;

        // Считаем FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        updateTimer -= Time.unscaledDeltaTime;
        if (updateTimer <= 0f)
        {
            updateTimer = 0.25f; // Обновляем 4 раза в секунду

            float fps = 1.0f / deltaTime;
            float ms = deltaTime * 1000.0f;

            // Память
            long csharpMemory = System.GC.GetTotalMemory(false) / (1024 * 1024);
            long totalRam = SystemInfo.systemMemorySize;

            statsText.text = $"<b>FPS:</b> {Mathf.Ceil(fps)} ({ms:F1} ms)\n" +
                             $"<b>RAM (C#):</b> {csharpMemory} MB\n" +
                             $"<b>PC RAM:</b> {totalRam} MB";
        }
    }
}