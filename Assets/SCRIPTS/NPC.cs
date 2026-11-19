using UnityEngine;

public class NPC : MonoBehaviour
{
    public enum State { Idle, Combat, Search, ReturnHome, Flee }
    public enum MoveMode { Stand, StrafeLeft, StrafeRight, Approach, Retreat }

    [Header("=== ССЫЛКИ ===")]
    public Renderer npcRenderer;
    public Transform eyePoint;
    public Transform firePoint;
    public Transform playerTarget;

    [Header("=== ТЕГИ ===")]
    public string playerTag = "Player";
    public string npcTag = "NPC";

    [Header("=== МАТЕРИАЛЫ ===")]
    public Material idleMaterial;
    public Material combatMaterial;
    public Material fireMaterial;
    public Material damageMaterial;

    [Header("=== ЗРЕНИЕ ===")]
    public float visionRadius = 15f;
    public LayerMask visionBlockLayers = ~0;

    [Header("=== ДВИЖЕНИЕ В БОЮ ===")]
    public float minDistance = 4f;
    public float maxDistance = 10f;
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float rotationSpeed = 6f;

    [Header("=== ПОВЕДЕНИЕ ===")]
    public float decisionInterval = 1.2f;
    [Range(0f, 1f)] public float standChance = 0.15f;
    [Range(0f, 1f)] public float strafeChance = 0.6f;
    public float alwaysApproachDistance = 12f;
    public float searchDuration = 7f;

    [Header("=== МАНСИНГ / УВОРОТ ===")]
    [Range(0f, 1f)] public float dodgeSmoothness = 0.85f;
    public float dodgeStrength = 1.4f;
    public float strafeChangeIntervalMin = 0.4f;
    public float strafeChangeIntervalMax = 1.2f;

    [Header("=== ПОБЕГ (LOW HP) ===")]
    public float fleeHealthThreshold = 30f;
    [Range(0f, 1f)] public float fleeChance = 0.5f;
    public float fleeSpeed = 5f;
    public float fleeStopDistance = 20f;
    public LayerMask obstacleLayers = ~0;
    public float obstacleCheckDistance = 1.5f;
    public float fleeDuration = 8f;

    [Header("=== СТРЕЛЬБА ===")]
    public float fireRate = 1.5f;
    public float fireMaterialDuration = 0.1f;
    public float shootRange = 100f;
    public float minBulletDamage = 5f;
    public float maxBulletDamage = 10f;
    [Range(0f, 1f)] public float missChanceAtMaxRange = 0.6f;
    public float damageMultiplierVsNPC = 2f;
    public LayerMask bulletHitLayers = ~0;

    [Header("=== ЗДОРОВЬЕ ===")]
    public float maxHealth = 100f;
    public float damageFlashDuration = 0.2f;

    [Header("=== СМЕРТЬ ===")]
    public GameObject corpsePrefab;
    public bool spawnCorpseOnDeath = true;
    public float destroyDelay = 0.05f;
    public float destroyDelayAfterDeath = 0.1f;

    [Header("=== ДРОП ПРЕДМЕТОВ ===")]
    public GameObject[] dropPrefabs;
    [Range(0f, 1f)] public float dropChance = 0.5f;
    public float dropRadius = 0.5f;
    public float dropUpForce = 3f;
    public float dropSideForce = 2f;
    public float dropLifetime = 0f;

    [Header("=== ФРАЗЫ ПРИ БОЕ (ЗВУКИ) ===")]
    public AudioClip[] combatPhrases;
    public float phraseInterval = 3f;
    [Range(0f, 1f)] public float phraseVolume = 1f;
    public float minPhrasePitch = 0.9f;
    public float maxPhrasePitch = 1.1f;

    [Header("=== ФРАЗЫ ПРИ СМЕРТИ (ЗВУКИ) ===")]
    public AudioClip[] deathPhrases;
    [Range(0f, 1f)] public float deathPhraseVolume = 1f;
    public float minDeathPhrasePitch = 0.95f;
    public float maxDeathPhrasePitch = 1.05f;

    [Header("=== ЗВУК ===")]
    public AudioClip shootSound;
    [Range(0f, 1f)] public float shootVolume = 1f;
    public float minShootPitch = 0.9f;
    public float maxShootPitch = 1.1f;

