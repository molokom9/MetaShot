using UnityEngine;

public static class GamepadInput
{
    // Кнопки Xbox
    private const KeyCode A_Btn = KeyCode.JoystickButton0;
    private const KeyCode B_Btn = KeyCode.JoystickButton1;
    private const KeyCode X_Btn = KeyCode.JoystickButton2;
    private const KeyCode Y_Btn = KeyCode.JoystickButton3;
    private const KeyCode LB_Btn = KeyCode.JoystickButton4;
    private const KeyCode RB_Btn = KeyCode.JoystickButton5;
    private const KeyCode Start_Btn = KeyCode.JoystickButton7;
    private const KeyCode L3_Btn = KeyCode.JoystickButton8;

    // Крестовина
    private const KeyCode DPadUp = KeyCode.JoystickButton10;
    private const KeyCode DPadDown = KeyCode.JoystickButton11;
    private const KeyCode DPadLeft = KeyCode.JoystickButton12;
    private const KeyCode DPadRight = KeyCode.JoystickButton13;

    public static bool PadAvailable => Input.GetJoystickNames().Length > 0;

    // Стики (КАМЕРА НЕ ТРОНУТА, ровно твой код)
    public static Vector2 LeftStick => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    public static Vector2 RightStick => new Vector2(Input.GetAxisRaw("RightStickX"), Input.GetAxisRaw("RightStickY"));

    // Триггеры
    public static float RightTrigger => Input.GetAxisRaw("RightTrigger");
    public static float LeftTrigger => Input.GetAxisRaw("LeftTrigger");

    // Кнопки
    public static bool A_Pressed => Input.GetKeyDown(A_Btn);
    public static bool A_Held => Input.GetKey(A_Btn);

    public static bool B_Pressed => Input.GetKeyDown(B_Btn);
    public static bool X_Pressed => Input.GetKeyDown(X_Btn);
    public static bool Y_Pressed => Input.GetKeyDown(Y_Btn);

    public static bool LB_Pressed => Input.GetKeyDown(LB_Btn);
    public static bool LB_Held => Input.GetKey(LB_Btn);
    public static bool RB_Pressed => Input.GetKeyDown(RB_Btn);
    public static bool RB_Held => Input.GetKey(RB_Btn);

    // Триггеры как кнопки (Стрельба)
    // Добавил проверку на 9-ю кнопку, если ось не прочитается
    public static bool RT_Pressed => RightTrigger > 0.1f || Input.GetKeyDown(KeyCode.JoystickButton9);
    public static bool RT_Held => RightTrigger > 0.1f || Input.GetKey(KeyCode.JoystickButton9);

    public static bool LT_Pressed => LeftTrigger > 0.5f;
    public static bool LT_Held => LeftTrigger > 0.5f;

    public static bool Start_Pressed => Input.GetKeyDown(Start_Btn);
    public static bool L3_Pressed => Input.GetKeyDown(L3_Btn);

    public static bool DPadRight_Pressed => Input.GetKeyDown(DPadRight);
    public static bool DPadLeft_Pressed => Input.GetKeyDown(DPadLeft);
    public static bool DPadUp_Pressed => Input.GetKeyDown(DPadUp);
    public static bool DPadDown_Pressed => Input.GetKeyDown(DPadDown);

    // Для меню
    public static Vector2 UINavigate
    {
        get
        {
            Vector2 stick = LeftStick;
            Vector2 dpad = Vector2.zero;
            if (Input.GetKey(DPadUp)) dpad.y = 1f;
            if (Input.GetKey(DPadDown)) dpad.y = -1f;
            if (Input.GetKey(DPadLeft)) dpad.x = -1f;
            if (Input.GetKey(DPadRight)) dpad.x = 1f;

            Vector2 combined = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
            if (combined.sqrMagnitude < 0.5f) return Vector2.zero;
            return combined;
        }
    }

    public static bool UISubmit => A_Pressed;
    public static bool UICancel => B_Pressed;
}