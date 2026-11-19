using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using UnityEngine.EventSystems;
using System.Collections;

public class DeveloperConsole : MonoBehaviour
{
    [Header("=== UI ССЫЛКИ ===")]
    public GameObject consolePanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI logText;

    [Header("=== ЧАТ НА ЭКРАНЕ ===")]
    [Tooltip("Панель с компонентом Vertical Layout Group, куда будут добавляться сообщения")]
    public Transform chatContainer;
    [Tooltip("Шрифт для чата (если пусто, будет использован стандартный)")]
    public TMP_FontAsset chatFont;
    public float chatFontSize = 24f;
    [Tooltip("Сколько секунд сообщение висит на экране (до исчезновения)")]
    public float chatDisplayDuration = 3f;
    public Color chatColor = Color.white;

    [Header("=== ПРОКРУТКА (SCROLLBAR) ===")]
    public Scrollbar scrollbar;
    public RectTransform contentRect;
    public float viewportHeight = 300f;

    [Header("=== НАСТРОЙКИ ===")]
    public KeyCode toggleKey = KeyCode.F10;
    public int maxLogLines = 50;

    public string successColor = "#00FF00";
    public string errorColor = "#FF3333";
    public string infoColor = "#FFCC00";
    public string cmdColor = "#00FFFF";

    [Header("=== ИГРОК И ОРУЖИЕ ===")]
    public PlayerController player;
    public PlayerHealth playerHealth;
    public WeaponManager weaponManager;

    // === ВНУТРЕННИЕ ===
    private bool isOpen = false;
    private List<string> logLines = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;
    private Camera mainCamera;
    private float defaultFOV = 60f;
    private float contentHeight = 0f;

    private const string PREFS_PREFIX = "ConsoleSave_";
    private const string NICKNAME_KEY = "PlayerNickname";
    private string playerNickname = "player";

    private struct UndoAction
    {
        public Component comp;
        public FieldInfo field;
        public PropertyInfo prop;
        public object oldValue;
    }
    private List<UndoAction> undoStack = new List<UndoAction>();

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (weaponManager == null) weaponManager = FindObjectOfType<WeaponManager>();

        mainCamera = Camera.main;
        if (mainCamera != null) defaultFOV = mainCamera.fieldOfView;

        if (consolePanel != null) consolePanel.SetActive(false);

        if (inputField != null) inputField.onSubmit.AddListener(OnSubmit);

        if (scrollbar != null)
        {
            scrollbar.onValueChanged.AddListener(OnScrollbarChanged);
            scrollbar.value = 0f;
        }

        playerNickname = PlayerPrefs.GetString(NICKNAME_KEY, "player");

        ApplySavedSettings();

