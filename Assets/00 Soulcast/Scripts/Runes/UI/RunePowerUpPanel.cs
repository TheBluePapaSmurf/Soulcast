using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RuneData;

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

        // ✅ NEW: Use RuneData's enhanced cost calculation
        powerUpCost = runeToPowerUp.GetCurrentUpgradeCost();

        // ✅ NEW: Use RuneData's enhanced success chance calculation  
        successChance = runeToPowerUp.GetCurrentUpgradeSuccessChance();
    }

    void UpdateUI()
    {
        if (runeToPowerUp == null) return;

        // Basic rune info
        if (runeIcon != null)
            runeIcon.sprite = runeToPowerUp.runeIcon;

        // Show upgrade preview with risk assessment
        if (runeNameText != null)
        {
            string currentMainStat = runeToPowerUp.mainStat?.GetDisplayText() ?? "";
            float nextLevelValue = runeToPowerUp.GetMainStatValueAtLevel(runeToPowerUp.currentLevel + 1);
            string nextMainStatDisplay = $"+{nextLevelValue:F0} {runeToPowerUp.mainStat?.GetStatDisplayName()}";

            // Get risk assessment
            var risk = runeToPowerUp.GetUpgradeRisk();
            string riskColor = GetRiskColor(risk);
            string riskText = GetRiskText(risk);

            runeNameText.text = $"{runeToPowerUp.runeName} +{runeToPowerUp.currentLevel}\n" +
                               $"{currentMainStat} → <color=#00FF00>{nextMainStatDisplay}</color>\n" +
                               $"<color={riskColor}>{riskText}</color>";
        }

        // Enhanced cost display with success chance
        if (powerUpCostText != null)
        {
            powerUpCostText.text = $"{powerUpCost:N0} Soul Coins\n" +
                                  $"<color={(successChance >= 0.5f ? "#00FF00" : "#FF4444")}>Success: {successChance:P0}</color>";
        }

        // Currency display
        UpdateCurrencyDisplay();

        // Update button state
        UpdateButtonState();
    }

    // ✅ NEW: Risk assessment helpers
    string GetRiskColor(UpgradeRisk risk)
    {
        switch (risk)
        {
            case UpgradeRisk.Safe: return "#00FF00";      // Green
            case UpgradeRisk.Moderate: return "#FFFF00"; // Yellow
            case UpgradeRisk.Risky: return "#FF8800";    // Orange
            case UpgradeRisk.Dangerous: return "#FF4444"; // Red
            case UpgradeRisk.Extreme: return "#FF0088";   // Pink/Magenta
            default: return "#FFFFFF";
        }
    }

    string GetRiskText(UpgradeRisk risk)
    {
        switch (risk)
        {
            case UpgradeRisk.Safe: return "SAFE UPGRADE";
            case UpgradeRisk.Moderate: return "Moderate Risk";
            case UpgradeRisk.Risky: return "RISKY!";
            case UpgradeRisk.Dangerous: return "DANGEROUS!";
            case UpgradeRisk.Extreme: return "EXTREME RISK!";
            default: return "Unknown Risk";
        }
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
                if (canAfford)
                {
                    buttonText.text = $"Power Up! ({successChance:P0})";
                }
                else
                {
                    buttonText.text = "Not Enough Soul Coins";
                }
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
        if (runeNameText != null)
        {
            if (success)
            {
                runeNameText.text = $"<color=#00FF00>✓ {runeToPowerUp.runeName} +{runeToPowerUp.currentLevel}</color>\n" +
                                   $"<color=#00FF00>UPGRADE SUCCESS!</color>";
            }
            else
            {
                runeNameText.text = $"<color=#FF4444>✗ {runeToPowerUp.runeName} +{runeToPowerUp.currentLevel}</color>\n" +
                                   $"<color=#FF4444>UPGRADE FAILED!</color>";
            }
        }

        if (confirmPowerUpButton != null)
        {
            var buttonText = confirmPowerUpButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (runeToPowerUp.currentLevel >= runeToPowerUp.maxLevel)
                {
                    buttonText.text = "MAX LEVEL REACHED";
                    confirmPowerUpButton.interactable = false;
                }
                else
                {
                    // Recalculate for next upgrade
                    CalculatePowerUpStats();

                    buttonText.text = success ?
                        $"Power Up Again! ({successChance:P0})" :
                        $"Try Again! ({successChance:P0})";

                    // Update displays
                    if (powerUpCostText != null)
                    {
                        powerUpCostText.text = $"{powerUpCost:N0} Soul Coins\n" +
                                              $"<color={(successChance >= 0.5f ? "#00FF00" : "#FF4444")}>Success: {successChance:P0}</color>";
                    }

                    UpdateCurrencyDisplay();
                    UpdateButtonState();
                }
            }
        }

        // Reset after delay
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
