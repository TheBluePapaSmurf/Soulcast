// Data/CollectedMonster.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class CollectedMonster
{
    // ✅ SERIALIZABLE data (saved to disk)
    public string monsterDataName;
    public int level = 1;
    public int currentStarLevel;
    public long dateObtained;
    public string uniqueID;

    [Header("Experience System")]
    public int currentExperience = 0;
    public int experienceToNextLevel = 100;

    [Header("Rune System")]
    public string[] equippedRuneNames = new string[6]; // Names in plaats van references

    // ✅ NON-SERIALIZABLE runtime data (loaded from resources)
    [System.NonSerialized]
    public MonsterData monsterData;

    [System.NonSerialized]
    private RuneSlot[] _runeSlots;

    public RuneSlot[] runeSlots
    {
        get
        {
            if (_runeSlots == null) InitializeRuneSlots();
            return _runeSlots;
        }
    }

    public DateTime GetDateObtained()
    {
        return DateTime.FromBinary(dateObtained);
    }

    public CollectedMonster(MonsterData data)
    {
        monsterData = data;
        monsterDataName = data != null ? data.name : "";
        currentStarLevel = data != null ? data.defaultStarLevel : 1;
        level = 1;
        dateObtained = DateTime.Now.ToBinary();
        uniqueID = Guid.NewGuid().ToString();

        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        currentExperience = 0;
        experienceToNextLevel = CalculateExperienceForLevel(2);
        equippedRuneNames = new string[6];
        InitializeRuneSlots();
    }

    private void InitializeRuneSlots()
    {
        _runeSlots = new RuneSlot[6];
        for (int i = 0; i < 6; i++)
        {
            _runeSlots[i] = new RuneSlot
            {
                slotIndex = i,
                equippedRune = null
            };
        }
    }

    // ========== EXPERIENCE SYSTEM ==========

    /// <summary>
    /// Add experience to this monster
    /// </summary>
    public void AddExperience(int amount)
    {
        currentExperience += amount;
        Debug.Log($"{monsterData?.monsterName ?? "Monster"} gained {amount} EXP! ({currentExperience}/{experienceToNextLevel})");
    }

    /// <summary>
    /// Check if monster can level up
    /// </summary>
    public bool CanLevelUp()
    {
        return currentExperience >= experienceToNextLevel && level < GetMaxLevel();
    }

    /// <summary>
    /// Level up the monster
    /// </summary>
    public void LevelUp()
    {
        if (!CanLevelUp()) return;

        currentExperience -= experienceToNextLevel;
        level++;

        // Increase experience requirement for next level (exponential growth)
        experienceToNextLevel = CalculateExperienceForLevel(level + 1);

        Debug.Log($"🆙 {monsterData?.monsterName ?? "Monster"} leveled up to {level}!");
    }

    /// <summary>
    /// Calculate experience needed for a specific level
    /// </summary>
    private int CalculateExperienceForLevel(int targetLevel)
    {
        // Exponential growth formula: 100 * level^1.5
        return Mathf.RoundToInt(100 * Mathf.Pow(targetLevel, 1.5f));
    }

    /// <summary>
    /// Get maximum level for monsters
    /// </summary>
    public int GetMaxLevel()
    {
        return 60; // Max level
    }

    /// <summary>
    /// Get experience percentage for UI progress bars
    /// </summary>
    public float GetExperiencePercentage()
    {
        if (experienceToNextLevel <= 0) return 1f;
        return (float)currentExperience / experienceToNextLevel;
    }

    // ========== STAT CALCULATION ==========

    /// <summary>
    /// Get current effective stats with level, star, and rune bonuses
    /// </summary>
    // In CollectedMonster class - Update GetEffectiveStats method
    public MonsterStats GetEffectiveStats()
    {
        if (monsterData == null) return new MonsterStats();

        // Get base stats (level + star bonuses)
        MonsterStats stats = new MonsterStats(monsterData, level, currentStarLevel);

        // Apply rune bonuses via RuneInventory
        if (RuneCollectionManager.Instance != null)
        {
            var runeBonuses = RuneCollectionManager.Instance.CalculateMonsterRuneBonuses(this);
            AddStats(ref stats, runeBonuses);
        }

        return stats;
    }

    private void AddStats(ref MonsterStats target, MonsterStats source)
    {
        target.health += source.health;
        target.attack += source.attack;
        target.defense += source.defense;
        target.speed += source.speed;
        target.energy += source.energy;
        target.criticalRate += source.criticalRate;
        target.criticalDamage += source.criticalDamage;
        target.accuracy += source.accuracy;
        target.resistance += source.resistance;
    }

    private void ApplyRuneBonuses(ref MonsterStats stats)
    {
        // Apply individual rune stats
        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (runeSlots[i]?.equippedRune != null)
            {
                ApplyRuneStats(runeSlots[i].equippedRune, ref stats);
            }
        }

        // Apply set bonuses
        ApplySetBonuses(ref stats);
    }

    private void ApplyRuneStats(RuneData rune, ref MonsterStats stats)
    {
        // Add rune's main stat and sub stats to monster stats
        var runeStats = RuneCollectionManager.Instance.CalculateRuneStatBonus(rune);

        stats.health += runeStats.health;
        stats.attack += runeStats.attack;
        stats.defense += runeStats.defense;
        stats.speed += runeStats.speed;
        stats.criticalRate += runeStats.criticalRate;
        stats.criticalDamage += runeStats.criticalDamage;
        stats.accuracy += runeStats.accuracy;
        stats.resistance += runeStats.resistance;
    }

    private void ApplySetBonuses(ref MonsterStats stats)
    {
        // Count rune sets
        var runeCounts = new Dictionary<RuneType, int>();

        foreach (var slot in runeSlots)
        {
            if (slot?.equippedRune != null)
            {
                var runeType = slot.equippedRune.runeType;
                runeCounts[runeType] = runeCounts.GetValueOrDefault(runeType, 0) + 1;
            }
        }

        // Apply set bonuses (2-piece and 4-piece sets)
        foreach (var kvp in runeCounts)
        {
            var runeType = kvp.Key;
            var count = kvp.Value;

            if (count >= 2)
            {
                ApplyTwoPieceBonus(runeType, ref stats);
            }

            if (count >= 4)
            {
                ApplyFourPieceBonus(runeType, ref stats);
            }
        }
    }

    private void ApplyTwoPieceBonus(RuneType runeType, ref MonsterStats stats)
    {
        switch (runeType)
        {
            case RuneType.Blade:
                stats.attack = Mathf.RoundToInt(stats.attack * 1.12f); // +12% ATK
                break;
            case RuneType.Fatal:
                stats.criticalDamage += 20f; // +20% Crit Damage
                break;
            case RuneType.Rage:
                stats.criticalRate += 12f; // +12% Crit Rate
                break;
            case RuneType.Energy:
                stats.speed = Mathf.RoundToInt(stats.speed * 1.15f); // +15% SPD
                break;
            case RuneType.Guard:
                stats.health = Mathf.RoundToInt(stats.health * 1.15f); // +15% HP
                break;
            case RuneType.Swift:
                stats.speed += 25; // +25 SPD
                break;
        }
    }

    private void ApplyFourPieceBonus(RuneType runeType, ref MonsterStats stats)
    {
        switch (runeType)
        {
            case RuneType.Blade:
                stats.criticalRate += 20f; // +20% Crit Rate
                break;
            case RuneType.Fatal:
                stats.attack = Mathf.RoundToInt(stats.attack * 1.35f); // +35% ATK
                break;
            case RuneType.Rage:
                stats.criticalDamage += 40f; // +40% Crit Damage
                break;
            case RuneType.Energy:
                stats.energy = Mathf.RoundToInt(stats.energy * 1.25f); // +25% Energy
                break;
            case RuneType.Guard:
                stats.defense = Mathf.RoundToInt(stats.defense * 1.25f); // +25% DEF
                break;
            case RuneType.Swift:
                stats.speed = Mathf.RoundToInt(stats.speed * 1.25f); // +25% SPD
                break;
        }
    }

    // ========== RUNE MANAGEMENT ==========

    public bool CanEquipRune(RuneData rune, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= runeSlots.Length) return false;
        if (rune == null) return false;

        // Add your rune equip logic here
        return true;
    }

    public bool EquipRune(RuneData rune, int slotIndex)
    {
        if (!CanEquipRune(rune, slotIndex)) return false;

        runeSlots[slotIndex].equippedRune = rune;
        equippedRuneNames[slotIndex] = rune.name;
        return true;
    }

    public RuneData UnequipRune(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= runeSlots.Length) return null;

        var rune = runeSlots[slotIndex].equippedRune;
        runeSlots[slotIndex].equippedRune = null;
        equippedRuneNames[slotIndex] = "";
        return rune;
    }

    public int GetMaxStarLevel()
    {
        return 6;
    }

    public bool CanUpgradeStar()
    {
        return currentStarLevel < GetMaxStarLevel();
    }

    // ========== SAVE/LOAD HELPERS ==========

    /// <summary>
    /// Prepare for saving - update serializable fields
    /// </summary>
    public void PrepareForSave()
    {
        // Update serializable fields before saving
        if (monsterData != null)
        {
            monsterDataName = monsterData.name;
        }

        // Update equipped rune names
        for (int i = 0; i < equippedRuneNames.Length; i++)
        {
            if (i < runeSlots.Length && runeSlots[i]?.equippedRune != null)
            {
                equippedRuneNames[i] = runeSlots[i].equippedRune.name;
            }
            else
            {
                equippedRuneNames[i] = "";
            }
        }
    }

    /// <summary>
    /// Restore after loading - rebuild references
    /// </summary>
    public void RestoreAfterLoad()
    {
        // Restore MonsterData reference
        LoadMonsterData();

        // Initialize runtime data
        InitializeRuneSlots();

        // Restore equipped runes
        RestoreEquippedRunes();
    }

    /// <summary>
    /// Load MonsterData from Resources
    /// </summary>
    /// <summary>
    /// Load MonsterData from multiple possible locations
    /// </summary>
    private void LoadMonsterData()
    {
        if (monsterData != null || string.IsNullOrEmpty(monsterDataName))
            return;

        Debug.Log($"🔍 Loading MonsterData: {monsterDataName}");

        // Strategy 1: Try standard Resources/Monsters/ folder
        monsterData = Resources.Load<MonsterData>($"Monsters/{monsterDataName}");
        if (monsterData != null)
        {
            Debug.Log($"✅ Loaded from Resources/Monsters/: {monsterDataName}");
            return;
        }

        // Strategy 2: Try Resources root folder
        monsterData = Resources.Load<MonsterData>(monsterDataName);
        if (monsterData != null)
        {
            Debug.Log($"✅ Loaded from Resources/: {monsterDataName}");
            return;
        }

        // Strategy 3: Try alternative folder structure
        monsterData = Resources.Load<MonsterData>($"MonsterData/{monsterDataName}");
        if (monsterData != null)
        {
            Debug.Log($"✅ Loaded from Resources/MonsterData/: {monsterDataName}");
            return;
        }

        // Strategy 4: Search in all loaded assets (non-Resources)
        var allMonsterData = Resources.FindObjectsOfTypeAll<MonsterData>();
        monsterData = allMonsterData.FirstOrDefault(m => m.name == monsterDataName);

        if (monsterData != null)
        {
            Debug.Log($"✅ Found in loaded assets: {monsterDataName}");
            return;
        }

        Debug.LogError($"❌ Could not load MonsterData: {monsterDataName}");
        Debug.LogError($"💡 Make sure '{monsterDataName}.asset' is in Resources/Monsters/ folder");
    }


    /// <summary>
    /// Restore equipped runes from names
    /// </summary>
    private void RestoreEquippedRunes()
    {
        if (RuneCollectionManager.Instance != null)
        {
            for (int i = 0; i < equippedRuneNames.Length && i < runeSlots.Length; i++)
            {
                if (!string.IsNullOrEmpty(equippedRuneNames[i]))
                {
                    var rune = RuneCollectionManager.Instance.GetAllRunes().FirstOrDefault(r => r.name == equippedRuneNames[i]);
                    if (rune != null && runeSlots[i] != null)
                    {
                        runeSlots[i].equippedRune = rune;
                    }
                }
            }
        }
    }
}

// ========== RUNE SLOT SYSTEM ==========

[System.Serializable]
public class RuneSlot
{
    public int slotIndex;
    public RuneType requiredRuneType;
    public RuneData equippedRune;

    public bool IsEmpty => equippedRune == null;

    public bool CanEquip(RuneData rune)
    {
        if (rune == null) return false;

        // Add your specific equip rules here
        return true;
    }
}
