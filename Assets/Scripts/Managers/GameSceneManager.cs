using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 集中處理場景切換的管理器。
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    private static GameSceneManager instance;
    private static bool isQuitting = false;
    public static GameSceneManager Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<GameSceneManager>();
                if (instance == null && !isQuitting)
                {
                    GameObject go = new GameObject("_GameSceneManager");
                    instance = go.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void OnApplicationQuit() => isQuitting = true;
    private void OnDestroy() { if (instance == this) instance = null; }

    [Header("Scene Names")]
    public string mainMenuSceneName = "StartMenu";
    public string gameSceneName = "GameScene";
    public string deathSceneName = "DeathScene";
    public string victorySceneName = "VictoryScene";

    /// <summary>
    /// 載入主選單。
    /// </summary>
    public void LoadMainMenu() => SceneManager.LoadScene(mainMenuSceneName);

    /// <summary>
    /// 開始新的遊戲。
    /// </summary>
    public void LoadGame() => SceneManager.LoadScene(gameSceneName);

    /// <summary>
    /// 載入死亡畫面。
    /// </summary>
    public void LoadDeathScene() => SceneManager.LoadScene(deathSceneName);

    /// <summary>
    /// 載入過關/結束畫面。
    /// </summary>
    public void LoadVictoryScene() => SceneManager.LoadScene(victorySceneName);

    /// <summary>
    /// 退出遊戲。
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
