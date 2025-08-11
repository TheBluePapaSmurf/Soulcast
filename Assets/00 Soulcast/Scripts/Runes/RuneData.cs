// ✅ COMPLETE FIXED: /Assets/00 Soulcast/Scripts/Runes/RuneData.cs
// Pure procedural data class with all missing methods

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure data class for procedural runes - NO ScriptableObject dependency
/// </summary>
[System.Serializable]
public class RuneData
{
    [Header("Basic Information")]
    public string runeName;
    [TextArea(2, 3)]
    public string description;
    public Sprite runeSprite; // Will be loaded from Resources
    public RuneType runeType;
    public RuneSlotPosition runeSlotPosition;
    public RuneRarity rarity;

    [Header("Identification")]
    public string uniqueID; // ✅ NEW: Unique ID for tracking
    public string name; // ✅ NEW: For reference compatibility

    [Header("Main Stat")]
    public RuneStat mainStat;

    [Header("Sub Stats")]
    public List<RuneStat> subStats = new List<RuneStat>();

    [Header("Set Bonus (Optional)")]
    public RuneSetData runeSet; // Still references RuneSetData ScriptableObject

    [Header("Level & Upgrade")]
    [Range(0, 15)]
    public int currentLevel = 0;
    public int maxLevel = 15;
    public float customPowerUpChance = -1f;
    public int customPowerUpCost = -1;
    public List<int> upgradeCosts = new List<int>();

    [Header("Generation Settings")]
    public DateTime creationTime; // ✅ NEW: When rune was generated
    public bool isProceduralGenerated = true; // ✅ NEW: Mark as procedural

    // ✅ FIXED: UpgradeRisk enum INSIDE RuneData class (not in RuneStat!)
    public enum UpgradeRisk
    {
        Safe,       // 80%+ success
        Moderate,   // 60-79% success  
        Risky,      // 40-59% success
        Dangerous,  // 20-39% success
        Extreme     // <20% success
    }

    // ========== CONSTRUCTORS ==========

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public RuneData()
    {
        uniqueID = System.Guid.NewGuid().ToString();
        name = uniqueID; // Fallback name
        creationTime = DateTime.Now;
        isProceduralGenerated = true;

        // 🔍 CRITICAL DEBUG: Track waar empty runes worden gemaakt
        Debug.LogWarning($"🔍 EMPTY RuneData() constructor called!");
        Debug.LogWarning($"🔍 StackTrace:\n{System.Environment.StackTrace}");

        mainStat = new RuneStat();
        subStats = new List<RuneStat>();
        upgradeCosts = new List<int>();
    }


    /// <summary>
    /// Constructor for procedural generation
    /// </summary>
    public RuneData(RuneType type, RuneSlotPosition position, RuneRarity rarity, string generatedName = null)
    {
        uniqueID = System.Guid.NewGuid().ToString();
        runeName = generatedName ?? $"Procedural {type} Rune";
        name = runeName;
        description = $"A mystical {rarity} {type} rune discovered during your adventures.";

        runeType = type;
        runeSlotPosition = position;
        this.rarity = rarity;

        currentLevel = 0;
        maxLevel = 15;
        creationTime = DateTime.Now;
        isProceduralGenerated = true;

        mainStat = new RuneStat();
        subStats = new List<RuneStat>();
        upgradeCosts = new List<int>();

        // Load sprite from Resources based on type
        LoadRuneSprite();

        // Link to RuneSetData if exists
        LoadRuneSetData();
    }

    // ========== RESOURCE LOADING ==========

