using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryMonsterCard : MonoBehaviour
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

    [Header("Default Colors (for non-inventory use)")]
    public Color defaultNormalColor = Color.white;
    public Color defaultSelectedColor = Color.cyan;

    private CollectedMonster monster;
    private MonsterInventoryUI inventoryUI;
    private bool isSelected = false;

    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    public void Setup(CollectedMonster collectedMonster, MonsterInventoryUI inventoryController)
    {
        monster = collectedMonster;
        inventoryUI = inventoryController;

        if (monster?.monsterData == null) return;

        // Set monster info
        if (monsterNameText != null)
        {
            monsterNameText.text = monster.monsterData.monsterName;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{monster.level}";
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

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(selected);
        }

        // Change background color based on selection
        if (backgroundImage != null)
        {
            Color targetColor;

            // FIXED: Handle null inventoryUI (for gacha results)
            if (inventoryUI != null)
            {
                // Use inventory colors when available
                targetColor = selected ? inventoryUI.selectedColor : inventoryUI.normalColor;
            }
            else
            {
                // Use default colors when no inventory UI (e.g., gacha results)
                targetColor = selected ? defaultSelectedColor : defaultNormalColor;
            }

            backgroundImage.color = targetColor;
        }
    }

    void OnCardClicked()
    {
        // Only handle clicks if we have an inventory UI
        if (inventoryUI != null && monster != null)
        {
            inventoryUI.OnMonsterCardClicked(monster);
        }
    }

    public CollectedMonster GetMonster()
    {
        return monster;
    }

    // NEW: Methods for external styling (useful for gacha results)
    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = color;
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (cardButton != null)
        {
            cardButton.interactable = interactable;
        }
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
}
