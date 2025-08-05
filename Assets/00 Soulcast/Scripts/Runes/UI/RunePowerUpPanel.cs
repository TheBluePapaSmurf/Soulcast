using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunePowerUpPanel : MonoBehaviour
{
    public static RunePowerUpPanel Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RunePowerUpPanel>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    Debug.LogError("RunePowerUpPanel not found in scene! Make sure it exists.");
                }
            }
            return _instance;
        }
    }

    private static RunePowerUpPanel _instance;

    [Header("UI References")]
    public Image runeIcon;
    public TextMeshProUGUI runeNameText;
    public TextMeshProUGUI powerUpCostText;

    [Header("Buttons")]
    public Button confirmPowerUpButton;
    public Button cancelButton;
    public Button closeButton;

    [Header("Currency Display")]
    public TextMeshProUGUI currentSoulCoinsText;
    public TextMeshProUGUI afterPowerUpSoulCoinsText;

    [Header("Power-Up Settings")]
    [Range(0f, 1f)]
    public float basePowerUpChance = 0.8f;
    [Range(0f, 0.1f)]
    public float chanceDecreasePerLevel = 0.05f;
    public int basePowerUpCost = 100;
    public float costMultiplierPerLevel = 1.5f;

    private RuneData runeToPowerUp;
    private int powerUpCost;
    private float successChance;
    private System.Action onPowerUpComplete;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            SetupButtons();
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Multiple RunePowerUpPanel instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        HidePanel();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void SetupButtons()
    {
        if (confirmPowerUpButton != null)
            confirmPowerUpButton.onClick.AddListener(ConfirmPowerUp);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(HidePanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
    }

    public void ShowPowerUpPanel(RuneData rune, System.Action onComplete = null)
    {
        if (rune == null)
        {
            Debug.LogWarning("Cannot show power-up panel for null rune!");
            return;
        }

        if (rune.currentLevel >= rune.maxLevel)
        {
            Debug.LogWarning("Rune is already at max level!");
            return;
        }

        runeToPowerUp = rune;
        onPowerUpComplete = onComplete;

        CalculatePowerUpStats();
        UpdateUI();
        ShowPanel();
    }

    void CalculatePowerUpStats()
    {
        if (runeToPowerUp == null) return;

        // Calculate cost based on rarity and current level
        int baseCost = GetBaseCostByRarity(runeToPowerUp.rarity);
        powerUpCost = Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplierPerLevel, runeToPowerUp.currentLevel));

        // Calculate success chance (decreases with level)
        successChance = Mathf.Max(0.1f, basePowerUpChance - (runeToPowerUp.currentLevel * chanceDecreasePerLevel));
    }

    int GetBaseCostByRarity(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common:
                return 100;
            case RuneRarity.Uncommon:
                return 200;
            case RuneRarity.Rare:
                return 400;
            case RuneRarity.Epic:
                return 800;
            case RuneRarity.Legendary:
                return 1500;
            default:
                return 100;
        }
    }

    // REPLACE the UpdateUI method in RunePowerUpPanel.cs to show upgrade preview:
    void UpdateUI()
    {
        if (runeToPowerUp == null) return;

        // Basic rune info
        if (runeIcon != null)
            runeIcon.sprite = runeToPowerUp.runeIcon;

        // Show current main stat and what it will become
        if (runeNameText != null)
        {
            string currentMainStat = runeToPowerUp.mainStat?.GetDisplayText() ?? "";
            float nextLevelValue = runeToPowerUp.GetMainStatValueAtLevel(runeToPowerUp.currentLevel + 1);
            string nextMainStatDisplay = $"+{nextLevelValue:F0} {runeToPowerUp.mainStat?.GetStatDisplayName()}";

            runeNameText.text = $"{runeToPowerUp.runeName} +{runeToPowerUp.currentLevel}\n" +
                               $"{currentMainStat} → <color=#00FF00>{nextMainStatDisplay}</color>";
        }

        // Power-up cost
        if (powerUpCostText != null)
            powerUpCostText.text = $"{powerUpCost}";

        // Currency display
        UpdateCurrencyDisplay();

        // Update button state
        UpdateButtonState();
    }


    void UpdateCurrencyDisplay()
    {
        int currentCoins = PlayerInventory.Instance.GetSoulCoins();
        int afterPowerUpCoins = currentCoins - powerUpCost;

        if (currentSoulCoinsText != null)
            currentSoulCoinsText.text = $"Current: {currentCoins}";

        if (afterPowerUpSoulCoinsText != null)
        {
            string colorCode = afterPowerUpCoins >= 0 ? "#FFFF00" : "#FF4444";
            afterPowerUpSoulCoinsText.text = $"After: <color={colorCode}>{afterPowerUpCoins}</color>";
        }
    }

    void UpdateButtonState()
    {
        bool canAfford = PlayerInventory.Instance.CanAfford(powerUpCost);

        if (confirmPowerUpButton != null)
        {
            confirmPowerUpButton.interactable = canAfford;

            var buttonText = confirmPowerUpButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = canAfford ? "Power Up!" : "Not Enough Soul Coins";
            }
        }
    }

    void ConfirmPowerUp()
    {
        if (runeToPowerUp == null)
        {
            Debug.LogError("No rune to power up!");
            return;
        }

        if (!PlayerInventory.Instance.CanAfford(powerUpCost))
        {
            Debug.LogWarning("Not enough Soul Coins for power-up!");
            return;
        }

        // Spend the Soul Coins regardless of outcome
        PlayerInventory.Instance.SpendSoulCoins(powerUpCost);

        // Roll for success
        float roll = Random.Range(0f, 1f);
        bool success = roll <= successChance;

        if (success)
        {
            // Apply the upgrades BEFORE incrementing level
            runeToPowerUp.UpgradeMainStat();
            runeToPowerUp.UpgradeSubStats(); // Optional: upgrades every 3rd level

            // Then increment the level
            runeToPowerUp.currentLevel++;

            Debug.Log($"Power-up SUCCESS! {runeToPowerUp.runeName} is now level +{runeToPowerUp.currentLevel}");
            Debug.Log($"Main stat is now: {runeToPowerUp.mainStat.GetDisplayText()}");

            ShowPowerUpResult(true);
        }
        else
        {
            // Failure: Rune stays the same level
            Debug.Log($"Power-up FAILED! {runeToPowerUp.runeName} remains at level +{runeToPowerUp.currentLevel}");
            ShowPowerUpResult(false);
        }

        // Call completion callback
        onPowerUpComplete?.Invoke();

        // Save the game to persist the changes
        SaveManager.Instance?.SaveGame();
    }

    void ShowPowerUpResult(bool success)
    {
        // Update the rune name text to show result
        if (runeNameText != null)
        {
            if (success)
            {
                runeNameText.text = $"<color=#00FF00>{runeToPowerUp.runeName} +{runeToPowerUp.currentLevel} - SUCCESS!</color>";
            }
            else
            {
                runeNameText.text = $"<color=#FF4444>{runeToPowerUp.runeName} +{runeToPowerUp.currentLevel} - FAILED!</color>";
            }
        }

        // Update button text to show result
        if (confirmPowerUpButton != null)
        {
            var buttonText = confirmPowerUpButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (runeToPowerUp.currentLevel >= runeToPowerUp.maxLevel)
                {
                    buttonText.text = "MAX LEVEL";
                    confirmPowerUpButton.interactable = false;
                }
                else
                {
                    buttonText.text = success ? "Power Up Again!" : "Try Again!";

                    // Recalculate cost for next upgrade
                    CalculatePowerUpStats();

                    // Update cost display
                    if (powerUpCostText != null)
                        powerUpCostText.text = $"{powerUpCost}";

                    // Update currency display
                    UpdateCurrencyDisplay();

                    // Check if can afford next upgrade
                    UpdateButtonState();
                }
            }
        }

        // Reset rune name color after a few seconds
        Invoke(nameof(ResetRuneNameColor), 3f);
    }

    void ResetRuneNameColor()
    {
        if (runeToPowerUp != null && runeNameText != null)
        {
            runeNameText.text = $"{runeToPowerUp.runeName} +{runeToPowerUp.currentLevel}";
        }
    }

    void ShowPanel()
    {
        gameObject.SetActive(true);
        Debug.Log($"Showing power-up panel for: {runeToPowerUp?.runeName}");
    }

    public void HidePanel()
    {
        CancelInvoke(); // Cancel any pending color reset

        // Reset button text
        if (confirmPowerUpButton != null)
        {
            var buttonText = confirmPowerUpButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "Power Up!";
            }
        }

        gameObject.SetActive(false);
        runeToPowerUp = null;
        onPowerUpComplete = null;
        Debug.Log("Hiding rune power-up panel");
    }
}
