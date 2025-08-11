using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuneDetailsPopup : MonoBehaviour
{
    public static RuneDetailsPopup Instance { get; private set; }

    [Header("UI References")]
    public Image runeIcon;
    public TextMeshProUGUI runeNameText;
    public TextMeshProUGUI runeLevelText;
    public TextMeshProUGUI mainStatText;
    public TextMeshProUGUI subStatsText;
    public TextMeshProUGUI setTenText;
    public Button closeButton;

    [Header("Action Buttons")]
    public Button equipButton;
    public Button powerUpButton;
    public Button sellButton;

    private RuneData currentRune;
    private bool isEquipped;
    private System.Action onRuneChanged;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep across scenes
        }
        else
        {
            Debug.LogWarning("Multiple RuneDetailsPopup instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        SetupButtons();
        HidePopup(); // Start hidden
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
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePopup);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);

        if (powerUpButton != null)
            powerUpButton.onClick.AddListener(OnPowerUpClicked);

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
    }

    public void ShowRune(RuneData rune, bool equipped = false, System.Action onChanged = null)
    {
        currentRune = rune;
        isEquipped = equipped;
        onRuneChanged = onChanged;

        UpdateUI();
        ShowPopup();
    }

    void UpdateUI()
    {
        if (currentRune == null) return;

        // ✅ FIXED: Use runeSprite instead of runeIcon for procedural runes
        if (runeIcon != null)
            runeIcon.sprite = currentRune.runeSprite; // Changed from runeIcon to runeSprite

        // ✅ ENHANCED: Show procedural rune name with generation info
        if (runeNameText != null)
        {
            string nameDisplay = currentRune.GetDisplayName(); // Uses +level format

            // Add procedural indicator if applicable
            if (currentRune.isProceduralGenerated)
            {
                string rarityColor = ColorUtility.ToHtmlStringRGB(currentRune.GetRarityColor());
                nameDisplay = $"<color=#{rarityColor}>{nameDisplay}</color>";
            }

            runeNameText.text = nameDisplay;
        }

        if (runeLevelText != null)
            runeLevelText.text = $"Level +{currentRune.currentLevel}";

        // ✅ ENHANCED: Better main stat display for procedural runes
        if (mainStatText != null && currentRune.mainStat != null)
        {
            string currentValue = currentRune.mainStat.GetDisplayText();

            // Show upgrade preview if not max level
            if (currentRune.currentLevel < currentRune.maxLevel)
            {
                float nextLevelValue = currentRune.GetMainStatValueAtLevel(currentRune.currentLevel + 1);
                string suffix = currentRune.mainStat.isPercentage ? "%" : "";
                string nextDisplay = $"+{nextLevelValue:F1}{suffix} {currentRune.mainStat.GetStatDisplayName()}";

                mainStatText.text = $"<b>Main Stat:</b>\n{currentValue}\n" +
                                   $"<color=#888888>Next: {nextDisplay}</color>";
            }
            else
            {
                mainStatText.text = $"<b>Main Stat:</b>\n{currentValue}\n" +
                                   $"<color=#FFD700>MAX LEVEL</color>";
            }
        }

        // ✅ ENHANCED: Better sub stats display with color coding
        if (subStatsText != null)
        {
            string subStatsDisplay = "<b>Sub Stats:</b>\n";

            if (currentRune.subStats != null && currentRune.subStats.Count > 0)
            {
                for (int i = 0; i < currentRune.subStats.Count; i++)
                {
                    var subStat = currentRune.subStats[i];
                    if (subStat != null)
                    {
                        // Color code the stat based on type
                        string statColor = ColorUtility.ToHtmlStringRGB(subStat.GetStatColor());
                        subStatsDisplay += $"• <color=#{statColor}>{subStat.GetDisplayText()}</color>";

                        // Show if this substat will upgrade at next level (every 3rd level)
                        if (currentRune.currentLevel < currentRune.maxLevel &&
                            (currentRune.currentLevel + 1) % 3 == 0)
                        {
                            subStatsDisplay += " <color=#888888>(+)</color>";
                        }

                        subStatsDisplay += "\n";
                    }
                }
            }
            else
            {
                subStatsDisplay += "<color=#888888>No sub stats</color>";
            }

            subStatsText.text = subStatsDisplay;
        }

        // ✅ ENHANCED: Better set information display with fallback
        if (setTenText != null)
        {
            if (currentRune.runeSet != null)
            {
                // Show set bonuses
                string setBonusText = "";
                foreach (var bonus in currentRune.runeSet.setBonuses)
                {
                    setBonusText += $"({bonus.requiredPieces}): {bonus.description}\n";
                }

                setTenText.text = $"<b>Set:</b> {currentRune.runeSet.setName}\n" +
                                 $"<b>Set Bonuses:</b>\n{setBonusText}";
            }
            else
            {
                // ✅ ENHANCED: Better fallback for missing set data
                setTenText.text = $"<b>Set:</b> {currentRune.runeType} Set\n" +
                                 $"<color=#888888>Set bonuses loading...</color>";
            }
        }

        UpdateButtonStates();
    }

    void UpdateButtonStates()
    {
        // Equip button
        if (equipButton != null)
        {
            var equipButtonText = equipButton.GetComponentInChildren<TextMeshProUGUI>();

            if (isEquipped)
            {
                if (equipButtonText != null)
                    equipButtonText.text = "Unequip";
                equipButton.interactable = true;
            }
            else
            {
                if (equipButtonText != null)
                    equipButtonText.text = "Equip";
                equipButton.interactable = true;
            }
        }

        // ✅ ENHANCED: Power-up button with Soul Coins and risk assessment
        if (powerUpButton != null)
        {
            bool canUpgrade = currentRune.currentLevel < currentRune.maxLevel;
            int upgradeCost = canUpgrade ? currentRune.GetUpgradeCost(currentRune.currentLevel) : 0;
            bool canAfford = CurrencyManager.Instance?.CanAffordSoulCoins(upgradeCost) ?? false;

            powerUpButton.interactable = canUpgrade && canAfford;

            var powerUpButtonText = powerUpButton.GetComponentInChildren<TextMeshProUGUI>();
            if (powerUpButtonText != null)
            {
                if (!canUpgrade)
                {
                    powerUpButtonText.text = "Max Level";
                }
                else if (!canAfford)
                {
                    powerUpButtonText.text = $"Need {upgradeCost:N0}\nSoul Coins";
                }
                else
                {
                    float successChance = currentRune.GetCurrentUpgradeSuccessChance();
                    var risk = currentRune.GetUpgradeRisk();
                    string riskText = GetRiskDisplayText(risk);

                    powerUpButtonText.text = $"Power Up\n{upgradeCost:N0} coins\n{riskText}";
                }
            }
        }

        // ✅ ENHANCED: Sell button with proper compatibility check
        if (sellButton != null)
        {
            bool canSell = !isEquipped && (RuneCollectionManager.Instance?.ContainsRune(currentRune) ?? false);
            sellButton.interactable = canSell;

            var sellButtonText = sellButton.GetComponentInChildren<TextMeshProUGUI>();
            if (sellButtonText != null)
            {
                if (isEquipped)
                {
                    sellButtonText.text = "Can't Sell\n(Equipped)";
                }
                else if (!canSell)
                {
                    sellButtonText.text = "Can't Sell\n(Not Owned)";
                }
                else
                {
                    int sellPrice = RuneCollectionManager.Instance?.GetRuneSellPrice(currentRune) ?? 0;
                    sellButtonText.text = $"Sell\n{sellPrice:N0} coins";
                }
            }
        }
    }

    private int CalculateRuneSellPrice()
    {
        if (currentRune == null || RuneCollectionManager.Instance == null)
            return 0;

        return RuneCollectionManager.Instance.GetRuneSellPrice(currentRune);
    }

    private int GetBaseSellPriceByRarity(RuneRarity rarity)
    {
        return rarity switch
        {
            RuneRarity.Common => 50,
            RuneRarity.Uncommon => 150,
            RuneRarity.Rare => 400,
            RuneRarity.Epic => 800,
            RuneRarity.Legendary => 1500,
            _ => 50
        };
    }

        void ShowPopup()
        {
        gameObject.SetActive(true);
        Debug.Log($"Showing popup for rune: {currentRune.runeName}");
        }

    public void HidePopup()
    {
        gameObject.SetActive(false);
        Debug.Log("Hiding rune details popup");
    }

    // Your existing button methods...
    void OnEquipClicked()
    {
        if (currentRune == null) return;

        if (isEquipped)
        {
            UnequipRune();
        }
        else
        {
            EquipRune();
        }
    }

    void EquipRune()
    {
        Debug.Log($"Equipping rune: {currentRune.runeName}");

        // Get the current monster with fallback logic
        CollectedMonster selectedMonster = GetCurrentMonsterForEquipping();

        if (selectedMonster == null)
        {
            Debug.LogWarning("No monster available for equipping rune! Opening monster selection...");

            // ✅ NEW: Show monster selection UI or auto-select first available monster
            AutoSelectFirstAvailableMonster();
            selectedMonster = GetCurrentMonsterForEquipping();

            if (selectedMonster == null)
            {
                Debug.LogError("❌ Still no monster available after auto-selection!");
                return;
            }
        }

        // Find available slot for this rune
        int targetSlotIndex = FindAvailableSlot(selectedMonster, currentRune);

        if (targetSlotIndex == -1)
        {
            Debug.LogWarning($"No available slot for rune type {currentRune.runeSlotPosition}!");
            return;
        }

        // Equip the rune
        bool success = PlayerInventory.Instance.EquipRuneToMonster(
            selectedMonster.uniqueID, targetSlotIndex, currentRune);

        if (success)
        {
            isEquipped = true;
            UpdateButtonStates();

            // IMPORTANT: Refresh all rune slot visuals
            RefreshAllRuneSlots();

            // NEW: Refresh the monster stats display
            MonsterInventoryUI inventoryUI = FindFirstObjectByType<MonsterInventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.RefreshCurrentMonsterStats();
            }

            // Call the callback if provided
            onRuneChanged?.Invoke();

            Debug.Log($"✅ Successfully equipped {currentRune.runeName} to {selectedMonster.monsterData.monsterName} slot {targetSlotIndex}");
            HidePopup();
        }
        else
        {
            Debug.LogError($"❌ Failed to equip {currentRune.runeName}");
        }
    }

    // ✅ SIMPLE FIX: Add to RuneDetailsPopup.cs

    // ✅ ENHANCED: GetCurrentMonsterForEquipping with better fallbacks
    private CollectedMonster GetCurrentMonsterForEquipping()
    {
        // Try 1: Get from MonsterInventoryUI
        MonsterInventoryUI inventoryUI = FindFirstObjectByType<MonsterInventoryUI>();
        if (inventoryUI != null)
        {
            CollectedMonster selectedMonster = inventoryUI.GetCurrentSelectedMonster();
            if (selectedMonster != null)
            {
                Debug.Log($"✅ Found selected monster: {selectedMonster.monsterData.monsterName}");
                return selectedMonster;
            }
        }

        // Try 2: Get first available monster as fallback
        var allMonsters = PlayerInventory.Instance?.GetAllMonsters();
        if (allMonsters != null && allMonsters.Count > 0)
        {
            var firstMonster = allMonsters[0];
            Debug.Log($"⚠️ Using first available monster as fallback: {firstMonster.monsterData.monsterName}");

            // Try to auto-select this monster in the UI
            if (inventoryUI != null)
            {
                inventoryUI.SelectMonster(firstMonster);
            }

            return firstMonster;
        }

        Debug.LogError("❌ No monsters found anywhere!");
        return null;
    }


    // ✅ NEW: Auto-select first available monster
    private void AutoSelectFirstAvailableMonster()
    {
        Debug.Log("🔄 Auto-selecting first available monster...");

        MonsterInventoryUI inventoryUI = FindFirstObjectByType<MonsterInventoryUI>();
        if (inventoryUI != null)
        {
            var allMonsters = PlayerInventory.Instance?.GetAllMonsters();
            if (allMonsters != null && allMonsters.Count > 0)
            {
                var firstMonster = allMonsters[0];
                Debug.Log($"🎯 Auto-selecting monster: {firstMonster.monsterData.monsterName}");

                // Force select this monster
                inventoryUI.SelectMonster(firstMonster);
            }

            inventoryUI.RefreshCurrentMonsterStats();
        }
    }

    void UnequipRune()
    {
        Debug.Log($"Unequipping rune: {currentRune.runeName}");

        // Get the current monster
        MonsterInventoryUI inventoryUI = FindAnyObjectByType<MonsterInventoryUI>();
        RunePanelUI runePanelUI = inventoryUI?.GetRunePanelUI();
        CollectedMonster selectedMonster = runePanelUI?.GetCurrentMonster();

        if (selectedMonster == null)
        {
            Debug.LogWarning("No monster selected for unequipping rune!");
            return;
        }

        // Find which slot has this rune equipped
        int equippedSlotIndex = FindEquippedSlot(selectedMonster, currentRune);

        if (equippedSlotIndex == -1)
        {
            Debug.LogWarning($"Rune {currentRune.runeName} is not equipped on this monster!");
            return;
        }

        // Unequip the rune
        RuneData unequippedRune = PlayerInventory.Instance.UnequipRuneFromMonster(
            selectedMonster.uniqueID, equippedSlotIndex);

        if (unequippedRune != null)
        {
            isEquipped = false;
            UpdateButtonStates();

            // IMPORTANT: Refresh all rune slot visuals
            RefreshAllRuneSlots();

            // NEW: Refresh the monster stats display
            if (inventoryUI != null)
            {
                inventoryUI.RefreshCurrentMonsterStats();
            }

            // Call the callback if provided
            onRuneChanged?.Invoke();

            Debug.Log($"Successfully unequipped {currentRune.runeName} from slot {equippedSlotIndex}");
            HidePopup();
        }
        else
        {
            Debug.LogError($"Failed to unequip {currentRune.runeName}");
        }
    }


    // ADD these helper methods to RuneDetailsPopup.cs:
    private int FindAvailableSlot(CollectedMonster monster, RuneData rune)
    {
        for (int i = 0; i < monster.runeSlots.Length; i++)
        {
            // Check if this slot matches the rune's required position
            if ((RuneSlotPosition)i == rune.runeSlotPosition)
            {
                return i; // Return first matching slot (will replace if occupied)
            }
        }
        return -1; // No matching slot found
    }

    private int FindEquippedSlot(CollectedMonster monster, RuneData rune)
    {
        for (int i = 0; i < monster.runeSlots.Length; i++)
        {
            if (monster.runeSlots[i].equippedRune == rune)
            {
                return i;
            }
        }
        return -1; // Rune not found in any slot
    }

    private void RefreshAllRuneSlots()
    {
        // Get the RunePanelUI and call its refresh method
        MonsterInventoryUI inventoryUI = FindAnyObjectByType<MonsterInventoryUI>();
        RunePanelUI runePanelUI = inventoryUI?.GetRunePanelUI();

        if (runePanelUI != null)
        {
            runePanelUI.RefreshAllRuneSlotVisuals();
        }
        else
        {
            Debug.LogWarning("RunePanelUI not found for refreshing rune slots!");
        }
    }

    // REPLACE the OnPowerUpClicked method in RuneDetailsPopup.cs:
    void OnPowerUpClicked()
    {
        if (currentRune == null)
        {
            Debug.LogWarning("Cannot power-up: rune is null!");
            return;
        }

        if (currentRune.currentLevel >= currentRune.maxLevel)
        {
            Debug.LogWarning("Rune is already at max level!");
            return;
        }

        Debug.Log($"Opening power-up panel for rune: {currentRune.runeName}");

        // Open the power-up panel
        if (RunePowerUpPanel.Instance != null)
        {
            RunePowerUpPanel.Instance.ShowPowerUpPanel(currentRune, () => {
                // Callback when power-up is complete
                onRuneChanged?.Invoke();

                // Refresh the UI to show updated level
                UpdateUI();

                // Refresh the rune panel UI if this rune is equipped
                if (isEquipped)
                {
                    MonsterInventoryUI inventoryUI = FindAnyObjectByType<MonsterInventoryUI>();
                    RunePanelUI runePanelUI = inventoryUI?.GetRunePanelUI();
                    if (runePanelUI != null)
                    {
                        runePanelUI.RefreshCurrentView();
                        inventoryUI.RefreshCurrentMonsterStats();
                    }
                }
            });
        }
        else
        {
            Debug.LogError("RunePowerUpPanel.Instance is null! Make sure the power-up panel exists in the scene.");
        }
    }

    // ✅ FIX: Update RuneDetailsPopup.cs helper method
    private string GetRiskDisplayText(RuneData.UpgradeRisk risk)
    {
        switch (risk)
        {
            case RuneData.UpgradeRisk.Safe: return "Safe";
            case RuneData.UpgradeRisk.Moderate: return "Medium Risk";
            case RuneData.UpgradeRisk.Risky: return "Risky";
            case RuneData.UpgradeRisk.Dangerous: return "Dangerous";
            case RuneData.UpgradeRisk.Extreme: return "EXTREME";
            default: return "Unknown";
        }
    }

    // ✅ FIX: Update RunePowerUpPanel.cs helper methods
    string GetRiskColor(RuneData.UpgradeRisk risk)
    {
        switch (risk)
        {
            case RuneData.UpgradeRisk.Safe: return "#00FF00";      // Green
            case RuneData.UpgradeRisk.Moderate: return "#FFFF00"; // Yellow
            case RuneData.UpgradeRisk.Risky: return "#FF8800";    // Orange
            case RuneData.UpgradeRisk.Dangerous: return "#FF4444"; // Red
            case RuneData.UpgradeRisk.Extreme: return "#FF0088";   // Pink/Magenta
            default: return "#FFFFFF";
        }
    }

    string GetRiskText(RuneData.UpgradeRisk risk)
    {
        switch (risk)
        {
            case RuneData.UpgradeRisk.Safe: return "SAFE UPGRADE";
            case RuneData.UpgradeRisk.Moderate: return "Moderate Risk";
            case RuneData.UpgradeRisk.Risky: return "RISKY!";
            case RuneData.UpgradeRisk.Dangerous: return "DANGEROUS!";
            case RuneData.UpgradeRisk.Extreme: return "EXTREME RISK!";
            default: return "Unknown Risk";
        }
    }


    // REPLACE the OnSellClicked method in RuneDetailsPopup.cs:
    void OnSellClicked()
    {
        if (currentRune == null || isEquipped)
        {
            Debug.LogWarning("Cannot sell: rune is null or equipped!");
            return;
        }

        Debug.Log($"Opening sell panel for rune: {currentRune.runeName}");

        // Open the sell panel
        if (RuneSellPanel.Instance != null)
        {
            RuneSellPanel.Instance.ShowSellPanel(currentRune, () => {
                // Callback when sell is complete
                onRuneChanged?.Invoke();

                // Refresh the UI
                MonsterInventoryUI inventoryUI = FindAnyObjectByType<MonsterInventoryUI>();
                RunePanelUI runePanelUI = inventoryUI?.GetRunePanelUI();
                if (runePanelUI != null)
                {
                    runePanelUI.RefreshCurrentView();
                }

                // Close this popup since the rune no longer exists
                HidePopup();
            });
        }
        else
        {
            Debug.LogError("RuneSellPanel.Instance is null! Make sure the sell panel exists in the scene.");
        }
    }
}
