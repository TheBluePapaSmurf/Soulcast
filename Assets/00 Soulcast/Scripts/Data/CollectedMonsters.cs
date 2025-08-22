using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// CollectedMonster with pure procedural rune system (ID-based only)
/// No legacy ScriptableObject support - procedural runes only
/// </summary>
[System.Serializable]
public class CollectedMonster
{
    [Header("Basic Monster Data")]
    public string uniqueID;
    public string monsterDataName;
    public MonsterData monsterData;

    // In CollectedMonster class, voeg toe onder Monster Status:
    [Header("Monster Status")]
    public int currentLevel = 1;
    public int currentExperience = 0;
    public int currentStarLevel = 1; // ✅ NEW: Add if you need star level functionality
    public MonsterStats baseStats;
    public MonsterStats currentStats;


    [Header("Rune Equipment - Pure ID System")]
    public string[] equippedRuneIDs = new string[6]; // Only unique IDs
    public RuneSlot[] runeSlots = new RuneSlot[6];

    [Header("Legacy Migration Only")]
    [System.Obsolete("Used for migration only")]
    public string[] equippedRuneNames = new string[6]; // Only for migration

    [Header("Monster State")]
    public bool isUnlocked = false;
    public bool isInBattleTeam = false;
    public DateTime acquisitionDate;

    // Constructor for new monster
    public CollectedMonster(MonsterData data)
    {
        uniqueID = System.Guid.NewGuid().ToString();
        monsterData = data;
        monsterDataName = data?.name ?? "Unknown";

        currentLevel = 1;
        currentExperience = 0;
        currentStarLevel = data?.defaultStarLevel ?? 1;

        if (data != null)
        {
            baseStats = new MonsterStats
            {
                health = data.baseHP,
                attack = data.baseATK,
                defense = data.baseDEF,
                speed = data.baseSPD,
                energy = data.baseEnergy,
                criticalRate = data.baseCriticalRate,
                criticalDamage = data.baseCriticalDamage,
                accuracy = data.baseAccuracy,
                resistance = data.baseResistance
            };
        }
        else
        {
            baseStats = new MonsterStats();
        }
        currentStats = CalculateCurrentStats();

        // Initialize rune system
        InitializeRuneSlots();
        InitializeRuneIDs();

        isUnlocked = true;
        acquisitionDate = DateTime.Now;

        Debug.Log($"✅ Created new monster: {monsterData?.monsterName} (ID: {uniqueID})");
    }

    // Default constructor for deserialization
    public CollectedMonster()
    {
        InitializeRuneIDs();
        InitializeRuneSlots();
    }

    // ========== PURE ID-BASED RUNE SYSTEM ==========

    /// <summary>
    /// Initialize rune ID arrays
    /// </summary>
    private void InitializeRuneIDs()
    {
        if (equippedRuneIDs == null || equippedRuneIDs.Length != 6)
        {
            equippedRuneIDs = new string[6];
        }

        // Initialize all to empty strings
        for (int i = 0; i < equippedRuneIDs.Length; i++)
        {
            if (equippedRuneIDs[i] == null)
            {
                equippedRuneIDs[i] = "";
            }
        }
    }

