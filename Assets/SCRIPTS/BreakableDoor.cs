using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableDoor : MonoBehaviour
{
    [Header("=== СОСТОЯНИЕ ===")]
    public bool isBroken = false;
    public bool onlyPlayerCanBreak = false;
    public string playerTag = "Player";

    [Header("=== СЛОИ ===")]
    public LayerMask breakLayers = ~0;

    [Header("=== ФИЗИКА ПРИ ВЫБИВАНИИ ===")]
    public float breakForce = 6f;
    public float torqueForce = 4f;
    public Transform forceOriginOverride;

    [Header("=== МАССА / ФИЗИКА ===")]
    public float doorMass = 5f;
    public float doorDrag = 1f;
    public float doorAngularDrag = 2f;

    [Header("=== ЗВУК ===")]
    public AudioClip breakSound;
    [Range(0f, 1f)] public float breakVolume = 1f;

    [Header("=== ОПЦИИ ===")]
    public float destroyAfter = 0f;

    [Header("=== ФИЗИКА ДВЕРИ ===")]
    [Tooltip("Толкать двери игроком/объектами? Если да — Rigidbody создаётся сразу, но constraints держат")]
    public bool pushable = false;

    // === ВНУТРЕННИЕ ===
    private Collider col;
    private Rigidbody rb;
    private AudioSource audioSource;
    private Transform tr;
    private bool isInitialized = false;

    private Vector3 cachedPosition;

    private const float POS_EPSILON_SQR = 0.0001f;

    void Awake()
    {
        tr = transform;
        col = GetComponent<Collider>();

        // ✅ ФИКС: дверь НЕ триггер — игрок не проходит сквозь.
        col.isTrigger = false;

        // ✅ Аудио
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // ✅ ФИКС: Rigidbody создаём ТОЛЬКО если дверь должна быть толкаемой.
        // Если нет — её физика = 0, вообще ничего не жрёт.
        rb = GetComponent<Rigidbody>();
        if (pushable)
        {
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = false;
            rb.mass = doorMass;
            rb.drag = doorDrag;
            rb.angularDrag = doorAngularDrag;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        else
        {
            // Убираем Rigidbody если он случайно висит в инспекторе
            if (rb != null)
            {
                Destroy(rb);
                rb = null;
            }
        }

        isInitialized = true;
    }

    void OnTriggerEnter(Collider other)
    {
        TryBreak(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryBreak(collision.collider);
    }

    void TryBreak(Collider other)
    {
        if (!isInitialized) return;
        if (isBroken) return;
        if (other == null) return;

        int otherLayerBit = 1 << other.gameObject.layer;
        if ((breakLayers.value & otherLayerBit) == 0) return;

        if (onlyPlayerCanBreak)
        {
            if (!IsPlayer(other)) return;
        }

        Break(other.transform);
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        Transform p = other.transform.parent;
        while (p != null)
        {
            if (p.CompareTag(playerTag)) return true;
            p = p.parent;
        }
        return false;
    }

    /// <summary>
    /// Выбить дверь.
    /// </summary>
    public void Break(Transform source)
    {
        if (isBroken) return;
        isBroken = true;

        cachedPosition = tr.position;

        // === 1. Если Rigidbody нет — создаём (единственный момент фриза) ===
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = doorMass;
            rb.drag = doorDrag;
            rb.angularDrag = doorAngularDrag;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // ⚠️ AddComponent<Rigidbody> — сам по себе не фризит сильно,
            // но первый кадр с dynamic-телом может дать микро-лаг.
            // Это неизбежно, если дверь не была dynamic изначально.
        }
        else
        {
            // Снимаем constraints — мгновенно
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = true;
        }

        // === 2. Направление отлёта ===
        Vector3 origin;
        if (forceOriginOverride != null)
            origin = forceOriginOverride.position;
        else if (source != null)
            origin = source.position;
        else
            origin = cachedPosition - tr.forward;

        float dx = cachedPosition.x - origin.x;
        float dz = cachedPosition.z - origin.z;
        float dirSqr = dx * dx + dz * dz;

        float fx, fy, fz;

        if (dirSqr < POS_EPSILON_SQR)
        {
            Vector3 fwd = tr.forward;
            fx = fwd.x;
            fy = fwd.y + 0.3f;
            fz = fwd.z;
        }
        else
        {
            float invDist = 1f / Mathf.Sqrt(dirSqr);
            fx = dx * invDist;
            fz = dz * invDist;
            fy = 0.3f;
        }

        // Нормализация
        float fSqr = fx * fx + fy * fy + fz * fz;
        if (fSqr > 0.0001f)
        {
            float fInv = 1f / Mathf.Sqrt(fSqr);
            fx *= fInv;
            fy *= fInv;
            fz *= fInv;
        }

        // === 3. Импульс ===
        float impulseScale = breakForce / doorMass;
        rb.velocity = new Vector3(fx * impulseScale, fy * impulseScale, fz * impulseScale);

        float torqueScale = torqueForce / doorMass;
        rb.angularVelocity = new Vector3(
            Random.Range(-1f, 1f) * torqueScale,
            Random.Range(-1f, 1f) * torqueScale,
            Random.Range(-1f, 1f) * torqueScale
        );

        // === 4. Звук ===
        if (breakSound != null && audioSource != null)
            audioSource.PlayOneShot(breakSound, breakVolume);

        // === 5. Уничтожение ===
        if (destroyAfter > 0f)
            Destroy(gameObject, destroyAfter);
    }

    public void ResetDoor()
    {
        isBroken = false;
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public bool IsBroken() => isBroken;
}