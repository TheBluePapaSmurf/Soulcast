using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RuneReward
{
    [Header("Rune Configuration")]
    public RuneType runeSet;                    // Blade, Fatal, Rage, etc.
    public RuneSlotPosition runeSlot;           // Slot1-6 (determines main stat)
    public RuneRarity rarity;                   // Common to Legendary

    [Header("Drop Settings")]
    [Range(0f, 1f)]
    public float dropChance = 1f;               // Chance to drop this rune

    [Header("Stat Ranges")]
    public RuneStatRange mainStatRange;         // Range for main stat
    public List<RuneStatRange> allowedSubStats; // Possible substats with ranges

    [Header("Sub Stat Configuration")]
    [Range(1, 4)]
    public int minSubStats = 1;                 // Min number of substats
    [Range(1, 4)]
    public int maxSubStats = 4;                 // Max number of substats

    // ✅ NEW: Auto-generate balanced stat ranges
    [Header("Auto-Balance")]
    public bool useAutoBalance = true;          // Use automatic balanced ranges

    // Validate configuration
    public bool IsValid()
    {
        return mainStatRange != null &&
               allowedSubStats != null &&
               allowedSubStats.Count > 0 &&
               minSubStats <= maxSubStats;
    }

    // ✅ NEW: Auto-setup balanced ranges for this rarity
    [ContextMenu("Setup Auto-Balanced Ranges")]
    public void SetupAutoBalancedRanges()
    {
        if (useAutoBalance)
        {
            mainStatRange = RuneBalanceRanges.CreateMainStatRange(rarity, runeSlot);
            allowedSubStats = RuneBalanceRanges.CreateSubStatRanges(rarity, mainStatRange.statType, mainStatRange.isPercentage);

            // Set substat count based on rarity
            var substatCounts = RuneBalanceRanges.GetSubStatCountRange(rarity);
            minSubStats = substatCounts.x;
            maxSubStats = substatCounts.y;

            Debug.Log($"🎯 Auto-balanced {rarity} rune ranges setup for {runeSet} {runeSlot}");
        }
    }

    // Generate RuneData from this configuration
    public RuneData GenerateRune()
    {
        if (useAutoBalance)
            SetupAutoBalancedRanges();

        return RuneGenerator.GenerateRune(this);
    }
}

[System.Serializable]
public class RuneStatRange
{
    public RuneStatType statType;
    public bool isPercentage;
    public float minValue;
    public float maxValue;

    public float GetRandomValue()
    {
        return Random.Range(minValue, maxValue);
    }

    public RuneStat CreateRandomStat()
    {
        return new RuneStat
        {
            statType = statType,
            value = GetRandomValue(),
            isPercentage = isPercentage
        };
    }
}

// ✅ NEW: Balanced stat ranges system integrated into RuneReward
public static class RuneBalanceRanges
{
    // ✅ Main stat ranges based on slot position
    public static RuneStatRange CreateMainStatRange(RuneRarity rarity, RuneSlotPosition slot)
    {
        var mainStatInfo = GetMainStatInfoForSlot(slot);
        var range = GetMainStatRange(rarity, mainStatInfo.statType, mainStatInfo.isPercentage);

        return new RuneStatRange
        {
            statType = mainStatInfo.statType,
            isPercentage = mainStatInfo.isPercentage,
            minValue = range.x,
            maxValue = range.y
        };
    }

    // ✅ NEW: Struct to hold main stat info
    public struct MainStatInfo
    {
        public RuneStatType statType;
        public bool isPercentage;
    }

    private static MainStatInfo GetMainStatInfoForSlot(RuneSlotPosition slot)
    {
        switch (slot)
        {
            case RuneSlotPosition.Slot1:
                return new MainStatInfo { statType = RuneStatType.ATK, isPercentage = false };     // ATK Flat (FIXED)

            case RuneSlotPosition.Slot2:
                return GetSlot2MainStatInfo(); // 🎲⚡ SPD + other stats (SPD EXCLUSIVE SLOT!)

            case RuneSlotPosition.Slot3:
                return new MainStatInfo { statType = RuneStatType.DEF, isPercentage = false };     // DEF Flat (FIXED)

            case RuneSlotPosition.Slot5:
                return new MainStatInfo { statType = RuneStatType.HP, isPercentage = false };      // HP Flat (FIXED)

            case RuneSlotPosition.Slot4:
            case RuneSlotPosition.Slot6:
                return GetRandomMainStatInfoExcludingSPD(); // 🎲 RANDOM but NO SPD!

            default:
                return new MainStatInfo { statType = RuneStatType.ATK, isPercentage = false };
        }
    }

