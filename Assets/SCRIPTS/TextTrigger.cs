using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class TextTrigger : MonoBehaviour
{
    [Header("=== UI И ТЕКСТ ===")]
    [Tooltip("Перетащи сюда ваш TextMeshProUGUI из Canvas")]
    public TextMeshProUGUI uiText;

    [TextArea(3, 6)]
    [Tooltip("Текст, который будет выводиться")]
    public string message = "Текст сообщения...";

    [Header("=== АНИМАЦИЯ И ТАЙМИНГИ ===")]
    [Tooltip("Задержка между появлением букв (сек)")]
    public float letterDelay = 0.04f;

    [Tooltip("Сколько секунд текст держится на экране после печати")]
    public float displayDuration = 3f;

    [Tooltip("Длительность плавного исчезновения (сек)")]
    public float fadeOutDuration = 1f;

    [Header("=== НАСТРОЙКИ ТРИГГЕРА ===")]
    public string playerTag = "Player";

    [Tooltip("Срабатывать только один раз")]
    public bool triggerOnce = true;

    private bool isUsed = false;
    private static Coroutine currentTextCoroutine;

    private void Awake()
    {
        if (TryGetComponent<Collider>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && isUsed) return;
        if (!IsPlayer(other.transform)) return;

        if (uiText == null)
        {
            Debug.LogError($"[TextTrigger] На '{gameObject.name}' не назначен TextMeshProUGUI!");
            return;
        }

        isUsed = true;

        // Если на экране уже печатался другой текст — сбрасываем его
        if (currentTextCoroutine != null)
        {
            StopCoroutine(currentTextCoroutine);
        }

        currentTextCoroutine = StartCoroutine(ShowTypewriterText());
    }

    private IEnumerator ShowTypewriterText()
    {
        uiText.gameObject.SetActive(true);
        uiText.alpha = 1f;
        uiText.text = "";

        // Побуквенный вывод
        for (int i = 0; i < message.Length; i++)
        {
            uiText.text += message[i];
            yield return new WaitForSeconds(letterDelay);
        }

        // Пауза перед исчезновением
        yield return new WaitForSeconds(displayDuration);

        // Плавный Fade Out
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            uiText.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        uiText.text = "";
        uiText.alpha = 1f;
        uiText.gameObject.SetActive(false);
        currentTextCoroutine = null;
    }

    private bool IsPlayer(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag(playerTag)) return true;
            t = t.parent;
        }
        return false;
    }
}