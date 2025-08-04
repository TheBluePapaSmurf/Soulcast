using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GachaManager : MonoBehaviour
{
    [Header("Gacha Configuration")]
    public List<GachaPool> gachaPools = new List<GachaPool>();
    public PlayerInventory playerInventory;

    [Header("Currency Settings")]
    public int singleSummonCost = 100;
    public int multiSummonCost = 900; // 10x summon with discount
    public int multiSummonCount = 10;

    [Header("Debug")]
    public bool debugMode = true;

    public static GachaManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeGachaPools();
    }

    void InitializeGachaPools()
    {
        if (gachaPools.Count == 0)
        {
            Debug.LogWarning("No gacha pools configured! Creating a default pool.");
            CreateDefaultGachaPool();
        }

        // Validate all pools
        foreach (var pool in gachaPools)
        {
            pool.ValidatePool();
        }
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
            // Assign rarity based on element (just as an example)
            MonsterRarity rarity = MonsterRarity.Common;
            switch (monster.element)
            {
                case ElementType.Fire:
                    rarity = MonsterRarity.Rare;
                    break;
                case ElementType.Water:
                    rarity = MonsterRarity.Uncommon;
                    break;
                case ElementType.Earth:
                    rarity = MonsterRarity.Common;
                    break;
            }

            defaultPool.monsters.Add(new GachaMonster
            {
                monsterData = monster,
                rarity = rarity,
                dropRate = GetDefaultDropRate(rarity)
            });
        }

        gachaPools.Add(defaultPool);
        Debug.Log($"Created default gacha pool with {defaultPool.monsters.Count} monsters");
    }

    float GetDefaultDropRate(MonsterRarity rarity)
    {
        switch (rarity)
        {
            case MonsterRarity.Common: return 60f;
            case MonsterRarity.Uncommon: return 30f;
            case MonsterRarity.Rare: return 8f;
            case MonsterRarity.Epic: return 1.8f;
            case MonsterRarity.Legendary: return 0.2f;
            default: return 60f;
        }
    }

    public GachaSummonResult PerformSingleSummon(int poolIndex = 0)
    {
        if (!CanAffordSummon(singleSummonCost))
        {
            return new GachaSummonResult { success = false, errorMessage = "Not enough Soul Coins!" };
        }

        if (poolIndex >= gachaPools.Count)
        {
            return new GachaSummonResult { success = false, errorMessage = "Invalid gacha pool!" };
        }

        // Deduct currency
        playerInventory.SpendSoulCoins(singleSummonCost);

        // Perform summon
        GachaMonster summonedMonster = gachaPools[poolIndex].SummonMonster();

        if (summonedMonster != null)
        {
            // Add to player collection
            playerInventory.AddMonster(summonedMonster.monsterData);

            return new GachaSummonResult
            {
                success = true,
                summonedMonsters = new List<GachaMonster> { summonedMonster },
                currencySpent = singleSummonCost
            };
        }

        return new GachaSummonResult { success = false, errorMessage = "Summon failed!" };
    }

    public GachaSummonResult PerformMultiSummon(int poolIndex = 0)
    {
        if (!CanAffordSummon(multiSummonCost))
        {
            return new GachaSummonResult { success = false, errorMessage = "Not enough Soul Coins!" };
        }

        if (poolIndex >= gachaPools.Count)
        {
            return new GachaSummonResult { success = false, errorMessage = "Invalid gacha pool!" };
        }

        // Deduct currency
        playerInventory.SpendSoulCoins(multiSummonCost);

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
                playerInventory.AddMonster(monster.monsterData);

                if (monster.rarity >= MonsterRarity.Rare)
                {
                    hasGuaranteedRare = true;
                }
            }
        }

        return new GachaSummonResult
        {
            success = true,
            summonedMonsters = summonedMonsters,
            currencySpent = multiSummonCost
        };
    }

    public bool CanAffordSummon(int cost)
    {
        return playerInventory.CanAfford(cost);
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
}

[System.Serializable]
public class GachaPool
{
    public string poolName;
    public string description;
    public List<GachaMonster> monsters = new List<GachaMonster>();
    public bool guaranteedRareInTenPull = true;

    public void ValidatePool()
    {
        float totalRate = monsters.Sum(m => m.dropRate);

        if (Mathf.Abs(totalRate - 100f) > 0.1f)
        {
            Debug.LogWarning($"Gacha pool '{poolName}' drop rates don't add up to 100% (current: {totalRate}%)");
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
}

[System.Serializable]
public class GachaSummonResult
{
    public bool success;
    public string errorMessage;
    public List<GachaMonster> summonedMonsters = new List<GachaMonster>();
    public int currencySpent;
}

public enum MonsterRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}
