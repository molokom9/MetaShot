using UnityEngine;

public class BulletDecal : MonoBehaviour
{
    [Header("=== ВРЕМЯ ЖИЗНИ ===")]
    [Tooltip("Сколько секунд живёт дырка (включая затухание)")]
    public float lifetime = 6f;

    [Tooltip("Сколько секунд длится плавное исчезновение в конце")]
    public float fadeDuration = 1f;

    [Header("=== ОПЦИИ ===")]
    [Tooltip("Прилипать к объекту, по которому попали")]
    public bool parentToHit = true;

    [Tooltip("Смещение от поверхности (чтоб не мерцало)")]
    public float surfaceOffset = 0.001f;

    [Tooltip("Дополнительный поворот декали. Для Quad обычно X = -90")]
    public Vector3 rotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("=== СЛУЧАЙНЫЙ РАЗМЕР ===")]
    [Tooltip("Минимальный масштаб (1 = как в префабе)")]
    public float minScale = 0.8f;

    [Tooltip("Максимальный масштаб")]
    public float maxScale = 1.3f;

    // === ВНУТРЕННИЕ ===
    private Renderer rend;
    private Material matInstance;
    private float timer = 0f;
    private bool isFading = false;
    private Color startColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            matInstance = rend.material;
            startColor = matInstance.HasProperty("_Color") ? matInstance.color : Color.white;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!isFading && timer >= lifetime - fadeDuration)
            isFading = true;

        if (isFading && matInstance != null)
        {
            float fadeProgress = (timer - (lifetime - fadeDuration)) / fadeDuration;
            float alpha = Mathf.Clamp01(1f - fadeProgress);

            Color c = startColor;
            c.a *= alpha;

            if (matInstance.HasProperty("_Color"))
                matInstance.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    /// <summary>Настроить дырку под попадание</summary>
    public void Setup(RaycastHit hit)
    {
        // позиция
        transform.position = hit.point + hit.normal * surfaceOffset;

        // ориентация строго по нормали поверхности + смещение
        Quaternion baseRot = Quaternion.LookRotation(-hit.normal);
        Quaternion finalRot = baseRot * Quaternion.Euler(rotationOffset);
        transform.rotation = finalRot;

        // случайный размер
        float scale = Random.Range(minScale, maxScale);
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(s.x * scale, s.y * scale, s.z * scale);

        // прилипить к объекту
        if (parentToHit && hit.collider != null)
            transform.SetParent(hit.collider.transform, true);
    }
}