using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuneSellPanel : MonoBehaviour
{
    public static RuneSellPanel Instance { get; private set; }

    [Header("UI References")]
    public Image runeIcon;
    public TextMeshProUGUI runeNameText;
    public TextMeshProUGUI runeLevelText;
    public TextMeshProUGUI sellPriceText;
    public TextMeshProUGUI confirmationText;

    [Header("Buttons")]
    public Button confirmSellButton;
    public Button cancelButton;
    public Button closeButton;

    [Header("Currency Display")]
    public TextMeshProUGUI currentSoulCoinsText;
    public TextMeshProUGUI afterSaleSoulCoinsText;

    private RuneData runeToSell;
    private int sellPrice;
    private System.Action onSellComplete;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupButtons();
            HidePanel(); // Hide immediately after setup
        }
        else
        {
            Debug.LogWarning("Multiple RuneSellPanel instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }


    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void SetupButtons()
    {
        if (confirmSellButton != null)
            confirmSellButton.onClick.AddListener(ConfirmSell);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(HidePanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
    }

    public void ShowSellPanel(RuneData rune, System.Action onComplete = null)
    {
        if (rune == null)
        {
            Debug.LogWarning("Cannot show sell panel for null rune!");
            return;
        }

        runeToSell = rune;
        onSellComplete = onComplete;
        sellPrice = CalculateSellPrice(rune);

        UpdateUI();
        ShowPanel();
    }

    void UpdateUI()
    {
        if (runeToSell == null) return;

        // Basic rune info
        if (runeIcon != null)
            runeIcon.sprite = runeToSell.runeIcon;

        if (runeNameText != null)
            runeNameText.text = runeToSell.runeName;

        if (runeLevelText != null)
            runeLevelText.text = $"Level +{runeToSell.currentLevel}";

        // Sell price
        if (sellPriceText != null)
            sellPriceText.text = $"{sellPrice}";

        // Confirmation text
        if (confirmationText != null)
        {
            confirmationText.text = $"Are you sure you want to sell this rune for {sellPrice} Soul Coins?\n\n" +
                                  "<color=#FF4444><b>This action cannot be undone!</b></color>";
        }

        // Currency display
        UpdateCurrencyDisplay();
    }

    void UpdateCurrencyDisplay()
    {
        int currentCoins = PlayerInventory.Instance.GetSoulCoins();
        int afterSaleCoins = currentCoins + sellPrice;

        if (currentSoulCoinsText != null)
            currentSoulCoinsText.text = $"Current: {currentCoins}";

        if (afterSaleSoulCoinsText != null)
            afterSaleSoulCoinsText.text = $"After Sale: <color=#00FF00>{afterSaleCoins}</color>";
    }

    int CalculateSellPrice(RuneData rune)
    {
        // Base sell price calculation
        int basePrice = GetBasePriceByRarity(rune.rarity);

        // Add bonus for level upgrades (50% of upgrade cost per level)
        int levelBonus = 0;
        for (int i = 0; i < rune.currentLevel; i++)
        {
            levelBonus += Mathf.RoundToInt(rune.GetUpgradeCost(i) * 0.5f);
        }

        return basePrice + levelBonus;
    }

    int GetBasePriceByRarity(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return 50;
            case RuneRarity.Uncommon:
                return 150;
            case RuneRarity.Rare:
                return 400;
            case RuneRarity.Epic:
                return 800;
            case RuneRarity.Legendary:
                return 1500;
            default:
                return 50;
        }
    }

    // ALTERNATIVE: Simplified ConfirmSell method without success popup:
    void ConfirmSell()
    {
        if (runeToSell == null)
        {
            Debug.LogError("No rune to sell!");
            return;
        }

        // Store for logging before clearing
        string soldRuneName = runeToSell.runeName;

        // Add Soul Coins to player inventory
        PlayerInventory.Instance.AddSoulCoins(sellPrice);

        // Remove rune from player inventory
        PlayerInventory.Instance.RemoveRune(runeToSell);

        Debug.Log($"Sold {soldRuneName} for {sellPrice} Soul Coins!");

        // Call completion callback
        onSellComplete?.Invoke();

        // Hide panel
        HidePanel();
    }

    void ShowPanel()
    {
        gameObject.SetActive(true);
        Debug.Log($"Showing sell panel for: {runeToSell?.runeName}");
    }

    public void HidePanel()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }

        runeToSell = null;
        onSellComplete = null;
        Debug.Log("Hiding rune sell panel");
    }

}
