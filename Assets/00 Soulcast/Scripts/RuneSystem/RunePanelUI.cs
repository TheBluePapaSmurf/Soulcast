using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class RunePanelUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject runeSlotsPanel;
    public GameObject runeInventoryPanel;

    [Header("Tab Buttons")]
    public Button runeSlotsTab;
    public Button runeInventoryTab;

    [Header("Rune Inventory Sub-Panels")] // UPDATED SECTION
    public GameObject runeTypeFilterScrollView;  // Step 1: Type selection
    public GameObject runeItemScrollView;        // Step 2: Filtered runes
    public Button backToFiltersButton;           // Back button
    public Button closeInventoryButton;

    [Header("Rune Type Filter")]
    public Transform runeTypeFilterContent;
    public GameObject runeTypeFilterButtonPrefab;

    [Header("Rune Items")]
    public Transform runeInventoryContent;
    public GameObject runeItemPrefab;
    public TextMeshProUGUI runeCountText;

    [Header("Other References")]
    public RuneSlotUI[] runeSlots = new RuneSlotUI[6];
    public TextMeshProUGUI setBonusText;

    private CollectedMonster currentMonster;
    private List<RuneItemUI> runeItems = new List<RuneItemUI>();
    private List<RuneTypeFilterButton> typeFilterButtons = new List<RuneTypeFilterButton>();
    private RuneType selectedRuneType = (RuneType)(-1);
    private RuneSetData selectedRuneSet = null;
    void Start()
    {
        SetupTabs();
        SetupTypeFilters();
        SetupBackButton();
        ShowRuneSlotsPanel(); // Default to rune slots
    }

    void SetupTabs()
    {
        if (runeSlotsTab != null)
            runeSlotsTab.onClick.AddListener(ShowRuneSlotsPanel);

        if (runeInventoryTab != null)
            runeInventoryTab.onClick.AddListener(ShowRuneInventoryPanel);
    }

    void SetupBackButton()
    {
        if (backToFiltersButton != null)
            backToFiltersButton.onClick.AddListener(BackToTypeSelection);

        if (closeInventoryButton != null)
            closeInventoryButton.onClick.AddListener(CloseRuneInventoryPanel);
    }

    public void ShowRuneSlotsPanel()
    {
        // Show rune slots, hide inventory
        if (runeSlotsPanel != null) runeSlotsPanel.SetActive(true);
        if (runeInventoryPanel != null) runeInventoryPanel.SetActive(false);

        UpdateTabVisuals(true);
    }

    public void ShowRuneInventoryPanel()
    {
        if (runeInventoryPanel != null)
            runeInventoryPanel.SetActive(true);

        ShowTypeSelection();
    }

    public void CloseRuneInventoryPanel()
    {
        if (runeInventoryPanel != null)
            runeInventoryPanel.SetActive(false);
    }

    // NEW METHOD: Show type filter buttons (Step 1)
    void ShowTypeSelection()
    {
        if (runeTypeFilterScrollView != null) runeTypeFilterScrollView.SetActive(true);
        if (runeItemScrollView != null) runeItemScrollView.SetActive(false);

        Debug.Log("Showing rune type selection");
    }

    // NEW METHOD: Show filtered runes (Step 2)
    void ShowFilteredRunes(RuneType runeType)
    {
        selectedRuneType = runeType;

        if (runeTypeFilterScrollView != null) runeTypeFilterScrollView.SetActive(false);
        if (runeItemScrollView != null) runeItemScrollView.SetActive(true);

        // Load and display runes of selected type
        LoadRunesOfType(runeType);

        Debug.Log($"Showing runes of type: {(runeType == (RuneType)(-1) ? "All Types" : runeType.ToString())}");
    }

    // NEW METHOD: Go back to type selection
    public void BackToTypeSelection()
    {
        ShowTypeSelection();
    }

    // NEW METHOD: Called by RuneTypeFilterButton
    public void OnRuneTypeSelected(RuneType runeType)
    {
        ShowFilteredRunes(runeType);
    }

    // ADD this method to RunePanelUI.cs:
    private void FilterRunesForSlot(int targetSlotIndex)
    {
        RuneSlotPosition targetPosition = (RuneSlotPosition)targetSlotIndex;
        RuneType targetType = (RuneType)targetSlotIndex;

        var compatibleRunes = PlayerInventory.Instance.ownedRunes
            .Where(rune => rune != null &&
                   rune.runeSlotPosition == targetPosition &&
                   rune.runeType == targetType)
            .ToList();

        DisplayFilteredRunes(compatibleRunes);
        Debug.Log($"Showing {compatibleRunes.Count} runes for {targetPosition} ({targetType})");
    }

    // ADD this helper method:
    private void DisplayFilteredRunes(List<RuneData> runes)
    {
        ClearRuneInventory();

        foreach (var rune in runes)
        {
            GameObject itemObj = Instantiate(runeItemPrefab, runeInventoryContent);
            RuneItemUI runeItem = itemObj.GetComponent<RuneItemUI>();

            if (runeItem != null)
            {
                bool isEquipped = IsRuneEquipped(rune);
                runeItem.Setup(rune, isEquipped);
                runeItems.Add(runeItem);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(runeInventoryContent.GetComponent<RectTransform>());
    }

    // REPLACE the old FilterRunesByTypeAndSlot method with this:
    public void FilterRunesBySlotPosition(RuneSlotPosition slotPosition)
    {
        var compatibleRunes = PlayerInventory.Instance.ownedRunes
            .Where(rune => rune != null && rune.runeSlotPosition == slotPosition)
            .ToList();

        DisplayFilteredRunes(compatibleRunes);
        Debug.Log($"Filtered to {compatibleRunes.Count} runes for slot position {slotPosition}");
    }


    void LoadRunesOfType(RuneType runeType)
    {
        Debug.Log("=== LoadRunesOfType Debug ===");
        Debug.Log($"Selected RuneType: {(runeType >= 0 ? runeType.ToString() : "All Types")}");
        Debug.Log($"PlayerInventory.Instance: {(PlayerInventory.Instance != null ? "Found" : "NULL")}");
        Debug.Log($"runeInventoryContent: {(runeInventoryContent != null ? "Found" : "NULL")}");
        Debug.Log($"runeItemPrefab: {(runeItemPrefab != null ? "Found" : "NULL")}");

        ClearRuneInventory();

        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("PlayerInventory.Instance is NULL!");
            return;
        }

        // Check total runes first
        int totalRunes = PlayerInventory.Instance.GetRuneCount();
        Debug.Log($"Total runes in PlayerInventory: {totalRunes}");

        if (totalRunes == 0)
        {
            Debug.LogWarning("PlayerInventory has NO RUNES! Need to add default runes.");
            return;
        }

        List<RuneData> filteredRunes;

        // Get runes based on selected type
        if (runeType >= 0)
        {
            filteredRunes = PlayerInventory.Instance.GetRunesByType(runeType);
            Debug.Log($"Found {filteredRunes.Count} runes of type {runeType}");
        }
        else
        {
            filteredRunes = PlayerInventory.Instance.ownedRunes.ToList();
            Debug.Log($"Found {filteredRunes.Count} total runes");
        }

        if (filteredRunes.Count == 0)
        {
            Debug.LogWarning($"No runes found for type: {(runeType >= 0 ? runeType.ToString() : "All Types")}");
            return;
        }

        // Create UI items for each rune
        int createdItems = 0;
        foreach (var rune in filteredRunes)
        {
            if (rune == null)
            {
                Debug.LogWarning("Found NULL rune in filteredRunes list!");
                continue;
            }

            Debug.Log($"Creating UI for rune: {rune.runeName} ({rune.runeType})");

            GameObject itemObj = Instantiate(runeItemPrefab, runeInventoryContent);
            if (itemObj == null)
            {
                Debug.LogError("Failed to instantiate runeItemPrefab!");
                continue;
            }

            RuneItemUI runeItem = itemObj.GetComponent<RuneItemUI>();
            if (runeItem == null)
            {
                Debug.LogError("RuneItemUI component not found on instantiated prefab!");
                continue;
            }

            // Check if this rune is equipped
            bool isEquipped = IsRuneEquipped(rune);
            runeItem.Setup(rune, isEquipped);
            runeItems.Add(runeItem);
            createdItems++;
        }

        Debug.Log($"Successfully created {createdItems} rune UI items");
        UpdateRuneCountDisplay(filteredRunes.Count, runeType);

        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(runeInventoryContent.GetComponent<RectTransform>());

        Debug.Log("=== LoadRunesOfType Complete ===");
    }

    void UpdateRuneCountDisplay(int displayedCount, RuneType runeType)
    {
        if (runeCountText == null) return;

        string typeText = runeType >= 0 ? runeType.ToString() : "All Types";
        runeCountText.text = $"{typeText}: {displayedCount} runes";
    }

    // Add this method to get current monster
    public CollectedMonster GetCurrentMonster()
    {
        return currentMonster;
    }

    // Add this method to refresh the current view
    public void RefreshCurrentView()
    {
        if (runeItemScrollView != null && runeItemScrollView.activeSelf)
        {
            // Currently showing filtered runes - refresh them using the NEW system only
            if (selectedRuneSet != null)
            {
                LoadRunesBySet(selectedRuneSet);
            }
            // REMOVED: The old LoadRunesOfType call that was causing duplicates
        }

        RefreshRuneSlots();
    }

    // ADD this method to RunePanelUI.cs:
    public void RefreshAllRuneSlotVisuals()
    {
        if (currentMonster == null) return;

        // Re-initialize all rune slot buttons to ensure they have the correct monster reference
        RuneSlotButton[] runeSlotButtons = FindObjectsByType<RuneSlotButton>(FindObjectsSortMode.None);

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            runeSlotButtons[i].Initialize(i, currentMonster, this);
        }

        Debug.Log($"RefreshAllRuneSlotVisuals: Re-initialized {runeSlotButtons.Length} rune slot buttons for {currentMonster.monsterData.monsterName}");
    }

    void SetupTypeFilters()
    {
        if (runeTypeFilterContent == null || runeTypeFilterButtonPrefab == null) return;

        // Clear existing buttons
        foreach (var button in typeFilterButtons)
        {
            if (button != null && button.gameObject != null)
                DestroyImmediate(button.gameObject);
        }
        typeFilterButtons.Clear();

        // REMOVED: CreateSetFilterButton(null, true); - No more "All Sets" button

        // Get all available rune sets and create buttons (only specific sets)
        RuneSetData[] allRuneSets = RuneTypeFilterButton.GetAllRuneSets();

        foreach (var runeSet in allRuneSets)
        {
            if (runeSet != null)
            {
                CreateSetFilterButton(runeSet); // Updated method call
            }
        }

        Debug.Log($"Created {typeFilterButtons.Count} rune set filter buttons");
    }

    void CreateSetFilterButton(RuneSetData runeSet)
    {
        GameObject buttonObj = Instantiate(runeTypeFilterButtonPrefab, runeTypeFilterContent);
        RuneTypeFilterButton filterButton = buttonObj.GetComponent<RuneTypeFilterButton>();

        if (filterButton != null)
        {
            filterButton.Initialize(runeSet, this); // Removed isAllSetsButton parameter
            typeFilterButtons.Add(filterButton);
        }
    }

    // Helper methods
    private bool IsRuneEquipped(RuneData rune)
    {
        if (PlayerInventory.Instance == null) return false;

        var allMonsters = PlayerInventory.Instance.GetAllMonsters();

        foreach (var monster in allMonsters)
        {
            foreach (var slot in monster.runeSlots)
            {
                if (slot.equippedRune == rune)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void OnRuneSetSelected(RuneSetData runeSet)
    {
        // VALIDATION: Ensure we always have a specific rune set
        if (runeSet == null)
        {
            Debug.LogWarning("OnRuneSetSelected called with null runeSet - ignoring");
            return;
        }

        selectedRuneSet = runeSet;
        ShowFilteredRunesBySet(runeSet);
    }

    // NEW METHOD: Show runes filtered by set
    void ShowFilteredRunesBySet(RuneSetData runeSet)
    {
        // VALIDATION: Ensure we have a specific rune set
        if (runeSet == null)
        {
            Debug.LogWarning("ShowFilteredRunesBySet called with null runeSet - returning");
            return;
        }

        if (runeTypeFilterScrollView != null) runeTypeFilterScrollView.SetActive(false);
        if (runeItemScrollView != null) runeItemScrollView.SetActive(true);

        LoadRunesBySet(runeSet);

        Debug.Log($"Showing runes of set: {runeSet.setName}");
    }

    void LoadRunesBySet(RuneSetData runeSet)
    {
        Debug.Log("=== LoadRunesBySet Debug ===");

        // VALIDATION: Must have a specific rune set
        if (runeSet == null)
        {
            Debug.LogError("LoadRunesBySet called with null runeSet!");
            return;
        }

        Debug.Log($"Selected RuneSet: {runeSet.setName}");

        ClearRuneInventory();

        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("PlayerInventory.Instance is NULL!");
            return;
        }

        // UPDATED: Only filter by specific rune set - no "all sets" option
        List<RuneData> filteredRunes = PlayerInventory.Instance.ownedRunes
            .Where(rune => rune != null && rune.runeSet == runeSet)
            .ToList();

        Debug.Log($"Found {filteredRunes.Count} runes of set {runeSet.setName}");

        if (filteredRunes.Count == 0)
        {
            Debug.LogWarning($"No runes found for set: {runeSet.setName}");
            return;
        }

        // Create UI items for each rune
        int createdItems = 0;
        foreach (var rune in filteredRunes)
        {
            if (rune == null) continue;

            GameObject itemObj = Instantiate(runeItemPrefab, runeInventoryContent);
            if (itemObj == null) continue;

            RuneItemUI runeItem = itemObj.GetComponent<RuneItemUI>();
            if (runeItem == null) continue;

            bool isEquipped = IsRuneEquipped(rune);
            runeItem.Setup(rune, isEquipped);
            runeItems.Add(runeItem);
            createdItems++;
        }

        Debug.Log($"Successfully created {createdItems} rune UI items");
        UpdateRuneCountDisplayForSet(filteredRunes.Count, runeSet);

        LayoutRebuilder.ForceRebuildLayoutImmediate(runeInventoryContent.GetComponent<RectTransform>());
    }

    void UpdateRuneCountDisplayForSet(int displayedCount, RuneSetData runeSet)
    {
        if (runeCountText == null || runeSet == null) return;

        // UPDATED: Always show specific set name - no "All Sets" option
        runeCountText.text = $"{runeSet.setName}: {displayedCount} runes";
    }


    void ClearRuneInventory()
    {
        foreach (var item in runeItems)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        runeItems.Clear();
    }

    void UpdateTabVisuals(bool slotsTabActive)
    {
        // Update tab button visuals
        if (runeSlotsTab != null)
        {
            runeSlotsTab.image.color = slotsTabActive ? Color.white : Color.gray;
        }

        if (runeInventoryTab != null)
        {
            runeInventoryTab.image.color = slotsTabActive ? Color.gray : Color.white;
        }
    }

    public void SetCurrentMonster(CollectedMonster monster)
    {
        currentMonster = monster;

        // ADDED: Initialize all RuneSlotButton components with the current monster
        InitializeRuneSlotButtons();

        RefreshRuneSlots();
    }

    // ADD this new method to RunePanelUI.cs:
    private void InitializeRuneSlotButtons()
    {
        if (currentMonster == null) return;

        // Find all RuneSlotButton components in the scene
        RuneSlotButton[] runeSlotButtons = FindObjectsByType<RuneSlotButton>(FindObjectsSortMode.None);

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            // Initialize each button with its slot index and current monster
            runeSlotButtons[i].Initialize(i, currentMonster, this);
        }

        Debug.Log($"Initialized {runeSlotButtons.Length} rune slot buttons for monster: {currentMonster.monsterData.monsterName}");
    }


    void RefreshRuneSlots()
    {
        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (runeSlots[i] != null)
            {
                runeSlots[i].SetTargetMonster(currentMonster);
            }
        }
    }
}
