using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GachaManager : MonoBehaviour
{
    [Header("Gacha Configuration")]
    public List<GachaPool> gachaPools = new List<GachaPool>();

    [Header("Legacy Reference (Backup)")]
    public PlayerInventory playerInventory; // Keep for backward compatibility

    [Header("Unknown Pool Settings (1-3⭐)")]
    [SerializeField] private int unknownSingleCost = 100;
    [SerializeField] private int unknownMultiCost = 900;

    [Header("Mythical Pool Settings (3-5⭐)")]
    [SerializeField] private int mythicalSingleCost = 300;
    [SerializeField] private int mythicalMultiCost = 2700;

    [Header("General Settings")]
    [SerializeField] private int multiSummonCount = 10;

    [Header("Manager References")]
    [SerializeField] private bool autoFindManagers = true;
    [Tooltip("Timeout in seconds to wait for persistent managers")]
    [SerializeField] private float managerSearchTimeout = 10f;

    [Header("Debug")]
    public bool debugMode = true;
    [SerializeField] private bool showInitializationLogs = false;
    [SerializeField] private bool showPoolDetails = true;

    // 🆕 NEW: Manager references for new architecture
    private CurrencyManager currencyManager;
    private MonsterCollectionManager monsterCollectionManager;
    private bool managersInitialized = false;

    // Pool indices constants
    public const int UNKNOWN_POOL_INDEX = 0;
    public const int MYTHICAL_POOL_INDEX = 1;

    public static GachaManager Instance { get; private set; }

    // 🆕 NEW: Properties for UI access
    public int UnknownSingleCost => unknownSingleCost;
    public int UnknownMultiCost => unknownMultiCost;
    public int MythicalSingleCost => mythicalSingleCost;
    public int MythicalMultiCost => mythicalMultiCost;
    public int MultiSummonCount => multiSummonCount;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Note: GachaManager is scene-specific, so no DontDestroyOnLoad
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeGachaManager());
    }

    // 🆕 NEW: Robust initialization system
    private System.Collections.IEnumerator InitializeGachaManager()
    {
        if (showInitializationLogs) Debug.Log("🎲 GachaManager: Starting initialization...");

        // Wait for persistent managers
        yield return StartCoroutine(WaitForPersistentManagers());

        // Create the dual pool system
        CreateDualPoolSystem();

        managersInitialized = true;
        if (showInitializationLogs) Debug.Log("✅ GachaManager: Dual pool system initialized!");
    }

    // 🆕 NEW: Wait for persistent managers to be available
    private System.Collections.IEnumerator WaitForPersistentManagers()
    {
        if (!autoFindManagers) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < managerSearchTimeout)
        {
            bool managersFound = true;

            // Find CurrencyManager
            if (currencyManager == null)
            {
                currencyManager = CurrencyManager.Instance;
                if (currencyManager == null) managersFound = false;
            }

            // Find MonsterCollectionManager
            if (monsterCollectionManager == null)
            {
                monsterCollectionManager = MonsterCollectionManager.Instance;
                if (monsterCollectionManager == null) managersFound = false;
            }

            // Find PlayerInventory (backup)
            if (playerInventory == null)
            {
                playerInventory = PlayerInventory.Instance;
            }

            if (managersFound)
            {
                if (showInitializationLogs) Debug.Log("✅ GachaManager: All persistent managers found!");
                break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        // Final validation
        if (currencyManager == null)
        {
            Debug.LogError("❌ GachaManager: CurrencyManager not found! Gacha purchases may not work properly.");
        }

        if (monsterCollectionManager == null)
        {
            Debug.LogError("❌ GachaManager: MonsterCollectionManager not found! Monster collection may not work properly.");
        }
    }

    // 🆕 NEW: Create the dual pool system automatically
    void CreateDualPoolSystem()
    {
        // Clear existing pools
        gachaPools.Clear();

        // Load all monsters
        MonsterData[] allMonsters = Resources.LoadAll<MonsterData>("Monsters");
        if (allMonsters.Length == 0)
        {
            // Try alternative loading method
            allMonsters = Resources.FindObjectsOfTypeAll<MonsterData>();
        }

        if (allMonsters.Length == 0)
        {
            Debug.LogError("❌ No MonsterData found! Cannot create gacha pools.");
            return;
        }

        // Create Unknown Pool (1-3⭐)
        CreateUnknownPool(allMonsters);

        // Create Mythical Pool (3-5⭐)
        CreateMythicalPool(allMonsters);

        if (debugMode)
        {
            Debug.Log($"🎲 Created dual pool system:");
            Debug.Log($"  - Unknown Pool: {gachaPools[UNKNOWN_POOL_INDEX].monsters.Count} monsters (1-3⭐)");
            Debug.Log($"  - Mythical Pool: {gachaPools[MYTHICAL_POOL_INDEX].monsters.Count} monsters (3-5⭐)");
        }
    }

    void CreateUnknownPool(MonsterData[] allMonsters)
    {
        var unknownPool = new GachaPool
        {
            poolName = "Unknown Summon",
            description = "Summon 1⭐ to 3⭐ monsters\nGreat for building your collection!",
            guaranteedRareInTenPull = true
        };

        // Filter monsters with 1-3 stars
        var unknownMonsters = allMonsters.Where(m => m.defaultStarLevel >= 1 && m.defaultStarLevel <= 3).ToArray();

        foreach (var monster in unknownMonsters)
        {
            MonsterRarity rarity = GetRarityFromStarLevel(monster.defaultStarLevel);
            unknownPool.monsters.Add(new GachaMonster
            {
                monsterData = monster,
                rarity = rarity,
                dropRate = GetUnknownPoolDropRate(rarity)
            });
        }

        unknownPool.ValidatePool();
        gachaPools.Add(unknownPool);

        if (showPoolDetails && debugMode)
        {
            Debug.Log($"📦 Unknown Pool created with {unknownPool.monsters.Count} monsters:");
            var starCounts = unknownMonsters.GroupBy(m => m.defaultStarLevel).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kvp in starCounts.OrderBy(x => x.Key))
            {
                string stars = new string('⭐', kvp.Key);
                Debug.Log($"  {stars}: {kvp.Value} monsters");
            }
        }
    }

    void CreateMythicalPool(MonsterData[] allMonsters)
    {
        var mythicalPool = new GachaPool
        {
            poolName = "Mythical Summon",
            description = "Summon 3⭐ to 5⭐ monsters\nHigher chance for legendary creatures!",
            guaranteedRareInTenPull = true
        };

        // Filter monsters with 3-5 stars
        var mythicalMonsters = allMonsters.Where(m => m.defaultStarLevel >= 3 && m.defaultStarLevel <= 5).ToArray();

        foreach (var monster in mythicalMonsters)
        {
            MonsterRarity rarity = GetRarityFromStarLevel(monster.defaultStarLevel);
            mythicalPool.monsters.Add(new GachaMonster
            {
                monsterData = monster,
                rarity = rarity,
                dropRate = GetMythicalPoolDropRate(rarity)
            });
        }

        mythicalPool.ValidatePool();
        gachaPools.Add(mythicalPool);

        if (showPoolDetails && debugMode)
        {
            Debug.Log($"📦 Mythical Pool created with {mythicalPool.monsters.Count} monsters:");
            var starCounts = mythicalMonsters.GroupBy(m => m.defaultStarLevel).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kvp in starCounts.OrderBy(x => x.Key))
            {
                string stars = new string('⭐', kvp.Key);
                Debug.Log($"  {stars}: {kvp.Value} monsters");
            }
        }
    }

    // 🆕 NEW: Different drop rates for Unknown pool (1-3⭐)
    float GetUnknownPoolDropRate(MonsterRarity rarity)
    {
        switch (rarity)
        {
            case MonsterRarity.Common: return 70f;  // 1⭐ monsters - 70%
            case MonsterRarity.Uncommon: return 25f;  // 2⭐ monsters - 25%
            case MonsterRarity.Rare: return 5f;   // 3⭐ monsters - 5%
            default: return 0f;
        }
    }

    // 🆕 NEW: Different drop rates for Mythical pool (3-5⭐)
    float GetMythicalPoolDropRate(MonsterRarity rarity)
    {
        switch (rarity)
        {
            case MonsterRarity.Rare: return 60f;  // 3⭐ monsters - 60%
            case MonsterRarity.Epic: return 30f;  // 4⭐ monsters - 30%
            case MonsterRarity.Legendary: return 10f;  // 5⭐ monsters - 10%
            default: return 0f;
        }
    }

    // Convert star level to rarity enum
    MonsterRarity GetRarityFromStarLevel(int starLevel)
    {
        switch (starLevel)
        {
            case 1: return MonsterRarity.Common;
            case 2: return MonsterRarity.Uncommon;
            case 3: return MonsterRarity.Rare;
            case 4: return MonsterRarity.Epic;
            case 5: return MonsterRarity.Legendary;
            default:
                Debug.LogWarning($"⚠️ Invalid star level: {starLevel}, defaulting to Common");
                return MonsterRarity.Common;
        }
    }

    // 🆕 NEW: Pool-specific affordability checks
    public bool CanAffordUnknownSummon(bool isMulti = false)
    {
        if (currencyManager == null) return false;
        int cost = isMulti ? unknownMultiCost : unknownSingleCost;
        return currencyManager.CanAffordSoulCoins(cost);
    }

    public bool CanAffordMythicalSummon(bool isMulti = false)
    {
        if (currencyManager == null) return false;
        int cost = isMulti ? mythicalMultiCost : mythicalSingleCost;
        return currencyManager.CanAffordSoulCoins(cost);
    }

    // 🆕 NEW: Pool-specific summon methods
    public GachaSummonResult PerformUnknownSummon(bool isMulti = false)
    {
        int cost = isMulti ? unknownMultiCost : unknownSingleCost;
        int count = isMulti ? multiSummonCount : 1;

        return PerformSummon(UNKNOWN_POOL_INDEX, cost, count, "Unknown");
    }

    public GachaSummonResult PerformMythicalSummon(bool isMulti = false)
    {
        int cost = isMulti ? mythicalMultiCost : mythicalSingleCost;
        int count = isMulti ? multiSummonCount : 1;

        return PerformSummon(MYTHICAL_POOL_INDEX, cost, count, "Mythical");
    }

    // 🔧 UPDATED: Generic summon method with enhanced logic
    private GachaSummonResult PerformSummon(int poolIndex, int cost, int count, string poolType)
    {
        if (!managersInitialized)
        {
            return new GachaSummonResult { success = false, errorMessage = "Gacha system not ready yet!" };
        }

        if (poolIndex >= gachaPools.Count)
        {
            return new GachaSummonResult { success = false, errorMessage = "Invalid gacha pool!" };
        }

        if (currencyManager == null)
        {
            return new GachaSummonResult { success = false, errorMessage = "Currency system not available!" };
        }

        if (!currencyManager.CanAffordSoulCoins(cost))
        {
            return new GachaSummonResult { success = false, errorMessage = "Not enough Crystals!" };
        }

        // Spend crystals
        if (!currencyManager.SpendSoulCoins(cost))
        {
            return new GachaSummonResult { success = false, errorMessage = "Failed to spend crystals!" };
        }

        // Perform summons
        List<GachaMonster> summonedMonsters = new List<GachaMonster>();
        bool hasGuaranteedRare = false;

        for (int i = 0; i < count; i++)
        {
            GachaMonster monster;

            // Guarantee rare on last summon if multi-summon and no rare yet
            if (count > 1 && i == count - 1 && !hasGuaranteedRare && gachaPools[poolIndex].guaranteedRareInTenPull)
            {
                monster = gachaPools[poolIndex].SummonMonsterWithMinimumRarity(MonsterRarity.Rare);
                if (debugMode) Debug.Log($"🎯 Guaranteed rare triggered for {poolType} pool");
            }
            else
            {
                monster = gachaPools[poolIndex].SummonMonster();
            }

            if (monster != null)
            {
                summonedMonsters.Add(monster);

                // Add to monster collection
                if (monsterCollectionManager != null)
                {
                    monsterCollectionManager.AddMonster(monster.monsterData);
                }
                else if (playerInventory != null)
                {
                    // Fallback to old system
                    playerInventory.AddMonster(monster.monsterData);
                }

                if (monster.rarity >= MonsterRarity.Rare)
                {
                    hasGuaranteedRare = true;
                }
            }
        }

        if (debugMode)
        {
            string summonType = count > 1 ? "Multi" : "Single";
            Debug.Log($"🎉 {poolType} {summonType} summon complete: {summonedMonsters.Count} monsters summoned");

            // Show rarity breakdown
            var rarityBreakdown = summonedMonsters.GroupBy(m => m.rarity).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kvp in rarityBreakdown.OrderByDescending(x => (int)x.Key))
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value} monsters");
            }
        }

        return new GachaSummonResult
        {
            success = true,
            summonedMonsters = summonedMonsters,
            currencySpent = cost,
            poolIndex = poolIndex,
            summonTime = Time.time
        };
    }

    // Legacy methods for backward compatibility
    public GachaSummonResult PerformSingleSummon(int poolIndex = 0)
    {
        if (poolIndex == UNKNOWN_POOL_INDEX) return PerformUnknownSummon(false);
        if (poolIndex == MYTHICAL_POOL_INDEX) return PerformMythicalSummon(false);
        return new GachaSummonResult { success = false, errorMessage = "Invalid pool index!" };
    }

    public GachaSummonResult PerformMultiSummon(int poolIndex = 0)
    {
        if (poolIndex == UNKNOWN_POOL_INDEX) return PerformUnknownSummon(true);
        if (poolIndex == MYTHICAL_POOL_INDEX) return PerformMythicalSummon(true);
        return new GachaSummonResult { success = false, errorMessage = "Invalid pool index!" };
    }

    // For backward compatibility
    public bool CanAffordSummon(int cost)
    {
        return currencyManager?.CanAffordSoulCoins(cost) ?? false;
    }

    public List<string> GetPoolNames()
    {
        return gachaPools.Select(pool => pool.poolName).ToList();
    }

    public GachaPool GetPool(int index)
    {
        if (index >= 0 && index < gachaPools.Count)
            return gachaPools[index];
        return null;
    }

    // 🆕 NEW: Public methods for debugging and testing
    [ContextMenu("Test Unknown Single Summon")]
    public void TestUnknownSingleSummon()
    {
        var result = PerformUnknownSummon(false);
        Debug.Log($"Test Unknown single summon: {result.success}, Message: {result.errorMessage}");
    }

    [ContextMenu("Test Unknown Multi Summon")]
    public void TestUnknownMultiSummon()
    {
        var result = PerformUnknownSummon(true);
        Debug.Log($"Test Unknown multi summon: {result.success}, Message: {result.errorMessage}");
    }

    [ContextMenu("Test Mythical Single Summon")]
    public void TestMythicalSingleSummon()
    {
        var result = PerformMythicalSummon(false);
        Debug.Log($"Test Mythical single summon: {result.success}, Message: {result.errorMessage}");
    }

    [ContextMenu("Test Mythical Multi Summon")]
    public void TestMythicalMultiSummon()
    {
        var result = PerformMythicalSummon(true);
        Debug.Log($"Test Mythical multi summon: {result.success}, Message: {result.errorMessage}");
    }

    [ContextMenu("Refresh Dual Pool System")]
    public void RefreshDualPoolSystem()
    {
        CreateDualPoolSystem();
        Debug.Log("🔄 Dual pool system refreshed!");
    }

    [ContextMenu("Refresh Manager Connections")]
    public void RefreshManagerConnections()
    {
        managersInitialized = false;
        currencyManager = null;
        monsterCollectionManager = null;
        StopAllCoroutines();
        StartCoroutine(InitializeGachaManager());
    }

    [ContextMenu("Show Pool Statistics")]
    public void ShowPoolStatistics()
    {
        if (gachaPools.Count == 0)
        {
            Debug.Log("❌ No pools available");
            return;
        }

        Debug.Log("📊 Gacha Pool Statistics:");
        for (int i = 0; i < gachaPools.Count; i++)
        {
            var pool = gachaPools[i];
            Debug.Log($"\n🎲 Pool {i}: {pool.poolName}");
            Debug.Log($"  Description: {pool.description}");
            Debug.Log($"  Total Monsters: {pool.monsters.Count}");

            var rarityStats = pool.monsters.GroupBy(m => m.rarity)
                .OrderByDescending(g => (int)g.Key)
                .ToDictionary(g => g.Key, g => new { Count = g.Count(), TotalRate = g.Sum(m => m.dropRate) });

            foreach (var kvp in rarityStats)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.Count} monsters ({kvp.Value.TotalRate:F1}% total rate)");
            }
        }
    }
}

