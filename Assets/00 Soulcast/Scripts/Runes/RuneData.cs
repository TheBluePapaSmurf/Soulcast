using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Rune", menuName = "Rune System/Rune Data")]
public class RuneData : ScriptableObject
{
    [Header("Basic Information")]
    public string runeName;
    [TextArea(2, 3)]
    public string description;
    public Sprite runeIcon;
    public RuneType runeType;
    public RuneSlotPosition runeSlotPosition;
    public RuneRarity rarity;

    [Header("Main Stat")]
    public RuneStat mainStat;

    [Header("Sub Stats")]
    public List<RuneStat> subStats = new List<RuneStat>();

    [Header("Set Bonus (Optional)")]
    public RuneSetData runeSet;

    [Header("Level & Upgrade")]
    [Range(0, 15)]
    public int currentLevel = 0;
    public int maxLevel = 15;
    public float customPowerUpChance = -1f;
    public int customPowerUpCost = -1;
    public List<int> upgradeCosts = new List<int>(); // Gold costs per level

    [Header("REBALANCED Upgrade Settings")]
    [Range(0.05f, 0.25f)]
    public float mainStatUpgradeRate = 0.08f; // 8% base increase per level (reduced from 10%)
    [Range(0.02f, 0.15f)]
    public float subStatUpgradeRate = 0.04f; // 4% increase per level for substats

    [Header("ENHANCED Upgrade Cost Settings")]
    public int baseCost = 1000;                    // Base upgrade cost
    public float costGrowthRate = 1.5f;            // How much cost increases per level
    public float raritycostMultiplier = 1.0f;     // Multiplier based on rarity

    [Header("Success Chance Settings")]
    [Range(0f, 1f)]
    public float baseSuccessChance = 0.85f;        // Base success chance
    [Range(0f, 0.1f)]
    public float chanceDecreasePerLevel = 0.05f;   // How much chance decreases per level

    // ✅ NEW: Exponential level scaling for "big dick damage"
    public float GetLevelMultiplier(int level)
    {
        if (level <= 0) return 1f;
        // Exponential growth: weak start, explosive end
        return 1f + (Mathf.Pow(level, 1.4f) * 0.12f);
    }

