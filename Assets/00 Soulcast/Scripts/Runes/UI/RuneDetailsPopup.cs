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

        // Basic rune info
        if (runeIcon != null)
            runeIcon.sprite = currentRune.runeIcon;

        if (runeNameText != null)
            runeNameText.text = currentRune.runeName;

        if (runeLevelText != null)
            runeLevelText.text = $"Level +{currentRune.currentLevel}";

        // Main stat
        if (mainStatText != null && currentRune.mainStat != null)
        {
            mainStatText.text = $"<b>Main Stat:</b>\n{currentRune.mainStat.GetDisplayText()}";
        }

        // Sub stats
        if (subStatsText != null)
        {
            string subStatsDisplay = "<b>Sub Stats:</b>\n";

            if (currentRune.subStats != null && currentRune.subStats.Count > 0)
            {
                foreach (var subStat in currentRune.subStats)
                {
                    if (subStat != null)
                    {
                        subStatsDisplay += $"• {subStat.GetDisplayText()}\n";
                    }
                }
            }
            else
            {
                subStatsDisplay += "No sub stats";
            }

            subStatsText.text = subStatsDisplay;
        }

        // Set information
        if (setTenText != null)
        {
            if (currentRune.runeSet != null)
            {
                setTenText.text = $"<b>Set:</b> {currentRune.runeSet.setName}\n" +
                                 $"<b>Set Bonus:</b>\n{currentRune.runeSet.GetSetBonusDescription(2)}";
            }
            else
            {
                setTenText.text = "<b>Set:</b> None";
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

        // Power-up button
        if (powerUpButton != null)
        {
            powerUpButton.interactable = currentRune.currentLevel < currentRune.maxLevel;

            var powerUpButtonText = powerUpButton.GetComponentInChildren<TextMeshProUGUI>();
            if (powerUpButtonText != null)
            {
                if (currentRune.currentLevel >= currentRune.maxLevel)
                    powerUpButtonText.text = "Max Level";
                else
                    powerUpButtonText.text = "Power Up";
            }
        }

        // Sell button
        if (sellButton != null)
        {
            sellButton.interactable = !isEquipped;

            var sellButtonText = sellButton.GetComponentInChildren<TextMeshProUGUI>();
            if (sellButtonText != null)
            {
                if (isEquipped)
                    sellButtonText.text = "Can't Sell (Equipped)";
                else
                    sellButtonText.text = "Sell";
            }
        }
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

    // REPLACE the EquipRune method in RuneDetailsPopup.cs:
    // REPLACE the EquipRune method in RuneDetailsPopup.cs:
    void EquipRune()
    {
        Debug.Log($"Equipping rune: {currentRune.runeName}");

        // Get the current monster
        MonsterInventoryUI inventoryUI = FindAnyObjectByType<MonsterInventoryUI>();
        RunePanelUI runePanelUI = inventoryUI?.GetRunePanelUI();
        CollectedMonster selectedMonster = runePanelUI?.GetCurrentMonster();

        if (selectedMonster == null)
        {
            Debug.LogWarning("No monster selected for equipping rune!");
            return;
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
            if (inventoryUI != null)
            {
                inventoryUI.RefreshCurrentMonsterStats();
            }

            // Call the callback if provided
            onRuneChanged?.Invoke();

            Debug.Log($"Successfully equipped {currentRune.runeName} to {selectedMonster.monsterData.monsterName} slot {targetSlotIndex}");
            HidePopup();
        }
        else
        {
            Debug.LogError($"Failed to equip {currentRune.runeName}");
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
