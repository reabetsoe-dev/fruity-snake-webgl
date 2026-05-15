using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void PlayGame()
    {
        // Enable loading screen before Level_1
        LoadingLevel.SetNextScene("Level_1");
        SceneManager.LoadScene("LoadingScene");
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Exit requested. Application.Quit only closes built players.");
#endif

        // Quit game
        Application.Quit();
    }
}
