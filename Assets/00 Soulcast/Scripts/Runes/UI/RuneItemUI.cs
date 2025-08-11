using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RuneItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image runeIcon;
    public Image runeImage; // 🆕 NEW: Specific RuneImage component for rarity-based visuals
    public Image rarityBorder;
    public Image runeTypeIcon;
    public TextMeshProUGUI runeNameText;
    public TextMeshProUGUI runeLevelText;
    public TextMeshProUGUI mainStatText;
    public Button selectButton;

    [Header("Equipped Status")]
    public GameObject equippedOverlay;
    public TextMeshProUGUI equippedText;
    public Image equippedIcon;

    [Header("🎨 Rarity-based Rune Images")]
    [Tooltip("Rune images based on slot and rarity. [slotIndex][rarityIndex]")]
    public RuneImageSet[] runeImagesBySlot = new RuneImageSet[6];

    [Header("Visual Settings")]
    public Color[] rarityColors = new Color[5]; // Common to Legendary
    public Sprite[] runeTypeSprites = new Sprite[6];
    public Color equippedOverlayColor = new Color(0f, 0f, 0f, 0.7f);
    public Color unequippedColor = Color.white;
    public bool useRarityBasedImages = true; // 🆕 Toggle for new system
    public bool showDebugLogs = true; // 🔍 Debug logging

    private RuneData runeData;
    private bool isEquipped = false;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    void Awake()
    {
        if (showDebugLogs) Debug.Log("🎮 RuneItemUI Awake() called");

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 🔧 ENHANCED: Better button setup with error checking
        SetupSelectButton();
    }

    void SetupSelectButton()
    {
        if (selectButton == null)
        {
            if (showDebugLogs) Debug.LogWarning("⚠️ SelectButton is null! Trying to find Button component...");

            // Try to find button component on this GameObject
            selectButton = GetComponent<Button>();

            if (selectButton == null)
            {
                // Try to find button in children
                selectButton = GetComponentInChildren<Button>();
            }
        }

        if (selectButton != null)
        {
            // Remove existing listeners to avoid duplicates
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnRuneSelected);

            if (showDebugLogs) Debug.Log("✅ SelectButton listener added successfully");
        }
        else
        {
            Debug.LogError("❌ No Button component found! RuneItemUI will not be clickable.");
        }
    }

    public void Setup(RuneData rune, bool equipped = false)
    {
        if (showDebugLogs) Debug.Log($"🔧 Setting up RuneItemUI for: {(rune != null ? rune.runeName : "NULL")}");

        runeData = rune;
        isEquipped = equipped;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (runeData == null)
        {
            if (showDebugLogs) Debug.LogWarning("❌ RuneData is null in UpdateVisuals");
            return;
        }

        if (showDebugLogs) Debug.Log($"🎨 Updating visuals for rune: {runeData.runeName}");

        // 🆕 NEW: Update RuneImage with rarity-based sprite
        UpdateRuneImage();

        // 🔧 ENHANCED: Rarity border color (still used for additional visual feedback)
        if (rarityBorder != null && rarityColors.Length > (int)runeData.rarity)
        {
            rarityBorder.color = rarityColors[(int)runeData.rarity];
            if (showDebugLogs) Debug.Log($"   ✅ Set border color for rarity: {runeData.rarity}");
        }

        // Original rune icon (for compatibility)
        if (runeIcon != null)
        {
            runeIcon.sprite = runeData.runeSprite;
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

    // 🆕 NEW: Update the specific RuneImage component
    void UpdateRuneImage()
    {
        if (runeImage == null)
        {
            if (showDebugLogs) Debug.LogWarning("❌ RuneImage component is null! Please assign it in the inspector.");
            return;
        }

        if (!useRarityBasedImages)
        {
            // Use original rune icon if rarity system is disabled
            runeImage.sprite = runeData.runeSprite;
            if (showDebugLogs) Debug.Log("   📷 Using original rune icon (rarity system disabled)");
            return;
        }

        Sprite raritySprite = GetRarityBasedSprite(runeData);
        if (raritySprite != null)
        {
            runeImage.sprite = raritySprite;
            if (showDebugLogs) Debug.Log($"   ✅ Set RuneImage sprite: {raritySprite.name}");
        }
        else
        {
            // Fallback to original icon
            runeImage.sprite = runeData.runeSprite;
            if (showDebugLogs) Debug.LogWarning($"   ⚠️ No rarity sprite found, using fallback for {runeData.runeName}");
        }
    }

    // 🔧 ENHANCED: Get rarity-based sprite for the rune
    Sprite GetRarityBasedSprite(RuneData rune)
    {
        if (rune == null)
        {
            if (showDebugLogs) Debug.LogWarning("GetRarityBasedSprite: rune is null");
            return null;
        }

        // Get slot index from rune slot position
        int slotIndex = (int)rune.runeSlotPosition;

        // Validate slot index
        if (slotIndex < 0 || slotIndex >= runeImagesBySlot.Length)
        {
            Debug.LogWarning($"Invalid slot index {slotIndex} for rune {rune.runeName}. Valid range: 0-{runeImagesBySlot.Length - 1}");
            return null;
        }

        // Get rarity index
        int rarityIndex = (int)rune.rarity;

        // Validate rarity index
        if (rarityIndex < 0 || rarityIndex >= 5) // 0=Common, 1=Uncommon, 2=Rare, 3=Epic, 4=Legendary
        {
            Debug.LogWarning($"Invalid rarity index {rarityIndex} for rune {rune.runeName}. Valid range: 0-4");
            return null;
        }

        // Get the sprite from the configured set
        RuneImageSet imageSet = runeImagesBySlot[slotIndex];
        if (imageSet == null)
        {
            Debug.LogWarning($"No RuneImageSet configured for slot {slotIndex}");
            return null;
        }

        if (imageSet.raritySprites == null || imageSet.raritySprites.Length <= rarityIndex)
        {
            Debug.LogWarning($"No rarity sprites array or insufficient sprites for slot {slotIndex}, rarity {rarityIndex}");
            return null;
        }

        Sprite raritySprite = imageSet.raritySprites[rarityIndex];
        if (raritySprite == null)
        {
            Debug.LogWarning($"Sprite at slot {slotIndex}, rarity {rarityIndex} is null for rune {rune.runeName}");
            return null;
        }

        if (showDebugLogs)
        {
            Debug.Log($"   🎯 Found sprite for Slot {slotIndex + 1}, {rune.rarity}: {raritySprite.name}");
        }

        return raritySprite;
    }

    // Rest of existing methods remain the same...
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

    // 🔧 ENHANCED: OnRuneSelected with comprehensive error checking
    void OnRuneSelected()
    {
        Debug.Log($"🎯 OnRuneSelected() called for rune: {(runeData != null ? runeData.runeName : "NULL")}");

        if (runeData == null)
        {
            Debug.LogError("❌ Cannot show rune details: runeData is null!");
            return;
        }

        try
        {
            // Find Canvas first
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("❌ Canvas not found in scene!");
                return;
            }

            // Check if RuneDetailsPopup exists as static instance
            if (RuneDetailsPopup.Instance != null)
            {
                if (showDebugLogs) Debug.Log("✅ Using RuneDetailsPopup.Instance");
                RuneDetailsPopup.Instance.ShowRune(runeData, isEquipped, OnRuneChanged);
                return;
            }

            // Fallback: Find RuneDetailsPopup in scene
            Transform popupTransform = canvas.transform.Find("RuneDetailsPopup");
            if (popupTransform != null)
            {
                if (showDebugLogs) Debug.Log("✅ Found RuneDetailsPopup in scene");

                // Activate the popup if it's inactive
                if (!popupTransform.gameObject.activeSelf)
                {
                    popupTransform.gameObject.SetActive(true);
                    if (showDebugLogs) Debug.Log("🔼 Activated inactive RuneDetailsPopup");
                }

                // Get component and show rune
                RuneDetailsPopup popup = popupTransform.GetComponent<RuneDetailsPopup>();
                if (popup != null)
                {
                    popup.ShowRune(runeData, isEquipped, OnRuneChanged);
                    if (showDebugLogs) Debug.Log("✅ Successfully called ShowRune()");
                }
                else
                {
                    Debug.LogError("❌ RuneDetailsPopup component not found on GameObject!");
                }
            }
            else
            {
                Debug.LogError("❌ RuneDetailsPopup not found under Canvas!");

                // Debug: List all Canvas children
                if (showDebugLogs)
                {
                    Debug.Log("🔍 Canvas children:");
                    for (int i = 0; i < canvas.transform.childCount; i++)
                    {
                        Debug.Log($"   {i}: {canvas.transform.GetChild(i).name}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Exception in OnRuneSelected: {e.Message}\n{e.StackTrace}");
        }
    }

    // New callback method for when rune changes
    private void OnRuneChanged()
    {
        if (showDebugLogs) Debug.Log("🔄 OnRuneChanged callback triggered");

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

    // 🆕 NEW: Context menu methods for testing
    [ContextMenu("Test Update Visuals")]
    public void TestUpdateVisuals()
    {
        UpdateVisuals();
    }

    [ContextMenu("Test Update RuneImage Only")]
    public void TestUpdateRuneImage()
    {
        UpdateRuneImage();
    }

    [ContextMenu("Test OnRuneSelected")]
    public void TestOnRuneSelected()
    {
        OnRuneSelected();
    }

    [ContextMenu("Debug Component References")]
    public void DebugComponentReferences()
    {
        Debug.Log($"🔍 RuneItemUI Component Debug:");
        Debug.Log($"   runeData: {(runeData != null ? runeData.runeName : "NULL")}");
        Debug.Log($"   selectButton: {(selectButton != null ? "Found" : "NULL")}");
        Debug.Log($"   runeImage: {(runeImage != null ? "Found" : "NULL")}");
        Debug.Log($"   runeIcon: {(runeIcon != null ? "Found" : "NULL")}");
        Debug.Log($"   Canvas in scene: {(FindAnyObjectByType<Canvas>() != null ? "Found" : "NULL")}");
        Debug.Log($"   RuneDetailsPopup.Instance: {(RuneDetailsPopup.Instance != null ? "Found" : "NULL")}");
    }

    [ContextMenu("Debug Rune Image Info")]
    public void DebugRuneImageInfo()
    {
        if (runeData == null)
        {
            Debug.Log("❌ No rune data assigned");
            return;
        }

        Debug.Log($"🔍 Rune Image Debug for: {runeData.runeName}");
        Debug.Log($"   Slot Position: {runeData.runeSlotPosition} (Index: {(int)runeData.runeSlotPosition})");
        Debug.Log($"   Rarity: {runeData.rarity} (Index: {(int)runeData.rarity})");
        Debug.Log($"   Use Rarity Images: {useRarityBasedImages}");
        Debug.Log($"   RuneImage Component: {(runeImage != null ? "Found" : "NULL")}");

        Sprite foundSprite = GetRarityBasedSprite(runeData);
        Debug.Log($"   Found Sprite: {(foundSprite != null ? foundSprite.name : "NULL")}");

        // Debug configured sprites for this slot
        int slotIndex = (int)runeData.runeSlotPosition;
        if (slotIndex >= 0 && slotIndex < runeImagesBySlot.Length && runeImagesBySlot[slotIndex] != null)
        {
            Debug.Log($"   Configured sprites for slot {slotIndex}:");
            for (int i = 0; i < runeImagesBySlot[slotIndex].raritySprites.Length; i++)
            {
                var sprite = runeImagesBySlot[slotIndex].raritySprites[i];
                Debug.Log($"     Rarity {i}: {(sprite != null ? sprite.name : "NULL")}");
            }
        }
    }
}

// 🎨 Serializable class for organizing rune images by slot
[System.Serializable]
public class RuneImageSet
{
    [Header("Slot Configuration")]
    public string slotName = "Slot X";

    [Header("Rarity Sprites (5 total)")]
    [Tooltip("Order: Common, Uncommon, Rare, Epic, Legendary")]
    public Sprite[] raritySprites = new Sprite[5];

    public RuneImageSet()
    {
        // Default constructor
    }

    public RuneImageSet(string name)
    {
        slotName = name;
    }
}
