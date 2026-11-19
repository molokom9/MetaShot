using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    [Tooltip("Тег игрока")]
    public string playerTag = "Player";

    [Tooltip("Смотреть задом наперёд (галочка = поворот на 180°)")]
    public bool lookBackwards = false;

    [Header("=== ФИКСИРОВАННЫЙ ПОВОРОТ ===")]
    [Tooltip("Использовать фиксированные X/Z из полей ниже. Y всё равно смотрит на игрока")]
    public bool useFixedRotation = false;

    [Tooltip("Фиксированные углы X и Z. Y игнорируется — он всегда смотрит на игрока")]
    public Vector3 fixedRotation = new Vector3(-90f, 0f, 0f);

    private Transform player;
    private float startRotationX;
    private float startRotationZ;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;

        // Запоминаем стартовые X и Z
        Vector3 e = transform.eulerAngles;
        startRotationX = e.x;
        startRotationZ = e.z;
    }

    void Update()
    {
        if (player == null) return;

        // Берём X и Z — либо фиксированные, либо стартовые
        float useX = useFixedRotation ? fixedRotation.x : startRotationX;
        float useZ = useFixedRotation ? fixedRotation.z : startRotationZ;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Vector3 lookEuler = Quaternion.LookRotation(direction).eulerAngles;
        lookEuler.x = useX;
        lookEuler.z = useZ;

        if (lookBackwards)
            lookEuler.y += 180f;

        transform.rotation = Quaternion.Euler(lookEuler);
    }
}