using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// MonsterCollectionManager for pure procedural rune system - FIXED VERSION
/// MonsterData remains as ScriptableObjects, but all rune logic is procedural-only
/// </summary>
public class MonsterCollectionManager : MonoBehaviour
{
    [Header("Monster Collection")]
    [SerializeField] private List<CollectedMonster> collectedMonsters = new List<CollectedMonster>();

    [Header("Settings")]
    public int maxCollectionSize = 500;

    // Events
    public static event Action<CollectedMonster> OnMonsterAdded;
    public static event Action<CollectedMonster> OnMonsterRemoved;
    public static event Action<CollectedMonster> OnMonsterLevelUp;
    public static event Action<CollectedMonster> OnMonsterStatsChanged;
    public static event Action OnCollectionChanged;

    public static MonsterCollectionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("🏆 MonsterCollectionManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Validate collection integrity on start
        ValidateCollectionIntegrity();
    }

    // ========== MONSTER MANAGEMENT ==========

    /// <summary>
    /// Add a new monster to the collection using MonsterData ScriptableObject
    /// </summary>
    public bool AddMonster(MonsterData monsterData)
    {
        if (monsterData == null)
        {
            Debug.LogWarning("⚠️ Cannot add null MonsterData");
            return false;
        }

        if (collectedMonsters.Count >= maxCollectionSize)
        {
            Debug.LogWarning("❌ Monster collection is full!");
            return false;
        }

        CollectedMonster newMonster = new CollectedMonster(monsterData);
        collectedMonsters.Add(newMonster);

        Debug.Log($"🐉 Added {monsterData.monsterName} to collection! (Total: {GetMonsterCount(monsterData)})");

        // Fire events
        OnMonsterAdded?.Invoke(newMonster);
        OnCollectionChanged?.Invoke();

        // Auto-save
        SaveManager.Instance?.AutoSave();

        return true;
    }

