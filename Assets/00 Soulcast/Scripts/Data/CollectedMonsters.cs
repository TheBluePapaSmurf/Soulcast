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

    public void AddExperience(int amount)
    {
        currentExperience += amount;
        Debug.Log($"{monsterData?.monsterName ?? "Monster"} gained {amount} EXP! ({currentExperience}/{experienceToNextLevel})");
    }

    public bool CanLevelUp()
    {
        return currentExperience >= experienceToNextLevel && level < GetMaxLevel();
    }

    public void LevelUp()
    {
        if (!CanLevelUp()) return;

        currentExperience -= experienceToNextLevel;
        level++;

        experienceToNextLevel = CalculateExperienceForLevel(level + 1);

        Debug.Log($"🆙 {monsterData?.monsterName ?? "Monster"} leveled up to {level}!");
    }

    private int CalculateExperienceForLevel(int targetLevel)
    {
        return Mathf.RoundToInt(100 * Mathf.Pow(targetLevel, 1.5f));
    }

    public int GetMaxLevel()
    {
        return 60;
    }

    public float GetExperiencePercentage()
    {
        if (experienceToNextLevel <= 0) return 1f;
        return (float)currentExperience / experienceToNextLevel;
    }

    // ========== STAT CALCULATION ==========

    /// <summary>
    /// Get current effective stats with level, star, and rune bonuses
    /// Uses ONLY the RuneCollectionManager system (no duplicate bonuses)
    /// </summary>
    public MonsterStats GetEffectiveStats()
    {
        if (monsterData == null) return new MonsterStats();

        // Get base stats (level + star bonuses)
        MonsterStats stats = new MonsterStats(monsterData, level, currentStarLevel);

        // Apply rune bonuses via RuneCollectionManager ONLY
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

    // ========== RUNE MANAGEMENT ==========

    public bool CanEquipRune(RuneData rune, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= runeSlots.Length) return false;
        if (rune == null) return false;

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

    public void PrepareForSave()
    {
        if (monsterData != null)
        {
            monsterDataName = monsterData.name;
        }

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

    public void RestoreAfterLoad()
    {
        LoadMonsterData();
        InitializeRuneSlots();
        RestoreEquippedRunes();
    }

    private void LoadMonsterData()
    {
        if (monsterData != null || string.IsNullOrEmpty(monsterDataName))
            return;

        Debug.Log($"🔍 Loading MonsterData: {monsterDataName}");

        monsterData = Resources.Load<MonsterData>($"Monsters/{monsterDataName}");
        if (monsterData != null)
        {
            Debug.Log($"✅ Loaded from Resources/Monsters/: {monsterDataName}");
            return;
        }

        monsterData = Resources.Load<MonsterData>(monsterDataName);
        if (monsterData != null)
        {
            Debug.Log($"✅ Loaded from Resources/: {monsterDataName}");
            return;
        }

        monsterData = Resources.Load<MonsterData>($"MonsterData/{monsterDataName}");
        if (monsterData != null)
        {
            Debug.Log($"✅ Loaded from Resources/MonsterData/: {monsterDataName}");
            return;
        }

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
        return true;
    }
}
