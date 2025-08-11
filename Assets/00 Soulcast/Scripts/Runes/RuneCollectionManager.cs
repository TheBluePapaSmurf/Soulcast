using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Manages a collection of procedural runes using pure ID-based system
/// NO legacy ScriptableObject support - procedural runes only
/// </summary>
public class RuneCollectionManager : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private List<RuneData> ownedRunes = new List<RuneData>();
    public int maxRuneCapacity = 500;

    [Header("Procedural Generation Settings")]
    [Range(0f, 1f)] public float commonDropChance = 0.50f;
    [Range(0f, 1f)] public float uncommonDropChance = 0.30f;
    [Range(0f, 1f)] public float rareDropChance = 0.15f;
    [Range(0f, 1f)] public float epicDropChance = 0.04f;
    [Range(0f, 1f)] public float legendaryDropChance = 0.01f;

    [Header("Stat Generation Ranges")]
    [SerializeField] private StatGenerationRange[] statRanges;

    [Header("Rune Set Data References")]
    [SerializeField] private List<RuneSetData> availableRuneSets = new List<RuneSetData>();

    [Header("Events")]
    public static System.Action<RuneData> OnRuneEquipped;
    public static System.Action<RuneData> OnRuneUnequipped;

    // Events
    public static event Action<RuneData> OnRuneAdded;
    public static event Action<RuneData> OnRuneRemoved;
    public static event Action<RuneData> OnRuneUpgraded;
    public static event Action OnRuneCollectionChanged;

    // Singleton
    public static RuneCollectionManager Instance { get; private set; }
    public static System.Action OnReturnToHUB;
    public bool HasPendingUIRefresh { get; private set; } = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStatRanges();
            LoadAvailableRuneSets();
            Debug.Log("💎 RuneCollectionManager initialized for procedural runes only");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Don't auto-load here - SaveManager handles loading order
        Debug.Log($"💎 RuneCollectionManager started (will be loaded by SaveManager)");
    }

    public void ForceRefreshUI()
    {
        HasPendingUIRefresh = false;
        OnRuneCollectionChanged?.Invoke();
        Debug.Log("🔄 Forced UI refresh for rune collection");
    }

    public void MarkUIForRefresh()
    {
        HasPendingUIRefresh = true;
    }

    // ========== INITIALIZATION ==========

    private void InitializeStatRanges()
    {
        if (statRanges == null || statRanges.Length == 0)
        {
            statRanges = new StatGenerationRange[]
            {
                new StatGenerationRange { statType = RuneStatType.HP, minValue = 100, maxValue = 500, canBePercentage = true },
                new StatGenerationRange { statType = RuneStatType.ATK, minValue = 20, maxValue = 100, canBePercentage = true },
                new StatGenerationRange { statType = RuneStatType.DEF, minValue = 15, maxValue = 80, canBePercentage = true },
                new StatGenerationRange { statType = RuneStatType.SPD, minValue = 5, maxValue = 40, canBePercentage = false },
                new StatGenerationRange { statType = RuneStatType.CriticalRate, minValue = 2, maxValue = 15, canBePercentage = true },
                new StatGenerationRange { statType = RuneStatType.CriticalDamage, minValue = 10, maxValue = 50, canBePercentage = true },
                new StatGenerationRange { statType = RuneStatType.Accuracy, minValue = 5, maxValue = 25, canBePercentage = true },
                new StatGenerationRange { statType = RuneStatType.Resistance, minValue = 5, maxValue = 25, canBePercentage = true }
            };
        }
    }

    private void LoadAvailableRuneSets()
    {
        if (availableRuneSets.Count == 0)
        {
            try
            {
                // Load all RuneSetData from Resources
                var runeSets = Resources.LoadAll<RuneSetData>("Runes/Sets");
                availableRuneSets.AddRange(runeSets);

                Debug.Log($"📦 Loaded {availableRuneSets.Count} rune sets from Resources");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to load rune sets: {e.Message}");
            }
        }
    }

    // ========== PROCEDURAL GENERATION ==========

    /// <summary>
    /// Generate a completely random procedural rune
    /// </summary>
    public RuneData GenerateRandomRune()
    {
        RuneRarity rarity = GenerateRandomRarity();
        RuneType type = GenerateRandomRuneType();
        RuneSlotPosition position = GenerateRandomSlotPosition();

        return GenerateProceduralRune(type, position, rarity);
    }

    /// <summary>
    /// Generate a procedural rune with specific parameters
    /// </summary>
    public RuneData GenerateProceduralRune(RuneType runeType, RuneSlotPosition slotPosition, RuneRarity rarity)
    {
        // Create base rune
        string runeName = GenerateRuneName(runeType, rarity);
        var rune = new RuneData(runeType, slotPosition, rarity, runeName);

        // Generate main stat
        rune.mainStat = GenerateMainStat(slotPosition, rarity);

        // Generate sub stats
        rune.subStats = GenerateSubStats(rarity, rune.mainStat);

        // Set rune set reference
        rune.runeSet = GetRuneSetByType(runeType);

        Debug.Log($"🎲 Generated {rarity} {runeType} procedural rune: {runeName} (ID: {rune.uniqueID})");
        Debug.Log($"   Main: {rune.mainStat.GetDisplayText()}");
        Debug.Log($"   Subs: {rune.subStats.Count} stats");

        return rune;
    }

    /// <summary>
    /// Generate multiple procedural runes at once
    /// </summary>
    public List<RuneData> GenerateMultipleRunes(int count)
    {
        var runes = new List<RuneData>();

        for (int i = 0; i < count; i++)
        {
            runes.Add(GenerateRandomRune());
        }

        Debug.Log($"🎲 Generated {count} procedural runes");
        return runes;
    }

    /// <summary>
    /// Generate a reward rune based on player level/progression
    /// </summary>
    public RuneData GenerateRewardRune(int playerLevel = 1)
    {
        // Adjust rarity chances based on player level
        float legendaryChance = Mathf.Min(0.05f, 0.001f + (playerLevel * 0.002f));
        float epicChance = Mathf.Min(0.15f, 0.02f + (playerLevel * 0.005f));
        float rareChance = Mathf.Min(0.35f, 0.10f + (playerLevel * 0.01f));

        float roll = UnityEngine.Random.value;
        RuneRarity rarity;

        if (roll < legendaryChance) rarity = RuneRarity.Legendary;
        else if (roll < legendaryChance + epicChance) rarity = RuneRarity.Epic;
        else if (roll < legendaryChance + epicChance + rareChance) rarity = RuneRarity.Rare;
        else if (roll < 0.7f) rarity = RuneRarity.Uncommon;
        else rarity = RuneRarity.Common;

        return GenerateProceduralRune(GenerateRandomRuneType(), GenerateRandomSlotPosition(), rarity);
    }

    // ========== RARITY GENERATION ==========

    private RuneRarity GenerateRandomRarity()
    {
        float roll = UnityEngine.Random.Range(0f, 1f);
        float cumulative = 0f;

        // Check from highest to lowest rarity
        cumulative += legendaryDropChance;
        if (roll <= cumulative) return RuneRarity.Legendary;

        cumulative += epicDropChance;
        if (roll <= cumulative) return RuneRarity.Epic;

        cumulative += rareDropChance;
        if (roll <= cumulative) return RuneRarity.Rare;

        cumulative += uncommonDropChance;
        if (roll <= cumulative) return RuneRarity.Uncommon;

        return RuneRarity.Common;
    }

    private RuneType GenerateRandomRuneType()
    {
        var types = Enum.GetValues(typeof(RuneType)).Cast<RuneType>().ToArray();
        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    private RuneSlotPosition GenerateRandomSlotPosition()
    {
        var positions = Enum.GetValues(typeof(RuneSlotPosition)).Cast<RuneSlotPosition>().ToArray();
        return positions[UnityEngine.Random.Range(0, positions.Length)];
    }

    // ========== STAT GENERATION ==========

    private RuneStat GenerateMainStat(RuneSlotPosition slotPosition, RuneRarity rarity)
    {
        // Main stat type depends on slot position
        RuneStatType mainStatType = GetMainStatTypeForSlot(slotPosition);

        var statRange = GetStatRange(mainStatType);
        if (statRange == null)
        {
            Debug.LogError($"❌ No stat range found for {mainStatType}");
            return new RuneStat { statType = mainStatType, value = 10f, isPercentage = false };
        }

        float baseValue = GenerateStatValue(statRange, rarity, true);
        bool isPercentage = ShouldBePercentage(mainStatType, true);

        return new RuneStat
        {
            statType = mainStatType,
            value = baseValue,
            isPercentage = isPercentage
        };
    }

    private List<RuneStat> GenerateSubStats(RuneRarity rarity, RuneStat mainStat)
    {
        var subStats = new List<RuneStat>();
        int subStatCount = GetSubStatCount(rarity);

        // Get available sub stat types (excluding main stat type)
        var availableStatTypes = Enum.GetValues(typeof(RuneStatType))
            .Cast<RuneStatType>()
            .Where(t => t != mainStat.statType)
            .ToList();

        // Randomly select sub stat types
        var selectedTypes = availableStatTypes
            .OrderBy(x => UnityEngine.Random.value)
            .Take(subStatCount)
            .ToList();

        foreach (var statType in selectedTypes)
        {
            var statRange = GetStatRange(statType);
            if (statRange != null)
            {
                float value = GenerateStatValue(statRange, rarity, false);
                bool isPercentage = ShouldBePercentage(statType, false);

                subStats.Add(new RuneStat
                {
                    statType = statType,
                    value = value,
                    isPercentage = isPercentage
                });
            }
        }

        return subStats;
    }

    private RuneStatType GetMainStatTypeForSlot(RuneSlotPosition slotPosition)
    {
        // Fixed main stats per slot (classic rune system)
        switch (slotPosition)
        {
            case RuneSlotPosition.Slot1: return RuneStatType.ATK;
            case RuneSlotPosition.Slot2: return RuneStatType.DEF;
            case RuneSlotPosition.Slot3: return RuneStatType.HP;
            case RuneSlotPosition.Slot4: return RuneStatType.CriticalRate;
            case RuneSlotPosition.Slot5: return RuneStatType.CriticalDamage;
            case RuneSlotPosition.Slot6: return RuneStatType.SPD;
            default: return RuneStatType.ATK;
        }
    }

    private int GetSubStatCount(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return UnityEngine.Random.Range(1, 3); // 1-2 substats
            case RuneRarity.Uncommon: return UnityEngine.Random.Range(2, 4); // 2-3 substats
            case RuneRarity.Rare: return UnityEngine.Random.Range(3, 5); // 3-4 substats
            case RuneRarity.Epic: return 4; // Always 4 substats
            case RuneRarity.Legendary: return 4; // Always 4 substats + higher values
            default: return 2;
        }
    }

    private float GenerateStatValue(StatGenerationRange statRange, RuneRarity rarity, bool isMainStat)
    {
        float baseMin = statRange.minValue;
        float baseMax = statRange.maxValue;

        // Rarity multipliers
        float rarityMultiplier = GetRarityStatMultiplier(rarity);

        // Main stats get higher values than sub stats
        float roleMultiplier = isMainStat ? 1.5f : 1.0f;

        float minValue = baseMin * rarityMultiplier * roleMultiplier;
        float maxValue = baseMax * rarityMultiplier * roleMultiplier;

        float rawValue = UnityEngine.Random.Range(minValue, maxValue);

        // Round to appropriate decimal places
        return Mathf.Round(rawValue * 10f) / 10f;
    }

    private float GetRarityStatMultiplier(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 1.0f;
            case RuneRarity.Uncommon: return 1.4f;
            case RuneRarity.Rare: return 1.8f;
            case RuneRarity.Epic: return 2.4f;
            case RuneRarity.Legendary: return 3.2f;
            default: return 1.0f;
        }
    }

    private bool ShouldBePercentage(RuneStatType statType, bool isMainStat)
    {
        var statRange = GetStatRange(statType);
        if (statRange == null || !statRange.canBePercentage) return false;

        // Main stats more likely to be percentage
        float percentageChance = isMainStat ? 0.6f : 0.3f;

        // Some stats are more likely to be percentage
        switch (statType)
        {
            case RuneStatType.CriticalRate:
            case RuneStatType.CriticalDamage:
            case RuneStatType.Accuracy:
            case RuneStatType.Resistance:
                percentageChance += 0.2f;
                break;
        }

        return UnityEngine.Random.value < percentageChance;
    }

    // ========== HELPER METHODS ==========

    private StatGenerationRange GetStatRange(RuneStatType statType)
    {
        return statRanges?.FirstOrDefault(r => r.statType == statType);
    }

    private RuneSetData GetRuneSetByType(RuneType runeType)
    {
        return availableRuneSets?.FirstOrDefault(set => set.name.Contains(runeType.ToString()));
    }

    private string GenerateRuneName(RuneType runeType, RuneRarity rarity)
    {
        // Simple naming for procedural runes based on rarity and type
        return $"{rarity} {runeType} Rune ({GenerateRuneTier()})";
    }

    private string GenerateRuneTier()
    {
        string[] tiers = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        return tiers[UnityEngine.Random.Range(0, tiers.Length)];
    }

    // ========== PURE ID-BASED COLLECTION MANAGEMENT ==========

    /// <summary>
    /// Add a procedural rune to the collection
    /// </summary>
    public bool AddRune(RuneData rune)
    {
        if (!ValidateProceduralRune(rune))
        {
            return false;
        }

        if (ownedRunes.Count >= maxRuneCapacity)
        {
            Debug.LogWarning($"⚠️ Rune collection is full! ({maxRuneCapacity}/{maxRuneCapacity})");
            return false;
        }

        // Ensure unique ID is set
        if (string.IsNullOrEmpty(rune.uniqueID))
        {
            rune.uniqueID = System.Guid.NewGuid().ToString();
            Debug.Log($"🔧 Generated missing uniqueID for rune: {rune.runeName}");
        }

        // Check for duplicate IDs
        if (ownedRunes.Any(r => r.uniqueID == rune.uniqueID))
        {
            Debug.LogWarning($"⚠️ Rune with ID {rune.uniqueID} already exists! Generating new ID.");
            rune.uniqueID = System.Guid.NewGuid().ToString();
        }

        ownedRunes.Add(rune);

        // Auto-save
        SaveRuneCollection();

        // Fire events
        OnRuneAdded?.Invoke(rune);
        OnRuneCollectionChanged?.Invoke();

        Debug.Log($"💎 Added procedural rune: {rune.runeName} (ID: {rune.uniqueID}) - Collection: {ownedRunes.Count}/{maxRuneCapacity}");
        return true;
    }

    /// <summary>
    /// Remove a procedural rune by ID
    /// </summary>
    public bool RemoveRune(RuneData rune)
    {
        if (rune == null || string.IsNullOrEmpty(rune.uniqueID))
        {
            Debug.LogWarning("⚠️ Cannot remove invalid rune");
            return false;
        }

        // Check if rune is equipped before removing
        if (IsRuneEquipped(rune))
        {
            Debug.LogWarning($"⚠️ Cannot remove equipped rune: {rune.runeName}");
            return false;
        }

        bool removed = ownedRunes.RemoveAll(r => r.uniqueID == rune.uniqueID) > 0;

        if (removed)
        {
            SaveRuneCollection();
            OnRuneRemoved?.Invoke(rune);
            OnRuneCollectionChanged?.Invoke();

            Debug.Log($"🗑️ Removed procedural rune: {rune.runeName} (ID: {rune.uniqueID})");
        }

        return removed;
    }

    /// <summary>
    /// Remove rune by unique ID only
    /// </summary>
    public bool RemoveRuneByID(string uniqueID)
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogWarning("⚠️ Cannot remove rune with empty ID");
            return false;
        }

        var rune = FindRuneByID(uniqueID);
        return rune != null && RemoveRune(rune);
    }

    /// <summary>
    /// Find procedural rune by unique ID only
    /// </summary>
    public RuneData FindRuneByID(string uniqueID)
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            return null;
        }

        var rune = ownedRunes.FirstOrDefault(r => r != null && r.uniqueID == uniqueID);

        if (rune != null)
        {
            Debug.Log($"✅ Found procedural rune by ID: {rune.runeName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No procedural rune found with ID: {uniqueID}");
        }

        return rune;
    }

    /// <summary>
    /// Validate that a rune is a valid procedural rune
    /// </summary>
    private bool ValidateProceduralRune(RuneData rune)
    {
        if (rune == null)
        {
            Debug.LogWarning("⚠️ Cannot add null rune");
            return false;
        }

        if (!rune.isProceduralGenerated)
        {
            Debug.LogError($"❌ Only procedural runes are supported! Attempted to add: {rune.runeName}");
            return false;
        }

        if (string.IsNullOrEmpty(rune.runeName))
        {
            Debug.LogError($"❌ Procedural rune missing name");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get all procedural runes
    /// </summary>
    public List<RuneData> GetAllRunes()
    {
        return new List<RuneData>(ownedRunes);
    }

    /// <summary>
    /// Get procedural runes by type
    /// </summary>
    public List<RuneData> GetRunesByType(RuneType runeType)
    {
        return ownedRunes.Where(r => r.runeType == runeType).ToList();
    }

    /// <summary>
    /// Get procedural runes by rarity
    /// </summary>
    public List<RuneData> GetRunesByRarity(RuneRarity rarity)
    {
        return ownedRunes.Where(r => r.rarity == rarity).ToList();
    }

    /// <summary>
    /// Get procedural runes by slot position
    /// </summary>
    public List<RuneData> GetRunesBySlot(RuneSlotPosition slotPosition)
    {
        return ownedRunes.Where(r => r.runeSlotPosition == slotPosition).ToList();
    }

    /// <summary>
    /// Get unequipped procedural runes
    /// </summary>
    public List<RuneData> GetUnequippedRunes()
    {
        // Check which runes are equipped on monsters
        var equippedRuneIDs = new HashSet<string>();

        if (MonsterCollectionManager.Instance != null)
        {
            var monsters = MonsterCollectionManager.Instance.GetAllMonsters();
            foreach (var monster in monsters)
            {
                for (int i = 0; i < 6; i++) // 6 rune slots
                {
                    var equippedRune = monster.GetEquippedRune(i);
                    if (equippedRune != null && !string.IsNullOrEmpty(equippedRune.uniqueID))
                    {
                        equippedRuneIDs.Add(equippedRune.uniqueID);
                    }
                }
            }
        }

        return ownedRunes.Where(r => !equippedRuneIDs.Contains(r.uniqueID)).ToList();
    }

    // ========== RUNE UPGRADE ==========

    /// <summary>
    /// Upgrade a procedural rune
    /// </summary>
    public bool UpgradeRune(RuneData rune, bool payCurrency = true)
    {
        if (!ValidateProceduralRune(rune) || rune.currentLevel >= rune.maxLevel)
        {
            Debug.LogWarning("⚠️ Cannot upgrade rune: invalid or max level");
            return false;
        }

        int upgradeCost = rune.GetUpgradeCost(rune.currentLevel);

        // Check currency
        if (payCurrency && CurrencyManager.Instance != null)
        {
            if (!CurrencyManager.Instance.CanAffordSoulCoins(upgradeCost))
            {
                Debug.LogWarning($"⚠️ Not enough Soul Coins to upgrade {rune.runeName}! Need {upgradeCost}");
                return false;
            }

            if (!CurrencyManager.Instance.SpendSoulCoins(upgradeCost))
            {
                Debug.LogWarning($"⚠️ Failed to spend Soul Coins for upgrade!");
                return false;
            }
        }

        bool success = rune.UpgradeRune();

        if (success)
        {
            SaveRuneCollection();
            OnRuneUpgraded?.Invoke(rune);
            OnRuneCollectionChanged?.Invoke();

            Debug.Log($"✨ Successfully upgraded {rune.runeName} to level {rune.currentLevel} for {upgradeCost} Soul Coins!");
        }

        return success;
    }

    // ========== RUNE SELLING ==========

    /// <summary>
    /// Get the sell price for a procedural rune
    /// </summary>
    public int GetRuneSellPrice(RuneData rune)
    {
        if (!ValidateProceduralRune(rune)) return 0;

        return CalculateRuneSellPrice(rune);
    }

    /// <summary>
    /// Sell a procedural rune
    /// </summary>
    public bool SellRune(RuneData rune, out int actualSellPrice)
    {
        actualSellPrice = 0;

        if (!ValidateProceduralRune(rune))
        {
            Debug.LogWarning("⚠️ Cannot sell invalid procedural rune");
            return false;
        }

        // Check if rune is equipped
        if (IsRuneEquipped(rune))
        {
            Debug.LogWarning($"⚠️ Cannot sell equipped rune: {rune.runeName}");
            return false;
        }

        actualSellPrice = CalculateRuneSellPrice(rune);

        // Remove rune from collection
        bool removed = RemoveRune(rune);
        if (removed && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddSoulCoins(actualSellPrice);
            Debug.Log($"💰 Sold procedural rune {rune.runeName} for {actualSellPrice} Soul Coins!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate sell price for a procedural rune
    /// </summary>
    private int CalculateRuneSellPrice(RuneData rune)
    {
        if (rune == null) return 0;

        // Base price by rarity
        int basePrice = GetBaseSellPriceByRarity(rune.rarity);

        // Level bonus (50% of upgrade costs invested)
        int levelBonus = 0;
        for (int i = 0; i < rune.currentLevel; i++)
        {
            levelBonus += Mathf.RoundToInt(rune.GetUpgradeCost(i) * 0.5f);
        }

        // Quality bonus for sub stats
        int qualityBonus = CalculateQualityBonus(rune);

        return basePrice + levelBonus + qualityBonus;
    }

    private int GetBaseSellPriceByRarity(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return 100;
            case RuneRarity.Uncommon: return 300;
            case RuneRarity.Rare: return 800;
            case RuneRarity.Epic: return 1500;
            case RuneRarity.Legendary: return 3000;
            default: return 100;
        }
    }

    private int CalculateQualityBonus(RuneData rune)
    {
        if (rune.subStats == null || rune.subStats.Count == 0)
            return 0;

        int bonus = 0;

        // Small bonus per sub stat
        bonus += rune.subStats.Count * 25;

        // Extra bonus for percentage stats
        foreach (var stat in rune.subStats)
        {
            if (stat.isPercentage)
                bonus += 50;
        }

        return bonus;
    }

    /// <summary>
    /// Get sell preview text for UI
    /// </summary>
    public string GetSellPreview(RuneData rune)
    {
        if (!ValidateProceduralRune(rune)) return "Invalid procedural rune";
        if (IsRuneEquipped(rune)) return "Cannot sell equipped rune";

        int sellPrice = CalculateRuneSellPrice(rune);
        return $"Sell for {sellPrice:N0} Soul Coins";
    }

    /// <summary>
    /// Get detailed sell breakdown for UI
    /// </summary>
    public string GetSellBreakdown(RuneData rune)
    {
        if (!ValidateProceduralRune(rune)) return "Invalid procedural rune";

        int basePrice = GetBaseSellPriceByRarity(rune.rarity);
        int levelBonus = 0;
        for (int i = 0; i < rune.currentLevel; i++)
        {
            levelBonus += Mathf.RoundToInt(rune.GetUpgradeCost(i) * 0.5f);
        }
        int qualityBonus = CalculateQualityBonus(rune);
        int totalPrice = basePrice + levelBonus + qualityBonus;

        string breakdown = $"Total: {totalPrice:N0} Soul Coins\n";
        breakdown += $"Base ({rune.rarity}): {basePrice:N0}";

        if (levelBonus > 0)
            breakdown += $"\nLevel Bonus: +{levelBonus:N0}";

        if (qualityBonus > 0)
            breakdown += $"\nQuality Bonus: +{qualityBonus:N0}";

        return breakdown;
    }

    // ========== MONSTER STAT CALCULATION ==========

    /// <summary>
    /// Calculate monster stat bonuses from equipped procedural runes
    /// </summary>
    public MonsterStats CalculateMonsterRuneBonuses(CollectedMonster monster)
    {
        var bonuses = new MonsterStats();

        if (monster?.runeSlots == null)
        {
            Debug.LogWarning("⚠️ Monster or rune slots are null");
            return bonuses;
        }

        // Track set counts for set bonuses
        var setCount = new Dictionary<string, int>();

        // Calculate bonuses from each equipped procedural rune
        for (int i = 0; i < 6; i++) // 6 rune slots
        {
            var rune = monster.GetEquippedRune(i);
            if (rune != null && rune.isProceduralGenerated)
            {
                // Add main stat bonus
                if (rune.mainStat != null)
                {
                    AddStatToMonsterStats(ref bonuses, rune.mainStat);
                }

                // Add sub stat bonuses
                if (rune.subStats != null)
                {
                    foreach (var subStat in rune.subStats)
                    {
                        if (subStat != null)
                        {
                            AddStatToMonsterStats(ref bonuses, subStat);
                        }
                    }
                }

                // Track set count
                if (rune.runeSet != null)
                {
                    string setName = rune.runeSet.setName;
                    setCount[setName] = setCount.GetValueOrDefault(setName, 0) + 1;
                }
            }
        }

        // Apply set bonuses if available
        if (availableRuneSets != null && availableRuneSets.Count > 0)
        {
            ApplySetBonuses(ref bonuses, setCount);
        }

        return bonuses;
    }

    /// <summary>
    /// Helper method to add a RuneStat to MonsterStats
    /// </summary>
    private void AddStatToMonsterStats(ref MonsterStats monsterStats, RuneStat runeStat)
    {
        if (runeStat == null) return;

        float value = runeStat.value;

        // Convert percentage bonuses to flat bonuses if needed
        if (runeStat.isPercentage)
        {
            value = value * 0.1f; // Convert 10% to 1 point, etc.
        }

        switch (runeStat.statType)
        {
            case RuneStatType.HP:
                monsterStats.health += Mathf.RoundToInt(value);
                break;
            case RuneStatType.ATK:
                monsterStats.attack += Mathf.RoundToInt(value);
                break;
            case RuneStatType.DEF:
                monsterStats.defense += Mathf.RoundToInt(value);
                break;
            case RuneStatType.SPD:
                monsterStats.speed += Mathf.RoundToInt(value);
                break;
            case RuneStatType.CriticalRate:
                monsterStats.attack += Mathf.RoundToInt(value * 0.5f);
                break;
            case RuneStatType.CriticalDamage:
                monsterStats.attack += Mathf.RoundToInt(value * 0.3f);
                break;
            case RuneStatType.Accuracy:
                monsterStats.speed += Mathf.RoundToInt(value * 0.2f);
                break;
            case RuneStatType.Resistance:
                monsterStats.defense += Mathf.RoundToInt(value * 0.3f);
                break;
        }
    }

    /// <summary>
    /// Apply set bonuses based on equipped rune sets
    /// </summary>
    private void ApplySetBonuses(ref MonsterStats bonuses, Dictionary<string, int> setCount)
    {
        foreach (var setData in availableRuneSets)
        {
            if (setData == null) continue;

            string setName = setData.setName;
            int equippedCount = setCount.GetValueOrDefault(setName, 0);

            // Apply set bonuses based on how many pieces are equipped
            if (setData.setBonuses != null)
            {
                foreach (var setBonus in setData.setBonuses)
                {
                    if (equippedCount >= setBonus.requiredPieces)
                    {
                        ApplySetBonus(ref bonuses, setBonus);
                        Debug.Log($"🎯 Applied {setName} set bonus ({setBonus.requiredPieces} pieces): {setBonus.description}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Apply individual set bonus to monster stats
    /// </summary>
    private void ApplySetBonus(ref MonsterStats bonuses, RuneSetBonus setBonus)
    {
        // Simplified implementation - expand based on your set bonus system
        if (setBonus.description.ToLower().Contains("attack"))
        {
            bonuses.attack += 20;
        }
        else if (setBonus.description.ToLower().Contains("defense"))
        {
            bonuses.defense += 20;
        }
        else if (setBonus.description.ToLower().Contains("health") || setBonus.description.ToLower().Contains("hp"))
        {
            bonuses.health += 50;
        }
        else if (setBonus.description.ToLower().Contains("speed"))
        {
            bonuses.speed += 10;
        }
        else
        {
            // Generic bonus if we can't determine the type
            bonuses.attack += 10;
            bonuses.defense += 10;
        }
    }

    // ========== EQUIPMENT SYSTEM ==========

    /// <summary>
    /// Check if a procedural rune is currently equipped on any monster
    /// </summary>
    public bool IsRuneEquipped(RuneData rune)
    {
        if (!ValidateProceduralRune(rune) || MonsterCollectionManager.Instance == null)
            return false;

        var monsters = MonsterCollectionManager.Instance.GetAllMonsters();
        foreach (var monster in monsters)
        {
            for (int i = 0; i < 6; i++) // 6 rune slots
            {
                var equippedRune = monster.GetEquippedRune(i);
                if (equippedRune != null && equippedRune.uniqueID == rune.uniqueID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a procedural rune is available for equipping
    /// </summary>
    public bool IsRuneAvailable(RuneData rune)
    {
        if (!ValidateProceduralRune(rune)) return false;

        // Check if we own this rune
        if (!ContainsRune(rune)) return false;

        // Check if it's already equipped somewhere
        return !IsRuneEquipped(rune);
    }

    /// <summary>
    /// Check if collection contains a specific procedural rune
    /// </summary>
    public bool ContainsRune(RuneData rune)
    {
        if (!ValidateProceduralRune(rune)) return false;
        return ownedRunes.Any(r => r.uniqueID == rune.uniqueID);
    }

    // ========== SAVE/LOAD SYSTEM ==========

    public void SaveRuneCollection()
    {
        try
        {
            // Convert all procedural runes to serializable format
            var serializableRunes = ownedRunes.Select(r => new SerializableRuneData(r)).ToList();

            ES3.Save("ProceduralRunes", serializableRunes, SaveManager.SAVE_FILE);
            ES3.Save("MaxRuneCapacity", maxRuneCapacity, SaveManager.SAVE_FILE);

            Debug.Log($"💾 Saved {serializableRunes.Count} procedural runes");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save procedural rune collection: {e.Message}");
        }
    }

    public void LoadRuneCollection()
    {
        try
        {
            Debug.Log("📥 Starting procedural rune collection load process...");

            ownedRunes.Clear();

            var serializableRunes = ES3.Load("ProceduralRunes", SaveManager.SAVE_FILE, new List<SerializableRuneData>());

            Debug.Log($"📦 Found {serializableRunes.Count} serialized procedural runes in save file");

            foreach (var serializableRune in serializableRunes)
            {
                try
                {
                    var rune = serializableRune.ToRuneData();
                    if (rune != null && rune.isProceduralGenerated)
                    {
                        ownedRunes.Add(rune);
                        Debug.Log($"   ✅ Loaded procedural rune: {rune.runeName} (ID: {rune.uniqueID})");
                    }
                    else if (rune != null && !rune.isProceduralGenerated)
                    {
                        Debug.LogWarning($"   ⚠️ Skipped non-procedural rune: {rune.runeName}");
                    }
                    else
                    {
                        Debug.LogWarning($"   ⚠️ Failed to deserialize rune: {serializableRune.runeName}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Failed to deserialize procedural rune: {e.Message}");
                }
            }

            maxRuneCapacity = ES3.Load("MaxRuneCapacity", SaveManager.SAVE_FILE, 500);

            Debug.Log($"💎 Successfully loaded {ownedRunes.Count} procedural runes from save file");

            // Fire events
            OnRuneCollectionChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load procedural rune collection: {e.Message}");
            ownedRunes = new List<RuneData>();
            maxRuneCapacity = 500;
        }
    }

    // ========== VALIDATION AND UTILITY ==========

    /// <summary>
    /// Validate procedural rune collection integrity
    /// </summary>
    public void ValidateCollection()
    {
        int fixedCount = 0;

        for (int i = ownedRunes.Count - 1; i >= 0; i--)
        {
            var rune = ownedRunes[i];
            if (rune == null)
            {
                ownedRunes.RemoveAt(i);
                fixedCount++;
                continue;
            }

            // Remove non-procedural runes
            if (!rune.isProceduralGenerated)
            {
                Debug.LogWarning($"⚠️ Removing non-procedural rune: {rune.runeName}");
                ownedRunes.RemoveAt(i);
                fixedCount++;
                continue;
            }

            // Fix missing unique ID
            if (string.IsNullOrEmpty(rune.uniqueID))
            {
                rune.uniqueID = System.Guid.NewGuid().ToString();
                Debug.Log($"🔧 Generated missing uniqueID for procedural rune: {rune.runeName}");
                fixedCount++;
            }

            // Check for duplicate IDs
            var duplicates = ownedRunes.Where(r =>
                r != null &&
                r != rune &&
                r.uniqueID == rune.uniqueID
            ).ToList();

            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"⚠️ Found duplicate procedural rune ID: {rune.uniqueID}, generating new ID");
                rune.uniqueID = System.Guid.NewGuid().ToString();
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"🔧 Fixed {fixedCount} procedural rune collection issues");
            SaveRuneCollection();
        }
    }

    public void RefreshRuneCollection()
    {
        Debug.Log("🔄 Refreshing procedural rune collection...");

        // Validate collection integrity
        ValidateCollection();

        // Fire refresh event
        OnRuneCollectionChanged?.Invoke();

        Debug.Log($"✅ Procedural rune collection refreshed - {ownedRunes.Count} runes validated");
    }

    // ========== SORTING AND FILTERING ==========

    public void SortRunesByPowerRating()
    {
        ownedRunes = ownedRunes.OrderByDescending(r => r.GetPowerRating()).ToList();
        OnRuneCollectionChanged?.Invoke();
    }

    public void SortRunesByRarity()
    {
        ownedRunes = ownedRunes.OrderByDescending(r => (int)r.rarity).ToList();
        OnRuneCollectionChanged?.Invoke();
    }

    public void SortRunesByLevel()
    {
        ownedRunes = ownedRunes.OrderByDescending(r => r.currentLevel).ToList();
        OnRuneCollectionChanged?.Invoke();
    }

    // ========== UTILITY PROPERTIES ==========

    public int GetRuneCount() => ownedRunes.Count;
    public int GetAvailableSpace() => maxRuneCapacity - ownedRunes.Count;
    public bool IsCollectionFull() => ownedRunes.Count >= maxRuneCapacity;

    // ========== DEBUG METHODS ==========

    [ContextMenu("Generate Test Procedural Runes")]
    public void GenerateTestRunes()
    {
        if (Application.isEditor)
        {
            var testRunes = GenerateMultipleRunes(5);
            foreach (var rune in testRunes)
            {
                AddRune(rune);
            }
            Debug.Log("🎲 Generated 5 test procedural runes");
        }
    }

    [ContextMenu("Clear All Procedural Runes")]
    public void ClearAllRunes()
    {
        if (Application.isEditor)
        {
            int count = ownedRunes.Count;
            ownedRunes.Clear();
            SaveRuneCollection();
            OnRuneCollectionChanged?.Invoke();
            Debug.Log($"🗑️ Cleared {count} procedural runes");
        }
    }

    [ContextMenu("Debug Procedural Rune Collection")]
    public void DebugRuneCollection()
    {
        Debug.Log($"=== PROCEDURAL RUNE COLLECTION DEBUG ===");
        Debug.Log($"Total Procedural Runes: {ownedRunes.Count}/{maxRuneCapacity}");
        Debug.Log($"Equipped Runes: {ownedRunes.Count - GetUnequippedRunes().Count}");
        Debug.Log($"Unequipped Runes: {GetUnequippedRunes().Count}");

        var rarityGroups = ownedRunes.GroupBy(r => r.rarity).OrderByDescending(g => (int)g.Key);
        Debug.Log("\n=== RARITY BREAKDOWN ===");
        foreach (var group in rarityGroups)
        {
            Debug.Log($"  {group.Key}: {group.Count()} runes");
        }

        if (ownedRunes.Count > 0)
        {
            Debug.Log("\n=== TOP 3 PROCEDURAL RUNES ===");
            var topRunes = ownedRunes.OrderByDescending(r => r.GetPowerRating()).Take(3);
            foreach (var rune in topRunes)
            {
                Debug.Log($"  {rune.runeName} +{rune.currentLevel} (Power: {rune.GetPowerRating()}, ID: {rune.uniqueID})");
            }
        }
    }
}

// ========== SUPPORTING CLASSES (unchanged) ==========

/// <summary>
/// Configuration for stat generation ranges
/// </summary>
[System.Serializable]
public class StatGenerationRange
{
    public RuneStatType statType;
    public float minValue;
    public float maxValue;
    public bool canBePercentage;
}

// SerializableRuneData class remains the same since it's already pure data-based


/// <summary>
/// Enhanced serializable wrapper for RuneData - handles all rune properties
/// Fully compatible with new pure data RuneData structure
/// </summary>
[System.Serializable]
public class SerializableRuneData
{
    // Basic identification
    public string uniqueID;
    public string runeName;
    public string name; // For compatibility
    public string description;
    public string spriteResourcePath;

    // Core properties
    public RuneType runeType;
    public RuneSlotPosition runeSlotPosition;
    public RuneRarity rarity;
    public int currentLevel;
    public int maxLevel;

    // Generation metadata
    public DateTime creationTime;
    public bool isProceduralGenerated;

    // Main stat
    public RuneStatType mainStatType;
    public float mainStatValue;
    public bool mainStatIsPercentage;

    // Sub stats
    public List<RuneStatType> subStatTypes = new List<RuneStatType>();
    public List<float> subStatValues = new List<float>();
    public List<bool> subStatIsPercentages = new List<bool>();

    // Upgrade costs (optional - can be recalculated)
    public List<int> upgradeCosts = new List<int>();

    // Set reference (stored as name for loading)
    public string runeSetName;

    // ✅ ENHANCED Constructor - handles all properties
    public SerializableRuneData(RuneData rune)
    {
        if (rune == null)
        {
            Debug.LogError("❌ Cannot serialize null RuneData");
            return;
        }

        // Basic identification
        uniqueID = rune.uniqueID ?? System.Guid.NewGuid().ToString();
        runeName = rune.runeName ?? "Unknown Rune";
        name = rune.name ?? rune.runeName;
        description = rune.description ?? "";

        // Core properties
        runeType = rune.runeType;
        runeSlotPosition = rune.runeSlotPosition;
        rarity = rune.rarity;
        currentLevel = rune.currentLevel;
        maxLevel = rune.maxLevel;

        // Generation metadata
        creationTime = rune.creationTime;
        isProceduralGenerated = rune.isProceduralGenerated;

        // Sprite path (try to determine resource path)
        if (rune.runeSprite != null)
        {
            spriteResourcePath = $"RuneIcon{rune.runeType}";
        }
        else
        {
            spriteResourcePath = $"UI/Runes/{rune.runeType}Icon";
        }

        // Serialize main stat
        if (rune.mainStat != null)
        {
            mainStatType = rune.mainStat.statType;
            mainStatValue = rune.mainStat.value;
            mainStatIsPercentage = rune.mainStat.isPercentage;
        }
        else
        {
            // Default main stat if missing
            mainStatType = RuneStatType.ATK;
            mainStatValue = 10f;
            mainStatIsPercentage = false;
        }

        // Serialize sub stats
        if (rune.subStats != null && rune.subStats.Count > 0)
        {
            foreach (var subStat in rune.subStats)
            {
                if (subStat != null)
                {
                    subStatTypes.Add(subStat.statType);
                    subStatValues.Add(subStat.value);
                    subStatIsPercentages.Add(subStat.isPercentage);
                }
            }
        }

        // Store upgrade costs if available
        if (rune.upgradeCosts != null && rune.upgradeCosts.Count > 0)
        {
            upgradeCosts = new List<int>(rune.upgradeCosts);
        }

        // Store rune set name for reference
        if (rune.runeSet != null)
        {
            runeSetName = rune.runeSet.name;
        }
        else
        {
            runeSetName = $"{rune.runeType}Set";
        }

        Debug.Log($"📦 Serialized rune: {runeName} (Level {currentLevel}, {subStatTypes.Count} substats)");
    }

    // ✅ ENHANCED ToRuneData - full reconstruction
    public RuneData ToRuneData()
    {
        try
        {
            // Create new rune data instance
            var rune = new RuneData();

            // Restore basic identification
            rune.uniqueID = uniqueID ?? System.Guid.NewGuid().ToString();
            rune.runeName = runeName ?? "Restored Rune";
            rune.name = name ?? rune.runeName;
            rune.description = description ?? "";

            // Restore core properties
            rune.runeType = runeType;
            rune.runeSlotPosition = runeSlotPosition;
            rune.rarity = rarity;
            rune.currentLevel = currentLevel;
            rune.maxLevel = maxLevel;

            // Restore generation metadata
            rune.creationTime = creationTime;
            rune.isProceduralGenerated = isProceduralGenerated;

            // ✅ Load sprite from Resources with fallbacks
            LoadRuneSprite(rune);

            // ✅ Load rune set from Resources
            LoadRuneSetData(rune);

            // Restore main stat
            rune.mainStat = new RuneStat
            {
                statType = mainStatType,
                value = mainStatValue,
                isPercentage = mainStatIsPercentage
            };

            // Restore sub stats
            rune.subStats = new List<RuneStat>();
            for (int i = 0; i < subStatTypes.Count && i < subStatValues.Count && i < subStatIsPercentages.Count; i++)
            {
                rune.subStats.Add(new RuneStat
                {
                    statType = subStatTypes[i],
                    value = subStatValues[i],
                    isPercentage = subStatIsPercentages[i]
                });
            }

            // Restore upgrade costs if available
            if (upgradeCosts != null && upgradeCosts.Count > 0)
            {
                rune.upgradeCosts = new List<int>(upgradeCosts);
            }

            Debug.Log($"🔧 Deserialized rune: {rune.runeName} (Level {rune.currentLevel}, {rune.subStats.Count} substats)");

            return rune;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to deserialize rune {runeName}: {e.Message}");
            return null;
        }
    }

    // ✅ Helper method for sprite loading
    private void LoadRuneSprite(RuneData rune)
    {
        try
        {
            // Try multiple sprite paths
            string[] spritePaths = {
                spriteResourcePath,
                $"RuneIcon{rune.runeType}",
                $"UI/Runes/{rune.runeType}Icon",
                $"Runes/{rune.runeType}",
                "UI/DefaultRuneIcon",
                "DefaultRune"
            };

            foreach (string path in spritePaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    rune.runeSprite = Resources.Load<Sprite>(path);
                    if (rune.runeSprite != null)
                    {
                        Debug.Log($"✅ Loaded sprite from: {path}");
                        break;
                    }
                }
            }

            if (rune.runeSprite == null)
            {
                Debug.LogWarning($"⚠️ Could not load sprite for {rune.runeType} rune");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Error loading sprite: {e.Message}");
        }
    }

    // ✅ Helper method for rune set loading
    private void LoadRuneSetData(RuneData rune)
    {
        try
        {
            // Try multiple set paths
            string[] setPaths = {
                $"Runes/Sets/{runeSetName}",
                $"Runes/Sets/{rune.runeType}Set",
                $"RuneSets/{rune.runeType}Set",
                $"Sets/{rune.runeType}Set"
            };

            foreach (string path in setPaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    rune.runeSet = Resources.Load<RuneSetData>(path);
                    if (rune.runeSet != null)
                    {
                        Debug.Log($"✅ Loaded rune set from: {path}");
                        break;
                    }
                }
            }

            if (rune.runeSet == null)
            {
                Debug.LogWarning($"⚠️ Could not load RuneSetData for {rune.runeType}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Error loading rune set: {e.Message}");
        }
    }

    // ✅ Validation method
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(uniqueID) &&
               !string.IsNullOrEmpty(runeName) &&
               subStatTypes.Count == subStatValues.Count &&
               subStatValues.Count == subStatIsPercentages.Count;
    }

    // ✅ Get summary for debugging
    public string GetSummary()
    {
        return $"{runeName} +{currentLevel} ({rarity} {runeType}) - {subStatTypes.Count} substats";
    }
}

