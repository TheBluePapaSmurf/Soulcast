using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterDisplayCard : MonoBehaviour
{
    [Header("Card Elements")]
    public Image monsterIcon;
    public Image backgroundImage;
    public TextMeshProUGUI monsterNameText;
    public TextMeshProUGUI levelText;
    public StarDisplay starDisplay;

    [Header("Rarity Effects")]
    public Image rarityBorder;
    public ParticleSystem rarityEffect;

    [Header("New Indicator")]
    public GameObject newIndicator;

    [Header("Duplicate Indicator")]
    public GameObject duplicateIndicator;
    public TextMeshProUGUI duplicateCountText;

    private GachaMonster gachaMonster;

    public void Setup(GachaMonster monster, Color rarityColor)
    {
        gachaMonster = monster;

        if (monster?.monsterData == null) return;

        // Set monster info
        if (monsterNameText != null)
        {
            monsterNameText.text = monster.monsterData.monsterName;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.1"; // New monsters start at level 1
        }

        // Set monster icon
        if (monsterIcon != null && monster.monsterData.icon != null)
        {
            monsterIcon.sprite = monster.monsterData.icon;
        }

        // Set star display
        if (starDisplay != null)
        {
            starDisplay.SetStarLevel(monster.monsterData.defaultStarLevel);
        }

        // Set rarity visual effects
        if (rarityBorder != null)
        {
            rarityBorder.color = rarityColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(Color.white, rarityColor, 0.2f);
        }

        // Start particle effect if available
        if (rarityEffect != null)
        {
            var main = rarityEffect.main;
            main.startColor = rarityColor;
            rarityEffect.Play();
        }
    }

    public GachaMonster GetGachaMonster()
    {
        return gachaMonster;
    }


void CheckNewOrDuplicate()
    {
        if (PlayerInventory.Instance == null || gachaMonster.monsterData == null) return;

        CollectedMonster existingMonster = PlayerInventory.Instance.GetAllMonsters()
    .FirstOrDefault(m => m.monsterData == gachaMonster.monsterData);

        if (existingMonster == null)
        {
            // This is a new monster
            ShowNewIndicator();
        }

        else
        {
            // First time getting this monster (just obtained)
            ShowNewIndicator();
        }
    }

    void ShowNewIndicator()
    {
        if (newIndicator != null)
        {
            newIndicator.SetActive(true);
        }

        if (duplicateIndicator != null)
        {
            duplicateIndicator.SetActive(false);
        }
    }

    public void OnCardClicked()
    {
        // Show detailed monster info (implement later)
        Debug.Log($"Clicked on {gachaMonster.monsterData.monsterName}");
    }
}