    private static MainStatInfo GetRandomMainStatInfo()
    {
        // Weighted random selection for main stats on farming slots
        var possibleStats = new List<(RuneStatType stat, bool isPercentage, float weight)>
    {
        // Percentage stats (more valuable, lower weight)
        (RuneStatType.ATK, true, 15f),           // ATK% - High demand
        (RuneStatType.HP, true, 15f),            // HP% - High demand  
        (RuneStatType.DEF, true, 12f),           // DEF% - Medium demand
        (RuneStatType.CriticalDamage, true, 8f), // CRIT DMG% - Rare but valuable
        (RuneStatType.CriticalRate, true, 6f),   // CRIT Rate% - Very rare
        (RuneStatType.Accuracy, true, 4f),       // Accuracy% - Niche
        (RuneStatType.Resistance, true, 4f),     // Resistance% - Niche
        
        // Flat stats (less valuable, higher weight)
        (RuneStatType.ATK, false, 20f),          // ATK Flat - Common
        (RuneStatType.HP, false, 20f),           // HP Flat - Common
        (RuneStatType.DEF, false, 18f),          // DEF Flat - Common
        (RuneStatType.SPD, false, 12f),          // SPD Flat - Medium value
    };

        // Calculate total weight
        float totalWeight = 0f;
        foreach (var stat in possibleStats)
        {
            totalWeight += stat.weight;
        }

        // Random selection
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var stat in possibleStats)
        {
            currentWeight += stat.weight;
            if (randomValue <= currentWeight)
            {
                return new MainStatInfo
                {
                    statType = stat.stat,
                    isPercentage = stat.isPercentage
                };
            }
        }

