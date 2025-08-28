using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText;
    public Button purchaseButton;
    public Image currencyIcon;
    public GameObject soldOutOverlay;
    public GameObject limitedBadge;

    [Header("Visual States")]
    public Color normalColor = Color.white;
    public Color soldOutColor = Color.gray;
    public Color expensiveColor = Color.red;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private ShopItem shopItem;
    private ShopUIManager shopManager;

    void OnEnable()
    {
        // Subscribe to currency changes for realtime updates
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.OnSoulCoinsChanged += OnCurrencyChanged;
            CurrencyManager.OnCrystalsChanged += OnCurrencyChanged;
        }
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.OnSoulCoinsChanged -= OnCurrencyChanged;
            CurrencyManager.OnCrystalsChanged -= OnCurrencyChanged;
        }
    }

    public void Setup(ShopItem item, ShopUIManager manager)
    {
        shopItem = item;
        shopManager = manager;

        UpdateItemDisplay();
        SetupPurchaseButton();

        if (showDebugLogs)
            Debug.Log($"🏪 ShopItemUI setup for: {item?.itemName ?? "Unknown"}");
    }

    void UpdateItemDisplay()
    {
        if (shopItem == null) return;

        // Basic info
        if (itemNameText != null)
            itemNameText.text = shopItem.itemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = shopItem.description;

        if (itemIcon != null && shopItem.itemIcon != null)
            itemIcon.sprite = shopItem.itemIcon;

        // Price and currency
        if (priceText != null)
            priceText.text = shopItem.price.ToString("N0");

        UpdateCurrencyIcon();

        // Stock info
        if (stockText != null)
        {
            if (shopItem.isLimitedQuantity)
            {
                stockText.text = $"Stock: {shopItem.currentStock}/{shopItem.maxQuantity}";
                stockText.gameObject.SetActive(true);

                if (limitedBadge != null)
                    limitedBadge.SetActive(true);
            }
            else
            {
                stockText.gameObject.SetActive(false);

                if (limitedBadge != null)
                    limitedBadge.SetActive(false);
            }
        }

        // Availability
        bool canPurchase = shopItem.CanPurchase() && HasEnoughCurrency();

        if (soldOutOverlay != null)
            soldOutOverlay.SetActive(!shopItem.CanPurchase());

        // Visual state
        Color targetColor = normalColor;
        if (!shopItem.CanPurchase())
        {
            targetColor = soldOutColor;
        }
        else if (!HasEnoughCurrency())
        {
            targetColor = expensiveColor;
        }

        if (itemIcon != null)
            itemIcon.color = targetColor;

        // Purchase button
        if (purchaseButton != null)
        {
            purchaseButton.interactable = canPurchase;

            var buttonText = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (!shopItem.CanPurchase())
                    buttonText.text = "SOLD OUT";
                else if (!HasEnoughCurrency())
                    buttonText.text = "CAN'T AFFORD";
                else
                    buttonText.text = "PURCHASE";
            }
        }
    }

    void UpdateCurrencyIcon()
    {
        if (currencyIcon == null) return;

        // You can set different currency icons here
        switch (shopItem.currencyType)
        {
            case ShopCurrency.SoulCoins:
                // Set soul coins icon
                break;
            case ShopCurrency.PremiumCrystals:
                // Set gems icon  
                break;
            case ShopCurrency.HonorPoints:
                // Set honor points icon
                break;
        }
    }

    void SetupPurchaseButton()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(() => OnPurchaseClicked());

            if (showDebugLogs)
                Debug.Log($"🔘 Purchase button setup for: {shopItem?.itemName ?? "Unknown Item"}");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"⚠️ Purchase button not assigned for: {shopItem?.itemName ?? "Unknown Item"}");
        }
    }

    void OnPurchaseClicked()
    {
        if (shopManager == null)
        {
            Debug.LogError("❌ ShopUIManager reference is null!");
            return;
        }

        if (shopItem == null)
        {
            Debug.LogError("❌ ShopItem reference is null!");
            return;
        }

        // Extra validation
        if (!shopItem.CanPurchase())
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ Cannot purchase {shopItem.itemName} - Item not available");
            PlayErrorFeedback();
            return;
        }

        if (!HasEnoughCurrency())
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ Cannot purchase {shopItem.itemName} - Not enough currency");
            PlayErrorFeedback();
            return;
        }

        if (showDebugLogs)
            Debug.Log($"🛒 Purchase button clicked for: {shopItem.itemName} (${shopItem.price} {shopItem.currencyType})");

        // Visual feedback
        PlayPurchaseAnimation();

        // Call shop manager
        shopManager.PurchaseItem(shopItem);
    }

    void PlayPurchaseAnimation()
    {
        // Item animation
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1f);

        // Button animation
        if (purchaseButton != null)
        {
            purchaseButton.transform.DOPunchScale(Vector3.one * 0.05f, 0.15f, 8, 0.5f);
        }
    }

    void PlayErrorFeedback()
    {
        // Shake animation for error
        transform.DOShakePosition(0.3f, new Vector3(5f, 0f, 0f), 10, 90f, false, true);

        // Flash red color
        if (itemIcon != null)
        {
            Color originalColor = itemIcon.color;
            itemIcon.DOColor(expensiveColor, 0.1f)
                .OnComplete(() => itemIcon.DOColor(originalColor, 0.2f));
        }
    }

    void OnCurrencyChanged(int newAmount)
    {
        // Refresh the purchase button state when currency changes
        if (shopItem != null)
        {
            UpdateItemDisplay();
        }
    }

    bool HasEnoughCurrency()
    {
        if (shopItem == null) return false;

        switch (shopItem.currencyType)
        {
            case ShopCurrency.SoulCoins:
                return CurrencyManager.Instance != null &&
                       CurrencyManager.Instance.GetSoulCoins() >= shopItem.price;
            case ShopCurrency.PremiumCrystals:
                return CurrencyManager.Instance != null &&
                       CurrencyManager.Instance.GetCrystals() >= shopItem.price;
            case ShopCurrency.HonorPoints:
                return true; // Placeholder
            default:
                return false;
        }
    }

    void OnDestroy()
    {
        transform.DOKill();
    }
}
