using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    public Transform cameraTransform;

    [Header("=== СКОРОСТИ ===")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 7f;
    public float acceleration = 15f;

    [Header("=== СКОЛЬЖЕНИЕ ===")]
    public float slideFriction = 3f;
    public float slideThreshold = 0.2f;

    [Header("=== АНТИ-ЗАСТРЕВАНИЕ ===")]
    public bool unstuckEnabled = true;
    public float unstuckDelay = 0.4f;
    public float unstuckForce = 2.5f;

    [Header("=== РЕЖИМ ХОДЬБЫ/БЕГА ===")]
    public KeyCode toggleModeKey = KeyCode.CapsLock;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("=== ТРЯСКА КАМЕРЫ ===")]
    public bool enableHeadBob = true;
    public float walkBobStrength = 0.04f;
    public float runBobStrength = 0.08f;
    public float bobFrequency = 10f;
    public float bobResetSpeed = 12f;

    [Header("=== МЫШЬ ===")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;
    public bool lockCursor = true;

    [Header("=== ГЕЙМПАД ===")]
    public float gamepadLookSensitivity = 150f;
    public float stickDeadZone = 0.15f;
    public bool invertGamepadY = true;

    [Header("=== ЧИТЫ ===")]
    public bool noclip = false;

    [Header("=== СОХРАНЕНИЕ ===")]
    public bool loadPositionFromSave = true;

    [Header("=== СТАРТОВЫЙ ЧЁРНЫЙ ЭКРАН ===")]
    public Image startBlackPanel;
    public float startBlackScreenDelay = 0.5f;
    public float startFadeDuration = 1f;

    private Rigidbody rb;
    private Transform tr;
    private Vector3 cameraStartPos;
    private float bobTimer = 0f;
    private float pitch = 0f;
    private bool isRunMode = false;
    private float currentBobStrength;
    private float inputX;
    private float inputZ;
    private bool sprintHeld;
    private bool hasInput = false;
    private Vector3 moveDir;
    private Vector3 currentHorizontal;
    private Vector3 newVelocity;
    private float stuckTimer = 0f;
    private Vector3 lastFramePosition;
    private const float MOVE_EPSILON_SQR = 0.01f;
    private const float CAM_EPSILON_SQR = 0.000001f;

    private Collider[] playerColliders;
    private bool[] collidersEnabled;

    // === БЛОКИРОВКА ОБЗОРА ===
    private float blockLookUntil = 0f;

    void Start()
    {
        tr = transform;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.None;

        if (rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
            else if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
            cameraStartPos = cameraTransform.localPosition;

        isRunMode = false;
        currentBobStrength = walkBobStrength;

        playerColliders = GetComponentsInChildren<Collider>(true);
        collidersEnabled = new bool[playerColliders.Length];
        for (int i = 0; i < playerColliders.Length; i++)
            collidersEnabled[i] = playerColliders[i].enabled;

        if (loadPositionFromSave && !GameSave.CheckNewGamePending())
        {
            SaveData save = GameSave.Load();
            if (save != null)
                Teleport(new Vector3(save.posX, save.posY, save.posZ), save.rotY);
        }

        lastFramePosition = tr.position;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (startBlackPanel != null)
            StartCoroutine(StartBlackScreenRoutine());
    }

    System.Collections.IEnumerator StartBlackScreenRoutine()
    {
        if (!startBlackPanel.gameObject.activeSelf)
            startBlackPanel.gameObject.SetActive(true);

        Color c = startBlackPanel.color;
        c.a = 1f;
        startBlackPanel.color = c;

        if (startBlackScreenDelay > 0f)
            yield return new WaitForSeconds(startBlackScreenDelay);

        if (startFadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < startFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / startFadeDuration);
                Color c2 = startBlackPanel.color;
                c2.a = Mathf.Lerp(1f, 0f, t);
                startBlackPanel.color = c2;
                yield return null;
            }
        }

        Color c3 = startBlackPanel.color;
        c3.a = 0f;
        startBlackPanel.color = c3;
        startBlackPanel.gameObject.SetActive(false);
    }

    public void Teleport(Vector3 pos, float yaw)
    {
        tr.position = pos;
        tr.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (rb != null)
        {
            rb.position = pos;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        lastFramePosition = pos;
    }

    public void ToggleNoclip() => SetNoclip(!noclip);

    public void SetNoclip(bool state)
    {
        noclip = state;

        if (rb != null)
        {
            rb.useGravity = !state;
            rb.detectCollisions = !state;
        }

        if (playerColliders != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                if (playerColliders[i] != null)
                    playerColliders[i].enabled = state ? false : collidersEnabled[i];
            }
        }

        gameObject.tag = state ? "Untagged" : "Player";
    }

    // === НОВЫЙ МЕТОД ДЛЯ БЛОКИРОВКИ КАМЕРЫ ===
    public void BlockLook(float duration)
    {
        blockLookUntil = Time.time + duration;
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputZ = Input.GetAxisRaw("Vertical");
        sprintHeld = Input.GetKey(sprintKey);

        if (GamepadInput.PadAvailable)
        {
            Vector2 left = GamepadInput.LeftStick;
            if (left.magnitude > stickDeadZone)
            {
                inputX = left.x;
                inputZ = left.y;
            }

            if (GamepadInput.LT_Held || GamepadInput.L3_Pressed)
                sprintHeld = true;

            if (GamepadInput.L3_Pressed) isRunMode = !isRunMode;
        }

        HandleMouseLook();
        HandleModeToggle();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleUnstuck();
    }

    void LateUpdate()
    {
        HandleHeadBob();
    }

    void HandleMouseLook()
    {
        // Блокировка камеры после закрытия консоли
        if (Time.time < blockLookUntil) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        Vector2 right = GamepadInput.RightStick;
        float gpX = 0f, gpY = 0f;

        if (right.magnitude > stickDeadZone)
        {
            gpX = Mathf.Clamp(right.x, -1f, 1f) * gamepadLookSensitivity * Time.deltaTime;
            gpY = Mathf.Clamp(right.y, -1f, 1f) * gamepadLookSensitivity * Time.deltaTime;

            if (invertGamepadY) gpY = -gpY;
        }

        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(gpX) > 0.01f)
            tr.Rotate(0f, mouseX + gpX, 0f, Space.Self);

        if ((Mathf.Abs(mouseY) > 0.01f || Mathf.Abs(gpY) > 0.01f) && cameraTransform != null)
        {
            pitch -= (mouseY + gpY);
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
            Vector3 camEuler = cameraTransform.localEulerAngles;
            camEuler.x = pitch;
            cameraTransform.localEulerAngles = camEuler;
        }
    }

    void HandleModeToggle()
    {
        if (Input.GetKeyDown(toggleModeKey)) isRunMode = !isRunMode;
    }

    void HandleMovement()
    {
        if (noclip)
        {
            float flySpeed = (Input.GetKey(sprintKey) || GamepadInput.LT_Held) ? runSpeed * 2f : walkSpeed * 2f;
            Vector3 flyDir = new Vector3(inputX, 0f, inputZ);

            if (Input.GetKey(KeyCode.E) || GamepadInput.RT_Held) flyDir.y += 1f;
            if (Input.GetKey(KeyCode.Q) || GamepadInput.LT_Held) flyDir.y -= 1f;

            if (flyDir.sqrMagnitude > 1f) flyDir.Normalize();

            rb.velocity = transform.TransformDirection(flyDir * flySpeed);
            bobTimer = 0f;
            return;
        }

        hasInput = (Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputZ) > 0.01f);

        if (!hasInput)
        {
            moveDir = Vector3.zero;
        }
        else
        {
            moveDir = new Vector3(inputX, 0f, inputZ);
            float sqr = moveDir.x * moveDir.x + moveDir.z * moveDir.z;
            if (sqr > 1f)
            {
                float inv = 1f / Mathf.Sqrt(sqr);
                moveDir.x *= inv;
                moveDir.z *= inv;
            }

            Vector3 fwd = tr.forward;
            Vector3 right = tr.right;
            float mx = moveDir.x;
            float mz = moveDir.z;
            moveDir.x = right.x * mx + fwd.x * mz;
            moveDir.y = 0f;
            moveDir.z = right.z * mx + fwd.z * mz;
        }

        bool runningNow = isRunMode ^ sprintHeld;
        float targetSpeed = runningNow ? runSpeed : walkSpeed;

        Vector3 v = rb.velocity;
        currentHorizontal.x = v.x;
        currentHorizontal.y = 0f;
        currentHorizontal.z = v.z;

        float targetX = moveDir.x * targetSpeed;
        float targetZ = moveDir.z * targetSpeed;

        float dx = targetX - currentHorizontal.x;
        float dz = targetZ - currentHorizontal.z;
        float distSqr = dx * dx + dz * dz;

        float currentAccel = hasInput ? acceleration : slideFriction;
        float maxDelta = currentAccel * Time.fixedDeltaTime;
        float maxDeltaSqr = maxDelta * maxDelta;

        if (distSqr <= maxDeltaSqr)
        {
            newVelocity.x = targetX;
            newVelocity.z = targetZ;
        }
        else
        {
            float dist = Mathf.Sqrt(distSqr);
            float k = maxDelta / dist;
            newVelocity.x = currentHorizontal.x + dx * k;
            newVelocity.z = currentHorizontal.z + dz * k;
        }

        if (!hasInput && distSqr < slideThreshold * slideThreshold)
        {
            newVelocity.x = 0f;
            newVelocity.z = 0f;
        }

        newVelocity.y = v.y;
        rb.velocity = newVelocity;

        float sqrSpeed = newVelocity.x * newVelocity.x + newVelocity.z * newVelocity.z;
        bool isMoving = sqrSpeed > MOVE_EPSILON_SQR;

        currentBobStrength = runningNow ? runBobStrength : walkBobStrength;

        if (enableHeadBob && isMoving && hasInput)
            bobTimer += Time.fixedDeltaTime * bobFrequency;
        else
            bobTimer = 0f;
    }

    void HandleUnstuck()
    {
        if (noclip || !unstuckEnabled) return;
        if (!hasInput) { stuckTimer = 0f; return; }

        Vector3 delta = tr.position - lastFramePosition;
        delta.y = 0f;

        if (delta.sqrMagnitude < 0.0001f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= unstuckDelay)
            {
                Vector3 v = rb.velocity;
                v.y = unstuckForce;
                rb.velocity = v;
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;

        lastFramePosition = tr.position;
    }

    void HandleHeadBob()
    {
        if (cameraTransform == null) return;

        float targetX = cameraStartPos.x;
        float targetY = cameraStartPos.y;

        if (enableHeadBob && bobTimer > 0f)
        {
            float sin = Mathf.Sin(bobTimer);
            float cos = Mathf.Cos(bobTimer * 0.5f);
            targetX = cameraStartPos.x + cos * currentBobStrength * 0.5f;
            targetY = cameraStartPos.y + sin * currentBobStrength;
        }

        Vector3 camLocalPos = cameraTransform.localPosition;
        float dx = targetX - camLocalPos.x;
        float dy = targetY - camLocalPos.y;
        float dz = cameraStartPos.z - camLocalPos.z;

        if (dx * dx + dy * dy + dz * dz < CAM_EPSILON_SQR) return;

        float t = Mathf.Clamp01(Time.deltaTime * bobResetSpeed);
        camLocalPos.x += dx * t;
        camLocalPos.y += dy * t;
        camLocalPos.z += dz * t;
        cameraTransform.localPosition = camLocalPos;
    }
}