    public AudioClip damageSound;
    [Range(0f, 1f)] public float damageVolume = 1f;
    public float minDamagePitch = 0.9f;
    public float maxDamagePitch = 1.1f;

    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathVolume = 1f;
    public float minDeathPitch = 0.9f;
    public float maxDeathPitch = 1.1f;

    [Header("=== СЛЫШИМОСТЬ ===")]
    public float maxHearDistance = 25f;
    [Range(0f, 1f)] public float soundFalloff = 1f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    public static event System.Action<NPC> OnAnyNpcDied;

    // === СОСТОЯНИЕ ===
    private State state = State.Idle;
    private Transform currentTarget;
    private bool targetIsPlayer = false;

    private float currentHealth;
    private float decisionTimer = 0f;
    private MoveMode moveMode = MoveMode.Stand;

    private Vector3 currentMoveDir = Vector3.zero;
    private Vector3 targetMoveDir = Vector3.zero;

    private float strafeChangeTimer = 0f;
    private float fleeTimer = 0f;
    private float fleeObstacleTurnTimer = 0f;
    private float fleeTurnDirection = 1f;

    private float nextFireTime = 0f;
    private float fireFlashTimer = 0f;
    private float damageFlashTimer = 0f;

    private float nextPhraseTime = 0f;

    private bool isDead = false;

    private Vector3 lastSeenPlayerPosition;
    private bool hasLastSeen = false;

    private float searchTimer = 0f;
    private Vector3 homePosition;

    private Material runtimeIdleMat;
    private Material runtimeCombatMat;
    private Material runtimeFireMat;
    private Material runtimeDamageMat;

    private Material currentMat;