        LogInfo("Терминал загружен. Используйте <b>/help</b> для команд.");
        LogInfo("<color=#AAAAAA>Всё, что не начинается с '/', отправляется в чат.</color>");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) ToggleConsole();
        if (!isOpen) return;

        if (inputField != null && !inputField.isFocused)
        {
            inputField.ActivateInputField();
            inputField.caretPosition = inputField.text.Length;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && commandHistory.Count > 0)
        {
            if (historyIndex > 0) historyIndex--;
            else if (historyIndex == -1) historyIndex = commandHistory.Count - 1;

            inputField.text = commandHistory[historyIndex];
            inputField.caretPosition = inputField.text.Length;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) && commandHistory.Count > 0)
        {
            if (historyIndex < commandHistory.Count - 1) historyIndex++;
            else historyIndex = -1;

            inputField.text = historyIndex >= 0 ? commandHistory[historyIndex] : "";
            inputField.caretPosition = inputField.text.Length;
        }

        float scrollWheel = Input.mouseScrollDelta.y;
        if (scrollWheel != 0f && scrollbar != null)
        {
            scrollbar.value = Mathf.Clamp01(scrollbar.value + scrollWheel * 0.05f);
        }
    }

    public void ToggleConsole()
    {
        isOpen = !isOpen;
        consolePanel.SetActive(isOpen);

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (EventSystem.current != null)
                EventSystem.current.sendNavigationEvents = false;

            inputField.ActivateInputField();
            inputField.caretPosition = inputField.text.Length;

            historyIndex = -1;
            if (player != null) player.enabled = false;
            UpdateScrollbarBounds();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (EventSystem.current != null)
                EventSystem.current.sendNavigationEvents = true;

            inputField.text = "";
            if (player != null && (playerHealth == null || !playerHealth.IsDead()))
            {
                player.enabled = true;
                // Блокируем обзор на 0.2 секунды, чтобы камера не дернулась при захвате мыши
                player.BlockLook(0.2f);
            }
        }
    }

    void OnSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        string trimmed = text.Trim();
        commandHistory.Add(trimmed);
        historyIndex = -1;

        if (trimmed.StartsWith("/"))
        {
            Log($"<color={cmdColor}>@console~> {trimmed}</color>");
            ProcessCommand(trimmed.Substring(1));
        }
        else
        {
            SendChatMessage(trimmed);
        }

        inputField.text = "";
        inputField.ActivateInputField();
        inputField.caretPosition = 0;

        if (scrollbar != null) scrollbar.value = 0f;
    }

    // === СИСТЕМА ЧАТА ===
    // === СИСТЕМА ЧАТА ===
    void SendChatMessage(string message)
    {
        string formattedMessage = $"@{playerNickname}~: {message}";

        // Пишем в лог консоли
        Log($"<color=#{ColorUtility.ToHtmlStringRGB(chatColor)}>{formattedMessage}</color>");

        if (chatContainer != null)
        {
            GameObject msgObj = new GameObject("ChatMsg", typeof(RectTransform));
            msgObj.transform.SetParent(chatContainer, false);

            RectTransform rt = msgObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;

            LayoutElement layout = msgObj.AddComponent<LayoutElement>();
            layout.minHeight = chatFontSize + 10f;

            TextMeshProUGUI tmp = msgObj.AddComponent<TextMeshProUGUI>();
            tmp.text = formattedMessage;
            tmp.fontSize = chatFontSize;
            tmp.alignment = TextAlignmentOptions.Left;
            if (chatFont != null) tmp.font = chatFont;
            tmp.enableWordWrapping = true;

            // === ВОТ ТУТ МЫ ВЕШАЕМ ОТДЕЛЬНЫЙ СКРИПТ ===
            ChatMessageFade fadeScript = msgObj.AddComponent<ChatMessageFade>();
            fadeScript.StartFade(chatDisplayDuration, chatColor);
        }
    }

    IEnumerator FadeChatMessage(TextMeshProUGUI tmp, GameObject msgObj)
    {
        float elapsed = 0f;
        float fadeInDuration = 0.2f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, alpha);
            yield return null;
        }
        tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);

        yield return new WaitForSecondsRealtime(chatDisplayDuration);

        elapsed = 0f;
        float fadeOutDuration = 0.5f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, alpha);
            yield return null;
        }

        Destroy(msgObj);
    }

    // === ОБРАБОТКА КОМАНД ===
    void ProcessCommand(string input)
    {
        string[] parts = input.Split(' ');
        string cmd = parts[0].ToLower();

        try
        {
            switch (cmd)
            {
                case "help": LogHelp(); break;
                case "clear": ClearLog(); break;
                case "metashot": LogSuccess("https://qwile-software.itch.io/metashot"); break;

                case "nick":
                    if (parts.Length < 2) throw new Exception("Использование: /nick [новый_ник]");
                    playerNickname = parts[1];
                    PlayerPrefs.SetString(NICKNAME_KEY, playerNickname);
                    PlayerPrefs.Save();
                    LogSuccess($"Ник изменен на: @{playerNickname}~");
                    break;

                case "status":
                    LogInfo($"=== СТАТУС СИСТЕМЫ ===");
                    Log($"Ник: @{playerNickname}~");
                    Log($"FPS: {Mathf.RoundToInt(1f / Time.smoothDeltaTime)}");
                    Log($"Позиция: {player.transform.position}");
                    Log($"Время: {Time.time:F1} сек");
                    break;

                case "hack":
                    LogSuccess("ВЗЛОМ СИСТЕМЫ...");
                    LogSuccess("ДОСТУП РАЗРЕШЕН.");
                    LogSuccess("ДОБРО ПОЖАЛОВАТЬ, АГЕНТ.");
                    break;

                case "set_walk":
                    if (player == null) throw new Exception("Игрок не найден");
                    if (parts.Length < 2) throw new Exception("Использование: /set_walk [скорость]");
                    player.walkSpeed = float.Parse(parts[1]);
                    LogSuccess($"Скорость ходьбы: {player.walkSpeed}");
                    break;

                case "set_run":
                    if (player == null) throw new Exception("Игрок не найден");
                    if (parts.Length < 2) throw new Exception("Использование: /set_run [скорость]");
                    player.runSpeed = float.Parse(parts[1]);
                    LogSuccess($"Скорость бега: {player.runSpeed}");
                    break;

                case "heal":
                    if (playerHealth == null) throw new Exception("PlayerHealth не найден");
                    float healAmt = parts.Length >= 2 ? float.Parse(parts[1]) : 100f;
                    playerHealth.Heal(healAmt);
                    LogSuccess($"Восстановлено {healAmt} HP");
                    break;

                case "kill":
                    if (playerHealth == null) throw new Exception("PlayerHealth не найден");
                    bool oldGod = playerHealth.godMode;
                    playerHealth.godMode = false;
                    playerHealth.TakeDamage(99999f);
                    playerHealth.godMode = oldGod;
                    LogSuccess("Игрок убит");
                    break;

                case "fov":
                    if (mainCamera == null) throw new Exception("Камера не найдена");
                    if (parts.Length < 2) throw new Exception("Использование: /fov [число]");
                    mainCamera.fieldOfView = Mathf.Clamp(float.Parse(parts[1]), 30f, 120f);
                    LogSuccess($"FOV: {mainCamera.fieldOfView}");
                    break;

                case "reset_fov":
                    if (mainCamera != null) mainCamera.fieldOfView = defaultFOV;
                    LogSuccess("FOV сброшен");
                    break;

                case "noclip":
                    if (player != null)
                    {
                        player.ToggleNoclip();
                        LogSuccess($"Noclip: {(player.noclip ? "ВКЛ (E - вверх, Q - вниз)" : "ВЫКЛ")}");
                    }
                    break;

                case "god":
                    if (playerHealth != null)
                    {
                        playerHealth.godMode = !playerHealth.godMode;
                        LogSuccess($"God Mode: {(playerHealth.godMode ? "ВКЛ" : "ВЫКЛ")}");
                    }
                    break;

                case "tp":
                    if (player == null) throw new Exception("Игрок не найден");
                    if (parts.Length < 4) throw new Exception("Использование: /tp [x] [y] [z]");
                    player.Teleport(new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])), player.transform.eulerAngles.y);
                    LogSuccess("Телепорт выполнен");
                    break;

                case "timescale":
                    if (parts.Length < 2) throw new Exception("Использование: /timescale [число]");
                    float ts = float.Parse(parts[1]);
                    if (ts < 0) ts = 0;
                    Time.timeScale = ts;
                    LogSuccess($"TimeScale = {Time.timeScale}");
                    break;

                case "scene":
                    if (parts.Length < 2) throw new Exception("Использование: /scene [название]");
                    SceneManager.LoadScene(parts[1]);
                    break;

                case "spawn":
                    if (parts.Length < 2) throw new Exception("Использование: /spawn [имя_объекта_в_сцене]");
                    GameObject objToSpawn = GameObject.Find(parts[1]);
                    if (objToSpawn == null) throw new Exception($"Объект '{parts[1]}' не найден в сцене.");

                    Vector3 spawnPos = player != null ? player.transform.position + player.transform.forward * 3f : Vector3.zero;
                    Instantiate(objToSpawn, spawnPos, Quaternion.identity);
                    LogSuccess($"Заспавнен: {parts[1]}");
                    break;

                case "destroy":
                    if (parts.Length < 2) throw new Exception("Использование: /destroy [имя_объекта]");
                    GameObject objToDestroy = GameObject.Find(parts[1]);
                    if (objToDestroy == null) throw new Exception($"Объект '{parts[1]}' не найден.");
                    Destroy(objToDestroy);
                    LogSuccess($"Удалён: {parts[1]}");
                    break;

                case "gravity":
                    if (parts.Length < 2) throw new Exception("Использование: /gravity [число] (0 = нет гравитации)");
                    Physics.gravity = new Vector3(0, float.Parse(parts[1]), 0);
                    LogSuccess($"Гравитация изменена на: {Physics.gravity.y}");
                    break;

                case "unlock_all":
                    if (weaponManager == null) throw new Exception("WeaponManager не найден в сцене!");
                    foreach (var w in weaponManager.weapons)
                    {
                        weaponManager.UnlockWeapon(w.weaponId);
                    }
                    LogSuccess("Всё оружие разблокировано!");
                    break;

                case "add_ammo":
                    if (parts.Length < 3) throw new Exception("Использование: /add_ammo [тип] [кол-во]. Типы: pistol, shotgun, rifle");
                    string ammoType = parts[1].ToLower();
                    int ammoAmount = int.Parse(parts[2]);

                    Weapon[] allWeapons = FindObjectsOfType<Weapon>(true);
                    bool found = false;
                    foreach (var w in allWeapons)
                    {
                        if (w.ammoType == ammoType)
                        {
                            w.AddAmmo(ammoAmount);
                            found = true;
                            break;
                        }
                    }
                    if (found) LogSuccess($"+{ammoAmount} патронов '{ammoType}'");
                    else LogError($"Оружие с типом '{ammoType}' не найдено.");
                    break;

                case "kill_all":
                    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                    if (enemies.Length == 0) { LogInfo("Врагов с тегом 'Enemy' нет."); break; }
                    foreach (var e in enemies) Destroy(e);
                    LogSuccess($"Уничтожено врагов: {enemies.Length}");
                    break;

                case "set":
                    if (parts.Length < 5) throw new Exception("Использование: /set [объект] [скрипт] [поле] [значение]");
                    DoSet(parts[1], parts[2], parts[3], parts[4], true);
                    break;

                case "set_temp":
                    if (parts.Length < 5) throw new Exception("Использование: /set_temp [объект] [скрипт] [поле] [значение]");
                    DoSet(parts[1], parts[2], parts[3], parts[4], false);
                    break;

                case "get":
                    if (parts.Length < 4) throw new Exception("Использование: /get [объект] [скрипт] [поле]");
                    DoGet(parts[1], parts[2], parts[3]);
                    break;

                case "fields":
                    if (parts.Length < 3) throw new Exception("Использование: /fields [объект] [скрипт]");
                    DoListFields(parts[1], parts[2]);
                    break;

                case "clear_saves":
                    PlayerPrefs.DeleteAll();
                    LogSuccess("Все сохранения (включая консольные) удалены.");
                    break;

                case "undo":
                    DoUndo();
                    break;

                default:
                    LogError($"Неизвестная команда: /{cmd}. Введите /help.");
                    break;
            }
        }
        catch (Exception ex)
        {
            LogError($"Ошибка: {ex.Message}");
        }
    }

    // ==========================================
    // === СИСТЕМА СОХРАНЕНИЯ И РЕФЛЕКСИИ ===
    // ==========================================

    void DoSet(string objName, string compName, string fieldName, string valStr, bool saveToPrefs)
    {
        Component comp = FindComponent(objName, compName);
        FieldInfo field = comp.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        PropertyInfo prop = comp.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (field == null && prop == null) throw new Exception($"Поле '{fieldName}' не найдено. Используй '/fields {objName} {compName}' чтобы посмотреть.");

        Type t = field != null ? field.FieldType : prop.PropertyType;
        object converted = Convert.ChangeType(valStr, t);
        object oldVal = field != null ? field.GetValue(comp) : prop.GetValue(comp);

        undoStack.Add(new UndoAction { comp = comp, field = field, prop = prop, oldValue = oldVal });

        if (field != null) field.SetValue(comp, converted);
        else prop.SetValue(comp, converted);

        if (saveToPrefs)
        {
            string key = PREFS_PREFIX + objName + "_" + compName + "_" + fieldName;
            PlayerPrefs.SetString(key, valStr);
            PlayerPrefs.Save();
            LogSuccess($"Установлено {fieldName} = {valStr} (СОХРАНЕНО НАВСЕГДА)");
        }
        else
        {
            LogSuccess($"Установлено {fieldName} = {valStr} (временно)");
        }
    }

    void ApplySavedSettings()
    {
        if (player != null) LoadPrefsForComponent("Player", "PlayerController", player);
        if (playerHealth != null) LoadPrefsForComponent("Player", "PlayerHealth", playerHealth);
    }

    void LoadPrefsForComponent(string objName, string compName, Component comp)
    {
        if (PlayerPrefs.HasKey(PREFS_PREFIX + "Player_PlayerController_walkSpeed"))
        {
            player.walkSpeed = PlayerPrefs.GetFloat(PREFS_PREFIX + "Player_PlayerController_walkSpeed");
        }
        if (PlayerPrefs.HasKey(PREFS_PREFIX + "Player_PlayerController_runSpeed"))
        {
            player.runSpeed = PlayerPrefs.GetFloat(PREFS_PREFIX + "Player_PlayerController_runSpeed");
        }
    }

    void DoGet(string objName, string compName, string fieldName)
    {
        Component comp = FindComponent(objName, compName);
        FieldInfo field = comp.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        PropertyInfo prop = comp.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (field == null && prop == null) throw new Exception($"Поле '{fieldName}' не найдено.");
        object val = field != null ? field.GetValue(comp) : prop.GetValue(comp);
        LogSuccess($"{objName}.{compName}.{fieldName} = {val}");
    }

    void DoListFields(string objName, string compName)
    {
        Component comp = FindComponent(objName, compName);
        FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        LogInfo($"=== ПОЛЯ {objName} -> {compName} ===");
        foreach (var f in fields)
        {
            if (f.Name.StartsWith("m_")) continue;
            Log($"- <b>{f.Name}</b> ({f.FieldType.Name})");
        }
    }

    void DoUndo()
    {
        if (undoStack.Count == 0) { LogInfo("Нечего отменять."); return; }
        var last = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        if (last.field != null) last.field.SetValue(last.comp, last.oldValue);
        if (last.prop != null) last.prop.SetValue(last.comp, last.oldValue);
        LogSuccess($"Отменено изменение на {last.comp.gameObject.name}.{last.comp.GetType().Name}");
    }

    Component FindComponent(string objName, string compName)
    {
        GameObject target = GameObject.Find(objName);
        if (target == null) throw new Exception($"Объект '{objName}' не найден.");
        foreach (var c in target.GetComponents<Component>())
            if (c.GetType().Name.ToLower() == compName.ToLower()) return c;
        throw new Exception($"Компонент '{compName}' не найден на '{objName}'.");
    }

    // ==========================================
    // === UI И ПРОКРУТКА ===
    // ==========================================

    void UpdateScrollbarBounds()
    {
        if (contentRect != null && logText != null)
        {
            Canvas.ForceUpdateCanvases();
            contentHeight = logText.preferredHeight;

            if (scrollbar != null)
            {
                scrollbar.size = Mathf.Clamp01(viewportHeight / (contentHeight > 0 ? contentHeight : viewportHeight));
                scrollbar.value = 0f;
            }
        }
    }

    void OnScrollbarChanged(float value)
    {
        if (contentRect != null)
        {
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, maxScroll * value);
        }
    }

    void LogHelp()
    {
        LogInfo("========================================");
        LogInfo("               CONSOLE METASHOT v3.0    ");
        LogInfo("========================================");

        LogInfo("=== ОСНОВЫ ===");
        Log($"<color={cmdColor}>/help</color> - Список команд");
        Log($"<color={cmdColor}>/nick [имя]</color> - Сменить ник (сохраняется)");
        Log($"<color={cmdColor}>/status</color> - Информация о системе");
        Log($"<color={cmdColor}>/hack</color> - =)");

        LogInfo("=== ДВИЖЕНИЕ И ИГРОК ===");
        Log($"<color={cmdColor}>/set_walk [число]</color> - Скорость ходьбы");
        Log($"<color={cmdColor}>/set_run [число]</color> - Скорость бега");
        Log($"<color={cmdColor}>/noclip</color> - Полет сквозь стены (E/Q)");
        Log($"<color={cmdColor}>/tp [x] [y] [z]</color> - Телепорт");

        LogInfo("=== ЗДОРОВЬЕ И МИР ===");
        Log($"<color={cmdColor}>/god</color> - Бессмертие");
        Log($"<color={cmdColor}>/heal [число], /kill</color> - HP");
        Log($"<color={cmdColor}>/gravity [число]</color> - Изменить гравитацию");
        Log($"<color={cmdColor}>/timescale [число]</color> - Скорость игры");

        LogInfo("=== МОДИФИКАЦИЯ ===");
        Log($"<color={cmdColor}>/spawn [объект]</color> - Заспавнить копию объекта перед собой");
        Log($"<color={cmdColor}>/destroy [объект]</color> - Удалить объект");
        Log($"<color={cmdColor}>/unlock_all</color> - Открыть всё оружие");
        Log($"<color={cmdColor}>/add_ammo [тип] [кол]</color> - Дать патроны");

        LogInfo("=== РЕДАКТОР И СОХРАНЕНИЕ ===");
        Log($"<color={cmdColor}>/fields [объект] [скрипт]</color> - Список полей");
        Log($"<color={cmdColor}>/set [объект] [скрипт] [поле] [знач]</color> - ИЗМЕНИТЬ И СОХРАНИТЬ");
        Log($"<color={cmdColor}>/set_temp ...</color> - Изменить временно");
        Log($"<color={cmdColor}>/get [объект] [скрипт] [поле]</color> - Узнать значение");
        Log($"<color={cmdColor}>/undo</color> - Отменить изменение");
        LogInfo("========================================");
        UpdateScrollbarBounds();
    }

    public void Log(string msg)
    {
        logLines.Add(msg);
        if (logLines.Count > maxLogLines) logLines.RemoveAt(0);

        if (logText != null)
        {
            logText.text = string.Join("\n", logLines);
            UpdateScrollbarBounds();
        }
    }

    void LogSuccess(string msg) => Log($"<color={successColor}>{msg}</color>");
    void LogError(string msg) => Log($"<color={errorColor}>{msg}</color>");
    void LogInfo(string msg) => Log($"<color={infoColor}>{msg}</color>");

    void ClearLog()
    {
        logLines.Clear();
        if (logText != null) logText.text = "";
        UpdateScrollbarBounds();
        LogInfo("Терминал очищен.");
    }
}