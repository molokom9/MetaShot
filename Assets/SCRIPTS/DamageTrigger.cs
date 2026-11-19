using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [Header("=== УРОН (диапазон) ===")]
    [Tooltip("Минимальный урон")]
    public float minDamage = 10f;

    [Tooltip("Максимальный урон")]
    public float maxDamage = 15f;

    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Бить ли постоянно пока игрок внутри")]
    public bool damageOverTime = false;

    [Tooltip("Как часто наносить урон (сек)")]
    public float damageInterval = 0.5f;

    [Tooltip("Тег того, кто получает урон")]
    public string targetTag = "Player";

    private float lastDamageTime = -999f;

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!damageOverTime) return;
        TryDamage(other);
    }

    void TryDamage(Collider other)
    {
        if (Time.timeScale <= 0f) return;
        if (!IsTarget(other.transform)) return;

        // защита от двойного срабатывания в одном кадре и от слишком частых вызовов
        float interval = damageOverTime ? damageInterval : 0.1f;
        if (Time.time < lastDamageTime + interval) return;

        PlayerHealth hp = other.GetComponent<PlayerHealth>();
        if (hp == null) hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;

        float dmg = Random.Range(minDamage, maxDamage);
        hp.TakeDamage(dmg);

        lastDamageTime = Time.time;
    }

    bool IsTarget(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag(targetTag)) return true;
            t = t.parent;
        }
        return false;
    }
}