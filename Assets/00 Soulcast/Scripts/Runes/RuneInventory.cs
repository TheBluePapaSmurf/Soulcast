using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RuneInventory : MonoBehaviour
{
    [Header("Rune Collection")]
    public List<RuneData> ownedRunes = new List<RuneData>();
    public int maxRuneCapacity = 200;

    [Header("Currency")]
    public int runeUpgradeCurrency = 5000; // Gold/gems for upgrading

    public static RuneInventory Instance { get; private set; }

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

    // Add rune to inventory
    public bool AddRune(RuneData rune)
    {
        if (ownedRunes.Count >= maxRuneCapacity)
        {
            Debug.LogWarning("Rune inventory is full!");
            return false;
        }

        ownedRunes.Add(rune);
        Debug.Log($"Added rune: {rune.runeName}");
        return true;
    }

    // Remove rune from inventory
    public bool RemoveRune(RuneData rune)
    {
        return ownedRunes.Remove(rune);
    }

    // Get runes by type
    public List<RuneData> GetRunesByType(RuneType runeType)
    {
        return ownedRunes.Where(r => r.runeType == runeType).ToList();
    }

    // Get runes by rarity
    public List<RuneData> GetRunesByRarity(RuneRarity rarity)
    {
        return ownedRunes.Where(r => r.rarity == rarity).ToList();
    }

    // Get unequipped runes
    public List<RuneData> GetUnequippedRunes()
    {
        // This would need to check against all monsters' equipped runes
        // For now, return all runes (implement equipped checking later)
        return new List<RuneData>(ownedRunes);
    }

    // Upgrade rune
    public bool UpgradeRune(RuneData rune)
    {
        if (rune.currentLevel >= rune.maxLevel)
        {
            Debug.LogWarning($"{rune.runeName} is already at max level!");
            return false;
        }

        int cost = rune.GetUpgradeCost(rune.currentLevel);
        if (runeUpgradeCurrency < cost)
        {
            Debug.LogWarning($"Not enough currency to upgrade {rune.runeName}! Need {cost}, have {runeUpgradeCurrency}");
            return false;
        }

        runeUpgradeCurrency -= cost;
        rune.currentLevel++;

        Debug.Log($"Upgraded {rune.runeName} to level {rune.currentLevel}!");
        return true;
    }

    // Currency management
    public void AddUpgradeCurrency(int amount)
    {
        runeUpgradeCurrency += amount;
    }

    public bool SpendUpgradeCurrency(int amount)
    {
        if (runeUpgradeCurrency >= amount)
        {
            runeUpgradeCurrency -= amount;
            return true;
        }
        return false;
    }

    // Get inventory stats
    public int GetRuneCount()
    {
        return ownedRunes.Count;
    }

    public int GetAvailableSpace()
    {
        return maxRuneCapacity - ownedRunes.Count;
    }
}
