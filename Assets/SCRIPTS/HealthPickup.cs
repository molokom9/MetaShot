using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("=== HP ===")]
    [Tooltip("Сколько HP восстанавливает")]
    public float healAmount = 25f;

    [Header("=== НАСТРОЙКИ ===")]
    public string playerTag = "Player";

    [Tooltip("Радиус подбора")]
    public float pickupRadius = 1.2f;

    [Header("=== ЗВУК ===")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("=== ВСПЫШКА ЭКРАНА ===")]
    [Tooltip("Синий цвет — аптечка")]
    public Color flashColor = new Color(0.2f, 0.6f, 1f, 1f);

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
        PlayerHealth hp = player.GetComponent<PlayerHealth>();
        if (hp == null) hp = player.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;

        // если игрок мёртв — не подбираем
        if (hp.IsDead()) return;

        // если HP уже максимум — не подбираем (не тратим аптечку зря)
        if (hp.GetHealth() >= hp.GetMaxHealth()) return;

        hp.Heal(healAmount);

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