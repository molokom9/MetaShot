using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class SavePoint : MonoBehaviour
{
    [Header("=== ID ЧЕКПОИНТА ===")]
    [Tooltip("Должен быть УНИКАЛЬНЫМ для каждого SavePoint в сцене. " +
             "Если пустой — сгенерируется из позиции.")]
    public string savePointId = "";

    [Header("=== ИГРОК ===")]
    public string playerTag = "Player";

    [Header("=== ССЫЛКИ ===")]
    public PlayerHealth playerHealth;
    public PlayerController playerController;
    public WeaponManager weaponManager;

    [Header("=== ЗВУК ===")]
    public AudioClip saveSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private static HashSet<string> usedIdsInScene = new HashSet<string>();
    private bool used = false;

    void Awake()
    {
        if (string.IsNullOrEmpty(savePointId))
        {
            Vector3 p = transform.position;
            savePointId = $"savepoint_auto_{p.x:F1}_{p.y:F1}_{p.z:F1}";
            if (debugLogs)
                Debug.Log($"[SavePoint] Авто-id: '{savePointId}'");
        }

        if (usedIdsInScene.Contains(savePointId))
        {
            Debug.LogError($"[SavePoint] ❌ ДУБЛЬ ID '{savePointId}' на объекте '{name}'! " +
                           $"Смени savePointId в инспекторе.");
        }
        else
        {
            usedIdsInScene.Add(savePointId);
        }
    }

    void OnDestroy()
    {
        usedIdsInScene.Remove(savePointId);
    }

    void Start()
    {
        // ============================================================
        // ✅ ФИКС: если новая игра — НЕ проверяем touched, остаёмся видимыми
        // ============================================================
        bool isNewGame = GameSave.CheckNewGamePending();

        bool touched = false;
        if (!isNewGame)
            touched = GameSave.IsCheckpointTouched(savePointId);

        if (debugLogs)
            Debug.Log($"[SavePoint '{savePointId}'] Start. isNewGame = {isNewGame}, touched = {touched}");

        if (touched)
        {
            if (debugLogs)
                Debug.Log($"[SavePoint '{savePointId}'] Уже пройден — удаляю");
            Destroy(gameObject);
            return;
        }

        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();
        if (weaponManager == null) weaponManager = FindObjectOfType<WeaponManager>();

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (Time.timeScale <= 0f) return;
        if (!IsPlayer(other.transform)) return;

        used = true;

        if (debugLogs)
            Debug.Log($"[SavePoint '{savePointId}'] Игрок вошёл. Сохраняю...");

        List<string> unlockedIds = null;
        if (weaponManager != null)
            unlockedIds = weaponManager.GetUnlockedIds();

        GameSave.SaveGame(savePointId, playerHealth, playerController, weaponManager, unlockedIds);

        if (saveSound != null)
        {
            GameObject go = new GameObject("SaveSound");
            go.transform.position = transform.position;
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = saveSound;
            src.volume = soundVolume;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.Play();
            Destroy(go, saveSound.length + 0.1f);
        }

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
}