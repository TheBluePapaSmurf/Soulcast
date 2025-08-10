// Create: Assets/00 Soulcast/Scripts/Core/BattleProgressionManager.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class BattleProgress
{
    public bool isCompleted;
    public int starsEarned;
    public float completionTime;
    public DateTime completedDate;

    public BattleProgress()
    {
        isCompleted = false;
        starsEarned = 0;
        completionTime = 0f;
        completedDate = default;
    }

    public BattleProgress(int stars, float time)
    {
        isCompleted = true;
        starsEarned = stars;
        completionTime = time;
        completedDate = DateTime.Now;
    }
}

[System.Serializable]
public class LevelProgress
{
    public bool isUnlocked;
    public bool isCompleted;
    public int totalStarsEarned;
    public Dictionary<int, BattleProgress> battleProgress; // battleId -> progress

    public LevelProgress()
    {
        isUnlocked = false;
        isCompleted = false;
        totalStarsEarned = 0;
        battleProgress = new Dictionary<int, BattleProgress>();
    }
}

[System.Serializable]
public class RegionProgress
{
    public bool isUnlocked;
    public bool isCompleted;
    public Dictionary<int, LevelProgress> levelProgress; // levelId -> progress

    public RegionProgress()
    {
        isUnlocked = false;
        isCompleted = false;
        levelProgress = new Dictionary<int, LevelProgress>();
    }
}

public class BattleProgressionManager : MonoBehaviour
{
    public static BattleProgressionManager Instance { get; private set; }

    [Header("Progression Settings")]
    public bool debugMode = false;
    public int maxRegions = 12;
    public int maxLevelsPerRegion = 8;

    [Header("Auto-Save")]
    public bool autoSaveProgression = true;

    // ✅ Easy Save 3 Keys
    private const string PROGRESSION_DATA_KEY = "BattleProgression";
    private const string CURRENT_REGION_KEY = "CurrentRegion";
    private const string CURRENT_LEVEL_KEY = "CurrentLevel";

    // Runtime data
    private Dictionary<int, RegionProgress> progressionData;

    // Events
    public static event Action<int, int, int, int> OnBattleCompleted; // region, level, battle, stars
    public static event Action<int, int> OnLevelCompleted; // region, level
    public static event Action<int> OnRegionCompleted; // region
    public static event Action OnProgressionLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeProgression();
            Debug.Log("🏆 BattleProgressionManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load progression data after other managers are ready
        LoadProgression();
    }

    private void InitializeProgression()
    {
        progressionData = new Dictionary<int, RegionProgress>();

        // Initialize all regions
        for (int regionId = 1; regionId <= maxRegions; regionId++)
        {
            var regionProgress = new RegionProgress();

            // Initialize all levels in region
            for (int levelId = 1; levelId <= maxLevelsPerRegion; levelId++)
            {
                regionProgress.levelProgress[levelId] = new LevelProgress();
            }

            progressionData[regionId] = regionProgress;
        }

        // ✅ Always unlock first region and level
        UnlockRegion(1);
        UnlockLevel(1, 1);
    }

    // ✅ MAIN API: Complete a battle
    public void CompleteBattle(int regionId, int levelId, int battleId, int starsEarned, float completionTime = 0f)
    {
        if (!IsValidBattleId(regionId, levelId, battleId)) return;

        // Ensure region and level exist
        EnsureRegionExists(regionId);
        EnsureLevelExists(regionId, levelId);

        var battleProgress = new BattleProgress(starsEarned, completionTime);
        progressionData[regionId].levelProgress[levelId].battleProgress[battleId] = battleProgress;

        if (debugMode)
        {
            Debug.Log($"🏆 Battle completed: Region {regionId}, Level {levelId}, Battle {battleId} ({starsEarned}★)");
        }

        // Check if level is completed
        CheckLevelCompletion(regionId, levelId);

        // Auto-save progression
        if (autoSaveProgression)
        {
            SaveProgression();
        }

        // Fire events
        OnBattleCompleted?.Invoke(regionId, levelId, battleId, starsEarned);
    }

