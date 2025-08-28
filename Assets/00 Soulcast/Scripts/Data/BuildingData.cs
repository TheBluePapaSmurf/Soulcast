using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "Soulcast/Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Basic Info")]
    public string buildingName;
    [TextArea(3, 5)]
    public string description;
    public Sprite buildingIcon;

    [Header("Building Prefab")]
    [Tooltip("Het prefab dat geplaatst wordt op het grid")]
    public GameObject buildingPrefab;

    [Header("Grid Properties")]
    [Tooltip("Hoeveel grid cells dit building inneemt")]
    public Vector2Int gridSize = Vector2Int.one;

    [Header("Shop Properties")]
    public string subCategory = "Buildings";
    public int cost = 1000;
    public ShopCurrency currencyType = ShopCurrency.SoulCoins;
    public int requiredLevel = 1;

    [Header("Placement Rules")]
    public bool canPlaceOnWater = false;
    public bool canPlaceOnTerrain = true;
    public bool requiresFlat = true;

    [Header("Audio")]
    public AudioClip purchaseSound;
    public AudioClip placementSound;
    public AudioClip removalSound;

    public Vector2Int GetGridSize()
    {
        return gridSize;
    }

    public bool IsValidForGridPlacement()
    {
        return buildingPrefab != null && !string.IsNullOrEmpty(buildingName);
    }
}
