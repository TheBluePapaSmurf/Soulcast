using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Rune Set", menuName = "Rune System/Rune Set")]
public class RuneSetData : ScriptableObject
{
    [Header("Set Information")]
    public string setName;
    [TextArea(2, 3)]
    public string setDescription;
    public Sprite setIcon;
    public Color setColor = Color.white;

    [Header("Set Bonuses")]
    public List<RuneSetBonus> setBonuses = new List<RuneSetBonus>();

    // Get active set bonus based on equipped runes count
    public List<RuneStat> GetActiveSetBonus(int equippedCount)
    {
        List<RuneStat> bonuses = new List<RuneStat>();

        foreach (var setBonus in setBonuses)
        {
            if (equippedCount >= setBonus.requiredPieces)
            {
                bonuses.AddRange(setBonus.bonusStats);
            }
        }

        return bonuses;
    }

    // Get set bonus description for UI
    public string GetSetBonusDescription(int equippedCount)
    {
        string description = "";

        foreach (var setBonus in setBonuses)
        {
            bool isActive = equippedCount >= setBonus.requiredPieces;
            string color = isActive ? "#00FF00" : "#808080"; // Green if active, gray if not

            description += $"<color={color}>({setBonus.requiredPieces}) {setBonus.description}</color>\n";
        }

        return description.TrimEnd('\n');
    }
}

[System.Serializable]
public class RuneSetBonus
{
    [Range(2, 6)]
    public int requiredPieces = 2;
    [TextArea(1, 2)]
    public string description;
    public List<RuneStat> bonusStats = new List<RuneStat>();
}
