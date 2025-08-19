using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;

public class MonsterUpgradePanel : MonoBehaviour
{
    [Header("Panel Animation")]
    public CanvasGroup canvasGroup;
    public RectTransform panelTransform;
    public float animationDuration = 0.3f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;

    [Header("Target Monster Display")]
    public Image targetMonsterIcon;
    public TextMeshProUGUI targetMonsterName;
    public TextMeshProUGUI targetMonsterLevel;
    public Transform targetStarContainer;
    public GameObject starPrefab;

    [Header("✨ Selected Monster Preview (Optional - can be same as Target)")]
    public Image selectedMonsterIcon;
    public TextMeshProUGUI selectedMonsterName;
    public TextMeshProUGUI selectedMonsterLevel;
    public Transform selectedMonsterStarContainer;
    public TextMeshProUGUI selectedMonsterStats;

    [Header("Upgrade Requirements")]
    public TextMeshProUGUI requirementText;
    public Transform materialSlotsContainer;
    public GameObject materialSlotPrefab;

    [Header("✨ Monster Inventory System")]
    public Transform allMonstersContainer;
    public GameObject monsterCardPrefab; // UniversalMonsterCard prefab
    public ScrollRect monstersScrollView;

    [Header("✨ Monster Filtering")]
    public Toggle showAllToggle;
    public Toggle showUpgradeableToggle;
    public Toggle showMaterialsToggle;
    public TMP_Dropdown starFilterDropdown;
    public TMP_Dropdown elementFilterDropdown;
    public TMP_InputField searchInput;

    [Header("✨ Monster Sorting")]
    public TMP_Dropdown sortDropdown;
    public Button sortOrderButton;
    public TextMeshProUGUI sortOrderText;

