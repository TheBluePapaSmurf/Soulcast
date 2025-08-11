using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneSlotButton : MonoBehaviour
{
    [Header("UI References")]
    public Image runeSlotIcon;
    public Image runeSlotBackground; // 🔧 CHANGED: This will now use rarity sprites
    public Image rarityBorder; // 🆕 NEW: Optional additional rarity border
    public TextMeshProUGUI runeSlotName;
    public TextMeshProUGUI runeLevel;
    public Button button;

    [Header("Slot Configuration")]
    public int slotIndex;
    public RuneSlotPosition requiredSlotPosition;

    [Header("Visual States")]
    public Sprite emptySlotSprite;      // For background when empty
    // 🗑️ REMOVED: filledSlotSprite - now using rarity sprites instead

    [Header("🎨 Rarity-based Slot Background Sprites")]
    [Tooltip("Rune background sprites based on slot and rarity. [slotIndex][rarityIndex]")]
    public RuneImageSet[] runeImagesBySlot = new RuneImageSet[6];

    [Header("Visual Colors")]
    public Color emptySlotColor = Color.gray;
    public Color filledSlotColor = Color.white;
    public Color[] rarityColors = new Color[5]; // Common to Legendary (for optional border)
    public bool useRarityBasedBackgrounds = true; // 🆕 Toggle for new system
    public bool showDebugLogs = true; // 🔍 Debug logging

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

        if (showDebugLogs) Debug.Log($"🔧 Initializing RuneSlot {index} for monster: {monster.monsterData.monsterName}");

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

        if (showDebugLogs) Debug.Log($"🎨 Updating slot {slotIndex} visuals. Has rune: {hasRune}");

        // 🔧 ENHANCED: Update slot background with rarity-based sprite
        UpdateSlotBackgroundWithRarity(hasRune);

        // 🆕 NEW: Update optional rarity border
        UpdateRarityBorder(hasRune);

        // Update original rune icon (for rune symbol/icon overlay)
        UpdateRuneIcon(hasRune);

        // Update text elements
        UpdateSlotTexts(hasRune);
    }

    // 🔧 ENHANCED: Update slot background with rarity sprite instead of generic filled sprite
    private void UpdateSlotBackgroundWithRarity(bool hasRune)
    {
        if (runeSlotBackground == null) return;

        if (!hasRune)
        {
            // Use empty slot sprite when no rune equipped
            if (emptySlotSprite != null)
            {
                runeSlotBackground.sprite = emptySlotSprite;
                runeSlotBackground.color = emptySlotColor;
            }

            if (showDebugLogs) Debug.Log($"   🔽 Set empty background for slot {slotIndex}");
            return;
        }

        // Has rune - use rarity-based background
        if (useRarityBasedBackgrounds)
        {
            Sprite raritySprite = GetRarityBasedSprite(equippedRune);
            if (raritySprite != null)
            {
                runeSlotBackground.sprite = raritySprite;
                runeSlotBackground.color = filledSlotColor;
                if (showDebugLogs) Debug.Log($"   ✅ Set rarity background for slot {slotIndex}: {raritySprite.name}");
            }
            else
            {
                // Fallback to empty sprite if no rarity sprite found
                runeSlotBackground.sprite = emptySlotSprite;
                runeSlotBackground.color = filledSlotColor;
                if (showDebugLogs) Debug.LogWarning($"   ⚠️ No rarity sprite found for slot {slotIndex}, using empty sprite as fallback");
            }
        }
        else
        {
            // Fallback to empty sprite if rarity system disabled
            runeSlotBackground.sprite = emptySlotSprite;
            runeSlotBackground.color = filledSlotColor;
            if (showDebugLogs) Debug.Log($"   📷 Rarity system disabled, using empty sprite for slot {slotIndex}");
        }
    }

    // 🆕 NEW: Update optional rarity border for additional visual feedback
    private void UpdateRarityBorder(bool hasRune)
    {
        if (rarityBorder == null) return;

        if (!hasRune)
        {
            rarityBorder.gameObject.SetActive(false);
            return;
        }

        rarityBorder.gameObject.SetActive(true);

        // Set rarity border color
        if (rarityColors.Length > (int)equippedRune.rarity)
        {
            rarityBorder.color = rarityColors[(int)equippedRune.rarity];
            if (showDebugLogs) Debug.Log($"   ✅ Set rarity border color for slot {slotIndex}: {equippedRune.rarity}");
        }
        else
        {
            rarityBorder.color = Color.white; // Default color
        }
    }

    // 🔧 ENHANCED: Update rune icon for symbol/type overlay (optional)
    private void UpdateRuneIcon(bool hasRune)
    {
        if (runeSlotIcon != null)
        {
            if (hasRune && equippedRune.runeSprite != null)
            {
                runeSlotIcon.sprite = equippedRune.runeSprite;
                runeSlotIcon.color = Color.white; // Keep icon visible over rarity background
                runeSlotIcon.gameObject.SetActive(true);
                if (showDebugLogs) Debug.Log($"   ✅ Updated runeSlotIcon for slot {slotIndex}");
            }
            else
            {
                runeSlotIcon.gameObject.SetActive(false);
            }
        }
    }

    // 🔧 Enhanced: Update text elements
    private void UpdateSlotTexts(bool hasRune)
    {
        // Update slot name/type text
        if (runeSlotName != null)
        {
            if (hasRune)
            {
                runeSlotName.text = equippedRune.runeName;
            }
            else
            {
                string slotTypeName = GetSlotTypeName(slotIndex);
                runeSlotName.text = slotTypeName;
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

    // 🆕 NEW: Get rarity-based sprite for the equipped rune
    private Sprite GetRarityBasedSprite(RuneData rune)
    {
        if (rune == null)
        {
            if (showDebugLogs) Debug.LogWarning("GetRarityBasedSprite: rune is null");
            return null;
        }

        // Get slot index from rune slot position
        int runeSlotIndex = (int)rune.runeSlotPosition;

        // Validate slot index
        if (runeSlotIndex < 0 || runeSlotIndex >= runeImagesBySlot.Length)
        {
            Debug.LogWarning($"Invalid slot index {runeSlotIndex} for rune {rune.runeName}. Valid range: 0-{runeImagesBySlot.Length - 1}");
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
        RuneImageSet imageSet = runeImagesBySlot[runeSlotIndex];
        if (imageSet == null)
        {
            Debug.LogWarning($"No RuneImageSet configured for slot {runeSlotIndex}");
            return null;
        }

        if (imageSet.raritySprites == null || imageSet.raritySprites.Length <= rarityIndex)
        {
            Debug.LogWarning($"No rarity sprites array or insufficient sprites for slot {runeSlotIndex}, rarity {rarityIndex}");
            return null;
        }

        Sprite raritySprite = imageSet.raritySprites[rarityIndex];
        if (raritySprite == null)
        {
            Debug.LogWarning($"Sprite at slot {runeSlotIndex}, rarity {rarityIndex} is null for rune {rune.runeName}");
            return null;
        }

        if (showDebugLogs)
        {
            Debug.Log($"   🎯 Found rarity background sprite for Slot {runeSlotIndex + 1}, {rune.rarity}: {raritySprite.name}");
        }

        return raritySprite;
    }

    // 🆕 NEW: Get user-friendly slot type name
    private string GetSlotTypeName(int index)
    {
        string[] slotNames = { "Weapon", "Helmet", "Armor", "Boots", "Accessory 1", "Accessory 2" };

        if (index >= 0 && index < slotNames.Length)
        {
            return slotNames[index];
        }

        return $"Slot {index + 1}";
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

                // Refresh the monster stats display
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

    // 🆕 NEW: Context menu for testing
    [ContextMenu("Test Refresh Slot")]
    public void TestRefreshSlot()
    {
        RefreshSlot();
    }

    [ContextMenu("Debug Slot State")]
    public void DebugSlotState()
    {
        Debug.Log($"=== RuneSlot {slotIndex} Debug ===");
        Debug.Log($"   RequiredSlotPosition: {requiredSlotPosition}");
        Debug.Log($"   TargetMonster: {(targetMonster != null ? targetMonster.monsterData.monsterName : "NULL")}");
        Debug.Log($"   EquippedRune: {(equippedRune != null ? equippedRune.runeName : "NONE")}");
        Debug.Log($"   RunePanelUI: {(runePanelUI != null ? "Found" : "NULL")}");
        Debug.Log($"   UseRarityBasedBackgrounds: {useRarityBasedBackgrounds}");
        Debug.Log($"   RuneImagesBySlot Count: {runeImagesBySlot.Length}");

        for (int i = 0; i < runeImagesBySlot.Length; i++)
        {
            var imageSet = runeImagesBySlot[i];
            if (imageSet != null)
            {
                Debug.Log($"     Slot {i}: {imageSet.slotName} ({imageSet.raritySprites.Length} sprites)");
            }
            else
            {
                Debug.Log($"     Slot {i}: NULL");
            }
        }
        Debug.Log("=== End Debug ===");
    }
}
