using UnityEngine;

// 新輸入系統包裝
// 自動建立並啟用 InputSystem_Actions
// 統一入口，避免多處 new 與 Enable
public static class Inputs
{
    private static InputSystem_Actions _actions;

    public static InputSystem_Actions Actions
    {
        get
        {
            if (_actions == null)
            {
                _actions = new InputSystem_Actions();
                _actions.Enable();
                _actions.UI.Enable();
            }
            return _actions;
        }
    }

    // 每次進入 Play 清掉舊的 _actions，避免「快速進入 Play」殘留
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (_actions != null)
        {
            _actions.Disable();
            _actions = null;
        }
    }
}