using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingLevel : MonoBehaviour
{
    public static int level = 1;

    // Use this for initialization
    void Start()
    {
        if (level <= 2)
        {
            SceneManager.LoadScene(level);
        }

        else
        {
            // Reset game
            level = 1;
            SceneManager.LoadScene("Main Menu");
        }
    }
}
