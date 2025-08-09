// MonsterData.cs - ENHANCED WITH ROLES AND BATTLE SYSTEM SUPPORT
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Monster Data", menuName = "Monster System/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("Basic Information")]
    public string monsterName;
    [TextArea(2, 4)]
    public string description;
    public ElementType element;

    [Header("Monster Role & Classification")]
    public MonsterRole role = MonsterRole.Balanced;
    [TextArea(1, 2)]
    public string roleDescription; // Automatisch gegenereerd

    [Header("Star Rating System")]
    [Range(1, 5)]
    public int defaultStarLevel = 1;  // Default star level (1-5)

    [Header("3D Model")]
    public GameObject modelPrefab;

    [Header("Level & Experience System")]
    public int defaultLevel = 1;
    public int maxLevel = 60;
    public int baseExperienceRequired = 100;
    public float experienceGrowthRate = 1.5f;

    [Header("Base Stats")]
    public int baseHP = 100;
    public int baseATK = 20;
    public int baseDEF = 15;
    public int baseSPD = 10;
    public int baseEnergy = 50;

    [Header("Base Combat Stats")]
    [Range(0f, 100f)]
    public float baseCriticalRate = 5f;     // Base critical hit chance (%)
    [Range(100f, 300f)]
    public float baseCriticalDamage = 150f; // Base critical damage multiplier (%)
    [Range(0f, 100f)]
    public float baseAccuracy = 85f;        // Base accuracy for debuffs (%)
    [Range(0f, 100f)]
    public float baseResistance = 15f;      // Base resistance to debuffs (%)

    [Header("Monster Actions")]
    public MonsterAction normalAttack;
    public MonsterAction specialAttack;
    public MonsterAction ultimate;

    [Header("Visual")]
    public Sprite icon;
    public Color monsterColor = Color.white;

    [Header("AI Behavior")]
    public float aggressiveness = 0.5f;
    public float intelligence = 0.5f;

    [Header("Battle Preferences")]
    public BattlePosition preferredPosition = BattlePosition.Any;
    public List<MonsterRole> synergyRoles = new List<MonsterRole>(); // Roles die goed samenwerken

    // Helper method to get all available actions
    public List<MonsterAction> GetAvailableActions()
    {
        List<MonsterAction> actions = new List<MonsterAction>();

        if (normalAttack != null) actions.Add(normalAttack);
        if (specialAttack != null) actions.Add(specialAttack);
        if (ultimate != null) actions.Add(ultimate);

        return actions;
    }

    // Get stat multiplier based on star level
    public float GetStarLevelMultiplier(int starLevel)
    {
        return 1f + (starLevel - 1) * 0.25f; // Each star adds 25% to stats
    }

    // Get rarity name based on star level
    public string GetRarityName()
    {
        return GetRarityName(defaultStarLevel);
    }

    public static string GetRarityName(int starLevel)
    {
        switch (starLevel)
        {
            case 1: return "Common";
            case 2: return "Uncommon";
            case 3: return "Rare";
            case 4: return "Epic";
            case 5: return "Legendary";
            default: return "Unknown";
        }
    }

    // Get role-specific stat bonuses
    public MonsterStats GetRoleAdjustedStats(int level = 1, int starLevel = 1)
    {
        MonsterStats baseStats = new MonsterStats(this, level, starLevel);
        return ApplyRoleBonuses(baseStats);
    }

    //Mocht dit statsbonussen in de monster collection geven zonder rune equiped. Pas de MonsterCollectionUI script aan, zodat de bonussen van de role niet als bonus stats worden weergeven.
    private MonsterStats ApplyRoleBonuses(MonsterStats stats)
    {
        MonsterStats adjustedStats = new MonsterStats
        {
            health = stats.health,
            attack = stats.attack,
            defense = stats.defense,
            speed = stats.speed,
            energy = stats.energy,
            criticalRate = stats.criticalRate,
            criticalDamage = stats.criticalDamage,
            accuracy = stats.accuracy,
            resistance = stats.resistance
        };

        // Apply role-specific multipliers
        switch (role)
        {
            case MonsterRole.Tank:
                adjustedStats.health = Mathf.RoundToInt(stats.health * 1.3f);
                adjustedStats.defense = Mathf.RoundToInt(stats.defense * 1.25f);
                adjustedStats.attack = Mathf.RoundToInt(stats.attack * 0.85f);
                adjustedStats.resistance += 10f;
                break;

            case MonsterRole.DPS:
                adjustedStats.attack = Mathf.RoundToInt(stats.attack * 1.35f);
                adjustedStats.criticalRate += 15f;
                adjustedStats.criticalDamage += 25f;
                adjustedStats.health = Mathf.RoundToInt(stats.health * 0.9f);
                break;

            case MonsterRole.Support:
                adjustedStats.energy = Mathf.RoundToInt(stats.energy * 1.2f);
                adjustedStats.speed = Mathf.RoundToInt(stats.speed * 1.15f);
                adjustedStats.accuracy += 15f;
                adjustedStats.attack = Mathf.RoundToInt(stats.attack * 0.8f);
                break;

            case MonsterRole.Healer:
                adjustedStats.energy = Mathf.RoundToInt(stats.energy * 1.25f);
                adjustedStats.resistance += 20f;
                adjustedStats.attack = Mathf.RoundToInt(stats.attack * 0.7f);
                adjustedStats.health = Mathf.RoundToInt(stats.health * 1.1f);
                break;

            case MonsterRole.Assassin:
                adjustedStats.speed = Mathf.RoundToInt(stats.speed * 1.4f);
                adjustedStats.criticalRate += 25f;
                adjustedStats.attack = Mathf.RoundToInt(stats.attack * 1.15f);
                adjustedStats.health = Mathf.RoundToInt(stats.health * 0.75f);
                adjustedStats.defense = Mathf.RoundToInt(stats.defense * 0.8f);
                break;

            case MonsterRole.Balanced:
                // No specific bonuses, well-rounded
                adjustedStats.health = Mathf.RoundToInt(stats.health * 1.05f);
                adjustedStats.attack = Mathf.RoundToInt(stats.attack * 1.05f);
                adjustedStats.defense = Mathf.RoundToInt(stats.defense * 1.05f);
                break;
        }

        return adjustedStats;
    }

    // Update role description automatically
    [ContextMenu("Update Role Description")]
    public void UpdateRoleDescription()
    {
        roleDescription = GetRoleDescription(role);
    }

    public static string GetRoleDescription(MonsterRole role)
    {
        switch (role)
        {
            case MonsterRole.Tank:
                return "High HP and Defense. Absorbs damage for the team.";
            case MonsterRole.DPS:
                return "High Attack and Critical Rate. Primary damage dealer.";
            case MonsterRole.Support:
                return "Buffs allies and debuffs enemies. High accuracy.";
            case MonsterRole.Healer:
                return "Restores HP and provides protection. High energy.";
            case MonsterRole.Assassin:
                return "Very fast with high critical chance. Low defense.";
            case MonsterRole.Balanced:
                return "Well-rounded stats. Adaptable to many situations.";
            default:
                return "Unknown role";
        }
    }

    // Validation in editor
    private void OnValidate()
    {
        // Auto-update role description when role changes
        if (Application.isEditor)
        {
            roleDescription = GetRoleDescription(role);
        }
    }
}

