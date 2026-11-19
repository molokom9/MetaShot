using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public float health;
    public float maxHealth;
    public float posX, posY, posZ;
    public float rotY;

    public List<string> ammoTypes = new List<string>();
    public List<int> ammoValues = new List<int>();

    public List<string> unlockedWeaponIds = new List<string>();
    public List<string> touchedCheckpoints = new List<string>();
}

public static class GameSave
{
    public const string Key = "GameSave_v1";

    private static SaveData current = null;
    private static bool loaded = false;

    // ============================================================
    // ✅ ФЛАГ "НОВАЯ ИГРА"
    // ============================================================
    // Устанавливается при Delete(). Живёт N секунд.
    // PlayerController и SavePoint смотрят на него и НЕ грузят старые данные.
    public static bool IsNewGamePending = false;
    public static float NewGamePendingUntil = 0f;
    private const float NEW_GAME_FLAG_DURATION = 5f;

    public static bool HasSave() => PlayerPrefs.HasKey(Key);

    public static void ResetCache()
    {
        loaded = false;
        current = null;
    }

    // ============================================================
    // ✅ ЖЁСТКОЕ УДАЛЕНИЕ ВСЕГО СОХРАНЕНИЯ
    // ============================================================
    public static void Delete()
    {
        Debug.Log($"[GameSave] Delete() START. HasSave = {HasSave()}");

        // Чистим текущий ключ
        PlayerPrefs.DeleteKey(Key);

        // ✅ Чистим возможные старые ключи (если раньше были другие имена)
        PlayerPrefs.DeleteKey("GameSave");
        PlayerPrefs.DeleteKey("GameSave_v0");
        PlayerPrefs.DeleteKey("GameSave_v2");
        PlayerPrefs.DeleteKey("SaveData");

        PlayerPrefs.Save();

        // Проверка — вдруг ключ не удалился
        if (HasSave())
        {
            Debug.LogWarning("[GameSave] Ключ всё ещё существует! Повторная очистка...");
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        ResetCache();

        // ✅ Ставим флаг «новая игра»
        IsNewGamePending = true;
        NewGamePendingUntil = Time.realtimeSinceStartup + NEW_GAME_FLAG_DURATION;

        Debug.Log($"[GameSave] Delete() END. HasSave = {HasSave()}, IsNewGamePending = true (на {NEW_GAME_FLAG_DURATION}с)");
    }

    // ============================================================
    // ✅ ПРОВЕРКА ФЛАГА "НОВАЯ ИГРА"
    // ============================================================
    public static bool CheckNewGamePending()
    {
        if (!IsNewGamePending) return false;
        if (Time.realtimeSinceStartup > NewGamePendingUntil)
        {
            IsNewGamePending = false;
            return false;
        }
        return true;
    }

    public static SaveData Load()
    {
        if (loaded) return current;

        loaded = true;

        if (!HasSave())
        {
            current = null;
            return null;
        }

        string json = PlayerPrefs.GetString(Key);
        if (string.IsNullOrEmpty(json))
        {
            current = null;
            return null;
        }

        try
        {
            current = JsonUtility.FromJson<SaveData>(json);

            if (current != null)
            {
                if (current.touchedCheckpoints == null)
                    current.touchedCheckpoints = new List<string>();
                if (current.unlockedWeaponIds == null)
                    current.unlockedWeaponIds = new List<string>();
                if (current.ammoTypes == null)
                    current.ammoTypes = new List<string>();
                if (current.ammoValues == null)
                    current.ammoValues = new List<int>();
            }
        }
        catch
        {
            current = null;
        }

        return current;
    }

    public static bool IsCheckpointTouched(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        SaveData d = Load();
        if (d == null || d.touchedCheckpoints == null) return false;
        return d.touchedCheckpoints.Contains(id);
    }

    public static void SaveGame(string checkpointId,
                                PlayerHealth health,
                                PlayerController pc,
                                WeaponManager weaponManager,
                                List<string> unlockedIds)
    {
        SaveData data = new SaveData();

        if (health != null)
        {
            data.health = health.GetHealth();
            data.maxHealth = health.GetMaxHealth();
        }

        if (pc != null)
        {
            Vector3 p = pc.transform.position;
            data.posX = p.x;
            data.posY = p.y;
            data.posZ = p.z;
            data.rotY = pc.transform.eulerAngles.y;
        }

        if (weaponManager != null)
        {
            foreach (var kv in weaponManager.ammoByType)
            {
                data.ammoTypes.Add(kv.Key);
                data.ammoValues.Add(kv.Value);
            }
        }

        if (unlockedIds != null)
            data.unlockedWeaponIds = new List<string>(unlockedIds);

        SaveData old = Load();
        if (old != null && old.touchedCheckpoints != null)
            data.touchedCheckpoints = new List<string>(old.touchedCheckpoints);

        if (!string.IsNullOrEmpty(checkpointId) && !data.touchedCheckpoints.Contains(checkpointId))
            data.touchedCheckpoints.Add(checkpointId);

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();

        current = data;
        loaded = true;

        // ✅ Если что-то сохранилось — значит новая игра уже не «чистая», снимаем флаг
        IsNewGamePending = false;

        Debug.Log($"[GameSave] Сохранено. Чекпоинтов: {data.touchedCheckpoints.Count} " +
                  $"({string.Join(", ", data.touchedCheckpoints)})");
    }
}