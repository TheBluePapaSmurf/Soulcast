using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneSlotButton : MonoBehaviour
{
    [Header("UI References")]
    public Image runeSlotIcon;
    public Image runeSlotBackground;
    public TextMeshProUGUI runeSlotName;
    public TextMeshProUGUI runeLevel;
    public Button button;

    [Header("Slot Configuration")]
    public int slotIndex;
    public RuneSlotPosition requiredSlotPosition;

    [Header("Visual States")]
    public Sprite emptySlotSprite;      // For background when empty
    public Sprite filledSlotSprite;     // For background when filled

    [Header("Visual Colors")]
    public Color emptySlotColor = Color.gray;
    public Color filledSlotColor = Color.white;

    private RuneData equippedRune;
    private CollectedMonster targetMonster;
    private RunePanelUI runePanelUI;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnSlotClicked);

        requiredSlotPosition = (RuneSlotPosition)slotIndex;
        RefreshSlot();
    }

    public void Initialize(int index, CollectedMonster monster, RunePanelUI panelUI)
    {
        slotIndex = index;
        targetMonster = monster;
        runePanelUI = panelUI;
        requiredSlotPosition = (RuneSlotPosition)index;

        RefreshSlot();
    }

    public void RefreshSlot()
    {
        equippedRune = GetEquippedRune();
        UpdateSlotVisuals();
    }

    private RuneData GetEquippedRune()
    {
        if (targetMonster == null || slotIndex >= targetMonster.runeSlots.Length)
            return null;

        return targetMonster.runeSlots[slotIndex].equippedRune;
    }

    private void UpdateSlotVisuals()
    {
        bool hasRune = equippedRune != null;

        // Update slot background with empty/filled sprites
        if (runeSlotBackground != null)
        {
            if (hasRune && filledSlotSprite != null)
            {
                runeSlotBackground.sprite = filledSlotSprite;
            }
            else if (emptySlotSprite != null)
            {
                runeSlotBackground.sprite = emptySlotSprite;
            }
        }

        // Update rune icon (only shows actual rune sprites)
        if (runeSlotIcon != null)
        {
            if (hasRune && equippedRune.runeIcon != null)
            {
                runeSlotIcon.sprite = equippedRune.runeIcon;
                runeSlotIcon.color = filledSlotColor;
                runeSlotIcon.gameObject.SetActive(true);
            }
            else
            {
                runeSlotIcon.gameObject.SetActive(false);
            }
        }

        // Update slot name/type text
        if (runeSlotName != null)
        {
            if (hasRune)
            {
                runeSlotName.text = equippedRune.runeName;
            }
            else
            {
                runeSlotName.text = $"Slot {slotIndex + 1}";
            }
        }

        // Update level text
        if (runeLevel != null)
        {
            if (hasRune)
            {
                runeLevel.text = $"+{equippedRune.currentLevel}";
                runeLevel.gameObject.SetActive(true);
            }
            else
            {
                runeLevel.gameObject.SetActive(false);
            }
        }
    }

    private void OnSlotClicked()
    {
        if (runePanelUI != null)
        {
            if (equippedRune != null)
            {
                ShowRuneOptions();
            }
            else
            {
                ShowCompatibleRunes();
            }
        }
    }

    // REPLACE the ShowRuneOptions method in RuneSlotButton.cs:
    private void ShowRuneOptions()
    {
        Debug.Log($"Showing options for equipped rune: {equippedRune.runeName}");

        // Open RuneDetailsPopup for the equipped rune
        if (RuneDetailsPopup.Instance != null)
        {
            bool isEquipped = CheckIfRuneIsEquipped(equippedRune);

            RuneDetailsPopup.Instance.ShowRune(equippedRune, isEquipped, () => {
                // Callback to refresh this slot when rune state changes
                RefreshSlot();

                // Also refresh the RunePanelUI rune list
                if (runePanelUI != null)
                {
                    runePanelUI.RefreshCurrentView();
                }

                // NEW: Refresh the monster stats display
                MonsterInventoryUI inventoryUI = FindAnyObjectByType<MonsterInventoryUI>();
                if (inventoryUI != null)
                {
                    inventoryUI.RefreshCurrentMonsterStats();
                }
            });
        }
        else
        {
            Debug.LogError("RuneDetailsPopup.Instance is null!");
        }
    }

    // ADD this helper method to RuneSlotButton.cs:
    private bool CheckIfRuneIsEquipped(RuneData rune)
    {
        if (targetMonster == null || rune == null) return false;

        // Check if this rune is equipped in any slot of the current monster
        foreach (var slot in targetMonster.runeSlots)
        {
            if (slot.equippedRune == rune)
            {
                return true;
            }
        }
        return false;
    }

    private void ShowCompatibleRunes()
    {
        Debug.Log($"Showing compatible runes for slot {slotIndex}");
        runePanelUI.FilterRunesBySlotPosition(requiredSlotPosition);
    }

    public bool CanEquipRune(RuneData rune)
    {
        if (rune == null) return false;
        return rune.runeSlotPosition == requiredSlotPosition;
    }

    public bool TryEquipRune(RuneData rune)
    {
        if (!CanEquipRune(rune))
        {
            Debug.LogWarning($"Cannot equip {rune.runeName} to slot {slotIndex}! Wrong slot position.");
            return false;
        }

        bool success = PlayerInventory.Instance.EquipRuneToMonster(
            targetMonster.uniqueID, slotIndex, rune);

        if (success)
        {
            RefreshSlot();
            Debug.Log($"Equipped {rune.runeName} to slot {slotIndex}");
        }

        return success;
    }
}
