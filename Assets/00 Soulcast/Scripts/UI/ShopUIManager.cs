// Complete update voor Assets/00 Soulcast/Scripts/UI/ShopUIManager.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ShopUIManager : MonoBehaviour
{
    [Header("Shop UI References")]
    public GameObject shopMainPanel;
    public Button closeShopButton;

    [Header("External Buttons")] // ✅ NIEUW
    [Tooltip("Shop open button (will be found automatically if not assigned)")]
    public Button shopOpenButton;

    [Header("Category Buttons")]
    public Button normalCategoryButton;
    public Button buildingsCategoryButton;
    public Button honorCategoryButton;
    public Button efficientCategoryButton;

    [Header("Modular Category Panel")]
    public ShopCategoryPanel categoryPanel;

    [Header("Currency Display")]
    public TextMeshProUGUI soulCoinsDisplay;
    public TextMeshProUGUI premiumGemsDisplay;
    public TextMeshProUGUI honorPointsDisplay;

    [Header("Shop Data")]
    public ShopData shopData;

    [Header("Audio")]
    public AudioClip shopOpenSound;
    public AudioClip shopCloseSound;
    public AudioClip categorySelectSound;
    public AudioClip purchaseSound;
    public AudioClip purchaseFailSound;

    // Private variables
    private ShopCategory currentCategory = ShopCategory.Normal;
    private Dictionary<ShopCategory, Button> categoryButtons;

    // Events
    public System.Action<ShopItem> OnItemPurchased;
    public System.Action OnShopOpened;
    public System.Action OnShopClosed;

    void Awake()
    {
        SetupCategoryButtons();
        SetupEventHandlers();

        if (shopMainPanel != null)
        {
            shopMainPanel.SetActive(false);
        }
    }

    void Start()
    {
        InitializeCategoryPanel();
        LoadShopData();
        UpdateCurrencyDisplay();
        SetupButtonEvents();
    }

    /// <summary>
    /// Setup alle button onClick events programmatically voor consistentie
    /// </summary>
    void SetupButtonEvents()
    {
        // Setup shop open button
        SetupShopOpenButton();

        // Setup shop close button  
        SetupShopCloseButton();

        // Category buttons zijn al setup in SetupCategoryButtons()

        Debug.Log("🔘 All shop button events setup programmatically");
    }

    void SetupShopOpenButton()
    {
        // Zoek de shop open button automatisch
        Button shopOpenBtn = FindShopOpenButton();

        if (shopOpenBtn != null)
        {
            shopOpenBtn.onClick.RemoveAllListeners();
            shopOpenBtn.onClick.AddListener(() => {
                Debug.Log("🏪 Shop open button clicked");
                OpenShop();
            });

            Debug.Log("✅ Shop open button connected");
        }
        else
        {
            Debug.LogWarning("⚠️ Shop open button not found! Please assign manually in inspector");
        }
    }

    void SetupShopCloseButton()
    {
        if (closeShopButton != null)
        {
            closeShopButton.onClick.RemoveAllListeners();
            closeShopButton.onClick.AddListener(() => {
                Debug.Log("❌ Shop close button clicked");
                CloseShop();
            });

            Debug.Log("✅ Shop close button connected");
        }
        else
        {
            Debug.LogWarning("⚠️ Close shop button not assigned in inspector");
        }
    }

    /// <summary>
    /// Automatisch zoeken naar shop open button
    /// </summary>
    Button FindShopOpenButton()
    {
        // Zoek button met specifieke naam
        GameObject shopBtnObj = GameObject.Find("ShopBtn");
        if (shopBtnObj != null)
        {
            return shopBtnObj.GetComponent<Button>();
        }

        // Fallback: zoek in Canvas/MainButtons
        Transform canvas = FindAnyObjectByType<Canvas>()?.transform;
        if (canvas != null)
        {
            Transform mainButtons = canvas.Find("MainButtons");
            if (mainButtons != null)
            {
                Transform shopBtn = mainButtons.Find("ShopBtn");
                if (shopBtn != null)
                {
                    return shopBtn.GetComponent<Button>();
                }
            }
        }

        return null;
    }

    void SetupCategoryButtons()
    {
        categoryButtons = new Dictionary<ShopCategory, Button>
        {
            { ShopCategory.Normal, normalCategoryButton },
            { ShopCategory.Buildings, buildingsCategoryButton },
            { ShopCategory.Honor, honorCategoryButton },
            { ShopCategory.Efficient, efficientCategoryButton }
        };

        if (normalCategoryButton != null)
            normalCategoryButton.onClick.AddListener(() => SelectCategory(ShopCategory.Normal));
        if (buildingsCategoryButton != null)
            buildingsCategoryButton.onClick.AddListener(() => SelectCategory(ShopCategory.Buildings));
        if (honorCategoryButton != null)
            honorCategoryButton.onClick.AddListener(() => SelectCategory(ShopCategory.Honor));
        if (efficientCategoryButton != null)
            efficientCategoryButton.onClick.AddListener(() => SelectCategory(ShopCategory.Efficient));
    }

    void SetupEventHandlers()
    {
        if (closeShopButton != null)
        {
            closeShopButton.onClick.AddListener(CloseShop);
        }
    }

    void InitializeCategoryPanel()
    {
        if (categoryPanel != null)
        {
            categoryPanel.Initialize(this);
            categoryPanel.OnItemClicked += OnItemClicked;
        }
    }

    void LoadShopData()
    {
        if (shopData == null)
        {
            Debug.LogWarning("⚠️ No ShopData assigned!");
            return;
        }

        List<ShopItem> allItems = shopData.GetAllItems();

        if (categoryPanel != null)
        {
            categoryPanel.SetAllItems(allItems);
        }

        Debug.Log($"📦 Loaded {allItems.Count} total shop items into modular panel");
    }

    public void OpenShop()
    {
        if (shopMainPanel != null)
        {
            // ✅ IMPORTANT: Stop alle lopende animaties en reset scale
            shopMainPanel.transform.DOKill();
            shopMainPanel.transform.localScale = Vector3.one;

            // ✅ FIRST: Activate panel before any operations
            shopMainPanel.SetActive(true);

            Debug.Log("🏪 ShopMainPanel activated");

            // ✅ THEN: Setup category after panel is active
            SelectCategory(ShopCategory.Normal);

            // ✅ THEN: Start animation AFTER category is setup
            shopMainPanel.transform.localScale = Vector3.zero;
            shopMainPanel.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            // ✅ Refresh currency display when opening shop
            UpdateCurrencyDisplay();

            PlaySound(shopOpenSound);
            OnShopOpened?.Invoke();

            Debug.Log("✅ Shop opened with proper sequence");
        }
        else
        {
            Debug.LogError("❌ ShopMainPanel is null! Cannot open shop.");
        }
    }



    public void CloseShop()
    {
        if (shopMainPanel != null)
        {
            shopMainPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => shopMainPanel.SetActive(false));

            PlaySound(shopCloseSound);
            OnShopClosed?.Invoke();
        }
    }

    public void SelectCategory(ShopCategory category)
    {
        currentCategory = category;

        Debug.Log($"🏪 SelectCategory called: {category}");

        // ✅ SAFETY: Check if shop is actually open
        if (shopMainPanel == null || !shopMainPanel.activeInHierarchy)
        {
            Debug.LogWarning($"⚠️ Cannot select category {category}: ShopMainPanel is not active");
            return;
        }

        if (categoryPanel != null)
        {
            Debug.Log($"🔧 Configuring CategoryPanel for: {category}");
            categoryPanel.ConfigureForCategory(category);

            Debug.Log($"🔧 Showing CategoryPanel");
            categoryPanel.ShowPanel();

            Debug.Log($"✅ CategoryPanel configured and shown for {category}");
        }
        else
        {
            Debug.LogError($"❌ CategoryPanel is null! Cannot select category: {category}");
        }

        UpdateCategoryButtonVisuals();
        PlaySound(categorySelectSound);
    }


    void UpdateCategoryButtonVisuals()
    {
        foreach (var kvp in categoryButtons)
        {
            if (kvp.Value != null)
            {
                Image buttonImage = kvp.Value.GetComponent<Image>();
                TextMeshProUGUI buttonText = kvp.Value.GetComponentInChildren<TextMeshProUGUI>();

                bool isSelected = kvp.Key == currentCategory;

                if (isSelected)
                {
                    if (buttonImage != null)
                        buttonImage.color = new Color(1f, 0.8f, 0.2f, 1f);
                    if (buttonText != null)
                        buttonText.color = Color.white;
                }
                else
                {
                    if (buttonImage != null)
                        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                    if (buttonText != null)
                        buttonText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                }
            }
        }
    }

    void OnItemClicked(ShopItem item)
    {
        Debug.Log($"🛒 Item clicked: {item.itemName} (Category: {item.category}, Filter: {item.subFilter})");
        PurchaseItem(item);
    }

    /// <summary>
    /// Purchase an item from the shop
    /// </summary>
    public void PurchaseItem(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogError("❌ Cannot purchase: item is null");
            PlaySound(purchaseFailSound);
            return;
        }

        Debug.Log($"🛒 Attempting to purchase: {item.itemName}");

        // Check if player can afford the item
        if (!CanAffordItem(item))
        {
            Debug.LogWarning($"❌ Cannot afford {item.itemName} - Cost: {item.price} {item.currencyType}");
            PlaySound(purchaseFailSound);
            ShowNotAffordableMessage(item);
            return;
        }

        // Handle different item types
        switch (item.itemType)
        {
            case ShopItemType.Building:
                HandleBuildingPurchase(item);
                break;

            case ShopItemType.Monster:
                HandleConsumablePurchase(item);
                break;

            case ShopItemType.Rune:
                HandleEquipmentPurchase(item);
                break;

            case ShopItemType.Resource:
                HandleCurrencyPurchase(item);
                break;

            case ShopItemType.Upgrade:
                HandleUpgradePurchase(item);
                break;

            default:
                HandleGenericPurchase(item);
                break;
        }
    }

    void HandleBuildingPurchase(ShopItem item)
    {
        BuildingData buildingData = item.GetBuildingData();

        if (buildingData == null)
        {
            Debug.LogError($"❌ No BuildingData assigned to shop item: {item.itemName}");
            PlaySound(purchaseFailSound);
            return;
        }

        if (!buildingData.IsValidForGridPlacement())
        {
            Debug.LogError($"❌ BuildingData '{buildingData.buildingName}' is not properly configured for grid placement!");
            PlaySound(purchaseFailSound);
            return;
        }

        // Check if GridBuildingSystem exists
        if (GridBuildingSystem.Instance == null)
        {
            Debug.LogError("❌ GridBuildingSystem not found in scene!");
            PlaySound(purchaseFailSound);
            return;
        }

        // Check if player can afford the building (maar trek nog NIET af!)
        if (!CanAffordItem(item))
        {
            Debug.LogWarning($"❌ Cannot afford {item.itemName} - Cost: {item.price} {item.currencyType}");
            PlaySound(purchaseFailSound);
            ShowNotAffordableMessage(item);
            return;
        }

        // ✅ FIXED: Start building placement mode ZONDER currency af te trekken
        bool success = GridBuildingSystem.Instance.StartBuildingModeWithPurchase(buildingData, item, this);

        if (success)
        {
            PlaySound(purchaseSound);
            CloseShop();
            Debug.Log($"🏗️ Starting building placement for {buildingData.buildingName} (Payment pending placement)");
        }
        else
        {
            PlaySound(purchaseFailSound);
            Debug.LogError($"❌ Failed to start building placement for {buildingData.buildingName}");
        }
    }

    /// <summary>
    /// Deze methode wordt aangeroepen door GridBuildingSystem wanneer building succesvol geplaatst is
    /// </summary>
    public void OnBuildingSuccessfullyPlaced(ShopItem item)
    {
        // NU pas de currency aftrekken
        DeductItemCost(item);
        UpdateCurrencyDisplay();

        Debug.Log($"💰 Payment processed for {item.itemName} - {item.price} {item.currencyType}");
    }

    /// <summary>
    /// Deze methode wordt aangeroepen als building placement wordt geannuleerd
    /// </summary>
    public void OnBuildingPlacementCancelled(ShopItem item)
    {
        // Geen currency aftrekken - speler heeft building niet geplaatst
        Debug.Log($"🚫 Building placement cancelled for {item.itemName} - No payment processed");
    }



    void HandleConsumablePurchase(ShopItem item)
    {
        DeductItemCost(item);
        PlaySound(purchaseSound);
        OnItemPurchased?.Invoke(item);

        Debug.Log($"✅ Purchased consumable: {item.itemName}");
        UpdateCurrencyDisplay();
    }

    void HandleEquipmentPurchase(ShopItem item)
    {
        DeductItemCost(item);
        PlaySound(purchaseSound);
        OnItemPurchased?.Invoke(item);

        Debug.Log($"✅ Purchased equipment: {item.itemName}");
        UpdateCurrencyDisplay();
    }

    void HandleCurrencyPurchase(ShopItem item)
    {
        Debug.Log($"💎 Currency purchase initiated: {item.itemName}");
        PlaySound(purchaseSound);
        OnItemPurchased?.Invoke(item);
    }

    void HandleUpgradePurchase(ShopItem item)
    {
        DeductItemCost(item);
        PlaySound(purchaseSound);
        OnItemPurchased?.Invoke(item);

        Debug.Log($"⬆️ Purchased upgrade: {item.itemName}");
        UpdateCurrencyDisplay();
    }

    void HandleGenericPurchase(ShopItem item)
    {
        DeductItemCost(item);
        PlaySound(purchaseSound);
        OnItemPurchased?.Invoke(item);

        Debug.Log($"✅ Purchased item: {item.itemName}");
        UpdateCurrencyDisplay();
    }

    void DeductItemCost(ShopItem item)
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("❌ CurrencyManager not found!");
            return;
        }

        switch (item.currencyType)
        {
            case ShopCurrency.SoulCoins:
                CurrencyManager.Instance.SpendSoulCoins(item.price);
                Debug.Log($"💰 Spent {item.price} Soul Coins");
                break;

            case ShopCurrency.PremiumCrystals:
                CurrencyManager.Instance.SpendCrystals(item.price);
                Debug.Log($"💎 Spent {item.price} Premium Crystals");
                break;

            case ShopCurrency.HonorPoints:
                Debug.Log($"🏆 Spent {item.price} Honor Points (placeholder)");
                break;

            case ShopCurrency.SpecialCurrency:
                Debug.Log($"⭐ Spent {item.price} Special Currency (placeholder)");
                break;
        }
    }

    void ShowNotAffordableMessage(ShopItem item)
    {
        string currencyName = GetCurrencyDisplayName(item.currencyType);
        int currentAmount = GetCurrentCurrencyAmount(item.currencyType);
        int needed = item.price - currentAmount;

        Debug.LogWarning($"💸 Not enough {currencyName}! Need {needed} more to buy {item.itemName}");
    }

    string GetCurrencyDisplayName(ShopCurrency currencyType)
    {
        switch (currencyType)
        {
            case ShopCurrency.SoulCoins: return "Soul Coins";
            case ShopCurrency.PremiumCrystals: return "Premium Crystals";
            case ShopCurrency.HonorPoints: return "Honor Points";
            case ShopCurrency.SpecialCurrency: return "Special Currency";
            default: return "Currency";
        }
    }

    int GetCurrentCurrencyAmount(ShopCurrency currencyType)
    {
        if (CurrencyManager.Instance == null) return 0;

        switch (currencyType)
        {
            case ShopCurrency.SoulCoins:
                return CurrencyManager.Instance.GetSoulCoins();
            case ShopCurrency.PremiumCrystals:
                return CurrencyManager.Instance.GetCrystals();
            case ShopCurrency.HonorPoints:
                return 0;
            case ShopCurrency.SpecialCurrency:
                return 0;
            default:
                return 0;
        }
    }

    BuildingData GetBuildingDataByID(string buildingID)
    {
        BuildingData buildingData = Resources.Load<BuildingData>($"Buildings/{buildingID}");

        if (buildingData == null)
        {
            BuildingData[] allBuildings = Resources.LoadAll<BuildingData>("Buildings");
            foreach (var building in allBuildings)
            {
                if (building.name.Equals(buildingID, System.StringComparison.OrdinalIgnoreCase) ||
                    building.buildingName.Equals(buildingID, System.StringComparison.OrdinalIgnoreCase))
                {
                    return building;
                }
            }
        }

        return buildingData;
    }

    bool CanAffordItem(ShopItem item)
    {
        if (CurrencyManager.Instance == null) return false;

        switch (item.currencyType)
        {
            case ShopCurrency.SoulCoins:
                return CurrencyManager.Instance.GetSoulCoins() >= item.price;
            case ShopCurrency.PremiumCrystals:
                return CurrencyManager.Instance.GetCrystals() >= item.price;
            default:
                return false;
        }
    }

    void UpdateCurrencyDisplay()
    {
        if (CurrencyManager.Instance != null)
        {
            if (soulCoinsDisplay != null)
                soulCoinsDisplay.text = CurrencyManager.Instance.GetSoulCoins().ToString("N0");
            if (premiumGemsDisplay != null)
                premiumGemsDisplay.text = CurrencyManager.Instance.GetCrystals().ToString("N0");
        }

        if (honorPointsDisplay != null)
            honorPointsDisplay.text = "0";
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 0.6f);
        }
    }

    [ContextMenu("Refresh Shop Data")]
    public void RefreshShopData()
    {
        LoadShopData();
        SelectCategory(currentCategory);
    }
}
