// ✅ UPDATED: BattleDataManager.cs - Optimized for InitScene placement
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class BattleSetupData
{
    public CombatTemplate combatTemplate;
    public List<string> selectedTeamIDs; // CollectedMonster unique IDs
    public int regionId;
    public int levelId;
    public int battleSequenceId;
    public bool isTestBattle;

    public BattleSetupData()
    {
        selectedTeamIDs = new List<string>();
    }
}

public class BattleDataManager : MonoBehaviour
{
    public static BattleDataManager Instance { get; private set; }

    [Header("Battle Data")]
    public BattleSetupData currentBattleData;

    [Header("Settings")]
    public bool debugMode = false;
    public bool persistentData = true;

    [Header("Auto-Clear Settings")]
    public bool autoClearAfterBattle = true;
    public float autoClearDelay = 5f;

    private void Awake()
    {
        // ✅ IMPROVED: Singleton pattern for InitScene managers
        if (Instance == null)
        {
            Instance = this;

            // ✅ Only DontDestroyOnLoad if we're in InitScene context
            if (persistentData)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeBattleData();
            Debug.Log("🎯 BattleDataManager initialized in InitScene");
        }
        else
        {
            Debug.LogWarning("BattleDataManager already exists! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void InitializeBattleData()
    {
        currentBattleData = new BattleSetupData();
        Debug.Log("BattleDataManager: Data initialized");
    }

    // ✅ ENHANCED: Better integration with other InitScene managers
    public void SetupBattleData(CombatTemplate template, List<CollectedMonster> selectedTeam, int region = 1, int level = 1, int battleSequence = 1)
    {
        currentBattleData.combatTemplate = template;
        currentBattleData.selectedTeamIDs = selectedTeam.Select(m => m.uniqueID).ToList();
        currentBattleData.regionId = region;
        currentBattleData.levelId = level;
        currentBattleData.battleSequenceId = battleSequence;
        currentBattleData.isTestBattle = false;

        if (debugMode)
        {
            Debug.Log($"🎯 Battle data setup: {template.combatName}");
            Debug.Log($"📍 Location: Region {region}, Level {level}, Battle {battleSequence}");
            Debug.Log($"👥 Team: {selectedTeam.Count} monsters selected");

            foreach (var monster in selectedTeam)
            {
                Debug.Log($"  - {monster.monsterData.monsterName} (Lv.{monster.currentLevel}, {monster.monsterData.defaultStarLevel}★)");
            }
        }
    }

    public BattleSetupData GetCurrentBattleData()
    {
        return currentBattleData;
    }

    // ✅ ENHANCED: Better integration with MonsterCollectionManager
    public List<CollectedMonster> GetSelectedTeam()
    {
        var selectedMonsters = new List<CollectedMonster>();

        // ✅ Wait for MonsterCollectionManager if not ready yet
        if (MonsterCollectionManager.Instance == null)
        {
            Debug.LogWarning("MonsterCollectionManager.Instance is null! Trying to find in scene...");
            var manager = FindFirstObjectByType<MonsterCollectionManager>();
            if (manager != null)
            {
                Debug.Log("Found MonsterCollectionManager in scene");
            }
            else
            {
                Debug.LogError("MonsterCollectionManager not found anywhere! Cannot retrieve selected team.");
                return selectedMonsters;
            }
        }

        var allMonsters = MonsterCollectionManager.Instance.GetAllMonsters();

        foreach (string id in currentBattleData.selectedTeamIDs)
        {
            var monster = allMonsters.FirstOrDefault(m => m.uniqueID == id);
            if (monster != null)
            {
                selectedMonsters.Add(monster);
            }
            else
            {
                Debug.LogWarning($"Could not find monster with ID: {id}");
            }
        }

        if (debugMode) Debug.Log($"Retrieved {selectedMonsters.Count} monsters for battle");
        return selectedMonsters;
    }

    // ✅ ENHANCED: Smart clear with auto-clear option
    public void ClearBattleData()
    {
        currentBattleData = new BattleSetupData();

        // Clear PlayerPrefs backup
        PlayerPrefs.DeleteKey("BattleData_TemplateName");
        PlayerPrefs.DeleteKey("BattleData_Region");
        PlayerPrefs.DeleteKey("BattleData_Level");
        PlayerPrefs.DeleteKey("BattleData_Battle");
        PlayerPrefs.DeleteKey("BattleData_TeamIDs");
        PlayerPrefs.DeleteKey("BattleData_IsTest");
        PlayerPrefs.Save();

        if (debugMode) Debug.Log("Battle data cleared");
    }

    // ✅ NEW: Auto-clear after battle completion
    public void StartAutoClear()
    {
        if (autoClearAfterBattle)
        {
            Invoke(nameof(ClearBattleData), autoClearDelay);
            if (debugMode) Debug.Log($"Auto-clear scheduled in {autoClearDelay} seconds");
        }
    }

    public bool HasValidBattleData()
    {
        return currentBattleData.combatTemplate != null &&
               currentBattleData.selectedTeamIDs.Count > 0;
    }

    // ✅ ENHANCED: Better test battle setup
    public void SetupTestBattle(CombatTemplate template)
    {
        currentBattleData.combatTemplate = template;
        currentBattleData.regionId = 1;
        currentBattleData.levelId = 1;
        currentBattleData.battleSequenceId = 1;
        currentBattleData.isTestBattle = true;

        // Get available monsters from MonsterCollectionManager
        if (MonsterCollectionManager.Instance != null)
        {
            var availableMonsters = MonsterCollectionManager.Instance.GetAllMonsters();
            var testTeam = availableMonsters.Take(3).ToList();
            currentBattleData.selectedTeamIDs = testTeam.Select(m => m.uniqueID).ToList();

            if (debugMode) Debug.Log($"🧪 Test battle setup: {template.combatName} with {testTeam.Count} test monsters");
        }
        else
        {
            Debug.LogWarning("MonsterCollectionManager.Instance is null! Cannot setup test team.");
        }
    }

    // ✅ NEW: Integration check for other InitScene managers
    public bool AreManagersReady()
    {
        bool playerInventoryReady = PlayerInventory.Instance != null;
        bool monsterCollectionReady = MonsterCollectionManager.Instance != null;
        bool sceneTransitionReady = SceneTransitionManager.Instance != null;
        bool battleProgressionReady = BattleProgressionManager.Instance != null;

        if (debugMode)
        {
            Debug.Log($"Managers Status:");
            Debug.Log($"  PlayerInventory: {(playerInventoryReady ? "✅" : "❌")}");
            Debug.Log($"  MonsterCollection: {(monsterCollectionReady ? "✅" : "❌")}");
            Debug.Log($"  SceneTransition: {(sceneTransitionReady ? "✅" : "❌")}");
            Debug.Log($"  BattleProgression: {(battleProgressionReady ? "✅" : "❌")}");
        }

        return playerInventoryReady && monsterCollectionReady && sceneTransitionReady && battleProgressionReady;
    }

    // ✅ DEBUG: Enhanced debug methods
    [ContextMenu("Debug Current Battle Data")]
    private void DebugCurrentBattleData()
    {
        if (currentBattleData.combatTemplate != null)
        {
            Debug.Log($"=== CURRENT BATTLE DATA ===");
            Debug.Log($"Template: {currentBattleData.combatTemplate.combatName}");
            Debug.Log($"Location: Region {currentBattleData.regionId}, Level {currentBattleData.levelId}, Battle {currentBattleData.battleSequenceId}");
            Debug.Log($"Selected Team IDs: {string.Join(", ", currentBattleData.selectedTeamIDs)}");
            Debug.Log($"Is Test Battle: {currentBattleData.isTestBattle}");
            Debug.Log($"Managers Ready: {AreManagersReady()}");
        }
        else
        {
            Debug.Log("No battle data currently set");
        }
    }

    [ContextMenu("Test Manager Dependencies")]
    private void TestManagerDependencies()
    {
        Debug.Log("=== TESTING MANAGER DEPENDENCIES ===");
        AreManagersReady();

        if (MonsterCollectionManager.Instance != null)
        {
            var monsters = MonsterCollectionManager.Instance.GetAllMonsters();
            Debug.Log($"Available monsters in collection: {monsters.Count}");
        }
    }
}
