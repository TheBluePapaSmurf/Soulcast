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

        // ✅ FIXED: Use runeSprite instead of runeIcon for procedural runes
        if (runeIcon != null)
            runeIcon.sprite = runeToPowerUp.runeSprite; // Changed from runeIcon to runeSprite

        // ✅ ENHANCED: Show upgrade preview with procedural rune details
        if (runeNameText != null)
        {
            string currentMainStat = runeToPowerUp.mainStat?.GetDisplayText() ?? "";
            float nextLevelValue = runeToPowerUp.GetMainStatValueAtLevel(runeToPowerUp.currentLevel + 1);
            string suffix = runeToPowerUp.mainStat?.isPercentage == true ? "%" : "";
            string nextMainStatDisplay = $"+{nextLevelValue:F1}{suffix} {runeToPowerUp.mainStat?.GetStatDisplayName()}";

            // Get risk assessment with enhanced display
            var risk = runeToPowerUp.GetUpgradeRisk();
            string riskColor = GetRiskColor(risk);
            string riskText = GetRiskText(risk);

            // ✅ ENHANCED: Add procedural rune metadata
            string setInfo = "";
            if (runeToPowerUp.runeSet != null)
            {
                setInfo = $"\n<size=16><color=#888888>{runeToPowerUp.runeSet.setName} Set</color></size>";
            }

            // Add power rating for procedural runes
            float powerRating = runeToPowerUp.GetPowerRating();
            string powerInfo = $"\n<size=14><color=#888888>Power: {powerRating:F0}</color></size>";

            // Add unique ID for debugging (only in editor)
            string debugInfo = "";
#if UNITY_EDITOR
            if (runeToPowerUp.isProceduralGenerated)
            {
                debugInfo = $"\n<size=10><color=#666666>ID: {runeToPowerUp.uniqueID[..8]}...</color></size>";
            }
#endif

            runeNameText.text = $"{runeToPowerUp.GetDisplayName()}{setInfo}{powerInfo}{debugInfo}\n" +
                               $"{currentMainStat} → <color=#00FF00>{nextMainStatDisplay}</color>\n" +
                               $"<color={riskColor}>{riskText}</color>";
        }

        // ✅ ENHANCED: Cost display with substat upgrade info and generation time
        if (powerUpCostText != null)
        {
            string substatInfo = "";
            if ((runeToPowerUp.currentLevel + 1) % 3 == 0)
            {
                int subStatCount = runeToPowerUp.subStats?.Count ?? 0;
                substatInfo = $"\n<color=#FFD700>+ {subStatCount} Sub Stats Upgrade!</color>";
            }

            // Add efficiency info for procedural runes
            float efficiency = runeToPowerUp.GetEfficiency();
            string efficiencyInfo = $"\n<size=12><color=#888888>Efficiency: {efficiency:F1}</color></size>";

            powerUpCostText.text = $"{powerUpCost:N0} Soul Coins\n" +
                                  $"<color={(successChance >= 0.5f ? "#00FF00" : "#FF4444")}>Success: {successChance:P0}</color>" +
                                  substatInfo + efficiencyInfo;
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
        int currentCoins = CurrencyManager.Instance?.GetSoulCoins() ?? 0; // Fixed method call
        int afterPowerUpCoins = currentCoins - powerUpCost;

        if (currentSoulCoinsText != null)
            currentSoulCoinsText.text = $"Current: {currentCoins:N0}"; // Added formatting

        if (afterPowerUpSoulCoinsText != null)
        {
            string colorCode = afterPowerUpCoins >= 0 ? "#FFFF00" : "#FF4444";
            afterPowerUpSoulCoinsText.text = $"After: <color={colorCode}>{afterPowerUpCoins:N0}</color>";
        }
    }

    void UpdateButtonState()
    {
        bool canAfford = CurrencyManager.Instance?.CanAffordSoulCoins(powerUpCost) ?? false; // Fixed method call

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
                    buttonText.text = $"Need {powerUpCost:N0}\nSoul Coins";
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

        // ✅ ENHANCED: Use RuneCollectionManager for upgrade
        if (RuneCollectionManager.Instance != null)
        {
            bool success = RuneCollectionManager.Instance.UpgradeRune(runeToPowerUp, true);

            if (success)
            {
                Debug.Log($"✅ Power-up SUCCESS! {runeToPowerUp.runeName} is now level +{runeToPowerUp.currentLevel}");
                Debug.Log($"Main stat is now: {runeToPowerUp.mainStat.GetDisplayText()}");

                ShowPowerUpResult(true);
            }
            else
            {
                Debug.LogWarning($"❌ Power-up failed for {runeToPowerUp.runeName}");
                ShowPowerUpResult(false);
            }
        }
        else
        {
            // ✅ FALLBACK: Manual upgrade if manager not available
            if (!CurrencyManager.Instance?.CanAffordSoulCoins(powerUpCost) == true)
            {
                Debug.LogWarning("Not enough Soul Coins for power-up!");
                return;
            }

            // Spend the Soul Coins regardless of outcome
            CurrencyManager.Instance.SpendSoulCoins(powerUpCost);

            // Roll for success
            float roll = Random.Range(0f, 1f);
            bool success = roll <= successChance;

            if (success)
            {
                runeToPowerUp.UpgradeRune(); // Use the enhanced upgrade method
                Debug.Log($"✅ Manual power-up SUCCESS! {runeToPowerUp.runeName} is now level +{runeToPowerUp.currentLevel}");
            }
            else
            {
                Debug.Log($"❌ Manual power-up FAILED! {runeToPowerUp.runeName} remains at level +{runeToPowerUp.currentLevel}");
            }

            ShowPowerUpResult(success);
        }

        // Call completion callback
        onPowerUpComplete?.Invoke();
    }

    void ShowPowerUpResult(bool success)
    {
        if (runeNameText != null)
        {
            string mainStatText = runeToPowerUp.mainStat?.GetDisplayText() ?? "";
            string powerRating = $"Power: {runeToPowerUp.GetPowerRating():F0}";

            if (success)
            {
                string upgradeInfo = "";
                if (runeToPowerUp.currentLevel % 3 == 0)
                {
                    upgradeInfo = " + Sub Stats!";
                }

                runeNameText.text = $"<color=#00FF00>✓ {runeToPowerUp.GetDisplayName()}</color>\n" +
                                   $"<color=#00FF00>UPGRADE SUCCESS!{upgradeInfo}</color>\n" +
                                   $"<size=18>{mainStatText}</size>\n" +
                                   $"<size=14><color=#888888>{powerRating}</color></size>";
            }
            else
            {
                runeNameText.text = $"<color=#FF4444>✗ {runeToPowerUp.GetDisplayName()}</color>\n" +
                                   $"<color=#FF4444>UPGRADE FAILED!</color>\n" +
                                   $"<size=18>{mainStatText}</size>\n" +
                                   $"<size=14><color=#888888>{powerRating}</color></size>";
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
                        string substatInfo = "";
                        if ((runeToPowerUp.currentLevel + 1) % 3 == 0)
                        {
                            substatInfo = "\n<color=#FFD700>+ Sub Stats Upgrade!</color>";
                        }

                        powerUpCostText.text = $"{powerUpCost:N0} Soul Coins\n" +
                                              $"<color={(successChance >= 0.5f ? "#00FF00" : "#FF4444")}>Success: {successChance:P0}</color>" +
                                              substatInfo;
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
            runeNameText.text = runeToPowerUp.GetDisplayName();
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
