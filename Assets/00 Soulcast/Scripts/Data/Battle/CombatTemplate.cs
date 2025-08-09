using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    public CombatRewards rewards;

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
public class CombatRewards
{
    [Header("Currency Rewards")]
    public int soulCoins;
    public int experience;

    [Header("Item Rewards")]
    public List<ItemReward> guaranteedItems = new List<ItemReward>();
    public List<ItemReward> randomItems = new List<ItemReward>();

    [Header("Monster Rewards")]
    public List<MonsterReward> unlockableMonsters = new List<MonsterReward>();

    [Header("Star-based Bonus Rewards")]
    public CombatRewards twoStarBonus;   // Extra rewards for 2+ stars
    public CombatRewards threeStarBonus; // Extra rewards for 3 stars
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
