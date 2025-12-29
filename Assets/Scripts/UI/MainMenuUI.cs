using UnityEngine;

/// <summary>
/// 控制主選單 UI 的按鈕行為。
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// 開始遊戲按鈕。
    /// </summary>
    public void OnStartGameButtonClicked()
    {
        GameSceneManager.Instance.LoadGame();
    }

    /// <summary>
    /// 退出遊戲按鈕。
    /// </summary>
    public void OnQuitButtonClicked()
    {
        GameSceneManager.Instance.QuitGame();
    }
}
