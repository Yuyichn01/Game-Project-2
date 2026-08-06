using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Input 兼容层 — 自动检测当前 Input Handler 模式，无缝兼容新旧 Input System。
/// Both 模式下委托给旧 API（ENABLE_LEGACY_INPUT_MANAGER defined），
/// 纯新 Input System 模式下降级到 UnityEngine.InputSystem。
/// </summary>
public static class InputHelper
{
    public static Vector3 mousePosition
    {
        get
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Mouse.current == null) return Vector3.zero;
            Vector2 pos = Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
#else
            return Input.mousePosition;
#endif
        }
    }

    public static bool GetMouseButtonDown(int button)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current == null) return false;
        return button switch
        {
            0 => Mouse.current.leftButton.wasPressedThisFrame,
            1 => Mouse.current.rightButton.wasPressedThisFrame,
            2 => Mouse.current.middleButton.wasPressedThisFrame,
            _ => false,
        };
#else
        return Input.GetMouseButtonDown(button);
#endif
    }

    public static bool GetKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current == null) return false;
        return key switch
        {
            KeyCode.Q          => Keyboard.current.qKey.wasPressedThisFrame,
            KeyCode.W          => Keyboard.current.wKey.wasPressedThisFrame,
            KeyCode.E          => Keyboard.current.eKey.wasPressedThisFrame,
            KeyCode.A          => Keyboard.current.aKey.wasPressedThisFrame,
            KeyCode.S          => Keyboard.current.sKey.wasPressedThisFrame,
            KeyCode.D          => Keyboard.current.dKey.wasPressedThisFrame,
            KeyCode.Escape     => Keyboard.current.escapeKey.wasPressedThisFrame,
            KeyCode.Return     => Keyboard.current.enterKey.wasPressedThisFrame,
            KeyCode.KeypadEnter=> Keyboard.current.numpadEnterKey.wasPressedThisFrame,
            KeyCode.Tab        => Keyboard.current.tabKey.wasPressedThisFrame,
            KeyCode.UpArrow    => Keyboard.current.upArrowKey.wasPressedThisFrame,
            KeyCode.DownArrow  => Keyboard.current.downArrowKey.wasPressedThisFrame,
            KeyCode.LeftArrow  => Keyboard.current.leftArrowKey.wasPressedThisFrame,
            KeyCode.RightArrow => Keyboard.current.rightArrowKey.wasPressedThisFrame,
            KeyCode.Alpha1     => Keyboard.current.digit1Key.wasPressedThisFrame,
            KeyCode.Alpha2     => Keyboard.current.digit2Key.wasPressedThisFrame,
            KeyCode.Alpha3     => Keyboard.current.digit3Key.wasPressedThisFrame,
            KeyCode.Alpha4     => Keyboard.current.digit4Key.wasPressedThisFrame,
            _ => false,
        };
#else
        return Input.GetKeyDown(key);
#endif
    }

    public static float GetAxisRaw(string axisName)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current == null) return 0f;
        return axisName switch
        {
            "Horizontal" => (Keyboard.current.dKey.isPressed ? 1f : 0f)
                          - (Keyboard.current.aKey.isPressed ? 1f : 0f),
            "Vertical"   => (Keyboard.current.wKey.isPressed ? 1f : 0f)
                          - (Keyboard.current.sKey.isPressed ? 1f : 0f),
            _ => 0f,
        };
#else
        return Input.GetAxisRaw(axisName);
#endif
    }

    public static float GetAxis(string axisName)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current == null) return 0f;
        return axisName switch
        {
            "Horizontal" => (Keyboard.current.dKey.isPressed ? 1f : 0f)
                          - (Keyboard.current.aKey.isPressed ? 1f : 0f),
            _ => 0f,
        };
#else
        return Input.GetAxis(axisName);
#endif
    }
}
