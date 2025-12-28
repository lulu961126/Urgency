using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    
    public void ExitGame() => Application.Quit();

    public void StartGame()
    {
        SceneManager.LoadScene("LevelSelectMenu");
    }

    public void OpenShop()
    {
        
    }

    public void OpenSettings()
    {
        
    }
}
