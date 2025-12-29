using UnityEngine;

/// <summary>
/// 控制死亡或勝利畫面 UI 的按鈕行為。
/// </summary>
public class GameEndUI : MonoBehaviour
{
    /// <summary>
    /// 重新開始新遊戲。
    /// </summary>
    public void OnRestartButtonClicked()
    {
        // 重新開始前，建議重置 Informations 裡面的狀態
        Informations.ResetGameState();
        GameSceneManager.Instance.LoadGame();
    }

    /// <summary>
    /// 返回主選單。
    /// </summary>
    public void OnMainMenuButtonClicked()
    {
        GameSceneManager.Instance.LoadMainMenu();
    }

    /// <summary>
    /// 退出遊戲。
    /// </summary>
    public void OnQuitButtonClicked()
    {
        GameSceneManager.Instance.QuitGame();
    }
}
