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
    public TextMeshProUGUI speedStatText;
    public TextMeshProUGUI critRateText;
    public TextMeshProUGUI critDamageText;
    public TextMeshProUGUI accuracyStatText;
    public TextMeshProUGUI resistanceStatText;
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

    // 🔧 ENHANCED: Better monster selection with comprehensive debugging
    public void SelectMonster(CollectedMonster monster)
    {
        if (showDebugLogs) Debug.Log($"🎯 SelectMonster called with: {(monster != null ? monster.monsterData.monsterName : "NULL")}");

        if (monster == null)
        {
            Debug.LogError("❌ Cannot select null monster!");
            return;
        }

        if (monster.monsterData == null)
        {
            Debug.LogError($"❌ Cannot select monster with null MonsterData! UniqueID: {monster.uniqueID}");
            return;
        }

        currentSelectedMonster = monster;
        if (showDebugLogs) Debug.Log($"✅ currentSelectedMonster set to: {monster.monsterData.monsterName} (ID: {monster.uniqueID})");

        // Update 3D display
        if (monsterDisplay3D != null)
        {
            monsterDisplay3D.DisplayMonster(monster.monsterData);
            if (showDebugLogs) Debug.Log($"✅ 3D display updated for: {monster.monsterData.monsterName}");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("⚠️ monsterDisplay3D is null!");
        }

        // 🔧 ENHANCED: Better RunePanelUI updating with validation
        if (runePanelUI != null)
        {
            if (showDebugLogs) Debug.Log($"🔧 Setting current monster on RunePanelUI: {monster.monsterData.monsterName}");
            runePanelUI.SetCurrentMonster(monster);

            // 🆕 NEW: Verify the monster was set correctly
            CollectedMonster verifyMonster = runePanelUI.GetCurrentMonster();
            if (verifyMonster == monster)
            {
                if (showDebugLogs) Debug.Log($"✅ RunePanelUI current monster verified: {verifyMonster.monsterData.monsterName}");
            }
            else
            {
                Debug.LogError($"❌ RunePanelUI current monster verification FAILED! Expected: {monster.monsterData.monsterName}, Got: {(verifyMonster != null ? verifyMonster.monsterData.monsterName : "NULL")}");
            }
        }
        else
        {
            Debug.LogError("❌ runePanelUI is null! Cannot set current monster for rune operations.");
        }

        UpdateStatsDisplay(monster);
        UpdateCardSelection(monster);

        // 🆕 NEW: Log final selection state
        if (showDebugLogs) Debug.Log($"🎯 Monster selection complete. Current monster: {currentSelectedMonster.monsterData.monsterName}");
    }

    void UpdateStatsDisplay(CollectedMonster monster)
    {
        if (monster?.monsterData == null) return;

        if (monsterNameText != null)
        {
            monsterNameText.text = monster.monsterData.monsterName;
        }

        Debug.Log($"📋 Level: {monster.level}, Star: {monster.currentStarLevel}");

        // Force create a NEW MonsterStats to trigger the constructor debug
        MonsterStats baseStats = new MonsterStats(monster.monsterData, monster.level, monster.currentStarLevel);

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
        MonsterStats totalStats = monster.GetEffectiveStats();

        // Calculate bonuses
        int healthBonus = totalStats.health - baseStats.health;
        int attackBonus = totalStats.attack - baseStats.attack;
        int defenseBonus = totalStats.defense - baseStats.defense;
        int speedBonus = totalStats.speed - baseStats.speed;

        // Update stat displays
        if (healthStatText != null)
        {
            healthStatText.text = healthBonus > 0 ?
                $"HP: {baseStats.health} <color=#00FF00>+{healthBonus}</color>" :
                $"HP: {baseStats.health}";
        }

        if (attackStatText != null)
        {
            attackStatText.text = attackBonus > 0 ?
                $"ATK: {baseStats.attack} <color=#00FF00>+{attackBonus}</color>" :
                $"ATK: {baseStats.attack}";
        }

        if (defenseStatText != null)
        {
            defenseStatText.text = defenseBonus > 0 ?
                $"DEF: {baseStats.defense} <color=#00FF00>+{defenseBonus}</color>" :
                $"DEF: {baseStats.defense}";
        }

        if (speedStatText != null)
        {
            speedStatText.text = speedBonus > 0 ?
                $"SPD: {baseStats.speed} <color=#00FF00>+{defenseBonus}</color>" :
                $"SPD: {baseStats.speed}";
        }

        if (critRateText != null)
        {
            critRateText.text = $"CRIT RATE: {totalStats.criticalRate}%";
        }

        if (critDamageText != null)
        {
            critDamageText.text = $"Resistance: {totalStats.criticalDamage}%";
        }

        if (resistanceStatText != null)
        {
            resistanceStatText.text = $"Resistance: {totalStats.resistance}%";
        }

        if (accuracyStatText != null)
        {
            accuracyStatText.text = $"Accuracy: {totalStats.accuracy}%";
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

    // 🔧 ENHANCED: Better OnMonsterCardClicked with debugging
    public void OnMonsterCardClicked(CollectedMonster monster)
    {
        if (showDebugLogs) Debug.Log($"🖱️ Monster card clicked: {(monster != null ? monster.monsterData.monsterName : "NULL")}");

        if (monster == null)
        {
            Debug.LogError("❌ OnMonsterCardClicked called with null monster!");
            return;
        }

        SelectMonster(monster);
    }

    // 🆕 NEW: Get current selected monster (for debugging)
    public CollectedMonster GetCurrentSelectedMonster()
    {
        return currentSelectedMonster;
    }

    void OnDisable()
    {
        if (monsterCollectionButton != null)
        {
            monsterCollectionButton.onClick.RemoveListener(OpenMonsterInventory);
        }
    }

    // 🆕 NEW: Enhanced context menus for testing
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

    [ContextMenu("Debug Current Selection State")]
    public void DebugCurrentSelectionState()
    {
        Debug.Log("=== Monster Selection Debug ===");
        Debug.Log($"currentSelectedMonster: {(currentSelectedMonster != null ? currentSelectedMonster.monsterData.monsterName : "NULL")}");
        Debug.Log($"runePanelUI: {(runePanelUI != null ? "Found" : "NULL")}");

        if (runePanelUI != null)
        {
            CollectedMonster runePanelMonster = runePanelUI.GetCurrentMonster();
            Debug.Log($"runePanelUI.GetCurrentMonster(): {(runePanelMonster != null ? runePanelMonster.monsterData.monsterName : "NULL")}");
            Debug.Log($"Monsters match: {(currentSelectedMonster == runePanelMonster)}");
        }

        Debug.Log($"currentSelectedCard: {(currentSelectedCard != null ? "Found" : "NULL")}");
        Debug.Log($"monsterCards.Count: {monsterCards.Count}");
        Debug.Log("=== End Debug ===");
    }

    [ContextMenu("Test Monster Card Click Simulation")]
    public void TestMonsterCardClickSimulation()
    {
        if (monsterCards.Count > 0)
        {
            var firstCard = monsterCards[0];
            var monster = firstCard.GetMonster();
            Debug.Log($"🧪 Simulating card click for: {monster.monsterData.monsterName}");
            OnMonsterCardClicked(monster);
        }
    }
}