    // ✅ NEW: Base stat multipliers for rune generation
    public float GetRarityBaseMultiplier()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.0f;     // 100% base
            case RuneRarity.Uncommon: return 1.8f;   // 180% base  
            case RuneRarity.Rare: return 3.2f;      // 320% base
            case RuneRarity.Epic: return 5.8f;      // 580% base
            case RuneRarity.Legendary: return 10.0f; // 1000% base!
            default: return 1.0f;
        }
    }

    // ✅ UPDATED: More dramatic rarity upgrade multipliers
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
            case RuneRarity.Common: return 1.0f;        // 100% base cost
            case RuneRarity.Uncommon: return 2.2f;      // 220% base cost
            case RuneRarity.Rare: return 4.8f;         // 480% base cost  
            case RuneRarity.Epic: return 9.5f;         // 950% base cost
            case RuneRarity.Legendary: return 18.0f;   // 1800% base cost!
            default: return 1.0f;
        }
    }

    // ✅ UPDATED: New exponential upgrade calculation
    public float GetMainStatUpgradeAmount()
    {
        if (mainStat == null) return 0f;

        // Base upgrade: percentage of current value
        float baseUpgrade = mainStat.value * mainStatUpgradeRate;

        // Multiply by rarity upgrade bonus (legendary gets 3x boost!)
        float rarityMultiplier = GetRarityUpgradeMultiplier();

        // Level scaling: each level makes upgrades stronger (compound effect)
        float levelScaling = 1f + (currentLevel * 0.05f); // +5% compound per level

        return baseUpgrade * rarityMultiplier * levelScaling;
    }

    // ✅ NEW: Get substat upgrade amount
    public float GetSubStatUpgradeAmount(RuneStat subStat)
    {
        if (subStat == null) return 0f;

        // Substats upgrade less than main stat
        float baseUpgrade = subStat.value * subStatUpgradeRate;
        float rarityMultiplier = GetRarityUpgradeMultiplier();
        float levelScaling = 1f + (currentLevel * 0.03f); // Smaller scaling for substats

        return baseUpgrade * rarityMultiplier * levelScaling;
    }

    public int GetUpgradeCost(int level)
    {
        if (level < 0 || level >= maxLevel) return 0;

        // Custom costs if defined
        if (upgradeCosts.Count > level && upgradeCosts[level] > 0)
            return upgradeCosts[level];

        // Calculate dynamic cost
        float baseUpgradeCost = baseCost;
        float rarityCostMultiplier = GetRarityCostMultiplier();
        float levelCostMultiplier = GetLevelCostMultiplier(level);

        int totalCost = Mathf.RoundToInt(baseUpgradeCost * rarityCostMultiplier * levelCostMultiplier);

        return totalCost;
    }

    public float GetLevelCostMultiplier(int level)
    {
        if (level <= 0) return 1f;

        // Different growth rates per rarity - Legendary gets expensive FAST
        float growthRate = GetCostGrowthRate();
        return Mathf.Pow(growthRate, level);
    }

    private float GetCostGrowthRate()
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.3f;       // Slow growth (30% per level)
            case RuneRarity.Uncommon: return 1.4f;     // Medium growth (40% per level)
            case RuneRarity.Rare: return 1.55f;       // Fast growth (55% per level)
            case RuneRarity.Epic: return 1.7f;        // Very fast growth (70% per level)  
            case RuneRarity.Legendary: return 1.9f;   // Extreme growth (90% per level)!
            default: return 1.5f;
        }
    }

    // ✅ NEW: Get current upgrade cost
    public int GetCurrentUpgradeCost()
    {
        return GetUpgradeCost(currentLevel);
    }

    // ✅ UPDATED: Enhanced main stat upgrade
    public void UpgradeMainStat()
    {
        if (mainStat == null || currentLevel >= maxLevel) return;

        float upgradeAmount = GetMainStatUpgradeAmount();

        // Round to appropriate decimal places
        if (mainStat.isPercentage)
        {
            mainStat.value += Mathf.Round(upgradeAmount * 10f) / 10f; // 1 decimal place for %
        }
        else
        {
            mainStat.value += Mathf.Round(upgradeAmount); // Whole numbers for flat stats
        }

        Debug.Log($"🔧 Upgraded {runeName} main stat ({mainStat.GetStatDisplayName()}) by {upgradeAmount:F1} to {mainStat.value}");
    }

    // ✅ UPDATED: Enhanced substat upgrades
    public void UpgradeSubStats()
    {
        if (subStats == null || subStats.Count == 0 || currentLevel >= maxLevel) return;

        // Upgrade substats every 3rd level (3, 6, 9, 12, 15)
        if (currentLevel % 3 != 0) return;

        foreach (var subStat in subStats)
        {
            if (subStat != null)
            {
                float upgradeAmount = GetSubStatUpgradeAmount(subStat);

                if (subStat.isPercentage)
                {
                    subStat.value += Mathf.Round(upgradeAmount * 10f) / 10f;
                }
                else
                {
                    subStat.value += Mathf.Round(upgradeAmount);
                }

                Debug.Log($"🔧 Upgraded {runeName} substat ({subStat.GetStatDisplayName()}) by {upgradeAmount:F1} to {subStat.value}");
            }
        }
    }

    // ✅ NEW: Complete upgrade (main stat + substats + level)
    public bool UpgradeRune()
    {
        if (currentLevel >= maxLevel) return false;

        // Upgrade main stat
        UpgradeMainStat();

        // Upgrade substats (if applicable for this level)
        UpgradeSubStats();

        // Increase level
        currentLevel++;

        Debug.Log($"✨ {runeName} upgraded to level {currentLevel}!");
        return true;
    }

    // ✅ NEW: Preview stat value at any level
    public float GetStatValueAtLevel(RuneStat stat, int targetLevel)
    {
        if (stat == null || targetLevel < 0) return 0f;

        float currentValue = stat.value;
        bool isMainStat = stat == mainStat;

        // Simulate upgrading from current level to target level
        for (int level = currentLevel; level < targetLevel && level < maxLevel; level++)
        {
            float upgradeAmount;

            if (isMainStat)
            {
                // Main stat gets upgraded every level
                float baseUpgrade = currentValue * mainStatUpgradeRate;
                float rarityMultiplier = GetRarityUpgradeMultiplier();
                float levelScaling = 1f + (level * 0.05f);
                upgradeAmount = baseUpgrade * rarityMultiplier * levelScaling;
            }
            else
            {
                // Substat only upgrades every 3rd level
                if ((level + 1) % 3 != 0) continue;

                float baseUpgrade = currentValue * subStatUpgradeRate;
                float rarityMultiplier = GetRarityUpgradeMultiplier();
                float levelScaling = 1f + (level * 0.03f);
                upgradeAmount = baseUpgrade * rarityMultiplier * levelScaling;
            }

            currentValue += upgradeAmount;
        }

        return currentValue;
    }

    // ✅ NEW: Get main stat value at specific level (for previewing)
    public float GetMainStatValueAtLevel(int level)
    {
        return GetStatValueAtLevel(mainStat, level);
    }

    // ✅ NEW: Get total upgrade cost from current to target level
    public int GetTotalUpgradeCost(int targetLevel)
    {
        int totalCost = 0;

        for (int level = currentLevel; level < targetLevel && level < maxLevel; level++)
        {
            totalCost += GetUpgradeCost(level);
        }

        return totalCost;
    }

    // ✅ NEW: Get rune power rating (for comparison)
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

    // ✅ NEW: Get rune efficiency (power per cost invested)
    public float GetEfficiency()
    {
        float totalCostInvested = GetTotalUpgradeCost(currentLevel);
        if (totalCostInvested <= 0) return GetPowerRating();

        return GetPowerRating() / totalCostInvested;
    }

    // Get total stats including main and sub stats (unchanged)
    public List<RuneStat> GetAllStats()
    {
        List<RuneStat> allStats = new List<RuneStat>();

        if (mainStat != null)
            allStats.Add(mainStat);

        allStats.AddRange(subStats);

        return allStats;
    }

    // ✅ DEPRECATED: Keep for backwards compatibility
    public float GetScaledStatValue(RuneStat stat)
    {
        Debug.LogWarning("GetScaledStatValue is deprecated. Use GetStatValueAtLevel instead.");
        return GetStatValueAtLevel(stat, currentLevel);
    }

    // ✅ NEW: Context menu actions for testing
    [ContextMenu("Preview at Level 15")]
    private void PreviewAtMaxLevel()
    {
        if (Application.isEditor)
        {
            Debug.Log($"=== {runeName} at Level 15 ===");
            Debug.Log($"Main Stat: {mainStat?.GetStatDisplayName()} = {GetMainStatValueAtLevel(15):F1}");

            foreach (var subStat in subStats)
            {
                Debug.Log($"Sub Stat: {subStat.GetStatDisplayName()} = {GetStatValueAtLevel(subStat, 15):F1}");
            }

            Debug.Log($"Total Upgrade Cost: {GetTotalUpgradeCost(15):N0} gold");
            Debug.Log($"Power Rating: {GetPowerRating():F0}");
        }
    }

    [ContextMenu("Upgrade Once")]
    private void TestUpgrade()
    {
        if (Application.isEditor)
        {
            UpgradeRune();
        }
    }

    // ✅ NEW: Validation for editor
    private void OnValidate()
    {
        if (Application.isEditor)
        {
            // Ensure level is within bounds
            currentLevel = Mathf.Clamp(currentLevel, 0, maxLevel);

            // Auto-generate upgrade costs if empty
            if (upgradeCosts.Count == 0)
            {
                upgradeCosts.Clear();
                for (int i = 0; i < maxLevel; i++)
                {
                    upgradeCosts.Add(GetUpgradeCost(i));
                }
            }
        }
    }

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

    // ✅ NEW: Get current upgrade success chance
    public float GetCurrentUpgradeSuccessChance()
    {
        return GetUpgradeSuccessChance(currentLevel);
    }

    // ✅ NEW: Preview method for UI
    public string GetUpgradePreviewText()
    {
        int cost = GetCurrentUpgradeCost();
        float successChance = GetCurrentUpgradeSuccessChance();

        return $"Cost: {cost:N0} | Success: {successChance:P0}";
    }

    // ✅ NEW: Risk assessment for players
    public UpgradeRisk GetUpgradeRisk()
    {
        float successChance = GetCurrentUpgradeSuccessChance();

        if (successChance >= 0.8f) return UpgradeRisk.Safe;
        if (successChance >= 0.6f) return UpgradeRisk.Moderate;
        if (successChance >= 0.4f) return UpgradeRisk.Risky;
        if (successChance >= 0.2f) return UpgradeRisk.Dangerous;
        return UpgradeRisk.Extreme;
    }

    public enum UpgradeRisk
    {
        Safe,       // 80%+ success
        Moderate,   // 60-79% success  
        Risky,      // 40-59% success
        Dangerous,  // 20-39% success
        Extreme     // <20% success
    }
}

