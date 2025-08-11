using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionCard : MonoBehaviour
{
    [Header("Card Elements")]
    public Image monsterImage;
    public TextMeshProUGUI monsterNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI duplicateCountText;
    public GameObject duplicateIndicator;

    private CollectedMonster collectedMonster;

    public void Setup(CollectedMonster monster)
    {
        collectedMonster = monster;

        if (monster.monsterData == null) return;

        // Set monster info
        if (monsterNameText != null)
        {
            monsterNameText.text = monster.monsterData.monsterName;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{monster.currentLevel}";
        }

        // Set monster sprite - FIXED: Use 'icon' instead of 'sprite'
        if (monsterImage != null && monster.monsterData.icon != null)
        {
            monsterImage.sprite = monster.monsterData.icon;
        }
    }

    public void OnCardClicked()
    {
        // Show detailed monster info
        Debug.Log($"Viewing details for {collectedMonster.monsterData.monsterName}");
        // You can implement a detailed view popup here
    }
}
