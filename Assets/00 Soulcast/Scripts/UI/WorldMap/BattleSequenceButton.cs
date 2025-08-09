using System;
using System.Collections.Generic;
using System.Linq; // ✅ FIX: Missing LINQ directive
using UnityEngine;
using UnityEngine.UI;

public class BattleSequenceButton : MonoBehaviour
{
    [Header("Battle Configuration")]
    [SerializeField] private LevelDatabase levelDatabase;

    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private Text battleTitle;
    [SerializeField] private Text battleDescription;
    [SerializeField] private Image battleIcon;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private GameObject completedOverlay;

    [Header("Star Rating")]
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite filledStar;
    [SerializeField] private Sprite emptyStar;

    [Header("Visual States")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color completedColor = Color.green;

    public event Action OnBattleSequenceSelected;

    private CombatTemplate combatTemplate;
    private int battleSequenceId;
    private bool isUnlocked;
    private bool isCompleted;
    private int starRating;

    private void Awake()
    {
        // Load battle database if not assigned
        if (levelDatabase == null)
            levelDatabase = Resources.Load<LevelDatabase>("LevelDatabase");

        // Safer component finding with null checks
        if (button == null)
        {
            button = GetComponent<Button>();

            // If still null, try to find it in children
            if (button == null)
                button = GetComponentInChildren<Button>();
        }

        // Only add listener if button exists
        if (button != null)
        {
            button.onClick.AddListener(HandleBattleClick);
        }
        else
        {
            Debug.LogError($"No Button component found on {gameObject.name} or its children!", this);
        }

        // Auto-find components if not assigned
        if (battleTitle == null)
            battleTitle = GetComponentInChildren<Text>();

        if (battleIcon == null)
            battleIcon = GetComponent<Image>();
    }

    public void SetBattleData(int id)
    {
        battleSequenceId = id;

        // Load battle configuration
        int currentRegion = PlayerPrefs.GetInt("CurrentRegion", 1);
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

        if (levelDatabase != null)
        {
            combatTemplate = levelDatabase.GetBattleConfiguration(currentRegion, currentLevel, id);

            if (combatTemplate != null)
            {
                // Update UI with battle config data
                if (battleTitle != null)
                    battleTitle.text = combatTemplate.combatName;

                if (battleDescription != null)
                    battleDescription.text = combatTemplate.combatDescription;

                if (battleIcon != null && combatTemplate.battleIcon != null)
                    battleIcon.sprite = combatTemplate.battleIcon;
            }
        }

        // Fallback to default naming
        if (battleTitle != null && string.IsNullOrEmpty(battleTitle.text))
            battleTitle.text = $"Battle {id}";
    }

    private void HandleBattleClick()
    {
        if (!isUnlocked) return;

        Debug.Log($"Battle sequence {battleSequenceId} clicked");

        // Show pre-battle team selection instead of direct battle
        var teamSelection = FindFirstObjectByType<PreBattleTeamSelection>();
        if (teamSelection != null && combatTemplate != null)
        {
            teamSelection.ShowTeamSelection(combatTemplate);

            // Setup callbacks - ✅ FIX: Correct parameter types
            teamSelection.OnBattleStart += (config, team) => StartBattleWithTeam(config, team);
            teamSelection.OnSelectionCancelled += () => Debug.Log("Team selection cancelled");
        }
        else
        {
            // Fallback to old behavior
            OnBattleSequenceSelected?.Invoke();
        }
    }

    // ✅ FIX: Changed parameter type from List<MonsterData> to List<CollectedMonster>
    private void StartBattleWithTeam(CombatTemplate config, List<CollectedMonster> team)
    {
        Debug.Log($"Starting {config.combatName} with team of {team.Count} monsters");

        // Save battle configuration and team data for combat scene
        PlayerPrefs.SetString("CurrentBattleConfig", config.name);

        // ✅ FIX: Use CollectedMonster uniqueID instead of MonsterData name
        PlayerPrefs.SetString("SelectedTeamIDs", string.Join(",", team.Select(m => m.uniqueID)));

        // Also save monster names for debugging/display purposes
        PlayerPrefs.SetString("SelectedTeamNames", string.Join(",", team.Select(m => m.monsterData.monsterName)));

        // Trigger original battle start
        OnBattleSequenceSelected?.Invoke();
    }

    public void UpdateBattleState(bool unlocked, bool completed, int stars)
    {
        isUnlocked = unlocked;
        isCompleted = completed;
        starRating = stars;

        // Update button interactability
        if (button != null)
            button.interactable = isUnlocked;

        // Update visual state
        UpdateVisualState();

        // Update overlays
        if (lockOverlay != null)
            lockOverlay.SetActive(!isUnlocked);

        if (completedOverlay != null)
            completedOverlay.SetActive(isCompleted);

        // Update star display
        UpdateStarDisplay();
    }

    private void UpdateVisualState()
    {
        Color targetColor = unlockedColor;

        if (!isUnlocked)
            targetColor = lockedColor;
        else if (isCompleted)
            targetColor = completedColor;

        // Apply color to icon
        if (battleIcon != null)
            battleIcon.color = targetColor;
    }

    private void UpdateStarDisplay()
    {
        if (stars == null) return;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].sprite = i < starRating ? filledStar : emptyStar;
                stars[i].gameObject.SetActive(isCompleted);
            }
        }
    }

    // Public properties voor external access
    public int BattleSequenceId => battleSequenceId;
    public bool IsUnlocked => isUnlocked;
    public bool IsCompleted => isCompleted;
    public int StarRating => starRating;
}
