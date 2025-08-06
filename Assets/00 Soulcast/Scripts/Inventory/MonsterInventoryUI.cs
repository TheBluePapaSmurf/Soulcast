using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class MonsterInventoryUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inventoryMainPanel;
    public GameObject noMonstersPanel;
    public GameObject mainButtons;

    [Header("Header")]
    public Button backButton;
    public TextMeshProUGUI titleText;

    [Header("3D Display")]
    public RawImage renderTextureDisplay;
    public MonsterDisplay3D monsterDisplay3D;

    [Header("Stats Panel")]
    public TextMeshProUGUI monsterNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI elementText;
    public TextMeshProUGUI healthStatText;
    public TextMeshProUGUI attackStatText;
    public TextMeshProUGUI defenseStatText;
    public TextMeshProUGUI duplicateInfoText;
    public StarDisplay statsStarDisplay;

    [Header("Rune Panel")]
    public RunePanelUI runePanelUI;
    public Button runeTabButton;

    [Header("Monster List")]
    public Transform monsterListContent;
    public GameObject inventoryMonsterCardPrefab;
    public ScrollRect monsterScrollView;

    [Header("Selection")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("🧪 Manual Test Setup")]
    [SerializeField] private List<MonsterData> testMonsterData = new List<MonsterData>();
    [SerializeField] private bool addTestMonstersOnStart = false;

    [Header("Auto Find Settings")]
    [SerializeField] private bool autoFindMainButtons = true;
    [SerializeField] private bool autoFindMonsterCollectionButton = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Manager References
    private MonsterCollectionManager monsterCollectionManager;
    private RuneCollectionManager runeCollectionManager;
    private CurrencyManager currencyManager;
    private PlayerInventory playerInventory;

    // UI State
    private List<InventoryMonsterCard> monsterCards = new List<InventoryMonsterCard>();
    private CollectedMonster currentSelectedMonster;
    private InventoryMonsterCard currentSelectedCard;
    private Button monsterCollectionButton;

    void Awake()
    {
        if (inventoryMainPanel != null)
        {
            inventoryMainPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (showDebugLogs) Debug.Log("🎮 MonsterInventoryUI Starting...");

        FindManagerReferences();
        SetupUI();
        Setup3DDisplay();
        SetupMonsterCollectionButton();

        // 🆕 NEW: Add test monsters if requested
        if (addTestMonstersOnStart)
        {
            AddTestMonstersToCollection();
        }
    }

    void FindManagerReferences()
    {
        if (showDebugLogs) Debug.Log("🔍 Finding manager references...");

        monsterCollectionManager = MonsterCollectionManager.Instance;
        runeCollectionManager = RuneCollectionManager.Instance;
        currencyManager = CurrencyManager.Instance;
        playerInventory = PlayerInventory.Instance;

        if (showDebugLogs)
        {
            Debug.Log($"🔍 MonsterCollectionManager found: {monsterCollectionManager != null}");
            Debug.Log($"🔍 RuneCollectionManager found: {runeCollectionManager != null}");
            Debug.Log($"🔍 CurrencyManager found: {currencyManager != null}");
            Debug.Log($"🔍 PlayerInventory found: {playerInventory != null}");
        }

        if (autoFindMainButtons && mainButtons == null)
        {
            Transform mainButtonsTransform = GameObject.Find("Canvas/MainButtons")?.transform;
            if (mainButtonsTransform != null)
            {
                mainButtons = mainButtonsTransform.gameObject;
                if (showDebugLogs) Debug.Log("✅ Auto-found MainButtons");
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning("⚠️ Could not find MainButtons!");
            }
        }
    }

    void SetupMonsterCollectionButton()
    {
        if (autoFindMonsterCollectionButton)
        {
            Transform buttonTransform = GameObject.Find("Canvas/MainButtons/MonsterCollection")?.transform;
            if (buttonTransform != null)
            {
                monsterCollectionButton = buttonTransform.GetComponent<Button>();
                if (monsterCollectionButton != null)
                {
                    monsterCollectionButton.onClick.AddListener(OpenMonsterInventory);
                    if (showDebugLogs) Debug.Log("✅ MonsterCollection button listener added");
                }
                else
                {
                    if (showDebugLogs) Debug.LogWarning("⚠️ MonsterCollection button component not found!");
                }
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning("⚠️ MonsterCollection button GameObject not found!");
            }
        }
    }

    void SetupUI()
    {
        if (showDebugLogs) Debug.Log("🔧 Setting up UI components...");

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
            if (showDebugLogs) Debug.Log("✅ Back button listener added");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("⚠️ Back button not assigned!");
        }

        if (titleText != null)
        {
            titleText.text = "Monster Collection";
        }

        if (runeTabButton != null)
        {
            runeTabButton.onClick.AddListener(OnRuneTabClicked);
        }

        // Validate critical references
        if (monsterListContent == null)
        {
            Debug.LogError("❌ MonsterListContent not assigned!");
        }

        if (inventoryMonsterCardPrefab == null)
        {
            Debug.LogError("❌ InventoryMonsterCardPrefab not assigned!");
        }
    }

    void Setup3DDisplay()
    {
        if (monsterDisplay3D != null && renderTextureDisplay != null)
        {
            RenderTexture rt = monsterDisplay3D.GetRenderTexture();
            if (rt != null)
            {
                renderTextureDisplay.texture = rt;
                if (showDebugLogs) Debug.Log("✅ 3D Display setup complete");
            }
        }
    }

    void OnRuneTabClicked()
    {
        if (runePanelUI != null)
        {
            runePanelUI.gameObject.SetActive(!runePanelUI.gameObject.activeSelf);
        }
    }

    // 🆕 NEW: Manually add test monsters (Runtime compatible)
    void AddTestMonstersToCollection()
    {
        if (monsterCollectionManager == null)
        {
            if (showDebugLogs) Debug.LogWarning("⚠️ MonsterCollectionManager not found for test monster addition!");
            return;
        }

        if (testMonsterData.Count == 0)
        {
            if (showDebugLogs) Debug.LogWarning("⚠️ No test MonsterData assigned! Please assign in inspector.");
            return;
        }

        foreach (var monsterData in testMonsterData)
        {
            if (monsterData != null)
            {
                monsterCollectionManager.AddMonster(monsterData);
                if (showDebugLogs) Debug.Log($"🧪 Added test monster: {monsterData.monsterName}");
            }
        }

        if (showDebugLogs) Debug.Log($"🧪 Added {testMonsterData.Count} test monsters to collection");
    }

    public void OpenMonsterInventory()
    {
        if (showDebugLogs) Debug.Log("🎮 Opening Monster Inventory...");

        if (mainButtons != null)
        {
            mainButtons.SetActive(false);
            if (showDebugLogs) Debug.Log("🔽 MainButtons hidden");
        }

        if (inventoryMainPanel != null)
        {
            inventoryMainPanel.SetActive(true);
            if (showDebugLogs) Debug.Log("🔼 InventoryMainPanel opened");
        }

        LoadMonsterCollection();
    }

    // 🔧 ENHANCED: Better monster loading with filtering
    public void LoadMonsterCollection()
    {
        if (showDebugLogs) Debug.Log("📦 Loading monster collection...");

        if (monsterCollectionManager == null)
        {
            Debug.LogError("❌ MonsterCollectionManager not found!");
            ShowNoMonstersPanel();
            return;
        }

        List<CollectedMonster> collectedMonsters = monsterCollectionManager.GetAllMonsters();
        if (showDebugLogs) Debug.Log($"📊 MonsterCollectionManager returned {collectedMonsters.Count} monsters");

        // Debug each monster and filter valid ones
        var validMonsters = new List<CollectedMonster>();

        for (int i = 0; i < collectedMonsters.Count; i++)
        {
            var monster = collectedMonsters[i];

            if (monster == null)
            {
                if (showDebugLogs) Debug.LogWarning($"⚠️ Monster {i} is null!");
                continue;
            }

            if (monster.monsterData == null)
            {
                if (showDebugLogs) Debug.LogWarning($"⚠️ Monster {i} has null MonsterData! UniqueID: {monster.uniqueID}");
                continue;
            }

            // Valid monster
            validMonsters.Add(monster);
            if (showDebugLogs) Debug.Log($"🐉 Valid Monster {validMonsters.Count}: {monster.monsterData.monsterName} (Level {monster.level}, Stars {monster.currentStarLevel})");
        }

        if (showDebugLogs) Debug.Log($"📋 Valid monsters (with data): {validMonsters.Count}");

        if (validMonsters.Count == 0)
        {
            if (showDebugLogs) Debug.LogWarning("⚠️ No valid monsters found, showing NoMonstersPanel");
            ShowNoMonstersPanel();
            return;
        }

        ShowInventoryPanel();
        PopulateMonsterList(validMonsters);

        if (validMonsters.Count > 0)
        {
            SelectMonster(validMonsters[0]);
        }

        if (showDebugLogs) Debug.Log($"✅ Monster collection loaded successfully with {validMonsters.Count} valid monsters");
    }

    void ShowNoMonstersPanel()
    {
        if (showDebugLogs) Debug.Log("📄 Showing NoMonstersPanel");
        if (inventoryMainPanel != null) inventoryMainPanel.SetActive(false);
        if (noMonstersPanel != null) noMonstersPanel.SetActive(true);
    }

    void ShowInventoryPanel()
    {
        if (showDebugLogs) Debug.Log("📄 Showing InventoryPanel");
        if (noMonstersPanel != null) noMonstersPanel.SetActive(false);
        if (inventoryMainPanel != null) inventoryMainPanel.SetActive(true);
    }

    void PopulateMonsterList(List<CollectedMonster> monsters)
    {
        if (showDebugLogs) Debug.Log($"🃏 Populating monster list with {monsters.Count} monsters...");

        ClearMonsterList();

        if (monsterListContent == null)
        {
            Debug.LogError("❌ MonsterListContent is null! Cannot populate list.");
            return;
        }

        if (inventoryMonsterCardPrefab == null)
        {
            Debug.LogError("❌ InventoryMonsterCardPrefab is null! Cannot create cards.");
            return;
        }

        for (int i = 0; i < monsters.Count; i++)
        {
            var monster = monsters[i];
            if (showDebugLogs) Debug.Log($"🃏 Creating card {i} for {monster.monsterData.monsterName}...");

            try
            {
                GameObject cardObj = Instantiate(inventoryMonsterCardPrefab, monsterListContent);
                if (showDebugLogs) Debug.Log($"✅ Card GameObject created for {monster.monsterData.monsterName}");

                InventoryMonsterCard card = cardObj.GetComponent<InventoryMonsterCard>();
                if (card != null)
                {
                    card.Setup(monster, this);
                    monsterCards.Add(card);
                    if (showDebugLogs) Debug.Log($"✅ Card {i} setup complete for {monster.monsterData.monsterName}");
                }
                else
                {
                    Debug.LogError($"❌ InventoryMonsterCard component not found on prefab! Card {i}");
                    Destroy(cardObj);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error creating card {i}: {e.Message}");
            }
        }

        if (showDebugLogs) Debug.Log($"🃏 Created {monsterCards.Count} monster cards total");
        StartCoroutine(RefreshHorizontalLayoutNextFrame());
    }

    IEnumerator RefreshHorizontalLayoutNextFrame()
    {
        if (showDebugLogs) Debug.Log("🔄 Refreshing layout...");
        yield return null;

        if (monsterListContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(monsterListContent.GetComponent<RectTransform>());
            if (showDebugLogs) Debug.Log("✅ Layout rebuild complete");
        }

        if (monsterScrollView != null)
        {
            monsterScrollView.horizontalNormalizedPosition = 0f;
            if (showDebugLogs) Debug.Log("✅ Scroll position reset");
        }
    }

    void ClearMonsterList()
    {
        if (showDebugLogs) Debug.Log($"🧹 Clearing {monsterCards.Count} existing monster cards...");

        foreach (var card in monsterCards)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        monsterCards.Clear();
        if (showDebugLogs) Debug.Log("✅ Monster cards cleared");
    }

    public void SelectMonster(CollectedMonster monster)
    {
        if (showDebugLogs) Debug.Log($"🎯 Selecting monster: {monster.monsterData.monsterName}");
        currentSelectedMonster = monster;

        if (monsterDisplay3D != null)
        {
            monsterDisplay3D.DisplayMonster(monster.monsterData);
        }

        if (runePanelUI != null)
        {
            runePanelUI.SetCurrentMonster(monster);
        }

        UpdateStatsDisplay(monster);
        UpdateCardSelection(monster);
    }

    void UpdateStatsDisplay(CollectedMonster monster)
    {
        if (monster?.monsterData == null) return;

        if (monsterNameText != null)
        {
            monsterNameText.text = monster.monsterData.monsterName;
        }

        if (levelText != null)
        {
            levelText.text = $"Level {monster.level}";
        }

        if (elementText != null)
        {
            elementText.text = $"Element: {monster.monsterData.element}";
        }

        if (statsStarDisplay != null)
        {
            statsStarDisplay.SetStarLevel(monster.currentStarLevel);
        }

        // Calculate stats
        MonsterStats baseStats = new MonsterStats(monster.monsterData, monster.level, monster.currentStarLevel);
        MonsterStats totalStats = monster.GetEffectiveStats();

        // Calculate bonuses
        int healthBonus = totalStats.health - baseStats.health;
        int attackBonus = totalStats.attack - baseStats.attack;
        int defenseBonus = totalStats.defense - baseStats.defense;

        // Update stat displays
        if (healthStatText != null)
        {
            healthStatText.text = healthBonus > 0 ?
                $"Health: {baseStats.health} <color=#00FF00>+{healthBonus}</color>" :
                $"Health: {baseStats.health}";
        }

        if (attackStatText != null)
        {
            attackStatText.text = attackBonus > 0 ?
                $"Attack: {baseStats.attack} <color=#00FF00>+{attackBonus}</color>" :
                $"Attack: {baseStats.attack}";
        }

        if (defenseStatText != null)
        {
            defenseStatText.text = defenseBonus > 0 ?
                $"Defense: {baseStats.defense} <color=#00FF00>+{defenseBonus}</color>" :
                $"Defense: {baseStats.defense}";
        }

        if (duplicateInfoText != null && monsterCollectionManager != null)
        {
            int totalOfThisType = monsterCollectionManager.GetMonsterCount(monster.monsterData);
            duplicateInfoText.text = $"Owned: {totalOfThisType}";
            duplicateInfoText.gameObject.SetActive(true);
        }
    }

    void UpdateCardSelection(CollectedMonster selectedMonster)
    {
        foreach (var card in monsterCards)
        {
            if (card.GetMonster() == selectedMonster)
            {
                card.SetSelected(true);
                currentSelectedCard = card;
            }
            else
            {
                card.SetSelected(false);
            }
        }
    }

    void OnBackClicked()
    {
        if (showDebugLogs) Debug.Log("🔙 Back button clicked - closing Monster Inventory");

        if (inventoryMainPanel != null)
        {
            inventoryMainPanel.SetActive(false);
            if (showDebugLogs) Debug.Log("🔽 InventoryMainPanel closed");
        }

        if (mainButtons != null)
        {
            mainButtons.SetActive(true);
            if (showDebugLogs) Debug.Log("🔼 MainButtons shown");
        }
    }

    public RunePanelUI GetRunePanelUI()
    {
        return runePanelUI;
    }

    public void RefreshCurrentMonsterStats()
    {
        if (currentSelectedMonster != null)
        {
            UpdateStatsDisplay(currentSelectedMonster);
            if (showDebugLogs) Debug.Log($"Refreshed stats display for: {currentSelectedMonster.monsterData.monsterName}");
        }
    }

    public void OnMonsterCardClicked(CollectedMonster monster)
    {
        SelectMonster(monster);
    }

    void OnDisable()
    {
        if (monsterCollectionButton != null)
        {
            monsterCollectionButton.onClick.RemoveListener(OpenMonsterInventory);
        }
    }

    // Context menus for testing
    [ContextMenu("Test Open Monster Inventory")]
    public void TestOpenMonsterInventory()
    {
        OpenMonsterInventory();
    }

    [ContextMenu("Test Close Monster Inventory")]
    public void TestCloseMonsterInventory()
    {
        OnBackClicked();
    }

    [ContextMenu("Test Reload Monster Collection")]
    public void TestReloadMonsterCollection()
    {
        LoadMonsterCollection();
    }

    [ContextMenu("Test Add Test Monsters")]
    public void TestAddTestMonsters()
    {
        AddTestMonstersToCollection();
    }

    [ContextMenu("Debug Manager References")]
    public void DebugManagerReferences()
    {
        FindManagerReferences();
    }
}