    /// <summary>
    /// Load sprite from Resources
    /// </summary>
    private void LoadRuneSprite()
    {
        try
        {
            string spritePath = $"UI/Runes/{runeType}Icon";
            runeSprite = Resources.Load<Sprite>(spritePath);

            if (runeSprite == null)
            {
                // Fallback sprite paths
                string[] fallbackPaths = {
                    $"RuneIcon{runeType}",
                    "UI/DefaultRuneIcon",
                    "DefaultRune"
                };

                foreach (string path in fallbackPaths)
                {
                    runeSprite = Resources.Load<Sprite>(path);
                    if (runeSprite != null) break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Could not load sprite for {runeType}: {e.Message}");
        }
    }

    /// <summary>
    /// Load RuneSetData from Resources
    /// </summary>
    private void LoadRuneSetData()
    {
        try
        {
            string setPath = $"Runes/Sets/{runeType}Set";
            runeSet = Resources.Load<RuneSetData>(setPath);

            if (runeSet == null)
            {
                Debug.LogWarning($"⚠️ Could not find RuneSetData for {runeType} at path: {setPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Could not load RuneSetData for {runeType}: {e.Message}");
        }
    }

    // ========== LEVEL AND UPGRADE METHODS ==========

    /// <summary>
    /// Get main stat value at a specific level (for preview)
    /// </summary>
    public float GetMainStatValueAtLevel(int targetLevel)
    {
        if (mainStat == null || targetLevel < 0) return 0f;

        float currentValue = mainStat.value;

        // Simulate upgrading from current level to target level
        for (int level = currentLevel; level < targetLevel && level < maxLevel; level++)
        {
            // Main stat gets upgraded every level
            float baseUpgrade = currentValue * 0.08f; // 8% base increase
            float rarityMultiplier = GetRarityUpgradeMultiplier();
            float levelScaling = 1f + (level * 0.05f); // +5% compound per level

            float upgradeAmount = baseUpgrade * rarityMultiplier * levelScaling;
            currentValue += upgradeAmount;
        }

        return currentValue;
    }

    /// <summary>
    /// Get current upgrade success chance
    /// </summary>
    public float GetCurrentUpgradeSuccessChance()
    {
        return GetUpgradeSuccessChance(currentLevel);
    }

    /// <summary>
    /// Get upgrade success chance for specific level
    /// </summary>
    public float GetUpgradeSuccessChance(int level)
    {
        // Base success chance by rarity
        float baseChance = GetRarityBaseSuccessChance();

        // Level penalty (gets harder each level)
        float levelPenalty = level * GetLevelSuccessPenalty();

        // Minimum success chance (never goes below this)
        float minChance = GetMinimumSuccessChance();

        return Mathf.Max(minChance, baseChance - levelPenalty);
    }

    /// <summary>
    /// Get current upgrade cost
    /// </summary>
    public int GetCurrentUpgradeCost()
    {
        return GetUpgradeCost(currentLevel);
    }

    /// <summary>
    /// Get upgrade risk assessment
    /// </summary>
    public UpgradeRisk GetUpgradeRisk()
    {
        float successChance = GetCurrentUpgradeSuccessChance();

        if (successChance >= 0.8f) return UpgradeRisk.Safe;
        if (successChance >= 0.6f) return UpgradeRisk.Moderate;
        if (successChance >= 0.4f) return UpgradeRisk.Risky;
        if (successChance >= 0.2f) return UpgradeRisk.Dangerous;
        return UpgradeRisk.Extreme;
    }

    /// <summary>
    /// Get rune efficiency (power per cost invested)
    /// </summary>
    public float GetEfficiency()
    {
        float totalCostInvested = GetTotalUpgradeCost(currentLevel);
        if (totalCostInvested <= 0) return GetPowerRating();

        return GetPowerRating() / totalCostInvested;
    }

    /// <summary>
    /// Get total upgrade cost from level 0 to target level
    /// </summary>
    public int GetTotalUpgradeCost(int targetLevel)
    {
        int totalCost = 0;

        for (int level = 0; level < targetLevel && level < maxLevel; level++)
        {
            totalCost += GetUpgradeCost(level);
        }

        return totalCost;
    }

    /// <summary>
    /// Get upgrade cost for specific level
    /// </summary>
    public int GetUpgradeCost(int level)
    {
        if (level < 0 || level >= maxLevel) return 0;

        // Custom costs if defined
        if (upgradeCosts.Count > level && upgradeCosts[level] > 0)
            return upgradeCosts[level];

        // Calculate dynamic cost
        float baseCost = 1000f;
        float rarityCostMultiplier = GetRarityCostMultiplier();
        float levelCostMultiplier = Mathf.Pow(GetCostGrowthRate(), level);

        return Mathf.RoundToInt(baseCost * rarityCostMultiplier * levelCostMultiplier);
    }

    /// <summary>
    /// Upgrade this rune
    /// </summary>
    public bool UpgradeRune()
    {
        if (currentLevel >= maxLevel) return false;

        // Upgrade main stat
        if (mainStat != null)
        {
            float upgradeAmount = mainStat.value * 0.08f * GetRarityUpgradeMultiplier();
            mainStat.value += upgradeAmount;
        }

        // Upgrade substats every 3rd level
        if ((currentLevel + 1) % 3 == 0)
        {
            foreach (var subStat in subStats)
            {
                if (subStat != null)
                {
                    float upgradeAmount = subStat.value * 0.04f * GetRarityUpgradeMultiplier();
                    subStat.value += upgradeAmount;
                }
            }
        }

        currentLevel++;
        Debug.Log($"✨ {runeName} upgraded to level {currentLevel}!");
        return true;
    }

    // ========== RARITY AND MULTIPLIER METHODS ==========

    public float GetLevelMultiplier(int level)
    {
        if (level <= 0) return 1f;
        return 1f + (Mathf.Pow(level, 1.4f) * 0.12f);
    }

    public float GetRarityBaseMultiplier()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.0f;
            case RuneRarity.Uncommon: return 1.8f;
            case RuneRarity.Rare: return 3.2f;
            case RuneRarity.Epic: return 5.8f;
            case RuneRarity.Legendary: return 10.0f;
            default: return 1.0f;
        }
    }

    public float GetRarityUpgradeMultiplier()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.0f;     // Normal upgrades
            case RuneRarity.Uncommon: return 1.3f;   // 30% better upgrades
            case RuneRarity.Rare: return 1.7f;      // 70% better upgrades  
            case RuneRarity.Epic: return 2.2f;      // 120% better upgrades
            case RuneRarity.Legendary: return 3.0f;  // 200% better upgrades!
            default: return 1.0f;
        }
    }

    public float GetRarityCostMultiplier()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.0f;
            case RuneRarity.Uncommon: return 2.2f;
            case RuneRarity.Rare: return 4.8f;
            case RuneRarity.Epic: return 9.5f;
            case RuneRarity.Legendary: return 18.0f;
            default: return 1.0f;
        }
    }

    private float GetCostGrowthRate()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.3f;
            case RuneRarity.Uncommon: return 1.4f;
            case RuneRarity.Rare: return 1.55f;
            case RuneRarity.Epic: return 1.7f;
            case RuneRarity.Legendary: return 1.9f;
            default: return 1.5f;
        }
    }

    // ========== SUCCESS CHANCE HELPER METHODS ==========

    private float GetRarityBaseSuccessChance()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 0.95f;      // 95% base success (almost guaranteed)
            case RuneRarity.Uncommon: return 0.85f;    // 85% base success (reliable)
            case RuneRarity.Rare: return 0.75f;       // 75% base success (decent)
            case RuneRarity.Epic: return 0.65f;       // 65% base success (risky)
            case RuneRarity.Legendary: return 0.50f;  // 50% base success (coin flip!)
            default: return 0.85f;
        }
    }

    private float GetLevelSuccessPenalty()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 0.02f;      // -2% per level (gentle)
            case RuneRarity.Uncommon: return 0.03f;    // -3% per level  
            case RuneRarity.Rare: return 0.04f;       // -4% per level
            case RuneRarity.Epic: return 0.05f;       // -5% per level
            case RuneRarity.Legendary: return 0.06f;  // -6% per level (harsh!)
            default: return 0.05f;
        }
    }

    private float GetMinimumSuccessChance()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 0.80f;      // Never below 80%
            case RuneRarity.Uncommon: return 0.65f;    // Never below 65%
            case RuneRarity.Rare: return 0.50f;       // Never below 50%
            case RuneRarity.Epic: return 0.35f;       // Never below 35%
            case RuneRarity.Legendary: return 0.20f;  // Never below 20% (still scary!)
            default: return 0.50f;
        }
    }

    // ========== POWER AND STAT METHODS ==========

    /// <summary>
    /// Calculate power rating of this rune
    /// </summary>
    public float GetPowerRating()
    {
        float powerRating = 0f;

        // Main stat contributes most to power
        if (mainStat != null)
        {
            float mainValue = mainStat.isPercentage ? mainStat.value * 10f : mainStat.value;
            powerRating += mainValue * 2f;
        }

        // Substats contribute less
        foreach (var subStat in subStats)
        {
            if (subStat != null)
            {
                float subValue = subStat.isPercentage ? subStat.value * 10f : subStat.value;
                powerRating += subValue;
            }
        }

        // Level multiplier
        powerRating *= (1f + currentLevel * 0.2f);

        // Rarity multiplier
        powerRating *= GetRarityBaseMultiplier();

        return powerRating;
    }

    /// <summary>
    /// Get all stats (main + sub stats)
    /// </summary>
    public List<RuneStat> GetAllStats()
    {
        List<RuneStat> allStats = new List<RuneStat>();

        if (mainStat != null)
            allStats.Add(mainStat);

        allStats.AddRange(subStats);

        return allStats;
    }

    // ========== DISPLAY METHODS ==========

    /// <summary>
    /// Get display name with level
    /// </summary>
    public string GetDisplayName()
    {
        if (currentLevel > 0)
        {
            return $"{runeName} (+{currentLevel})";
        }
        return runeName;
    }

    /// <summary>
    /// Get rarity color for UI
    /// </summary>
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return new Color(0.8f, 0.8f, 0.8f); // Light Gray
            case RuneRarity.Uncommon: return new Color(0.2f, 0.8f, 0.2f); // Green  
            case RuneRarity.Rare: return new Color(0.2f, 0.4f, 1.0f); // Blue
            case RuneRarity.Epic: return new Color(0.8f, 0.2f, 0.8f); // Purple
            case RuneRarity.Legendary: return new Color(1.0f, 0.6f, 0.0f); // Orange
            default: return Color.white;
        }
    }
}

