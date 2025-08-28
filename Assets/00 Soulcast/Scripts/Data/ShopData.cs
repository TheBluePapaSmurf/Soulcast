// Update: Assets/00 Soulcast/Scripts/Data/ShopData.cs

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopItem
{
    [Header("Basic Info")]
    public string itemId;
    public string itemName;
    public string description;
    public Sprite itemIcon;

    [Header("Pricing")]
    public ShopCurrency currencyType;
    public int price;
    public bool isLimitedQuantity;
    public int maxQuantity = 1;
    public int currentStock = 1;

    [Header("Category")]
    public ShopCategory category;

    [Header("Filtering")]
    public string subFilter = "All"; // e.g., "Buildings", "Decoration", "Tree", "Plants"

    [Header("Item Data")]
    public ShopItemType itemType;

    // ✅ FIXED: Separate fields for different data types
    [Header("Direct Item References")]
    [Tooltip("Direct reference to BuildingData (for Building items)")]
    public BuildingData buildingData;

    [Tooltip("Direct reference to MonsterData ScriptableObject (for Monster items)")]
    public MonsterData monsterData;

    [Tooltip("Direct reference to RuneData class (for Rune items) - Note: This is serialized data, not a ScriptableObject")]
    public RuneData runeData;

    // Keep the old itemDataId for backward compatibility (optional)
    [Header("Legacy Support")]
    [Tooltip("Legacy string ID - use direct references above instead")]
    public string itemDataId;

    [Header("Availability")]
    public bool isAvailable = true;
    public bool requiresUnlock;
    public string unlockCondition;

    /// <summary>
    /// Get the appropriate data object - returns object type since they're different base types
    /// </summary>
    public object GetItemData()
    {
        switch (itemType)
        {
            case ShopItemType.Building:
                return buildingData;
            case ShopItemType.Monster:
                return monsterData;
            case ShopItemType.Rune:
                return runeData;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get BuildingData specifically (type-safe)
    /// </summary>
    public BuildingData GetBuildingData()
    {
        return itemType == ShopItemType.Building ? buildingData : null;
    }

    /// <summary>
    /// Get MonsterData specifically (type-safe)
    /// </summary>
    public MonsterData GetMonsterData()
    {
        return itemType == ShopItemType.Monster ? monsterData : null;
    }

    /// <summary>
    /// Get RuneData specifically (type-safe)
    /// </summary>
    public RuneData GetRuneData()
    {
        return itemType == ShopItemType.Rune ? runeData : null;
    }

    /// <summary>
    /// Check if item has proper data assigned
    /// </summary>
    public bool HasValidData()
    {
        switch (itemType)
        {
            case ShopItemType.Building:
                return buildingData != null;
            case ShopItemType.Monster:
                return monsterData != null;
            case ShopItemType.Rune:
                return runeData != null && !string.IsNullOrEmpty(runeData.runeName);
            case ShopItemType.Resource:
            case ShopItemType.Upgrade:
            case ShopItemType.Special:
                return !string.IsNullOrEmpty(itemDataId); // Fall back to legacy for these types
            default:
                return !string.IsNullOrEmpty(itemDataId); // Fall back to legacy
        }
    }

    /// <summary>
    /// Get display name for this item
    /// </summary>
    public string GetDisplayName()
    {
        switch (itemType)
        {
            case ShopItemType.Building:
                return buildingData != null ? buildingData.buildingName : itemName;
            case ShopItemType.Monster:
                return monsterData != null ? monsterData.monsterName : itemName;
            case ShopItemType.Rune:
                return runeData != null ? runeData.GetDisplayName() : itemName;
            default:
                return itemName;
        }
    }

    public bool CanPurchase()
    {
        return isAvailable && currentStock > 0 && HasValidData();
    }

    public void Purchase()
    {
        if (isLimitedQuantity && currentStock > 0)
        {
            currentStock--;
        }
    }
}

[System.Serializable]
public enum ShopCategory
{
    Normal,
    Buildings,
    Honor,
    Efficient
}

[System.Serializable]
public enum ShopCurrency
{
    SoulCoins,
    PremiumCrystals,
    HonorPoints,
    SpecialCurrency
}

[System.Serializable]
public enum ShopItemType
{
    Monster,
    Building,
    Rune,
    Upgrade,
    Resource,
    Special
}

[CreateAssetMenu(fileName = "New Shop Data", menuName = "Soulcast/Shop/Shop Data")]
public class ShopData : ScriptableObject
{
    [Header("Shop Configuration")]
    public string shopName = "Mystical Shop";
    public string shopDescription = "Purchase powerful items and upgrades";

    [Header("Shop Items")]
    public List<ShopItem> normalItems = new List<ShopItem>();
    public List<ShopItem> buildingItems = new List<ShopItem>();
    public List<ShopItem> honorItems = new List<ShopItem>();
    public List<ShopItem> efficientItems = new List<ShopItem>();

    public List<ShopItem> GetItemsByCategory(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Normal: return normalItems;
            case ShopCategory.Buildings: return buildingItems;
            case ShopCategory.Honor: return honorItems;
            case ShopCategory.Efficient: return efficientItems;
            default: return new List<ShopItem>();
        }
    }

    public List<ShopItem> GetAllItems()
    {
        List<ShopItem> allItems = new List<ShopItem>();
        allItems.AddRange(normalItems);
        allItems.AddRange(buildingItems);
        allItems.AddRange(honorItems);
        allItems.AddRange(efficientItems);
        return allItems;
    }

    /// <summary>
    /// Validate all shop items have proper data references
    /// </summary>
    [ContextMenu("Validate Shop Items")]
    public void ValidateShopItems()
    {
        ValidateItemList("Normal Items", normalItems);
        ValidateItemList("Building Items", buildingItems);
        ValidateItemList("Honor Items", honorItems);
        ValidateItemList("Efficient Items", efficientItems);
    }

    void ValidateItemList(string listName, List<ShopItem> items)
    {
        Debug.Log($"🔍 Validating {listName}:");
        foreach (var item in items)
        {
            if (item.HasValidData())
            {
                Debug.Log($"   ✅ {item.GetDisplayName()} - Valid");
            }
            else
            {
                Debug.LogWarning($"   ❌ {item.itemName} - Missing data reference for type {item.itemType}");

                // Show specific guidance based on item type
                switch (item.itemType)
                {
                    case ShopItemType.Building:
                        Debug.LogWarning($"      💡 Assign a BuildingData asset to the 'buildingData' field");
                        break;
                    case ShopItemType.Monster:
                        Debug.LogWarning($"      💡 Assign a MonsterData asset to the 'monsterData' field");
                        break;
                    case ShopItemType.Rune:
                        Debug.LogWarning($"      💡 Configure the 'runeData' field with RuneData");
                        break;
                    default:
                        Debug.LogWarning($"      💡 Set a valid 'itemDataId' or implement specific data handling");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Quick setup helper for building items
    /// </summary>
    [ContextMenu("Auto Setup Building Items")]
    public void AutoSetupBuildingItems()
    {
        foreach (var item in buildingItems)
        {
            if (item.itemType != ShopItemType.Building)
            {
                item.itemType = ShopItemType.Building;
                Debug.Log($"🔧 Set item type to Building for: {item.itemName}");
            }

            if (item.category != ShopCategory.Buildings)
            {
                item.category = ShopCategory.Buildings;
                Debug.Log($"🔧 Set category to Buildings for: {item.itemName}");
            }
        }

        Debug.Log("✅ Building items auto-setup complete!");
    }
}
