using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponManager : MonoBehaviour
{
    [Header("=== ОРУЖИЕ ===")]
    public List<Weapon> weapons = new List<Weapon>();

    [Header("=== КЛАВИШИ СЛОТОВ ===")]
    public KeyCode[] slotKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

    [Header("=== КОЛЕСО МЫШИ ===")]
    public bool useMouseWheel = true;

    [Header("=== ГЕЙМПАД ===")]
    public bool useGamepadSwitching = true;
    public bool invertGamepadSwitch = false;

    [Header("=== АВТО-ВЗЯТИЕ ===")]
    public bool autoEquipOnPickup = true;

    [Header("=== АВТО-РЕГИСТРАЦИЯ ===")]
    public bool autoRegisterToPlayerHealth = true;

    [Header("=== UI ПАТРОНЫ ===")]
    public Image ammoIconUI;
    public GameObject ammoIconContainer;
    public bool hideIconIfNoSprite = true;
    public Sprite[] ammoIcons;

    [Header("=== UI ОРУЖИЕ ===")]
    public Image weaponIconUI;
    public GameObject weaponIconContainer;
    public bool hideWeaponIconIfNoSprite = true;
    public Sprite[] weaponIcons;

    [Header("=== ПАТРОНЫ ===")]
    public List<string> ammoTypeKeys = new List<string>();
    public List<int> ammoTypeValues = new List<int>();

    private int currentIndex = -1;
    private bool[] unlocked;
    private PlayerHealth playerHealth;

    public Dictionary<string, int> ammoByType = new Dictionary<string, int>();

    void Start()
    {
        SyncDictFromInspector();
        int count = weapons.Count;
        unlocked = new bool[count];

        for (int i = 0; i < count; i++)
        {
            weapons[i].gameObject.SetActive(false);
            weapons[i].HideUI();
        }

        currentIndex = -1;

        SaveData save = GameSave.Load();
        if (save != null)
        {
            if (save.unlockedWeaponIds != null && save.unlockedWeaponIds.Count > 0)
            {
                for (int i = 0; i < count; i++)
                    if (weapons[i] != null && save.unlockedWeaponIds.Contains(weapons[i].weaponId))
                        unlocked[i] = true;
            }

            if (save.ammoTypes != null && save.ammoValues != null)
            {
                ammoByType.Clear();
                for (int i = 0; i < save.ammoTypes.Count; i++)
                {
                    if (i >= save.ammoValues.Count) break;
                    string type = save.ammoTypes[i];
                    if (string.IsNullOrEmpty(type)) continue;
                    ammoByType[type] = save.ammoValues[i];
                }
                SyncInspectorFromDict();
            }
        }

        if (ammoIconContainer == null && ammoIconUI != null) ammoIconContainer = ammoIconUI.gameObject;
        if (ammoIconContainer != null) ammoIconContainer.SetActive(false);

        if (weaponIconContainer == null && weaponIconUI != null) weaponIconContainer = weaponIconUI.gameObject;
        if (weaponIconContainer != null) weaponIconContainer.SetActive(false);

        for (int i = 0; i < count; i++)
            if (unlocked[i]) { SwitchTo(i); break; }

        for (int i = 0; i < count; i++)
            if (weapons[i] != null) weapons[i].UpdateAmmoUI();

        if (autoRegisterToPlayerHealth)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null) playerHealth.RegisterWeaponScript(this);
        }
    }

    public void SyncDictFromInspector()
    {
        ammoByType.Clear();
        int count = Mathf.Min(ammoTypeKeys.Count, ammoTypeValues.Count);
        for (int i = 0; i < count; i++)
        {
            string key = ammoTypeKeys[i];
            if (string.IsNullOrEmpty(key)) continue;
            ammoByType[key] = ammoTypeValues[i];
        }
    }

    public void SyncInspectorFromDict()
    {
        ammoTypeKeys.Clear();
        ammoTypeValues.Clear();
        foreach (var kv in ammoByType) { ammoTypeKeys.Add(kv.Key); ammoTypeValues.Add(kv.Value); }
    }

    void Update()
    {
        HandleNumberKeys();
        if (useMouseWheel) HandleMouseWheel();
        if (useGamepadSwitching) HandleGamepadSwitch();
    }

    void HandleNumberKeys()
    {
        for (int i = 0; i < slotKeys.Length && i < weapons.Count; i++)
            if (Input.GetKeyDown(slotKeys[i]) && unlocked[i]) SwitchTo(i);
    }

    void HandleMouseWheel()
    {
        if (currentIndex < 0)
        {
            for (int i = 0; i < weapons.Count; i++)
                if (unlocked[i]) { SwitchTo(i); return; }
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f) return;
        int dir = scroll > 0 ? -1 : 1;
        SwitchByDirection(dir);
    }

    void HandleGamepadSwitch()
    {
        if (!GamepadInput.PadAvailable) return;

        if (GamepadInput.RB_Pressed)
            SwitchByDirection(invertGamepadSwitch ? -1 : 1);

        if (GamepadInput.LB_Pressed)
            SwitchByDirection(invertGamepadSwitch ? 1 : -1);

        if (GamepadInput.DPadRight_Pressed)
            SwitchByDirection(1);

        if (GamepadInput.DPadLeft_Pressed)
            SwitchByDirection(-1);
    }

    void SwitchByDirection(int dir)
    {
        if (currentIndex < 0)
        {
            for (int i = 0; i < weapons.Count; i++)
                if (unlocked[i]) { SwitchTo(i); return; }
            return;
        }

        int next = currentIndex;
        for (int step = 0; step < weapons.Count; step++)
        {
            next = (next + dir + weapons.Count) % weapons.Count;
            if (unlocked[next]) break;
        }
        if (next != currentIndex) SwitchTo(next);
    }

    void SwitchTo(int index)
    {
        if (index < 0 || index >= weapons.Count) return;
        if (index == currentIndex) return;
        if (!unlocked[index]) return;

        if (currentIndex >= 0 && currentIndex < weapons.Count)
            weapons[currentIndex].gameObject.SetActive(false);

        weapons[index].gameObject.SetActive(true);
        currentIndex = index;

        UpdateWeaponIcon();
        UpdateAmmoIcon();
        weapons[index].UpdateAmmoUI();
    }

    void UpdateWeaponIcon()
    {
        if (weaponIconUI == null) return;
        if (currentIndex < 0 || currentIndex >= weapons.Count)
        {
            if (weaponIconContainer != null) weaponIconContainer.SetActive(false);
            return;
        }

        Sprite icon = (weaponIcons != null && currentIndex < weaponIcons.Length) ? weaponIcons[currentIndex] : null;
        if (icon == null)
        {
            if (weaponIconContainer != null) weaponIconContainer.SetActive(!hideWeaponIconIfNoSprite);
            return;
        }
        if (weaponIconContainer != null) weaponIconContainer.SetActive(true);
        weaponIconUI.sprite = icon;
    }

    void UpdateAmmoIcon()
    {
        if (ammoIconUI == null) return;
        if (currentIndex < 0 || currentIndex >= weapons.Count)
        {
            if (ammoIconContainer != null) ammoIconContainer.SetActive(false);
            return;
        }
        Sprite icon = (ammoIcons != null && currentIndex < ammoIcons.Length) ? ammoIcons[currentIndex] : null;
        if (icon == null)
        {
            if (ammoIconContainer != null) ammoIconContainer.SetActive(!hideIconIfNoSprite);
            return;
        }
        if (ammoIconContainer != null) ammoIconContainer.SetActive(true);
        ammoIconUI.sprite = icon;
    }

    public bool UnlockWeapon(string weaponId)
    {
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i].weaponId == weaponId && !unlocked[i])
            {
                unlocked[i] = true;
                if (autoEquipOnPickup) SwitchTo(i);
                return true;
            }
        return false;
    }

    public List<string> GetUnlockedIds()
    {
        List<string> result = new List<string>();
        if (weapons == null || unlocked == null) return result;
        for (int i = 0; i < weapons.Count && i < unlocked.Length; i++)
            if (unlocked[i] && weapons[i] != null) result.Add(weapons[i].weaponId);
        return result;
    }

    public int GetAmmo(string type)
    {
        if (string.IsNullOrEmpty(type)) return 0;
        return ammoByType.TryGetValue(type, out int v) ? v : 0;
    }

    public bool AddAmmo(string type, int amount, int maxForType)
    {
        if (string.IsNullOrEmpty(type) || amount <= 0) return false;
        if (!ammoByType.ContainsKey(type)) ammoByType[type] = 0;
        int current = ammoByType[type];
        if (current >= maxForType) return false;
        int space = maxForType - current;
        int taken = amount < space ? amount : space;
        ammoByType[type] = current + taken;
        SyncInspectorFromDict();
        RefreshAllUI(type);
        return taken > 0;
    }

    public void SetAmmo(string type, int value)
    {
        if (string.IsNullOrEmpty(type)) return;
        ammoByType[type] = value;
        SyncInspectorFromDict();
        RefreshAllUI(type);
    }

    void RefreshAllUI(string type)
    {
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i] != null && weapons[i].ammoType == type)
                weapons[i].UpdateAmmoUI();
    }

    public void RegisterSelf(PlayerHealth health)
    {
        if (health == null) return;
        playerHealth = health;
        playerHealth.RegisterWeaponScript(this);
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.UnregisterWeaponScript(this);
    }
}