// 🔧 Enhanced GachaPool class
[System.Serializable]
public class GachaPool
{
    [Header("Pool Information")]
    public string poolName;
    [TextArea(2, 4)]
    public string description;

    [Header("Monsters")]
    public List<GachaMonster> monsters = new List<GachaMonster>();

    [Header("Settings")]
    public bool guaranteedRareInTenPull = true;

    public void ValidatePool()
    {
        if (monsters.Count == 0)
        {
            Debug.LogWarning($"⚠️ Gacha pool '{poolName}' has no monsters!");
            return;
        }

        float totalRate = monsters.Sum(m => m.dropRate);

        if (Mathf.Abs(totalRate - 100f) > 0.1f)
        {
            Debug.LogWarning($"⚠️ Gacha pool '{poolName}' drop rates don't add up to 100% (current: {totalRate:F1}%)");
        }

        // Check for duplicate monsters
        var duplicates = monsters.GroupBy(m => m.monsterData)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key?.name ?? "Unknown");

        foreach (var duplicate in duplicates)
        {
            Debug.LogWarning($"⚠️ Gacha pool '{poolName}' has duplicate monster: {duplicate}");
        }

        // Show star level distribution in editor
        if (Application.isEditor)
        {
            var starDistribution = monsters.GroupBy(m => m.StarLevel)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            Debug.Log($"🌟 Pool '{poolName}' star distribution:");
            foreach (var kvp in starDistribution)
            {
                string stars = new string('⭐', kvp.Key);
                Debug.Log($"  {stars} ({kvp.Key}⭐): {kvp.Value} monsters");
            }
        }
    }

    public GachaMonster SummonMonster()
    {
        if (monsters.Count == 0) return null;

        float randomValue = Random.Range(0f, 100f);
        float currentRate = 0f;

        // Sort by rarity (lowest first) to ensure proper probability
        var sortedMonsters = monsters.OrderBy(m => (int)m.rarity).ToList();

        foreach (var monster in sortedMonsters)
        {
            currentRate += monster.dropRate;
            if (randomValue <= currentRate)
            {
                return monster;
            }
        }

        // Fallback to last monster
        return sortedMonsters.LastOrDefault();
    }

    public GachaMonster SummonMonsterWithMinimumRarity(MonsterRarity minimumRarity)
    {
        var eligibleMonsters = monsters.Where(m => m.rarity >= minimumRarity).ToList();

        if (eligibleMonsters.Count == 0)
            return SummonMonster(); // Fallback to normal summon

        // Recalculate probabilities for eligible monsters only
        float totalEligibleRate = eligibleMonsters.Sum(m => m.dropRate);
        if (totalEligibleRate <= 0) return eligibleMonsters.FirstOrDefault();

        float randomValue = Random.Range(0f, totalEligibleRate);
        float currentRate = 0f;

        foreach (var monster in eligibleMonsters)
        {
            currentRate += monster.dropRate;
            if (randomValue <= currentRate)
            {
                return monster;
            }
        }

        return eligibleMonsters.FirstOrDefault();
    }
}