    /// <summary>
    /// Remove a monster from the collection by unique ID
    /// </summary>
    public bool RemoveMonster(string uniqueID)
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogWarning("⚠️ Cannot remove monster with empty ID");
            return false;
        }

        var monster = GetMonsterByID(uniqueID);
        if (monster == null)
        {
            Debug.LogWarning($"⚠️ Monster not found with ID: {uniqueID}");
            return false;
        }

        // Unequip all runes before removing
        UnequipAllRunesFromMonster(monster);

        // Remove from collection
        bool removed = collectedMonsters.Remove(monster);

        if (removed)
        {
            Debug.Log($"🗑️ Removed {monster.GetDisplayName()} from collection");

            // Fire events
            OnMonsterRemoved?.Invoke(monster);
            OnCollectionChanged?.Invoke();

            // Auto-save
            SaveManager.Instance?.AutoSave();
        }

        return removed;
    }

    /// <summary>
    /// Get all monsters in the collection
    /// </summary>
    public List<CollectedMonster> GetAllMonsters()
    {
        return new List<CollectedMonster>(collectedMonsters);
    }

    /// <summary>
    /// Get monster by unique ID
    /// </summary>
    public CollectedMonster GetMonsterByID(string uniqueID)
    {
        if (string.IsNullOrEmpty(uniqueID)) return null;

        return collectedMonsters.FirstOrDefault(m => m.uniqueID == uniqueID);
    }

    /// <summary>
    /// Get all monsters of a specific MonsterData type
    /// </summary>
    public List<CollectedMonster> GetMonstersByType(MonsterData monsterData)
    {
        if (monsterData == null) return new List<CollectedMonster>();

        return collectedMonsters.Where(m => m.monsterData == monsterData).ToList();
    }

    /// <summary>
    /// Get count of monsters of a specific type
    /// </summary>
    public int GetMonsterCount(MonsterData monsterData)
    {
        if (monsterData == null) return 0;

        return collectedMonsters.Count(m => m.monsterData == monsterData);
    }

    /// <summary>
    /// Get all unique MonsterData types in collection
    /// </summary>
    public List<MonsterData> GetUniqueMonsterTypes()
    {
        return collectedMonsters
            .Where(m => m.monsterData != null)
            .Select(m => m.monsterData)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Get total number of monsters in collection
    /// </summary>
    public int GetCollectionCount()
    {
        return collectedMonsters.Count;
    }

    /// <summary>
    /// Get number of unique monster types
    /// </summary>
    public int GetUniqueMonsterCount()
    {
        return GetUniqueMonsterTypes().Count;
    }

    /// <summary>
    /// Remove a monster from the collection
    /// </summary>
    public void RemoveMonster(CollectedMonster monster)
    {
        if (monster == null) return;

        collectedMonsters.Remove(monster);
        Debug.Log($"🗑️ Removed monster: {monster.GetDisplayName()}");
    }


    // ========== MONSTER PROGRESSION ==========

    /// <summary>
    /// Add experience to a monster (ENHANCED DEBUG)
    /// </summary>
    public bool AddExperienceToMonster(string monsterID, int experience)
    {
        Debug.Log($"💫 === MONSTER COLLECTION MANAGER XP ===");
        Debug.Log($"💫 Monster ID: '{monsterID}'");
        Debug.Log($"💫 Experience to add: {experience}");

        if (experience <= 0)
        {
            Debug.LogError($"💫 ❌ Experience amount must be positive: {experience}");
            return false;
        }

        var monster = GetMonsterByID(monsterID);
        if (monster == null)
        {
            Debug.LogError($"💫 ❌ Monster not found with ID: '{monsterID}'");

            // Debug all available monster IDs
            var allMonsters = GetAllMonsters();
            Debug.Log($"💫 Available monsters in collection ({allMonsters.Count}):");
            for (int i = 0; i < allMonsters.Count; i++)
            {
                Debug.Log($"💫   {i}: '{allMonsters[i].uniqueID}' - {allMonsters[i].monsterData?.monsterName ?? "NULL MonsterData"}");
            }

            return false;
        }

        Debug.Log($"💫 ✅ Monster found: {monster.monsterData?.monsterName ?? "NULL MonsterData"}");
        Debug.Log($"💫 Before XP: Level {monster.currentLevel}, XP {monster.currentExperience}");

        // Apply XP to the monster
        bool leveledUp = monster.AddExperience(experience);

        Debug.Log($"💫 After XP: Level {monster.currentLevel}, XP {monster.currentExperience}");
        Debug.Log($"💫 Level up occurred: {leveledUp}");

        if (leveledUp)
        {
            Debug.Log($"💫 🎉 {monster.GetDisplayName()} leveled up to level {monster.currentLevel}!");
            OnMonsterLevelUp?.Invoke(monster);
            OnMonsterStatsChanged?.Invoke(monster);
        }

        // Auto-save
        SaveManager.Instance?.AutoSave();

        Debug.Log($"💫 === END MONSTER COLLECTION XP ===");
        return leveledUp;
    }



    /// <summary>
    /// Set monster level directly (for debugging/admin)
    /// </summary>
    public bool SetMonsterLevel(string monsterID, int targetLevel)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null) return false;

        if (targetLevel < 1 || targetLevel > monster.GetMaxLevel())
        {
            Debug.LogWarning($"⚠️ Invalid level: {targetLevel} (valid range: 1-{monster.GetMaxLevel()})");
            return false;
        }

        int oldLevel = monster.currentLevel;
        monster.currentLevel = targetLevel;
        monster.currentExperience = 0; // Reset experience
        monster.RefreshStats();

        Debug.Log($"🔧 Set {monster.GetDisplayName()} level: {oldLevel} → {targetLevel}");

        OnMonsterStatsChanged?.Invoke(monster);
        SaveManager.Instance?.AutoSave();

        return true;
    }

    // ========== PROCEDURAL RUNE EQUIPMENT ==========

    /// <summary>
    /// Equip a procedural rune to a monster
    /// </summary>
    public bool EquipRuneToMonster(string monsterID, int slotIndex, RuneData rune)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null)
        {
            Debug.LogWarning($"⚠️ Monster not found with ID: {monsterID}");
            return false;
        }

        // Validate rune is procedural
        if (rune == null || !rune.isProceduralGenerated)
        {
            Debug.LogError("❌ Only procedural runes can be equipped!");
            return false;
        }

        // Check if rune is available in RuneCollectionManager
        if (RuneCollectionManager.Instance == null || !RuneCollectionManager.Instance.IsRuneAvailable(rune))
        {
            Debug.LogWarning("❌ Rune is not available for equipping!");
            return false;
        }

        // Unequip rune from any other location first
        UnequipRuneFromAllMonsters(rune);

        // Equip to the target monster
        bool success = monster.EquipRune(slotIndex, rune);

        if (success)
        {
            Debug.Log($"✅ Equipped {rune.runeName} to {monster.GetDisplayName()} slot {slotIndex}");

            // Fire events
            OnMonsterStatsChanged?.Invoke(monster);

            // Auto-save
            SaveManager.Instance?.AutoSave();
        }

        return success;
    }

    /// <summary>
    /// Unequip a rune from a monster
    /// </summary>
    public RuneData UnequipRuneFromMonster(string monsterID, int slotIndex)
    {
        var monster = GetMonsterByID(monsterID);
        if (monster == null)
        {
            Debug.LogWarning($"⚠️ Monster not found with ID: {monsterID}");
            return null;
        }

        var equippedRune = monster.GetEquippedRune(slotIndex);
        bool success = monster.UnequipRune(slotIndex);

        if (success && equippedRune != null)
        {
            Debug.Log($"✅ Unequipped {equippedRune.runeName} from {monster.GetDisplayName()} slot {slotIndex}");

            // Fire events
            OnMonsterStatsChanged?.Invoke(monster);

            // Auto-save
            SaveManager.Instance?.AutoSave();
        }

        return equippedRune;
    }

    /// <summary>
    /// Unequip all runes from a specific monster
    /// </summary>
    public int UnequipAllRunesFromMonster(CollectedMonster monster)
    {
        if (monster == null) return 0;

        int unequippedCount = 0;

        for (int i = 0; i < 6; i++) // 6 rune slots
        {
            if (monster.HasRuneEquipped(i))
            {
                if (monster.UnequipRune(i))
                {
                    unequippedCount++;
                }
            }
        }

        if (unequippedCount > 0)
        {
            Debug.Log($"🔓 Unequipped {unequippedCount} runes from {monster.GetDisplayName()}");
            OnMonsterStatsChanged?.Invoke(monster);
        }

        return unequippedCount;
    }

    /// <summary>
    /// Unequip a specific procedural rune from all monsters
    /// </summary>
    private void UnequipRuneFromAllMonsters(RuneData rune)
    {
        if (rune == null || string.IsNullOrEmpty(rune.uniqueID)) return;

        foreach (var monster in collectedMonsters)
        {
            for (int i = 0; i < 6; i++) // 6 rune slots
            {
                var equippedRune = monster.GetEquippedRune(i);
                if (equippedRune != null && equippedRune.uniqueID == rune.uniqueID)
                {
                    monster.UnequipRune(i);
                    Debug.Log($"🔄 Auto-unequipped {rune.runeName} from {monster.GetDisplayName()}");
                    return; // Rune can only be equipped once
                }
            }
        }
    }

    /// <summary>
    /// Get all monsters that have a specific procedural rune equipped
    /// </summary>
    public List<CollectedMonster> GetMonstersWithEquippedRune(RuneData rune)
    {
        if (rune == null || string.IsNullOrEmpty(rune.uniqueID))
        {
            return new List<CollectedMonster>();
        }

        return collectedMonsters.Where(monster =>
            monster.GetAllEquippedRunes().Any(equippedRune =>
                equippedRune != null && equippedRune.uniqueID == rune.uniqueID)
        ).ToList();
    }

    /// <summary>
    /// Check if a procedural rune is equipped on any monster
    /// </summary>
    public bool IsRuneEquippedOnAnyMonster(RuneData rune)
    {
        return GetMonstersWithEquippedRune(rune).Count > 0;
    }

    // ========== SAVE/LOAD SYSTEM ==========

    /// <summary>
    /// Save monster collection to disk
    /// </summary>
    public void SaveMonsterCollection()
    {
        try
        {
            // Validate collection before saving
            ValidateCollectionIntegrity();

            ES3.Save("MonsterCollection", collectedMonsters, SaveManager.SAVE_FILE);
            ES3.Save("MaxCollectionSize", maxCollectionSize, SaveManager.SAVE_FILE);

            Debug.Log($"💾 Saved {collectedMonsters.Count} monsters to collection");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save monster collection: {e.Message}");
        }
    }

    /// <summary>
    /// Load monster collection from disk
    /// </summary>
    public void LoadMonsterCollection()
    {
        try
        {
            collectedMonsters = ES3.Load("MonsterCollection", SaveManager.SAVE_FILE, new List<CollectedMonster>());
            maxCollectionSize = ES3.Load("MaxCollectionSize", SaveManager.SAVE_FILE, 500);

            Debug.Log($"📥 Loading {collectedMonsters.Count} monsters...");

            // Restore references after loading
            int restoredCount = 0;
            foreach (var monster in collectedMonsters)
            {
                if (monster != null)
                {
                    monster.RestoreAfterLoad();
                    restoredCount++;
                }
            }

            // Remove any null monsters
            collectedMonsters.RemoveAll(m => m == null);

            Debug.Log($"🐉 Loaded {collectedMonsters.Count} monsters ({restoredCount} restored successfully)");

            // Fire event
            OnCollectionChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load monster collection: {e.Message}");
            collectedMonsters = new List<CollectedMonster>();
        }
    }

    // ========== COLLECTION QUERIES ==========

    /// <summary>
    /// Get monsters by element type (if MonsterData has element field)
    /// </summary>
    public List<CollectedMonster> GetMonstersByElement(ElementType element)
    {
        return collectedMonsters.Where(m =>
            m.monsterData != null &&
            m.monsterData.element == element
        ).ToList();
    }

    /// <summary>
    /// Get monsters by level range
    /// </summary>
    public List<CollectedMonster> GetMonstersByLevelRange(int minLevel, int maxLevel)
    {
        return collectedMonsters.Where(m =>
            m.currentLevel >= minLevel &&
            m.currentLevel <= maxLevel
        ).ToList();
    }

    /// <summary>
    /// Get monsters sorted by power rating
    /// </summary>
    public List<CollectedMonster> GetMonstersByPowerRating(bool descending = true)
    {
        if (descending)
        {
            return collectedMonsters.OrderByDescending(m => m.GetPowerRating()).ToList();
        }
        else
        {
            return collectedMonsters.OrderBy(m => m.GetPowerRating()).ToList();
        }
    }

    /// <summary>
    /// Get highest level monster
    /// </summary>
    public CollectedMonster GetHighestLevelMonster()
    {
        return collectedMonsters.OrderByDescending(m => m.currentLevel).FirstOrDefault();
    }

    /// <summary>
    /// Get strongest monster by power rating
    /// </summary>
    public CollectedMonster GetStrongestMonster()
    {
        return collectedMonsters.OrderByDescending(m => m.GetPowerRating()).FirstOrDefault();
    }

    /// <summary>
    /// Get monsters with the most runes equipped
    /// </summary>
    public List<CollectedMonster> GetMonstersWithMostRunes()
    {
        return collectedMonsters.OrderByDescending(m => m.GetEquippedRuneCount()).ToList();
    }

    /// <summary>
    /// Get collection statistics - FIXED VERSION
    /// </summary>
    public CollectionStats GetCollectionStats()
    {
        var stats = new CollectionStats
        {
            totalMonsters = collectedMonsters.Count,
            uniqueTypes = GetUniqueMonsterCount(),
            // ✅ FIXED: Use currentLevel instead of non-existent property
            averageLevel = collectedMonsters.Count > 0 ?
                (float)collectedMonsters.Average(m => m.currentLevel) : 0,
            highestLevel = collectedMonsters.Count > 0 ?
                collectedMonsters.Max(m => m.currentLevel) : 0,
            totalPowerRating = collectedMonsters.Sum(m => m.GetPowerRating()),
            totalEquippedRunes = collectedMonsters.Sum(m => m.GetEquippedRuneCount())
        };

        return stats;
    }

    // ========== VALIDATION AND UTILITY ==========

    /// <summary>
    /// Validate collection integrity
    /// </summary>
    public void ValidateCollectionIntegrity()
    {
        int issuesFixed = 0;

        for (int i = collectedMonsters.Count - 1; i >= 0; i--)
        {
            var monster = collectedMonsters[i];

            if (monster == null)
            {
                collectedMonsters.RemoveAt(i);
                issuesFixed++;
                continue;
            }

            // Validate monster integrity
            if (!monster.ValidateIntegrity())
            {
                issuesFixed++;
            }

            // Check for duplicate IDs
            var duplicates = collectedMonsters.Where(m =>
                m != null &&
                m != monster &&
                m.uniqueID == monster.uniqueID
            ).ToList();

            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"⚠️ Found duplicate monster ID: {monster.uniqueID}, generating new ID");
                monster.uniqueID = System.Guid.NewGuid().ToString();
                issuesFixed++;
            }
        }

        if (issuesFixed > 0)
        {
            Debug.Log($"🔧 Fixed {issuesFixed} collection integrity issues");
        }
    }

    /// <summary>
    /// Debug collection information
    /// </summary>
    [ContextMenu("Debug Collection Info")]
    public void DebugCollectionInfo()
    {
        var stats = GetCollectionStats();

        Debug.Log("=== MONSTER COLLECTION DEBUG ===");
        Debug.Log($"Total Monsters: {stats.totalMonsters}/{maxCollectionSize}");
        Debug.Log($"Unique Types: {stats.uniqueTypes}");
        Debug.Log($"Average Level: {stats.averageLevel:F1}");
        Debug.Log($"Highest Level: {stats.highestLevel}");
        Debug.Log($"Total Power: {stats.totalPowerRating}");
        Debug.Log($"Total Equipped Runes: {stats.totalEquippedRunes}");

        Debug.Log("\n=== TOP 5 MONSTERS ===");
        var topMonsters = GetMonstersByPowerRating().Take(5);
        foreach (var monster in topMonsters)
        {
            Debug.Log($"  {monster.GetSummary()}");
        }
    }

    /// <summary>
    /// Clear entire collection (for debugging)
    /// </summary>
    [ContextMenu("Clear Collection (DEBUG)")]
    public void ClearCollection()
    {
        int count = collectedMonsters.Count;
        collectedMonsters.Clear();

        Debug.LogWarning($"🗑️ Cleared {count} monsters from collection");
        OnCollectionChanged?.Invoke();
        SaveManager.Instance?.AutoSave();
    }
}

// ========== COLLECTION STATISTICS ==========

/// <summary>
/// Statistics about the monster collection
/// </summary>
[System.Serializable]
public class CollectionStats
{
    public int totalMonsters;
    public int uniqueTypes;
    public float averageLevel;  // ✅ FIXED: Now using float for averages
    public int highestLevel;
    public int totalPowerRating;
    public int totalEquippedRunes;
}
