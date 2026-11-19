using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("=== ПАТРОНЫ ===")]
    public string ammoType = "pistol";
    public int amount = 12;

    [Header("=== НАСТРОЙКИ ===")]
    public string playerTag = "Player";
    public float pickupRadius = 1.2f;

    [Tooltip("Требовать чтобы влезло ПОЛНОСТЬЮ. Если выкл — подбирается если есть хоть немного места")]
    public bool requireFullAmount = false;

    [Header("=== ЗВУК ===")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("=== ВСПЫШКА ЭКРАНА ===")]
    public Color flashColor = new Color(1f, 0.9f, 0.2f, 1f);

    private bool picked = false;
    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (picked) return;
        if (Time.timeScale <= 0f) return;
        if (player == null) return;

        Vector3 diff = player.position - transform.position;
        if (diff.sqrMagnitude > pickupRadius * pickupRadius) return;

        TryPickup();
    }

    void TryPickup()
    {
        // ✅ Проверяем есть ли оружие такого типа в сцене
        int maxForType = Weapon.GetMaxAmmoForType(ammoType);
        int current = Weapon.GetAmmoStatic(ammoType);

        // Если пул ещё не создан (оружия нет) — current будет 0
        // Тогда всё равно пробуем — AddAmmoToType сам проверит

        int space = maxForType - current;

        if (requireFullAmount)
        {
            if (space < amount) return;
        }
        else
        {
            if (space <= 0) return;
        }

        // ✅ Пробуем добавить через статический метод
        if (!Weapon.AddAmmoToType(ammoType, amount, maxForType, out int taken))
            return;

        // Подобрали — обновляем UI, звук, вспышка
        PlaySound2D(pickupSound, soundVolume);
        ScreenFlash.Flash(flashColor);

        picked = true;
        Destroy(gameObject);
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