using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Names")]
    public string hubSceneName = "HUB";
    public string worldMapSceneName = "WorldMap";
    public string battleSceneTemplate = "Battle Level Template";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadWorldMap()
    {
        SceneManager.LoadScene(worldMapSceneName);
    }

    public void LoadHubScene()
    {
        SceneManager.LoadScene(hubSceneName);
    }

    public void LoadBattleLevel(int regionId, int levelId)
    {
        // Sla huidige level data op
        PlayerPrefs.SetInt("CurrentRegion", regionId);
        PlayerPrefs.SetInt("CurrentLevel", levelId);

        // Laad battle scene
        SceneManager.LoadScene(battleSceneTemplate);
    }
}