    // ✅ Check if level is completed and handle progression
    private void CheckLevelCompletion(int regionId, int levelId)
    {
        var levelProgress = progressionData[regionId].levelProgress[levelId];

        // Get all battles in this level from database
        var battleDatabase = Resources.Load<LevelDatabase>("Battle/Database/BattleDatabase");
        if (battleDatabase == null) return;

        var levelBattles = battleDatabase.GetLevelBattles(regionId, levelId);
        if (levelBattles.Count == 0) return;

        // Check if all battles are completed
        bool allBattlesCompleted = true;
        int totalStars = 0;

        for (int battleId = 1; battleId <= levelBattles.Count; battleId++)
        {
            if (levelProgress.battleProgress.ContainsKey(battleId) &&
                levelProgress.battleProgress[battleId].isCompleted)
            {
                totalStars += levelProgress.battleProgress[battleId].starsEarned;
            }
            else
            {
                allBattlesCompleted = false;
                break;
            }
        }

        if (allBattlesCompleted && !levelProgress.isCompleted)
        {
            // Mark level as completed
            levelProgress.isCompleted = true;
            levelProgress.totalStarsEarned = totalStars;

            if (debugMode)
            {
                Debug.Log($"🎉 Level completed: Region {regionId}, Level {levelId} ({totalStars}★ total)");
            }

            // Unlock next level
            UnlockLevel(regionId, levelId + 1);

            // Check if region is completed
            CheckRegionCompletion(regionId);

            OnLevelCompleted?.Invoke(regionId, levelId);
        }
    }

    // ✅ Check if region is completed
    private void CheckRegionCompletion(int regionId)
    {
        var regionProgress = progressionData[regionId];

        bool allLevelsCompleted = regionProgress.levelProgress.Values.All(level => level.isCompleted);

        if (allLevelsCompleted && !regionProgress.isCompleted)
        {
            regionProgress.isCompleted = true;

            if (debugMode)
            {
                Debug.Log($"🌟 Region completed: Region {regionId}");
            }

            // Unlock next region
            UnlockRegion(regionId + 1);

            OnRegionCompleted?.Invoke(regionId);
        }
    }

    // ✅ Unlock region
    public void UnlockRegion(int regionId)
    {
        if (regionId > maxRegions) return;

        EnsureRegionExists(regionId);

        if (!progressionData[regionId].isUnlocked)
        {
            progressionData[regionId].isUnlocked = true;

            if (debugMode)
            {
                Debug.Log($"🔓 Region {regionId} unlocked!");
            }
        }
    }

    // ✅ Unlock level
    public void UnlockLevel(int regionId, int levelId)
    {
        if (levelId > maxLevelsPerRegion) return;

        EnsureRegionExists(regionId);
        EnsureLevelExists(regionId, levelId);

        if (!progressionData[regionId].levelProgress[levelId].isUnlocked)
        {
            progressionData[regionId].levelProgress[levelId].isUnlocked = true;

            if (debugMode)
            {
                Debug.Log($"🔓 Level {levelId} in Region {regionId} unlocked!");
            }
        }
    }

    // ✅ QUERY API
    public bool IsBattleCompleted(int regionId, int levelId, int battleId)
    {
        if (!IsValidBattleId(regionId, levelId, battleId)) return false;

        return progressionData[regionId].levelProgress[levelId].battleProgress.ContainsKey(battleId) &&
               progressionData[regionId].levelProgress[levelId].battleProgress[battleId].isCompleted;
    }

    public bool IsLevelCompleted(int regionId, int levelId)
    {
        if (!IsValidLevelId(regionId, levelId)) return false;
        return progressionData[regionId].levelProgress[levelId].isCompleted;
    }

    public bool IsLevelUnlocked(int regionId, int levelId)
    {
        if (!IsValidLevelId(regionId, levelId)) return false;
        return progressionData[regionId].levelProgress[levelId].isUnlocked;
    }

    public bool IsRegionUnlocked(int regionId)
    {
        if (!IsValidRegionId(regionId)) return false;
        return progressionData[regionId].isUnlocked;
    }

    public int GetBattleStars(int regionId, int levelId, int battleId)
    {
        if (!IsValidBattleId(regionId, levelId, battleId)) return 0;

        if (progressionData[regionId].levelProgress[levelId].battleProgress.ContainsKey(battleId))
        {
            return progressionData[regionId].levelProgress[levelId].battleProgress[battleId].starsEarned;
        }
        return 0;
    }

    public int GetLevelStars(int regionId, int levelId)
    {
        if (!IsValidLevelId(regionId, levelId)) return 0;
        return progressionData[regionId].levelProgress[levelId].totalStarsEarned;
    }

