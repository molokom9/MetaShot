using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorButton : MonoBehaviour
{
    [Header("=== ДВЕРИ ===")]
    public List<Transform> doors = new List<Transform>();
    public float doorOpenHeight = 3f;
    public float doorOpenDuration = 1.5f;
    public AnimationCurve doorOpenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("=== КНОПКА — ВИД ===")]
    public Renderer buttonRenderer;
    public Material idleMaterial;
    public Material pressedMaterial;

    [Header("=== ВЗАИМОДЕЙСТВИЕ ===")]
    public Transform cameraTransform;
    public float interactDistance = 3f;
    public LayerMask interactLayers = ~0;
    public KeyCode interactKey = KeyCode.E;
    public bool allowMouseClick = true;
    public GameObject hintObject;

    [Header("=== ЗВУК ===")]
    public AudioClip pressSound;
    [Range(0f, 1f)] public float soundVolume = 1f;
    public AudioClip doorOpenSound;
    [Range(0f, 1f)] public float doorSoundVolume = 1f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private bool isOpened = false;
    private AudioSource audioSource;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        if (buttonRenderer != null && idleMaterial != null)
            buttonRenderer.material = idleMaterial;

        if (hintObject != null) hintObject.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    void Update()
    {
        if (isOpened)
        {
            if (hintObject != null && hintObject.activeSelf) hintObject.SetActive(false);
            return;
        }

        if (Time.timeScale <= 0f) return;

        bool lookingAtButton = CanInteract();

        if (hintObject != null && lookingAtButton != hintObject.activeSelf)
            hintObject.SetActive(lookingAtButton);

        if (!lookingAtButton) return;

        // ✅ Взаимодействие: E, ПКМ или Кнопка X на геймпаде
        bool pressed = Input.GetKeyDown(interactKey) ||
                       (allowMouseClick && Input.GetMouseButtonDown(1)) ||
                       GamepadInput.X_Pressed;

        if (!pressed) return;

        OnPressed();
    }

    bool CanInteract()
    {
        if (cameraTransform == null) return false;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward,
            out RaycastHit hit, interactDistance, interactLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform) return true;
            if (hit.transform.IsChildOf(transform)) return true;
            if (hit.transform.parent == transform) return true;
        }
        return false;
    }

    void OnPressed()
    {
        if (isOpened) return;
        isOpened = true;

        if (buttonRenderer != null && pressedMaterial != null)
            buttonRenderer.material = pressedMaterial;

        if (pressSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(pressSound, soundVolume);
        }

        if (hintObject != null) hintObject.SetActive(false);
        StartCoroutine(OpenDoorsRoutine());
    }

    IEnumerator OpenDoorsRoutine()
    {
        if (doors == null || doors.Count == 0) yield break;

        List<Transform> validDoors = new List<Transform>();
        List<Vector3> startPositions = new List<Vector3>();

        for (int i = 0; i < doors.Count; i++)
        {
            Transform d = doors[i];
            if (d == null) continue;

            validDoors.Add(d);
            startPositions.Add(d.position);

            Rigidbody doorRb = d.GetComponent<Rigidbody>();
            if (doorRb != null)
            {
                doorRb.isKinematic = true;
                doorRb.useGravity = false;
            }
        }

        if (validDoors.Count == 0) yield break;

        if (doorOpenSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(doorOpenSound, doorSoundVolume);
        }

        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpenDuration);
            float curveT = doorOpenCurve.Evaluate(t);

            for (int i = 0; i < validDoors.Count; i++)
            {
                if (validDoors[i] == null) continue;
                Vector3 endPos = startPositions[i] + Vector3.up * doorOpenHeight;
                validDoors[i].position = Vector3.Lerp(startPositions[i], endPos, curveT);
            }
            yield return null;
        }

        for (int i = 0; i < validDoors.Count; i++)
        {
            if (validDoors[i] == null) continue;
            validDoors[i].position = startPositions[i] + Vector3.up * doorOpenHeight;
        }
    }
}