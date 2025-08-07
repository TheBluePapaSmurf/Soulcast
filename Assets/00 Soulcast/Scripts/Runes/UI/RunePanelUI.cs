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

    [Header("Rune Inventory Sub-Panels")]
    public GameObject runeTypeFilterScrollView;  // Step 1: Type selection
    public GameObject runeItemScrollView;        // Step 2: Filtered runes
    public Button backToFiltersButton;           // Back button

    [Header("Rune Type Filter")]
    public Transform runeTypeFilterContent;
    public GameObject runeTypeFilterButtonPrefab;

    [Header("Rune Items")]
    public Transform runeInventoryContent;
    public GameObject runeItemPrefab;
    public TextMeshProUGUI runeCountText;

    [Header("Other References")]
    public RuneSlotButton[] runeSlots = new RuneSlotButton[6]; // 🔧 CHANGED: RuneSlotUI → RuneSlotButton
    public TextMeshProUGUI setBonusText;

    [Header("🆕 Default View Settings")]
    [SerializeField] private bool startWithRuneInventoryOpen = true;
    [SerializeField] private bool hideRuneSlotsInInventoryView = true; // 🆕 NEW: Option to hide RuneSlotPanel
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool autoSetupOnEnable = true; // 🆕 NEW: Auto setup when panel is enabled

    private CollectedMonster currentMonster;
    private List<RuneItemUI> runeItems = new List<RuneItemUI>();
    private List<RuneTypeFilterButton> typeFilterButtons = new List<RuneTypeFilterButton>();
    private RuneType selectedRuneType = (RuneType)(-1);
    private RuneSetData selectedRuneSet = null;
    private bool isInitialized = false; // 🆕 NEW: Track initialization state

    void Start()
    {
        if (showDebugLogs) Debug.Log("🔧 RunePanelUI Starting...");

        InitializeRunePanel();

        if (showDebugLogs) Debug.Log($"✅ RunePanelUI initialized with default view: {(startWithRuneInventoryOpen ? "Rune Inventory" : "Rune Slots")}");
    }

    // 🆕 NEW: Called when GameObject is enabled (when panel is opened)
    void OnEnable()
    {
        if (autoSetupOnEnable && isInitialized)
        {
            if (showDebugLogs) Debug.Log("🔼 RunePanel enabled, setting up default view...");
            SetupDefaultViewOnEnable();
        }
    }

    // 🆕 NEW: Setup default view when panel is enabled
    void SetupDefaultViewOnEnable()
    {
        // Always ensure proper initialization when panel is opened
        EnsureRuneSlotsAreActive();

        if (startWithRuneInventoryOpen)
        {
            // 🔧 ENHANCED: Force show inventory with type filters when panel opens
            ShowRuneInventoryPanelWithFilters();
        }
        else
        {
            ShowRuneSlotsPanel();
        }
    }

    // 🆕 NEW: Enhanced method to show inventory with filters guaranteed
    public void ShowRuneInventoryPanelWithFilters()
    {
        if (showDebugLogs) Debug.Log("🔼 Showing Rune Inventory with guaranteed filters...");

        // 🆕 NEW: Conditionally hide/show rune slots panel
        if (hideRuneSlotsInInventoryView)
        {
            if (runeSlotsPanel != null)
            {
                runeSlotsPanel.SetActive(false);
                if (showDebugLogs) Debug.Log("🔽 RuneSlotPanel hidden (hideRuneSlotsInInventoryView = true)");
            }
        }
        else
        {
            if (runeSlotsPanel != null)
            {
                runeSlotsPanel.SetActive(true);
                if (showDebugLogs) Debug.Log("🔼 RuneSlotPanel shown (hideRuneSlotsInInventoryView = false)");
            }
        }

        // 🔧 ENHANCED: Always show inventory panel
        if (runeInventoryPanel != null)
        {
            runeInventoryPanel.SetActive(true);
            if (showDebugLogs) Debug.Log("✅ RuneInventoryPanel activated");
        }

        // 🔧 ENHANCED: Force show type selection with extra checks
        ForceShowTypeSelection();

        UpdateTabVisuals(false); // Inventory tab is active

        if (showDebugLogs) Debug.Log("✅ Rune Inventory panel shown with guaranteed type filters");
    }

    // 🆕 NEW: Force show type selection with comprehensive checks
    void ForceShowTypeSelection()
    {
        if (showDebugLogs) Debug.Log("📋 Force showing rune type selection filters...");

        // 🔧 ENHANCED: Double-check panel states
        if (runeTypeFilterScrollView != null)
        {
            runeTypeFilterScrollView.SetActive(true);
            if (showDebugLogs) Debug.Log("✅ RuneTypeFilterScrollView activated");
        }
        else
        {
            Debug.LogError("❌ runeTypeFilterScrollView is NULL!");
        }

        if (runeItemScrollView != null)
        {
            runeItemScrollView.SetActive(false);
            if (showDebugLogs) Debug.Log("🔽 RuneItemScrollView deactivated");
        }

        // 🆕 NEW: Ensure filter buttons are populated
        if (typeFilterButtons.Count == 0)
        {
            if (showDebugLogs) Debug.Log("⚠️ No filter buttons found, setting up type filters...");
            SetupTypeFilters();
        }
        else
        {
            if (showDebugLogs) Debug.Log($"✅ Found {typeFilterButtons.Count} existing filter buttons");
        }

        if (showDebugLogs) Debug.Log($"✅ Type selection forced with {typeFilterButtons.Count} filter buttons");
    }

    // 🔧 ENHANCED: Initialize method with better structure
    void InitializeRunePanel()
    {
        SetupTabs();
        SetupTypeFilters();
        SetupBackButton();

        // 🆕 NEW: Ensure rune slots are properly initialized
        EnsureRuneSlotsAreActive();
        PopulateRuneSlotsArray();

        // 🆕 NEW: Choose default view
        if (startWithRuneInventoryOpen)
        {
            ShowRuneInventoryPanelWithFilters();
        }
        else
        {
            ShowRuneSlotsPanel();
        }

        isInitialized = true; // Mark as initialized
    }

    // 🆕 NEW: Ensure rune slots are active and visible
    void EnsureRuneSlotsAreActive()
    {
        if (showDebugLogs) Debug.Log("🔍 Ensuring RuneSlots are active...");

        RuneSlotButton[] allRuneSlotButtons = FindObjectsByType<RuneSlotButton>(FindObjectsSortMode.None);

        foreach (var runeSlot in allRuneSlotButtons)
        {
            if (!runeSlot.gameObject.activeSelf)
            {
                runeSlot.gameObject.SetActive(true);
                if (showDebugLogs) Debug.Log($"   ✅ Activated RuneSlot: {runeSlot.name}");
            }
        }

        if (showDebugLogs) Debug.Log($"✅ Found and activated {allRuneSlotButtons.Length} RuneSlots");
    }

    // 🆕 NEW: Populate runeSlots array automatically
    void PopulateRuneSlotsArray()
    {
        if (showDebugLogs) Debug.Log("🏗️ Populating RuneSlots array...");

        // Find all RuneSlotButtons in the scene
        RuneSlotButton[] allRuneSlotButtons = FindObjectsByType<RuneSlotButton>(FindObjectsSortMode.None);

        // Sort them by their slotIndex or by name
        var sortedRuneSlots = allRuneSlotButtons
            .Where(rs => rs.name.Contains("RuneSlot_"))
            .OrderBy(rs => ExtractSlotNumber(rs.name))
            .ToArray();

        // Assign to runeSlots array
        for (int i = 0; i < runeSlots.Length && i < sortedRuneSlots.Length; i++)
        {
            runeSlots[i] = sortedRuneSlots[i];
            if (showDebugLogs) Debug.Log($"   ✅ Assigned {sortedRuneSlots[i].name} to runeSlots[{i}]");
        }

        if (showDebugLogs) Debug.Log($"✅ Populated {runeSlots.Length} rune slots");
    }

    // 🆕 NEW: Extract slot number from GameObject name
    int ExtractSlotNumber(string name)
    {
        // Extract number from "RuneSlot_X" format
        if (name.Contains("RuneSlot_"))
        {
            string numberPart = name.Substring(name.LastIndexOf('_') + 1);
            if (int.TryParse(numberPart, out int slotNumber))
            {
                return slotNumber - 1; // Convert to 0-based index
            }
        }
        return 0; // Default to 0 if parsing fails
    }

    void SetupTabs()
    {
        if (runeSlotsTab != null)
            runeSlotsTab.onClick.AddListener(ShowRuneSlotsPanel);
    }

    void SetupBackButton()
    {
        if (backToFiltersButton != null)
            backToFiltersButton.onClick.AddListener(BackToTypeSelection);
    }

    // 🔧 LEGACY: Keep for compatibility
    public void ShowRuneInventoryPanelAsDefault()
    {
        ShowRuneInventoryPanelWithFilters();
    }

    public void ShowRuneSlotsPanel()
    {
        if (showDebugLogs) Debug.Log("🔧 Switching to Rune Slots panel...");

        // Always show rune slots when explicitly switching to slots view
        if (runeSlotsPanel != null) runeSlotsPanel.SetActive(true);
        if (runeInventoryPanel != null) runeInventoryPanel.SetActive(false);

        // 🆕 NEW: Ensure slots are active when showing slots panel
        EnsureRuneSlotsAreActive();

        UpdateTabVisuals(true);

        if (showDebugLogs) Debug.Log("✅ Rune Slots panel shown");
    }

    public void ShowRuneInventoryPanel()
    {
        if (showDebugLogs) Debug.Log("🔼 Switching to Rune Inventory panel...");

        ShowRuneInventoryPanelWithFilters();

        if (showDebugLogs) Debug.Log("✅ Rune Inventory panel shown with type selection");
    }

    public void CloseRuneInventoryPanel()
    {
        if (runeInventoryPanel != null)
            runeInventoryPanel.SetActive(false);
    }

    // Show type filter buttons (Step 1)
    void ShowTypeSelection()
    {
        ForceShowTypeSelection();
    }

    // Show filtered runes (Step 2)
    void ShowFilteredRunes(RuneType runeType)
    {
        selectedRuneType = runeType;

        if (runeTypeFilterScrollView != null) runeTypeFilterScrollView.SetActive(false);
        if (runeItemScrollView != null) runeItemScrollView.SetActive(true);

        // Load and display runes of selected type
        LoadRunesOfType(runeType);

        if (showDebugLogs) Debug.Log($"Showing runes of type: {(runeType == (RuneType)(-1) ? "All Types" : runeType.ToString())}");
    }

    // Go back to type selection
    public void BackToTypeSelection()
    {
        if (showDebugLogs) Debug.Log("🔙 Going back to type selection...");
        ForceShowTypeSelection();
    }

    // Called by RuneTypeFilterButton
    public void OnRuneTypeSelected(RuneType runeType)
    {
        if (showDebugLogs) Debug.Log($"🎯 Rune type selected: {runeType}");
        ShowFilteredRunes(runeType);
    }

    // 🔧 ENHANCED: Setup type filters with better logging
    void SetupTypeFilters()
    {
        if (showDebugLogs) Debug.Log("🏗️ Setting up type filter buttons...");

        if (runeTypeFilterContent == null)
        {
            Debug.LogError("❌ runeTypeFilterContent is null! Cannot setup type filters.");
            return;
        }

        if (runeTypeFilterButtonPrefab == null)
        {
            Debug.LogError("❌ runeTypeFilterButtonPrefab is null! Cannot setup type filters.");
            return;
        }

        // Clear existing buttons
        foreach (var button in typeFilterButtons)
        {
            if (button != null && button.gameObject != null)
                DestroyImmediate(button.gameObject);
        }
        typeFilterButtons.Clear();

        // Get all available rune sets and create buttons
        RuneSetData[] allRuneSets = RuneTypeFilterButton.GetAllRuneSets();

        if (showDebugLogs) Debug.Log($"📦 Found {allRuneSets.Length} rune sets to create buttons for");

        foreach (var runeSet in allRuneSets)
        {
            if (runeSet != null)
            {
                CreateSetFilterButton(runeSet);
            }
        }

        if (showDebugLogs) Debug.Log($"✅ Created {typeFilterButtons.Count} rune set filter buttons");

        // 🆕 NEW: Force layout rebuild after creating buttons
        if (runeTypeFilterContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(runeTypeFilterContent.GetComponent<RectTransform>());
            if (showDebugLogs) Debug.Log("🔄 Layout rebuilt for type filter content");
        }
    }

    void CreateSetFilterButton(RuneSetData runeSet)
    {
        if (showDebugLogs) Debug.Log($"🔨 Creating filter button for set: {runeSet.setName}");

        GameObject buttonObj = Instantiate(runeTypeFilterButtonPrefab, runeTypeFilterContent);
        RuneTypeFilterButton filterButton = buttonObj.GetComponent<RuneTypeFilterButton>();

        if (filterButton != null)
        {
            filterButton.Initialize(runeSet, this);
            typeFilterButtons.Add(filterButton);

            if (showDebugLogs) Debug.Log($"✅ Created filter button: {runeSet.setName}");
        }
        else
        {
            Debug.LogError($"❌ RuneTypeFilterButton component not found on prefab for set: {runeSet.setName}");
        }
    }

    // Rest of your existing methods...
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

    public CollectedMonster GetCurrentMonster()
    {
        return currentMonster;
    }

    public void RefreshCurrentView()
    {
        if (runeItemScrollView != null && runeItemScrollView.activeSelf)
        {
            if (selectedRuneSet != null)
            {
                LoadRunesBySet(selectedRuneSet);
            }
        }

        RefreshRuneSlots();
    }

    // 🔧 UPDATED: Use RuneSlotButton instead of RuneSlotUI
    public void RefreshAllRuneSlotVisuals()
    {
        if (currentMonster == null) return;

        // Ensure slots are active before refreshing
        EnsureRuneSlotsAreActive();

        // Initialize all RuneSlotButtons found in scene
        RuneSlotButton[] runeSlotButtons = FindObjectsByType<RuneSlotButton>(FindObjectsSortMode.None);

        for (int i = 0; i < runeSlotButtons.Length; i++)
        {
            // Use the proper slot index based on GameObject name or position
            int slotIndex = ExtractSlotNumber(runeSlotButtons[i].name);
            runeSlotButtons[i].Initialize(slotIndex, currentMonster, this);
        }

        Debug.Log($"RefreshAllRuneSlotVisuals: Re-initialized {runeSlotButtons.Length} rune slot buttons for {currentMonster.monsterData.monsterName}");
    }

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
        if (runeSet == null)
        {
            Debug.LogWarning("OnRuneSetSelected called with null runeSet - ignoring");
            return;
        }

        selectedRuneSet = runeSet;
        ShowFilteredRunesBySet(runeSet);
    }

    void ShowFilteredRunesBySet(RuneSetData runeSet)
    {
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
    }

    // 🔧 ENHANCED: Use RuneSlotButton Initialize method with better error handling
    public void SetCurrentMonster(CollectedMonster monster)
    {
        if (showDebugLogs) Debug.Log($"🎯 SetCurrentMonster called with: {(monster != null ? monster.monsterData.monsterName : "NULL")}");

        currentMonster = monster;

        if (monster != null)
        {
            // Ensure slots are populated and active
            PopulateRuneSlotsArray();
            EnsureRuneSlotsAreActive();

            InitializeRuneSlotButtons();
            RefreshRuneSlots();
        }
    }

    // 🔧 ENHANCED: Use RuneSlotButton Initialize method with better slot indexing
    private void InitializeRuneSlotButtons()
    {
        if (currentMonster == null)
        {
            if (showDebugLogs) Debug.LogWarning("Cannot initialize rune slot buttons: currentMonster is null");
            return;
        }

        if (showDebugLogs) Debug.Log("🏗️ Initializing rune slot buttons...");

        int initializedCount = 0;

        // Initialize the assigned runeSlots array first
        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (runeSlots[i] != null)
            {
                runeSlots[i].Initialize(i, currentMonster, this);
                initializedCount++;
                if (showDebugLogs) Debug.Log($"   ✅ Initialized runeSlots[{i}]: {runeSlots[i].name}");
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"   ⚠️ runeSlots[{i}] is null");
            }
        }

        // Also initialize any RuneSlotButtons found in the scene (backup)
        RuneSlotButton[] sceneRuneSlots = FindObjectsByType<RuneSlotButton>(FindObjectsSortMode.None);
        foreach (var runeSlot in sceneRuneSlots)
        {
            int slotIndex = ExtractSlotNumber(runeSlot.name);
            runeSlot.Initialize(slotIndex, currentMonster, this);
        }

        if (showDebugLogs) Debug.Log($"✅ Initialized {initializedCount} assigned rune slots + {sceneRuneSlots.Length} scene rune slot buttons for monster: {currentMonster.monsterData.monsterName}");
    }

    // 🔧 ENHANCED: Use RuneSlotButton RefreshSlot method with error handling
    void RefreshRuneSlots()
    {
        if (showDebugLogs) Debug.Log("🔄 Refreshing rune slots...");

        int refreshedCount = 0;

        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (runeSlots[i] != null)
            {
                runeSlots[i].RefreshSlot();
                refreshedCount++;
                if (showDebugLogs) Debug.Log($"   🔄 Refreshed runeSlots[{i}]: {runeSlots[i].name}");
            }
        }

        if (showDebugLogs) Debug.Log($"✅ Refreshed {refreshedCount} rune slots");
    }

    // 🆕 NEW: Context menu methods for testing
    [ContextMenu("Test Show Rune Inventory With Filters")]
    public void TestShowRuneInventoryWithFilters()
    {
        ShowRuneInventoryPanelWithFilters();
    }

    [ContextMenu("Test Force Show Type Selection")]
    public void TestForceShowTypeSelection()
    {
        ForceShowTypeSelection();
    }

    [ContextMenu("Test Setup Type Filters")]
    public void TestSetupTypeFilters()
    {
        SetupTypeFilters();
    }

    [ContextMenu("Test Activate RuneSlots")]
    public void TestActivateRuneSlots()
    {
        EnsureRuneSlotsAreActive();
        PopulateRuneSlotsArray();
    }

    [ContextMenu("Debug Panel States")]
    public void DebugPanelStates()
    {
        Debug.Log($"🔍 Panel States Debug:");
        Debug.Log($"   RuneSlotsPanel: {(runeSlotsPanel != null ? runeSlotsPanel.activeSelf.ToString() : "NULL")}");
        Debug.Log($"   RuneInventoryPanel: {(runeInventoryPanel != null ? runeInventoryPanel.activeSelf.ToString() : "NULL")}");
        Debug.Log($"   RuneTypeFilterScrollView: {(runeTypeFilterScrollView != null ? runeTypeFilterScrollView.activeSelf.ToString() : "NULL")}");
        Debug.Log($"   RuneItemScrollView: {(runeItemScrollView != null ? runeItemScrollView.activeSelf.ToString() : "NULL")}");
        Debug.Log($"   Type Filter Buttons Count: {typeFilterButtons.Count}");
        Debug.Log($"   Hide RuneSlots In Inventory: {hideRuneSlotsInInventoryView}");
        Debug.Log($"   Assigned RuneSlots: {runeSlots.Count(rs => rs != null)}/{runeSlots.Length}");
        Debug.Log($"   Is Initialized: {isInitialized}");
        Debug.Log($"   Auto Setup On Enable: {autoSetupOnEnable}");

        // Debug individual rune slots
        for (int i = 0; i < runeSlots.Length; i++)
        {
            Debug.Log($"     runeSlots[{i}]: {(runeSlots[i] != null ? runeSlots[i].name + " (Active: " + runeSlots[i].gameObject.activeSelf + ")" : "NULL")}");
        }
    }
}
