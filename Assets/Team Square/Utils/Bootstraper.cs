using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstraper : MonoBehaviour
{
    private const string INIT_SCENE = "InitScene";
    private const string MENU_SCENE = "MenuScene";
    private const string MAIN_SCENE = "MainScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static async Task Init()
    {
        if (GameConfig.Instance.cheatSettings.disableBootStrapper) return;

        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.name != INIT_SCENE)
        {
            foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                obj.SetActive(false);

            await SceneManager.LoadSceneAsync(INIT_SCENE);
        }

        LoadSceneAfterInit();
    }

    private static void LoadSceneAfterInit()
    {
        if (GameConfig.Instance.cheatSettings.noMenu)
            SceneManager.LoadSceneAsync(MAIN_SCENE, LoadSceneMode.Single);
        else
            SceneManager.LoadSceneAsync(MENU_SCENE, LoadSceneMode.Single);
    }
}
