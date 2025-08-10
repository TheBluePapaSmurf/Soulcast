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

    // ✅ REPLACE the DrawRewardsTab method in CombatCreatorEditorWindow.cs

    private void DrawRewardsTab()
    {
        EditorGUILayout.LabelField("Combat Rewards", EditorStyles.largeLabel);
        EditorGUILayout.Space(5);

        // Initialize rewards if null
        if (currentCombat.rewards == null)
            currentCombat.rewards = new CombatReward();

        // ✅ IMPROVED: More prominent configuration mode selection
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Reward Configuration Mode", EditorStyles.boldLabel);

        // Auto vs Manual toggle with better explanation
        bool useAuto = currentCombat.rewards.useRegionBasedDropRates;
        bool newUseAuto = EditorGUILayout.Toggle("Use Auto-Balance (Region-Based)", useAuto);
        if (newUseAuto != useAuto)
        {
            currentCombat.rewards.useRegionBasedDropRates = newUseAuto;
            EditorUtility.SetDirty(currentCombat);
        }

        if (currentCombat.rewards.useRegionBasedDropRates)
        {
            EditorGUILayout.HelpBox("✅ AUTO MODE: Rune rarity will be determined automatically based on region/chapter/level. Good for most battles.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚙️ MANUAL MODE: You can specify exact rune sets, slots, rarities and drop rates. Good for special rewards.", MessageType.Warning);
        }

        currentCombat.rewards.useRegionBasedCoins = EditorGUILayout.Toggle("Use Region-Based Coins", currentCombat.rewards.useRegionBasedCoins);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Soul Coins Configuration
        EditorGUILayout.LabelField("Soul Coins Reward", EditorStyles.boldLabel);
        if (currentCombat.rewards.useRegionBasedCoins)
        {
            EditorGUILayout.HelpBox("Coins will be calculated automatically based on region/chapter/level. Use the multiplier to adjust.", MessageType.Info);
            currentCombat.rewards.soulCoinMultiplier = EditorGUILayout.Slider("Coin Multiplier", currentCombat.rewards.soulCoinMultiplier, 0.1f, 5.0f);

            // Preview calculation
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preview (Region 1, Chapter 1, Level 1):");
            int previewCoins = Mathf.RoundToInt(currentCombat.rewards.CalculateSoulCoinsReward(1, 1, 1) * currentCombat.rewards.soulCoinMultiplier);
            EditorGUILayout.LabelField($"{previewCoins:N0} coins", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            currentCombat.rewards.baseSoulCoins = EditorGUILayout.IntField("Base Soul Coins", currentCombat.rewards.baseSoulCoins);
            currentCombat.rewards.soulCoinMultiplier = EditorGUILayout.Slider("Coin Multiplier", currentCombat.rewards.soulCoinMultiplier, 0.1f, 5.0f);

            int finalCoins = Mathf.RoundToInt(currentCombat.rewards.baseSoulCoins * currentCombat.rewards.soulCoinMultiplier);
            EditorGUILayout.LabelField($"Final Reward: {finalCoins:N0} coins", EditorStyles.helpBox);
        }

        EditorGUILayout.Space(10);

        // ✅ IMPROVED: Rune Rewards Configuration
        EditorGUILayout.LabelField("Rune Rewards", EditorStyles.boldLabel);

        currentCombat.rewards.guaranteedRuneDrop = EditorGUILayout.Toggle("Guaranteed Rune Drop", currentCombat.rewards.guaranteedRuneDrop);

        if (currentCombat.rewards.guaranteedRuneDrop)
        {
            currentCombat.rewards.maxRuneDrops = EditorGUILayout.IntSlider("Max Rune Drops", currentCombat.rewards.maxRuneDrops, 1, 5);

            if (currentCombat.rewards.useRegionBasedDropRates)
            {
                // ✅ AUTO MODE UI
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("🤖 Auto-Balance Mode Active", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Runes will be generated automatically with these rates:");

                // Preview for different regions
                EditorGUILayout.LabelField("📊 Drop Rate Preview:");
                EditorGUILayout.LabelField("Early Game (Regions 1-4): 70% Common, 25% Uncommon, 5% Rare");
                EditorGUILayout.LabelField("Mid Game (Regions 5-8): 40% Common, 45% Uncommon, 14% Rare, 1% Epic");
                EditorGUILayout.LabelField("Late Game (Regions 9-11): 20% Common, 35% Uncommon, 35% Rare, 9% Epic, 1% Legendary");
                EditorGUILayout.LabelField("End Game: 5% Common, 15% Uncommon, 40% Rare, 30% Epic, 10% Legendary");
                EditorGUILayout.EndVertical();

                // Quick switch to manual button
                EditorGUILayout.Space(5);
                if (GUILayout.Button("🔧 Switch to Manual Mode for Custom Runes"))
                {
                    currentCombat.rewards.useRegionBasedDropRates = false;
                    EditorUtility.SetDirty(currentCombat);
                }
            }
            else
            {
                // ✅ MANUAL MODE UI - IMPROVED
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("⚙️ Manual Mode Active - Custom Rune Configuration", EditorStyles.miniBoldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Configure exactly which runes can drop from this battle:");
                if (GUILayout.Button("🤖 Back to Auto Mode", GUILayout.Width(150)))
                {
                    currentCombat.rewards.useRegionBasedDropRates = true;
                    EditorUtility.SetDirty(currentCombat);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // ✅ IMPROVED: Guaranteed Runes Section
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("🎯 Guaranteed Runes (Always Drop)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("These runes will ALWAYS drop when the battle is won:");
                DrawEnhancedRuneRewardList(currentCombat.rewards.customGuaranteedRunes, "guaranteed");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // ✅ IMPROVED: Random Runes Section  
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("🎲 Random Runes (Chance to Drop)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("These runes have a chance to drop based on their drop rate:");
                DrawEnhancedRuneRewardList(currentCombat.rewards.customRandomRunes, "random");
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.Space(10);

        // ✅ IMPROVED: Quick Presets with better descriptions
        EditorGUILayout.LabelField("⚡ Quick Setup Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Reward Presets:");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Early Game\n(Regions 1-4)"))
            SetRewardPreset("early");
        if (GUILayout.Button("Mid Game\n(Regions 5-8)"))
            SetRewardPreset("mid");
        if (GUILayout.Button("Late Game\n(Regions 9-11)"))
            SetRewardPreset("late");
        if (GUILayout.Button("End Game\n(Region 12+)"))
            SetRewardPreset("endgame");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Custom Rune Presets (switches to Manual Mode):");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⚔️ Early ATK\n(Common)"))
            SetRunePreset("early_atk");
        if (GUILayout.Button("👑 Boss Rare\n(Rare)"))
            SetRunePreset("boss_rare");
        if (GUILayout.Button("💎 Epic Random\n(Epic)"))
            SetRunePreset("epic_random");
        if (GUILayout.Button("🌟 Legendary\n(Legendary)"))
            SetRunePreset("legendary");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // ✅ NEW: Quick Test Buttons
        EditorGUILayout.LabelField("🧪 Test Reward Generation", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Test what rewards this battle would give:");
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Test Region 1\n(Early Game)"))
            TestRewardGeneration(1, 1, 1);
        if (GUILayout.Button("Test Region 5\n(Mid Game)"))
            TestRewardGeneration(5, 1, 1);
        if (GUILayout.Button("Test Region 9\n(Late Game)"))
            TestRewardGeneration(9, 1, 1);
        if (GUILayout.Button("Test Boss\n(Chapter End)"))
            TestRewardGeneration(1, 1, 8);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void SetRunePreset(string presetType)
    {
        if (currentCombat.rewards == null)
            currentCombat.rewards = new CombatReward();

        RuneReward preset = null;

        switch (presetType)
        {
            case "early_atk":
                preset = RuneRewardTemplates.CreateEarlyGameATKRune();
                currentCombat.rewards.customGuaranteedRunes.Add(preset);
                break;
            case "boss_rare":
                preset = RuneRewardTemplates.CreateBossRareRune();
                currentCombat.rewards.customRandomRunes.Add(preset);
                break;
            case "epic_random":
                preset = RuneRewardTemplates.CreateLateGameEpicRune(); // ✅ NOW EXISTS
                currentCombat.rewards.customRandomRunes.Add(preset);
                break;
            case "legendary":
                preset = RuneRewardTemplates.CreateBossLegendaryRune();
                currentCombat.rewards.customRandomRunes.Add(preset);
                break;
        }

        if (preset != null)
        {
            // Switch to manual mode when adding custom runes
            currentCombat.rewards.useRegionBasedDropRates = false;
            Debug.Log($"Added rune preset: {presetType}");
        }
    }

    private void TestRewardGeneration(int region, int chapter, int level)
    {
        if (currentCombat.rewards == null)
        {
            Debug.LogWarning("No rewards configured to test!");
            return;
        }

        var result = currentCombat.rewards.GenerateRewards(region, chapter, level);

        Debug.Log($"=== TEST REWARDS (Region {region}, Chapter {chapter}, Level {level}) ===");
        Debug.Log($"Soul Coins: {result.soulCoinsEarned:N0}");
        Debug.Log($"Runes Earned: {result.runesEarned.Count}");

        foreach (var rune in result.runesEarned)
        {
            // ✅ FIXED: Use correct RuneData properties
            Debug.Log($"• {rune.rarity} {rune.runeName} - {rune.mainStat?.GetDisplayText()}");
        }
    }

    // ✅ FIXED: DrawEnhancedRuneRewardList with proper GUI layout handling
    private void DrawEnhancedRuneRewardList(List<RuneReward> runeRewards, string listType)
    {
        if (runeRewards.Count == 0)
        {
            EditorGUILayout.HelpBox($"No {listType} runes configured. Click 'Add {listType} rune' to start.", MessageType.Info);
        }

        // ✅ FIXED: Track which items to remove AFTER the GUI loop
        int indexToRemove = -1;

        for (int i = 0; i < runeRewards.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with rune info and remove button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Rune {i + 1}", EditorStyles.boldLabel, GUILayout.Width(60));

            // Quick rune summary
            string runeInfo = $"{runeRewards[i].rarity} {runeRewards[i].runeSet} {runeRewards[i].runeSlot}";
            if (listType == "random")
                runeInfo += $" ({runeRewards[i].dropChance:P0})";
            EditorGUILayout.LabelField(runeInfo, EditorStyles.miniLabel);

            if (GUILayout.Button("❌", GUILayout.Width(30)))
            {
                indexToRemove = i; // ✅ FIXED: Mark for removal instead of immediate removal
            }
            EditorGUILayout.EndHorizontal(); // ✅ PROPERLY CLOSED

            EditorGUILayout.Space(3);

            // Better organized controls
            EditorGUILayout.BeginHorizontal();

            // Rune Set
            EditorGUILayout.LabelField("Set:", GUILayout.Width(30));
            runeRewards[i].runeSet = (RuneType)EditorGUILayout.EnumPopup(runeRewards[i].runeSet, GUILayout.Width(80));

            // Rune Slot
            EditorGUILayout.LabelField("Slot:", GUILayout.Width(35));
            runeRewards[i].runeSlot = (RuneSlotPosition)EditorGUILayout.EnumPopup(runeRewards[i].runeSlot, GUILayout.Width(60));

            // Rune Rarity
            EditorGUILayout.LabelField("Rarity:", GUILayout.Width(45));
            runeRewards[i].rarity = (RuneRarity)EditorGUILayout.EnumPopup(runeRewards[i].rarity, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal(); // ✅ PROPERLY CLOSED

            // Drop Chance (only for random runes)
            if (listType == "random")
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Drop Chance:", GUILayout.Width(85));
                runeRewards[i].dropChance = EditorGUILayout.Slider(runeRewards[i].dropChance, 0f, 1f);
                EditorGUILayout.LabelField($"{runeRewards[i].dropChance:P1}", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal(); // ✅ PROPERLY CLOSED
            }

            // AUTO-BALANCE toggle
            EditorGUILayout.BeginHorizontal();
            runeRewards[i].useAutoBalance = EditorGUILayout.Toggle("Auto-Balance Stats", runeRewards[i].useAutoBalance);
            if (!runeRewards[i].useAutoBalance)
            {
                if (GUILayout.Button("⚙️ Manual Stats", GUILayout.Width(100)))
                {
                    Debug.Log("Manual stats editing - TODO: Implement detailed stats editor");
                }
            }
            EditorGUILayout.EndHorizontal(); // ✅ PROPERLY CLOSED

            // Show what this rune would generate
            if (runeRewards[i].useAutoBalance)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel, GUILayout.Width(50));
                string preview = GetRunePreview(runeRewards[i]);
                EditorGUILayout.LabelField(preview, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal(); // ✅ PROPERLY CLOSED
            }

            EditorGUILayout.EndVertical(); // ✅ PROPERLY CLOSED
            EditorGUILayout.Space(5);
        }

        // ✅ FIXED: Handle removal AFTER the GUI loop
        if (indexToRemove >= 0)
        {
            runeRewards.RemoveAt(indexToRemove);
            EditorUtility.SetDirty(currentCombat);
        }

        // Add button with preset options
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button($"➕ Add {listType} rune"))
        {
            var newRune = new RuneReward
            {
                runeSet = RuneType.Blade,
                runeSlot = RuneSlotPosition.Slot1,
                rarity = RuneRarity.Common,
                dropChance = listType == "random" ? 0.5f : 1.0f,
                useAutoBalance = true
            };
            runeRewards.Add(newRune);
            EditorUtility.SetDirty(currentCombat);
        }

        // Quick add preset buttons
        if (GUILayout.Button("⚔️ ATK", GUILayout.Width(50)))
            AddPresetRune(runeRewards, "atk", listType);
        if (GUILayout.Button("🛡️ DEF", GUILayout.Width(50)))
            AddPresetRune(runeRewards, "def", listType);
        if (GUILayout.Button("❤️ HP", GUILayout.Width(50)))
            AddPresetRune(runeRewards, "hp", listType);
        if (GUILayout.Button("⚡ SPD", GUILayout.Width(50)))
            AddPresetRune(runeRewards, "spd", listType);

        EditorGUILayout.EndHorizontal(); // ✅ PROPERLY CLOSED
    }


    // ✅ NEW: Helper method to add preset runes quickly
    private void AddPresetRune(List<RuneReward> runeList, string type, string listType)
    {
        RuneReward newRune = null;

        switch (type)
        {
            case "atk":
                newRune = new RuneReward
                {
                    runeSet = RuneType.Blade,
                    runeSlot = RuneSlotPosition.Slot1, // ATK Flat
                    rarity = RuneRarity.Uncommon,
                    dropChance = listType == "random" ? 0.3f : 1.0f,
                    useAutoBalance = true
                };
                break;
            case "def":
                newRune = new RuneReward
                {
                    runeSet = RuneType.Guard,
                    runeSlot = RuneSlotPosition.Slot3, // DEF Flat
                    rarity = RuneRarity.Uncommon,
                    dropChance = listType == "random" ? 0.3f : 1.0f,
                    useAutoBalance = true
                };
                break;
            case "hp":
                newRune = new RuneReward
                {
                    runeSet = RuneType.Energy,
                    runeSlot = RuneSlotPosition.Slot5, // HP Flat
                    rarity = RuneRarity.Uncommon,
                    dropChance = listType == "random" ? 0.3f : 1.0f,
                    useAutoBalance = true
                };
                break;
            case "spd":
                newRune = new RuneReward
                {
                    runeSet = RuneType.Swift,
                    runeSlot = RuneSlotPosition.Slot2, // SPD main stat possible
                    rarity = RuneRarity.Rare,
                    dropChance = listType == "random" ? 0.15f : 1.0f,
                    useAutoBalance = true
                };
                break;
        }

        if (newRune != null)
        {
            runeList.Add(newRune);
            EditorUtility.SetDirty(currentCombat);
        }
    }

    // ✅ NEW: Helper method to show rune preview
    private string GetRunePreview(RuneReward rune)
    {
        // This would show what the rune's main stat would be
        string mainStat = "Unknown";

        switch (rune.runeSlot)
        {
            case RuneSlotPosition.Slot1:
                mainStat = "ATK Flat";
                break;
            case RuneSlotPosition.Slot2:
                mainStat = "SPD/ATK%/HP%/etc"; // Random slot
                break;
            case RuneSlotPosition.Slot3:
                mainStat = "DEF Flat";
                break;
            case RuneSlotPosition.Slot4:
                mainStat = "Random Main Stat"; // Random slot
                break;
            case RuneSlotPosition.Slot5:
                mainStat = "HP Flat";
                break;
            case RuneSlotPosition.Slot6:
                mainStat = "Random Main Stat"; // Random slot
                break;
        }

        int substats = 0;
        switch (rune.rarity)
        {
            case RuneRarity.Common: substats = 0; break;
            case RuneRarity.Uncommon: substats = 1; break;
            case RuneRarity.Rare: substats = 2; break;
            case RuneRarity.Epic: substats = 3; break;
            case RuneRarity.Legendary: substats = 4; break;
        }

        return $"{mainStat} + {substats} substats";
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
        currentCombat.rewards = new CombatReward();
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

    private void SetRewardPreset(string presetType)
    {
        if (currentCombat.rewards == null)
            currentCombat.rewards = new CombatReward();

        switch (presetType)
        {
            case "early":
                currentCombat.rewards.baseSoulCoins = 1000;
                currentCombat.rewards.soulCoinMultiplier = 1.0f;
                currentCombat.rewards.useRegionBasedCoins = true;
                currentCombat.rewards.useRegionBasedDropRates = true;
                currentCombat.rewards.guaranteedRuneDrop = true;
                currentCombat.rewards.maxRuneDrops = 1;
                break;

            case "mid":
                currentCombat.rewards.baseSoulCoins = 3000;
                currentCombat.rewards.soulCoinMultiplier = 1.2f;
                currentCombat.rewards.useRegionBasedCoins = true;
                currentCombat.rewards.useRegionBasedDropRates = true;
                currentCombat.rewards.guaranteedRuneDrop = true;
                currentCombat.rewards.maxRuneDrops = 1;
                break;

            case "late":
                currentCombat.rewards.baseSoulCoins = 8000;
                currentCombat.rewards.soulCoinMultiplier = 1.5f;
                currentCombat.rewards.useRegionBasedCoins = true;
                currentCombat.rewards.useRegionBasedDropRates = true;
                currentCombat.rewards.guaranteedRuneDrop = true;
                currentCombat.rewards.maxRuneDrops = 2;
                break;

            case "endgame":
                currentCombat.rewards.baseSoulCoins = 15000;
                currentCombat.rewards.soulCoinMultiplier = 2.0f;
                currentCombat.rewards.useRegionBasedCoins = true;
                currentCombat.rewards.useRegionBasedDropRates = true;
                currentCombat.rewards.guaranteedRuneDrop = true;
                currentCombat.rewards.maxRuneDrops = 3;
                break;

            case "boss":
                currentCombat.rewards.soulCoinMultiplier = 2.5f;
                currentCombat.rewards.maxRuneDrops = 2;
                break;
        }

        Debug.Log($"Applied {presetType} reward preset");
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
            battle.rewards = new CombatReward();
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
