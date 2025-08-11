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
            runeIcon.sprite = runeToSell.runeSprite;

        if (runeNameText != null)
            runeNameText.text = $"{runeToSell.runeName}";

        if (runeLevelText != null)
            runeLevelText.text = $"Level +{runeToSell.currentLevel} ({runeToSell.rarity})";

        // ✅ ENHANCED: Sell price with breakdown
        if (sellPriceText != null)
        {
            int basePrice = GetBasePriceByRarity(runeToSell.rarity);
            int levelBonus = 0;
            for (int i = 0; i < runeToSell.currentLevel; i++)
            {
                levelBonus += Mathf.RoundToInt(runeToSell.GetUpgradeCost(i) * 0.5f);
            }
            int qualityBonus = CalculateQualityBonus(runeToSell);

            sellPriceText.text = $"{sellPrice:N0}\n" +
                                $"<size=18><color=#888888>" +
                                $"Base: {basePrice}" +
                                (levelBonus > 0 ? $"\nLevel: +{levelBonus}" : "") +
                                (qualityBonus > 0 ? $"\nQuality: +{qualityBonus}" : "") +
                                "</color></size>";
        }

        // ✅ ENHANCED: Confirmation text with more details
        if (confirmationText != null)
        {
            string statsText = "";
            if (runeToSell.mainStat != null)
            {
                statsText += $"Main: {runeToSell.mainStat.GetDisplayText()}\n";
            }
            if (runeToSell.subStats != null && runeToSell.subStats.Count > 0)
            {
                statsText += $"Subs: {runeToSell.subStats.Count} stats\n";
            }

            confirmationText.text = $"Sell this rune for {sellPrice:N0} Soul Coins?\n\n" +
                                   $"{statsText}" +
                                   $"<color=#FF4444><b>This action cannot be undone!</b></color>";
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
        // ✅ FIX: Use GetRuneSellPrice instead of SellRune
        if (RuneCollectionManager.Instance != null)
        {
            return RuneCollectionManager.Instance.GetRuneSellPrice(rune);
        }

        // Fallback calculation
        int basePrice = GetBasePriceByRarity(rune.rarity);

        // ✅ ENHANCED: Add bonus for level upgrades and stat quality
        int levelBonus = 0;
        for (int i = 0; i < rune.currentLevel; i++)
        {
            levelBonus += Mathf.RoundToInt(rune.GetUpgradeCost(i) * 0.5f);
        }

        // ✅ NEW: Add small bonus for high-quality sub stats
        int qualityBonus = CalculateQualityBonus(rune);

        return basePrice + levelBonus + qualityBonus;
    }


    private int CalculateQualityBonus(RuneData rune)
    {
        if (rune.subStats == null || rune.subStats.Count == 0)
            return 0;

        int bonus = 0;

        // Small bonus per sub stat (more sub stats = better rune)
        bonus += rune.subStats.Count * 10;

        // Extra bonus for percentage stats (usually more valuable)
        foreach (var stat in rune.subStats)
        {
            if (stat.isPercentage)
                bonus += 20;
        }

        return bonus;
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

    void ConfirmSell()
    {
        if (runeToSell == null)
        {
            Debug.LogError("No rune to sell!");
            return;
        }

        // Store for logging before clearing
        string soldRuneName = runeToSell.runeName;
        int soldRuneLevel = runeToSell.currentLevel;
        RuneRarity soldRuneRarity = runeToSell.rarity;

        // ✅ CORRECT: Use SellRune only when actually confirming the sale
        if (RuneCollectionManager.Instance != null)
        {
            bool success = RuneCollectionManager.Instance.SellRune(runeToSell, out int actualSellPrice);

            if (success)
            {
                Debug.Log($"✅ Sold {soldRuneName} +{soldRuneLevel} ({soldRuneRarity}) for {actualSellPrice} Soul Coins!");

                // Call completion callback
                onSellComplete?.Invoke();

                // Hide panel
                HidePanel();
            }
            else
            {
                Debug.LogError($"❌ Failed to sell {soldRuneName}!");
            }
        }
        else
        {
            // Fallback: manual sell process (should not be used normally)
            Debug.LogWarning("RuneCollectionManager not found, using fallback sell process");

            PlayerInventory.Instance.AddSoulCoins(sellPrice);
            PlayerInventory.Instance.RemoveRune(runeToSell);

            Debug.Log($"✅ Sold {soldRuneName} +{soldRuneLevel} for {sellPrice} Soul Coins!");
            onSellComplete?.Invoke();
            HidePanel();
        }
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