// 🔧 Enhanced GachaMonster class
[System.Serializable]
public class GachaMonster
{
    [Header("Monster Data")]
    public MonsterData monsterData;
    public MonsterRarity rarity;

    [Header("Drop Settings")]
    [Range(0f, 100f)]
    public float dropRate;

    [Header("Visual (Optional)")]
    public Sprite rarityIcon; // Optional: Icon for this rarity
    public Color rarityColor = Color.white; // Optional: Color for this rarity

    // 🆕 NEW: Helper properties for UI display
    public int StarLevel => monsterData?.defaultStarLevel ?? 1;
    public string StarDisplay => new string('⭐', StarLevel);
    public ElementType Element => monsterData?.element ?? ElementType.Fire;
    public Color ElementColor => ElementalSystem.GetElementColor(Element);
    public string DisplayName => monsterData?.name ?? "Unknown Monster";
}

// 🔧 Enhanced GachaSummonResult class
[System.Serializable]
public class GachaSummonResult
{
    [Header("Result Data")]
    public bool success;
    public string errorMessage;
    public List<GachaMonster> summonedMonsters = new List<GachaMonster>();
    public int currencySpent;

    [Header("Metadata")]
    public float summonTime; // When the summon happened
    public int poolIndex; // Which pool was used

    public GachaSummonResult()
    {
        summonTime = Time.time;
        summonedMonsters = new List<GachaMonster>();
    }

    // 🆕 NEW: Helper properties
    public int TotalMonstersObtained => summonedMonsters?.Count ?? 0;
    public bool IsMultiSummon => TotalMonstersObtained > 1;
    public string PoolName => poolIndex == GachaManager.UNKNOWN_POOL_INDEX ? "Unknown" : "Mythical";

    // Get highest rarity obtained
    public MonsterRarity HighestRarity
    {
        get
        {
            if (summonedMonsters == null || summonedMonsters.Count == 0)
                return MonsterRarity.Common;
            return summonedMonsters.Max(m => m.rarity);
        }
    }
}

// Monster rarity enum
public enum MonsterRarity
{
    Common = 1,      // 1⭐
    Uncommon = 2,    // 2⭐
    Rare = 3,        // 3⭐
    Epic = 4,        // 4⭐
    Legendary = 5    // 5⭐
}
