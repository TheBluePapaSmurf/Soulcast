using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[CreateAssetMenu(fileName = "CombatTemplate", menuName = "Soulcast/Combat Template")]
public class CombatTemplate : ScriptableObject
{
    [Header("Combat Info")]
    public string combatName;
    [TextArea(2, 4)]
    public string combatDescription;
    public Sprite battleIcon;

    [Header("Requirements")]
    public int requiredPlayerLevel = 1;
    public int recommendedTeamSize = 3;
    public List<MonsterRole> recommendedRoles = new List<MonsterRole>(); // Suggested team composition

    [Header("Victory Rewards")]
    public CombatReward rewards; // ✅ FIXED: Was CombatRewards, now CombatReward

    [Header("Combat Waves")]
    public List<WaveConfiguration> waves = new List<WaveConfiguration>();

    [Header("Battle Settings")]
    public float timeLimit = 300f; // 5 minutes
    public BattleDifficulty difficulty = BattleDifficulty.Normal;
    public List<BattleModifier> modifiers = new List<BattleModifier>();

    [Header("Star Rating Requirements")]
    public StarRequirements starRequirements;

    public int TotalWaves => waves.Count;
    public int TotalEnemies
    {
        get
        {
            int total = 0;
            foreach (var wave in waves)
            {
                total += wave.TotalEnemies;
            }
            return total;
        }
    }

    // Get recommended team composition as string
    public string GetRecommendedComposition()
    {
        if (recommendedRoles.Count == 0) return "Any composition";

        var roleGroups = recommendedRoles.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        var compositionParts = roleGroups.Select(kvp =>
            kvp.Value == 1 ? kvp.Key.ToString() : $"{kvp.Value}x {kvp.Key}");

        return string.Join(", ", compositionParts);
    }
}

[System.Serializable]
public class WaveConfiguration
{
    [Header("Wave Info")]
    public string waveName;
    public float waveDelay = 2f;

    [Header("Enemy Spawns")]
    public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();

    [Header("Wave Conditions")]
    public WaveCompletionType completionType = WaveCompletionType.DefeatAllEnemies;
    public float maxWaveTime = 60f;

    public int TotalEnemies
    {
        get
        {
            int total = 0;
            foreach (var spawn in enemySpawns)
            {
                total += spawn.spawnCount;
            }
            return total;
        }
    }
}

[System.Serializable]
public class EnemySpawn
{
    [Header("Monster Data")]
    public MonsterData monsterData;

    [Header("Level & Stars (uses your system)")]
    [Range(1, 60)]
    public int monsterLevel = 1;
    [Range(1, 5)]
    public int starLevel = 1; // Your star system: 1=Common, 5=Legendary

    [Header("Spawn Settings")]
    public int spawnCount = 1;
    public float spawnDelay = 0f;
    public Vector3 spawnPosition = Vector3.zero;
    public SpawnPattern spawnPattern = SpawnPattern.Sequential;

    [Header("AI Behavior Overrides")]
    public bool useCustomBehavior = false;
    [Range(0f, 1f)]
    public float aggressivenessOverride = 0.5f;
    [Range(0f, 1f)]
    public float intelligenceOverride = 0.5f;

    // Get display info
    public string GetDisplayInfo()
    {
        string rarity = MonsterData.GetRarityName(starLevel);
        return $"Lv.{monsterLevel} {rarity} {monsterData?.monsterName ?? "Unknown"}";
    }

    // Get effective stats for this spawn
    public MonsterStats GetEffectiveStats()
    {
        if (monsterData == null) return new MonsterStats();
        return monsterData.GetRoleAdjustedStats(monsterLevel, starLevel);
    }
}

[System.Serializable]
public class CombatReward
{
    [Header("Soul Coins Reward")]
    public int baseSoulCoins = 1000;
    public float soulCoinMultiplier = 1.0f;
    public bool useRegionBasedCoins = true;

    [Header("Rune Rewards")]
    public bool guaranteedRuneDrop = true;
    public int maxRuneDrops = 1;
    public bool useRegionBasedDropRates = true;

