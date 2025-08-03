using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading;

public class MonsterInventoryUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inventoryMainPanel;
    public GameObject noMonstersPanel;
    public GameObject mainGachaPanel;

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
    public StarDisplay statsStarDisplay; // NEW: Star display in stats panel

    [Header("Rune Panel")]
    public RunePanelUI runePanelUI;
    public Button runeTabButton; // New tab button for runes

    [Header("Monster List")]
    public Transform monsterListContent;
    public GameObject inventoryMonsterCardPrefab;
    public ScrollRect monsterScrollView;

    [Header("Selection")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Scroll Buttons")]
    public float scrollAmount = 200f;

    private List<InventoryMonsterCard> monsterCards = new List<InventoryMonsterCard>();
    private CollectedMonster currentSelectedMonster;
    private InventoryMonsterCard currentSelectedCard;

    void Start()
    {
        SetupUI();
        Setup3DDisplay();
        LoadMonsterCollection();
    }

    void SetupUI()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (titleText != null)
        {
            titleText.text = "Monster Collection";
        }

        if (runeTabButton != null)
        {
            runeTabButton.onClick.AddListener(OnRuneTabClicked);
        }
    }

    void Setup3DDisplay()
    {
        // Connect the render texture to the raw image
        if (monsterDisplay3D != null && renderTextureDisplay != null)
        {
            RenderTexture rt = monsterDisplay3D.GetRenderTexture();
            if (rt != null)
            {
                renderTextureDisplay.texture = rt;
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

    public void LoadMonsterCollection()
    {
        if (PlayerInventory.Instance == null)
        {
            ShowNoMonstersPanel();
            return;
        }

        List<CollectedMonster> collectedMonsters = PlayerInventory.Instance.GetAllMonsters();

        if (collectedMonsters.Count == 0)
        {
            ShowNoMonstersPanel();
            return;
        }

        // Show main panel and populate list
        ShowInventoryPanel();
        PopulateMonsterList(collectedMonsters);

        // Select first monster by default
        if (collectedMonsters.Count > 0)
        {
            SelectMonster(collectedMonsters[0]);
        }
    }

    void ShowNoMonstersPanel()
    {
        if (inventoryMainPanel != null) inventoryMainPanel.SetActive(false);
        if (noMonstersPanel != null) noMonstersPanel.SetActive(true);
    }

    void ShowInventoryPanel()
    {
        if (noMonstersPanel != null) noMonstersPanel.SetActive(false);
        if (inventoryMainPanel != null) inventoryMainPanel.SetActive(true);
    }

    void PopulateMonsterList(List<CollectedMonster> monsters)
    {
        // Clear existing cards
        ClearMonsterList();

        // CHANGED: Create cards for each individual monster (including duplicates)
        foreach (var monster in monsters)
        {
            GameObject cardObj = Instantiate(inventoryMonsterCardPrefab, monsterListContent);
            InventoryMonsterCard card = cardObj.GetComponent<InventoryMonsterCard>();

            if (card != null)
            {
                card.Setup(monster, this);
                monsterCards.Add(card);
            }
        }

        // Force layout rebuild for horizontal layout
        StartCoroutine(RefreshHorizontalLayoutNextFrame());
    }

    // Updated helper coroutine for horizontal layout
    IEnumerator RefreshHorizontalLayoutNextFrame()
    {
        yield return null; // Wait one frame

        // Force layout system to recalculate
        LayoutRebuilder.ForceRebuildLayoutImmediate(monsterListContent.GetComponent<RectTransform>());

        // Reset scroll position to left (start)
        if (monsterScrollView != null)
        {
            monsterScrollView.horizontalNormalizedPosition = 0f;
        }
    }


    void ClearMonsterList()
    {
        foreach (var card in monsterCards)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        monsterCards.Clear();
    }

    public void SelectMonster(CollectedMonster monster)
    {
        currentSelectedMonster = monster;

        // Update 3D display
        if (monsterDisplay3D != null)
        {
            monsterDisplay3D.DisplayMonster(monster.monsterData);
        }

        if (runePanelUI != null)
        {
            runePanelUI.SetCurrentMonster(monster);
        }

        // Update stats panel
        UpdateStatsDisplay(monster);

        // Update card selection visuals
        UpdateCardSelection(monster);
    }

    // REPLACE the UpdateStatsDisplay method in MonsterInventoryUI.cs:
    void UpdateStatsDisplay(CollectedMonster monster)
    {
        if (monster?.monsterData == null) return;

        // Basic info
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

        // Update star display in stats panel
        if (statsStarDisplay != null)
        {
            statsStarDisplay.SetStarLevel(monster.currentStarLevel);
        }

        // Calculate base stats (without runes) and total stats (with runes)
        MonsterStats baseStats = new MonsterStats(monster.monsterData, monster.level, monster.currentStarLevel);
        MonsterStats totalStats = monster.GetEffectiveStats();

        // Calculate bonus stats from runes
        int healthBonus = totalStats.health - baseStats.health;
        int attackBonus = totalStats.attack - baseStats.attack;
        int defenseBonus = totalStats.defense - baseStats.defense;

        // Update health stat with bonus display
        if (healthStatText != null)
        {
            if (healthBonus > 0)
            {
                healthStatText.text = $"Health: {baseStats.health} <color=#00FF00>+{healthBonus}</color>";
            }
            else
            {
                healthStatText.text = $"Health: {baseStats.health}";
            }
        }

        // Update attack stat with bonus display
        if (attackStatText != null)
        {
            if (attackBonus > 0)
            {
                attackStatText.text = $"Attack: {baseStats.attack} <color=#00FF00>+{attackBonus}</color>";
            }
            else
            {
                attackStatText.text = $"Attack: {baseStats.attack}";
            }
        }

        // Update defense stat with bonus display
        if (defenseStatText != null)
        {
            if (defenseBonus > 0)
            {
                defenseStatText.text = $"Defense: {baseStats.defense} <color=#00FF00>+{defenseBonus}</color>";
            }
            else
            {
                defenseStatText.text = $"Defense: {baseStats.defense}";
            }
        }

        // Show monster instance info
        if (duplicateInfoText != null)
        {
            int totalOfThisType = PlayerInventory.Instance.GetMonsterCount(monster.monsterData);
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
        // Hide inventory panel
        if (inventoryMainPanel != null)
        {
            inventoryMainPanel.SetActive(false);
        }

        // Show main gacha panel using direct reference
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(true);
        }
        else
        {
            // Fallback: try to find it
            GameObject gachaPanel = GameObject.Find("Canvas").transform.Find("MainGachaUI").gameObject;
            if (gachaPanel != null)
            {
                gachaPanel.SetActive(true);
            }
        }
    }

    // ADD this method to MonsterInventoryUI.cs:
    public RunePanelUI GetRunePanelUI()
    {
        return runePanelUI;
    }

    // ADD this method to MonsterInventoryUI.cs (place it after the GetRunePanelUI method):
    public void RefreshCurrentMonsterStats()
    {
        if (currentSelectedMonster != null)
        {
            UpdateStatsDisplay(currentSelectedMonster);
            Debug.Log($"Refreshed stats display for: {currentSelectedMonster.monsterData.monsterName}");
        }
    }

    // Public method for cards to call when clicked
    public void OnMonsterCardClicked(CollectedMonster monster)
    {
        SelectMonster(monster);
    }
}