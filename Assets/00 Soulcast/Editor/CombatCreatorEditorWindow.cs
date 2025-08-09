using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class CombatCreatorEditorWindow : EditorWindow
{
    [MenuItem("Soulcast/Combat Creator")]
    public static void ShowWindow()
    {
        CombatCreatorEditorWindow window = GetWindow<CombatCreatorEditorWindow>();
        window.titleContent = new GUIContent("Combat Creator Window",
            EditorGUIUtility.IconContent("ScriptableObject Icon").image);
        window.minSize = new Vector2(800, 600);
        window.Show();
    }

    private CombatTemplate currentCombat;
    private LevelDatabase levelDatabase;
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Basic Info", "Waves", "Rewards", "Star Requirements", "Database" };

    // UI State - ✅ FIXED: Initialize dictionary properly
    private Dictionary<int, bool> waveExpandedState = new Dictionary<int, bool>();

    // Templates
    private int selectedTemplateIndex = -1;
    private readonly BattleTemplate[] templates = {
        new BattleTemplate("Single Enemy", "Simple 1v1 battle"),
        new BattleTemplate("Small Group", "2-3 enemies, 1 wave"),
        new BattleTemplate("Progressive", "3 waves, increasing difficulty"),
        new BattleTemplate("Boss Fight", "Minions + Boss wave"),
        new BattleTemplate("Survival", "Multiple waves, time pressure")
    };

    private void OnEnable()
    {
        LoadDatabase();
        if (currentCombat == null)
            CreateNewBattle();
    }

    private void LoadDatabase()
    {
        levelDatabase = AssetDatabase.LoadAssetAtPath<LevelDatabase>(
            "Assets/00 Soulcast/Resources/Battle/Database/BattleDatabase.asset");

        if (levelDatabase == null)
        {
            Debug.LogWarning("BattleDatabase.asset not found in Resources/Battle/Database/");
        }
    }

    private void OnGUI()
    {
        // ✅ FIXED: Proper GUI layout structure
        try
        {
            EditorGUILayout.BeginVertical();

            DrawToolbar();
            EditorGUILayout.Space(5);

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawBasicInfoTab(); break;
                case 1: DrawWavesTab(); break;
                case 2: DrawRewardsTab(); break;
                case 3: DrawStarRequirementsTab(); break;
                case 4: DrawDatabaseTab(); break;
            }

            EditorGUILayout.EndScrollView();
            DrawBottomBar();

            EditorGUILayout.EndVertical();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GUI Error in BattleConfigurationEditorWindow: {e.Message}");
            GUIUtility.ExitGUI();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Battle Configuration Editor", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        // File operations
        if (GUILayout.Button("New", GUILayout.Width(60)))
            CreateNewBattle();

        if (GUILayout.Button("Load", GUILayout.Width(60)))
            LoadBattle();

        if (GUILayout.Button("Save", GUILayout.Width(60)))
            SaveBattle();

        if (GUILayout.Button("Save As", GUILayout.Width(70)))
            SaveBattleAs();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawBasicInfoTab()
    {
        if (currentCombat == null) return;

        EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Template Selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Template:", GUILayout.Width(80));
        selectedTemplateIndex = EditorGUILayout.Popup(selectedTemplateIndex,
            templates.Select(t => t.name).ToArray());

        if (selectedTemplateIndex >= 0 && GUILayout.Button("Apply Template", GUILayout.Width(100)))
            ApplyTemplate(templates[selectedTemplateIndex]);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Basic Info
        currentCombat.combatName = EditorGUILayout.TextField("Battle Name", currentCombat.combatName);
        currentCombat.combatDescription = EditorGUILayout.TextField("Description", currentCombat.combatDescription);
        currentCombat.battleIcon = (Sprite)EditorGUILayout.ObjectField("Battle Icon", currentCombat.battleIcon, typeof(Sprite), false);

        EditorGUILayout.Space(10);

        // Requirements
        EditorGUILayout.LabelField("Requirements", EditorStyles.boldLabel);
        currentCombat.requiredPlayerLevel = EditorGUILayout.IntField("Required Player Level", currentCombat.requiredPlayerLevel);
        currentCombat.recommendedTeamSize = EditorGUILayout.IntField("Recommended Team Size", currentCombat.recommendedTeamSize);
        currentCombat.difficulty = (BattleDifficulty)EditorGUILayout.EnumPopup("Difficulty", currentCombat.difficulty);
        currentCombat.timeLimit = EditorGUILayout.FloatField("Time Limit (0 = no limit)", currentCombat.timeLimit);

        EditorGUILayout.Space(5);

        // Recommended Roles
        EditorGUILayout.LabelField("Recommended Team Composition", EditorStyles.boldLabel);
        for (int i = 0; i < currentCombat.recommendedRoles.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            currentCombat.recommendedRoles[i] = (MonsterRole)EditorGUILayout.EnumPopup(currentCombat.recommendedRoles[i]);
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                currentCombat.recommendedRoles.RemoveAt(i);
                break; // ✅ FIXED: Break to avoid index issues
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Recommended Role"))
            currentCombat.recommendedRoles.Add(MonsterRole.DPS);

        // Quick Stats
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Stats", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Total Waves", currentCombat.TotalWaves);
            EditorGUILayout.IntField("Total Enemies", currentCombat.TotalEnemies);
            EditorGUILayout.FloatField("Estimated Duration", EstimateBattleDuration());
            EditorGUILayout.TextField("Recommended Composition", currentCombat.GetRecommendedComposition());
        }
    }

    private void DrawWavesTab()
    {
        if (currentCombat == null) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Wave Configuration", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Add Wave", GUILayout.Width(100)))
            AddNewWave();

        if (GUILayout.Button("Clear All", GUILayout.Width(100)))
            ClearAllWaves();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // ✅ FIXED: Ensure expanded state is properly initialized
        RefreshWaveExpandedState();

        // Wave List
        for (int i = 0; i < currentCombat.waves.Count; i++)
        {
            DrawWaveEditor(i);
            EditorGUILayout.Space(5);
        }

        if (currentCombat.waves.Count == 0)
        {
            EditorGUILayout.HelpBox("No waves configured. Add a wave to get started!", MessageType.Info);
        }
    }

    // ✅ NEW: Ensure wave expanded state is properly initialized
    private void RefreshWaveExpandedState()
    {
        for (int i = 0; i < currentCombat.waves.Count; i++)
        {
            if (!waveExpandedState.ContainsKey(i))
            {
                waveExpandedState[i] = true;
            }
        }

        // Remove any keys that are beyond the current wave count
        var keysToRemove = waveExpandedState.Keys.Where(k => k >= currentCombat.waves.Count).ToList();
        foreach (var key in keysToRemove)
        {
            waveExpandedState.Remove(key);
        }
    }

    private void DrawWaveEditor(int waveIndex)
    {
        if (waveIndex >= currentCombat.waves.Count) return;

        var wave = currentCombat.waves[waveIndex];

        EditorGUILayout.BeginVertical("box");

        // ✅ FIXED: Safe access to waveExpandedState
        bool isExpanded = waveExpandedState.ContainsKey(waveIndex) ? waveExpandedState[waveIndex] : true;

        // Wave Header
        EditorGUILayout.BeginHorizontal();

        isExpanded = EditorGUILayout.Foldout(isExpanded,
            $"Wave {waveIndex + 1}: {wave.waveName} ({wave.TotalEnemies} enemies)", true, EditorStyles.foldoutHeader);

        waveExpandedState[waveIndex] = isExpanded;

        GUILayout.FlexibleSpace();

        // Wave Controls
        if (GUILayout.Button("↑", GUILayout.Width(25)) && waveIndex > 0)
        {
            SwapWaves(waveIndex, waveIndex - 1);
            GUIUtility.ExitGUI(); // ✅ FIXED: Exit GUI after structural changes
        }

        if (GUILayout.Button("↓", GUILayout.Width(25)) && waveIndex < currentCombat.waves.Count - 1)
        {
            SwapWaves(waveIndex, waveIndex + 1);
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Clone", GUILayout.Width(50)))
        {
            CloneWave(waveIndex);
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            RemoveWave(waveIndex);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();

        if (isExpanded)
        {
            EditorGUI.indentLevel++;

            // Wave Settings
            wave.waveName = EditorGUILayout.TextField("Wave Name", wave.waveName);
            wave.waveDelay = EditorGUILayout.FloatField("Delay Before Wave", wave.waveDelay);
            wave.maxWaveTime = EditorGUILayout.FloatField("Max Wave Time (0 = no limit)", wave.maxWaveTime);
            wave.completionType = (WaveCompletionType)EditorGUILayout.EnumPopup("Completion Type", wave.completionType);

            EditorGUILayout.Space(5);

            // Enemy Spawns
            EditorGUILayout.LabelField("Enemy Spawns", EditorStyles.boldLabel);

            // ✅ FIXED: Safe enemy spawn iteration
            for (int j = wave.enemySpawns.Count - 1; j >= 0; j--)
            {
                if (j < wave.enemySpawns.Count) // Safety check
                    DrawEnemySpawnEditor(wave.enemySpawns[j], j, waveIndex);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button("Add Enemy", GUILayout.Width(100)))
                AddEnemySpawn(waveIndex);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEnemySpawnEditor(EnemySpawn spawn, int spawnIndex, int waveIndex)
    {
        EditorGUILayout.BeginVertical("helpbox");

        EditorGUILayout.BeginHorizontal();
        spawn.monsterData = (MonsterData)EditorGUILayout.ObjectField(
            $"Enemy {spawnIndex + 1}", spawn.monsterData, typeof(MonsterData), false);

        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            if (waveIndex < currentCombat.waves.Count && spawnIndex < currentCombat.waves[waveIndex].enemySpawns.Count)
            {
                currentCombat.waves[waveIndex].enemySpawns.RemoveAt(spawnIndex);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (spawn.monsterData != null)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            spawn.monsterLevel = EditorGUILayout.IntField("Level", spawn.monsterLevel, GUILayout.Width(200));
            spawn.starLevel = EditorGUILayout.IntSlider("Stars", spawn.starLevel, 1, 5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            spawn.spawnCount = EditorGUILayout.IntField("Count", spawn.spawnCount, GUILayout.Width(200));
            spawn.spawnDelay = EditorGUILayout.FloatField("Spawn Delay", spawn.spawnDelay);
            EditorGUILayout.EndHorizontal();

            spawn.spawnPattern = (SpawnPattern)EditorGUILayout.EnumPopup("Spawn Pattern", spawn.spawnPattern);

            // Show calculated stats
            try
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    var stats = spawn.GetEffectiveStats();
                    EditorGUILayout.TextField("Display Info", spawn.GetDisplayInfo());
                    EditorGUILayout.IntField("Effective HP", stats.health);
                    EditorGUILayout.IntField("Effective ATK", stats.attack);
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error calculating stats: {e.Message}", MessageType.Warning);
            }

            // Quick preset buttons
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Presets:", GUILayout.Width(60));
            if (GUILayout.Button("Weak", GUILayout.Width(50)))
                SetEnemyPreset(spawn, "weak");
            if (GUILayout.Button("Normal", GUILayout.Width(60)))
                SetEnemyPreset(spawn, "normal");
            if (GUILayout.Button("Strong", GUILayout.Width(60)))
                SetEnemyPreset(spawn, "strong");
            if (GUILayout.Button("Boss", GUILayout.Width(50)))
                SetEnemyPreset(spawn, "boss");
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRewardsTab()
    {
        if (currentCombat == null) return;

        EditorGUILayout.LabelField("Battle Rewards", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Initialize rewards if null
        if (currentCombat.rewards == null)
            currentCombat.rewards = new CombatRewards();

        // Basic Rewards
        currentCombat.rewards.soulCoins = EditorGUILayout.IntField("Soul Coins", currentCombat.rewards.soulCoins);
        currentCombat.rewards.experience = EditorGUILayout.IntField("Experience", currentCombat.rewards.experience);

        EditorGUILayout.Space(10);

        // Guaranteed Items
        EditorGUILayout.LabelField("Guaranteed Items", EditorStyles.boldLabel);
        DrawItemList(currentCombat.rewards.guaranteedItems, "guaranteed");

        EditorGUILayout.Space(10);

        // Random Items  
        EditorGUILayout.LabelField("Random Items", EditorStyles.boldLabel);
        DrawItemList(currentCombat.rewards.randomItems, "random");

        EditorGUILayout.Space(10);

        // Monster Rewards
        EditorGUILayout.LabelField("Unlockable Monsters", EditorStyles.boldLabel);
        DrawMonsterRewardList(currentCombat.rewards.unlockableMonsters);

        EditorGUILayout.Space(10);

        // Reward Presets
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Early Game"))
            SetRewardPreset("early");
        if (GUILayout.Button("Mid Game"))
            SetRewardPreset("mid");
        if (GUILayout.Button("Late Game"))
            SetRewardPreset("late");
        if (GUILayout.Button("Boss Rewards"))
            SetRewardPreset("boss");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStarRequirementsTab()
    {
        if (currentCombat == null) return;

        EditorGUILayout.LabelField("Star Requirements", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Initialize star requirements if null
        if (currentCombat.starRequirements == null)
            currentCombat.starRequirements = new StarRequirements();

        var starReq = currentCombat.starRequirements;

        // 1 Star
        EditorGUILayout.LabelField("⭐ One Star", EditorStyles.boldLabel);
        starReq.oneStarDescription = EditorGUILayout.TextField("Description", starReq.oneStarDescription);

        EditorGUILayout.Space(5);

        // 2 Stars
        EditorGUILayout.LabelField("⭐⭐ Two Stars", EditorStyles.boldLabel);
        starReq.twoStarCondition = (StarCondition)EditorGUILayout.EnumPopup("Condition", starReq.twoStarCondition);
        if (starReq.twoStarCondition == StarCondition.CompleteUnderTime)
            starReq.twoStarTimeLimit = EditorGUILayout.FloatField("Time Limit", starReq.twoStarTimeLimit);
        starReq.twoStarMaxLosses = EditorGUILayout.IntField("Max Losses", starReq.twoStarMaxLosses);
        starReq.twoStarDescription = EditorGUILayout.TextField("Description", starReq.twoStarDescription);

        EditorGUILayout.Space(5);

        // 3 Stars
        EditorGUILayout.LabelField("⭐⭐⭐ Three Stars", EditorStyles.boldLabel);
        starReq.threeStarCondition = (StarCondition)EditorGUILayout.EnumPopup("Condition", starReq.threeStarCondition);
        if (starReq.threeStarCondition == StarCondition.CompleteUnderTime)
            starReq.threeStarTimeLimit = EditorGUILayout.FloatField("Time Limit", starReq.threeStarTimeLimit);
        starReq.threeStarMaxLosses = EditorGUILayout.IntField("Max Losses", starReq.threeStarMaxLosses);
        starReq.threeStarDescription = EditorGUILayout.TextField("Description", starReq.threeStarDescription);
    }

    private void DrawDatabaseTab()
    {
        EditorGUILayout.LabelField("Database Management", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (levelDatabase == null)
        {
            EditorGUILayout.HelpBox("Database not loaded. Please ensure BattleDatabase.asset exists.", MessageType.Warning);
            if (GUILayout.Button("Create Database"))
                CreateDatabase();
            return;
        }

        // ✅ FIXED: Clean up null references before showing info
        CleanupDatabaseNullReferences();

        // Database Info
        EditorGUILayout.LabelField($"Total Battles: {levelDatabase.allCombats.Count}");
        EditorGUILayout.LabelField($"Regions: {levelDatabase.regions.Count}");

        EditorGUILayout.Space(10);

        // Quick Actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add to Database"))
            AddToDatabase();
        if (GUILayout.Button("Refresh Database"))
            LoadDatabase();
        if (GUILayout.Button("Clean Null Refs"))
            CleanupDatabaseNullReferences();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Batch Operations
        EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create 10 Battles"))
            CreateBattleBatch(10);
        if (GUILayout.Button("Validate All"))
            ValidateAllBattles();
        EditorGUILayout.EndHorizontal();
    }

    // ✅ NEW: Clean up null references in database
    private void CleanupDatabaseNullReferences()
    {
        if (levelDatabase == null) return;

        int removedCount = 0;

        // Clean allBattles list
        for (int i = levelDatabase.allCombats.Count - 1; i >= 0; i--)
        {
            if (levelDatabase.allCombats[i] == null)
            {
                levelDatabase.allCombats.RemoveAt(i);
                removedCount++;
            }
        }

        // Clean region battle references
        foreach (var region in levelDatabase.regions)
        {
            foreach (var level in region.levels)
            {
                for (int i = level.combats.Count - 1; i >= 0; i--)
                {
                    if (level.combats[i] == null)
                    {
                        level.combats.RemoveAt(i);
                        removedCount++;
                    }
                }
            }
        }

        if (removedCount > 0)
        {
            EditorUtility.SetDirty(levelDatabase);
            Debug.Log($"Cleaned up {removedCount} null references from database");
        }
    }

    private void DrawItemList(List<ItemReward> items, string type)
    {
        // ✅ FIXED: Safe iteration and proper layout
        for (int i = items.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            items[i].itemName = EditorGUILayout.TextField("Item Name", items[i].itemName, GUILayout.Width(200));
            items[i].itemIcon = (Sprite)EditorGUILayout.ObjectField(items[i].itemIcon, typeof(Sprite), false, GUILayout.Width(60));
            items[i].quantity = EditorGUILayout.IntField(items[i].quantity, GUILayout.Width(60));
            if (type == "random")
                items[i].dropChance = EditorGUILayout.Slider(items[i].dropChance, 0f, 1f, GUILayout.Width(100));
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                items.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button($"Add {type} item"))
            items.Add(new ItemReward());
    }

    private void DrawMonsterRewardList(List<MonsterReward> monsters)
    {
        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            monsters[i].monster = (MonsterData)EditorGUILayout.ObjectField(monsters[i].monster, typeof(MonsterData), false);
            monsters[i].level = EditorGUILayout.IntField("Lv", monsters[i].level, GUILayout.Width(50));
            monsters[i].starLevel = EditorGUILayout.IntSlider(monsters[i].starLevel, 1, 5, GUILayout.Width(100));
            monsters[i].unlockChance = EditorGUILayout.Slider(monsters[i].unlockChance, 0f, 1f, GUILayout.Width(100));
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                monsters.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Monster Reward"))
            monsters.Add(new MonsterReward());
    }

    private void DrawBottomBar()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal("toolbar");

        if (currentCombat != null)
        {
            GUILayout.Label($"Current: {currentCombat.combatName}");
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Preview", GUILayout.Width(60)))
            PreviewBattle();

        if (GUILayout.Button("Test", GUILayout.Width(60)))
            TestBattle();

        EditorGUILayout.EndHorizontal();
    }

    // Helper Methods
    private void CreateNewBattle()
    {
        currentCombat = CreateInstance<CombatTemplate>();
        currentCombat.combatName = "New Battle";
        currentCombat.combatDescription = "";
        currentCombat.requiredPlayerLevel = 1;
        currentCombat.recommendedTeamSize = 3;
        currentCombat.difficulty = BattleDifficulty.Normal;
        currentCombat.waves = new List<WaveConfiguration>();
        currentCombat.rewards = new CombatRewards();
        currentCombat.starRequirements = new StarRequirements();

        // Clear UI state
        waveExpandedState.Clear();

        // Add default wave
        AddNewWave();
    }

    private void LoadBattle()
    {
        string path = EditorUtility.OpenFilePanel("Load Battle Configuration",
            "Assets/00 Soulcast/Resources/Battle/Configs", "asset");

        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            currentCombat = AssetDatabase.LoadAssetAtPath<CombatTemplate>(path);

            // Reset UI state
            waveExpandedState.Clear();
            RefreshWaveExpandedState();
        }
    }

    private void SaveBattle()
    {
        if (currentCombat == null) return;

        string path = AssetDatabase.GetAssetPath(currentCombat);
        if (string.IsNullOrEmpty(path))
        {
            SaveBattleAs();
            return;
        }

        EditorUtility.SetDirty(currentCombat);
        AssetDatabase.SaveAssets();
        Debug.Log($"Saved battle: {currentCombat.combatName}");
    }

    private void SaveBattleAs()
    {
        if (currentCombat == null) return;

        string path = EditorUtility.SaveFilePanel("Save Battle Configuration",
            "Assets/00 Soulcast/Resources/Battle/Configs",
            currentCombat.combatName,
            "asset");

        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            AssetDatabase.CreateAsset(currentCombat, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created new battle: {path}");
        }
    }

    private void AddNewWave()
    {
        var wave = new WaveConfiguration();
        wave.waveName = $"Wave {currentCombat.waves.Count + 1}";
        wave.waveDelay = 2f;
        wave.enemySpawns = new List<EnemySpawn>();
        wave.completionType = WaveCompletionType.DefeatAllEnemies;

        currentCombat.waves.Add(wave);
        waveExpandedState[currentCombat.waves.Count - 1] = true;
    }

    private void AddEnemySpawn(int waveIndex)
    {
        if (waveIndex >= 0 && waveIndex < currentCombat.waves.Count)
        {
            var spawn = new EnemySpawn();
            spawn.monsterLevel = 1;
            spawn.starLevel = 1;
            spawn.spawnCount = 1;
            spawn.spawnDelay = 0;
            spawn.spawnPattern = SpawnPattern.Sequential;

            currentCombat.waves[waveIndex].enemySpawns.Add(spawn);
        }
    }

    private void SetEnemyPreset(EnemySpawn spawn, string preset)
    {
        switch (preset)
        {
            case "weak":
                spawn.monsterLevel = 1;
                spawn.starLevel = 1;
                spawn.spawnCount = 1;
                break;
            case "normal":
                spawn.monsterLevel = 5;
                spawn.starLevel = 2;
                spawn.spawnCount = 2;
                break;
            case "strong":
                spawn.monsterLevel = 10;
                spawn.starLevel = 3;
                spawn.spawnCount = 1;
                break;
            case "boss":
                spawn.monsterLevel = 15;
                spawn.starLevel = 4;
                spawn.spawnCount = 1;
                break;
        }
    }

    private void SetRewardPreset(string preset)
    {
        switch (preset)
        {
            case "early":
                currentCombat.rewards.soulCoins = 50;
                currentCombat.rewards.experience = 25;
                break;
            case "mid":
                currentCombat.rewards.soulCoins = 150;
                currentCombat.rewards.experience = 75;
                break;
            case "late":
                currentCombat.rewards.soulCoins = 300;
                currentCombat.rewards.experience = 150;
                break;
            case "boss":
                currentCombat.rewards.soulCoins = 500;
                currentCombat.rewards.experience = 250;
                break;
        }
    }

    private void ApplyTemplate(BattleTemplate template)
    {
        switch (template.name)
        {
            case "Single Enemy":
                currentCombat.waves.Clear();
                waveExpandedState.Clear();
                AddNewWave();
                AddEnemySpawn(0);
                break;
            case "Small Group":
                currentCombat.waves.Clear();
                waveExpandedState.Clear();
                AddNewWave();
                AddEnemySpawn(0);
                AddEnemySpawn(0);
                AddEnemySpawn(0);
                break;
            case "Progressive":
                currentCombat.waves.Clear();
                waveExpandedState.Clear();
                for (int i = 0; i < 3; i++)
                {
                    AddNewWave();
                    for (int j = 0; j <= i; j++)
                        AddEnemySpawn(i);
                }
                break;
        }
        Debug.Log($"Applied template: {template.name}");
    }

    private float EstimateBattleDuration()
    {
        float total = 0f;
        foreach (var wave in currentCombat.waves)
        {
            total += wave.waveDelay;
            total += 30f; // Estimated 30 seconds per wave
        }
        return total;
    }

    private void SwapWaves(int a, int b)
    {
        if (a >= 0 && a < currentCombat.waves.Count && b >= 0 && b < currentCombat.waves.Count)
        {
            var temp = currentCombat.waves[a];
            currentCombat.waves[a] = currentCombat.waves[b];
            currentCombat.waves[b] = temp;

            // Swap expanded states too
            bool tempExpanded = waveExpandedState.ContainsKey(a) ? waveExpandedState[a] : true;
            waveExpandedState[a] = waveExpandedState.ContainsKey(b) ? waveExpandedState[b] : true;
            waveExpandedState[b] = tempExpanded;
        }
    }

    private void CloneWave(int index)
    {
        if (index >= 0 && index < currentCombat.waves.Count)
        {
            var original = currentCombat.waves[index];
            var clone = new WaveConfiguration();
            clone.waveName = original.waveName + " (Copy)";
            clone.waveDelay = original.waveDelay;
            clone.maxWaveTime = original.maxWaveTime;
            clone.completionType = original.completionType;
            clone.enemySpawns = new List<EnemySpawn>(original.enemySpawns);

            currentCombat.waves.Insert(index + 1, clone);
            RefreshWaveExpandedState();
        }
    }

    private void RemoveWave(int index)
    {
        if (index >= 0 && index < currentCombat.waves.Count)
        {
            currentCombat.waves.RemoveAt(index);
            RefreshWaveExpandedState();
        }
    }

    private void ClearAllWaves()
    {
        if (EditorUtility.DisplayDialog("Clear All Waves",
            "Are you sure you want to remove all waves?", "Yes", "Cancel"))
        {
            currentCombat.waves.Clear();
            waveExpandedState.Clear();
        }
    }

    private void AddToDatabase()
    {
        if (levelDatabase != null && currentCombat != null)
        {
            if (!levelDatabase.allCombats.Contains(currentCombat))
            {
                levelDatabase.allCombats.Add(currentCombat);
                EditorUtility.SetDirty(levelDatabase);
                Debug.Log($"Added {currentCombat.combatName} to database");
            }
        }
    }

    private void CreateDatabase()
    {
        levelDatabase = CreateInstance<LevelDatabase>();
        AssetDatabase.CreateAsset(levelDatabase, "Assets/00 Soulcast/Resources/Battle/Database/BattleDatabase.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Created new BattleDatabase");
    }

    private void CreateBattleBatch(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var battle = CreateInstance<CombatTemplate>();
            battle.combatName = $"Auto Battle {i + 1}";
            battle.rewards = new CombatRewards();
            battle.starRequirements = new StarRequirements();
            battle.waves = new List<WaveConfiguration>();

            string path = $"Assets/00 Soulcast/Resources/Battle/Configs/Auto_Battle_{i + 1:00}.asset";
            AssetDatabase.CreateAsset(battle, path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Created {count} battles");
    }

    private void ValidateAllBattles()
    {
        if (levelDatabase == null) return;

        int errors = 0;
        foreach (var battle in levelDatabase.allCombats)
        {
            if (battle == null)
            {
                errors++;
                continue; // ✅ FIXED: Don't log error here, just count it
            }

            if (battle.waves.Count == 0)
            {
                Debug.LogWarning($"{battle.combatName}: No waves configured");
                errors++;
            }

            foreach (var wave in battle.waves)
            {
                if (wave.enemySpawns.Count == 0)
                {
                    Debug.LogWarning($"{battle.combatName}: Wave '{wave.waveName}' has no enemies");
                    errors++;
                }
            }
        }

        Debug.Log($"Validation complete. {errors} issues found.");
    }

    private void PreviewBattle()
    {
        if (currentCombat != null)
        {
            Debug.Log($"=== BATTLE PREVIEW: {currentCombat.combatName} ===");
            Debug.Log($"Difficulty: {currentCombat.difficulty}");
            Debug.Log($"Waves: {currentCombat.TotalWaves}");
            Debug.Log($"Total Enemies: {currentCombat.TotalEnemies}");
            Debug.Log($"Estimated Duration: {EstimateBattleDuration():F1} seconds");
            Debug.Log($"Recommended Team: {currentCombat.GetRecommendedComposition()}");
        }
    }

    private void TestBattle()
    {
        Debug.Log("Test battle functionality - implement based on your battle system");
    }
}

[System.Serializable]
public class BattleTemplate
{
    public string name;
    public string description;

    public BattleTemplate(string name, string description)
    {
        this.name = name;
        this.description = description;
    }
}
