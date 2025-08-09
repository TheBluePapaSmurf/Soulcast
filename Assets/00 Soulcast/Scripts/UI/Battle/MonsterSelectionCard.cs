using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MonsterSelectionCard : MonoBehaviour
{
    [Header("Card Elements")]
    [SerializeField] private Image monsterImage;
    [SerializeField] private TextMeshProUGUI monsterNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI starText;
    [SerializeField] private Button selectButton;

    [Header("Selection Visual")]
    [SerializeField] private GameObject selectedOverlay;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color disabledColor = Color.gray;

    [Header("Monster Info Display")]
    [SerializeField] private Image elementIcon;
    [SerializeField] private Image roleIcon;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private GameObject[] starImages; // Array of star GameObjects

    [Header("Stats Preview")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI spdText;

    private CollectedMonster collectedMonster;
    private Action onSelect;
    private Action onDeselect;
    private bool isSelected = false;
    private bool isInteractable = true;

    public CollectedMonster MonsterData => collectedMonster;

    private void Awake()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();

        selectButton?.onClick.AddListener(OnCardClicked);
    }

    public void Setup(CollectedMonster monster, Action onSelectCallback, Action onDeselectCallback)
    {
        collectedMonster = monster;
        onSelect = onSelectCallback;
        onDeselect = onDeselectCallback;

        if (monster?.monsterData == null)
        {
            Debug.LogWarning("MonsterSelectionCard: Invalid monster data");
            return;
        }

        UpdateCardDisplay();
        UpdateSelectionState(false);
    }

    private void UpdateCardDisplay()
    {
        var monsterData = collectedMonster.monsterData;

        // Basic info
        if (monsterNameText != null)
            monsterNameText.text = monsterData.monsterName;

        if (levelText != null)
            levelText.text = $"Lv.{collectedMonster.level}";

        if (starText != null)
            starText.text = $"{collectedMonster.currentStarLevel}★";

        // Monster icon
        if (monsterImage != null && monsterData.icon != null)
            monsterImage.sprite = monsterData.icon;

        // Role info
        if (roleText != null)
            roleText.text = monsterData.role.ToString();

        if (roleIcon != null)
            roleIcon.color = MonsterRoleUtility.GetRoleColor(monsterData.role);

        // Star display
        UpdateStarDisplay();

        // Stats preview
        UpdateStatsDisplay();

        // Element icon (if you have element icons)
        UpdateElementDisplay();
    }

    private void UpdateStarDisplay()
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].SetActive(i < collectedMonster.currentStarLevel);
            }
        }
    }

    private void UpdateStatsDisplay()
    {
        // Get role-adjusted stats for display
        var stats = collectedMonster.monsterData.GetRoleAdjustedStats(collectedMonster.level, collectedMonster.currentStarLevel);

        if (hpText != null)
            hpText.text = stats.health.ToString();

        if (atkText != null)
            atkText.text = stats.attack.ToString();

        if (defText != null)
            defText.text = stats.defense.ToString();

        if (spdText != null)
            spdText.text = stats.speed.ToString();
    }

    private void UpdateElementDisplay()
    {
        // If you have element icons, update them here
        if (elementIcon != null)
        {
            // You can add element-specific colors or icons
            // elementIcon.sprite = GetElementSprite(collectedMonster.monsterData.element);
        }
    }

    public void UpdateSelectionState(bool selected)
    {
        isSelected = selected;

        // Update visual state
        if (selectedOverlay != null)
            selectedOverlay.SetActive(selected);

        if (cardBackground != null)
        {
            cardBackground.color = selected ? selectedColor : (isInteractable ? normalColor : disabledColor);
        }

        // Update button interactability
        if (selectButton != null)
            selectButton.interactable = isInteractable;
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        UpdateSelectionState(isSelected);
    }

    private void OnCardClicked()
    {
        if (!isInteractable) return;

        if (isSelected)
        {
            onDeselect?.Invoke();
        }
        else
        {
            onSelect?.Invoke();
        }
    }

    // Context menu for testing
    [ContextMenu("Test Card Click")]
    private void TestCardClick()
    {
        if (Application.isPlaying)
            OnCardClicked();
    }
}