    [Header("Manual Override")]
    public List<RuneReward> customGuaranteedRunes = new List<RuneReward>();
    public List<RuneReward> customRandomRunes = new List<RuneReward>();

    // ✅ Calculate actual rewards based on region/chapter/level
    public CombatResult GenerateRewards(int region, int chapter, int level)
    {
        var result = new CombatResult();

        // Calculate Soul Coins
        if (useRegionBasedCoins)
        {
            result.soulCoinsEarned = CalculateSoulCoinsReward(region, chapter, level);
        }
        else
        {
            result.soulCoinsEarned = Mathf.RoundToInt(baseSoulCoins * soulCoinMultiplier);
        }

        // Generate Rune Drops
        if (guaranteedRuneDrop)
        {
            if (useRegionBasedDropRates)
            {
                // Use automatic region-based rarity determination
                var rarity = DetermineRuneRarity(region, chapter, level, level == 8);
                var runeReward = CreateRandomRuneReward(rarity);
                result.runesEarned.Add(runeReward.GenerateRune());
            }
            else
            {
                // Use custom manual configuration
                foreach (var customRune in customGuaranteedRunes)
                {
                    result.runesEarned.Add(customRune.GenerateRune());
                }
            }
        }

        return result;
    }

    // ✅ Soul Coins Calculation per Combat Sequence
    public int CalculateSoulCoinsReward(int region, int chapter, int level)
    {
        int baseCoins = GetBaseCoinsByRegion(region);
        int levelProgression = GetLevelWithinRegion(region, chapter, level); // 1-64
        int scalingAmount = GetCoinScalingByRegion(region);

        int totalCoins = baseCoins + (levelProgression * scalingAmount);

        // Bonus multipliers
        float difficultyMultiplier = GetDifficultyMultiplier(region, chapter, level);
        float chapterBonusMultiplier = GetChapterBonusMultiplier(chapter); // Last level of chapter = bonus

        return Mathf.RoundToInt(totalCoins * difficultyMultiplier * chapterBonusMultiplier);
    }

    // ✅ NEW: Get level within region (1-64)
    private int GetLevelWithinRegion(int region, int chapter, int level)
    {
        // Calculate absolute level within the region
        // Each region has 8 chapters × 8 levels = 64 levels total
        int levelInRegion = ((chapter - 1) * 8) + level;
        return Mathf.Clamp(levelInRegion, 1, 64);
    }

    // ✅ NEW: Get difficulty multiplier
    private float GetDifficultyMultiplier(int region, int chapter, int level)
    {
        // Base multiplier = 1.0
        float multiplier = 1.0f;

        // Region difficulty scaling
        if (region <= 4) multiplier += 0.0f;   // Early game: no bonus
        else if (region <= 8) multiplier += 0.2f;   // Mid game: +20%
        else if (region <= 11) multiplier += 0.5f;   // Late game: +50%
        else multiplier += 1.0f;   // End game: +100%

        // Chapter boss bonus (level 8 of each chapter)
        if (level == 8) multiplier += 0.3f; // +30% for chapter bosses

        // Final boss bonus (chapter 8, level 8 = region boss)
        if (chapter == 8 && level == 8) multiplier += 0.5f; // Additional +50% for region bosses

        return multiplier;
    }

    private int GetBaseCoinsByRegion(int region)
    {
        if (region <= 4) return 1000;      // Early game
        if (region <= 8) return 3000;      // Mid game  
        if (region <= 11) return 8000;     // Late game
        return 15000;                      // End game
    }

    private int GetCoinScalingByRegion(int region)
    {
        if (region <= 4) return 50;        // +50 per level (early)
        if (region <= 8) return 100;       // +100 per level (mid)
        if (region <= 11) return 200;      // +200 per level (late)
        return 500;                        // +500 per level (end)
    }

    private float GetChapterBonusMultiplier(int chapter)
    {
        return (chapter == 8) ? 1.5f : 1.0f; // 50% bonus for chapter boss (level 8)
    }

