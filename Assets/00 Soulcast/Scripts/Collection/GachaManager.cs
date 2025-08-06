using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GachaManager : MonoBehaviour
{
    [Header("Gacha Configuration")]
    public List<GachaPool> gachaPools = new List<GachaPool>();

    [Header("Legacy Reference (Backup)")]
    public PlayerInventory playerInventory; // Keep for backward compatibility

    [Header("Currency Settings")]
    public int singleSummonCost = 100;
    public int multiSummonCost = 900; // 10x summon with discount
    public int multiSummonCount = 10;

    [Header("Manager References")]
    [SerializeField] private bool autoFindManagers = true;
    [Tooltip("Timeout in seconds to wait for persistent managers")]
    [SerializeField] private float managerSearchTimeout = 10f;

    [Header("Debug")]
    public bool debugMode = true;
    [SerializeField] private bool showInitializationLogs = false;

    // 🆕 NEW: Manager references for new architecture
    private CurrencyManager currencyManager;
    private MonsterCollectionManager monsterCollectionManager;
    private bool managersInitialized = false;

    public static GachaManager Instance { get; private set; }

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

        // Initialize gacha pools
        InitializeGachaPools();

        managersInitialized = true;
        if (showInitializationLogs) Debug.Log("✅ GachaManager: Initialization complete!");
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

    void InitializeGachaPools()
    {
        if (gachaPools.Count == 0)
        {
            Debug.LogWarning("⚠️ No gacha pools configured! Creating a default pool.");
            CreateDefaultGachaPool();
        }

        // Validate all pools
        foreach (var pool in gachaPools)
        {
            pool.ValidatePool();
        }

        if (debugMode) Debug.Log($"🎲 GachaManager: Initialized {gachaPools.Count} gacha pools");
    }

    void CreateDefaultGachaPool()
    {
        // Find all MonsterData assets in the project
        MonsterData[] allMonsters = Resources.LoadAll<MonsterData>("Monsters");

        if (allMonsters.Length == 0)
        {
            // Try loading from different path
            allMonsters = Resources.FindObjectsOfTypeAll<MonsterData>();
        }

        GachaPool defaultPool = new GachaPool
        {
            poolName = "Standard Banner",
            description = "Contains all available monsters"
        };

        foreach (var monster in allMonsters)
        {
            // 🌟 CORRECTED: Use star level to determine rarity
            MonsterRarity rarity = GetRarityFromStarLevel(monster.defaultStarLevel);

            defaultPool.monsters.Add(new GachaMonster
            {
                monsterData = monster,
                rarity = rarity,
                dropRate = GetDefaultDropRate(rarity)
            });
        }

        gachaPools.Add(defaultPool);
        Debug.Log($"📦 Created default gacha pool with {defaultPool.monsters.Count} monsters");

        // Debug info about rarity distribution
        if (debugMode)
        {
            var rarityCount = defaultPool.monsters.GroupBy(m => m.rarity)
                .ToDictionary(g => g.Key, g => g.Count());
            foreach (var kvp in rarityCount)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value} monsters");
            }
        }
    }

    private MonsterRarity GetRarityFromStarLevel(int starLevel)
    {
        switch (starLevel)
        {
            case 1: return MonsterRarity.Common;     // 1⭐ = Common
            case 2: return MonsterRarity.Uncommon;   // 2⭐ = Uncommon  
            case 3: return MonsterRarity.Rare;       // 3⭐ = Rare
            case 4: return MonsterRarity.Epic;       // 4⭐ = Epic
            case 5: return MonsterRarity.Legendary;  // 5⭐ = Legendary
            default:
                Debug.LogWarning($"⚠️ Invalid star level: {starLevel}, defaulting to Common");
                return MonsterRarity.Common;
        }
    }

    float GetDefaultDropRate(MonsterRarity rarity)
    {
        switch (rarity)
        {
            case MonsterRarity.Common: return 50f;  // 1⭐ monsters - 50%
            case MonsterRarity.Uncommon: return 30f;  // 2⭐ monsters - 30%
            case MonsterRarity.Rare: return 15f;  // 3⭐ monsters - 15%
            case MonsterRarity.Epic: return 4f;   // 4⭐ monsters - 4%
            case MonsterRarity.Legendary: return 1f;   // 5⭐ monsters - 1%
            default: return 50f;
        }
    }

    // 🆕 UPDATED: Use CurrencyManager for affordability checks
    public bool CanAffordSummon(int cost)
    {
        if (currencyManager != null)
        {
            return currencyManager.CanAffordCrystals(cost); // Use Crystals for Gacha
        }

        // Fallback to old system
        if (playerInventory != null)
        {
            return playerInventory.CanAffordCrystals(cost);
        }

        Debug.LogWarning("⚠️ GachaManager: No currency manager available for affordability check!");
        return false;
    }

    // 🆕 UPDATED: Single summon with new manager system
    public GachaSummonResult PerformSingleSummon(int poolIndex = 0)
    {
        if (!managersInitialized)
        {
            return new GachaSummonResult { success = false, errorMessage = "Gacha system not ready yet!" };
        }

        if (!CanAffordSummon(singleSummonCost))
        {
            return new GachaSummonResult { success = false, errorMessage = "Not enough Crystals!" };
        }

        if (poolIndex >= gachaPools.Count)
        {
            return new GachaSummonResult { success = false, errorMessage = "Invalid gacha pool!" };
        }

        // Deduct crystals using CurrencyManager
        if (currencyManager != null)
        {
            if (!currencyManager.SpendCrystals(singleSummonCost))
            {
                return new GachaSummonResult { success = false, errorMessage = "Failed to spend crystals!" };
            }
        }
        else
        {
            // Fallback to PlayerInventory
            if (playerInventory != null)
            {
                if (!playerInventory.SpendCrystals(singleSummonCost))
                {
                    return new GachaSummonResult { success = false, errorMessage = "Failed to spend crystals!" };
                }
            }
            else
            {
                return new GachaSummonResult { success = false, errorMessage = "No currency system available!" };
            }
        }

        // Perform summon
        GachaMonster summonedMonster = gachaPools[poolIndex].SummonMonster();

        if (summonedMonster != null)
        {
            // Add to player collection using MonsterCollectionManager
            if (monsterCollectionManager != null)
            {
                monsterCollectionManager.AddMonster(summonedMonster.monsterData);
            }
            else if (playerInventory != null)
            {
                // Fallback
                playerInventory.AddMonster(summonedMonster.monsterData);
            }
            else
            {
                Debug.LogError("❌ GachaManager: No monster collection system available!");
            }

            if (debugMode)
            {
                Debug.Log($"🎉 Single summon: {summonedMonster.monsterData.name} ({summonedMonster.rarity})");
            }

            return new GachaSummonResult
            {
                success = true,
                summonedMonsters = new List<GachaMonster> { summonedMonster },
                currencySpent = singleSummonCost
            };
        }

        return new GachaSummonResult { success = false, errorMessage = "Summon failed!" };
    }

    // 🆕 UPDATED: Multi-summon with new manager system
    public GachaSummonResult PerformMultiSummon(int poolIndex = 0)
    {
        if (!managersInitialized)
        {
            return new GachaSummonResult { success = false, errorMessage = "Gacha system not ready yet!" };
        }

        if (!CanAffordSummon(multiSummonCost))
        {
            return new GachaSummonResult { success = false, errorMessage = "Not enough Crystals!" };
        }

        if (poolIndex >= gachaPools.Count)
        {
            return new GachaSummonResult { success = false, errorMessage = "Invalid gacha pool!" };
        }

        // Deduct crystals
        if (currencyManager != null)
        {
            if (!currencyManager.SpendCrystals(multiSummonCost))
            {
                return new GachaSummonResult { success = false, errorMessage = "Failed to spend crystals!" };
            }
        }
        else if (playerInventory != null)
        {
            if (!playerInventory.SpendCrystals(multiSummonCost))
            {
                return new GachaSummonResult { success = false, errorMessage = "Failed to spend crystals!" };
            }
        }
        else
        {
            return new GachaSummonResult { success = false, errorMessage = "No currency system available!" };
        }

        List<GachaMonster> summonedMonsters = new List<GachaMonster>();

        // Guarantee at least one rare in multi-summon
        bool hasGuaranteedRare = false;

        for (int i = 0; i < multiSummonCount; i++)
        {
            GachaMonster monster;

            // Force rare on last summon if none obtained yet
            if (i == multiSummonCount - 1 && !hasGuaranteedRare)
            {
                monster = gachaPools[poolIndex].SummonMonsterWithMinimumRarity(MonsterRarity.Rare);
            }
            else
            {
                monster = gachaPools[poolIndex].SummonMonster();
            }

            if (monster != null)
            {
                summonedMonsters.Add(monster);

                // Add to collection
                if (monsterCollectionManager != null)
                {
                    monsterCollectionManager.AddMonster(monster.monsterData);
                }
                else if (playerInventory != null)
                {
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
            Debug.Log($"🎉 Multi-summon complete: {summonedMonsters.Count} monsters summoned");
            var rareCounts = summonedMonsters.GroupBy(m => m.rarity)
                .ToDictionary(g => g.Key, g => g.Count());
            foreach (var kvp in rareCounts)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value}");
            }
        }

        return new GachaSummonResult
        {
            success = true,
            summonedMonsters = summonedMonsters,
            currencySpent = multiSummonCost
        };
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
    [ContextMenu("Test Single Summon")]
    public void TestSingleSummon()
    {
        var result = PerformSingleSummon(0);
        Debug.Log($"Test summon result: {result.success}, Message: {result.errorMessage}");
    }

    [ContextMenu("Test Multi Summon")]
    public void TestMultiSummon()
    {
        var result = PerformMultiSummon(0);
        Debug.Log($"Test multi-summon result: {result.success}, Message: {result.errorMessage}");
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
}

// 🔧 Updated GachaPool class with improved validation
[System.Serializable]
public class GachaPool
{
    public string poolName;
    [TextArea(2, 4)]
    public string description;
    public List<GachaMonster> monsters = new List<GachaMonster>();
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

[System.Serializable]
public class GachaMonster
{
    public MonsterData monsterData;
    public MonsterRarity rarity;
    [Range(0f, 100f)]
    public float dropRate;

    [Header("Visual")]
    public Sprite rarityIcon; // Optional: Icon for this rarity
    public Color rarityColor = Color.white; // Optional: Color for this rarity
}

[System.Serializable]
public class GachaSummonResult
{
    public bool success;
    public string errorMessage;
    public List<GachaMonster> summonedMonsters = new List<GachaMonster>();
    public int currencySpent;

    // 🆕 NEW: Additional result data
    public float summonTime; // When the summon happened
    public int poolIndex; // Which pool was used

    public GachaSummonResult()
    {
        summonTime = Time.time;
    }
}

public enum MonsterRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}
