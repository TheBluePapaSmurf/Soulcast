using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UniversalMonsterCard : MonoBehaviour
{
    [Header("Card Elements")]
    public Image monsterIcon;
    public Image backgroundImage;
    public TextMeshProUGUI monsterNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI duplicateCountText;
    public GameObject duplicateIndicator;
    public Button cardButton;
    public StarDisplay starDisplay; // Star display component

    [Header("Selection Visual")]
    public Image selectionBorder;

    [Header("Role Display (optional)")]
    public Image roleIcon;
    public TextMeshProUGUI roleText;

    [Header("Stats Display (optional)")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI spdText;

    [Header("Default Colors (for non-inventory use)")]
    public Color defaultNormalColor = Color.white;
    public Color defaultSelectedColor = Color.cyan;
    public Color defaultDisabledColor = Color.gray;

    private CollectedMonster monster;
    private MonsterInventoryUI inventoryUI;
    private bool isSelected = false;
    private bool isInteractable = true;

    // Battle selection callbacks
    private Action onSelectCallback;
    private Action onDeselectCallback;
    private CardMode currentMode = CardMode.Inventory;

    public enum CardMode
    {
        Inventory,      // Normal inventory browsing
        BattleSelection, // Team selection for battle
        Display,        // Display only (gacha results, wave preview, etc.)
        WavePreview     // Enemy preview in wave
    }

    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    #region Setup Methods

    // Original inventory setup (unchanged for backward compatibility)
    public void Setup(CollectedMonster collectedMonster, MonsterInventoryUI inventoryController)
    {
        SetupInternal(collectedMonster, CardMode.Inventory, inventoryController);
    }

    // NEW: Battle selection setup
    public void SetupForBattleSelection(CollectedMonster collectedMonster, Action onSelect, Action onDeselect)
    {
        onSelectCallback = onSelect;
        onDeselectCallback = onDeselect;
        SetupInternal(collectedMonster, CardMode.BattleSelection);

        // Show additional info for battle selection
        UpdateRoleDisplay();
        UpdateStatsDisplay();
    }

    // NEW: Display only setup (gacha results, previews, etc.)
    public void SetupForDisplay(CollectedMonster collectedMonster, bool showStats = false)
    {
        SetupInternal(collectedMonster, CardMode.Display);

        // Hide interactive elements
        if (cardButton != null)
            cardButton.interactable = false;

        HideSelectionBorder();
        HideDuplicateInfo();

        if (showStats)
        {
            UpdateRoleDisplay();
            UpdateStatsDisplay();
        }
    }

    // NEW: Wave preview setup (for enemy cards)
    public void SetupForWavePreview(MonsterData monsterData, int level, int stars, int count = 1)
    {
        // Create temporary CollectedMonster for display
        var tempMonster = new CollectedMonster(monsterData);
        tempMonster.currentLevel = level;
        tempMonster.currentStarLevel = stars;

        SetupInternal(tempMonster, CardMode.WavePreview);

        // Show count if multiple enemies
        if (count > 1 && duplicateCountText != null)
        {
            duplicateCountText.text = $"x{count}";
            if (duplicateIndicator != null)
                duplicateIndicator.SetActive(true);
        }

        // Disable interaction
        if (cardButton != null)
            cardButton.interactable = false;

        HideSelectionBorder();
        UpdateThreatLevelColor(level, stars);
    }

    private void SetupInternal(CollectedMonster collectedMonster, CardMode mode, MonsterInventoryUI inventoryController = null)
    {
        monster = collectedMonster;
        inventoryUI = inventoryController;
        currentMode = mode;

        if (monster?.monsterData == null) return;

        // Set monster info
        if (monsterNameText != null)
        {
            monsterNameText.text = monster.monsterData.monsterName;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{monster.currentLevel}";
        }

        // Set monster icon
        if (monsterIcon != null && monster.monsterData.icon != null)
        {
            monsterIcon.sprite = monster.monsterData.icon;
        }

        // Set star display
        if (starDisplay != null)
        {
            starDisplay.SetStarLevel(monster.currentStarLevel);
        }

        // Set initial selection state
        SetSelected(false);
    }

    #endregion

    #region Display Updates

    private void UpdateRoleDisplay()
    {
        if (monster?.monsterData == null) return;

        if (roleText != null)
            roleText.text = monster.monsterData.role.ToString();

        if (roleIcon != null)
            roleIcon.color = MonsterRoleUtility.GetRoleColor(monster.monsterData.role);
    }

    private void UpdateStatsDisplay()
    {
        if (monster?.monsterData == null) return;

        var stats = monster.monsterData.GetRoleAdjustedStats(monster.currentLevel, monster.currentStarLevel);

        if (hpText != null)
            hpText.text = stats.health.ToString();

        if (atkText != null)
            atkText.text = stats.attack.ToString();

        if (defText != null)
            defText.text = stats.defense.ToString();

        if (spdText != null)
            spdText.text = stats.speed.ToString();
    }

    private void UpdateThreatLevelColor(int level, int stars)
    {
        if (backgroundImage == null) return;

        Color threatColor;
        if (stars >= 5 || level >= 50)
            threatColor = new Color(1f, 0.5f, 0.5f, 0.8f); // Light red for boss
        else if (stars >= 4 || level >= 30)
            threatColor = new Color(1f, 1f, 0.5f, 0.8f); // Light yellow for elite
        else
            threatColor = defaultNormalColor;

        backgroundImage.color = threatColor;
    }

    #endregion

    #region Selection & Interaction

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(selected);
        }

        // Change background color based on selection and mode
        if (backgroundImage != null)
        {
            Color targetColor = GetTargetColor(selected);
            backgroundImage.color = targetColor;
        }
    }

    private Color GetTargetColor(bool selected)
    {
        if (!isInteractable)
            return defaultDisabledColor;

        switch (currentMode)
        {
            case CardMode.Inventory:
                if (inventoryUI != null)
                    return selected ? inventoryUI.selectedColor : inventoryUI.normalColor;
                break;

            case CardMode.BattleSelection:
                return selected ? defaultSelectedColor : defaultNormalColor;

            case CardMode.Display:
            case CardMode.WavePreview:
                return defaultNormalColor;
        }

        return selected ? defaultSelectedColor : defaultNormalColor;
    }

    void OnCardClicked()
    {
        if (!isInteractable || monster == null) return;

        switch (currentMode)
        {
            case CardMode.Inventory:
                inventoryUI?.OnMonsterCardClicked(monster);
                break;

            case CardMode.BattleSelection:
                if (isSelected)
                    onDeselectCallback?.Invoke();
                else
                    onSelectCallback?.Invoke();
                break;

            case CardMode.Display:
            case CardMode.WavePreview:
                // No interaction for display modes
                break;
        }
    }

    #endregion

    #region Public Utility Methods

    public CollectedMonster GetMonster()
    {
        return monster;
    }

    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = color;
        }
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        if (cardButton != null)
        {
            cardButton.interactable = interactable;
        }

        // Update visual state
        SetSelected(isSelected);
    }

    public void HideSelectionBorder()
    {
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(false);
        }
    }

    public void HideDuplicateInfo()
    {
        if (duplicateIndicator != null)
        {
            duplicateIndicator.SetActive(false);
        }
    }

    public void ShowRoleInfo(bool show)
    {
        if (roleText != null)
            roleText.gameObject.SetActive(show);

        if (roleIcon != null)
            roleIcon.gameObject.SetActive(show);
    }

    public void ShowStatsInfo(bool show)
    {
        if (hpText != null) hpText.gameObject.SetActive(show);
        if (atkText != null) atkText.gameObject.SetActive(show);
        if (defText != null) defText.gameObject.SetActive(show);
        if (spdText != null) spdText.gameObject.SetActive(show);
    }

    // Context menu for testing
    [ContextMenu("Test Card Click")]
    private void TestCardClick()
    {
        if (Application.isPlaying)
            OnCardClicked();
    }

    #endregion
}