    // ✅ Rune Drop Rate Calculation
    public RuneRarity DetermineRuneRarity(int region, int chapter, int level, bool isBoss = false)
    {
        var dropRates = GetDropRatesByRegion(region);

        if (isBoss || level == 8) // Chapter boss
        {
            dropRates = ApplyBossMultipliers(dropRates, region);
        }

        float randomValue = UnityEngine.Random.Range(0f, 100f); // ✅ FIXED: Explicit namespace
        float cumulativeChance = 0f;

        // Check from highest rarity to lowest
        RuneRarity[] rarities = { RuneRarity.Legendary, RuneRarity.Epic, RuneRarity.Rare, RuneRarity.Uncommon, RuneRarity.Common };

        foreach (var rarity in rarities)
        {
            cumulativeChance += dropRates[rarity];
            if (randomValue <= cumulativeChance)
            {
                return rarity;
            }
        }

        return RuneRarity.Common; // Fallback
    }

    // ✅ NEW: Apply boss multipliers to drop rates
    private Dictionary<RuneRarity, float> ApplyBossMultipliers(Dictionary<RuneRarity, float> baseRates, int region)
    {
        var bossRates = new Dictionary<RuneRarity, float>(baseRates);

        // Boss multipliers based on region
        if (region <= 4) // Early Game
        {
            bossRates[RuneRarity.Common] *= 1.0f;      // No change
            bossRates[RuneRarity.Uncommon] *= 1.2f;    // +20%
            bossRates[RuneRarity.Rare] *= 2.0f;       // +100%
            bossRates[RuneRarity.Epic] = 0f;          // Still 0%
            bossRates[RuneRarity.Legendary] = 0f;     // Still 0%
        }
        else if (region <= 8) // Mid Game
        {
            bossRates[RuneRarity.Common] *= 0.8f;      // -20%
            bossRates[RuneRarity.Uncommon] *= 1.0f;    // No change
            bossRates[RuneRarity.Rare] *= 1.5f;       // +50%
            bossRates[RuneRarity.Epic] *= 3.0f;       // +200%
            bossRates[RuneRarity.Legendary] = 0f;     // Still 0%
        }
        else if (region <= 11) // Late Game
        {
            bossRates[RuneRarity.Common] *= 0.5f;      // -50%
            bossRates[RuneRarity.Uncommon] *= 0.8f;    // -20%
            bossRates[RuneRarity.Rare] *= 1.2f;       // +20%
            bossRates[RuneRarity.Epic] *= 2.0f;       // +100%
            bossRates[RuneRarity.Legendary] *= 4.0f;  // +300%
        }
        else // End Game
        {
            bossRates[RuneRarity.Common] *= 0.2f;      // -80%
            bossRates[RuneRarity.Uncommon] *= 0.5f;    // -50%
            bossRates[RuneRarity.Rare] *= 0.8f;       // -20%
            bossRates[RuneRarity.Epic] *= 1.5f;       // +50%
            bossRates[RuneRarity.Legendary] *= 3.0f;  // +200%
        }

        // Normalize to ensure total doesn't exceed 100%
        float total = bossRates.Values.Sum();
        if (total > 100f)
        {
            foreach (var rarity in bossRates.Keys.ToList())
            {
                bossRates[rarity] = (bossRates[rarity] / total) * 100f;
            }
        }

        return bossRates;
    }

    // ✅ NEW: Create random rune reward for a specific rarity
    private RuneReward CreateRandomRuneReward(RuneRarity rarity)
    {
        // Random slot selection with weighted probabilities
        var slots = new RuneSlotPosition[]
        {
            RuneSlotPosition.Slot1, RuneSlotPosition.Slot2, RuneSlotPosition.Slot3,
            RuneSlotPosition.Slot4, RuneSlotPosition.Slot5, RuneSlotPosition.Slot6
        };

        // Random rune set selection
        var runeSets = new RuneType[]
        {
            RuneType.Blade, RuneType.Fatal, RuneType.Rage,
            RuneType.Energy, RuneType.Guard, RuneType.Swift
        };

        // Create rune reward with auto-balance
        var runeReward = new RuneReward
        {
            runeSet = runeSets[UnityEngine.Random.Range(0, runeSets.Length)],
            runeSlot = slots[UnityEngine.Random.Range(0, slots.Length)],
            rarity = rarity,
            dropChance = 1.0f, // Already determined by the rarity system
            useAutoBalance = true
        };

        // Auto-setup balanced ranges
        if (runeReward.useAutoBalance)
            runeReward.SetupAutoBalancedRanges();

        return runeReward;
    }