    /// <summary>
    /// Initialize rune slots with proper slot configuration
    /// </summary>
    private void InitializeRuneSlots()
    {
        if (runeSlots == null || runeSlots.Length != 6)
        {
            runeSlots = new RuneSlot[6];
        }

        // Initialize each slot
        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (runeSlots[i] == null)
            {
                runeSlots[i] = new RuneSlot
                {
                    slotIndex = i,
                    equippedRune = null
                };
            }
        }
    }

    /// <summary>
    /// Equip a procedural rune to a specific slot
    /// </summary>
    public bool EquipRune(int slotIndex, RuneData rune)
    {
        if (!IsValidSlotIndex(slotIndex) || !IsValidProceduralRune(rune))
        {
            Debug.LogWarning($"⚠️ Invalid equip parameters: slot {slotIndex}, rune {rune?.runeName}");
            return false;
        }

        // Unequip existing rune if present
        if (!string.IsNullOrEmpty(equippedRuneIDs[slotIndex]))
        {
            UnequipRune(slotIndex);
        }

        // Equip the new rune
        runeSlots[slotIndex].equippedRune = rune;
        equippedRuneIDs[slotIndex] = rune.uniqueID;

        // Update stats
        currentStats = CalculateCurrentStats();

        Debug.Log($"✅ Equipped procedural rune {rune.runeName} (ID: {rune.uniqueID}) to slot {slotIndex}");
        return true;
    }

    /// <summary>
    /// Unequip a rune from a specific slot
    /// </summary>
    public bool UnequipRune(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning($"⚠️ Invalid slot index for unequip: {slotIndex}");
            return false;
        }

        var previousRune = runeSlots[slotIndex].equippedRune;

        // Clear the slot
        runeSlots[slotIndex].equippedRune = null;
        equippedRuneIDs[slotIndex] = "";

        // Update stats
        currentStats = CalculateCurrentStats();

        if (previousRune != null)
        {
            Debug.Log($"✅ Unequipped procedural rune {previousRune.runeName} from slot {slotIndex}");
        }

        return true;
    }

    /// <summary>
    /// Validate that a rune is a valid procedural rune
    /// </summary>
    private bool IsValidProceduralRune(RuneData rune)
    {
        if (rune == null)
        {
            Debug.LogWarning("⚠️ Rune is null");
            return false;
        }

        if (!rune.isProceduralGenerated)
        {
            Debug.LogError($"❌ Only procedural runes are supported! Rune: {rune.runeName}");
            return false;
        }

        if (string.IsNullOrEmpty(rune.uniqueID))
        {
            Debug.LogError($"❌ Procedural rune missing uniqueID: {rune.runeName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Find procedural rune by unique ID
    /// </summary>
    private RuneData FindRuneByID(string runeID)
    {
        if (RuneCollectionManager.Instance == null || string.IsNullOrEmpty(runeID))
        {
            return null;
        }

        Debug.Log($"🔍 Looking for procedural rune with ID: {runeID}");

        var rune = RuneCollectionManager.Instance.FindRuneByID(runeID);

        if (rune != null)
        {
            if (!rune.isProceduralGenerated)
            {
                Debug.LogError($"❌ Found rune with ID {runeID} but it's not procedural: {rune.runeName}");
                return null;
            }

            Debug.Log($"✅ Found procedural rune: {rune.runeName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Procedural rune not found with ID: {runeID}");
        }

        return rune;
    }

    /// <summary>
    /// Check if slot index is valid
    /// </summary>
    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < runeSlots.Length;
    }

    /// <summary>
    /// Get equipped rune in a specific slot
    /// </summary>
    public RuneData GetEquippedRune(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex) || runeSlots[slotIndex] == null)
        {
            return null;
        }

        var rune = runeSlots[slotIndex].equippedRune;

        // 🛡️ CRITICAL FIX: Block empty runes from being returned
        if (rune != null && string.IsNullOrEmpty(rune.runeName))
        {
            Debug.LogWarning($"🚫 Blocked empty rune in slot {slotIndex} - clearing it");
            runeSlots[slotIndex].equippedRune = null; // Clear the empty rune
            return null;
        }

        return rune;
    }



    /// <summary>
    /// Check if a specific slot has a rune equipped
    /// </summary>
    public bool HasRuneEquipped(int slotIndex)
    {
        return GetEquippedRune(slotIndex) != null;
    }

    /// <summary>
    /// Get all equipped runes
    /// </summary>
    public List<RuneData> GetAllEquippedRunes()
    {
        var equippedRunes = new List<RuneData>();

        for (int i = 0; i < runeSlots.Length; i++)
        {
            var rune = GetEquippedRune(i);
            if (rune != null)
            {
                equippedRunes.Add(rune);
            }
        }

        return equippedRunes;
    }

    /// <summary>
    /// Get count of equipped runes
    /// </summary>
    public int GetEquippedRuneCount()
    {
        int count = 0;
        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (HasRuneEquipped(i))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Check if monster can equip a specific rune (not already equipped elsewhere)
    /// </summary>
    public bool CanEquipRune(RuneData rune)
    {
        if (!IsValidProceduralRune(rune))
        {
            return false;
        }

        // Check if rune is available in collection
        if (!RuneCollectionManager.Instance.ContainsRune(rune))
        {
            Debug.LogWarning($"⚠️ Rune not found in collection: {rune.runeName}");
            return false;
        }

        // Check if rune is already equipped on this monster
        for (int i = 0; i < equippedRuneIDs.Length; i++)
        {
            if (equippedRuneIDs[i] == rune.uniqueID)
            {
                Debug.LogWarning($"⚠️ Rune already equipped on this monster: {rune.runeName}");
                return false;
            }
        }

        // Check if rune is equipped on another monster
        if (RuneCollectionManager.Instance.IsRuneEquipped(rune))
        {
            Debug.LogWarning($"⚠️ Rune equipped on another monster: {rune.runeName}");
            return false;
        }

        return true;
    }

    // ========== SAVE/LOAD SYSTEM ==========

    /// <summary>
    /// Restore equipped runes using pure ID system
    /// </summary>
    private void RestoreEquippedRunes()
    {
        if (RuneCollectionManager.Instance == null)
        {
            Debug.LogWarning("⚠️ RuneCollectionManager.Instance is null during rune restoration!");
            return;
        }

        int runeCount = RuneCollectionManager.Instance.GetRuneCount();
        if (runeCount == 0)
        {
            Debug.LogWarning($"⚠️ No procedural runes available for restoration of {monsterData?.monsterName}");
            return;
        }

        Debug.Log($"🔄 Restoring equipped runes for {monsterData?.monsterName} (from {runeCount} available procedural runes)...");

        // Ensure arrays are properly initialized
        InitializeRuneIDs();
        InitializeRuneSlots();

        // 🔍 BEGIN NEW DEBUG CODE - Plaats hier!
        Debug.LogWarning($"🔍 DEBUGGING equippedRuneIDs for {monsterData?.monsterName}:");
        for (int j = 0; j < equippedRuneIDs.Length; j++)
        {
            Debug.LogWarning($"  Slot {j}: '{equippedRuneIDs[j]}' (IsNullOrEmpty: {string.IsNullOrEmpty(equippedRuneIDs[j])})");
        }
        // 🔍 END NEW DEBUG CODE

        // Restore using pure ID system
        for (int i = 0; i < equippedRuneIDs.Length && i < runeSlots.Length; i++)
        {
            if (!string.IsNullOrEmpty(equippedRuneIDs[i]))
            {
                Debug.Log($"🔍 Restoring slot {i}: ID {equippedRuneIDs[i]}");

                var rune = FindRuneByID(equippedRuneIDs[i]);

                // 🔍 BEGIN NEW DEBUG CODE - Plaats hier ook!
                if (rune != null)
                {
                    Debug.LogWarning($"🔍 FindRuneByID returned rune: '{rune.runeName}' (UniqueID: {rune.uniqueID})");
                    if (string.IsNullOrEmpty(rune.runeName))
                    {
                        Debug.LogError($"🚨 EMPTY RUNE DETECTED from FindRuneByID!");
                        Debug.LogError($"🚨 This should NOT be equipped!");
                        continue; // Skip equipping this empty rune
                    }
                }
                // 🔍 END NEW DEBUG CODE

                if (rune != null && runeSlots[i] != null)
                {
                    runeSlots[i].equippedRune = rune;
                    Debug.Log($"✅ Restored procedural rune '{rune.runeName}' to slot {i}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not restore procedural rune with ID '{equippedRuneIDs[i]}' to slot {i}");
                    equippedRuneIDs[i] = ""; // Clear invalid reference
                }
            }
        }


        Debug.Log($"🔄 Restoring equipped runes for {monsterData?.monsterName} (from {runeCount} available procedural runes)...");

        // Ensure arrays are properly initialized
        InitializeRuneIDs();
        InitializeRuneSlots();

        // Restore using pure ID system
        for (int i = 0; i < equippedRuneIDs.Length && i < runeSlots.Length; i++)
        {
            if (!string.IsNullOrEmpty(equippedRuneIDs[i]))
            {
                Debug.Log($"🔍 Restoring slot {i}: ID {equippedRuneIDs[i]}");

                var rune = FindRuneByID(equippedRuneIDs[i]);

                if (rune != null && runeSlots[i] != null)
                {
                    runeSlots[i].equippedRune = rune;
                    Debug.Log($"✅ Restored procedural rune '{rune.runeName}' to slot {i}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not restore procedural rune with ID '{equippedRuneIDs[i]}' to slot {i}");
                    equippedRuneIDs[i] = ""; // Clear invalid reference
                }
            }
        }

        // Update stats after restoring runes
        currentStats = CalculateCurrentStats();
    }
    public void RestoreAfterLoad()
    {
        // Load monster data first
        LoadMonsterData();

        // Start coroutine for delayed rune restoration
        if (Application.isPlaying)
        {
            MonoBehaviour monoBehaviour = MonsterCollectionManager.Instance;
            if (monoBehaviour != null)
            {
                monoBehaviour.StartCoroutine(DelayedRuneRestore());
            }
            else
            {
                // Fallback: direct restoration
                RestoreEquippedRunes();
            }
        }
        else
        {
            // In editor: direct restoration
            RestoreEquippedRunes();
        }
    }

    /// <summary>
    /// Delayed rune restoration to ensure RuneCollectionManager is fully loaded
    /// </summary>
    private IEnumerator DelayedRuneRestore()
    {
        // Wait one frame to ensure all managers are initialized
        yield return null;

        // Ensure RuneCollectionManager is loaded
        if (RuneCollectionManager.Instance != null)
        {
            // Force load if not already loaded
            if (RuneCollectionManager.Instance.GetRuneCount() == 0)
            {
                RuneCollectionManager.Instance.LoadRuneCollection();
            }
        }

        // Wait another frame for loading to complete
        yield return null;

        // Now restore equipped runes
        RestoreEquippedRunes();
    }

    /// <summary>
    /// Load monster data from Resources
    /// </summary>
    private void LoadMonsterData()
    {
        if (monsterData == null && !string.IsNullOrEmpty(monsterDataName))
        {
            Debug.Log($"🔍 Loading MonsterData: {monsterDataName}");

            monsterData = Resources.Load<MonsterData>($"Monsters/{monsterDataName}");

            if (monsterData != null)
            {
                Debug.Log($"✅ Loaded from Resources/Monsters/: {monsterDataName}");

                // Ensure base stats are set
                if (baseStats == null)
                {
                    if (monsterData != null)
                    {
                        baseStats = new MonsterStats
                        {
                            health = monsterData.baseHP,
                            attack = monsterData.baseATK,
                            defense = monsterData.baseDEF,
                            speed = monsterData.baseSPD,
                            energy = monsterData.baseEnergy,
                            criticalRate = monsterData.baseCriticalRate,
                            criticalDamage = monsterData.baseCriticalDamage,
                            accuracy = monsterData.baseAccuracy,
                            resistance = monsterData.baseResistance
                        };
                    }
                    else
                    {
                        baseStats = new MonsterStats { health = 100, attack = 20, defense = 15, speed = 10 };
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to load MonsterData: {monsterDataName}");

                // Create fallback stats
                baseStats = new MonsterStats { health = 100, attack = 20, defense = 15, speed = 10 };
            }
        }
    }

    // ========== STATS CALCULATION ==========

    /// <summary>
    /// Calculate current stats including level scaling and rune bonuses
    /// </summary>
    public MonsterStats CalculateCurrentStats()
    {
        // Start with base stats
        var stats = new MonsterStats();
        if (baseStats != null)
        {
            stats.health = baseStats.health;
            stats.attack = baseStats.attack;
            stats.defense = baseStats.defense;
            stats.speed = baseStats.speed;
        }

        // Apply level scaling
        ApplyLevelScaling(ref stats);

        // Apply procedural rune bonuses
        ApplyRuneBonuses(ref stats);

        return stats;
    }

    /// <summary>
    /// Apply level-based stat scaling
    /// </summary>
    private void ApplyLevelScaling(ref MonsterStats stats)
    {
        if (currentLevel <= 1) return;

        // Simple linear scaling (can be made more complex)
        float levelMultiplier = 1.0f + ((currentLevel - 1) * 0.1f); // 10% per level

        stats.health = Mathf.RoundToInt(stats.health * levelMultiplier);
        stats.attack = Mathf.RoundToInt(stats.attack * levelMultiplier);
        stats.defense = Mathf.RoundToInt(stats.defense * levelMultiplier);
        stats.speed = Mathf.RoundToInt(stats.speed * levelMultiplier);
    }

    /// <summary>
    /// Apply procedural rune bonuses to stats
    /// </summary>
    private void ApplyRuneBonuses(ref MonsterStats stats)
    {
        if (RuneCollectionManager.Instance == null)
        {
            return;
        }

        // Get rune bonuses from RuneCollectionManager
        var runeBonuses = RuneCollectionManager.Instance.CalculateMonsterRuneBonuses(this);

        // Add rune bonuses to stats
        stats.health += runeBonuses.health;
        stats.attack += runeBonuses.attack;
        stats.defense += runeBonuses.defense;
        stats.speed += runeBonuses.speed;

        Debug.Log($"🎯 Applied rune bonuses to {monsterData?.monsterName}: +{runeBonuses.health} HP, +{runeBonuses.attack} ATK, +{runeBonuses.defense} DEF, +{runeBonuses.speed} SPD");
    }

    /// <summary>
    /// Refresh stats (call after equipping/unequipping runes)
    /// </summary>
    public void RefreshStats()
    {
        currentStats = CalculateCurrentStats();
        Debug.Log($"🔄 Refreshed stats for {monsterData?.monsterName}: HP:{currentStats.health} ATK:{currentStats.attack} DEF:{currentStats.defense} SPD:{currentStats.speed}");
    }

    // ========== EXPERIENCE AND LEVELING ==========

    /// <summary>
    /// Add experience to the monster (ENHANCED DEBUG VERSION)
    /// </summary>
    public bool AddExperience(int expAmount)
    {
        Debug.Log($"🎯 === MONSTER ADD EXPERIENCE ===");
        Debug.Log($"🎯 Monster: {monsterData?.monsterName ?? "NULL MonsterData"}");
        Debug.Log($"🎯 XP to add: {expAmount}");
        Debug.Log($"🎯 Current level: {currentLevel}");
        Debug.Log($"🎯 Current XP: {currentExperience}");
        Debug.Log($"🎯 Required for next level: {GetExperienceRequiredForNextLevel()}");

        if (expAmount <= 0)
        {
            Debug.LogWarning($"🎯 Invalid XP amount: {expAmount}");
            return false;
        }

        int oldExperience = currentExperience;
        int oldLevel = currentLevel;

        currentExperience += expAmount;
        bool leveledUp = false;

        Debug.Log($"🎯 New current XP: {currentExperience}");

        // Check for level ups
        while (currentExperience >= GetExperienceRequiredForNextLevel() && currentLevel < GetMaxLevel())
        {
            int xpToDeduct = GetExperienceRequiredForNextLevel();
            currentExperience -= xpToDeduct;
            currentLevel++;
            leveledUp = true;

            Debug.Log($"🎯 LEVEL UP! New level: {currentLevel}, XP after level up: {currentExperience}");
            Debug.Log($"🎉 {monsterData?.monsterName} leveled up to level {currentLevel}!");
        }

        // Refresh stats if leveled up
        if (leveledUp)
        {
            currentStats = CalculateCurrentStats();
            Debug.Log($"🎯 Stats refreshed due to level up");
        }

        Debug.Log($"🎯 Final state: Level {currentLevel}, XP {currentExperience}/{GetExperienceRequiredForNextLevel()}");
        Debug.Log($"🎯 Summary: {oldLevel}→{currentLevel}, {oldExperience}→{currentExperience}, Leveled up: {leveledUp}");
        Debug.Log($"🎯 === END MONSTER XP ===");

        return leveledUp;
    }


    /// <summary>
    /// Get experience required for next level (USES MonsterData)
    /// </summary>
    public int GetExperienceRequiredForNextLevel()
    {
        if (monsterData == null)
        {
            Debug.LogWarning("⚠️ MonsterData is null, using fallback XP calculation");
            return currentLevel * 100; // Fallback
        }

        // ✅ CORRECT: Use MonsterData properties
        float baseXP = monsterData.baseExperienceRequired;
        float growthRate = monsterData.experienceGrowthRate;

        // Calculate XP requirement based on MonsterData
        int requiredXP = Mathf.RoundToInt(baseXP * Mathf.Pow(growthRate, currentLevel - 1));

        // Ensure minimum value
        return Mathf.Max(requiredXP, 50);
    }

    public float GetExperienceProgress()
    {
        if (currentLevel >= GetMaxLevel()) return 1.0f;

        int requiredExp = GetExperienceRequiredForNextLevel();
        return requiredExp > 0 ? (float)currentExperience / requiredExp : 0f;
    }

    /// <summary>
    /// Get maximum level for this monster (USES MonsterData)
    /// </summary>
    public int GetMaxLevel()
    {
        if (monsterData == null)
        {
            Debug.LogWarning("⚠️ MonsterData is null, using fallback max level");
            return 100; // Fallback
        }

        // ✅ CORRECT: Use MonsterData property
        return monsterData.maxLevel;
    }

    // ========== UTILITY METHODS ==========

    /// <summary>
    /// Get display name for the monster
    /// </summary>
    public string GetDisplayName()
    {
        return monsterData?.monsterName ?? "Unknown Monster";
    }

    /// <summary>
    /// Get power rating based on stats and level
    /// </summary>
    public int GetPowerRating()
    {
        if (currentStats == null) return 0;

        return currentStats.health + currentStats.attack + currentStats.defense + currentStats.speed;
    }

    /// <summary>
    /// Debug information about equipped procedural runes
    /// </summary>
    public void DebugEquippedRunes()
    {
        Debug.Log($"=== EQUIPPED PROCEDURAL RUNES FOR {GetDisplayName()} ===");

        for (int i = 0; i < runeSlots.Length; i++)
        {
            var rune = GetEquippedRune(i);
            string runeID = i < equippedRuneIDs.Length ? equippedRuneIDs[i] : "NULL";

            if (rune != null)
            {
                Debug.Log($"   Slot {i}: {rune.runeName} (ID: {runeID})");
                Debug.Log($"      Level: {rune.currentLevel}, Rarity: {rune.rarity}");
                if (rune.mainStat != null)
                {
                    Debug.Log($"      Main Stat: {rune.mainStat.GetDisplayText()}");
                }
            }
            else
            {
                Debug.Log($"   Slot {i}: EMPTY (Stored ID: {runeID})");
            }
        }

        Debug.Log($"Total Power: {GetPowerRating()}");
        Debug.Log($"Equipped Runes: {GetEquippedRuneCount()}/6");
    }

    [ContextMenu("Debug Monster Runes")]
    public void DebugAllEquippedRunes()
    {
        Debug.Log($"=== DEBUGGING RUNES FOR {monsterData?.monsterName} ===");

        for (int i = 0; i < 6; i++)
        {
            var rune = GetEquippedRune(i);
            if (rune != null)
            {
                Debug.LogWarning($"  Slot {i}: {rune.runeName}");
                Debug.LogWarning($"    UniqueID: {rune.uniqueID}");
                Debug.LogWarning($"    Main Stat: {rune.mainStat?.statType} = {rune.mainStat?.value ?? 0}");
                Debug.LogWarning($"    Sub Stats: {rune.subStats?.Count ?? 0}");
                Debug.LogWarning($"    Is Procedural: {rune.isProceduralGenerated}");
                Debug.LogWarning($"    Creation Time: {rune.creationTime}");

                if (rune.mainStat?.value == 0 || string.IsNullOrEmpty(rune.runeName))
                {
                    Debug.LogError($"    ❌ PROBLEMATIC RUNE FOUND!");
                }
            }
            else
            {
                Debug.Log($"  Slot {i}: Empty");
            }
        }
    }

    [ContextMenu("🧪 Test Add 200 XP")]
    public void TestAdd200XP()
    {
        Debug.Log($"🧪 Testing direct XP addition to {monsterData?.monsterName}");
        bool result = AddExperience(200);
        Debug.Log($"🧪 XP addition result: {result}");
    }

    [ContextMenu("🧪 Debug XP State")]
    public void DebugXPState()
    {
        Debug.Log($"🧪 === XP STATE DEBUG ===");
        Debug.Log($"🧪 Monster: {monsterData?.monsterName ?? "NULL MonsterData"}");
        Debug.Log($"🧪 UniqueID: {uniqueID}");
        Debug.Log($"🧪 Current Level: {currentLevel}");
        Debug.Log($"🧪 Current XP: {currentExperience}");
        Debug.Log($"🧪 Required for next level: {GetExperienceRequiredForNextLevel()}");
        Debug.Log($"🧪 Max level: {GetMaxLevel()}");
        Debug.Log($"🧪 XP Progress: {GetExperienceProgress():P1}");
        Debug.Log($"🧪 === END DEBUG ===");
    }


    /// <summary>
    /// Validate monster data integrity
    /// </summary>
    public bool ValidateIntegrity()
    {
        bool isValid = true;

        // Check unique ID
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
            Debug.LogWarning($"⚠️ Generated missing uniqueID for monster: {GetDisplayName()}");
        }

        // Check arrays
        if (equippedRuneIDs == null || equippedRuneIDs.Length != 6)
        {
            InitializeRuneIDs();
            isValid = false;
        }

        if (runeSlots == null || runeSlots.Length != 6)
        {
            InitializeRuneSlots();
            isValid = false;
        }

        // Check stats
        if (currentStats == null)
        {
            currentStats = CalculateCurrentStats();
            isValid = false;
        }

        // Validate equipped runes are actually procedural
        for (int i = 0; i < runeSlots.Length; i++)
        {
            var rune = runeSlots[i]?.equippedRune;
            if (rune != null && !rune.isProceduralGenerated)
            {
                Debug.LogError($"❌ Non-procedural rune found in slot {i}: {rune.runeName}");
                runeSlots[i].equippedRune = null;
                equippedRuneIDs[i] = "";
                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>
    /// Get experience required for the current level (level floor)
    /// </summary>
    public int GetExperienceRequiredForCurrentLevel()
    {
        if (currentLevel <= 1)
            return 0;

        // Calculate XP required for current level
        int xpForCurrentLevel = 0;
        for (int level = 1; level < currentLevel; level++)
        {
            xpForCurrentLevel += GetExperienceRequiredForLevel(level);
        }

        return xpForCurrentLevel;
    }

    /// <summary>
    /// Get experience required for a specific level
    /// </summary>
    private int GetExperienceRequiredForLevel(int level)
    {
        // Standard RPG XP formula: baseXP * level^1.5
        int baseXP = 100;
        return Mathf.RoundToInt(baseXP * Mathf.Pow(level, 1.5f));
    }


    /// <summary>
    /// Get summary of this monster for debugging
    /// </summary>
    public string GetSummary()
    {
        return $"{GetDisplayName()} Lv.{currentLevel} (Power: {GetPowerRating()}, Runes: {GetEquippedRuneCount()}/6)";
    }
}

// ========== SIMPLIFIED RUNE SLOT SYSTEM ==========

/// <summary>
/// Simplified rune slot for procedural runes only
/// </summary>
[System.Serializable]
public class RuneSlot
{
    public int slotIndex;
    public RuneData equippedRune;

    public bool IsEmpty => equippedRune == null;
    public bool IsOccupied => equippedRune != null;

    /// <summary>
    /// Check if a procedural rune can be equipped in this slot
    /// </summary>
    public bool CanEquipRune(RuneData rune)
    {
        if (rune == null) return false;

        // Only procedural runes allowed
        if (!rune.isProceduralGenerated)
        {
            Debug.LogError($"❌ Only procedural runes can be equipped! Rune: {rune.runeName}");
            return false;
        }

        // Must have valid unique ID
        if (string.IsNullOrEmpty(rune.uniqueID))
        {
            Debug.LogError($"❌ Procedural rune missing uniqueID: {rune.runeName}");
            return false;
        }

        return true;
    }
}
