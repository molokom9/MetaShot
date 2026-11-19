using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Weapon : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    public Image weaponImage;
    public Sprite idleSprite;
    public Sprite fireSprite;
    public TextMeshProUGUI ammoText;
    public Transform cameraTransform;

    [Header("=== ССЫЛКА НА МЕНЕДЖЕР ===")]
    [Tooltip("Перетащи сюда WeaponManager игрока")]
    public WeaponManager weaponManager;

    [Header("=== ИДЕНТИФИКАЦИЯ ===")]
    public string weaponId = "pistol";
    public string ammoType = "pistol";

    [Header("=== ПАТРОНЫ ===")]
    public int ammoPerShot = 1;
    public int maxAmmo = 999;

    [Header("=== СТРЕЛЬБА ===")]
    public float range = 100f;
    public float damage = 25f;
    public float fireRate = 0.15f;
    public float fireDuration = 0.1f;
    public bool automatic = false;
    public KeyCode fireKey = KeyCode.Mouse0;
    public LayerMask hitLayers = ~0;
    public float spread = 0f;
    public int pelletsPerShot = 1;

    [Header("=== ПУЛЯ / ДЕКАЛЬ ===")]
    public GameObject bulletPrefab;
    public GameObject decalPrefab;
    public int maxDecals = 100;

    [Header("=== ЗВУК ВЫСТРЕЛА ===")]
    public AudioClip fireSound;
    [Range(0f, 1f)] public float fireVolume = 1f;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("=== ЗВУК ПУСТОГО МАГАЗИНА ===")]
    public AudioClip emptySound;
    [Range(0f, 1f)] public float emptyVolume = 1f;

    [Header("=== ОТЛАДКА ===")]
    public GameObject hitMarkerPrefab;
    public float hitMarkerDuration = 1f;
    public bool drawDebugRay = true;
    public Color debugRayColor = Color.red;

    [Header("=== ПОДЪЁМ ОРУЖИЯ ===")]
    public bool enableRaise = true;
    public float raiseOffset = 300f;
    public float raiseDuration = 0.25f;
    public bool blockFireWhileRaising = true;

    [Header("=== ТРЯСКА ОРУЖИЯ ===")]
    public bool enableWeaponBob = true;
    public float walkBobStrength = 4f;
    public float walkBobFrequency = 6f;
    public float runBobStrength = 10f;
    public float runBobFrequency = 12f;
    public float bobReturnSpeed = 10f;

    [Header("=== СВЯЗЬ С ИГРОКОМ ===")]
    public PlayerController playerController;
    public PlayerHealth playerHealth;

    // === ВНУТРЕННИЕ ===
    private float nextFireTime = 0f;
    private float fireSpriteUntil = 0f;

    private Vector2 weaponStartPos;
    private float bobTimer = 0f;
    private float currentBobStrength;
    private float currentBobFrequency;

    private bool isRaising = false;
    private float raiseTimer = 0f;

    private Rigidbody playerRb;
    private bool positionSaved = false;

    private AudioSource audioSource;

    // === СТАТИЧЕСКИЕ МЕТОДЫ ===
    private static WeaponManager FindManager()
    {
        return FindObjectOfType<WeaponManager>();
    }

    public static int GetAmmoStatic(string type)
    {
        WeaponManager wm = FindManager();
        if (wm == null) return 0;
        return wm.GetAmmo(type);
    }

    public static bool AddAmmoToType(string type, int amount, int maxForType, out int taken)
    {
        taken = 0;
        WeaponManager wm = FindManager();
        if (wm == null) return false;

        int before = wm.GetAmmo(type);
        bool ok = wm.AddAmmo(type, amount, maxForType);
        int after = wm.GetAmmo(type);
        taken = after - before;
        return ok;
    }

    public static int GetMaxAmmoForType(string type)
    {
        Weapon[] all = FindObjectsOfType<Weapon>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].ammoType == type)
                return all[i].maxAmmo;
        }
        return 999;
    }

    public static Dictionary<string, int> GetAllAmmo()
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        WeaponManager wm = FindManager();
        if (wm != null) return new Dictionary<string, int>(wm.ammoByType);
        return result;
    }

    public static void ApplySaveAmmo(SaveData save)
    {
        if (save == null) return;
        if (save.ammoTypes == null || save.ammoValues == null) return;

        WeaponManager wm = FindManager();
        if (wm == null) return;

        for (int i = 0; i < save.ammoTypes.Count; i++)
        {
            if (i >= save.ammoValues.Count) break;
            string type = save.ammoTypes[i];
            if (string.IsNullOrEmpty(type)) continue;

            wm.ammoByType[type] = save.ammoValues[i];
            Debug.Log($"[Weapon] Загрузка: {type} = {save.ammoValues[i]}");
        }

        wm.SyncInspectorFromDict();

        Weapon[] all = FindObjectsOfType<Weapon>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null) all[i].UpdateAmmoUI();
        }
    }

    void Awake()
    {
        if (weaponManager == null)
            weaponManager = GetComponentInParent<WeaponManager>();

        if (weaponManager == null)
            weaponManager = FindObjectOfType<WeaponManager>();

        SaveStartPosition();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.UnregisterWeaponScript(this);
    }

    void SaveStartPosition()
    {
        if (positionSaved) return;
        positionSaved = true;
        if (weaponImage != null)
            weaponStartPos = weaponImage.rectTransform.anchoredPosition;
    }

    void Start()
    {
        UpdateAmmoUI();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (playerController != null)
            playerRb = playerController.GetComponent<Rigidbody>();

        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
    }

    void OnEnable()
    {
        if (weaponManager == null)
            weaponManager = GetComponentInParent<WeaponManager>();

        if (weaponManager == null)
            weaponManager = FindObjectOfType<WeaponManager>();

        SaveStartPosition();

        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.RegisterWeaponScript(this);

        if (weaponImage != null)
        {
            weaponImage.gameObject.SetActive(true);

            if (enableRaise)
            {
                weaponImage.rectTransform.anchoredPosition =
                    weaponStartPos - new Vector2(0f, raiseOffset);
                isRaising = true;
                raiseTimer = 0f;
            }
            else
            {
                weaponImage.rectTransform.anchoredPosition = weaponStartPos;
                isRaising = false;
            }

            if (idleSprite != null) weaponImage.sprite = idleSprite;
        }

        if (ammoText != null)
            ammoText.gameObject.SetActive(true);

        bobTimer = 0f;
        fireSpriteUntil = 0f;

        UpdateAmmoUI();
    }

    void OnDisable()
    {
        HideUI();
        if (playerHealth != null)
            playerHealth.UnregisterWeaponScript(this);
    }

    public void HideUI()
    {
        if (weaponImage != null)
            weaponImage.gameObject.SetActive(false);

        if (ammoText != null)
            ammoText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead())
            return;

        HandleFireInput();
        HandleFireSprite();
        HandleWeaponBob();
    }

    void HandleFireInput()
    {
        if (Time.time < nextFireTime) return;
        if (isRaising && blockFireWhileRaising) return;
        if (playerHealth != null && playerHealth.IsDead()) return;

        // ==========================================
        // ВОТ ТУТ ДОБАВЛЕНА СТРЕЛЬБА НА RT
        // ==========================================
        bool wantsToFire = false;

        if (automatic)
        {
            // Для автоматов: зажимаем мышь ИЛИ зажимаем RT
            wantsToFire = Input.GetKey(fireKey) || GamepadInput.RT_Held;
        }
        else
        {
            // Для пистолетов: клик мышью ИЛИ нажатие RT
            wantsToFire = Input.GetKeyDown(fireKey) || GamepadInput.RT_Pressed;
        }

        if (!wantsToFire) return;

        if (GetAmmo() < ammoPerShot)
        {
            PlaySound(emptySound, emptyVolume);
            nextFireTime = Time.time + fireRate;
            return;
        }

        SpendAmmo(ammoPerShot);
        nextFireTime = Time.time + fireRate;
        PlaySound(fireSound, fireVolume);

        if (weaponImage != null && fireSprite != null)
        {
            weaponImage.sprite = fireSprite;
            fireSpriteUntil = Time.time + fireDuration;
        }

        for (int i = 0; i < pelletsPerShot; i++)
            FireRay();
    }

    void FireRay()
    {
        if (cameraTransform == null) return;

        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;

        if (spread > 0f)
        {
            direction = Quaternion.Euler(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0f
            ) * direction;
        }

        if (drawDebugRay)
            Debug.DrawRay(origin, direction * range, debugRayColor, 0.5f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore))
        {
            OnHit(hit);
        }
    }

    void OnHit(RaycastHit hit)
    {
        if (bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(
                bulletPrefab,
                hit.point + hit.normal * 0.01f,
                Quaternion.LookRotation(hit.normal)
            );

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.damage = damage;
                bullet.decalPrefab = decalPrefab;
                bullet.maxDecals = maxDecals;
                bullet.ApplyHit(hit);
            }

            SpawnHitMarker(hit);
            return;
        }

        hit.collider.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        SpawnHitMarker(hit);
        SpawnDecal(hit);
    }

    void SpawnHitMarker(RaycastHit hit)
    {
        if (hitMarkerPrefab == null) return;

        Vector3 spawnPos = hit.point + hit.normal * 0.01f;
        Quaternion spawnRot = Quaternion.LookRotation(hit.normal);

        GameObject marker = Instantiate(hitMarkerPrefab, spawnPos, spawnRot);
        Destroy(marker, hitMarkerDuration);
    }

    void SpawnDecal(RaycastHit hit)
    {
        if (decalPrefab == null) return;

        GameObject decal = Instantiate(decalPrefab);

        BulletDecal bd = decal.GetComponent<BulletDecal>();
        if (bd != null)
            bd.Setup(hit);
        else
        {
            decal.transform.position = hit.point + hit.normal * 0.001f;
            decal.transform.rotation = Quaternion.LookRotation(-hit.normal);
        }

        Destroy(decal, 6f);
    }

    void HandleFireSprite()
    {
        if (weaponImage == null || idleSprite == null) return;

        if (fireSpriteUntil > 0f && Time.time >= fireSpriteUntil)
        {
            weaponImage.sprite = idleSprite;
            fireSpriteUntil = 0f;
        }
    }

    void HandleWeaponBob()
    {
        if (weaponImage == null) return;

        Vector2 bobOffset = Vector2.zero;

        if (enableWeaponBob && playerRb != null)
        {
            float speed = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z).magnitude;
            bool isMoving = speed > 0.1f;

            float runThreshold = 4.5f;
            if (playerController != null)
                runThreshold = (playerController.walkSpeed + playerController.runSpeed) * 0.5f;

            bool running = speed > runThreshold;

            currentBobStrength = running ? runBobStrength : walkBobStrength;
            currentBobFrequency = running ? runBobFrequency : walkBobFrequency;

            if (isMoving)
            {
                bobTimer += Time.deltaTime * currentBobFrequency;
                bobOffset.x = Mathf.Sin(bobTimer) * currentBobStrength;
                bobOffset.y = Mathf.Abs(Mathf.Cos(bobTimer)) * currentBobStrength * 0.5f;
            }
            else bobTimer = 0f;
        }

        float raiseY = 0f;

        if (isRaising)
        {
            raiseTimer += Time.deltaTime;
            float t = Mathf.Clamp01(raiseTimer / raiseDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            raiseY = Mathf.Lerp(-raiseOffset, 0f, t);
            if (t >= 1f) isRaising = false;
        }

        Vector2 targetPos = weaponStartPos + bobOffset + new Vector2(0f, raiseY);

        if (isRaising)
            weaponImage.rectTransform.anchoredPosition = targetPos;
        else
            weaponImage.rectTransform.anchoredPosition = Vector2.Lerp(
                weaponImage.rectTransform.anchoredPosition,
                targetPos,
                Time.deltaTime * bobReturnSpeed);
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
    }

    public void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        if (!gameObject.activeInHierarchy) return;
        ammoText.text = GetAmmo().ToString();
    }

    public int GetAmmo()
    {
        if (weaponManager == null) return 0;
        return weaponManager.GetAmmo(ammoType);
    }

    public int GetMaxAmmo() => maxAmmo;

    public bool IsFullAmmo()
    {
        return GetAmmo() >= maxAmmo;
    }

    public bool AddAmmo(int amount)
    {
        if (weaponManager == null) return false;
        return weaponManager.AddAmmo(ammoType, amount, maxAmmo);
    }

    void SpendAmmo(int amount)
    {
        if (weaponManager == null) return;
        if (string.IsNullOrEmpty(ammoType)) return;

        int current = weaponManager.GetAmmo(ammoType);
        current -= amount;
        if (current < 0) current = 0;

        weaponManager.SetAmmo(ammoType, current);
    }
}