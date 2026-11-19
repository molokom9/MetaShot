using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [Header("=== ТРИГГЕРЫ ===")]
    public Collider startTrigger;
    public List<Collider> stopTriggers = new List<Collider>();

    [Header("=== МУЗЫКА ===")]
    public List<AudioClip> musicClips = new List<AudioClip>();

    [Header("=== ГРОМКОСТЬ ===")]
    [Tooltip("МАКСИМАЛЬНАЯ громкость этого трека (0.5 = 50%). Итоговая = musicVolume × слайдер")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Tooltip("Время затухания при остановке (сек)")]
    public float fadeOutDuration = 1.5f;

    [Header("=== НАСТРОЙКИ ===")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    // === ВНУТРЕННИЕ ===
    private AudioSource audioSource;
    private int currentClipIndex = 0;
    private bool isPlaying = false;
    private bool isFading = false;

    private bool startUsed = false;
    private HashSet<Collider> usedStopTriggers = new HashSet<Collider>();

    private Coroutine playlistCoroutine;
    private Coroutine fadeCoroutine;

    // Глобальный множитель (из настроек)
    private float globalVolumeMultiplier = 1f;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        // ✅ Загрузка глобального множителя из PlayerPrefs
        globalVolumeMultiplier = PlayerPrefs.GetFloat("MusicVolume", 1f);

        audioSource.volume = musicVolume * globalVolumeMultiplier;
    }

    private void Start()
    {
        SetupTrigger(startTrigger, true);

        if (stopTriggers != null)
        {
            foreach (var col in stopTriggers)
                SetupTrigger(col, false);
        }
    }

    private void SetupTrigger(Collider col, bool isStart)
    {
        if (col == null) return;

        col.isTrigger = true;

        MusicTriggerListener listener = col.gameObject.GetComponent<MusicTriggerListener>();
        if (listener == null)
            listener = col.gameObject.AddComponent<MusicTriggerListener>();

        listener.Init(this, col, isStart);
    }

    public void OnChildTriggerEnter(Collider triggerCol, Collider other, bool isStart)
    {
        if (!IsPlayer(other.transform)) return;

        if (isStart)
        {
            if (triggerOnce && startUsed) return;

            if (debugLogs) Debug.Log($"[MusicTrigger] СТАРТ '{triggerCol.gameObject.name}'");

            startUsed = true;
            PlayMusic();
        }
        else
        {
            if (triggerOnce && usedStopTriggers.Contains(triggerCol)) return;

            if (debugLogs) Debug.Log($"[MusicTrigger] СТОП '{triggerCol.gameObject.name}'");

            usedStopTriggers.Add(triggerCol);
            StopMusic();
        }
    }

    private void PlayMusic()
    {
        if (isPlaying) return;

        if (musicClips == null || musicClips.Count == 0)
        {
            Debug.LogWarning("[MusicTrigger] musicClips пуст!");
            return;
        }

        isPlaying = true;
        isFading = false;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (playlistCoroutine != null) StopCoroutine(playlistCoroutine);

        playlistCoroutine = StartCoroutine(PlayPlaylistRoutine());
    }

    private void StopMusic()
    {
        if (!isPlaying || isFading) return;

        isFading = true;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator PlayPlaylistRoutine()
    {
        while (isPlaying)
        {
            AudioClip clip = musicClips[currentClipIndex];
            if (clip != null)
            {
                audioSource.clip = clip;

                // ✅ Итоговая = base × globalMultiplier
                audioSource.volume = musicVolume * globalVolumeMultiplier;
                audioSource.Play();

                if (debugLogs)
                    Debug.Log($"[MusicTrigger] Играет [{currentClipIndex + 1}/{musicClips.Count}]: {clip.name} " +
                              $"(vol = {musicVolume} × {globalVolumeMultiplier} = {audioSource.volume})");

                yield return new WaitForSeconds(clip.length);

                currentClipIndex = (currentClipIndex + 1) % musicClips.Count;
            }
            else
            {
                currentClipIndex = (currentClipIndex + 1) % musicClips.Count;
                yield return null;
            }
        }
    }

    private IEnumerator FadeOutAndStop()
    {
        float startVol = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            audioSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        audioSource.Stop();

        if (playlistCoroutine != null) StopCoroutine(playlistCoroutine);

        if (musicClips.Count > 0)
            currentClipIndex = (currentClipIndex + 1) % musicClips.Count;

        isPlaying = false;
        isFading = false;

        if (debugLogs) Debug.Log("[MusicTrigger] Музыка остановлена");
    }

    // ✅ Публичный метод — вызывается из SettingsMenu
    public void ApplyMusicVolume(float sliderValue)
    {
        globalVolumeMultiplier = sliderValue;

        if (audioSource != null && isPlaying && !isFading)
        {
            audioSource.volume = musicVolume * globalVolumeMultiplier;
        }
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

public class MusicTriggerListener : MonoBehaviour
{
    private MusicTrigger manager;
    private Collider myCollider;
    private bool isStart;

    public void Init(MusicTrigger mgr, Collider col, bool start)
    {
        manager = mgr;
        myCollider = col;
        isStart = start;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (manager != null)
            manager.OnChildTriggerEnter(myCollider, other, isStart);
    }
}