        // Fallback (shouldn't happen)
        return new MainStatInfo { statType = RuneStatType.ATK, isPercentage = true };
    }

    private static MainStatInfo GetSlot2MainStatInfo()
    {
        // Weighted random selection for Slot 2 (includes SPD!)
        var possibleStats = new List<(RuneStatType stat, bool isPercentage, float weight)>
    {
        // ⚡ SPD EXCLUSIVE - Only available on Slot 2!
        (RuneStatType.SPD, false, 12f),          // SPD Flat - EXCLUSIVE TO SLOT 2!
        
        // Other percentage stats
        (RuneStatType.ATK, true, 15f),           // ATK% - High demand
        (RuneStatType.HP, true, 15f),            // HP% - High demand  
        (RuneStatType.DEF, true, 12f),           // DEF% - Medium demand
        (RuneStatType.CriticalDamage, true, 8f), // CRIT DMG% - Rare but valuable
        (RuneStatType.CriticalRate, true, 6f),   // CRIT Rate% - Very rare
        (RuneStatType.Accuracy, true, 4f),       // Accuracy% - Niche
        (RuneStatType.Resistance, true, 4f),     // Resistance% - Niche
        
        // Other flat stats
        (RuneStatType.ATK, false, 20f),          // ATK Flat - Common
        (RuneStatType.HP, false, 20f),           // HP Flat - Common
        (RuneStatType.DEF, false, 18f),          // DEF Flat - Common
    };

        return SelectRandomStat(possibleStats);
    }

    // ✅ NEW: Slot 4 & 6 main stat generation (NO SPD allowed!)
    private static MainStatInfo GetRandomMainStatInfoExcludingSPD()
    {
        // Weighted random selection for Slot 4 & 6 (SPD EXCLUDED!)
        var possibleStats = new List<(RuneStatType stat, bool isPercentage, float weight)>
    {
        // Percentage stats (more valuable, lower weight)
        (RuneStatType.ATK, true, 18f),           // ATK% - High demand (increased weight since no SPD)
        (RuneStatType.HP, true, 18f),            // HP% - High demand (increased weight)
        (RuneStatType.DEF, true, 15f),           // DEF% - Medium demand (increased weight)
        (RuneStatType.CriticalDamage, true, 10f), // CRIT DMG% - Rare but valuable
        (RuneStatType.CriticalRate, true, 8f),   // CRIT Rate% - Very rare
        (RuneStatType.Accuracy, true, 5f),       // Accuracy% - Niche
        (RuneStatType.Resistance, true, 5f),     // Resistance% - Niche
        
        // Flat stats (less valuable, higher weight)
        (RuneStatType.ATK, false, 25f),          // ATK Flat - Common
        (RuneStatType.HP, false, 25f),           // HP Flat - Common
        (RuneStatType.DEF, false, 22f),          // DEF Flat - Common
        
        // ❌ NO SPD HERE! SPD main stat only on Slot 2!
    };

        return SelectRandomStat(possibleStats);
    }

    // ✅ HELPER: Random stat selection method
    private static MainStatInfo SelectRandomStat(List<(RuneStatType stat, bool isPercentage, float weight)> possibleStats)
    {
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var stat in possibleStats)
        {
            totalWeight += stat.weight;
        }

        // Random selection
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var stat in possibleStats)
        {
            currentWeight += stat.weight;
            if (randomValue <= currentWeight)
            {
                return new MainStatInfo
                {
                    statType = stat.stat,
                    isPercentage = stat.isPercentage
                };
            }
        }

        // Fallback (shouldn't happen)
        return new MainStatInfo { statType = RuneStatType.ATK, isPercentage = true };
    }

    // ✅ NEW: Helper method to check if stat is available on slot
    public static bool IsMainStatAvailableOnSlot(RuneStatType statType, bool isPercentage, RuneSlotPosition slot)
    {
        switch (slot)
        {
            case RuneSlotPosition.Slot1:
                return statType == RuneStatType.ATK && !isPercentage; // Only ATK flat

            case RuneSlotPosition.Slot2:
                return true; // All stats available (including SPD!)

            case RuneSlotPosition.Slot3:
                return statType == RuneStatType.DEF && !isPercentage; // Only DEF flat

            case RuneSlotPosition.Slot5:
                return statType == RuneStatType.HP && !isPercentage; // Only HP flat

            case RuneSlotPosition.Slot4:
            case RuneSlotPosition.Slot6:
                return statType != RuneStatType.SPD; // All stats EXCEPT SPD

            default:
                return false;
        }
    }

    // ✅ REBALANCED: Main stat ranges (weak → big dick damage)
    public static Vector2 GetMainStatRange(RuneRarity rarity, RuneStatType statType, bool isPercentage)
    {
        float multiplier = GetRarityBaseMultiplier(rarity);

        if (isPercentage)
        {
            // Percentage main stats - REBALANCED for dramatic differences
            switch (statType)
            {
                case RuneStatType.ATK:
                case RuneStatType.HP:
                case RuneStatType.DEF:
                    return new Vector2(2f * multiplier, 4f * multiplier); // 2-4% base → 20-40% legendary
                case RuneStatType.CriticalDamage:
                    return new Vector2(4f * multiplier, 8f * multiplier); // 4-8% base → 40-80% legendary
                default:
                    return new Vector2(2f * multiplier, 4f * multiplier);
            }
        }
        else
        {
            // Flat stats - REBALANCED for dramatic scaling
            switch (statType)
            {
                case RuneStatType.HP:
                    return new Vector2(50f * multiplier, 100f * multiplier); // 50-100 → 500-1000
                case RuneStatType.ATK:
                    return new Vector2(8f * multiplier, 15f * multiplier);   // 8-15 → 80-150
                case RuneStatType.DEF:
                    return new Vector2(3f * multiplier, 8f * multiplier);    // 3-8 → 30-80
                case RuneStatType.SPD:
                    return new Vector2(2f * multiplier, 5f * multiplier);    // 2-5 → 20-50
                default:
                    return new Vector2(5f * multiplier, 10f * multiplier);
            }
        }
    }

    // ✅ Generate all possible substat ranges for a rarity (excluding main stat conflicts)
    public static List<RuneStatRange> CreateSubStatRanges(RuneRarity rarity, RuneStatType mainStatType, bool mainIsPercentage)
    {
        var subStatRanges = new List<RuneStatRange>();
        var allStatTypes = System.Enum.GetValues(typeof(RuneStatType));

        foreach (RuneStatType statType in allStatTypes)
        {
            // Add flat version if main stat isn't flat of same type
            if (!(statType == mainStatType && !mainIsPercentage))
            {
                var flatRange = GetSubStatRange(rarity, statType, false);
                subStatRanges.Add(new RuneStatRange
                {
                    statType = statType,
                    isPercentage = false,
                    minValue = flatRange.x,
                    maxValue = flatRange.y
                });
            }

            // Add percentage version if main stat isn't percentage of same type
            if (!(statType == mainStatType && mainIsPercentage))
            {
                var percentRange = GetSubStatRange(rarity, statType, true);
                subStatRanges.Add(new RuneStatRange
                {
                    statType = statType,
                    isPercentage = true,
                    minValue = percentRange.x,
                    maxValue = percentRange.y
                });
            }
        }

        return subStatRanges;
    }

    // ✅ Substat ranges (lower than main stats)
    public static Vector2 GetSubStatRange(RuneRarity rarity, RuneStatType statType, bool isPercentage)
    {
        // Substats are 30-60% of main stat values
        float substatMultiplier = Random.Range(0.3f, 0.6f);
        var mainRange = GetMainStatRange(rarity, statType, isPercentage);

        return new Vector2(
            mainRange.x * substatMultiplier,
            mainRange.y * substatMultiplier
        );
    }

    // ✅ DRAMATIC: Rarity multipliers for "big dick damage" scaling
    private static float GetRarityBaseMultiplier(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.0f;      // 100% - weak
            case RuneRarity.Uncommon: return 1.8f;    // 180% - decent  
            case RuneRarity.Rare: return 3.2f;       // 320% - good
            case RuneRarity.Epic: return 5.8f;       // 580% - great
            case RuneRarity.Legendary: return 10.0f;  // 1000% - BIG DICK DAMAGE!
            default: return 1.0f;
        }
    }

    // ✅ Substat count ranges by rarity
    public static Vector2Int GetSubStatCountRange(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return new Vector2Int(1, 2);    // 1-2 substats
            case RuneRarity.Uncommon: return new Vector2Int(2, 3);  // 2-3 substats
            case RuneRarity.Rare: return new Vector2Int(3, 4);     // 3-4 substats
            case RuneRarity.Epic: return new Vector2Int(4, 4);     // Always 4 substats
            case RuneRarity.Legendary: return new Vector2Int(4, 4); // Always 4 substats
            default: return new Vector2Int(1, 2);
        }
    }

    // ✅ Helper: Get stat description for UI
    public static string GetStatRangeDescription(RuneRarity rarity, RuneStatType statType, bool isPercentage)
    {
        var range = GetMainStatRange(rarity, statType, isPercentage);
        string suffix = isPercentage ? "%" : "";
        return $"{range.x:F0}{suffix} - {range.y:F0}{suffix}";
    }

    // ✅ Helper: Get rarity power comparison
    public static float GetRarityPowerComparison(RuneRarity fromRarity, RuneRarity toRarity)
    {
        float fromMultiplier = GetRarityBaseMultiplier(fromRarity);
        float toMultiplier = GetRarityBaseMultiplier(toRarity);
        return toMultiplier / fromMultiplier;
    }

    // ✅ Debug: Print all ranges for testing
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void PrintAllRanges()
    {
        Debug.Log("=== RUNE BALANCE RANGES ===");

        foreach (RuneRarity rarity in System.Enum.GetValues(typeof(RuneRarity)))
        {
            Debug.Log($"\n🎯 {rarity} Runes (Multiplier: {GetRarityBaseMultiplier(rarity):F1}x):");

            // Main stats examples
            Debug.Log($"  ATK Flat: {GetStatRangeDescription(rarity, RuneStatType.ATK, false)}");
            Debug.Log($"  ATK %: {GetStatRangeDescription(rarity, RuneStatType.ATK, true)}");
            Debug.Log($"  HP Flat: {GetStatRangeDescription(rarity, RuneStatType.HP, false)}");
            Debug.Log($"  CRIT DMG %: {GetStatRangeDescription(rarity, RuneStatType.CriticalDamage, true)}");
        }

        // Power comparisons
        Debug.Log($"\n💪 Power Comparisons:");
        Debug.Log($"  Common → Legendary: {GetRarityPowerComparison(RuneRarity.Common, RuneRarity.Legendary):F1}x stronger");
        Debug.Log($"  Rare → Legendary: {GetRarityPowerComparison(RuneRarity.Rare, RuneRarity.Legendary):F1}x stronger");
    }
}