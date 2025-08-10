using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameSetup : MonoBehaviour
{
    [Header("Monster Prefabs")]
    public GameObject monsterPrefab; // Base monster prefab with Monster component

    [Header("Monster Data")]
    public MonsterData[] playerMonsterData;
    public MonsterData[] enemyMonsterData;

    [Header("Spawn Positions")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("3D Model Settings")]
    public bool spawnModels = true;
    public Vector3 modelOffset = Vector3.zero; // Offset for model positioning
    public Vector3 modelScale = Vector3.one; // Scale multiplier for models

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab; // Add this field!
    public bool spawnHealthBars = true;

    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        Debug.Log("GameSetup: Starting battle setup...");

        // ✅ Wait for BattleDataManager if needed
        if (BattleDataManager.Instance == null)
        {
            Debug.LogWarning("BattleDataManager.Instance is null! Trying to find it...");
            var manager = FindFirstObjectByType<BattleDataManager>();
            if (manager == null)
            {
                Debug.LogError("BattleDataManager not found! Setting up fallback game...");
                SetupFallbackGame();
                return;
            }
        }

        // Check if we have valid battle data
        if (BattleDataManager.Instance.HasValidBattleData())
        {
            SetupFromBattleData();
        }
        else
        {
            // Try to load from PlayerPrefs as fallback
            if (BattleDataManager.Instance)
            {
                Debug.Log("Loaded battle data from PlayerPrefs backup");
                SetupFromBattleData();
            }
            else
            {
                Debug.LogWarning("No battle data found. Setting up fallback game...");
                SetupFallbackGame();
            }
        }
    }

    private void SetupFromBattleData()
    {
        var battleData = BattleDataManager.Instance.GetCurrentBattleData();
        var combatTemplate = battleData.combatTemplate;
        var selectedTeam = BattleDataManager.Instance.GetSelectedTeam();

        if (combatTemplate == null)
        {
            Debug.LogError("CombatTemplate is null! Cannot setup battle.");
            SetupFallbackGame();
            return;
        }

        if (selectedTeam.Count == 0)
        {
            Debug.LogError("No selected team found! Cannot setup battle.");
            SetupFallbackGame();
            return;
        }

        Debug.Log($"🎯 Setting up battle: {combatTemplate.combatName}");
        Debug.Log($"👥 Player team: {selectedTeam.Count} monsters");
        Debug.Log($"⚔️ Enemy waves: {combatTemplate.waves.Count}");

        // Clear any existing monsters
        ClearExistingMonsters();

        // Setup player team from selected monsters
        SetupPlayerTeamFromData(selectedTeam);

        // Setup enemies from combat template
        SetupEnemiesFromTemplate(combatTemplate);

        Debug.Log("✅ Battle setup from BattleDataManager complete!");
    }

    // ✅ NEW: Setup fallback game using existing arrays
    private void SetupFallbackGame()
    {
        Debug.Log("GameSetup: Setting up fallback game with predefined data...");

        // Clear any existing monsters first
        ClearExistingMonsters();

        // Use the original playerMonsterData and enemyMonsterData arrays
        SetupOriginalGame();
    }

    // ✅ NEW: Clear existing monsters from scene
    private void ClearExistingMonsters()
    {
        Monster[] existingMonsters = FindObjectsByType<Monster>(FindObjectsSortMode.None); // ✅ UPDATED
        foreach (var monster in existingMonsters)
        {
            if (Application.isPlaying)
                Destroy(monster.gameObject);
            else
                DestroyImmediate(monster.gameObject);
        }

        Debug.Log($"Cleared {existingMonsters.Length} existing monsters");
    }

    // ✅ NEW: Setup player team from selected CollectedMonsters
    private void SetupPlayerTeamFromData(List<CollectedMonster> selectedTeam)
    {
        Debug.Log($"Setting up player team with {selectedTeam.Count} monsters...");

        for (int i = 0; i < Mathf.Min(selectedTeam.Count, playerSpawnPoints.Length); i++)
        {
            var collectedMonster = selectedTeam[i];
            var spawnPoint = playerSpawnPoints[i];

            // Create MonsterData from CollectedMonster
            var monsterData = CreateRuntimeMonsterData(collectedMonster);

            // Spawn the monster
            SpawnMonster(monsterData, spawnPoint, true, $"(Player {i + 1})");

            Debug.Log($"✅ Spawned player monster {i + 1}: {collectedMonster.monsterData.monsterName} (Lv.{collectedMonster.level})");
        }
    }

    // ✅ NEW: Setup enemies from combat template
    private void SetupEnemiesFromTemplate(CombatTemplate combatTemplate)
    {
        Debug.Log($"Setting up enemies from template: {combatTemplate.waves.Count} waves...");

        int enemyIndex = 0;

        foreach (var wave in combatTemplate.waves)
        {
            foreach (var enemySpawn in wave.enemySpawns)
            {
                if (enemyIndex >= enemySpawnPoints.Length)
                {
                    Debug.LogWarning("Not enough enemy spawn points for all enemies!");
                    break;
                }

                // Create multiple copies if spawnCount > 1
                for (int copy = 0; copy < enemySpawn.spawnCount; copy++)
                {
                    if (enemyIndex >= enemySpawnPoints.Length) break;

                    var spawnPoint = enemySpawnPoints[enemyIndex];

                    // Create MonsterData from EnemySpawn
                    var monsterData = CreateRuntimeEnemyData(enemySpawn);

                    // Spawn the enemy
                    SpawnMonster(monsterData, spawnPoint, false, $"(Enemy {enemyIndex + 1})");

                    Debug.Log($"✅ Spawned enemy {enemyIndex + 1}: {enemySpawn.monsterData.monsterName} (Lv.{enemySpawn.monsterLevel})");

                    enemyIndex++;
                }
            }
        }
    }

    // ✅ NEW: Create runtime MonsterData from CollectedMonster
    private MonsterData CreateRuntimeMonsterData(CollectedMonster collectedMonster)
    {
        // Clone the original MonsterData
        var runtimeData = Instantiate(collectedMonster.monsterData);

        // Apply level scaling and star bonuses
        var scaledStats = collectedMonster.GetEffectiveStats();

        // Update base stats with scaled values
        runtimeData.baseHP = scaledStats.health;
        runtimeData.baseATK = scaledStats.attack;
        runtimeData.baseDEF = scaledStats.defense;
        runtimeData.baseSPD = scaledStats.speed;
        runtimeData.baseEnergy = scaledStats.energy;

        // Update name to show level and stars
        runtimeData.monsterName = $"{collectedMonster.monsterData.monsterName} Lv.{collectedMonster.level}";

        Debug.Log($"Created runtime data for {runtimeData.monsterName}: HP={runtimeData.baseHP}, ATK={runtimeData.baseATK}");

        return runtimeData;
    }

    // ✅ NEW: Create runtime MonsterData from EnemySpawn
    private MonsterData CreateRuntimeEnemyData(EnemySpawn enemySpawn)
    {
        // Clone the original MonsterData
        var runtimeData = Instantiate(enemySpawn.monsterData);

        // Apply level and star scaling using the Monster system
        var effectiveStats = enemySpawn.GetEffectiveStats();

        // Update base stats
        runtimeData.baseHP = effectiveStats.health;
        runtimeData.baseATK = effectiveStats.attack;
        runtimeData.baseDEF = effectiveStats.defense;
        runtimeData.baseSPD = effectiveStats.speed;
        runtimeData.baseEnergy = effectiveStats.energy;

        // Update name to show level and stars
        string rarity = MonsterData.GetRarityName(enemySpawn.starLevel);
        runtimeData.monsterName = $"{enemySpawn.monsterData.monsterName} Lv.{enemySpawn.monsterLevel} ({rarity})";

        Debug.Log($"Created enemy runtime data: {runtimeData.monsterName}: HP={runtimeData.baseHP}, ATK={runtimeData.baseATK}");

        return runtimeData;
    }

    // ✅ NEW: Original setup method (for backwards compatibility)
    private void SetupOriginalGame()
    {
        Debug.Log("GameSetup: Starting original game setup...");

        // Spawn player monsters
        for (int i = 0; i < Mathf.Min(playerSpawnPoints.Length, playerMonsterData.Length); i++)
        {
            if (i < playerSpawnPoints.Length)
            {
                SpawnMonster(
                    playerMonsterData[i],
                    playerSpawnPoints[i],
                    true,
                    "(Player)"
                );
            }
        }

        // Spawn enemy monsters
        for (int i = 0; i < Mathf.Min(6, enemyMonsterData.Length); i++)
        {
            if (i < enemySpawnPoints.Length)
            {
                SpawnMonster(
                    enemyMonsterData[i],
                    enemySpawnPoints[i],
                    false,
                    "(Enemy)"
                );
            }
        }

        Debug.Log("GameSetup: Original game setup complete!");
    }

    private void SpawnMonster(MonsterData monsterData, Transform spawnPoint, bool isPlayerControlled, string suffix)
    {
        // 1. Create the base monster GameObject
        GameObject monster = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);

        // 2. Setup Monster component
        Monster monsterComponent = monster.GetComponent<Monster>();
        monsterComponent.monsterData = monsterData;
        monsterComponent.isPlayerControlled = isPlayerControlled;

        // 3. Spawn the 3D model if available
        if (spawnModels && monsterData.modelPrefab != null)
        {
            GameObject model3D = SpawnModel(monsterData.modelPrefab, monster.transform);
            Debug.Log($"Spawned 3D model for {monsterData.monsterName}: {model3D.name}");
        }
        else if (spawnModels && monsterData.modelPrefab == null)
        {
            Debug.LogWarning($"No 3D model prefab assigned for {monsterData.monsterName}!");
            CreateFallbackVisual(monster.transform, monsterData);
        }

        // 4. Initialize the monster
        monsterComponent.InitializeMonster();

        // 5. Set the name
        monster.name = monsterData.monsterName + " " + suffix;

        Debug.Log($"Spawned {monster.name} at {spawnPoint.position}");
    }

    private GameObject SpawnModel(GameObject modelPrefab, Transform parent)
    {
        // Instantiate the 3D model as a child of the monster
        GameObject model = Instantiate(modelPrefab, parent);

        // Apply position offset
        model.transform.localPosition = modelOffset;

        // Apply scale
        model.transform.localScale = Vector3.Scale(model.transform.localScale, modelScale);

        // Ensure the model is properly oriented
        model.transform.localRotation = Quaternion.identity;

        // Name the model
        model.name = "3D_Model";

        // Add ModelController component for visual effects
        ModelController modelController = model.GetComponent<ModelController>();
        if (modelController == null)
        {
            modelController = model.AddComponent<ModelController>();
        }

        // Set health bar prefab reference
        if (spawnHealthBars && healthBarPrefab != null)
        {
            modelController.healthBarPrefab = healthBarPrefab;
        }

        return model;
    }

    private void CreateFallbackVisual(Transform parent, MonsterData monsterData)
    {
        // Create a simple primitive as fallback
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.transform.SetParent(parent);
        fallback.transform.localPosition = modelOffset;
        fallback.transform.localScale = modelScale;
        fallback.name = "Fallback_Visual";

        // Color it based on element
        Renderer renderer = fallback.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = ElementalSystem.GetElementColor(monsterData.element);
        }

        // Add ModelController even for fallback visuals
        ModelController modelController = fallback.AddComponent<ModelController>();
        if (spawnHealthBars && healthBarPrefab != null)
        {
            modelController.healthBarPrefab = healthBarPrefab;
        }

        Debug.Log($"Created fallback visual for {monsterData.monsterName}");
    }

    // ✅ IMPROVED: Enhanced debug methods
    [ContextMenu("Test Battle Data Setup")]
    private void TestBattleDataSetup()
    {
        if (Application.isPlaying)
        {
            Debug.Log("=== TESTING BATTLE DATA SETUP ===");

            if (BattleDataManager.Instance != null)
            {
                bool hasData = BattleDataManager.Instance.HasValidBattleData();
                Debug.Log($"BattleDataManager found: {hasData}");

                if (hasData)
                {
                    var battleData = BattleDataManager.Instance.GetCurrentBattleData();
                    Debug.Log($"Battle Template: {battleData.combatTemplate?.combatName}");
                    Debug.Log($"Selected Team Count: {battleData.selectedTeamIDs.Count}");

                    var selectedTeam = BattleDataManager.Instance.GetSelectedTeam();
                    Debug.Log($"Retrieved Team Count: {selectedTeam.Count}");
                }
            }
            else
            {
                Debug.LogWarning("BattleDataManager.Instance is null!");
            }
        }
    }

    // Debug method to respawn all monsters
    [ContextMenu("Respawn All Monsters")]
    [System.Obsolete]
    public void RespawnMonsters()
    {
        // Clear existing monsters
        Monster[] existingMonsters = FindObjectsOfType<Monster>();
        for (int i = 0; i < existingMonsters.Length; i++)
        {
            if (Application.isPlaying)
                Destroy(existingMonsters[i].gameObject);
            else
                DestroyImmediate(existingMonsters[i].gameObject);
        }

        // Respawn
        SetupGame();
    }
}
