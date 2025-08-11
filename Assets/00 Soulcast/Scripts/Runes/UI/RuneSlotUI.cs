// RuneSlotUI.cs - Attach to each rune slot
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RuneSlotUI : MonoBehaviour, IDropHandler
{
    [Header("UI References")]
    public Image slotBackground;
    public Image runeIcon;
    public Image slotTypeIcon;
    public TextMeshProUGUI runeLevel;
    public Button unequipButton;

    [Header("Slot Configuration")]
    public RuneType requiredRuneType;
    public int slotIndex;

    [Header("Visual Settings")]
    public Color emptySlotColor = Color.gray;
    public Color occupiedSlotColor = Color.white;
    public Sprite[] runeTypeSprites = new Sprite[6]; // 0=Square, 1=Triangle, etc.

    private RuneData equippedRune;
    private CollectedMonster targetMonster;
    private RunePanelUI runePanelUI;

    void Start()
    {
        SetupSlotVisuals();
        SetupButtons();
    }

    void SetupSlotVisuals()
    {
        // Set slot type icon
        if (slotTypeIcon != null && runeTypeSprites.Length > (int)requiredRuneType)
        {
            slotTypeIcon.sprite = runeTypeSprites[(int)requiredRuneType];
        }

        UpdateVisuals();
    }

    void SetupButtons()
    {
        if (unequipButton != null)
        {
            unequipButton.onClick.AddListener(UnequipRune);
            unequipButton.gameObject.SetActive(false);
        }
    }

    public void Initialize(RuneType runeType, int index, RunePanelUI panelUI)
    {
        requiredRuneType = runeType;
        slotIndex = index;
        runePanelUI = panelUI;
        SetupSlotVisuals();
    }

    public void SetTargetMonster(CollectedMonster monster)
    {
        targetMonster = monster;
        RefreshSlot();
    }

    public void RefreshSlot()
    {
        if (targetMonster != null && slotIndex >= 0 && slotIndex < targetMonster.runeSlots.Length)
        {
            equippedRune = targetMonster.runeSlots[slotIndex].equippedRune;
        }
        else
        {
            equippedRune = null;
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        bool hasRune = equippedRune != null;

        // Update background color
        if (slotBackground != null)
        {
            slotBackground.color = hasRune ? occupiedSlotColor : emptySlotColor;
        }

        // Update rune icon
        if (runeIcon != null)
        {
            runeIcon.gameObject.SetActive(hasRune);
            if (hasRune)
            {
                runeIcon.sprite = equippedRune.runeSprite;
            }
        }

        // Update level text
        if (runeLevel != null)
        {
            runeLevel.gameObject.SetActive(hasRune);
            if (hasRune)
            {
                runeLevel.text = $"+{equippedRune.currentLevel}";
            }
        }

        // Update unequip button
        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(hasRune);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        RuneItemUI runeItem = eventData.pointerDrag?.GetComponent<RuneItemUI>();
        if (runeItem != null && runeItem.GetRuneData() != null)
        {
            TryEquipRune(runeItem.GetRuneData());
        }
    }

    public bool CanEquipRune(RuneData rune)
    {
        if (rune == null) return false;

        // Check rune type
        if (rune.runeType != requiredRuneType) return false;

        // Check slot position
        RuneSlotPosition targetPosition = (RuneSlotPosition)slotIndex;
        if (rune.runeSlotPosition != targetPosition) return false;

        return true;
    }


    public bool TryEquipRune(RuneData rune)
    {
        // Basic null checks
        if (targetMonster == null || rune == null)
        {
            Debug.LogWarning("Cannot equip rune: Missing target monster or rune data!");
            return false;
        }

        // Check rune type compatibility
        if (rune.runeType != requiredRuneType)
        {
            Debug.LogWarning($"Cannot equip {rune.runeType} rune to {requiredRuneType} slot!");
            return false;
        }

        // Check slot position compatibility
        RuneSlotPosition targetSlotPosition = (RuneSlotPosition)slotIndex;
        if (rune.runeSlotPosition != targetSlotPosition)
        {
            Debug.LogWarning($"Cannot equip {rune.runeName}! This rune belongs in {rune.runeSlotPosition}, not in {targetSlotPosition}!");
            return false;
        }

        // Attempt to equip the rune
        bool success = PlayerInventory.Instance.EquipRuneToMonster(targetMonster.uniqueID, slotIndex, rune);

        if (success)
        {
            RefreshSlot();
            Debug.Log($"Successfully equipped {rune.runeName} to slot {slotIndex}");
        }
        else
        {
            Debug.LogError($"Failed to equip {rune.runeName} to slot {slotIndex}");
        }

        return success;
    }

    // Helper method to check if rune is equipped elsewhere
    private bool IsRuneEquippedElsewhere(RuneData rune)
    {
        if (targetMonster == null) return false;

        for (int i = 0; i < targetMonster.runeSlots.Length; i++)
        {
            if (i != slotIndex && targetMonster.runeSlots[i].equippedRune == rune)
            {
                return true;
            }
        }
        return false;
    }


    public void UnequipRune()
    {
        if (targetMonster == null || equippedRune == null) return;

        RuneData unequippedRune = PlayerInventory.Instance.UnequipRuneFromMonster(targetMonster.uniqueID, slotIndex);
        if (unequippedRune != null)
        {
            RefreshSlot();
            Debug.Log($"Unequipped {unequippedRune.runeName} from slot {slotIndex}");
        }
    }

    public RuneData GetEquippedRune()
    {
        return equippedRune;
    }
}
