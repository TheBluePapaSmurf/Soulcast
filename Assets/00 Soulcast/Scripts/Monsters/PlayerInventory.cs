// Monsters/PlayerInventory.cs (Refactored)
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

public class PlayerInventory : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private MonsterCollectionManager monsterCollectionManager;
    [SerializeField] private RuneCollectionManager runeCollectionManager;

    public static PlayerInventory Instance { get; private set; }

    void Awake()
    {
        // Handle singleton pattern more carefully
        if (Instance == null)
        {
            Instance = this;

            // Only make persistent if we're not in a UI scene
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isUIScene = sceneName.Contains("Inventory") || sceneName.Contains("Gacha") || sceneName.Contains("Menu");

            if (!isUIScene)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("PlayerInventory: Made persistent (Main game scene)");
            }
            else
            {
                Debug.Log("PlayerInventory: Local instance (UI scene)");
            }
        }
        else
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isUIScene = sceneName.Contains("Inventory") || sceneName.Contains("Gacha") || sceneName.Contains("Menu");

            if (isUIScene)
            {
                Debug.Log("PlayerInventory: Keeping local UI instance");
                return;
            }

            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Get manager references if not assigned
        if (currencyManager == null)
            currencyManager = CurrencyManager.Instance;
        if (monsterCollectionManager == null)
            monsterCollectionManager = MonsterCollectionManager.Instance;
        if (runeCollectionManager == null)
            runeCollectionManager = RuneCollectionManager.Instance;
    }

    // ========== CURRENCY CONVENIENCE METHODS ==========

    public void AddSoulCoins(int amount) => currencyManager?.AddSoulCoins(amount);
    public bool SpendSoulCoins(int amount) => currencyManager?.SpendSoulCoins(amount) ?? false;
    public int GetSoulCoins() => currencyManager?.GetSoulCoins() ?? 0;
    public bool CanAfford(int amount) => currencyManager?.CanAffordSoulCoins(amount) ?? false;

    public void AddCrystals(int amount) => currencyManager?.AddCrystals(amount);
    public bool SpendCrystals(int amount) => currencyManager?.SpendCrystals(amount) ?? false;
    public int GetCrystals() => currencyManager?.GetCrystals() ?? 0;
    public bool CanAffordCrystals(int amount) => currencyManager?.CanAffordCrystals(amount) ?? false;

    // ========== MONSTER CONVENIENCE METHODS ==========

    public void AddMonster(MonsterData data) => monsterCollectionManager?.AddMonster(data);
    public List<CollectedMonster> GetAllMonsters() => monsterCollectionManager?.GetAllMonsters() ?? new List<CollectedMonster>();
    public CollectedMonster GetMonsterByID(string id) => monsterCollectionManager?.GetMonsterByID(id);
    public int GetMonsterCount(MonsterData data) => monsterCollectionManager?.GetMonsterCount(data) ?? 0;
    public int GetCollectionCount() => monsterCollectionManager?.GetCollectionCount() ?? 0;

    // ========== RUNE CONVENIENCE METHODS ==========

    public bool AddRune(RuneData rune) => runeCollectionManager?.AddRune(rune) ?? false;
    public void RemoveRune(RuneData rune) => runeCollectionManager?.RemoveRune(rune);
    public List<RuneData> GetUnequippedRunes() => runeCollectionManager?.GetUnequippedRunes() ?? new List<RuneData>();
    public List<RuneData> GetRunesByType(RuneType type) => runeCollectionManager?.GetRunesByType(type) ?? new List<RuneData>();
    public int GetRuneCount() => runeCollectionManager?.GetRuneCount() ?? 0;

    // Add these methods to PlayerInventory.cs

    // ========== MISSING MONSTER METHODS ==========

    public CollectedMonster GetMonster(string monsterID)
    {
        return MonsterCollectionManager.Instance?.GetMonsterByID(monsterID);
    }

    public List<MonsterData> GetUniqueMonsters()
    {
        return MonsterCollectionManager.Instance?.GetUniqueMonsterTypes() ?? new List<MonsterData>();
    }

    // ========== MISSING RUNE METHODS ==========

    public List<RuneData> ownedRunes => RuneCollectionManager.Instance?.GetAllRunes() ?? new List<RuneData>();

    public bool EquipRuneToMonster(string monsterID, int slotIndex, RuneData rune)
    {
        var monster = GetMonster(monsterID);
        if (monster != null && RuneCollectionManager.Instance != null)
        {
            return MonsterCollectionManager.Instance.EquipRuneToMonster(monster.uniqueID, slotIndex, rune);
        }
        return false;
    }

    public RuneData UnequipRuneFromMonster(string monsterID, int slotIndex)
    {
        var monster = GetMonster(monsterID);
        if (monster != null && RuneCollectionManager.Instance != null)
        {
            return MonsterCollectionManager.Instance.UnequipRuneFromMonster(monster.uniqueID, slotIndex);
        }
        return null;
    }


    // ========== LEGACY SAVE METHOD ==========

    [System.Obsolete("Use SaveManager.Instance.SaveGame() instead")]
    public void SaveToPlayerPrefs()
    {
        SaveManager.Instance?.SaveGame();
    }

}
