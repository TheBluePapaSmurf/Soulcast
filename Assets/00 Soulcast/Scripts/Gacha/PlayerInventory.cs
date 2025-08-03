using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerInventory : MonoBehaviour
{
    [Header("Currency")]
    public int soulCoins = 6000;

    [Header("Monster Collection")]
    public List<CollectedMonster> collectedMonsters = new List<CollectedMonster>();

    [Header("Rune Collection")]
    public List<RuneData> ownedRunes = new List<RuneData>();
    public int maxRuneCapacity = 200;

    [Header("Settings")]
    public int maxCollectionSize = 100;

    [Header("Easy Save 3 Settings")]
    public string saveFileName = "PlayerSave.es3";
    public ES3Settings saveSettings;

    public static PlayerInventory Instance { get; private set; }

    void Awake()
    {
        // Handle singleton pattern more carefully
        if (Instance == null)
        {
            Instance = this;

            // Only make persistent if we're not in a UI scene
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isUIScene = sceneName.Contains("Inventory") || sceneName.Contains("Gacha") || sceneName.Contains("Menu");

            if (!isUIScene)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("PlayerInventory: Made persistent (Main game scene)");
            }
            else
            {
                Debug.Log("PlayerInventory: Local instance (UI scene)");
            }
        }
        else
        {
            // If we're in a UI scene and Instance already exists, don't destroy this one
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isUIScene = sceneName.Contains("Inventory") || sceneName.Contains("Gacha") || sceneName.Contains("Menu");

            if (isUIScene)
            {
                // Keep local instance for UI, but don't make it the global Instance
                Debug.Log("PlayerInventory: Keeping local UI instance");
                return;
            }

            // In main game scenes, destroy duplicate
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Automatically load game data when starting
        LoadGame();
    }

    // ========== MONSTER MANAGEMENT ==========

    public void AddMonster(MonsterData monsterData)
    {
        if (monsterData == null) return;

        CollectedMonster newMonster = new CollectedMonster(monsterData);
        collectedMonsters.Add(newMonster);

        Debug.Log($"Added new monster to collection: {monsterData.monsterName} (Total: {GetMonsterCount(monsterData)})");
        AutoSave(); // Auto-save after getting new monster

        if (collectedMonsters.Count > maxCollectionSize)
        {
            Debug.LogWarning("Collection is at maximum capacity!");
        }
    }

    public int GetMonsterCount(MonsterData monsterData)
    {
        return collectedMonsters.Count(m => m.monsterData == monsterData);
    }

    public List<CollectedMonster> GetMonstersByType(MonsterData monsterData)
    {
        return collectedMonsters.Where(m => m.monsterData == monsterData).ToList();
    }

    public List<MonsterData> GetUniqueMonsterTypes()
    {
        return collectedMonsters.Select(m => m.monsterData).Distinct().ToList();
    }

    public List<CollectedMonster> GetAllMonsters()
    {
        return collectedMonsters.ToList();
    }

    public int GetCollectionCount()
    {
        return collectedMonsters.Count;
    }

    public CollectedMonster GetMonster(MonsterData monsterData)
    {
        return collectedMonsters.FirstOrDefault(m => m.monsterData == monsterData);
    }

    public List<CollectedMonster> GetUniqueMonsters()
    {
        var uniqueMonsters = new List<CollectedMonster>();
        var seenTypes = new HashSet<MonsterData>();

        foreach (var monster in collectedMonsters)
        {
            if (monster?.monsterData != null && !seenTypes.Contains(monster.monsterData))
            {
                seenTypes.Add(monster.monsterData);
                uniqueMonsters.Add(monster);
            }
        }

        return uniqueMonsters;
    }

    public CollectedMonster GetMonsterByID(string uniqueID)
    {
        return collectedMonsters.FirstOrDefault(m => m.uniqueID == uniqueID);
    }

    // ========== RUNE MANAGEMENT ==========

    public bool AddRune(RuneData rune)
    {
        if (ownedRunes.Count >= maxRuneCapacity)
        {
            Debug.LogWarning("Rune inventory is full!");
            return false;
        }

        ownedRunes.Add(rune);
        Debug.Log($"Added rune: {rune.runeName}");
        AutoSave(); // Auto-save after getting new rune
        return true;
    }

    public void RemoveRune(RuneData rune)
    {
        if (ownedRunes.Contains(rune))
        {
            ownedRunes.Remove(rune);
            Debug.Log($"Removed {rune.runeName} from inventory");
            AutoSave(); // Auto-save after removing rune
        }
    }

    public List<RuneData> GetRunesByType(RuneType runeType)
    {
        return ownedRunes.Where(r => r.runeType == runeType).ToList();
    }

    public List<RuneData> GetRunesByRarity(RuneRarity rarity)
    {
        return ownedRunes.Where(r => r.rarity == rarity).ToList();
    }

    public List<RuneData> GetUnequippedRunes()
    {
        var equippedRunes = new HashSet<RuneData>();

        // Collect all equipped runes from all monsters
        foreach (var monster in collectedMonsters)
        {
            foreach (var runeName in monster.equippedRuneNames)
            {
                if (!string.IsNullOrEmpty(runeName))
                {
                    var rune = ownedRunes.FirstOrDefault(r => r.name == runeName);
                    if (rune != null)
                    {
                        equippedRunes.Add(rune);
                    }
                }
            }
        }

        // Return runes that are not equipped
        return ownedRunes.Where(r => !equippedRunes.Contains(r)).ToList();
    }

    public List<RuneData> GetUnequippedRunesByType(RuneType runeType)
    {
        return GetUnequippedRunes().Where(r => r.runeType == runeType).ToList();
    }

    public bool EquipRuneToMonster(string monsterID, int slotIndex, RuneData rune)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null)
        {
            Debug.LogWarning($"Monster with ID {monsterID} not found!");
            return false;
        }

        // Unequip rune from previous location first
        UnequipRuneFromAllMonsters(rune);

        bool success = monster.EquipRune(rune, slotIndex);
        if (success)
        {
            AutoSave(); // Auto-save after equipping rune
        }
        return success;
    }

    public RuneData UnequipRuneFromMonster(string monsterID, int slotIndex)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null)
        {
            Debug.LogWarning($"Monster with ID {monsterID} not found!");
            return null;
        }

        RuneData unequippedRune = monster.UnequipRune(slotIndex);
        if (unequippedRune != null)
        {
            AutoSave(); // Auto-save after unequipping rune
        }
        return unequippedRune;
    }

    private void UnequipRuneFromAllMonsters(RuneData rune)
    {
        foreach (var monster in collectedMonsters)
        {
            for (int i = 0; i < monster.equippedRuneNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(monster.equippedRuneNames[i]) && monster.equippedRuneNames[i] == rune.name)
                {
                    monster.UnequipRune(i);
                    return; // Rune can only be equipped once
                }
            }
        }
    }

    public bool UpgradeRune(RuneData rune)
    {
        if (rune.currentLevel >= rune.maxLevel)
        {
            Debug.LogWarning($"{rune.runeName} is already at max level!");
            return false;
        }

        int cost = rune.GetUpgradeCost(rune.currentLevel);
        if (!CanAfford(cost))
        {
            Debug.LogWarning($"Not enough Soul Coins to upgrade {rune.runeName}! Need {cost}, have {soulCoins}");
            return false;
        }

        SpendSoulCoins(cost);
        rune.currentLevel++;

        Debug.Log($"Upgraded {rune.runeName} to level {rune.currentLevel} for {cost} Soul Coins!");
        AutoSave(); // Auto-save after upgrading rune
        return true;
    }

    // ========== SOUL COINS CURRENCY MANAGEMENT ==========

    public void AddSoulCoins(int amount)
    {
        soulCoins += amount;
        Debug.Log($"Added {amount} Soul Coins. Total: {soulCoins}");
        AutoSave(); // Auto-save after currency change
    }

    public bool SpendSoulCoins(int amount)
    {
        if (soulCoins >= amount)
        {
            soulCoins -= amount;
            Debug.Log($"Spent {amount} Soul Coins. Remaining: {soulCoins}");
            AutoSave(); // Auto-save after currency change
            return true;
        }

        Debug.LogWarning($"Not enough Soul Coins! Have {soulCoins}, need {amount}");
        return false;
    }

    public int GetSoulCoins()
    {
        return soulCoins;
    }

    public bool CanAfford(int amount)
    {
        return soulCoins >= amount;
    }

    // ========== RUNE SELLING SYSTEM ==========

    public bool SellRune(RuneData rune, out int sellPrice)
    {
        sellPrice = 0;

        if (!ownedRunes.Contains(rune))
        {
            Debug.LogWarning("Cannot sell rune: not owned by player!");
            return false;
        }

        // Check if rune is currently equipped
        if (IsRuneEquipped(rune))
        {
            Debug.LogWarning("Cannot sell equipped rune! Unequip first.");
            return false;
        }

        // Calculate sell price
        sellPrice = CalculateRuneSellPrice(rune);

        // Remove rune from inventory
        RemoveRune(rune);

        // Add Soul Coins
        AddSoulCoins(sellPrice);

        Debug.Log($"Sold {rune.runeName} for {sellPrice} Soul Coins!");
        return true;
    }

    private bool IsRuneEquipped(RuneData rune)
    {
        foreach (var monster in collectedMonsters)
        {
            foreach (var runeName in monster.equippedRuneNames)
            {
                if (runeName == rune.name)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private int CalculateRuneSellPrice(RuneData rune)
    {
        // Base price by rarity
        int basePrice = GetBaseSellPriceByRarity(rune.rarity);

        // Level bonus (50% of upgrade costs)
        int levelBonus = 0;
        for (int i = 0; i < rune.currentLevel; i++)
        {
            levelBonus += Mathf.RoundToInt(rune.GetUpgradeCost(i) * 0.5f);
        }

        return basePrice + levelBonus;
    }

    private int GetBaseSellPriceByRarity(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return 50;
            case RuneRarity.Uncommon:
                return 150;
            case RuneRarity.Rare:
                return 400;
            case RuneRarity.Epic:
                return 800;
            case RuneRarity.Legendary:
                return 1500;
            default:
                return 50;
        }
    }

    // ========== INVENTORY STATS ==========

    public int GetRuneCount()
    {
        return ownedRunes.Count;
    }

    public int GetAvailableRuneSpace()
    {
        return maxRuneCapacity - ownedRunes.Count;
    }

    // ========== EASY SAVE 3 SYSTEM ==========

    /// <summary>
    /// Save all player data using Easy Save 3
    /// </summary>
    public void SaveGame()
    {
        try
        {
            // 1. Save SoulCoins
            ES3.Save("SoulCoins", soulCoins, saveFileName);

            // 2. Save Monster Collection (prepare first)
            SaveMonsterCollection();

            // 3. Save Rune Collection
            SaveRuneCollection();

            // 4. Save additional metadata
            ES3.Save("SaveVersion", "2.0", saveFileName);
            ES3.Save("LastSaveTime", System.DateTime.Now.ToBinary(), saveFileName);

            Debug.Log("💾 Game saved successfully with Easy Save 3!");

            // Optional: Show save confirmation to player
            ShowSaveConfirmation();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    /// <summary>
    /// Load all player data using Easy Save 3
    /// </summary>
    public void LoadGame()
    {
        try
        {
            // Check if save file exists
            if (!ES3.FileExists(saveFileName))
            {
                Debug.Log("No save file found - starting with default values");
                InitializeDefaultValues();
                return;
            }

            // Check for migration
            string saveVersion = ES3.Load("SaveVersion", saveFileName, "1.0");
            if (saveVersion == "1.0")
            {
                MigrateFromOldSaveFormat();
                return;
            }

            // 1. Load SoulCoins
            soulCoins = ES3.Load("SoulCoins", saveFileName, 6000);

            // 2. Load Monster Collection
            LoadMonsterCollection();

            // 3. Load Rune Collection  
            LoadRuneCollection();

            // 4. Load metadata
            long lastSaveTime = ES3.Load("LastSaveTime", saveFileName, System.DateTime.Now.ToBinary());

            Debug.Log($"✅ Game loaded successfully! Save version: {saveVersion}, Last saved: {System.DateTime.FromBinary(lastSaveTime)}");
            Debug.Log($"📊 Loaded: {soulCoins} Soul Coins, {collectedMonsters.Count} monsters, {ownedRunes.Count} runes");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            InitializeDefaultValues();
        }
    }

    /// <summary>
    /// Save the complete monster collection directly
    /// </summary>
    private void SaveMonsterCollection()
    {
        // Prepare all monsters for saving
        foreach (var monster in collectedMonsters)
        {
            monster.PrepareForSave();
        }

        // Direct save - Easy Save 3 handles the serialization
        ES3.Save("MonsterCollection", collectedMonsters, saveFileName);
        Debug.Log($"🐉 Saved {collectedMonsters.Count} monsters directly");
    }

    /// <summary>
    /// Load the complete monster collection directly
    /// </summary>
    private void LoadMonsterCollection()
    {
        // Direct load
        collectedMonsters = ES3.Load("MonsterCollection", saveFileName, new List<CollectedMonster>());

        // Restore references after loading
        foreach (var monster in collectedMonsters)
        {
            monster.RestoreAfterLoad();
        }

        Debug.Log($"🐉 Loaded {collectedMonsters.Count} monsters directly");
    }

    /// <summary>
    /// Save all owned runes with their upgrade levels
    /// </summary>
    private void SaveRuneCollection()
    {
        ES3.Save("OwnedRunes", ownedRunes, saveFileName);
        ES3.Save("MaxRuneCapacity", maxRuneCapacity, saveFileName);

        Debug.Log($"💎 Saved {ownedRunes.Count} runes to Easy Save 3");
    }

    /// <summary>
    /// Load all owned runes
    /// </summary>
    private void LoadRuneCollection()
    {
        ownedRunes = ES3.Load("OwnedRunes", saveFileName, new List<RuneData>());
        maxRuneCapacity = ES3.Load("MaxRuneCapacity", saveFileName, 200);

        Debug.Log($"💎 Loaded {ownedRunes.Count} runes from Easy Save 3");
    }

    /// <summary>
    /// Migration from old save format (if needed)
    /// </summary>
    private void MigrateFromOldSaveFormat()
    {
        Debug.Log("🔄 Migrating from old save format...");

        // Your migration logic here if you have old saves
        // For now, just initialize defaults and upgrade version
        InitializeDefaultValues();

        // Upgrade save version
        ES3.Save("SaveVersion", "2.0", saveFileName);
        SaveGame();

        Debug.Log("✅ Migration completed!");
    }

    /// <summary>
    /// Initialize default values for new players
    /// </summary>
    private void InitializeDefaultValues()
    {
        soulCoins = 6000;
        collectedMonsters.Clear();
        ownedRunes.Clear();

        Debug.Log("🆕 Initialized with default values for new player");
    }

    /// <summary>
    /// Delete save file (for testing or reset functionality)
    /// </summary>
    public void DeleteSaveFile()
    {
        if (ES3.FileExists(saveFileName))
        {
            ES3.DeleteFile(saveFileName);
            Debug.Log("🗑️ Save file deleted!");

            // Reload with default values
            InitializeDefaultValues();
        }
    }

    /// <summary>
    /// Check if save file exists
    /// </summary>
    public bool HasSaveFile()
    {
        return ES3.FileExists(saveFileName);
    }

    /// <summary>
    /// Get save file info
    /// </summary>
    public string GetSaveFileInfo()
    {
        if (!HasSaveFile()) return "No save file found";

        try
        {
            long lastSaveTime = ES3.Load("LastSaveTime", saveFileName, 0);
            string saveVersion = ES3.Load("SaveVersion", saveFileName, "Unknown");

            return $"Version: {saveVersion}, Last saved: {System.DateTime.FromBinary(lastSaveTime):yyyy-MM-dd HH:mm:ss}";
        }
        catch
        {
            return "Save file corrupted";
        }
    }

    /// <summary>
    /// Auto-save functionality (call this after important actions)
    /// </summary>
    public void AutoSave()
    {
        SaveGame();
    }

    /// <summary>
    /// Show save confirmation (optional UI feedback)
    /// </summary>
    private void ShowSaveConfirmation()
    {
        // You can implement UI feedback here
        Debug.Log("💾 Game Saved!");
    }

    // ========== LEGACY METHODS (OBSOLETE) ==========

    [System.Obsolete("Use SaveGame() instead - PlayerPrefs system has been replaced with Easy Save 3")]
    public void SaveToPlayerPrefs()
    {
        SaveGame();
    }

    [System.Obsolete("Use LoadGame() instead - PlayerPrefs system has been replaced with Easy Save 3")]
    public void LoadFromPlayerPrefs()
    {
        LoadGame();
    }
}

// ========== ENHANCED COLLECTED MONSTER WITH EXPERIENCE SYSTEM ==========

[System.Serializable]
public class CollectedMonster
{
    // ✅ SERIALIZABLE data (saved to disk)
    public string monsterDataName;
    public int level = 1;
    public int currentStarLevel;
    public long dateObtained;
    public string uniqueID;

    [Header("Experience System")]
    public int currentExperience = 0;
    public int experienceToNextLevel = 100;

    [Header("Rune System")]
    public string[] equippedRuneNames = new string[6]; // Names in plaats van references

    // ✅ NON-SERIALIZABLE runtime data (loaded from resources)
    [System.NonSerialized]
    public MonsterData monsterData;

    [System.NonSerialized]
    private RuneSlot[] _runeSlots;

    public RuneSlot[] runeSlots
    {
        get
        {
            if (_runeSlots == null) InitializeRuneSlots();
            return _runeSlots;
        }
    }

    public System.DateTime GetDateObtained()
    {
        return System.DateTime.FromBinary(dateObtained);
    }

    public CollectedMonster(MonsterData data)
    {
        monsterData = data;
        monsterDataName = data != null ? data.name : "";
        currentStarLevel = data != null ? data.defaultStarLevel : 1;
        level = 1;
        dateObtained = System.DateTime.Now.ToBinary();
        uniqueID = System.Guid.NewGuid().ToString();

        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        currentExperience = 0;
        experienceToNextLevel = CalculateExperienceForLevel(2);
        equippedRuneNames = new string[6];
        InitializeRuneSlots();
    }

    private void InitializeRuneSlots()
    {
        _runeSlots = new RuneSlot[6];
        for (int i = 0; i < 6; i++)
        {
            _runeSlots[i] = new RuneSlot
            {
                slotIndex = i,
                equippedRune = null
            };
        }
    }

    // ========== EXPERIENCE SYSTEM ==========

    /// <summary>
    /// Add experience to this monster
    /// </summary>
    public void AddExperience(int amount)
    {
        currentExperience += amount;
        Debug.Log($"{monsterData?.monsterName ?? "Monster"} gained {amount} EXP! ({currentExperience}/{experienceToNextLevel})");
    }

    /// <summary>
    /// Check if monster can level up
    /// </summary>
    public bool CanLevelUp()
    {
        return currentExperience >= experienceToNextLevel && level < GetMaxLevel();
    }

    /// <summary>
    /// Level up the monster
    /// </summary>
    public void LevelUp()
    {
        if (!CanLevelUp()) return;

        currentExperience -= experienceToNextLevel;
        level++;

        // Increase experience requirement for next level (exponential growth)
        experienceToNextLevel = CalculateExperienceForLevel(level + 1);

        Debug.Log($"🆙 {monsterData?.monsterName ?? "Monster"} leveled up to {level}!");
    }

    /// <summary>
    /// Calculate experience needed for a specific level
    /// </summary>
    private int CalculateExperienceForLevel(int targetLevel)
    {
        // Exponential growth formula: 100 * level^1.5
        return Mathf.RoundToInt(100 * Mathf.Pow(targetLevel, 1.5f));
    }

    /// <summary>
    /// Get maximum level for monsters
    /// </summary>
    public int GetMaxLevel()
    {
        return 60; // Max level
    }

    /// <summary>
    /// Get experience percentage for UI progress bars
    /// </summary>
    public float GetExperiencePercentage()
    {
        if (experienceToNextLevel <= 0) return 1f;
        return (float)currentExperience / experienceToNextLevel;
    }

    // ========== STAT CALCULATION ==========

    /// <summary>
    /// Get current effective stats with level, star, and rune bonuses
    /// </summary>
    public MonsterStats GetEffectiveStats()
    {
        if (monsterData == null) return new MonsterStats();

        MonsterStats stats = new MonsterStats(monsterData, level, currentStarLevel);
        ApplyRuneBonuses(ref stats);
        return stats;
    }

    private void ApplyRuneBonuses(ref MonsterStats stats)
    {
        // Apply individual rune stats
        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (runeSlots[i]?.equippedRune != null)
            {
                ApplyRuneStats(runeSlots[i].equippedRune, ref stats);
            }
        }

        // Apply set bonuses
        ApplySetBonuses(ref stats);
    }

    private void ApplyRuneStats(RuneData rune, ref MonsterStats stats)
    {
        // Implementation depends on your rune stat system
        // This is a placeholder - implement based on your RuneData structure
    }

    private void ApplySetBonuses(ref MonsterStats stats)
    {
        // Implementation depends on your rune set system
        // This is a placeholder - implement based on your rune set structure
    }

    // ========== RUNE MANAGEMENT ==========

    public bool CanEquipRune(RuneData rune, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= runeSlots.Length) return false;
        if (rune == null) return false;

        // Add your rune equip logic here
        return true;
    }

    public bool EquipRune(RuneData rune, int slotIndex)
    {
        if (!CanEquipRune(rune, slotIndex)) return false;

        runeSlots[slotIndex].equippedRune = rune;
        equippedRuneNames[slotIndex] = rune.name;
        return true;
    }

    public RuneData UnequipRune(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= runeSlots.Length) return null;

        var rune = runeSlots[slotIndex].equippedRune;
        runeSlots[slotIndex].equippedRune = null;
        equippedRuneNames[slotIndex] = "";
        return rune;
    }

    public int GetMaxStarLevel()
    {
        return 6;
    }

    public bool CanUpgradeStar()
    {
        return currentStarLevel < GetMaxStarLevel();
    }

    // ========== SAVE/LOAD HELPERS ==========

    /// <summary>
    /// Prepare for saving - update serializable fields
    /// </summary>
    public void PrepareForSave()
    {
        // Update serializable fields before saving
        if (monsterData != null)
        {
            monsterDataName = monsterData.name;
        }

        // Update equipped rune names
        for (int i = 0; i < equippedRuneNames.Length; i++)
        {
            if (i < runeSlots.Length && runeSlots[i]?.equippedRune != null)
            {
                equippedRuneNames[i] = runeSlots[i].equippedRune.name;
            }
            else
            {
                equippedRuneNames[i] = "";
            }
        }
    }

    /// <summary>
    /// Restore after loading - rebuild references
    /// </summary>
    public void RestoreAfterLoad()
    {
        // Restore MonsterData reference
        LoadMonsterData();

        // Initialize runtime data
        InitializeRuneSlots();

        // Restore equipped runes
        RestoreEquippedRunes();
    }

    /// <summary>
    /// Load MonsterData from Resources
    /// </summary>
    private void LoadMonsterData()
    {
        if (monsterData == null && !string.IsNullOrEmpty(monsterDataName))
        {
            monsterData = Resources.Load<MonsterData>($"Monsters/{monsterDataName}");
            if (monsterData == null)
            {
                Debug.LogWarning($"Could not load MonsterData: {monsterDataName}");
            }
        }
    }

    /// <summary>
    /// Restore equipped runes from names
    /// </summary>
    private void RestoreEquippedRunes()
    {
        if (PlayerInventory.Instance != null)
        {
            for (int i = 0; i < equippedRuneNames.Length && i < runeSlots.Length; i++)
            {
                if (!string.IsNullOrEmpty(equippedRuneNames[i]))
                {
                    var rune = PlayerInventory.Instance.ownedRunes.FirstOrDefault(r => r.name == equippedRuneNames[i]);
                    if (rune != null && runeSlots[i] != null)
                    {
                        runeSlots[i].equippedRune = rune;
                    }
                }
            }
        }
    }
}

// ========== RUNE SLOT SYSTEM ==========

[System.Serializable]
public class RuneSlot
{
    public int slotIndex;
    public RuneType requiredRuneType;
    public RuneData equippedRune;

    public bool IsEmpty => equippedRune == null;

    public bool CanEquip(RuneData rune)
    {
        if (rune == null) return false;

        // Add your specific equip rules here
        return true;
    }
}