// ✅ ENHANCED: RuneStat with new display methods
[System.Serializable]
public class RuneStat
{
    public RuneStatType statType;
    public float value;
    public bool isPercentage; // True for %, false for flat numbers

    public string GetDisplayText()
    {
        string prefix = value > 0 ? "+" : "";
        string suffix = isPercentage ? "%" : "";
        return $"{prefix}{value:F1}{suffix} {GetStatDisplayName()}";
    }

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

    // ✅ NEW: Get stat color for UI
    public Color GetStatColor()
    {
        switch (statType)
        {
            case RuneStatType.HP: return new Color(0.2f, 0.8f, 0.2f); // Green
            case RuneStatType.ATK: return new Color(0.8f, 0.2f, 0.2f); // Red
            case RuneStatType.DEF: return new Color(0.4f, 0.4f, 0.8f); // Blue
            case RuneStatType.SPD: return new Color(0.8f, 0.8f, 0.2f); // Yellow
            case RuneStatType.CriticalRate: return new Color(0.8f, 0.4f, 0.2f); // Orange
            case RuneStatType.CriticalDamage: return new Color(0.8f, 0.2f, 0.8f); // Purple
            case RuneStatType.Accuracy: return new Color(0.2f, 0.8f, 0.8f); // Cyan
            case RuneStatType.Resistance: return new Color(0.6f, 0.6f, 0.6f); // Gray
            default: return Color.white;
        }
    }
}

// Enums (unchanged)
public enum RuneType
{
    Blade,
    Fatal,
    Rage,
    Energy,
    Guard,
    Swift
}

public enum RuneSlotPosition
{
    Slot1,    // Slot 1
    Slot2,    // Slot 2  
    Slot3,    // Slot 3
    Slot4,    // Slot 4
    Slot5,    // Slot 5
    Slot6     // Slot 6
}

public enum RuneStatType
{
    HP,
    ATK,
    DEF,
    SPD,
    CriticalRate,
    CriticalDamage,
    Accuracy,
    Resistance
}

public enum RuneRarity
{
    Common,    // 1-2 sub stats
    Uncommon,  // 2-3 sub stats
    Rare,      // 3-4 sub stats
    Epic,      // 4 sub stats + higher values
    Legendary  // 4 sub stats + max values
}