// Monster Role Enum
public enum MonsterRole
{
    Tank,       // High HP/Defense, protects team
    DPS,        // High Attack/Crit, main damage
    Support,    // Buffs/Debuffs, utility
    Healer,     // Healing and protection
    Assassin,   // Speed/Crit, high damage but fragile
    Balanced    // Well-rounded, no specific focus
}

// Battle Position Enum
public enum BattlePosition
{
    Front,      // First to be targeted
    Middle,     // Balanced position
    Back,       // Protected position
    Any         // No preference
}

// Enhanced Monster Stats Structure (unchanged, but added for completeness)
[System.Serializable]
public class MonsterStats
{
    public int health;
    public int attack;
    public int defense;
    public int speed;
    public int energy;
    public float criticalRate;      // Percentage (0-100)
    public float criticalDamage;    // Percentage (100-300+)
    public float accuracy;          // Percentage (0-100)
    public float resistance;        // Percentage (0-100)

    public MonsterStats()
    {
        health = 0;
        attack = 0;
        defense = 0;
        speed = 0;
        energy = 0;
        criticalRate = 0f;
        criticalDamage = 0f;
        accuracy = 0f;
        resistance = 0f;
    }

    public MonsterStats(MonsterData monsterData, int level = 1, int starLevel = 1)
    {
        float levelMultiplier = 1f + (level - 1) * 0.1f;
        float starMultiplier = monsterData.GetStarLevelMultiplier(starLevel);
        float totalMultiplier = levelMultiplier * starMultiplier;

        health = Mathf.RoundToInt(monsterData.baseHP * totalMultiplier);
        attack = Mathf.RoundToInt(monsterData.baseATK * totalMultiplier);
        defense = Mathf.RoundToInt(monsterData.baseDEF * totalMultiplier);
        speed = Mathf.RoundToInt(monsterData.baseSPD * totalMultiplier);
        energy = Mathf.RoundToInt(monsterData.baseEnergy * totalMultiplier);

        // Combat stats don't scale with level/star (affected by runes instead)
        criticalRate = monsterData.baseCriticalRate;
        criticalDamage = monsterData.baseCriticalDamage;
        accuracy = monsterData.baseAccuracy;
        resistance = monsterData.baseResistance;
    }
}
