// MonsterData.cs - ENHANCED WITH RUNE SYSTEM SUPPORT
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

    [Header("Star Rating System")]
    [Range(1, 5)]
    public int defaultStarLevel = 1;  // Default star level (1-5)

    [Header("3D Model")]
    public GameObject modelPrefab;

    [Header("Level & Experience System")]
    public int maxLevel = 60;
    public int baseExperienceRequired = 100;
    public float experienceGrowthRate = 1.5f;

    [Header("Base Stats")]
    public int baseHP = 100;
    public int baseATK = 20;
    public int baseDEF = 15;
    public int baseSPD = 10;
    public int baseEnergy = 50;

    [Header("Base Combat Stats (NEW)")]
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
}

// NEW: Enhanced Monster Stats Structure
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
        criticalDamage = 150f;
        accuracy = 85f;
        resistance = 15f;
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
