using System.Collections.Generic;
using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    [Header("=== Ќј—“–ќ… » ===")]
    [Tooltip("ќбъекты, которые »—„≈«Ќ”“ (скроетс€ только их 3D-модель, скрипты продолжат работать)")]
    public List<GameObject> objectsToHide = new List<GameObject>();

    [Tooltip("ќбъекты, которые ѕќя¬я“—я")]
    public List<GameObject> objectsToShow = new List<GameObject>();

    [Tooltip("—работает только один раз (если выключено, будет работать при каждом входе)")]
    public bool triggerOnce = true;

    [Header("=== «¬”  (Ќ≈ќЅя«ј“≈Ћ№Ќќ) ===")]
    public AudioClip triggerSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    private bool hasTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // ѕровер€ем, что в триггер вошел именно »грок
        if (!other.CompareTag("Player")) return;

        // ≈сли стоит галочка "—работает один раз" и уже срабатывало Ч выходим
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;

        // 1. — –џ¬ј≈ћ ќЅЏ≈ “џ (отключаем только визуал, чтобы не сломать скрипты)
        foreach (var obj in objectsToHide)
        {
            if (obj == null) continue;

            // ќтключаем все рендереры (модели) внутри этого объекта и его детей
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = false;
            }
        }

        // 2. ѕќ ј«џ¬ј≈ћ ќЅЏ≈ “џ (включаем их)
        foreach (var obj in objectsToShow)
        {
            if (obj != null) obj.SetActive(true);
        }

        // 3. »граем звук
        if (triggerSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(triggerSound, soundVolume);
        }

        Debug.Log($"[TriggerZone] —работал триггер на объекте {gameObject.name}");
    }
}