    private Renderer cachedRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        homePosition = transform.position;

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) playerTarget = p.transform;
        }

        if (eyePoint == null) eyePoint = transform;
        if (firePoint == null) firePoint = transform;

        if (npcRenderer == null)
            npcRenderer = GetComponentInChildren<Renderer>();

        cachedRenderer = npcRenderer;

        runtimeIdleMat = idleMaterial != null ? new Material(idleMaterial) : null;
        runtimeCombatMat = combatMaterial != null ? new Material(combatMaterial) : null;
        runtimeFireMat = fireMaterial != null ? new Material(fireMaterial) : null;

        runtimeDamageMat = damageMaterial != null ? new Material(damageMaterial)
                        : (runtimeCombatMat != null ? new Material(runtimeCombatMat) : null);

        SetMaterial(runtimeIdleMat);

        strafeChangeTimer = Random.Range(strafeChangeIntervalMin, strafeChangeIntervalMax);
        nextPhraseTime = Time.time + Random.Range(0.5f, phraseInterval);
    }

    void Update()
    {
        if (isDead) return;

        UpdateMaterialTimers();
        UpdatePhrases();

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Combat: UpdateCombat(); break;
            case State.Search: UpdateSearch(); break;
            case State.ReturnHome: UpdateReturnHome(); break;
            case State.Flee: UpdateFlee(); break;
        }
    }

    void UpdateIdle()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) playerTarget = p.transform;
        }

        if (playerTarget != null && CanSee(playerTarget))
        {
            lastSeenPlayerPosition = playerTarget.position;
            hasLastSeen = true;
            EnterCombat(playerTarget, true);
        }
    }

    void UpdateSearch()
    {
        searchTimer -= Time.deltaTime;

        Vector3 dir = lastSeenPlayerPosition - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 1f)
        {
            if (searchTimer <= 0f) EnterReturnHome();
            return;
        }

        dir.Normalize();
        transform.position += dir * moveSpeed * Time.deltaTime;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);

        if (playerTarget != null && CanSee(playerTarget))
        {
            lastSeenPlayerPosition = playerTarget.position;
            hasLastSeen = true;
            EnterCombat(playerTarget, true);
            return;
        }

        if (searchTimer <= 0f) EnterReturnHome();
    }

    void UpdateReturnHome()
    {
        Vector3 dir = homePosition - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 1f)
        {
            state = State.Idle;
            hasLastSeen = false;
            SetMaterial(runtimeIdleMat);
            return;
        }

        dir.Normalize();
        transform.position += dir * moveSpeed * Time.deltaTime;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);

        if (playerTarget != null && CanSee(playerTarget))
        {
            lastSeenPlayerPosition = playerTarget.position;
            hasLastSeen = true;
            EnterCombat(playerTarget, true);
        }
    }

    void EnterReturnHome()
    {
        state = State.ReturnHome;
        if (debugLogs) Debug.Log($"[NPC {name}] Возвращаюсь домой");
    }

    void UpdateFlee()
    {
        fleeTimer -= Time.deltaTime;

        float distToPlayer = playerTarget != null
            ? Vector3.Distance(transform.position, playerTarget.position)
            : 999f;

        if (fleeTimer <= 0f || distToPlayer >= fleeStopDistance)
        {
            if (playerTarget != null && CanSee(playerTarget))
            {
                EnterCombat(playerTarget, true);
            }
            else
            {
                ExitCombat();
            }
            return;
        }

        Vector3 awayDir;
        if (playerTarget != null)
        {
            awayDir = transform.position - playerTarget.position;
            awayDir.y = 0f;
            awayDir.Normalize();
        }
        else
        {
            awayDir = transform.forward;
        }

        Vector3 finalDir = AvoidObstacles(awayDir);

        transform.position += finalDir * fleeSpeed * Time.deltaTime;

        if (finalDir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(finalDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * 1.5f * Time.deltaTime);
        }

        if (playerTarget != null && CanSee(playerTarget))
        {
            HandleFleeShooting();
        }
    }

    Vector3 AvoidObstacles(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, desiredDir, out RaycastHit hit, obstacleCheckDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 right = Vector3.Cross(Vector3.up, desiredDir);

            fleeObstacleTurnTimer -= Time.deltaTime;
            if (fleeObstacleTurnTimer <= 0f)
            {
                fleeObstacleTurnTimer = Random.Range(0.4f, 1f);
                fleeTurnDirection = Random.value < 0.5f ? -1f : 1f;
            }

            Vector3 leftDir = -right;
            Vector3 rightDir = right;

            bool leftBlocked = Physics.Raycast(origin, leftDir, obstacleCheckDistance, obstacleLayers, QueryTriggerInteraction.Ignore);
            bool rightBlocked = Physics.Raycast(origin, rightDir, obstacleCheckDistance, obstacleLayers, QueryTriggerInteraction.Ignore);

            Vector3 sideDir;
            if (!leftBlocked && !rightBlocked)
                sideDir = fleeTurnDirection > 0 ? rightDir : leftDir;
            else if (!leftBlocked)
                sideDir = leftDir;
            else if (!rightBlocked)
                sideDir = rightDir;
            else
                sideDir = -desiredDir;

            return (desiredDir * 0.3f + sideDir * 0.7f).normalized;
        }

        return desiredDir;
    }

    void HandleFleeShooting()
    {
        if (Time.time < nextFireTime) return;
        if (currentTarget == null) return;
        if (damageFlashTimer > 0f) return;
        if (!CanSee(currentTarget)) return;
        if (!HasLineOfSight(currentTarget)) return;

        nextFireTime = Time.time + fireRate * 2f;
        Shoot();
    }

    void UpdateCombat()
    {
        if (currentTarget == null) { ExitCombat(); return; }

        if (targetIsPlayer)
        {
            if (!currentTarget.CompareTag(playerTag))
            {
                ExitCombat();
                return;
            }

            PlayerHealth ph = currentTarget.GetComponent<PlayerHealth>();
            if (ph != null && ph.IsDead()) { ExitCombat(); return; }

            if (CanSee(currentTarget))
            {
                lastSeenPlayerPosition = currentTarget.position;
                hasLastSeen = true;
            }
            else
            {
                if (hasLastSeen) { EnterSearch(); return; }
                else { ExitCombat(); return; }
            }

            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist > visionRadius * 1.5f)
            {
                if (hasLastSeen) { EnterSearch(); return; }
                ExitCombat();
                return;
            }
        }
        else
        {
            // ✅ Цель — другой NPC
            NPC other = currentTarget.GetComponent<NPC>();
            if (other == null || other.IsDead())
            {
                // Цель умерла — ищем новую
                if (playerTarget != null && CanSee(playerTarget))
                {
                    currentTarget = playerTarget;
                    targetIsPlayer = true;
                }
                else
                {
                    // Ищем ближайшего NPC-врага
                    NPC newTarget = FindNearestEnemyNPC();
                    if (newTarget != null)
                    {
                        currentTarget = newTarget.transform;
                        targetIsPlayer = false;
                        lastSeenPlayerPosition = newTarget.transform.position;
                        hasLastSeen = true;
                    }
                    else
                    {
                        ExitCombat();
                        return;
                    }
                }
            }
            else
            {
                if (!CanSee(currentTarget))
                {
                    if (hasLastSeen) { EnterSearch(); return; }
                    else { ExitCombat(); return; }
                }
                else
                {
                    lastSeenPlayerPosition = currentTarget.position;
                    hasLastSeen = true;
                }
            }
        }

        FaceTarget();

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            DecideMove();
        }

        strafeChangeTimer -= Time.deltaTime;
        if (strafeChangeTimer <= 0f)
        {
            strafeChangeTimer = Random.Range(strafeChangeIntervalMin, strafeChangeIntervalMax);
            if (moveMode == MoveMode.StrafeLeft) moveMode = MoveMode.StrafeRight;
            else if (moveMode == MoveMode.StrafeRight) moveMode = MoveMode.StrafeLeft;
        }

        UpdateMovementDirection();
        ApplyMovement();
        HandleShooting();
    }

    // ✅ Поиск ближайшего живого NPC (для продолжения боя после смерти цели)
    NPC FindNearestEnemyNPC()
    {
        NPC[] allNPCs = FindObjectsOfType<NPC>();
        NPC nearest = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < allNPCs.Length; i++)
        {
            NPC n = allNPCs[i];
            if (n == null || n == this) continue;
            if (n.IsDead()) continue;

            float d = Vector3.Distance(transform.position, n.transform.position);
            if (d < bestDist && d <= visionRadius)
            {
                bestDist = d;
                nearest = n;
            }
        }

        return nearest;
    }

    void EnterSearch()
    {
        state = State.Search;
        searchTimer = searchDuration;
        moveMode = MoveMode.Stand;

        if (debugLogs) Debug.Log($"[NPC {name}] Ищу цель у {lastSeenPlayerPosition}");
    }

    void DecideMove()
    {
        float dist = Vector3.Distance(transform.position, currentTarget.position);
        float edgeOfVision = visionRadius * 0.75f;

        if (dist > alwaysApproachDistance)
        {
            moveMode = MoveMode.Approach;
            return;
        }

        float localStand = standChance;
        float localStrafe = strafeChance;

        if (dist > edgeOfVision)
        {
            localStand = 0.3f;
            localStrafe = 0.5f;
        }

        float r = Random.value;

        if (dist > maxDistance)
        {
            if (r < 0.55f) moveMode = MoveMode.Approach;
            else if (r < 0.95f) moveMode = Random.value < 0.5f ? MoveMode.StrafeLeft : MoveMode.StrafeRight;
            else moveMode = MoveMode.Stand;
            return;
        }

        if (dist < minDistance)
        {
            if (r < 0.5f) moveMode = MoveMode.Retreat;
            else if (r < 0.95f) moveMode = Random.value < 0.5f ? MoveMode.StrafeLeft : MoveMode.StrafeRight;
            else moveMode = MoveMode.Stand;
            return;
        }

        if (r < localStand) moveMode = MoveMode.Stand;
        else if (r < localStand + localStrafe)
            moveMode = Random.value < 0.5f ? MoveMode.StrafeLeft : MoveMode.StrafeRight;
        else moveMode = MoveMode.Approach;

        if (dist < minDistance) moveMode = MoveMode.Retreat;
    }

    void UpdateMovementDirection()
    {
        if (currentTarget == null)
        {
            targetMoveDir = Vector3.zero;
            return;
        }

        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
        {
            targetMoveDir = Vector3.zero;
            return;
        }
        toTarget.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, toTarget);

        Vector3 dir = Vector3.zero;
        switch (moveMode)
        {
            case MoveMode.Stand: dir = Vector3.zero; break;
            case MoveMode.StrafeLeft: dir = -right * dodgeStrength; break;
            case MoveMode.StrafeRight: dir = right * dodgeStrength; break;
            case MoveMode.Approach: dir = toTarget; break;
            case MoveMode.Retreat: dir = -toTarget * dodgeStrength; break;
        }

        targetMoveDir = dir;
    }

    void ApplyMovement()
    {
        if (currentTarget == null) return;

        float speed = (moveMode == MoveMode.Approach) ? chaseSpeed : moveSpeed;

        float smooth = Mathf.Lerp(15f, 3f, dodgeSmoothness);
        currentMoveDir = Vector3.Lerp(currentMoveDir, targetMoveDir, Time.deltaTime * smooth);

        if (currentMoveDir.sqrMagnitude < 0.0001f) return;

        transform.position += currentMoveDir * speed * Time.deltaTime;
    }

    void FaceTarget()
    {
        if (currentTarget == null) return;

        Vector3 dir = currentTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
    }

    void HandleShooting()
    {
        if (Time.time < nextFireTime) return;
        if (currentTarget == null) return;
        if (damageFlashTimer > 0f) return;
        if (!CanSee(currentTarget)) return;
        if (!HasLineOfSight(currentTarget)) return;

        nextFireTime = Time.time + fireRate;
        Shoot();
    }

    bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;
        if (firePoint == null) return false;

        Vector3 origin = firePoint.position;
        Vector3 targetPoint = target.position + Vector3.up * 1f;
        Vector3 dir = targetPoint - origin;
        float dist = dir.magnitude;

        if (dist > shootRange) return false;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, visionBlockLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == target || hit.transform.IsChildOf(target))
                return true;

            return false;
        }

        return true;
    }

    float GetMissChance(float distance)
    {
        if (distance <= minDistance) return 0f;

        float maxRange = visionRadius;
        if (distance >= maxRange) return missChanceAtMaxRange;

        float t = (distance - minDistance) / Mathf.Max(0.001f, maxRange - minDistance);
        return Mathf.Lerp(0f, missChanceAtMaxRange, t);
    }

    // ✅ ПОЛНОСТЬЮ ПЕРЕПИСАН Shoot — правильно находит цель и агрит NPC
    void Shoot()
    {
        if (firePoint == null) return;
        if (currentTarget == null) return;

        if (damageFlashTimer <= 0f)
        {
            SetMaterial(runtimeFireMat);
            fireFlashTimer = fireMaterialDuration;
        }

        PlaySoundSpatial(shootSound, shootVolume, minShootPitch, maxShootPitch);

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        float currentMissChance = GetMissChance(dist);

        if (Random.value < currentMissChance)
        {
            if (debugLogs) Debug.Log($"[NPC {name}] Промах! (дист={dist:F1})");
            return;
        }

        Vector3 targetPoint = currentTarget.position + Vector3.up * 1f;
        Vector3 dir = (targetPoint - firePoint.position).normalized;

        float rollDamage = Random.Range(minBulletDamage, maxBulletDamage);

        // ✅ RaycastAll — ищем первый НЕ-свой объект
        int layerMask = bulletHitLayers.value;
        RaycastHit[] hits = Physics.RaycastAll(firePoint.position, dir, shootRange, layerMask, QueryTriggerInteraction.Ignore);

        // Сортируем по дистанции
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Transform hitT = hit.collider.transform;

            // ✅ Пропускаем СВОИ коллайдеры (свои и дети)
            if (hitT == transform || hitT.IsChildOf(transform))
                continue;

            // ✅ Игрок?
            if (hit.collider.CompareTag(playerTag))
            {
                hit.collider.SendMessage("TakeDamage", rollDamage, SendMessageOptions.DontRequireReceiver);

                if (debugLogs) Debug.Log($"[NPC {name}] Попал в игрока! Урон={rollDamage:F1}");
                return;
            }

            // ✅ Другой NPC?
            NPC npc = hit.collider.GetComponentInParent<NPC>();
            if (npc == null)
                npc = hit.collider.GetComponent<NPC>();
            if (npc == null)
                npc = hit.collider.transform.root.GetComponentInChildren<NPC>();

            if (npc != null && npc != this && !npc.IsDead())
            {
                float npcDamage = rollDamage * damageMultiplierVsNPC;
                npc.TakeDamage(npcDamage, gameObject);

                if (debugLogs) Debug.Log($"[NPC {name}] Попал в {npc.name}. Урон={npcDamage:F1} (x{damageMultiplierVsNPC})");
                return;
            }

            // ✅ Стена — прекращаем
            if (debugLogs) Debug.Log($"[NPC {name}] Попал в стену: {hit.collider.name}");
            return;
        }
    }

    bool CanSee(Transform target)
    {
        if (target == null) return false;

        Vector3 eyePos = eyePoint.position;
        Vector3 targetPos = target.position + Vector3.up * 1f;

        Vector3 dir = targetPos - eyePos;
        float dist = dir.magnitude;
        if (dist > visionRadius) return false;

        if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dist, visionBlockLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
                return false;
        }

        return true;
    }

    void EnterCombat(Transform target, bool isPlayer)
    {
        state = State.Combat;
        currentTarget = target;
        targetIsPlayer = isPlayer;
        decisionTimer = 0f;
        currentMoveDir = Vector3.zero;
        targetMoveDir = Vector3.zero;

        if (damageFlashTimer <= 0f && fireFlashTimer <= 0f)
            SetMaterial(runtimeCombatMat);
    }

    void ExitCombat()
    {
        if (hasLastSeen && targetIsPlayer)
        {
            EnterSearch();
            return;
        }

        state = State.Idle;
        currentTarget = null;
        targetIsPlayer = false;
        moveMode = MoveMode.Stand;
        currentMoveDir = Vector3.zero;
        targetMoveDir = Vector3.zero;

        if (damageFlashTimer <= 0f && fireFlashTimer <= 0f)
            SetMaterial(runtimeIdleMat);
    }

    void EnterFlee()
    {
        state = State.Flee;
        fleeTimer = fleeDuration;
        fleeObstacleTurnTimer = 0f;
        currentMoveDir = Vector3.zero;
        targetMoveDir = Vector3.zero;

        if (playerTarget != null)
            currentTarget = playerTarget;

        if (debugLogs) Debug.Log($"[NPC {name}] Убегаю! HP={currentHealth}");
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (amount <= 0f) return;
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;

        if (runtimeDamageMat != null)
        {
            SetMaterial(runtimeDamageMat);
            damageFlashTimer = damageFlashDuration;
        }

        PlaySoundSpatial(damageSound, damageVolume, minDamagePitch, maxDamagePitch);

        if (debugLogs) Debug.Log($"[NPC {name}] Урон {amount}. HP={currentHealth}. Источник: {(source != null ? source.name : "нет")}");

        // ✅ Побег от игрока
        if (source != null && source.CompareTag(playerTag))
        {
            if (currentHealth > 0f && currentHealth <= fleeHealthThreshold)
            {
                if (Random.value < fleeChance)
                {
                    EnterFlee();
                    return;
                }
            }
        }

        // ✅ Агро на источник
        if (source != null && source != gameObject)
        {
            NPC sourceNPC = source.GetComponent<NPC>();
            if (sourceNPC == null)
                sourceNPC = source.GetComponentInParent<NPC>();

            if (sourceNPC != null && sourceNPC != this)
            {
                // Другой NPC стрелял → агримся на него
                if (state != State.Combat || currentTarget != sourceNPC.transform)
                    EnterCombat(sourceNPC.transform, false);

                lastSeenPlayerPosition = sourceNPC.transform.position;
                hasLastSeen = true;

                if (debugLogs) Debug.Log($"[NPC {name}] Агрюсь на NPC: {sourceNPC.name}");
            }
            else if (source.CompareTag(playerTag))
            {
                if (playerTarget != null)
                {
                    lastSeenPlayerPosition = playerTarget.position;
                    hasLastSeen = true;
                    EnterCombat(playerTarget, true);
                }
            }
        }
        else
        {
            if (playerTarget != null && !CanSee(playerTarget))
            {
                lastSeenPlayerPosition = playerTarget.position;
                hasLastSeen = true;
                EnterSearch();
            }
            else if (playerTarget != null)
            {
                lastSeenPlayerPosition = playerTarget.position;
                hasLastSeen = true;
                EnterCombat(playerTarget, true);
            }
        }

        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // ✅ Сообщаем всем слушателям (BossDeathActivator и т.п.), что этот NPC умер.
        // Срабатывает РОВНО ОДИН РАЗ на этого NPC, до уничтожения объекта.
        OnAnyNpcDied?.Invoke(this);

        PlaySoundDetached(deathSound, deathVolume, minDeathPitch, maxDeathPitch);

        if (deathPhrases != null && deathPhrases.Length > 0)
        {
            int idx = Random.Range(0, deathPhrases.Length);
            AudioClip phrase = deathPhrases[idx];

            if (phrase != null)
                PlaySoundDetached(phrase, deathPhraseVolume, minDeathPhrasePitch, maxDeathPhrasePitch);
        }

        if (spawnCorpseOnDeath && corpsePrefab != null)
            Instantiate(corpsePrefab, transform.position, transform.rotation);

        SpawnDrop();

        float delay = destroyDelay > 0f ? destroyDelay : destroyDelayAfterDeath;
        Destroy(gameObject, delay);
    }

    void SpawnDrop()
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;
        if (Random.value > dropChance) return;

        int index = Random.Range(0, dropPrefabs.Length);
        GameObject prefab = dropPrefabs[index];

        if (prefab == null) return;

        Vector2 randomCircle = Random.insideUnitCircle * dropRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

        GameObject drop = Instantiate(prefab, spawnPos, Quaternion.identity);

        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb == null)
            rb = drop.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            Vector3 upForce = Vector3.up * dropUpForce;
            Vector3 sideForce = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ).normalized * dropSideForce;

            rb.AddForce(upForce + sideForce, ForceMode.Impulse);
        }

        if (dropLifetime > 0f)
            Destroy(drop, dropLifetime);
    }

    public bool IsDead() => isDead;

    void SetMaterial(Material mat)
    {
        if (cachedRenderer == null) return;
        if (mat == null) return;
        if (currentMat == mat) return;

        currentMat = mat;
        cachedRenderer.material = mat;
    }

    Material GetDefaultMaterial()
    {
        return state == State.Combat ? runtimeCombatMat : runtimeIdleMat;
    }

    void UpdateMaterialTimers()
    {
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;

            if (damageFlashTimer <= 0f)
            {
                if (fireFlashTimer <= 0f)
                    SetMaterial(GetDefaultMaterial());
            }
            return;
        }

        if (fireFlashTimer > 0f)
        {
            fireFlashTimer -= Time.deltaTime;
            if (fireFlashTimer <= 0f)
                SetMaterial(GetDefaultMaterial());
        }
    }

    void UpdatePhrases()
    {
        if (state != State.Combat) return;
        if (combatPhrases == null || combatPhrases.Length == 0) return;

        if (Time.time < nextPhraseTime) return;

        nextPhraseTime = Time.time + phraseInterval;

        int idx = Random.Range(0, combatPhrases.Length);
        AudioClip phrase = combatPhrases[idx];

        if (phrase == null) return;

        PlaySoundSpatial(phrase, phraseVolume, minPhrasePitch, maxPhrasePitch);
    }

    Transform GetPlayerForSound()
    {
        if (playerTarget != null) return playerTarget;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            playerTarget = p.transform;
            return playerTarget;
        }

        return null;
    }

    void PlaySoundSpatial(AudioClip clip, float baseVolume, float minPitch, float maxPitch)
    {
        if (clip == null) return;

        Transform player = GetPlayerForSound();
        float volumeMultiplier = 1f;

        if (player != null && maxHearDistance > 0f)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > maxHearDistance) return;

            float t = 1f - (dist / maxHearDistance);
            t = Mathf.Clamp01(t);
            volumeMultiplier = Mathf.Lerp(t, 1f, 1f - soundFalloff);
        }

        float finalVolume = baseVolume * volumeMultiplier;
        if (finalVolume <= 0.001f) return;

        GameObject go = new GameObject("NPCSound");
        go.transform.position = transform.position;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = finalVolume;
        src.pitch = Random.Range(minPitch, maxPitch);
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }

    void PlaySoundDetached(AudioClip clip, float volume, float minPitch, float maxPitch)
    {
        if (clip == null) return;

        Transform player = GetPlayerForSound();
        float volumeMultiplier = 1f;

        if (player != null && maxHearDistance > 0f)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > maxHearDistance) return;

            float t = 1f - (dist / maxHearDistance);
            volumeMultiplier = Mathf.Clamp01(t);
        }

        float finalVolume = volume * volumeMultiplier;
        if (finalVolume <= 0.001f) return;

        GameObject go = new GameObject("NPCDeathSound");
        go.transform.position = transform.position;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = finalVolume;
        src.pitch = Random.Range(minPitch, maxPitch);
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minDistance);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxHearDistance);

        if (hasLastSeen)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastSeenPlayerPosition, 0.7f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawWireSphere(homePosition, 0.5f);
    }
}