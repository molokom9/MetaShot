using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class TeleportTrigger : MonoBehaviour
{
    [Header("=== ТЕЛЕПОРТ ===")]
    [Tooltip("Куда телепортировать игрока")]
    public Transform teleportTarget;

    [Tooltip("Тег игрока")]
    public string playerTag = "Player";

    [Tooltip("Только один раз? (иначе можно телепортироваться повторно)")]
    public bool useOnce = true;

    [Header("=== ЭКРАН ЗАТЕМНЕНИЯ ===")]
    [Tooltip("Image на весь экран (чёрный). По дефолту НЕАКТИВЕН!")]
    public Image fadeScreen;

    [Tooltip("Сколько секунд затемняется (сек)")]
    public float fadeOutDuration = 1f;

    [Tooltip("Сколько секунд ждать после телепорта (сек)")]
    public float holdDuration = 0.3f;

    [Tooltip("Сколько секунд высветляется (сек)")]
    public float fadeInDuration = 1f;

    [Header("=== ОТКЛЮЧАЕМЫЕ СКРИПТЫ ===")]
    [Tooltip("Скрипты, которые выключаются во время телепорта и включаются обратно")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    [Header("=== ЗВУК ===")]
    public AudioClip teleportSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private bool used = false;
    private bool isTeleporting = false;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // ✅ Экран по дефолту скрыт
        if (fadeScreen != null)
        {
            Color c = fadeScreen.color;
            c.a = 0f;
            fadeScreen.color = c;
            fadeScreen.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (isTeleporting) return;
        if (Time.timeScale <= 0f) return;
        if (!IsPlayer(other.transform)) return;

        used = true;
        StartCoroutine(TeleportRoutine(other.transform));
    }

    IEnumerator TeleportRoutine(Transform player)
    {
        isTeleporting = true;

        if (debugLogs) Debug.Log($"[TeleportTrigger] Телепорт начался");

        // === 1. Отключаем скрипты ===
        SetScriptsEnabled(false);

        // === 2. Затемняем экран ===
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);

                Color c = fadeScreen.color;
                c.a = t;
                fadeScreen.color = c;

                yield return null;
            }

            Color cf = fadeScreen.color;
            cf.a = 1f;
            fadeScreen.color = cf;
        }

        // === 3. Звук ===
        if (teleportSound != null)
        {
            GameObject go = new GameObject("TeleportSound");
            go.transform.position = player.position;
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = teleportSound;
            src.volume = soundVolume;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.Play();
            Destroy(go, teleportSound.length + 0.1f);
        }

        // === 4. Телепорт ===
        if (teleportTarget != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (cc != null) cc.enabled = false;

            player.position = teleportTarget.position;
            player.rotation = teleportTarget.rotation;

            if (rb != null)
            {
                rb.position = teleportTarget.position;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (cc != null) cc.enabled = true;

            if (debugLogs) Debug.Log($"[TeleportTrigger] Телепорт в {teleportTarget.position}");
        }
        else
        {
            Debug.LogWarning("[TeleportTrigger] teleportTarget не назначен!");
        }

        // === 5. Ждём немного ===
        yield return new WaitForSecondsRealtime(holdDuration);

        // === 6. Высветляем экран ===
        if (fadeScreen != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);

                Color c = fadeScreen.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                fadeScreen.color = c;

                yield return null;
            }

            Color cf = fadeScreen.color;
            cf.a = 0f;
            fadeScreen.color = cf;
            fadeScreen.gameObject.SetActive(false);
        }

        // === 7. Включаем скрипты обратно ===
        SetScriptsEnabled(true);

        isTeleporting = false;

        if (debugLogs) Debug.Log($"[TeleportTrigger] Телепорт завершён");
    }

    void SetScriptsEnabled(bool state)
    {
        if (scriptsToDisable == null) return;

        for (int i = 0; i < scriptsToDisable.Count; i++)
        {
            MonoBehaviour mb = scriptsToDisable[i];
            if (mb != null)
                mb.enabled = state;
        }
    }

    bool IsPlayer(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag(playerTag)) return true;
            t = t.parent;
        }
        return false;
    }
}