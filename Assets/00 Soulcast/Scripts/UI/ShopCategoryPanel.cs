// Update: Assets/00 Soulcast/Scripts/UI/ShopCategoryPanel.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ShopCategoryPanel : MonoBehaviour
{
    [Header("Panel UI References")]
    public GameObject panelRoot;

    // REMOVED: Category Title, Filter Text, and Item Count Text
    // These UI elements are now hidden/disabled

    [Header("Filter System")]
    public Transform filterButtonContainer;
    public Button filterButtonPrefab;

    [Header("Item Display")]
    public ScrollRect itemScrollRect;
    public Transform itemContainer;
    public GameObject shopItemPrefab;

    [Header("Layout Settings")]
    public bool useHorizontalLayout = true;
    public float itemSpacing = 10f;

    [Header("Category Filter Configuration")]
    public CategoryFilterConfig[] categoryConfigs;

    // Private variables
    private ShopCategory currentCategory;
    private List<ShopItem> allShopItems = new List<ShopItem>();
    private List<ShopItem> currentCategoryItems = new List<ShopItem>();
    private List<ShopItemUI> currentItemUIs = new List<ShopItemUI>();
    private string currentFilter = "All";
    private Dictionary<string, Button> filterButtons = new Dictionary<string, Button>();
    private ShopUIManager shopManager;
    private string[] currentAvailableFilters;

    // Events
    public System.Action<ShopItem> OnItemClicked;

    void Awake()
    {
        SetupItemContainer();
        HideUnnecessaryUIElements();
    }

    public void Initialize(ShopUIManager manager)
    {
        shopManager = manager;
    }

    public void SetAllItems(List<ShopItem> allItems)
    {
        allShopItems = allItems;
    }

    /// <summary>
    /// Hide UI elements that are no longer needed
    /// </summary>
    void HideUnnecessaryUIElements()
    {
        // Find and hide category title text
        TextMeshProUGUI categoryTitle = transform.Find("CategoryTitle")?.GetComponent<TextMeshProUGUI>();
        if (categoryTitle != null)
        {
            categoryTitle.gameObject.SetActive(false);
            Debug.Log("🙈 Hidden Category Title");
        }

        // Find and hide current filter text
        TextMeshProUGUI currentFilterDisplay = transform.Find("CurrentFilter")?.GetComponent<TextMeshProUGUI>();
        if (currentFilterDisplay != null)
        {
            currentFilterDisplay.gameObject.SetActive(false);
            Debug.Log("🙈 Hidden Current Filter Text");
        }

        // Find and hide item count text
        TextMeshProUGUI itemCountDisplay = transform.Find("ItemCount")?.GetComponent<TextMeshProUGUI>();
        if (itemCountDisplay != null)
        {
            itemCountDisplay.gameObject.SetActive(false);
            Debug.Log("🙈 Hidden Item Count Text");
        }

        // Also try common naming patterns
        HideTextElementByName("Category Title");
        HideTextElementByName("CategoryTitleText");
        HideTextElementByName("Filter Text");
        HideTextElementByName("CurrentFilterText");
        HideTextElementByName("Item Count");
        HideTextElementByName("ItemCountText");
    }

    /// <summary>
    /// Helper method to hide text elements by name
    /// </summary>
    void HideTextElementByName(string elementName)
    {
        TextMeshProUGUI textElement = GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(tmp => tmp.name.Contains(elementName));

        if (textElement != null)
        {
            textElement.gameObject.SetActive(false);
            Debug.Log($"🙈 Hidden UI element: {elementName}");
        }
    }

    /// <summary>
    /// Configure panel for specific category
    /// </summary>
    public void ConfigureForCategory(ShopCategory category)
    {
        currentCategory = category;

        // Get configuration for this category
        CategoryFilterConfig config = GetConfigForCategory(category);

        // Setup filters for this category
        currentAvailableFilters = config.availableFilters;
        SetupFilterButtons();

        // Filter items for this category
        FilterItemsForCategory();

        // ✅ UPDATED: Select first filter or "All" if no filters
        if (currentAvailableFilters != null && currentAvailableFilters.Length > 0)
        {
            SelectFilter(currentAvailableFilters[0]); // Select first filter
        }
        else
        {
            currentFilter = "All"; // No filters, show all items
            RefreshItemDisplay();
        }

        Debug.Log($"🔧 Configured panel for category: {category} (no 'All' button)");
    }

    CategoryFilterConfig GetConfigForCategory(ShopCategory category)
    {
        // Find config in array
        foreach (CategoryFilterConfig config in categoryConfigs)
        {
            if (config.category == category)
            {
                return config;
            }
        }

        // Return default config if not found
        return new CategoryFilterConfig
        {
            category = category,
            categoryDisplayName = category.ToString(),
            availableFilters = new string[0]
        };
    }

    void FilterItemsForCategory()
    {
        currentCategoryItems = allShopItems.Where(item => item.category == currentCategory).ToList();
        Debug.Log($"📦 Filtered {currentCategoryItems.Count} items for category {currentCategory}");
    }

    void SetupFilterButtons()
    {
        // Clear existing buttons
        ClearFilterButtons();

        // Only create filter buttons if filters are specified
        if (currentAvailableFilters != null && currentAvailableFilters.Length > 0)
        {
            // ✅ REMOVED: No more "All" filter button
            // Only create specific filter buttons
            foreach (string filter in currentAvailableFilters)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    CreateFilterButton(filter);
                }
            }

            filterButtonContainer.gameObject.SetActive(true);

            Debug.Log($"🔘 Created {currentAvailableFilters.Length} filter buttons (no 'All' button)");
        }
        else
        {
            // Hide filter container if no filters
            filterButtonContainer.gameObject.SetActive(false);
            Debug.Log("🔘 No filters available - hiding filter container");
        }
    }


    void CreateFilterButton(string filterName)
    {
        if (filterButtonPrefab == null || filterButtonContainer == null) return;

        GameObject buttonObj = Instantiate(filterButtonPrefab.gameObject, filterButtonContainer);
        Button button = buttonObj.GetComponent<Button>();

        if (button != null)
        {
            // Setup button text
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = filterName;
            }

            // Setup button click
            button.onClick.AddListener(() => SelectFilter(filterName));

            // Store button reference
            filterButtons[filterName] = button;
        }
    }

    void ClearFilterButtons()
    {
        filterButtons.Clear();

        if (filterButtonContainer != null)
        {
            foreach (Transform child in filterButtonContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    public void SelectFilter(string filterName)
    {
        currentFilter = filterName;

        // NO LONGER UPDATE FILTER DISPLAY TEXT - It's hidden

        // Update button visuals
        UpdateFilterButtonVisuals();

        // Refresh item display
        RefreshItemDisplay();

        Debug.Log($"🔽 Selected filter '{filterName}' for category {currentCategory} (filter text hidden)");
    }

    void UpdateFilterButtonVisuals()
    {
        foreach (var kvp in filterButtons)
        {
            Button button = kvp.Value;
            bool isSelected = kvp.Key == currentFilter;

            if (button != null)
            {
                Image buttonImage = button.GetComponent<Image>();
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

                if (isSelected)
                {
                    // Selected state - Golden
                    if (buttonImage != null)
                        buttonImage.color = new Color(1f, 0.8f, 0.2f, 1f);
                    if (buttonText != null)
                        buttonText.color = Color.white;
                }
                else
                {
                    // Unselected state - Gray
                    if (buttonImage != null)
                        buttonImage.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                    if (buttonText != null)
                        buttonText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                }
            }
        }
    }

    void RefreshItemDisplay()
    {
        // Clear existing items
        ClearItemDisplay();

        // Filter items based on current filter
        List<ShopItem> filteredItems = GetFilteredItems();

        // NO LONGER UPDATE ITEM COUNT TEXT - It's hidden

        // Create UI items
        foreach (ShopItem item in filteredItems)
        {
            CreateItemUI(item);
        }

        // Reset scroll position
        if (itemScrollRect != null)
        {
            if (useHorizontalLayout)
                itemScrollRect.horizontalNormalizedPosition = 0f;
            else
                itemScrollRect.verticalNormalizedPosition = 1f;
        }

        Debug.Log($"🔄 Refreshed display: {filteredItems.Count} items with filter '{currentFilter}' (count text hidden)");
    }

    List<ShopItem> GetFilteredItems()
    {
        // If no specific filter is set or no filters available, show all category items
        if (string.IsNullOrEmpty(currentFilter) ||
            currentFilter == "All" ||
            currentAvailableFilters == null ||
            currentAvailableFilters.Length == 0)
        {
            return currentCategoryItems;
        }

        // Filter items based on subFilter
        return currentCategoryItems.Where(item =>
            item.subFilter.Equals(currentFilter, System.StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    void CreateItemUI(ShopItem item)
    {
        if (shopItemPrefab == null || itemContainer == null) return;

        GameObject itemObj = Instantiate(shopItemPrefab, itemContainer);
        ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(item, shopManager);
            currentItemUIs.Add(itemUI);
        }
    }

    void ClearItemDisplay()
    {
        // Clear UI list
        currentItemUIs.Clear();

        // Destroy GameObjects
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    void SetupItemContainer()
    {
        if (itemContainer != null)
        {
            // Setup layout based on settings
            HorizontalLayoutGroup horizontalLayout = itemContainer.GetComponent<HorizontalLayoutGroup>();
            VerticalLayoutGroup verticalLayout = itemContainer.GetComponent<VerticalLayoutGroup>();

            if (useHorizontalLayout)
            {
                if (horizontalLayout == null)
                    horizontalLayout = itemContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

                horizontalLayout.spacing = itemSpacing;
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childControlHeight = false;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childForceExpandHeight = false;

                // Remove vertical layout if exists
                if (verticalLayout != null)
                    DestroyImmediate(verticalLayout);
            }
            else
            {
                if (verticalLayout == null)
                    verticalLayout = itemContainer.gameObject.AddComponent<VerticalLayoutGroup>();

                verticalLayout.spacing = itemSpacing;
                verticalLayout.childControlWidth = false;
                verticalLayout.childControlHeight = false;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;

                // Remove horizontal layout if exists
                if (horizontalLayout != null)
                    DestroyImmediate(horizontalLayout);
            }
        }
    }

    public void ShowPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}

/// <summary>
/// Configuration for category filters
/// </summary>
[System.Serializable]
public class CategoryFilterConfig
{
    [Header("Category Configuration")]
    public ShopCategory category;
    public string categoryDisplayName;

    [Header("Available Filters")]
    [Tooltip("Leave empty for no filters. 'All' filter is automatically added.")]
    public string[] availableFilters;
}
