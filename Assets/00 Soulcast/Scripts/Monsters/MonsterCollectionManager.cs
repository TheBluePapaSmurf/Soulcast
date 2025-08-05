// Monsters/MonsterCollectionManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class MonsterCollectionManager : MonoBehaviour
{
    [Header("Monster Collection")]
    [SerializeField] private List<CollectedMonster> collectedMonsters = new List<CollectedMonster>();

    [Header("Settings")]
    public int maxCollectionSize = 500;

    // Events
    public static event Action<CollectedMonster> OnMonsterAdded;
    public static event Action<CollectedMonster> OnMonsterLevelUp;
    public static event Action<CollectedMonster> OnMonsterStarUpgrade;

    public static MonsterCollectionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========== MONSTER MANAGEMENT ==========

    public void AddMonster(MonsterData monsterData)
    {
        if (monsterData == null) return;

        if (collectedMonsters.Count >= maxCollectionSize)
        {
            Debug.LogWarning("❌ Monster collection is full!");
            return;
        }

        CollectedMonster newMonster = new CollectedMonster(monsterData);
        collectedMonsters.Add(newMonster);

        OnMonsterAdded?.Invoke(newMonster);

        Debug.Log($"🐉 Added {monsterData.monsterName} to collection! (Total: {GetMonsterCount(monsterData)})");
        SaveManager.Instance?.AutoSave();
    }

    public List<CollectedMonster> GetAllMonsters()
    {
        return new List<CollectedMonster>(collectedMonsters);
    }

    public CollectedMonster GetMonsterByID(string uniqueID)
    {
        return collectedMonsters.FirstOrDefault(m => m.uniqueID == uniqueID);
    }

    public List<CollectedMonster> GetMonstersByType(MonsterData monsterData)
    {
        return collectedMonsters.Where(m => m.monsterData == monsterData).ToList();
    }

    public int GetMonsterCount(MonsterData monsterData)
    {
        return collectedMonsters.Count(m => m.monsterData == monsterData);
    }

    public List<MonsterData> GetUniqueMonsterTypes()
    {
        return collectedMonsters.Select(m => m.monsterData).Distinct().ToList();
    }

    public int GetCollectionCount()
    {
        return collectedMonsters.Count;
    }

    public int GetUniqueMonsterCount()
    {
        return GetUniqueMonsterTypes().Count;
    }

    // ========== MONSTER PROGRESSION ==========

    public void AddExperienceToMonster(string monsterID, int experience)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null) return;

        monster.AddExperience(experience);

        // Check for level up
        while (monster.CanLevelUp())
        {
            monster.LevelUp();
            OnMonsterLevelUp?.Invoke(monster);
        }

        SaveManager.Instance?.AutoSave();
    }

    public bool UpgradeMonsterStar(string monsterID)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null || !monster.CanUpgradeStar()) return false;

        monster.currentStarLevel++;
        OnMonsterStarUpgrade?.Invoke(monster);

        Debug.Log($"⭐ Upgraded {monster.monsterData.monsterName} to {monster.currentStarLevel} stars!");
        SaveManager.Instance?.AutoSave();
        return true;
    }

    // ========== RUNE EQUIPMENT ==========

    public bool EquipRuneToMonster(string monsterID, int slotIndex, RuneData rune)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null) return false;

        // Check if rune is available in RuneCollectionManager
        if (!RuneCollectionManager.Instance.IsRuneAvailable(rune))
        {
            Debug.LogWarning("❌ Rune is not available for equipping!");
            return false;
        }

        // Unequip rune from previous location
        UnequipRuneFromAllMonsters(rune);

        bool success = monster.EquipRune(rune, slotIndex);
        if (success)
        {
            SaveManager.Instance?.AutoSave();
        }
        return success;
    }

    public RuneData UnequipRuneFromMonster(string monsterID, int slotIndex)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null) return null;

        RuneData unequippedRune = monster.UnequipRune(slotIndex);
        if (unequippedRune != null)
        {
            SaveManager.Instance?.AutoSave();
        }
        return unequippedRune;
    }

    private void UnequipRuneFromAllMonsters(RuneData rune)
    {
        foreach (var monster in collectedMonsters)
        {
            for (int i = 0; i < monster.runeSlots.Length; i++)
            {
                if (monster.runeSlots[i]?.equippedRune == rune)
                {
                    monster.UnequipRune(i);
                    return; // Rune can only be equipped once
                }
            }
        }
    }

    public List<CollectedMonster> GetMonstersWithEquippedRune(RuneData rune)
    {
        return collectedMonsters.Where(m =>
            m.runeSlots.Any(slot => slot?.equippedRune == rune)
        ).ToList();
    }

    // ========== SAVE/LOAD ==========

    public void SaveMonsterCollection()
    {
        // Prepare all monsters for saving
        foreach (var monster in collectedMonsters)
        {
            monster.PrepareForSave();
        }

        ES3.Save("MonsterCollection", collectedMonsters, SaveManager.SAVE_FILE);
        Debug.Log($"🐉 Saved {collectedMonsters.Count} monsters");
    }

    public void LoadMonsterCollection()
    {
        collectedMonsters = ES3.Load("MonsterCollection", SaveManager.SAVE_FILE, new List<CollectedMonster>());

        // Restore references after loading
        foreach (var monster in collectedMonsters)
        {
            monster.RestoreAfterLoad();
        }

        Debug.Log($"🐉 Loaded {collectedMonsters.Count} monsters");
    }

    // ========== UTILITY ==========

    public List<CollectedMonster> GetMonstersByElement(ElementType element)
    {
        return collectedMonsters.Where(m => m.monsterData.element == element).ToList();
    }

    public List<CollectedMonster> GetMonstersByStarLevel(int starLevel)
    {
        return collectedMonsters.Where(m => m.currentStarLevel == starLevel).ToList();
    }

    public CollectedMonster GetHighestLevelMonster()
    {
        return collectedMonsters.OrderByDescending(m => m.level).FirstOrDefault();
    }
}
