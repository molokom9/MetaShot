using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("=== УРОН ===")]
    public float damage = 25f;

    [Header("=== ДЕКАЛЬ ===")]
    public GameObject decalPrefab;
    public int maxDecals = 100;

    [Header("=== ЭФФЕКТЫ ===")]
    public GameObject hitEffectPrefab;
    public float hitEffectDuration = 1f;

    [Header("=== АВТОУНИЧТОЖЕНИЕ ===")]
    public float destroyAfter = 0.05f;

    [Header("=== ЗАЩИТА ===")]
    [Tooltip("Если true — пуля не наносит урон игроку (по тегу Player или по PlayerHealth)")]
    public bool ignorePlayerDamage = true;

    // общий пул декалей
    private static Queue<GameObject> decalQueue = new Queue<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDecals()
    {
        decalQueue = new Queue<GameObject>();
    }

    /// <summary>Применить попадание. Вызывать из Weapon сразу после Instantiate</summary>
    public void ApplyHit(RaycastHit hit)
    {
        // === ЗАЩИТА ОТ САМОПОПАДАНИЯ ===
        if (ignorePlayerDamage)
        {
            if (hit.collider.CompareTag("Player")) return;
            if (hit.collider.GetComponentInParent<PlayerHealth>() != null) return;
        }

        // урон
        hit.collider.gameObject.SendMessage(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        // декаль
        SpawnDecal(hit);

        // эффект
        SpawnHitEffect(hit);

        // самоуничтожение
        if (destroyAfter > 0f)
            Destroy(gameObject, destroyAfter);
        else
            Destroy(gameObject);
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

        decalQueue.Enqueue(decal);
        while (decalQueue.Count > maxDecals)
        {
            GameObject old = decalQueue.Dequeue();
            if (old != null) Destroy(old);
        }
    }

    void SpawnHitEffect(RaycastHit hit)
    {
        if (hitEffectPrefab == null) return;

        GameObject fx = Instantiate(
            hitEffectPrefab,
            hit.point + hit.normal * 0.01f,
            Quaternion.LookRotation(hit.normal)
        );

        Destroy(fx, hitEffectDuration);
    }
}