    // ✅ EASY SAVE 3 INTEGRATION
    public void SaveProgression()
    {
        try
        {
            ES3.Save(PROGRESSION_DATA_KEY, progressionData, SaveManager.SAVE_FILE);

            if (debugMode)
            {
                Debug.Log("💾 Battle progression saved to Easy Save 3");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save battle progression: {e.Message}");
        }
    }

    public void LoadProgression()
    {
        try
        {
            if (ES3.KeyExists(PROGRESSION_DATA_KEY, SaveManager.SAVE_FILE))
            {
                progressionData = ES3.Load<Dictionary<int, RegionProgress>>(PROGRESSION_DATA_KEY, SaveManager.SAVE_FILE);

                if (debugMode)
                {
                    Debug.Log("📂 Battle progression loaded from Easy Save 3");
                    LogProgressionSummary();
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log("📝 No battle progression found - using default progression");
                }

                // Ensure first region/level is unlocked
                UnlockRegion(1);
                UnlockLevel(1, 1);
            }

            OnProgressionLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load battle progression: {e.Message}");
            InitializeProgression(); // Fallback to default
        }
    }

    // ✅ UTILITY METHODS
    private void EnsureRegionExists(int regionId)
    {
        if (!progressionData.ContainsKey(regionId))
        {
            progressionData[regionId] = new RegionProgress();
        }
    }

    private void EnsureLevelExists(int regionId, int levelId)
    {
        if (!progressionData[regionId].levelProgress.ContainsKey(levelId))
        {
            progressionData[regionId].levelProgress[levelId] = new LevelProgress();
        }
    }

    private bool IsValidRegionId(int regionId) => regionId >= 1 && regionId <= maxRegions;
    private bool IsValidLevelId(int regionId, int levelId) => IsValidRegionId(regionId) && levelId >= 1 && levelId <= maxLevelsPerRegion;
    private bool IsValidBattleId(int regionId, int levelId, int battleId) => IsValidLevelId(regionId, levelId) && battleId >= 1;

    // ✅ DEBUG & TESTING
    private void LogProgressionSummary()
    {
        Debug.Log("=== BATTLE PROGRESSION SUMMARY ===");

        foreach (var regionKvp in progressionData)
        {
            int regionId = regionKvp.Key;
            var regionProgress = regionKvp.Value;

            if (!regionProgress.isUnlocked) continue;

            Debug.Log($"Region {regionId}: {(regionProgress.isCompleted ? "✅ COMPLETED" : "🔄 In Progress")}");

            foreach (var levelKvp in regionProgress.levelProgress)
            {
                int levelId = levelKvp.Key;
                var levelProgress = levelKvp.Value;

                if (!levelProgress.isUnlocked) continue;

                Debug.Log($"  Level {levelId}: {(levelProgress.isCompleted ? "✅" : "🔄")} ({levelProgress.totalStarsEarned}★)");
            }
        }
    }

    [ContextMenu("Save Progression")]
    public void ManualSaveProgression() => SaveProgression();

    [ContextMenu("Load Progression")]
    public void ManualLoadProgression() => LoadProgression();

    [ContextMenu("Reset All Progression")]
    public void ResetAllProgression()
    {
        if (Application.isPlaying)
        {
            InitializeProgression();
            SaveProgression();
            Debug.Log("🔄 All battle progression reset!");
        }
    }

    [ContextMenu("Unlock All Content")]
    public void UnlockAllContent()
    {
        if (Application.isPlaying)
        {
            // Unlock and complete everything for testing
            for (int regionId = 1; regionId <= maxRegions; regionId++)
            {
                UnlockRegion(regionId);

                for (int levelId = 1; levelId <= maxLevelsPerRegion; levelId++)
                {
                    UnlockLevel(regionId, levelId);

                    // Complete sample battles with max stars
                    for (int battleId = 1; battleId <= 5; battleId++)
                    {
                        CompleteBattle(regionId, levelId, battleId, 3);
                    }
                }
            }

            Debug.Log("🔓 All content unlocked and completed!");
        }
    }

    [ContextMenu("Show Current Progress")]
    public void ShowCurrentProgress()
    {
        if (Application.isPlaying)
        {
            LogProgressionSummary();
        }
    }
}
