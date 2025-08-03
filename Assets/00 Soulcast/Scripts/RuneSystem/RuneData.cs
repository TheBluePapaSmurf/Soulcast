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

    [Header("Upgrade Settings")]
    [Range(0.05f, 0.25f)]
    public float mainStatUpgradeRate = 0.1f; // 10% increase per level
    [Range(0.02f, 0.15f)]
    public float subStatUpgradeRate = 0.05f; // 5% increase per level


    // Get total stats including main and sub stats
    public List<RuneStat> GetAllStats()
    {
        List<RuneStat> allStats = new List<RuneStat>();

        if (mainStat != null)
            allStats.Add(mainStat);

        allStats.AddRange(subStats);

        return allStats;
    }

    // Get upgrade cost for specific level
    public int GetUpgradeCost(int level)
    {
        if (level < 0 || level >= upgradeCosts.Count)
            return 1000; // Default cost

        return upgradeCosts[level];
    }

    // Get stat value with level scaling
    public float GetScaledStatValue(RuneStat stat)
    {
        if (stat == null) return 0f;

        float baseValue = stat.value;
        float levelMultiplier = 1f + (currentLevel * 0.1f); // 10% per level

        return baseValue * levelMultiplier;
    }

    public float GetMainStatUpgradeAmount()
    {
        if (mainStat == null) return 0f;

        float baseUpgrade = mainStat.value * mainStatUpgradeRate;

        // Apply rarity multiplier
        float rarityMultiplier = GetRarityUpgradeMultiplier();

        return baseUpgrade * rarityMultiplier;
    }

    public float GetRarityUpgradeMultiplier()
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return 1.0f;      // 100% of base upgrade
            case RuneRarity.Uncommon:
                return 1.2f;      // 120% of base upgrade  
            case RuneRarity.Rare:
                return 1.4f;      // 140% of base upgrade
            case RuneRarity.Epic:
                return 1.6f;      // 160% of base upgrade
            case RuneRarity.Legendary:
                return 2.0f;      // 200% of base upgrade
            default:
                return 1.0f;
        }
    }

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

        Debug.Log($"Upgraded {runeName} main stat ({mainStat.GetStatDisplayName()}) by {upgradeAmount:F1} to {mainStat.value}");
    }

    // Optional: Upgrade sub stats (smaller increases)
    public void UpgradeSubStats()
    {
        if (subStats == null || subStats.Count == 0 || currentLevel >= maxLevel) return;

        // Only upgrade sub stats every few levels to avoid overpowering
        if (currentLevel % 3 != 0) return; // Upgrade every 3rd level

        foreach (var subStat in subStats)
        {
            if (subStat != null)
            {
                float upgradeAmount = subStat.value * subStatUpgradeRate * GetRarityUpgradeMultiplier();

                if (subStat.isPercentage)
                {
                    subStat.value += Mathf.Round(upgradeAmount * 10f) / 10f;
                }
                else
                {
                    subStat.value += Mathf.Round(upgradeAmount);
                }
            }
        }
    }

    // Get the total main stat value at a specific level (for preview)
    public float GetMainStatValueAtLevel(int level)
    {
        if (mainStat == null) return 0f;

        float upgradeAmount = GetMainStatUpgradeAmount();
        float totalUpgradeValue = upgradeAmount * level;

        return mainStat.value + totalUpgradeValue;
    }
}

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
}

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
    Slot2,  // Slot 2  
    Slot3,    // Slot 3
    Slot4,   // Slot 4
    Slot5,  // Slot 5
    Slot6    // Slot 6
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
