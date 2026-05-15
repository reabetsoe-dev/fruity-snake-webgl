using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingLevel : MonoBehaviour
{
    private const string NextSceneKey = "NextScene";

    public static int level = 1;
    public static string nextSceneName = "Level_1";

    public float loadDelay = 0.5f;

    public static void SetNextScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "Level_1";
        }

        nextSceneName = sceneName;
        PlayerPrefs.SetString(NextSceneKey, sceneName);
        PlayerPrefs.Save();
    }

    // Use this for initialization
    void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        string sceneName = PlayerPrefs.GetString(NextSceneKey, nextSceneName);

        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = GetSceneNameFromLegacyLevel();
        }

        Debug.Log("LoadingScene loading next scene: " + sceneName);

        if (loadDelay > 0f)
        {
            yield return new WaitForSeconds(loadDelay);
        }

        SceneManager.LoadScene(sceneName);
    }

    private string GetSceneNameFromLegacyLevel()
    {
        if (level == 2)
        {
            return "Level_2";
        }

        return "Level_1";
    }
}
