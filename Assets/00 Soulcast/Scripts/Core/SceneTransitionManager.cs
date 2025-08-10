// ✅ REPLACE the existing SceneTransitionManager.cs with this enhanced version

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Names")]
    public string hubSceneName = "HUB";
    public string worldMapSceneName = "WorldMap";
    public string battleSceneTemplate = "Battle Level Template";

    [Header("Loading")]
    public bool showLoadingScreen = true;
    public GameObject loadingScreenPrefab;

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
        Debug.Log("Loading World Map scene...");
        SceneManager.LoadScene(worldMapSceneName);
    }

    public void LoadHubScene()
    {
        Debug.Log("Loading Hub scene...");
        SceneManager.LoadScene(hubSceneName);
    }

    // ✅ ENHANCED: Load battle with complete team and combat data
    public void LoadBattleWithTeam(CombatTemplate combatTemplate, List<CollectedMonster> selectedTeam, int region = 1, int level = 1, int battleSequence = 1)
    {
        if (combatTemplate == null)
        {
            Debug.LogError("Cannot start battle: CombatTemplate is null!");
            return;
        }

        if (selectedTeam == null || selectedTeam.Count == 0)
        {
            Debug.LogError("Cannot start battle: No team selected!");
            return;
        }

        // Setup battle data for the battle scene
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.SetupBattleData(combatTemplate, selectedTeam, region, level, battleSequence);
        }
        else
        {
            Debug.LogError("BattleDataManager.Instance is null! Make sure it exists in the scene.");
        }

        // Store additional data in PlayerPrefs as backup
        PlayerPrefs.SetInt("CurrentRegion", region);
        PlayerPrefs.SetInt("CurrentLevel", level);
        PlayerPrefs.SetInt("CurrentBattleSequence", battleSequence);
        PlayerPrefs.SetString("CurrentCombatTemplate", combatTemplate.name);

        Debug.Log($"🚀 Loading battle scene: {combatTemplate.combatName}");
        Debug.Log($"📍 Region {region}, Level {level}, Battle {battleSequence}");
        Debug.Log($"👥 Team size: {selectedTeam.Count}");

        // Load the battle scene
        SceneManager.LoadScene(battleSceneTemplate);
    }

    // ✅ LEGACY: Still supported for backwards compatibility
    public void LoadBattleLevel(int regionId, int levelId)
    {
        Debug.LogWarning("LoadBattleLevel() is deprecated. Use LoadBattleWithTeam() instead.");

        PlayerPrefs.SetInt("CurrentRegion", regionId);
        PlayerPrefs.SetInt("CurrentLevel", levelId);
        SceneManager.LoadScene(battleSceneTemplate);
    }

    // ✅ NEW: Return to world map after battle
    public void ReturnToWorldMapAfterBattle()
    {
        // Clear battle data
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.ClearBattleData();
        }

        Debug.Log("Returning to World Map after battle...");
        LoadWorldMap();
    }

    // ✅ NEW: Quick test battle loading
    public void LoadTestBattle(CombatTemplate combatTemplate)
    {
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.SetupTestBattle(combatTemplate);
        }

        Debug.Log($"🧪 Loading test battle: {combatTemplate.combatName}");
        SceneManager.LoadScene(battleSceneTemplate);
    }
}
