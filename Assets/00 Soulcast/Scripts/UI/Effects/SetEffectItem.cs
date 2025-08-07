using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SetEffectItem : MonoBehaviour
{
    [Header("UI References")]
    public Image setIcon;
    public TextMeshProUGUI setNameText;
    public TextMeshProUGUI setEffectText;
    // 🗑️ REMOVED: pieceCountText - not needed anymore

    [Header("Visual Settings")]
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;
    public bool showDebugLogs = false;

    // 🔧 SIMPLIFIED: Only setup active sets (isActive parameter removed since we only show active)
    public void Setup(RuneSetData setData, int requiredPieces)
    {
        if (setData == null)
        {
            Debug.LogWarning("⚠️ SetData is null in SetEffectItem.Setup!");
            return;
        }

        UpdateSetIcon(setData);
        UpdateSetName(setData);
        UpdateSetEffect(setData, requiredPieces);

        if (showDebugLogs)
        {
            Debug.Log($"🔧 SetEffectItem: {setData.setName} {requiredPieces}-piece set effect");
        }
    }

    private void UpdateSetIcon(RuneSetData setData)
    {
        if (setIcon != null)
        {
            if (setData.setIcon != null)
            {
                setIcon.sprite = setData.setIcon;
                setIcon.color = activeColor; // Always active color since we only show active sets
            }
            else
            {
                setIcon.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateSetName(RuneSetData setData)
    {
        if (setNameText != null)
        {
            setNameText.text = setData.setName;
            setNameText.color = activeColor; // Always active color
        }
    }

    private void UpdateSetEffect(RuneSetData setData, int requiredPieces)
    {
        if (setEffectText != null)
        {
            string effectText = GetSetEffectDescription(setData, requiredPieces);
            setEffectText.text = effectText;
            setEffectText.color = activeColor; // Always active color
        }
    }

    // 🔧 SIMPLIFIED: Get clean effect description without piece count
    private string GetSetEffectDescription(RuneSetData setData, int requiredPieces)
    {
        if (setData.setBonuses == null || setData.setBonuses.Count == 0)
        {
            return setData.setDescription;
        }

        // Find the bonus for this piece requirement
        var bonus = setData.setBonuses.FirstOrDefault(b => b.requiredPieces == requiredPieces);
        if (bonus == null || bonus.bonusStats == null || bonus.bonusStats.Count == 0)
        {
            return setData.setDescription;
        }

        // 🎨 CLEAN FORMAT: Just show the bonus stats without "X-Piece:" prefix
        string bonusText = "";
        for (int i = 0; i < bonus.bonusStats.Count; i++)
        {
            var stat = bonus.bonusStats[i];
            string statText = stat.isPercentage ?
                $"{stat.statType} +{stat.value}%" :
                $"{stat.statType} +{stat.value}";

            bonusText += statText;
            if (i < bonus.bonusStats.Count - 1) bonusText += ", ";
        }

        return bonusText;
    }
}