// ========== SUPPORTING CLASSES & ENUMS ==========

/// <summary>
/// Individual rune stat (main or sub stat)
/// </summary>
[System.Serializable]
public class RuneStat
{
    public RuneStatType statType;
    public float value;
    public bool isPercentage;

    /// <summary>
    /// Get formatted display text for UI
    /// </summary>
    public string GetDisplayText()
    {
        string prefix = value > 0 ? "+" : "";
        string suffix = isPercentage ? "%" : "";
        return $"{prefix}{value:F1}{suffix} {GetStatDisplayName()}";
    }

    /// <summary>
    /// Get human-readable stat name
    /// </summary>
    public string GetStatDisplayName()
    {
        switch (statType)
        {
            case RuneStatType.HP: return "HP";
            case RuneStatType.ATK: return "ATK";
            case RuneStatType.DEF: return "DEF";
            case RuneStatType.SPD: return "SPD";
            case RuneStatType.CriticalRate: return "CRIT Rate";
            case RuneStatType.CriticalDamage: return "CRIT DMG";
            case RuneStatType.Accuracy: return "Accuracy";
            case RuneStatType.Resistance: return "Resistance";
            default: return statType.ToString();
        }
    }

    /// <summary>
    /// Get color for this stat type
    /// </summary>
    public Color GetStatColor()
    {
        switch (statType)
        {
            case RuneStatType.HP: return new Color(0.2f, 0.8f, 0.2f);
            case RuneStatType.ATK: return new Color(0.8f, 0.2f, 0.2f);
            case RuneStatType.DEF: return new Color(0.4f, 0.4f, 0.8f);
            case RuneStatType.SPD: return new Color(0.8f, 0.8f, 0.2f);
            case RuneStatType.CriticalRate: return new Color(0.8f, 0.4f, 0.2f);
            case RuneStatType.CriticalDamage: return new Color(0.8f, 0.2f, 0.8f);
            case RuneStatType.Accuracy: return new Color(0.2f, 0.8f, 0.8f);
            case RuneStatType.Resistance: return new Color(0.6f, 0.6f, 0.6f);
            default: return Color.white;
        }
    }
}

// ========== ENUMS ==========

public enum RuneType
{
    Blade, Fatal, Rage, Energy, Guard, Swift
}

public enum RuneSlotPosition
{
    Slot1, Slot2, Slot3, Slot4, Slot5, Slot6
}

public enum RuneStatType
{
    HP, ATK, DEF, SPD, CriticalRate, CriticalDamage, Accuracy, Resistance
}

public enum RuneRarity
{
    Common, Uncommon, Rare, Epic, Legendary
}
