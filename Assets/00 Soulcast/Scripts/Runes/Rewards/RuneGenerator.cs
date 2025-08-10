using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class RuneGenerator
{
    // ✅ NEW: Smart rune generation with proper drop logic
    public static List<RuneData> GenerateRunesFromRewards(List<RuneReward> guaranteedRunes, List<RuneReward> randomRunes)
    {
        var generatedRunes = new List<RuneData>();

        // Generate guaranteed runes (always drop)
        foreach (var runeReward in guaranteedRunes)
        {
            var rune = GenerateRune(runeReward);
            if (rune != null)
                generatedRunes.Add(rune);
        }

        // Generate random runes with smart drop logic
        var randomRune = GenerateRandomRune(randomRunes);
        if (randomRune != null)
            generatedRunes.Add(randomRune);

        return generatedRunes;
    }

    // ✅ NEW: Smart random rune generation
    public static RuneData GenerateRandomRune(List<RuneReward> randomRunes)
    {
        if (randomRunes == null || randomRunes.Count == 0)
            return null;

        // Check if all drop chances are 0 - if so, use equal distribution
        bool allZero = randomRunes.All(r => r.dropChance <= 0f);

        if (allZero)
        {
            return GenerateEqualDistributionRune(randomRunes);
        }
        else
        {
            return GenerateWeightedRandomRune(randomRunes);
        }
    }

    // ✅ NEW: Equal distribution when all drop chances are 0
    private static RuneData GenerateEqualDistributionRune(List<RuneReward> randomRunes)
    {
        Debug.Log("🎲 All drop chances are 0 - using equal distribution");

        // Select random rune with equal probability
        int randomIndex = Random.Range(0, randomRunes.Count);
        var selectedRune = randomRunes[randomIndex];

        Debug.Log($"📦 Selected rune: {selectedRune.runeSet} {selectedRune.runeSlot} ({selectedRune.rarity})");

        return GenerateRune(selectedRune);
    }

    // ✅ NEW: Weighted random selection based on drop chances
    private static RuneData GenerateWeightedRandomRune(List<RuneReward> randomRunes)
    {
        // First, check if we get ANY rune at all
        float noDropChance = CalculateNoDropChance(randomRunes);
        float rollForAnyDrop = Random.Range(0f, 1f);

        Debug.Log($"🎲 Rolling for any drop: {rollForAnyDrop:F3} vs no-drop chance: {noDropChance:F3}");

        if (rollForAnyDrop < noDropChance)
        {
            Debug.Log("❌ No rune dropped this time");
            return null; // No rune drops
        }

        // If we get here, we're guaranteed to get a rune
        // Now select which one based on weighted probabilities
        return SelectWeightedRune(randomRunes);
    }

    // Calculate the chance that NO rune drops
    private static float CalculateNoDropChance(List<RuneReward> randomRunes)
    {
        float noDropChance = 1f;

        // For independent probability: P(none) = (1-p1) × (1-p2) × (1-p3) × ...
        foreach (var rune in randomRunes)
        {
            noDropChance *= (1f - rune.dropChance);
        }

        return noDropChance;
    }

    // Select rune using weighted random based on drop chances
    private static RuneData SelectWeightedRune(List<RuneReward> randomRunes)
    {
        // Create weighted list based on drop chances
        var weightedRunes = new List<WeightedRuneEntry>();

        foreach (var rune in randomRunes)
        {
            if (rune.dropChance > 0f)
            {
                weightedRunes.Add(new WeightedRuneEntry
                {
                    runeReward = rune,
                    weight = rune.dropChance
                });
            }
        }

        if (weightedRunes.Count == 0)
            return null;

        // Calculate total weight
        float totalWeight = weightedRunes.Sum(w => w.weight);

        // Roll for weighted selection
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var weightedRune in weightedRunes)
        {
            currentWeight += weightedRune.weight;
            if (randomValue <= currentWeight)
            {
                Debug.Log($"📦 Weighted selection: {weightedRune.runeReward.runeSet} {weightedRune.runeReward.runeSlot} " +
                         $"({weightedRune.runeReward.rarity}) - Weight: {weightedRune.weight:F2}/{totalWeight:F2}");

                return GenerateRune(weightedRune.runeReward);
            }
        }

        // Fallback (shouldn't happen)
        return GenerateRune(weightedRunes.Last().runeReward);
    }

    // Helper class for weighted selection
    private class WeightedRuneEntry
    {
        public RuneReward runeReward;
        public float weight;
    }

    // Main generation method (unchanged)
    public static RuneData GenerateRune(RuneReward runeReward)
    {
        if (!runeReward.IsValid())
        {
            Debug.LogError("Invalid RuneReward configuration!");
            return null;
        }

        // Create new RuneData instance
        var rune = ScriptableObject.CreateInstance<RuneData>();

        // Set basic properties
        rune.runeName = GenerateRuneName(runeReward.runeSet, runeReward.runeSlot, runeReward.rarity);
        rune.description = GenerateRuneDescription(runeReward);
        rune.runeType = runeReward.runeSet;
        rune.runeSlotPosition = runeReward.runeSlot;
        rune.rarity = runeReward.rarity;
        rune.currentLevel = 0; // Always start at level 0

        // Set rune set data
        rune.runeSet = GetRuneSetData(runeReward.runeSet);

        // Generate main stat
        rune.mainStat = runeReward.mainStatRange.CreateRandomStat();

        // Generate substats
        rune.subStats = GenerateSubStats(runeReward);

        return rune;
    }

    // Rest of the methods remain the same...
    // (GenerateSubStats, GetAvailableSubStats, etc.)

    private static List<RuneStat> GenerateSubStats(RuneReward runeReward)
    {
        var subStats = new List<RuneStat>();

        int subStatCount = GetSubStatCountForRarity(runeReward.rarity, runeReward.minSubStats, runeReward.maxSubStats);
        var availableSubStats = GetAvailableSubStats(runeReward);
        var selectedSubStats = SelectRandomSubStats(availableSubStats, subStatCount);

        foreach (var subStatRange in selectedSubStats)
        {
            subStats.Add(subStatRange.CreateRandomStat());
        }

        return subStats;
    }

    private static List<RuneStatRange> GetAvailableSubStats(RuneReward runeReward)
    {
        var available = new List<RuneStatRange>();

        foreach (var subStat in runeReward.allowedSubStats)
        {
            if (subStat.statType != runeReward.mainStatRange.statType)
            {
                available.Add(subStat);
            }
            else if (subStat.isPercentage != runeReward.mainStatRange.isPercentage)
            {
                available.Add(subStat);
            }
        }

        return available;
    }

    private static List<RuneStatRange> SelectRandomSubStats(List<RuneStatRange> available, int count)
    {
        if (available.Count <= count)
            return available;

        var selected = new List<RuneStatRange>();
        var tempList = new List<RuneStatRange>(available);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            selected.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }

        return selected;
    }

    private static int GetSubStatCountForRarity(RuneRarity rarity, int min, int max)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return Mathf.Clamp(Random.Range(1, 3), min, max);
            case RuneRarity.Uncommon:
                return Mathf.Clamp(Random.Range(2, 4), min, max);
            case RuneRarity.Rare:
                return Mathf.Clamp(Random.Range(3, 5), min, max);
            case RuneRarity.Epic:
            case RuneRarity.Legendary:
                return Mathf.Clamp(4, min, max);
            default:
                return Mathf.Clamp(2, min, max);
        }
    }

    private static string GenerateRuneName(RuneType runeSet, RuneSlotPosition slot, RuneRarity rarity)
    {
        string rarityPrefix = GetRarityPrefix(rarity);
        string slotSuffix = GetSlotSuffix(slot);
        return $"{rarityPrefix} {runeSet} Rune {slotSuffix}";
    }

    private static string GetRarityPrefix(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return "Common";
            case RuneRarity.Uncommon: return "Uncommon";
            case RuneRarity.Rare: return "Rare";
            case RuneRarity.Epic: return "Epic";
            case RuneRarity.Legendary: return "Legendary";
            default: return "Common";
        }
    }

    private static string GetSlotSuffix(RuneSlotPosition slot)
    {
        switch (slot)
        {
            case RuneSlotPosition.Slot1: return "(I)";
            case RuneSlotPosition.Slot2: return "(II)";
            case RuneSlotPosition.Slot3: return "(III)";
            case RuneSlotPosition.Slot4: return "(IV)";
            case RuneSlotPosition.Slot5: return "(V)";
            case RuneSlotPosition.Slot6: return "(VI)";
            default: return "(I)";
        }
    }

    private static string GenerateRuneDescription(RuneReward runeReward)
    {
        string setName = runeReward.runeSet.ToString();
        string mainStatName = runeReward.mainStatRange.statType.ToString();
        string rarityName = runeReward.rarity.ToString();

        return $"A {rarityName.ToLower()} {setName} rune focused on {mainStatName}. Generated from combat rewards.";
    }

    private static RuneSetData GetRuneSetData(RuneType runeType)
    {
        return Resources.Load<RuneSetData>($"Runes/Sets/{runeType}Set");
    }
}
