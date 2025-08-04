using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CollectionUI : MonoBehaviour
{
    [Header("Collection Display")]
    public Transform collectionGrid;
    public GameObject collectionCardPrefab;
    public TextMeshProUGUI collectionCountText;
    public TMP_Dropdown sortDropdown;
    public TMP_Dropdown filterDropdown;

    [Header("Navigation")]
    public Button backButton;
    public Button gachaButton;

    private List<CollectionCard> currentCards = new List<CollectionCard>();
    private List<CollectedMonster> allMonsters = new List<CollectedMonster>();

    void Start()
    {
        SetupDropdowns();
        SetupButtons();
        RefreshCollection();
    }

    void SetupDropdowns()
    {
        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string>
            {
                "Date Obtained",
                "Name A-Z",
                "Name Z-A",
                "Rarity",
                "Element"
            });
            sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }

        if (filterDropdown != null)
        {
            filterDropdown.ClearOptions();
            filterDropdown.AddOptions(new List<string>
            {
                "All",
                "Fire",
                "Water",
                "Earth",
                "Common",
                "Uncommon",
                "Rare",
                "Epic",
                "Legendary"
            });
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }
    }

    void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (gachaButton != null)
        {
            gachaButton.onClick.AddListener(OnGachaClicked);
        }
    }

    public void RefreshCollection()
    {
        if (PlayerInventory.Instance == null) return;

        allMonsters = PlayerInventory.Instance.GetAllMonsters();
        DisplayCollection(allMonsters);
        UpdateCollectionCount();
    }

    void DisplayCollection(List<CollectedMonster> monstersToShow)
    {
        ClearCollection();

        foreach (var monster in monstersToShow)
        {
            GameObject cardObj = Instantiate(collectionCardPrefab, collectionGrid);
            CollectionCard card = cardObj.GetComponent<CollectionCard>();

            if (card != null)
            {
                card.Setup(monster);
                currentCards.Add(card);
            }
        }
    }

    void ClearCollection()
    {
        foreach (var card in currentCards)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        currentCards.Clear();
    }

    void UpdateCollectionCount()
    {
        if (collectionCountText != null && PlayerInventory.Instance != null)
        {
            int totalCount = PlayerInventory.Instance.GetCollectionCount();
            int uniqueCount = PlayerInventory.Instance.GetUniqueMonsters().Count;
            collectionCountText.text = $"Collection: {uniqueCount} Unique / {totalCount} Total";
        }
    }

    void OnSortChanged(int sortIndex)
    {
        List<CollectedMonster> sortedMonsters = new List<CollectedMonster>(allMonsters);

        switch (sortIndex)
        {
            case 0: // Date Obtained
                sortedMonsters = sortedMonsters.OrderByDescending(m => m.dateObtained).ToList();
                break;
            case 1: // Name A-Z
                sortedMonsters = sortedMonsters.OrderBy(m => m.monsterData.monsterName).ToList();
                break;
            case 2: // Name Z-A
                sortedMonsters = sortedMonsters.OrderByDescending(m => m.monsterData.monsterName).ToList();
                break;
            case 3: // Rarity
                // You'll need to add rarity info to CollectedMonster or get it from GachaManager
                break;
            case 4: // Element
                sortedMonsters = sortedMonsters.OrderBy(m => m.monsterData.element).ToList();
                break;
        }

        DisplayCollection(sortedMonsters);
    }

    void OnFilterChanged(int filterIndex)
    {
        List<CollectedMonster> filteredMonsters = new List<CollectedMonster>(allMonsters);

        switch (filterIndex)
        {
            case 0: // All
                break;
            case 1: // Fire
                filteredMonsters = filteredMonsters.Where(m => m.monsterData.element == ElementType.Fire).ToList();
                break;
            case 2: // Water
                filteredMonsters = filteredMonsters.Where(m => m.monsterData.element == ElementType.Water).ToList();
                break;
            case 3: // Earth
                filteredMonsters = filteredMonsters.Where(m => m.monsterData.element == ElementType.Earth).ToList();
                break;
                // Add rarity filters if needed
        }

        DisplayCollection(filteredMonsters);
    }

    void OnBackClicked()
    {
        // Return to previous scene or close collection
        Debug.Log("Returning from collection...");
    }

    void OnGachaClicked()
    {
        // Go to gacha scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("GachaScene");
    }
}
