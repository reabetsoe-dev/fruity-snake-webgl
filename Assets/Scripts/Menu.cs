using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void PlayGame()
    {
        // Enable loading screen
        SceneManager.LoadScene("LoadingScene");
    }

    public void ExitGame()
    {
        // Quit game
        Application.Quit();
    }
}
