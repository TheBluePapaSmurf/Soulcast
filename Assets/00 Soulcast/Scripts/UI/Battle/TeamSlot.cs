using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TeamSlot : MonoBehaviour
{
    [Header("Slot Elements")]
    [SerializeField] private Image monsterImage;
    [SerializeField] private TextMeshProUGUI monsterNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private StarDisplay starDisplay;
    [SerializeField] private Button removeButton;
    [SerializeField] private GameObject emptySlotIndicator;
    [SerializeField] private TextMeshProUGUI slotNumberText;

    [Header("Role Display")]
    [SerializeField] private Image roleBackground;
    [SerializeField] private TextMeshProUGUI roleText;

    [Header("Visual States")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Color emptySlotColor = Color.gray;
    [SerializeField] private Color filledSlotColor = Color.white;

    private CollectedMonster assignedMonster;
    private int slotIndex;
    private Action onRemoveCallback;

    public bool IsEmpty => assignedMonster == null;
    public CollectedMonster AssignedMonster => assignedMonster;

    private void Awake()
    {
        removeButton?.onClick.AddListener(OnRemoveClicked);
    }

    public void Setup(int index, Action onRemove)
    {
        slotIndex = index;
        onRemoveCallback = onRemove;

        if (slotNumberText != null)
            slotNumberText.text = (index + 1).ToString();

        ClearSlot();
    }

    public void SetMonster(CollectedMonster monster)
    {
        assignedMonster = monster;

        if (monster?.monsterData == null)
        {
            ClearSlot();
            return;
        }

        UpdateSlotDisplay();
    }

    private void UpdateSlotDisplay()
    {
        var monsterData = assignedMonster.monsterData;

        // Show monster info
        if (monsterNameText != null)
            monsterNameText.text = monsterData.monsterName;

        // ✅ UPDATED: Separate level and star display
        if (levelText != null)
            levelText.text = $"Lv.{assignedMonster.level}";

        // ✅ NEW: Use StarDisplay component for visual stars
        if (starDisplay != null)
            starDisplay.SetStarLevel(assignedMonster.currentStarLevel);

        // Set monster icon
        if (monsterImage != null)
        {
            if (monsterData.icon != null)
            {
                monsterImage.sprite = monsterData.icon;
                monsterImage.color = Color.white;
            }
            else
            {
                monsterImage.sprite = null;
                monsterImage.color = Color.clear;
            }
        }

        // Role display
        if (roleText != null)
            roleText.text = monsterData.role.ToString();

        if (roleBackground != null)
            roleBackground.color = MonsterRoleUtility.GetRoleColor(monsterData.role);

        // Visual state
        if (slotBackground != null)
            slotBackground.color = filledSlotColor;

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(false);

        if (removeButton != null)
            removeButton.gameObject.SetActive(true);
    }

    public void ClearSlot()
    {
        assignedMonster = null;

        // Clear display
        if (monsterNameText != null)
            monsterNameText.text = "";

        // ✅ UPDATED: Clear level text
        if (levelText != null)
            levelText.text = "";

        if (monsterImage != null)
        {
            monsterImage.sprite = null;
            monsterImage.color = Color.clear;
        }

        if (roleText != null)
            roleText.text = "";

        if (roleBackground != null)
            roleBackground.color = Color.clear;

        // Visual state
        if (slotBackground != null)
            slotBackground.color = emptySlotColor;

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(true);

        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
    }

    private void OnRemoveClicked()
    {
        if (assignedMonster != null)
        {
            onRemoveCallback?.Invoke();
        }
    }

    // ✅ NEW: Method to toggle star visibility
    public void ShowStarsOnly(bool showStarsOnly)
    {
        if (levelText != null)
            levelText.gameObject.SetActive(!showStarsOnly);

        if (starDisplay != null)
            starDisplay.gameObject.SetActive(true);
    }

    // Drag & Drop support (optional future feature)
    public void OnDrop(CollectedMonster monster)
    {
        SetMonster(monster);
    }

    // Context menu for testing
    [ContextMenu("Clear Slot")]
    private void TestClearSlot()
    {
        if (Application.isPlaying)
            ClearSlot();
    }

    [ContextMenu("Test Monster Setup")]
    private void TestMonsterSetup()
    {
        if (Application.isPlaying && MonsterCollectionManager.Instance != null)
        {
            var monsters = MonsterCollectionManager.Instance.GetAllMonsters();
            if (monsters.Count > 0)
            {
                SetMonster(monsters[0]);
                Debug.Log($"Set test monster: {monsters[0].monsterData.monsterName}");
            }
        }
    }
}