    [Header("Upgrade Button")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;

    [Header("Control Buttons")]
    public Button closeButton;
    public Button clearMaterialsButton;

    [Header("Preview")]
    public GameObject previewPanel;
    public TextMeshProUGUI previewStarLevel;
    public TextMeshProUGUI previewStats;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Private fields
    private CollectedMonster selectedTargetMonster; // Single monster reference - serves as both target and preview
    private List<CollectedMonster> selectedMaterials = new List<CollectedMonster>();
    private List<MaterialSlotUI> materialSlots = new List<MaterialSlotUI>();
    private List<UniversalMonsterCard> monsterCards = new List<UniversalMonsterCard>();
    private UpgradeRequirement currentRequirement;

    // Filtering and sorting
    private FilterMode currentFilterMode = FilterMode.All;
    private SortMode currentSortMode = SortMode.Level;
    private bool sortAscending = false;
    private int starFilter = -1; // -1 = all stars
    private ElementType? elementFilter = null; // null = all elements
    private string searchQuery = "";

    public enum FilterMode
    {
        All,
        Upgradeable,
        Materials
    }

    public enum SortMode
    {
        Name,
        Level,
        StarLevel,
        PowerRating,
        Element
    }

    private void Start()
    {
        SetupUI();
        gameObject.SetActive(false);
    }

    private void SetupUI()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (clearMaterialsButton != null)
            clearMaterialsButton.onClick.AddListener(ClearMaterials);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(PerformUpgrade);

        SetupFilteringControls();
        SetupSortingControls();

        // Initially disable upgrade button
        SetUpgradeButtonEnabled(false);

        // Ensure CanvasGroup is properly configured for interaction
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void SetupFilteringControls()
    {
        // Filter toggles
        if (showAllToggle != null)
        {
            showAllToggle.onValueChanged.AddListener((value) => {
                if (value) SetFilterMode(FilterMode.All);
            });
            showAllToggle.isOn = true;
        }

        if (showUpgradeableToggle != null)
        {
            showUpgradeableToggle.onValueChanged.AddListener((value) => {
                if (value) SetFilterMode(FilterMode.Upgradeable);
            });
        }

        if (showMaterialsToggle != null)
        {
            showMaterialsToggle.onValueChanged.AddListener((value) => {
                if (value) SetFilterMode(FilterMode.Materials);
            });
        }

        // Star filter dropdown
        if (starFilterDropdown != null)
        {
            starFilterDropdown.onValueChanged.AddListener(OnStarFilterChanged);
            SetupStarFilterDropdown();
        }

        // Element filter dropdown
        if (elementFilterDropdown != null)
        {
            elementFilterDropdown.onValueChanged.AddListener(OnElementFilterChanged);
            SetupElementFilterDropdown();
        }

        // Search input
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchQueryChanged);
        }
    }

    private void SetupSortingControls()
    {
        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.AddListener(OnSortModeChanged);
            SetupSortDropdown();
        }

        if (sortOrderButton != null)
        {
            sortOrderButton.onClick.AddListener(ToggleSortOrder);
            UpdateSortOrderDisplay();
        }
    }

    private void SetupStarFilterDropdown()
    {
        if (starFilterDropdown == null) return;

        starFilterDropdown.options.Clear();
        starFilterDropdown.options.Add(new TMP_Dropdown.OptionData("All Stars"));
        for (int i = 1; i <= 6; i++)
        {
            starFilterDropdown.options.Add(new TMP_Dropdown.OptionData($"{i}⭐"));
        }
        starFilterDropdown.value = 0;
    }

    private void SetupElementFilterDropdown()
    {
        if (elementFilterDropdown == null) return;

        elementFilterDropdown.options.Clear();
        elementFilterDropdown.options.Add(new TMP_Dropdown.OptionData("All Elements"));

        // Add all available elements from your ElementType enum
        var elements = System.Enum.GetValues(typeof(ElementType));
        foreach (ElementType element in elements)
        {
            elementFilterDropdown.options.Add(new TMP_Dropdown.OptionData(element.ToString()));
        }
        elementFilterDropdown.value = 0;
    }

    private void SetupSortDropdown()
    {
        if (sortDropdown == null) return;

        sortDropdown.options.Clear();
        sortDropdown.options.Add(new TMP_Dropdown.OptionData("Name"));
        sortDropdown.options.Add(new TMP_Dropdown.OptionData("Level"));
        sortDropdown.options.Add(new TMP_Dropdown.OptionData("Star Level"));
        sortDropdown.options.Add(new TMP_Dropdown.OptionData("Power Rating"));
        sortDropdown.options.Add(new TMP_Dropdown.OptionData("Element"));
        sortDropdown.value = 1; // Default to Level
    }

    #region Filter and Sort Events

    private void SetFilterMode(FilterMode mode)
    {
        currentFilterMode = mode;
        if (showDebugLogs) Debug.Log($"🔍 Filter mode changed to: {mode}");
        RefreshMonsterList();
    }

    private void OnStarFilterChanged(int index)
    {
        starFilter = index == 0 ? -1 : index; // 0 = all, 1+ = specific star level
        if (showDebugLogs) Debug.Log($"⭐ Star filter changed to: {(starFilter == -1 ? "All" : starFilter.ToString())}");
        RefreshMonsterList();
    }

    private void OnElementFilterChanged(int index)
    {
        if (index == 0)
        {
            elementFilter = null; // Represents "all elements"
        }
        else
        {
            var elements = System.Enum.GetValues(typeof(ElementType));
            var elementArray = elements.Cast<ElementType>().ToArray();
            if (index - 1 < elementArray.Length)
            {
                elementFilter = elementArray[index - 1];
            }
        }
        if (showDebugLogs) Debug.Log($"🌟 Element filter changed to: {(elementFilter?.ToString() ?? "All")}");
        RefreshMonsterList();
    }

    private void OnSearchQueryChanged(string query)
    {
        searchQuery = query.ToLower();
        if (showDebugLogs) Debug.Log($"🔍 Search query changed to: '{searchQuery}'");
        RefreshMonsterList();
    }

    private void OnSortModeChanged(int index)
    {
        currentSortMode = (SortMode)index;
        if (showDebugLogs) Debug.Log($"📊 Sort mode changed to: {currentSortMode}");
        RefreshMonsterList();
    }

    private void ToggleSortOrder()
    {
        sortAscending = !sortAscending;
        UpdateSortOrderDisplay();
        if (showDebugLogs) Debug.Log($"📊 Sort order changed to: {(sortAscending ? "Ascending" : "Descending")}");
        RefreshMonsterList();
    }

    private void UpdateSortOrderDisplay()
    {
        if (sortOrderText != null)
        {
            sortOrderText.text = sortAscending ? "↑" : "↓";
        }
    }

    #endregion

    /// <summary>
    /// Open the upgrade panel
    /// </summary>
    public void OpenPanel()
    {
        if (showDebugLogs) Debug.Log("🔮 Opening MonsterUpgradePanel");

        gameObject.SetActive(true);

        // Ensure CanvasGroup allows interaction
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Set initial target monster if none selected
        if (selectedTargetMonster == null)
        {
            var upgradeableMonsters = GetUpgradeableMonsters();
            if (upgradeableMonsters.Count > 0)
            {
                SetTargetMonster(upgradeableMonsters[0]);
            }
            else
            {
                // No upgradeable monsters, just select first available
                var allMonsters = GetFilteredAndSortedMonsters();
                if (allMonsters.Count > 0)
                {
                    SetTargetMonster(allMonsters[0]);
                }
            }
        }

        RefreshUI();
        AnimateShow();
    }

    /// <summary>
    /// Close the upgrade panel
    /// </summary>
    public void ClosePanel()
    {
        if (showDebugLogs) Debug.Log("🔮 Closing MonsterUpgradePanel");

        AnimateHide(() => {
            gameObject.SetActive(false);
            ClearSelection();
            NotifyManagerOfClose();
        });
    }

    /// <summary>
    /// Notify MonsterUpgradeManager that panel is being closed
    /// </summary>
    private void NotifyManagerOfClose()
    {
        if (showDebugLogs) Debug.Log("🔄 Notifying MonsterUpgradeManager and Altars that panel is closed");

        // Notify upgrade altars directly
        var upgradeAltars = FindObjectsByType<AltarInteraction>(FindObjectsSortMode.None);
        foreach (var altar in upgradeAltars)
        {
            if (altar.altarType == AltarType.Upgrade)
            {
                altar.OnSystemUIClosed();
                if (showDebugLogs) Debug.Log($"✅ Notified {altar.name} that upgrade panel is closed");
            }
        }
    }

    /// <summary>
    /// Set the target monster for upgrade (also updates preview automatically)
    /// </summary>
    public void SetTargetMonster(CollectedMonster monster)
    {
        selectedTargetMonster = monster;

        if (showDebugLogs) Debug.Log($"🎯 Target monster set to: {monster?.GetDisplayName() ?? "NULL"}");

        // Clear any materials that are the same as the new target
        selectedMaterials.RemoveAll(m => m == monster);

        // Update all UI elements
        RefreshTargetMonsterDisplay();
        RefreshRequirements();
        RefreshMaterialSlots();
        RefreshMonsterList();
        RefreshUpgradeButton();
        RefreshPreview();
    }

    /// <summary>
    /// Clear all selected materials
    /// </summary>
    public void ClearMaterials()
    {
        selectedMaterials.Clear();
        RefreshMaterialSlots();
        RefreshUpgradeButton();
        RefreshMonsterList(); // Refresh to update availability
    }

    public void AddMaterial(CollectedMonster material)
    {
        if (material == null) return;
        if (selectedMaterials.Contains(material)) return;
        if (currentRequirement == null || !currentRequirement.canUpgrade) return;
        if (selectedMaterials.Count >= currentRequirement.requiredCount) return;

        // Validate material can be used
        if (!CanUseAsUpgradeMaterial(material))
        {
            if (showDebugLogs) Debug.Log($"❌ Cannot use {material.GetDisplayName()} as material");
            return;
        }

        selectedMaterials.Add(material);

        if (showDebugLogs) Debug.Log($"✅ Added {material.GetDisplayName()} as material ({selectedMaterials.Count}/{currentRequirement.requiredCount})");

        RefreshMaterialSlots();
        RefreshUpgradeButton();
        RefreshMonsterList(); // Update card visuals

        // Show progress feedback
        if (selectedMaterials.Count == currentRequirement.requiredCount)
        {
            if (showDebugLogs) Debug.Log($"🎉 All materials selected! Ready to upgrade {selectedTargetMonster.GetDisplayName()}");
            ShowReadyToUpgradeMessage();
        }
    }

    /// <summary>
    /// Remove material monster from selection
    /// </summary>
    public void RemoveMaterial(CollectedMonster material)
    {
        if (material == null) return;
        if (!selectedMaterials.Contains(material)) return;

        selectedMaterials.Remove(material);

        if (showDebugLogs) Debug.Log($"🗑️ Removed {material.GetDisplayName()} from materials ({selectedMaterials.Count}/{currentRequirement?.requiredCount ?? 0})");

        RefreshMaterialSlots();
        RefreshUpgradeButton();
        RefreshMonsterList(); // Update card visuals
    }

    /// <summary>
    /// Show visual feedback when ready to upgrade
    /// </summary>
    private void ShowReadyToUpgradeMessage()
    {
        // Optional: Add visual/audio feedback
        if (upgradeButton != null)
        {
            // Could add glow effect, color change, etc.
            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = "UPGRADE NOW!";
                upgradeButtonText.color = Color.green;
            }
        }

        // Optional: Play sound effect
        // AudioSource.PlayClipAtPoint(readySound, Camera.main.transform.position);
    }


    /// <summary>
    /// Perform the upgrade
    /// </summary>
    private void PerformUpgrade()
    {
        if (selectedTargetMonster == null || selectedMaterials.Count == 0) return;

        if (showDebugLogs)
        {
            Debug.Log($"🔮 Attempting to upgrade {selectedTargetMonster.GetDisplayName()} from {selectedTargetMonster.currentStarLevel}⭐ to {selectedTargetMonster.currentStarLevel + 1}⭐");
            Debug.Log($"🔧 Using {selectedMaterials.Count} materials: {string.Join(", ", selectedMaterials.ConvertAll(m => m.GetDisplayName()))}");
        }

        bool success = MonsterUpgradeManager.Instance.UpgradeMonster(selectedTargetMonster, selectedMaterials);

        if (success)
        {
            if (showDebugLogs) Debug.Log($"✅ Upgrade successful! {selectedTargetMonster.GetDisplayName()} is now {selectedTargetMonster.currentStarLevel}⭐");

            ShowUpgradeSuccess();

            // Clear materials but keep target selected to see the result
            selectedMaterials.Clear();

            // Refresh UI to show the upgraded monster
            RefreshUI();

            // Reset upgrade button text
            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = "UPGRADE";
                upgradeButtonText.color = Color.white;
            }
        }
        else
        {
            if (showDebugLogs) Debug.Log($"❌ Upgrade failed for {selectedTargetMonster.GetDisplayName()}");
            ShowUpgradeError();
        }
    }


    /// <summary>
    /// Refresh the entire UI
    /// </summary>
    private void RefreshUI()
    {
        RefreshTargetMonsterDisplay(); // This now handles both target and preview
        RefreshRequirements();
        RefreshMaterialSlots();
        RefreshMonsterList();
        RefreshUpgradeButton();
        RefreshPreview();
    }

    /// <summary>
    /// Refresh target monster display (serves as both target and preview)
    /// </summary>
    private void RefreshTargetMonsterDisplay()
    {
        if (selectedTargetMonster == null)
        {
            // Try to select first available monster
            var allMonsters = GetFilteredAndSortedMonsters();
            if (allMonsters.Count > 0)
            {
                selectedTargetMonster = allMonsters[0];
            }
        }

        if (selectedTargetMonster == null)
        {
            // Clear display if no monster available
            ClearTargetMonsterDisplay();
            return;
        }

        // Update target monster icon
        if (targetMonsterIcon != null && selectedTargetMonster.monsterData?.icon != null)
        {
            targetMonsterIcon.sprite = selectedTargetMonster.monsterData.icon;
            targetMonsterIcon.gameObject.SetActive(true);
        }

        // Update target monster name
        if (targetMonsterName != null)
        {
            targetMonsterName.text = selectedTargetMonster.GetDisplayName();
        }

        // Update target monster level
        if (targetMonsterLevel != null)
        {
            targetMonsterLevel.text = $"Level {selectedTargetMonster.currentLevel}";
        }

        // Update star display
        RefreshStarDisplay();

        // Also update preview/selected monster display if references exist
        UpdateSelectedMonsterPreview();

        // Update card selection visuals
        UpdateCardSelectionVisuals();
    }

    /// <summary>
    /// Update the selected monster preview (if separate UI elements exist)
    /// </summary>
    private void UpdateSelectedMonsterPreview()
    {
        if (selectedTargetMonster == null) return;

        // Update selected monster icon (if different from target)
        if (selectedMonsterIcon != null && selectedMonsterIcon != targetMonsterIcon)
        {
            if (selectedTargetMonster.monsterData?.icon != null)
            {
                selectedMonsterIcon.sprite = selectedTargetMonster.monsterData.icon;
                selectedMonsterIcon.gameObject.SetActive(true);
            }
        }

        // Update selected monster name (if different from target)
        if (selectedMonsterName != null && selectedMonsterName != targetMonsterName)
        {
            selectedMonsterName.text = selectedTargetMonster.GetDisplayName();
        }

        // Update selected monster level (if different from target)
        if (selectedMonsterLevel != null && selectedMonsterLevel != targetMonsterLevel)
        {
            selectedMonsterLevel.text = $"Level {selectedTargetMonster.currentLevel}";
        }

        // Update selected monster star display (if different container)
        if (selectedMonsterStarContainer != null && selectedMonsterStarContainer != targetStarContainer)
        {
            UpdateStarContainer(selectedMonsterStarContainer, selectedTargetMonster.currentStarLevel);
        }

        // Update stats display
        if (selectedMonsterStats != null)
        {
            UpdateSelectedMonsterStatsDisplay();
        }
    }

    /// <summary>
    /// Update star container with given star level
    /// </summary>
    private void UpdateStarContainer(Transform starContainer, int starLevel)
    {
        if (starContainer == null || starPrefab == null) return;

        // Clear existing stars
        foreach (Transform child in starContainer)
        {
            Destroy(child.gameObject);
        }

        // Create stars for current level
        for (int i = 0; i < starLevel; i++)
        {
            Instantiate(starPrefab, starContainer);
        }
    }

    /// <summary>
    /// Clear target monster display
    /// </summary>
    private void ClearTargetMonsterDisplay()
    {
        if (targetMonsterIcon != null)
            targetMonsterIcon.gameObject.SetActive(false);

        if (targetMonsterName != null)
            targetMonsterName.text = "No Monster Selected";

        if (targetMonsterLevel != null)
            targetMonsterLevel.text = "";

        if (selectedMonsterStats != null)
            selectedMonsterStats.text = "Select a monster to view details";
    }

    /// <summary>
    /// Update stats display for selected target monster
    /// ✨ UPDATED: Show simplified material requirements
    /// </summary>
    private void UpdateSelectedMonsterStatsDisplay()
    {
        if (selectedMonsterStats == null || selectedTargetMonster?.monsterData == null) return;

        // Calculate current stats
        MonsterStats currentStats = selectedTargetMonster.CalculateCurrentStats();

        // Create stats text
        string statsText = $"<b>Stats:</b>\n" +
                          $"HP: {currentStats.health}\n" +
                          $"ATK: {currentStats.attack}\n" +
                          $"DEF: {currentStats.defense}\n" +
                          $"SPD: {currentStats.speed}";

        // Add status info
        string statusInfo = "\n\n<b>Status:</b>";

        // Check if can be upgrade target
        if (MonsterUpgradeManager.Instance.CanUpgradeMonster(selectedTargetMonster))
        {
            statusInfo += "\n<color=#00FF00>✅ Ready for upgrade</color>";

            if (currentRequirement != null && currentRequirement.canUpgrade)
            {
                statusInfo += $"\n<color=#00DDFF>🔧 Needs {currentRequirement.requiredCount}x {selectedTargetMonster.currentStarLevel}⭐ materials</color>";
                statusInfo += $"\n<color=#00DDFF>📋 Selected: {selectedMaterials.Count}/{currentRequirement.requiredCount}</color>";
            }
        }
        else if (selectedTargetMonster.currentStarLevel >= 6)
        {
            statusInfo += "\n<color=#FFD700>⭐ Maximum star level</color>";
        }
        else if (selectedTargetMonster.currentLevel < MonsterUpgradeManager.Instance.GetMaxLevelForStar(selectedTargetMonster.currentStarLevel))
        {
            int maxLevel = MonsterUpgradeManager.Instance.GetMaxLevelForStar(selectedTargetMonster.currentStarLevel);
            statusInfo += $"\n<color=#FFA500>📈 Needs level {maxLevel} first</color>";
        }

        // Check if in battle team
        if (selectedTargetMonster.isInBattleTeam)
        {
            statusInfo += "\n<color=#FF6B6B>⚔️ In battle team</color>";
        }

        selectedMonsterStats.text = statsText + statusInfo;
    }

    /// <summary>
    /// Update visual selection of monster cards
    /// </summary>
    private void UpdateCardSelectionVisuals()
    {
        foreach (var card in monsterCards)
        {
            var cardMonster = card.GetMonster();

            if (cardMonster == selectedTargetMonster)
            {
                // This is the target monster
                card.SetSelected(true);
            }
            else if (selectedMaterials.Contains(cardMonster))
            {
                // This is a material monster
                card.SetSelected(true);
            }
            else
            {
                card.SetSelected(false);
            }
        }
    }

    /// <summary>
    /// Refresh star display for target monster
    /// </summary>
    private void RefreshStarDisplay()
    {
        if (targetStarContainer == null || starPrefab == null || selectedTargetMonster == null) return;

        UpdateStarContainer(targetStarContainer, selectedTargetMonster.currentStarLevel);
    }

    /// <summary>
    /// Refresh upgrade requirements
    /// ✨ UPDATED: Simplified requirements display
    /// </summary>
    private void RefreshRequirements()
    {
        if (selectedTargetMonster == null) return;

        currentRequirement = MonsterUpgradeManager.Instance.GetUpgradeRequirement(selectedTargetMonster.currentStarLevel);

        if (requirementText != null)
        {
            if (selectedTargetMonster.currentStarLevel >= 6)
            {
                requirementText.text = "Maximum star level reached!";
            }
            else if (selectedTargetMonster.currentLevel < MonsterUpgradeManager.Instance.GetMaxLevelForStar(selectedTargetMonster.currentStarLevel))
            {
                int maxLevel = MonsterUpgradeManager.Instance.GetMaxLevelForStar(selectedTargetMonster.currentStarLevel);
                requirementText.text = $"Target needs level {maxLevel} first!\n\nThen requires: {currentRequirement.requiredCount}x {selectedTargetMonster.currentStarLevel}⭐ monsters";
            }
            else
            {
                // ✅ SIMPLIFIED: Show simplified requirement text
                requirementText.text = $"Select {currentRequirement.requiredCount}x {selectedTargetMonster.currentStarLevel}⭐ monsters as materials\n\n<color=#888888>Click monsters in the list below to select as materials</color>";
            }
        }

        CreateMaterialSlots();
    }


    /// <summary>
    /// Create material slots based on requirements
    /// </summary>
    private void CreateMaterialSlots()
    {
        if (materialSlotsContainer == null || materialSlotPrefab == null) return;
        if (currentRequirement == null || !currentRequirement.canUpgrade) return;

        // Clear existing slots
        foreach (var slot in materialSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        materialSlots.Clear();

        // Create new slots
        for (int i = 0; i < currentRequirement.requiredCount; i++)
        {
            GameObject slotObj = Instantiate(materialSlotPrefab, materialSlotsContainer);
            MaterialSlotUI slotUI = slotObj.GetComponent<MaterialSlotUI>();

            if (slotUI != null)
            {
                slotUI.Initialize(this, i);
                materialSlots.Add(slotUI);
            }
        }
    }

    /// <summary>
    /// Refresh material slots with selected materials
    /// </summary>
    private void RefreshMaterialSlots()
    {
        for (int i = 0; i < materialSlots.Count; i++)
        {
            if (i < selectedMaterials.Count)
            {
                materialSlots[i].SetMaterial(selectedMaterials[i]);
            }
            else
            {
                materialSlots[i].ClearMaterial();
            }
        }
    }

    /// <summary>
    /// Refresh the full monster list with filtering and sorting
    /// </summary>
    private void RefreshMonsterList()
    {
        if (allMonstersContainer == null || monsterCardPrefab == null) return;

        // Clear existing cards
        ClearMonsterCards();

        // Get all monsters from collection
        var allMonsters = GetFilteredAndSortedMonsters();

        // Create cards for each monster
        foreach (var monster in allMonsters)
        {
            CreateMonsterCard(monster);
        }

        if (showDebugLogs) Debug.Log($"🃏 Created {monsterCards.Count} monster cards");
    }

    /// <summary>
    /// Get filtered and sorted monster list
    /// </summary>
    private List<CollectedMonster> GetFilteredAndSortedMonsters()
    {
        if (MonsterCollectionManager.Instance == null) return new List<CollectedMonster>();

        var allMonsters = MonsterCollectionManager.Instance.GetAllMonsters();
        var filteredMonsters = new List<CollectedMonster>();

        // Apply filters
        foreach (var monster in allMonsters)
        {
            if (!PassesFilters(monster)) continue;
            filteredMonsters.Add(monster);
        }

        // Apply sorting
        return SortMonsters(filteredMonsters);
    }

    /// <summary>
    /// Check if monster passes all current filters
    /// </summary>
    private bool PassesFilters(CollectedMonster monster)
    {
        if (monster?.monsterData == null) return false;

        // Mode filter
        switch (currentFilterMode)
        {
            case FilterMode.Upgradeable:
                if (!MonsterUpgradeManager.Instance.CanUpgradeMonster(monster))
                    return false;
                break;
            case FilterMode.Materials:
                // Only show monsters that can be used as materials for current target
                if (selectedTargetMonster == null) return false;
                if (monster == selectedTargetMonster) return false; // Can't use target as material
                if (monster.isInBattleTeam) return false;

                // ✅ SIMPLIFIED: Only check star level matching
                if (monster.currentStarLevel != selectedTargetMonster.currentStarLevel) return false;

                // ✅ REMOVED: Max level requirement
                break;

        }

        // Star filter
        if (starFilter != -1 && monster.currentStarLevel != starFilter)
            return false;

        // Element filter
        if (elementFilter.HasValue && monster.monsterData.element != elementFilter.Value)
            return false;

        // Search filter
        if (!string.IsNullOrEmpty(searchQuery))
        {
            string monsterName = monster.monsterData.monsterName.ToLower();
            if (!monsterName.Contains(searchQuery))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Sort monsters based on current sort settings
    /// </summary>
    private List<CollectedMonster> SortMonsters(List<CollectedMonster> monsters)
    {
        switch (currentSortMode)
        {
            case SortMode.Name:
                monsters = sortAscending
                    ? monsters.OrderBy(m => m.monsterData.monsterName).ToList()
                    : monsters.OrderByDescending(m => m.monsterData.monsterName).ToList();
                break;
            case SortMode.Level:
                monsters = sortAscending
                    ? monsters.OrderBy(m => m.currentLevel).ToList()
                    : monsters.OrderByDescending(m => m.currentLevel).ToList();
                break;
            case SortMode.StarLevel:
                monsters = sortAscending
                    ? monsters.OrderBy(m => m.currentStarLevel).ToList()
                    : monsters.OrderByDescending(m => m.currentStarLevel).ToList();
                break;
            case SortMode.PowerRating:
                monsters = sortAscending
                    ? monsters.OrderBy(m => m.GetPowerRating()).ToList()
                    : monsters.OrderByDescending(m => m.GetPowerRating()).ToList();
                break;
            case SortMode.Element:
                monsters = sortAscending
                    ? monsters.OrderBy(m => m.monsterData.element.ToString()).ToList()
                    : monsters.OrderByDescending(m => m.monsterData.element.ToString()).ToList();
                break;
        }

        return monsters;
    }

    /// <summary>
    /// Create a monster card for the given monster
    /// </summary>
    private void CreateMonsterCard(CollectedMonster monster)
    {
        GameObject cardObj = Instantiate(monsterCardPrefab, allMonstersContainer);
        UniversalMonsterCard card = cardObj.GetComponent<UniversalMonsterCard>();

        if (card != null)
        {
            // Setup card for upgrade panel usage
            card.Setup(monster, null); // No inventory UI needed

            // Add click listener for monster selection
            Button cardButton = card.GetComponent<Button>();
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(() => OnMonsterCardClicked(monster));
            }

            // Update card appearance based on state
            UpdateCardAppearance(card, monster);

            monsterCards.Add(card);
        }
    }

    /// <summary>
    /// Update card appearance based on monster state
    /// </summary>
    private void UpdateCardAppearance(UniversalMonsterCard card, CollectedMonster monster)
    {
        if (card == null || monster == null) return;

        // Determine monster role and state
        bool isTargetMonster = monster == selectedTargetMonster;
        bool isMaterialMonster = selectedMaterials.Contains(monster);
        bool canBeTarget = MonsterUpgradeManager.Instance.CanUpgradeMonster(monster);
        bool canBeMaterial = CanUseAsUpgradeMaterial(monster);
        bool isInBattleTeam = monster.isInBattleTeam;

        // Set selection state
        if (isTargetMonster || isMaterialMonster)
        {
            card.SetSelected(true);
        }
        else
        {
            card.SetSelected(false);
        }

        // Set interactability
        bool canUse = !isInBattleTeam && (canBeTarget || canBeMaterial || isTargetMonster || isMaterialMonster);

        Button cardButton = card.GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.interactable = canUse;

            // Optional: Add visual styling based on role
            Image cardImage = cardButton.GetComponent<Image>();
            if (cardImage != null)
            {
                if (isTargetMonster)
                {
                    // Target monster - golden border or special color
                    cardImage.color = Color.yellow;
                }
                else if (isMaterialMonster)
                {
                    // Material monster - blue border or special color
                    cardImage.color = Color.cyan;
                }
                else if (canBeTarget)
                {
                    // Can be target - normal color
                    cardImage.color = Color.white;
                }
                else if (canBeMaterial)
                {
                    // Can be material - light green
                    cardImage.color = Color.green;
                }
                else if (isInBattleTeam)
                {
                    // In battle team - red tint
                    cardImage.color = Color.red;
                }
                else
                {
                    // Cannot be used - gray
                    cardImage.color = Color.gray;
                }
            }
        }
    }

    /// <summary>
    /// Check if monster can be used (as target or material)
    /// </summary>
    private bool CanUseMonster(CollectedMonster monster)
    {
        if (monster == null || monster.isInBattleTeam) return false;

        // Can always be used as target if upgradeable
        if (MonsterUpgradeManager.Instance.CanUpgradeMonster(monster))
            return true;

        // Can be used as material if requirements met
        if (currentRequirement != null && currentRequirement.canUpgrade)
        {
            if (monster.currentStarLevel == currentRequirement.requiredStarLevel &&
                monster.currentLevel >= MonsterUpgradeManager.Instance.GetMaxLevelForStar(monster.currentStarLevel) &&
                monster != selectedTargetMonster &&
                selectedMaterials.Count < currentRequirement.requiredCount)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Handle monster card click - Always sets as target and updates preview
    /// </summary>
    /// <summary>
    /// Handle monster card click - Target selection OR Material selection
    /// </summary>
    private void OnMonsterCardClicked(CollectedMonster monster)
    {
        if (monster == null) return;

        if (showDebugLogs) Debug.Log($"🖱️ Monster card clicked: {monster.GetDisplayName()}");

        // Check if this monster is already selected as material
        if (selectedMaterials.Contains(monster))
        {
            // Remove from materials
            RemoveMaterial(monster);
            if (showDebugLogs) Debug.Log($"🔧 Removed {monster.GetDisplayName()} from materials");
            return;
        }

        // Check if this monster can be used as upgrade target
        if (MonsterUpgradeManager.Instance.CanUpgradeMonster(monster))
        {
            // If we don't have a target yet, or if clicking a different upgradeable monster, set as target
            if (selectedTargetMonster == null || selectedTargetMonster != monster)
            {
                SetTargetMonster(monster);
                if (showDebugLogs) Debug.Log($"🎯 Set {monster.GetDisplayName()} as target monster");
                return;
            }
        }

        // Check if this monster can be used as material for current target
        if (selectedTargetMonster != null && CanUseAsUpgradeMaterial(monster))
        {
            // Add as material if we have space
            if (currentRequirement != null && selectedMaterials.Count < currentRequirement.requiredCount)
            {
                AddMaterial(monster);
                if (showDebugLogs) Debug.Log($"🔧 Added {monster.GetDisplayName()} as upgrade material ({selectedMaterials.Count}/{currentRequirement.requiredCount})");
                return;
            }
            else
            {
                if (showDebugLogs) Debug.Log($"⚠️ Cannot add more materials - already at max ({currentRequirement?.requiredCount})");
            }
        }

        // If none of the above, try to set as target (fallback)
        if (MonsterUpgradeManager.Instance.CanUpgradeMonster(monster))
        {
            SetTargetMonster(monster);
            if (showDebugLogs) Debug.Log($"🎯 Set {monster.GetDisplayName()} as target monster (fallback)");
        }
        else
        {
            if (showDebugLogs) Debug.Log($"❌ {monster.GetDisplayName()} cannot be used as target or material");
        }
    }

    /// <summary>
    /// Check if monster can be used as upgrade material for current target
    /// ✨ SIMPLIFIED: Only requires same star level as target (no max level requirement)
    /// </summary>
    private bool CanUseAsUpgradeMaterial(CollectedMonster monster)
    {
        if (monster == null || selectedTargetMonster == null) return false;

        // Cannot use target monster as material
        if (monster == selectedTargetMonster) return false;

        // Cannot use monsters in battle team
        if (monster.isInBattleTeam) return false;

        // ✅ SIMPLIFIED: Must have same star level as target monster
        if (monster.currentStarLevel != selectedTargetMonster.currentStarLevel) return false;

        // ✅ REMOVED: Max level requirement - any level is OK now!

        return true;
    }



    /// <summary>
    /// Clear all monster cards
    /// </summary>
    private void ClearMonsterCards()
    {
        foreach (var card in monsterCards)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        monsterCards.Clear();
    }

    /// <summary>
    /// Refresh upgrade button state
    /// </summary>
    private void RefreshUpgradeButton()
    {
        bool canUpgrade = selectedTargetMonster != null &&
                         currentRequirement != null &&
                         currentRequirement.canUpgrade &&
                         selectedMaterials.Count == currentRequirement.requiredCount &&
                         MonsterUpgradeManager.Instance.CanUpgradeMonster(selectedTargetMonster);

        SetUpgradeButtonEnabled(canUpgrade);
    }

    /// <summary>
    /// Set upgrade button enabled state
    /// </summary>
    private void SetUpgradeButtonEnabled(bool enabled)
    {
        if (upgradeButton != null)
        {
            upgradeButton.interactable = enabled;

            if (upgradeButtonText != null)
            {
                upgradeButtonText.color = enabled ? Color.white : Color.gray;
            }
        }
    }

    /// <summary>
    /// Refresh preview panel
    /// </summary>
    private void RefreshPreview()
    {
        if (previewPanel == null || selectedTargetMonster == null) return;

        bool showPreview = currentRequirement != null && currentRequirement.canUpgrade;
        previewPanel.SetActive(showPreview);

        if (showPreview)
        {
            if (previewStarLevel != null)
                previewStarLevel.text = $"{selectedTargetMonster.currentStarLevel + 1}⭐";

            if (previewStats != null)
            {
                // Calculate preview stats
                var previewMonster = new CollectedMonster(selectedTargetMonster.monsterData);
                previewMonster.currentStarLevel = selectedTargetMonster.currentStarLevel + 1;
                previewMonster.currentLevel = 1;
                var previewMonsterStats = previewMonster.CalculateCurrentStats();

                previewStats.text = $"HP: {previewMonsterStats.health}\nATK: {previewMonsterStats.attack}\nDEF: {previewMonsterStats.defense}\nSPD: {previewMonsterStats.speed}";
            }
        }
    }

    /// <summary>
    /// Get all upgradeable monsters
    /// </summary>
    private List<CollectedMonster> GetUpgradeableMonsters()
    {
        if (MonsterCollectionManager.Instance == null) return new List<CollectedMonster>();

        var allMonsters = MonsterCollectionManager.Instance.GetAllMonsters();
        var upgradeableMonsters = new List<CollectedMonster>();

        foreach (var monster in allMonsters)
        {
            if (MonsterUpgradeManager.Instance.CanUpgradeMonster(monster))
            {
                upgradeableMonsters.Add(monster);
            }
        }

        return upgradeableMonsters;
    }

    /// <summary>
    /// Clear all selections
    /// </summary>
    private void ClearSelection()
    {
        selectedTargetMonster = null;
        selectedMaterials.Clear();
        currentRequirement = null;
    }

    /// <summary>
    /// Show upgrade success feedback
    /// </summary>
    private void ShowUpgradeSuccess()
    {
        Debug.Log("✅ Monster upgrade successful!");

        // Optional: Add particle effects, screen flash, etc.
        if (selectedTargetMonster != null)
        {
            Debug.Log($"🎉 {selectedTargetMonster.GetDisplayName()} upgraded to {selectedTargetMonster.currentStarLevel}⭐!");
        }

        // TODO: Add visual/audio feedback
        // - Particle effects
        // - Screen flash
        // - Success sound
        // - Animation
    }

    /// <summary>
    /// Show upgrade error feedback
    /// </summary>
    private void ShowUpgradeError()
    {
        Debug.Log("❌ Monster upgrade failed!");

        // TODO: Add visual/audio feedback
        // - Error sound
        // - Screen shake
        // - Red flash
        // - Error message popup
    }


    /// <summary>
    /// Animate panel show
    /// </summary>
    private void AnimateShow()
    {
        if (canvasGroup == null || panelTransform == null) return;

        // Disable interaction during animation
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 0f;
        panelTransform.localScale = Vector3.zero;

        canvasGroup.DOFade(1f, animationDuration);
        panelTransform.DOScale(Vector3.one, animationDuration)
            .SetEase(showEase)
            .OnComplete(() => {
                // Re-enable interaction after animation
                canvasGroup.interactable = true;
                if (showDebugLogs) Debug.Log("✅ MonsterUpgradePanel animation complete - interaction enabled");
            });
    }

    /// <summary>
    /// Animate panel hide
    /// </summary>
    private void AnimateHide(System.Action onComplete = null)
    {
        if (canvasGroup == null || panelTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Disable interaction during hide animation
        canvasGroup.interactable = false;

        canvasGroup.DOFade(0f, animationDuration);
        panelTransform.DOScale(Vector3.zero, animationDuration)
            .SetEase(hideEase)
            .OnComplete(() => {
                // Reset CanvasGroup state
                canvasGroup.interactable = true;
                canvasGroup.alpha = 1f;
                onComplete?.Invoke();
            });
    }

    // Emergency: Force close method for debugging
    [ContextMenu("Force Close Panel")]
    public void ForceClosePanel()
    {
        if (showDebugLogs) Debug.Log("🚨 Force closing MonsterUpgradePanel");

        gameObject.SetActive(false);
        ClearSelection();

        // Reset CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Reset transform
        if (panelTransform != null)
        {
            panelTransform.localScale = Vector3.one;
        }

        NotifyManagerOfClose();
    }
}
