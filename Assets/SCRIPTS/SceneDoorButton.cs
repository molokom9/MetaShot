using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneDoorButton : MonoBehaviour
{
    [Header("=== ПЕРЕХОД НА СЦЕНУ ===")]
    [Tooltip("Имя сцены, которую нужно загрузить (должна быть в Build Settings!)")]
    public string sceneName = "Level2";

    [Tooltip("Задержка перед загрузкой после полного затемнения (сек)")]
    public float delayBeforeLoad = 0.2f;

    [Header("=== ЗАТЕМНЕНИЕ ===")]
    [Tooltip("CanvasGroup чёрной панели. Панель может быть ВЫКЛЮЧЕНА в сцене — скрипт сам её включит.")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("Сколько секунд длится затемнение")]
    public float fadeOutDuration = 3f;

    [Tooltip("Пауза после включения панели, до начала затемнения (сек)")]
    public float delayBeforeFade = 0f;

    [Header("=== КНОПКА — ВИД ===")]
    public Renderer buttonRenderer;
    public Material idleMaterial;
    public Material pressedMaterial;

    [Header("=== ВЗАИМОДЕЙСТВИЕ ===")]
    [Tooltip("Камера игрока. Если пусто — найдёт Camera.main")]
    public Transform cameraTransform;

    [Tooltip("Дистанция, с которой можно нажать кнопку")]
    public float interactDistance = 3f;

    [Tooltip("Слои, по которым проверяем нажатие (кнопка должна быть в них!)")]
    public LayerMask interactLayers = ~0;

    public KeyCode interactKey = KeyCode.E;

    [Tooltip("Разрешить нажатие ПКМ")]
    public bool allowMouseClick = true;

    [Tooltip("СВОЙ TextMeshPro для ЭТОЙ кнопки. НЕ общий!")]
    public GameObject hintObject;

    [Header("=== ЗВУК ===")]
    public AudioClip pressSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    // === ВНУТРЕННИЕ ===
    private bool isTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        if (buttonRenderer != null && idleMaterial != null)
            buttonRenderer.material = idleMaterial;

        if (hintObject != null)
            hintObject.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // ✅ Подготовка затемнения: панель может быть ВЫКЛЮЧЕНА.
        //    Гарантируем alpha=0 и выключаем лучи, но НЕ трогаем SetActive здесь —
        //    включим только когда нажмут кнопку.
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;

            // Если панель была включена в сцене — выключим, чтобы не висела
            if (fadeCanvasGroup.gameObject.activeSelf)
                fadeCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[SceneDoorButton] '{name}' — fadeCanvasGroup не назначен! Затемнения не будет.");
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning($"[SceneDoorButton] '{name}' — нет Collider!");
        else if (!col.enabled)
            Debug.LogWarning($"[SceneDoorButton] '{name}' — Collider выключен!");

        if (string.IsNullOrEmpty(sceneName))
            Debug.LogWarning($"[SceneDoorButton] '{name}' — sceneName пустой!");
    }

    void Update()
    {
        if (isTriggered)
        {
            if (hintObject != null && hintObject.activeSelf)
                hintObject.SetActive(false);
            return;
        }

        if (Time.timeScale <= 0f) return;

        bool lookingAtButton = CanInteract();

        if (hintObject != null && lookingAtButton != hintObject.activeSelf)
            hintObject.SetActive(lookingAtButton);

        if (!lookingAtButton) return;

        bool pressed = Input.GetKeyDown(interactKey);

        if (allowMouseClick && Input.GetMouseButtonDown(1))
            pressed = true;

        if (!pressed) return;

        OnPressed();
    }

    bool CanInteract()
    {
        if (cameraTransform == null) return false;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward,
            out RaycastHit hit, interactDistance, interactLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform) return true;
            if (hit.transform.IsChildOf(transform)) return true;
            if (hit.transform.parent == transform) return true;
        }

        return false;
    }

    void OnPressed()
    {
        if (isTriggered) return;
        isTriggered = true;

        if (debugLogs) Debug.Log($"[SceneDoorButton] '{name}' нажата -> загрузка '{sceneName}'");

        if (buttonRenderer != null && pressedMaterial != null)
            buttonRenderer.material = pressedMaterial;

        if (pressSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(pressSound, soundVolume);
        }

        if (hintObject != null) hintObject.SetActive(false);

        StartCoroutine(FadeAndLoadRoutine());
    }

    IEnumerator FadeAndLoadRoutine()
    {
        // 1) ВКЛЮЧАЕМ панель затемнения (важно: до анимации alpha!)
        if (fadeCanvasGroup != null)
        {
            // Включаем GameObject — иначе CanvasGroup не работает
            if (!fadeCanvasGroup.gameObject.activeSelf)
                fadeCanvasGroup.gameObject.SetActive(true);

            // Сброс альфы и блокировка кликов
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = false;

            if (delayBeforeFade > 0f)
                yield return new WaitForSeconds(delayBeforeFade);

            // 2) Плавно затемняем за fadeOutDuration (по умолчанию 3 сек)
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fadeOutDuration);

            if (debugLogs)
                Debug.Log($"[SceneDoorButton] Затемняю экран за {duration} сек...");

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        if (delayBeforeLoad > 0f)
            yield return new WaitForSeconds(delayBeforeLoad);

        // 3) Грузим сцену
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneDoorButton] sceneName пустой — загрузка отменена");
            yield break;
        }

        if (debugLogs)
            Debug.Log($"[SceneDoorButton] Загружаю сцену '{sceneName}'...");

        SceneManager.LoadScene(sceneName);
    }
}