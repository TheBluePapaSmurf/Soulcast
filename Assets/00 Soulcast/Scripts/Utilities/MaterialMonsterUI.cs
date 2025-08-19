using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaterialMonsterUI : MonoBehaviour
{
    [Header("UI References")]
    public Image monsterIcon;
    public TextMeshProUGUI monsterName;
    public TextMeshProUGUI monsterLevel;
    public Button selectButton;
    public GameObject selectedIndicator;

    private MonsterUpgradePanel upgradePanel;
    private CollectedMonster material;
    private bool isSelected;

    public void Initialize(MonsterUpgradePanel panel, CollectedMonster monster)
    {
        upgradePanel = panel;
        material = monster;

        if (selectButton != null)
            selectButton.onClick.AddListener(ToggleSelection);

        if (monsterIcon != null && monster.monsterData?.icon != null)
            monsterIcon.sprite = monster.monsterData.icon;

        if (monsterName != null)
            monsterName.text = monster.GetDisplayName();

        if (monsterLevel != null)
            monsterLevel.text = $"Lv.{monster.currentLevel} - {monster.currentStarLevel}⭐";

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);
    }

    private void ToggleSelection()
    {
        if (upgradePanel == null || material == null) return;

        if (isSelected)
        {
            upgradePanel.RemoveMaterial(material);
        }
        else
        {
            upgradePanel.AddMaterial(material);
        }
    }
}
