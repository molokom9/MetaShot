using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Kick : MonoBehaviour
{
    [Header("=== ÑÑÛËÊÈ ===")]
    public Image kickImage;
    public PlayerHealth playerHealth;

    [Header("=== ÂÂÎÄ ===")]
    public KeyCode kickKey = KeyCode.Space;

    [Tooltip("Êíîïêà ãåéìïàäà äëÿ óäàğà — ïî óìîë÷àíèş A (South)")]
    public bool useGamepadKick = true;

    [Header("=== ÒÀÉÌÈÍÃÈ ===")]
    public float kickDuration = 0.4f;
    public float cooldown = 1f;

    [Header("=== ÎÁÚÅÊÒÛ ÓÄÀĞÀ ===")]
    public List<GameObject> hitObjects = new List<GameObject>();
    public float hitObjectsDuration = 0.1f;

    [Header("=== ÀÍÈÌÀÖÈß ===")]
    public float kickStartOffsetY = -300f;
    public float kickPeakOffsetY = 0f;
    public float kickRiseDuration = 0.1f;
    public float kickFallDuration = 0.15f;

    [Header("=== ÇÂÓÊ ===")]
    public AudioClip kickSound;
    [Range(0f, 1f)] public float kickVolume = 1f;

    [Header("=== ÎÒËÀÄÊÀ ===")]
    public bool drawDebugLog = false;

    private Vector2 kickStartPos;
    private bool isKicking = false;
    private float kickTimer = 0f;
    private float nextKickTime = 0f;

    private AudioSource audioSource;
    private Coroutine hitObjectsCoroutine;

    void Awake()
    {
        if (kickImage != null) kickStartPos = kickImage.rectTransform.anchoredPosition;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();

        if (kickImage != null)
        {
            kickImage.gameObject.SetActive(false);
            kickImage.rectTransform.anchoredPosition = kickStartPos + new Vector2(0f, kickStartOffsetY);
        }

        SetHitObjectsActive(false);
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead()) return;
        HandleInput();
        HandleKickAnimation();
    }

    void HandleInput()
    {
        if (isKicking) return;
        if (Time.time < nextKickTime) return;

        bool pressed = Input.GetKeyDown(kickKey);
        if (useGamepadKick && GamepadInput.A_Pressed) pressed = true;

        if (pressed) StartKick();
    }

    void StartKick()
    {
        isKicking = true;
        kickTimer = 0f;
        nextKickTime = Time.time + cooldown;

        if (kickImage != null)
        {
            kickImage.gameObject.SetActive(true);
            kickImage.rectTransform.anchoredPosition = kickStartPos + new Vector2(0f, kickStartOffsetY);
        }

        if (kickSound != null && audioSource != null)
            audioSource.PlayOneShot(kickSound, kickVolume);

        if (hitObjectsCoroutine != null) StopCoroutine(hitObjectsCoroutine);
        hitObjectsCoroutine = StartCoroutine(HitObjectsRoutine());

        if (drawDebugLog) Debug.Log("[Kick] Óäàğ íîãîé!");
    }

    System.Collections.IEnumerator HitObjectsRoutine()
    {
        SetHitObjectsActive(true);
        yield return new WaitForSeconds(hitObjectsDuration);
        SetHitObjectsActive(false);
        hitObjectsCoroutine = null;
    }

    void SetHitObjectsActive(bool state)
    {
        if (hitObjects == null) return;
        for (int i = 0; i < hitObjects.Count; i++)
            if (hitObjects[i] != null) hitObjects[i].SetActive(state);
    }

    void HandleKickAnimation()
    {
        if (!isKicking) return;
        if (kickImage == null) { isKicking = false; return; }

        kickTimer += Time.deltaTime;
        float totalDuration = kickRiseDuration + kickFallDuration;
        Vector2 targetPos;

        if (kickTimer <= kickRiseDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(kickTimer / kickRiseDuration));
            targetPos = kickStartPos + new Vector2(0f, Mathf.Lerp(kickStartOffsetY, kickPeakOffsetY, t));
        }
        else if (kickTimer <= totalDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((kickTimer - kickRiseDuration) / kickFallDuration));
            targetPos = kickStartPos + new Vector2(0f, Mathf.Lerp(kickPeakOffsetY, kickStartOffsetY, t));
        }
        else
        {
            kickImage.gameObject.SetActive(false);
            isKicking = false;
            return;
        }

        kickImage.rectTransform.anchoredPosition = targetPos;
    }

    public bool IsKicking() => isKicking;
    public bool IsOnCooldown() => Time.time < nextKickTime;
}