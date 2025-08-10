using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RuneRewardPresets", menuName = "Soulcast/Rune Reward Presets")]
public class RuneRewardPresets : ScriptableObject
{
    [Header("Preset Configurations")]
    public List<RuneRewardPreset> presets = new List<RuneRewardPreset>();

    public RuneReward GetPreset(string presetName)
    {
        var preset = presets.Find(p => p.presetName == presetName);
        return preset?.runeReward;
    }

    public List<string> GetPresetNames()
    {
        var names = new List<string>();
        foreach (var preset in presets)
        {
            names.Add(preset.presetName);
        }
        return names;
    }
}

[System.Serializable]
public class RuneRewardPreset
{
    public string presetName;           // "Early Game ATK Rune", "Boss DEF Rune", etc.
    public RuneReward runeReward;
}

// ✅ COMPLETE: RuneRewardTemplates with all required methods
public static class RuneRewardTemplates
{
    // ✅ REQUIRED: For Combat Creator Window - Early game preset
    public static RuneReward CreateEarlyGameATKRune()
    {
        return new RuneReward
        {
            runeSet = RuneType.Blade,
            runeSlot = RuneSlotPosition.Slot1,
            rarity = RuneRarity.Common,
            dropChance = 0.8f,
            mainStatRange = new RuneStatRange
            {
                statType = RuneStatType.ATK,
                isPercentage = false,
                minValue = 8f,    // ✅ REBALANCED: Weak start
                maxValue = 15f    // ✅ REBALANCED: Weak start
            },
            allowedSubStats = new List<RuneStatRange>
            {
                new RuneStatRange { statType = RuneStatType.HP, isPercentage = false, minValue = 15f, maxValue = 60f },
                new RuneStatRange { statType = RuneStatType.DEF, isPercentage = false, minValue = 3f, maxValue = 9f },
                new RuneStatRange { statType = RuneStatType.SPD, isPercentage = false, minValue = 2f, maxValue = 5f },
                new RuneStatRange { statType = RuneStatType.ATK, isPercentage = true, minValue = 1f, maxValue = 2f }
            },
            minSubStats = 1,
            maxSubStats = 2
        };
    }

    // ✅ REQUIRED: For Combat Creator Window - Boss rare preset  
    public static RuneReward CreateBossRareRune()
    {
        return new RuneReward
        {
            runeSet = RuneType.Fatal,
            runeSlot = RuneSlotPosition.Slot4,
            rarity = RuneRarity.Rare,    // ✅ RARE for mid-game progression
            dropChance = 0.15f,          // ✅ 15% drop chance
            mainStatRange = new RuneStatRange
            {
                statType = RuneStatType.CriticalDamage,
                isPercentage = true,
                minValue = 15f,   // ✅ REBALANCED: Rare level (was 40f)
                maxValue = 25f    // ✅ REBALANCED: Rare level (was 65f)
            },
            allowedSubStats = new List<RuneStatRange>
            {
                new RuneStatRange { statType = RuneStatType.ATK, isPercentage = true, minValue = 4f, maxValue = 10f },
                new RuneStatRange { statType = RuneStatType.CriticalRate, isPercentage = true, minValue = 3f, maxValue = 8f },
                new RuneStatRange { statType = RuneStatType.SPD, isPercentage = false, minValue = 10f, maxValue = 18f },
                new RuneStatRange { statType = RuneStatType.HP, isPercentage = true, minValue = 4f, maxValue = 10f }
            },
            minSubStats = 3,  // ✅ 3-4 substats for Rare
            maxSubStats = 4
        };
    }

