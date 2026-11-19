using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Триггер, который наносит урон NPC один раз за удар.
/// Вешается на объект с Collider (Is Trigger = true).
/// </summary>
public class NPCDamageTrigger : MonoBehaviour
{
    [Header("=== УРОН ===")]
    public float damage = 25f;

    [Tooltip("Слои, по которым бьёт триггер (например слой Kick)")]
    public LayerMask targetLayers = ~0;

    [Tooltip("Урон наносится один раз на каждый NPC (не спамит)")]
    public bool oneTimePerTarget = true;

    [Tooltip("Уничтожить триггер после первого попадания")]
    public bool destroyAfterHit = false;

    // ✅ Кэшируем сами NPC, а не коллайдеры — один NPC = один урон
    private readonly HashSet<NPC> damagedNPCs = new HashSet<NPC>();

    // ✅ Дополнительно — кулдаун на всякий случай
    [Tooltip("Минимальный интервал между ударами (сек) — защита от спама")]
    public float cooldown = 0.1f;
    private float lastHitTime = -999f;

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        int otherBit = 1 << other.gameObject.layer;
        if ((targetLayers.value & otherBit) == 0) return;

        NPC npc = other.GetComponentInParent<NPC>();
        if (npc == null) return;
        if (npc.IsDead()) return;

        // Проверка: уже били этого NPC?
        if (oneTimePerTarget && damagedNPCs.Contains(npc)) return;

        // Кулдаун
        if (Time.time - lastHitTime < cooldown) return;
        lastHitTime = Time.time;

        npc.TakeDamage(damage);

        if (oneTimePerTarget) damagedNPCs.Add(npc);
        if (destroyAfterHit) Destroy(gameObject);
    }

    // ✅ Сброс списка когда триггер выключается/включается
    void OnDisable()
    {
        damagedNPCs.Clear();
    }
}