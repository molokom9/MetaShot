using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("=== ОРУЖИЕ ===")]
    public string weaponId = "shotgun";

    [Header("=== НАСТРОЙКИ ===")]
    public string playerTag = "Player";

    [Header("=== ЗВУК ===")]
    public AudioClip pickupSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("=== ВСПЫШКА ЭКРАНА ===")]
    public Color flashColor = new Color(1f, 0.5f, 0.1f, 1f);

    private bool picked = false;

    // ✅ Кэш WeaponManager — ищем 1 раз
    private static WeaponManager cachedManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cachedManager = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.timeScale <= 0f) return;
        TryPickup(other);
    }

    void TryPickup(Collider other)
    {
        if (picked) return;
        if (!IsPlayer(other.transform)) return;

        // ✅ Кэшированный менеджер
        WeaponManager manager = GetWeaponManager();
        if (manager == null) return;

        if (manager.UnlockWeapon(weaponId))
        {
            PlaySound2D(pickupSound, soundVolume);
            ScreenFlash.Flash(flashColor);

            picked = true;
            Destroy(gameObject);
        }
    }

    // ✅ Кэш: ищем 1 раз, потом переиспользуем
    WeaponManager GetWeaponManager()
    {
        if (cachedManager == null)
            cachedManager = FindObjectOfType<WeaponManager>();

        return cachedManager;
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

        GameObject go = new GameObject("PickupSoundTemp");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }
}