    // ✅ NEW: Legendary version for true end-game
    public static RuneReward CreateBossLegendaryRune()
    {
        return new RuneReward
        {
            runeSet = RuneType.Fatal,
            runeSlot = RuneSlotPosition.Slot4,
            rarity = RuneRarity.Legendary,  // ✅ LEGENDARY for big dick damage
            dropChance = 0.05f,             // ✅ 5% drop chance (very rare)
            mainStatRange = new RuneStatRange
            {
                statType = RuneStatType.CriticalDamage,
                isPercentage = true,
                minValue = 40f,   // ✅ BIG DICK DAMAGE level
                maxValue = 65f    // ✅ BIG DICK DAMAGE level
            },
            allowedSubStats = new List<RuneStatRange>
            {
                new RuneStatRange { statType = RuneStatType.ATK, isPercentage = true, minValue = 14f, maxValue = 35f },
                new RuneStatRange { statType = RuneStatType.CriticalRate, isPercentage = true, minValue = 10f, maxValue = 25f },
                new RuneStatRange { statType = RuneStatType.SPD, isPercentage = false, minValue = 30f, maxValue = 50f },
                new RuneStatRange { statType = RuneStatType.HP, isPercentage = true, minValue = 14f, maxValue = 35f }
            },
            minSubStats = 4,  // ✅ Always 4 substats for Legendary
            maxSubStats = 4
        };
    }

    // ✅ NEW: Epic version for late game progression
    public static RuneReward CreateBossEpicRune()
    {
        return new RuneReward
        {
            runeSet = RuneType.Fatal,
            runeSlot = RuneSlotPosition.Slot4,
            rarity = RuneRarity.Epic,       // ✅ EPIC for late game
            dropChance = 0.1f,              // ✅ 10% drop chance
            mainStatRange = new RuneStatRange
            {
                statType = RuneStatType.CriticalDamage,
                isPercentage = true,
                minValue = 25f,   // ✅ Between Rare and Legendary
                maxValue = 40f    // ✅ Between Rare and Legendary
            },
            allowedSubStats = new List<RuneStatRange>
            {
                new RuneStatRange { statType = RuneStatType.ATK, isPercentage = true, minValue = 7f, maxValue = 18f },
                new RuneStatRange { statType = RuneStatType.CriticalRate, isPercentage = true, minValue = 6f, maxValue = 15f },
                new RuneStatRange { statType = RuneStatType.SPD, isPercentage = false, minValue = 18f, maxValue = 30f },
                new RuneStatRange { statType = RuneStatType.HP, isPercentage = true, minValue = 7f, maxValue = 18f }
            },
            minSubStats = 4,  // ✅ Always 4 substats for Epic
            maxSubStats = 4
        };
    }

    // ✅ NEW: Different stat focus runes
    public static RuneReward CreateSpeedRune(RuneRarity rarity = RuneRarity.Uncommon)
    {
        var baseMultiplier = GetRarityMultiplier(rarity);

        return new RuneReward
        {
            runeSet = RuneType.Swift,
            runeSlot = RuneSlotPosition.Slot6, // SPD main stat
            rarity = rarity,
            dropChance = GetRarityDropChance(rarity),
            mainStatRange = new RuneStatRange
            {
                statType = RuneStatType.SPD,
                isPercentage = false,
                minValue = 2f * baseMultiplier,
                maxValue = 5f * baseMultiplier
            },
            allowedSubStats = CreateGenericSubStats(rarity),
            minSubStats = GetMinSubStats(rarity),
            maxSubStats = GetMaxSubStats(rarity)
        };
    }

    public static RuneReward CreateSpeedFarmingRune()
    {
        return new RuneReward
        {
            runeSet = RuneType.Swift,
            runeSlot = RuneSlotPosition.Slot6,  // 🎲 FARMING SLOT!
            rarity = RuneRarity.Epic,
            dropChance = 0.1f, // 10% to drop, random main stat
            useAutoBalance = true
        };
    }

    public static RuneReward CreateHPRune(RuneRarity rarity = RuneRarity.Uncommon)
    {
        var baseMultiplier = GetRarityMultiplier(rarity);

        return new RuneReward
        {
            runeSet = RuneType.Energy,
            runeSlot = RuneSlotPosition.Slot5, // HP main stat
            rarity = rarity,
            dropChance = GetRarityDropChance(rarity),
            mainStatRange = new RuneStatRange
            {
                statType = RuneStatType.HP,
                isPercentage = Random.value > 0.5f, // 50/50 flat vs %
                minValue = Random.value > 0.5f ? (50f * baseMultiplier) : (2f * baseMultiplier),
                maxValue = Random.value > 0.5f ? (100f * baseMultiplier) : (4f * baseMultiplier)
            },
            allowedSubStats = CreateGenericSubStats(rarity),
            minSubStats = GetMinSubStats(rarity),
            maxSubStats = GetMaxSubStats(rarity)
        };
    }

