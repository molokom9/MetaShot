using UnityEngine;
using System.Collections;

/// <summary>
/// Вешается на пустой GameObject в сцене.
/// Слушает смерть конкретного босса (NPC):
///  - активирует objectsToActivate
///  - деактивирует objectsToDeactivate
///  - плавно опускает objectsToLower (алгоритм как в DoorButton, только вниз)
/// Остальные NPC не затрагиваются.
/// </summary>
public class BossDeathActivator : MonoBehaviour
{
    [Header("=== БОСС ===")]
    [Tooltip("Компонент NPC, который является боссом")]
    public NPC boss;

    [Header("=== АКТИВИРУЕМЫЕ ОБЪЕКТЫ ===")]
    [Tooltip("Эти объекты будут ВКЛЮЧЕНЫ (SetActive(true)) после смерти босса")]
    public GameObject[] objectsToActivate;

    [Header("=== ОТКЛЮЧАЕМЫЕ ОБЪЕКТЫ ===")]
    [Tooltip("Эти объекты будут ВЫКЛЮЧЕНЫ (SetActive(false)) после смерти босса")]
    public GameObject[] objectsToDeactivate;

    [Header("=== ОПУСКАЕМЫЕ ОБЪЕКТЫ ===")]
    [Tooltip("Эти объекты будут ПЛАВНО ОПУСКАТЬСЯ ВНИЗ после смерти босса")]
    public GameObject[] objectsToLower;

    [Tooltip("На сколько метров вниз опустить каждый объект")]
    public float lowerDistance = 5f;

    [Tooltip("За сколько секунд объект опустится")]
    public float lowerDuration = 3f;

    [Tooltip("Кривая скорости опускания (X=0..1 время, Y=0..1 прогресс)")]
    public AnimationCurve lowerCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Уничтожить объект после опускания")]
    public bool destroyAfterLower = false;

    [Header("=== ОПЦИИ ===")]
    [Tooltip("Сработать только один раз")]
    public bool activateOnlyOnce = true;

    [Tooltip("Лог в консоль")]
    public bool debugLogs = true;

    private bool triggered = false;

    void OnEnable()
    {
        NPC.OnAnyNpcDied += HandleNpcDied;
    }

    void OnDisable()
    {
        NPC.OnAnyNpcDied -= HandleNpcDied;
    }

    void HandleNpcDied(NPC deadNpc)
    {
        if (triggered && activateOnlyOnce) return;
        if (boss == null) return;

        // Нас интересует только наш босс
        if (deadNpc != boss) return;

        triggered = true;

        int actCount = objectsToActivate != null ? objectsToActivate.Length : 0;
        int deactCount = objectsToDeactivate != null ? objectsToDeactivate.Length : 0;
        int lowerCount = objectsToLower != null ? objectsToLower.Length : 0;

        if (debugLogs)
            Debug.Log($"[BossDeathActivator] Босс '{boss.name}' погиб. Включаю {actCount}, выключаю {deactCount}, опускаю {lowerCount}.");

        // 1) Сначала включаем
        if (objectsToActivate != null)
        {
            for (int i = 0; i < objectsToActivate.Length; i++)
            {
                var go = objectsToActivate[i];
                if (go == null) continue;

                go.SetActive(true);

                if (debugLogs)
                    Debug.Log($"[BossDeathActivator] Включён: {go.name}");
            }
        }

        // 2) Потом выключаем
        if (objectsToDeactivate != null)
        {
            for (int i = 0; i < objectsToDeactivate.Length; i++)
            {
                var go = objectsToDeactivate[i];
                if (go == null) continue;

                go.SetActive(false);

                if (debugLogs)
                    Debug.Log($"[BossDeathActivator] Выключен: {go.name}");
            }
        }

        // 3) Опускаем
        if (objectsToLower != null)
        {
            for (int i = 0; i < objectsToLower.Length; i++)
            {
                var go = objectsToLower[i];
                if (go == null) continue;

                go.SetActive(true);
                StartCoroutine(LowerRoutine(go));

                if (debugLogs)
                    Debug.Log($"[BossDeathActivator] Начинаю опускать: {go.name}");
            }
        }
    }

    // ✅ Алгоритм 1-в-1 как в DoorButton.OpenDoorsRoutine, только Vector3.down вместо Vector3.up
    IEnumerator LowerRoutine(GameObject go)
    {
        if (go == null) yield break;

        // Как в DoorButton: фиксируем стартовую позицию ОДИН РАЗ
        Vector3 startPos = go.transform.position;

        // Как в DoorButton: конечная позиция вычисляется ОДИН РАЗ
        Vector3 endPos = startPos + Vector3.down * lowerDistance;

        float duration = Mathf.Max(0.01f, lowerDuration);

        if (debugLogs)
            Debug.Log($"[BossDeathActivator] Опускаю '{go.name}': {startPos} -> {endPos} за {duration} сек.");

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (go == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = lowerCurve != null ? lowerCurve.Evaluate(t) : t;

            go.transform.position = Vector3.Lerp(startPos, endPos, curveT);

            yield return null;
        }

        if (go == null) yield break;

        // Как в DoorButton: финально выставляем точную конечную позицию
        go.transform.position = endPos;

        if (debugLogs)
            Debug.Log($"[BossDeathActivator] Опущен: {go.name}");

        if (destroyAfterLower)
            Destroy(go);
    }
}