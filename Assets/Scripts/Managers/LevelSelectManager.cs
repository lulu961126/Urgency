using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    public void Back() => SceneManager.LoadScene("StartMenu");
        
    public void StartTutorial()
    {
        
    }
    
    public void ContinueGame(){}
    
}