    // Add this method to RuneRewardTemplates class in RuneRewardPresets.cs

    public static RuneReward CreateLateGameEpicRune()
    {
        return new RuneReward
        {
            runeSet = RuneType.Rage,
            runeSlot = RuneSlotPosition.Slot2, // ATK% main stat
            rarity = RuneRarity.Epic,
            dropChance = 0.1f,
            useAutoBalance = true,
            mainStatRange = new RuneStatRange
            {
                statType = RuneStatType.ATK,
                isPercentage = true,
                minValue = 25f,   // Epic level
                maxValue = 40f    // Epic level
            },
            allowedSubStats = new List<RuneStatRange>
        {
            new RuneStatRange { statType = RuneStatType.HP, isPercentage = true, minValue = 7f, maxValue = 18f },
            new RuneStatRange { statType = RuneStatType.CriticalDamage, isPercentage = true, minValue = 6f, maxValue = 15f },
            new RuneStatRange { statType = RuneStatType.SPD, isPercentage = false, minValue = 18f, maxValue = 30f },
            new RuneStatRange { statType = RuneStatType.CriticalRate, isPercentage = true, minValue = 6f, maxValue = 15f }
        },
            minSubStats = 3,  // Epic gets 3 substats
            maxSubStats = 3
        };
    }


    // ✅ Helper methods for consistent rarity scaling
    private static float GetRarityMultiplier(RuneRarity rarity)
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

    private static float GetRarityDropChance(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 0.8f;      // 80%
            case RuneRarity.Uncommon: return 0.4f;    // 40%
            case RuneRarity.Rare: return 0.15f;      // 15%
            case RuneRarity.Epic: return 0.1f;       // 10%
            case RuneRarity.Legendary: return 0.05f; // 5%
            default: return 1.0f;
        }
    }

    private static int GetMinSubStats(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1;
            case RuneRarity.Uncommon: return 2;
            case RuneRarity.Rare: return 3;
            case RuneRarity.Epic: return 4;
            case RuneRarity.Legendary: return 4;
            default: return 1;
        }
    }

    private static int GetMaxSubStats(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 2;
            case RuneRarity.Uncommon: return 3;
            case RuneRarity.Rare: return 4;
            case RuneRarity.Epic: return 4;
            case RuneRarity.Legendary: return 4;
            default: return 2;
        }
    }

    private static List<RuneStatRange> CreateGenericSubStats(RuneRarity rarity)
    {
        var multiplier = GetRarityMultiplier(rarity) * 0.5f; // Substats are weaker

        return new List<RuneStatRange>
        {
            new RuneStatRange { statType = RuneStatType.ATK, isPercentage = false, minValue = 3f * multiplier, maxValue = 9f * multiplier },
            new RuneStatRange { statType = RuneStatType.ATK, isPercentage = true, minValue = 1f * multiplier, maxValue = 3f * multiplier },
            new RuneStatRange { statType = RuneStatType.HP, isPercentage = false, minValue = 15f * multiplier, maxValue = 60f * multiplier },
            new RuneStatRange { statType = RuneStatType.HP, isPercentage = true, minValue = 1f * multiplier, maxValue = 3f * multiplier },
            new RuneStatRange { statType = RuneStatType.DEF, isPercentage = false, minValue = 2f * multiplier, maxValue = 6f * multiplier },
            new RuneStatRange { statType = RuneStatType.DEF, isPercentage = true, minValue = 1f * multiplier, maxValue = 3f * multiplier },
            new RuneStatRange { statType = RuneStatType.SPD, isPercentage = false, minValue = 1f * multiplier, maxValue = 4f * multiplier },
            new RuneStatRange { statType = RuneStatType.CriticalRate, isPercentage = true, minValue = 1f * multiplier, maxValue = 4f * multiplier },
            new RuneStatRange { statType = RuneStatType.CriticalDamage, isPercentage = true, minValue = 2f * multiplier, maxValue = 6f * multiplier },
            new RuneStatRange { statType = RuneStatType.Accuracy, isPercentage = true, minValue = 1f * multiplier, maxValue = 4f * multiplier },
            new RuneStatRange { statType = RuneStatType.Resistance, isPercentage = true, minValue = 1f * multiplier, maxValue = 4f * multiplier }
        };
    }
}
