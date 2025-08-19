using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaterialSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image monsterIcon;
    public TextMeshProUGUI monsterName;
    public Button removeButton;
    public GameObject emptyState;
    public GameObject filledState;

    private MonsterUpgradePanel upgradePanel;
    private CollectedMonster assignedMaterial;
    private int slotIndex;

    public void Initialize(MonsterUpgradePanel panel, int index)
    {
        upgradePanel = panel;
        slotIndex = index;

        if (removeButton != null)
            removeButton.onClick.AddListener(RemoveMaterial);

        ClearMaterial();
    }

    public void SetMaterial(CollectedMonster material)
    {
        assignedMaterial = material;

        if (filledState != null) filledState.SetActive(true);
        if (emptyState != null) emptyState.SetActive(false);

        if (monsterIcon != null && material.monsterData?.icon != null)
            monsterIcon.sprite = material.monsterData.icon;

        if (monsterName != null)
            monsterName.text = material.GetDisplayName();
    }

    public void ClearMaterial()
    {
        assignedMaterial = null;

        if (filledState != null) filledState.SetActive(false);
        if (emptyState != null) emptyState.SetActive(true);
    }

    private void RemoveMaterial()
    {
        if (assignedMaterial != null && upgradePanel != null)
        {
            upgradePanel.RemoveMaterial(assignedMaterial);
        }
    }
}