    private Dictionary<RuneRarity, float> GetDropRatesByRegion(int region)
    {
        if (region <= 4) // Early Game
        {
            return new Dictionary<RuneRarity, float>
            {
                { RuneRarity.Common, 70f },
                { RuneRarity.Uncommon, 25f },
                { RuneRarity.Rare, 5f },
                { RuneRarity.Epic, 0f },
                { RuneRarity.Legendary, 0f }
            };
        }
        else if (region <= 8) // Mid Game
        {
            return new Dictionary<RuneRarity, float>
            {
                { RuneRarity.Common, 40f },
                { RuneRarity.Uncommon, 45f },
                { RuneRarity.Rare, 14f },
                { RuneRarity.Epic, 1f },
                { RuneRarity.Legendary, 0f }
            };
        }
        else if (region <= 11) // Late Game
        {
            return new Dictionary<RuneRarity, float>
            {
                { RuneRarity.Common, 20f },
                { RuneRarity.Uncommon, 35f },
                { RuneRarity.Rare, 35f },
                { RuneRarity.Epic, 9f },
                { RuneRarity.Legendary, 1f }
            };
        }
        else // End Game
        {
            return new Dictionary<RuneRarity, float>
            {
                { RuneRarity.Common, 5f },
                { RuneRarity.Uncommon, 15f },
                { RuneRarity.Rare, 40f },
                { RuneRarity.Epic, 30f },
                { RuneRarity.Legendary, 10f }
            };
        }
    }
}

public class CombatResult
{
    public int soulCoinsEarned;
    public List<RuneData> runesEarned = new List<RuneData>();
    public float experienceEarned;
}

[System.Serializable]
public class StarRequirements
{
    [Header("One Star (Complete Battle)")]
    public string oneStarDescription = "Complete the battle";

    [Header("Two Stars")]
    public StarCondition twoStarCondition = StarCondition.CompleteUnderTime;
    public float twoStarTimeLimit = 180f; // 3 minutes
    public int twoStarMaxLosses = 1; // Max monsters that can be defeated
    public string twoStarDescription = "Complete under 3 minutes";

    [Header("Three Stars")]
    public StarCondition threeStarCondition = StarCondition.CompleteWithoutLosses;
    public float threeStarTimeLimit = 120f; // 2 minutes
    public int threeStarMaxLosses = 0; // No losses allowed
    public string threeStarDescription = "Complete without losing any monsters";
}

[System.Serializable]
public class ItemReward
{
    public string itemName;
    public Sprite itemIcon;
    public int quantity = 1;
    [Range(0f, 1f)]
    public float dropChance = 1f;
}

[System.Serializable]
public class MonsterReward
{
    public MonsterData monster;
    [Range(1, 5)]
    public int starLevel = 1;
    [Range(1, 60)]
    public int level = 1;
    [Range(0f, 1f)]
    public float unlockChance = 1f;
}

// Enums
public enum BattleDifficulty
{
    Easy,
    Normal,
    Hard,
    Nightmare
}

public enum WaveCompletionType
{
    DefeatAllEnemies,
    SurviveTime,
    DefeatSpecificEnemy,
    CollectItems
}

public enum SpawnPattern
{
    Sequential,
    Simultaneous,
    Random,
    Formation
}

public enum BattleModifier
{
    DoubleDamage,
    HalfHealth,
    SpeedBoost,
    NoHealing,
    TimePressure,
    ElementalBoost
}

public enum StarCondition
{
    CompleteUnderTime,
    CompleteWithoutLosses,
    CompleteWithSpecificTeam,
    CompleteWithMaxDamage
}
