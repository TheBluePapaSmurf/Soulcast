// Runes/RuneCollectionManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class RuneCollectionManager : MonoBehaviour
{
    [Header("Rune Collection")]
    [SerializeField] private List<RuneData> ownedRunes = new List<RuneData>();

    [Header("Settings")]
    public int maxRuneCapacity = 500;

    // Events
    public static event Action<RuneData> OnRuneAdded;
    public static event Action<RuneData> OnRuneRemoved;
    public static event Action<RuneData> OnRuneUpgraded;
    public static event Action<RuneData> OnRuneEquipped;
    public static event Action<RuneData> OnRuneUnequipped;


    public static RuneCollectionManager Instance { get; private set; }

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

    // ========== RUNE MANAGEMENT ==========

    public bool AddRune(RuneData rune)
    {
        if (rune == null) return false;

        if (ownedRunes.Count >= maxRuneCapacity)
        {
            Debug.LogWarning("❌ Rune inventory is full!");
            return false;
        }

        ownedRunes.Add(rune);
        OnRuneAdded?.Invoke(rune);

        Debug.Log($"💎 Added rune: {rune.runeName}");
        SaveManager.Instance?.AutoSave();
        return true;
    }

    public void RemoveRune(RuneData rune)
    {
        if (ownedRunes.Contains(rune))
        {
            ownedRunes.Remove(rune);
            OnRuneRemoved?.Invoke(rune);

            Debug.Log($"🗑️ Removed {rune.runeName} from inventory");
            SaveManager.Instance?.AutoSave();
        }
    }

    public List<RuneData> GetAllRunes()
    {
        return new List<RuneData>(ownedRunes);
    }

    public List<RuneData> GetRunesByType(RuneType runeType)
    {
        return ownedRunes.Where(r => r.runeType == runeType).ToList();
    }

    public List<RuneData> GetRunesByRarity(RuneRarity rarity)
    {
        return ownedRunes.Where(r => r.rarity == rarity).ToList();
    }

    public bool IsRuneAvailable(RuneData rune)
    {
        return ownedRunes.Contains(rune);
    }

    // ========== EQUIPPED RUNES ==========

    public List<RuneData> GetUnequippedRunes()
    {
        var equippedRunes = GetAllEquippedRunes();
        return ownedRunes.Where(r => !equippedRunes.Contains(r)).ToList();
    }

    public List<RuneData> GetUnequippedRunesByType(RuneType runeType)
    {
        return GetUnequippedRunes().Where(r => r.runeType == runeType).ToList();
    }

    private HashSet<RuneData> GetAllEquippedRunes()
    {
        var equippedRunes = new HashSet<RuneData>();

        if (MonsterCollectionManager.Instance != null)
        {
            foreach (var monster in MonsterCollectionManager.Instance.GetAllMonsters())
            {
                foreach (var slot in monster.runeSlots)
                {
                    if (slot?.equippedRune != null)
                    {
                        equippedRunes.Add(slot.equippedRune);
                    }
                }
            }
        }

        return equippedRunes;
    }

    public bool IsRuneEquipped(RuneData rune)
    {
        return GetAllEquippedRunes().Contains(rune);
    }

    // ========== RUNE UPGRADE SYSTEM ==========

    public bool UpgradeRune(RuneData rune)
    {
        if (!ownedRunes.Contains(rune))
        {
            Debug.LogWarning("❌ Cannot upgrade rune: not owned by player!");
            return false;
        }

        if (rune.currentLevel >= rune.maxLevel)
        {
            Debug.LogWarning($"❌ {rune.runeName} is already at max level!");
            return false;
        }

        int cost = rune.GetUpgradeCost(rune.currentLevel);
        if (!CurrencyManager.Instance.CanAffordSoulCoins(cost))
        {
            Debug.LogWarning($"❌ Not enough Soul Coins to upgrade {rune.runeName}! Need {cost}");
            return false;
        }

        CurrencyManager.Instance.SpendSoulCoins(cost);
        rune.currentLevel++;
        OnRuneUpgraded?.Invoke(rune);

        Debug.Log($"⬆️ Upgraded {rune.runeName} to level {rune.currentLevel}!");
        SaveManager.Instance?.AutoSave();
        return true;
    }

    // ========== RUNE SELLING SYSTEM ==========

    public bool SellRune(RuneData rune, out int sellPrice)
    {
        sellPrice = 0;

        if (!ownedRunes.Contains(rune))
        {
            Debug.LogWarning("❌ Cannot sell rune: not owned by player!");
            return false;
        }

        if (IsRuneEquipped(rune))
        {
            Debug.LogWarning("❌ Cannot sell equipped rune! Unequip first.");
            return false;
        }

        sellPrice = CalculateRuneSellPrice(rune);
        RemoveRune(rune);
        CurrencyManager.Instance.AddSoulCoins(sellPrice);

        Debug.Log($"💰 Sold {rune.runeName} for {sellPrice} Soul Coins!");
        return true;
    }

    private int CalculateRuneSellPrice(RuneData rune)
    {
        int basePrice = GetBaseSellPriceByRarity(rune.rarity);

        // Level bonus (50% of upgrade costs)
        int levelBonus = 0;
        for (int i = 0; i < rune.currentLevel; i++)
        {
            levelBonus += Mathf.RoundToInt(rune.GetUpgradeCost(i) * 0.5f);
        }

        return basePrice + levelBonus;
    }

    private int GetBaseSellPriceByRarity(RuneRarity rarity)
    {
        return rarity switch
        {
            RuneRarity.Common => 50,
            RuneRarity.Uncommon => 150,
            RuneRarity.Rare => 400,
            RuneRarity.Epic => 800,
            RuneRarity.Legendary => 1500,
            _ => 50
        };
    }

    // ========== SAVE/LOAD ==========

    public void SaveRuneCollection()
    {
        ES3.Save("OwnedRunes", ownedRunes, SaveManager.SAVE_FILE);
        ES3.Save("MaxRuneCapacity", maxRuneCapacity, SaveManager.SAVE_FILE);
        Debug.Log($"💎 Saved {ownedRunes.Count} runes");
    }

    public void LoadRuneCollection()
    {
        ownedRunes = ES3.Load("OwnedRunes", SaveManager.SAVE_FILE, new List<RuneData>());
        maxRuneCapacity = ES3.Load("MaxRuneCapacity", SaveManager.SAVE_FILE, 500);
        Debug.Log($"💎 Loaded {ownedRunes.Count} runes");
    }

    // ========== UTILITY ==========

    public int GetRuneCount() => ownedRunes.Count;
    public int GetAvailableRuneSpace() => maxRuneCapacity - ownedRunes.Count;

    public List<RuneData> GetRunesByLevel(int minLevel, int maxLevel)
    {
        return ownedRunes.Where(r => r.currentLevel >= minLevel && r.currentLevel <= maxLevel).ToList();
    }

    // ========== STAT CALCULATION SYSTEM ==========

    /// <summary>
    /// Calculate stat bonuses from a specific rune at its current level
    /// </summary>
    public MonsterStats CalculateRuneStatBonus(RuneData rune)
    {
        MonsterStats statBonus = new MonsterStats();

        if (rune == null) return statBonus;

        // Apply main stat
        if (rune.mainStat != null)
        {
            ApplyRuneStatToMonsterStats(rune.mainStat, ref statBonus, rune.currentLevel);
        }

        // Apply sub stats
        foreach (var subStat in rune.subStats)
        {
            if (subStat != null)
            {
                ApplyRuneStatToMonsterStats(subStat, ref statBonus, rune.currentLevel);
            }
        }

        return statBonus;
    }

    private void ApplyRuneStatToMonsterStats(RuneStat runeStat, ref MonsterStats stats, int runeLevel)
    {
        float levelMultiplier = 1f + (runeLevel * 0.1f); // +10% per level
        float statValue = runeStat.value * levelMultiplier;

        switch (runeStat.statType)
        {
            case RuneStatType.HP:
                stats.health += Mathf.RoundToInt(statValue);
                break;
            case RuneStatType.ATK:
                stats.attack += Mathf.RoundToInt(statValue);
                break;
            case RuneStatType.DEF:
                stats.defense += Mathf.RoundToInt(statValue);
                break;
            case RuneStatType.SPD:
                stats.speed += Mathf.RoundToInt(statValue);
                break;
            case RuneStatType.CriticalRate:
                stats.criticalRate += statValue;
                break;
            case RuneStatType.CriticalDamage:
                stats.criticalDamage += statValue;
                break;
            case RuneStatType.Accuracy:
                stats.accuracy += statValue;
                break;
            case RuneStatType.Resistance:
                stats.resistance += statValue;
                break;
        }
    }

    /// <summary>
    /// Calculate total stat bonus from all equipped runes on a monster
    /// </summary>
    public MonsterStats CalculateMonsterRuneBonuses(CollectedMonster monster)
    {
        MonsterStats totalBonus = new MonsterStats();

        if (monster?.equippedRuneNames == null) return totalBonus;

        // Add individual rune bonuses
        foreach (var runeName in monster.equippedRuneNames)
        {
            if (!string.IsNullOrEmpty(runeName))
            {
                var rune = ownedRunes.FirstOrDefault(r => r.name == runeName);
                if (rune != null)
                {
                    var runeBonus = CalculateRuneStatBonus(rune);
                    AddStats(ref totalBonus, runeBonus);
                }
            }
        }

        // Add set bonuses  
        var setBonuses = CalculateSetBonuses(monster);
        AddStats(ref totalBonus, setBonuses);

        return totalBonus;
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

    // ========== SET BONUS SYSTEM ==========

    public MonsterStats CalculateSetBonuses(CollectedMonster monster)
    {
        MonsterStats setBonuses = new MonsterStats();

        if (monster?.equippedRuneNames == null) return setBonuses;

        // Count rune types
        var runeCounts = new Dictionary<RuneType, int>();

        foreach (var runeName in monster.equippedRuneNames)
        {
            if (!string.IsNullOrEmpty(runeName))
            {
                var rune = ownedRunes.FirstOrDefault(r => r.name == runeName);
                if (rune != null)
                {
                    if (runeCounts.ContainsKey(rune.runeType))
                        runeCounts[rune.runeType]++;
                    else
                        runeCounts[rune.runeType] = 1;
                }
            }
        }

        // Apply set bonuses
        foreach (var kvp in runeCounts)
        {
            var runeType = kvp.Key;
            var count = kvp.Value;

            if (count >= 2)
            {
                ApplyTwoPieceSetBonus(runeType, ref setBonuses, monster);
            }

            if (count >= 4)
            {
                ApplyFourPieceSetBonus(runeType, ref setBonuses, monster);
            }
        }

        return setBonuses;
    }

    private void ApplyTwoPieceSetBonus(RuneType runeType, ref MonsterStats stats, CollectedMonster monster)
    {
        switch (runeType)
        {
            case RuneType.Blade:
                stats.attack += Mathf.RoundToInt(stats.attack * 0.12f); // +12% ATK
                break;
            case RuneType.Fatal:
                stats.criticalDamage += 20f; // +20% Crit Damage
                break;
            case RuneType.Rage:
                stats.criticalRate += 12f; // +12% Crit Rate
                break;
            case RuneType.Energy:
                stats.speed += Mathf.RoundToInt(stats.speed * 0.15f); // +15% SPD
                break;
            case RuneType.Guard:
                stats.health += Mathf.RoundToInt(stats.health * 0.15f); // +15% HP
                break;
            case RuneType.Swift:
                stats.speed += 25; // +25 SPD flat
                break;
        }
    }

    private void ApplyFourPieceSetBonus(RuneType runeType, ref MonsterStats stats, CollectedMonster monster)
    {
        switch (runeType)
        {
            case RuneType.Blade:
                stats.criticalRate += 20f; // +20% Crit Rate
                break;
            case RuneType.Fatal:
                stats.attack += Mathf.RoundToInt(stats.attack * 0.35f); // +35% ATK
                break;
            case RuneType.Rage:
                stats.criticalDamage += 40f; // +40% Crit Damage
                break;
            case RuneType.Energy:
                stats.energy += Mathf.RoundToInt(stats.energy * 0.25f); // +25% Energy
                break;
            case RuneType.Guard:
                stats.defense += Mathf.RoundToInt(stats.defense * 0.25f); // +25% DEF
                break;
            case RuneType.Swift:
                stats.speed += Mathf.RoundToInt(stats.speed * 0.25f); // +25% SPD
                break;
        }
    }

    // ========== RUNE EQUIPPING SYSTEM ==========

    /// <summary>
    /// Equipment tracking and event triggering for UI updates
    /// </summary>
    public void NotifyRuneEquipped(RuneData rune, CollectedMonster monster)
    {
        if (rune != null)
        {
            OnRuneEquipped?.Invoke(rune);
            Debug.Log($"🔗 {rune.runeName} equipped to {monster?.monsterData?.monsterName ?? "monster"}");
        }
    }

    /// <summary>
    /// Unequipment tracking and event triggering for UI updates
    /// </summary>
    public void NotifyRuneUnequipped(RuneData rune, CollectedMonster monster)
    {
        if (rune != null)
        {
            OnRuneUnequipped?.Invoke(rune);
            Debug.Log($"🔓 {rune.runeName} unequipped from {monster?.monsterData?.monsterName ?? "monster"}");
        }
    }

    /// <summary>
    /// Helper method to equip rune and trigger events
    /// </summary>
    public bool EquipRuneToMonster(CollectedMonster monster, int slotIndex, RuneData rune)
    {
        if (monster == null || rune == null) return false;

        // Check if rune is available
        if (!IsRuneAvailable(rune))
        {
            Debug.LogWarning($"❌ Cannot equip {rune.runeName}: not owned by player!");
            return false;
        }

        // Check if rune is already equipped elsewhere
        if (IsRuneEquipped(rune))
        {
            Debug.LogWarning($"❌ Cannot equip {rune.runeName}: already equipped on another monster!");
            return false;
        }

        // Unequip current rune in slot if any
        if (!string.IsNullOrEmpty(monster.equippedRuneNames[slotIndex]))
        {
            var currentRune = ownedRunes.FirstOrDefault(r => r.name == monster.equippedRuneNames[slotIndex]);
            if (currentRune != null)
            {
                NotifyRuneUnequipped(currentRune, monster);
            }
        }

        // Equip new rune
        monster.equippedRuneNames[slotIndex] = rune.name;
        if (monster.runeSlots != null && slotIndex < monster.runeSlots.Length)
        {
            monster.runeSlots[slotIndex].equippedRune = rune;
        }

        // Trigger event
        NotifyRuneEquipped(rune, monster);

        // Auto-save
        SaveManager.Instance?.AutoSave();

        return true;
    }

    /// <summary>
    /// Helper method to unequip rune and trigger events
    /// </summary>
    public RuneData UnequipRuneFromMonster(CollectedMonster monster, int slotIndex)
    {
        if (monster == null || slotIndex < 0 || slotIndex >= monster.equippedRuneNames.Length)
            return null;

        string runeName = monster.equippedRuneNames[slotIndex];
        if (string.IsNullOrEmpty(runeName)) return null;

        var rune = ownedRunes.FirstOrDefault(r => r.name == runeName);
        if (rune == null) return null;

        // Unequip rune
        monster.equippedRuneNames[slotIndex] = "";
        if (monster.runeSlots != null && slotIndex < monster.runeSlots.Length)
        {
            monster.runeSlots[slotIndex].equippedRune = null;
        }

        // Trigger event
        NotifyRuneUnequipped(rune, monster);

        // Auto-save
        SaveManager.Instance?.AutoSave();

        return rune;
    }

    /// <summary>
    /// Get display info for a rune showing its current stats
    /// </summary>
    public string GetRuneStatsDisplay(RuneData rune)
    {
        if (rune == null) return "";

        var stats = CalculateRuneStatBonus(rune);

        List<string> statLines = new List<string>();

        if (stats.health > 0) statLines.Add($"+{stats.health} HP");
        if (stats.attack > 0) statLines.Add($"+{stats.attack} ATK");
        if (stats.defense > 0) statLines.Add($"+{stats.defense} DEF");
        if (stats.speed > 0) statLines.Add($"+{stats.speed} SPD");
        if (stats.criticalRate > 0) statLines.Add($"+{stats.criticalRate:F1}% CRIT Rate");
        if (stats.criticalDamage > 0) statLines.Add($"+{stats.criticalDamage:F1}% CRIT DMG");
        if (stats.accuracy > 0) statLines.Add($"+{stats.accuracy:F1}% Accuracy");
        if (stats.resistance > 0) statLines.Add($"+{stats.resistance:F1}% Resistance");

        return string.Join("\n", statLines);
    }

}
