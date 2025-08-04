using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RuneItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image runeIcon;
    public Image rarityBorder;
    public Image runeTypeIcon;
    public TextMeshProUGUI runeNameText;
    public TextMeshProUGUI runeLevelText;
    public TextMeshProUGUI mainStatText;
    public Button selectButton;

    [Header("Equipped Status")] // NEW SECTION
    public GameObject equippedOverlay;
    public TextMeshProUGUI equippedText;
    public Image equippedIcon;

    [Header("Visual Settings")]
    public Color[] rarityColors = new Color[5]; // Common to Legendary
    public Sprite[] runeTypeSprites = new Sprite[6];
    public Color equippedOverlayColor = new Color(0f, 0f, 0f, 0.7f);
    public Color unequippedColor = Color.white;

    private RuneData runeData;
    private bool isEquipped = false;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnRuneSelected);
        }
    }

    public void Setup(RuneData rune, bool equipped = false)
    {
        runeData = rune;
        isEquipped = equipped;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (runeData == null) return;

        // Rune icon
        if (runeIcon != null)
        {
            runeIcon.sprite = runeData.runeIcon;
        }

        // Rarity border color
        if (rarityBorder != null && rarityColors.Length > (int)runeData.rarity)
        {
            rarityBorder.color = rarityColors[(int)runeData.rarity];
        }

        // Rune type icon
        if (runeTypeIcon != null && runeTypeSprites.Length > (int)runeData.runeType)
        {
            runeTypeIcon.sprite = runeTypeSprites[(int)runeData.runeType];
        }

        // Rune name
        if (runeNameText != null)
        {
            runeNameText.text = runeData.runeName;
        }

        // Rune level
        if (runeLevelText != null)
        {
            runeLevelText.text = $"+{runeData.currentLevel}";
        }

        // Main stat
        if (mainStatText != null && runeData.mainStat != null)
        {
            mainStatText.text = runeData.mainStat.GetDisplayText();
        }

        // Equipped status
        UpdateEquippedStatus();
    }

    // REPLACE the UpdateEquippedStatus method in RuneItemUI.cs:
    void UpdateEquippedStatus()
    {
        // Show/hide equipped overlay
        if (equippedOverlay != null)
        {
            equippedOverlay.SetActive(isEquipped);
        }

        // Update equipped text with null check
        if (equippedText != null)
        {
            if (isEquipped)
            {
                // Get equipment info
                var runePanelUI = FindAnyObjectByType<RunePanelUI>();
                if (runePanelUI != null)
                {
                    var equipmentInfo = GetRuneEquipmentInfo();
                    if (equipmentInfo.monster != null)
                    {
                        equippedText.text = $"Equipped on\n{equipmentInfo.monster.monsterData.monsterName}";
                    }
                    else
                    {
                        equippedText.text = "Equipped";
                    }
                }
                else
                {
                    equippedText.text = "Equipped";
                }
            }
            else
            {
                // Clear text when not equipped
                equippedText.text = "";
            }
        }

        // Visual feedback for equipped status
        if (isEquipped)
        {
            // Darken the rune when equipped
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.7f;
            }
        }
        else
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        // Update drag capability
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = !isEquipped; // Can't drag equipped runes
        }
    }

    // REPLACE the GetRuneEquipmentInfo method in RuneItemUI.cs:
    private (CollectedMonster monster, int slotIndex) GetRuneEquipmentInfo()
    {
        if (PlayerInventory.Instance == null || runeData == null)
        {
            return (null, -1);
        }

        try
        {
            var allMonsters = PlayerInventory.Instance.GetAllMonsters();

            if (allMonsters == null)
            {
                return (null, -1);
            }

            foreach (var monster in allMonsters)
            {
                if (monster?.runeSlots == null) continue;

                for (int i = 0; i < monster.runeSlots.Length; i++)
                {
                    if (monster.runeSlots[i]?.equippedRune == runeData)
                    {
                        return (monster, i);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in GetRuneEquipmentInfo: {e.Message}");
        }

        return (null, -1);
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isEquipped)
        {
            // Don't allow dragging equipped runes directly from inventory
            // Player should unequip from the rune slots first
            return;
        }

        originalParent = transform.parent;
        originalPosition = transform.position;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Move to canvas level for proper rendering during drag
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            transform.SetParent(canvas.transform, true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isEquipped) return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isEquipped) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Return to original parent
        transform.SetParent(originalParent, true);
        transform.position = originalPosition;
    }

    void OnRuneSelected()
    {
        Debug.Log($"=== Rune Selected: {runeData.runeName} ===");

        // Find Canvas first
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas != null)
        {
            // Transform.Find CAN find inactive children
            Transform popupTransform = canvas.transform.Find("RuneDetailsPopup");

            if (popupTransform != null)
            {
                // Activate the popup
                popupTransform.gameObject.SetActive(true);

                // Get component and show rune
                RuneDetailsPopup popup = popupTransform.GetComponent<RuneDetailsPopup>();
                popup?.ShowRune(runeData, isEquipped, OnRuneChanged);

                Debug.Log("Inactive popup found and activated!");
            }
            else
            {
                Debug.LogError("RuneDetailsPopup not found under Canvas!");
            }
        }
        else
        {
            Debug.LogError("Canvas not found in scene!");
        }
    }



    // New callback method for when rune changes
    private void OnRuneChanged()
    {
        // Refresh the rune inventory to reflect changes
        var runePanelUI = FindAnyObjectByType<RunePanelUI>();
        if (runePanelUI != null)
        {
            runePanelUI.RefreshCurrentView();
        }
    }


    void ShowEquippedRuneOptions()
    {
        var equipmentInfo = GetRuneEquipmentInfo();
        if (equipmentInfo.monster != null)
        {
            Debug.Log($"Rune {runeData.runeName} is equipped on {equipmentInfo.monster.monsterData.monsterName} in slot {equipmentInfo.slotIndex + 1}");

            // Optional: Ask if player wants to unequip
            // This could open a confirmation dialog
        }
    }

    public RuneData GetRuneData()
    {
        return runeData;
    }

    public bool IsEquipped()
    {
        return isEquipped;
    }

    public void SetEquippedStatus(bool equipped)
    {
        isEquipped = equipped;
        UpdateEquippedStatus();
    }
}
