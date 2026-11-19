using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerTrigger : MonoBehaviour
{
    [Header("=== ВРАГИ ===")]
    [Tooltip("Перетащи сюда выключенных врагов. При входе в триггер они активируются")]
    public GameObject[] enemiesToActivate;

    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Тег игрока, который активирует триггер")]
    public string playerTag = "Player";

    [Tooltip("Удалить триггер после срабатывания")]
    public bool destroyAfterTrigger = true;

    [Tooltip("Одноразовый триггер (не срабатывает повторно)")]
    public bool oneTime = true;

    [Header("=== ЭФФЕКТЫ (опционально) ===")]
    public AudioClip spawnSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    public bool flashOnSpawn = false;
    public Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);

    // === ВНУТРЕННИЕ ===
    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (Time.timeScale <= 0f) return;
        if (triggered && oneTime) return;
        if (!IsPlayer(other.transform)) return;

        TriggerEnemies();
    }

    void TriggerEnemies()
    {
        triggered = true;

        // включаем всех врагов
        if (enemiesToActivate != null)
        {
            for (int i = 0; i < enemiesToActivate.Length; i++)
            {
                if (enemiesToActivate[i] != null)
                    enemiesToActivate[i].SetActive(true);
            }
        }

        // звук
        if (spawnSound != null)
            PlaySound2D(spawnSound, soundVolume);

        // вспышка
        if (flashOnSpawn)
            ScreenFlash.Flash(flashColor);

        // удаляем триггер
        if (destroyAfterTrigger)
            Destroy(gameObject);
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

    void PlaySound2D(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameObject go = new GameObject("SpawnSoundTemp");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }
}