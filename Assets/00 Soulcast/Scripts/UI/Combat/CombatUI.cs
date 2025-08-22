using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CombatUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject attackSelectionPanel;
    public GameObject tooltipPanel;
    public GameObject combatInfoPanel;

    [Header("Action Buttons")]
    public Button[] actionButtons;

    [Header("Tooltip Elements")]
    public TextMeshProUGUI tooltipTitle;
    public TextMeshProUGUI tooltipDescription;
    public TextMeshProUGUI tooltipPower;
    public TextMeshProUGUI tooltipCost;
    public TextMeshProUGUI tooltipType;
    public TextMeshProUGUI tooltipCooldown;

    [Header("Combat Info")]
    public TextMeshProUGUI currentMonsterName;
    public TextMeshProUGUI turnInfo;
    public Slider currentMonsterHP;
    public Slider currentMonsterEnergy;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI energyText;

    [Header("Target Selection")]
    public GameObject targetSelectionPanel;
    public Button[] targetButtons;

    [Header("Element Display")]
    public Image monsterElementIcon;
    public TextMeshProUGUI monsterElementText;

    private Monster currentActiveMonster;
    private MonsterAction selectedAction;
    private List<Monster> availableTargets = new List<Monster>();

    [Header("Performance Settings")]
    public bool enableSmartUpdates = true;
    private Monster lastActiveMonster;
    private float lastHPValue = -1f;
    private float lastEnergyValue = -1f;

    public static CombatUI Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeUI();
    }

    void InitializeUI()
    {
        // Hide all panels initially
        attackSelectionPanel.SetActive(false);
        tooltipPanel.SetActive(false);
        targetSelectionPanel.SetActive(false);

        // Setup action button events
        for (int i = 0; i < actionButtons.Length; i++)
        {
            int buttonIndex = i; // Capture for closure
            actionButtons[i].onClick.AddListener(() => OnActionButtonClicked(buttonIndex));
        }

        // Setup target button events
        for (int i = 0; i < targetButtons.Length; i++)
        {
            int buttonIndex = i; // Capture for closure
            targetButtons[i].onClick.AddListener(() => OnTargetButtonClicked(buttonIndex));
        }
    }

    public void ShowPlayerTurn(Monster monster)
    {
        if (currentActiveMonster != monster)
        {
            Debug.Log($"UI: Switching from {(currentActiveMonster?.monsterData?.monsterName ?? "none")} to {monster.monsterData.monsterName}");
        }

        currentActiveMonster = monster;
        selectedAction = null;

        UpdateCombatInfo();
        SetupActionButtons();
        attackSelectionPanel.SetActive(true);
        targetSelectionPanel.SetActive(false);
        tooltipPanel.SetActive(false);

        Debug.Log($"UI: Showing turn for {monster.monsterData.monsterName}");
    }

    public void HidePlayerTurn()
    {
        attackSelectionPanel.SetActive(false);
        targetSelectionPanel.SetActive(false);
        tooltipPanel.SetActive(false);

        Debug.Log("UI: Hidden player turn");
    }

    void UpdateCombatInfo()
    {
        if (currentActiveMonster != null)
        {
            currentMonsterName.text = currentActiveMonster.monsterData.monsterName;
            turnInfo.text = $"Round {CombatManager.Instance.currentTurn}";

            // Update HP slider and text
            currentMonsterHP.maxValue = currentActiveMonster.monsterData.baseHP;
            currentMonsterHP.value = currentActiveMonster.currentHP;
            if (hpText != null)
                hpText.text = StringCache.GetCachedString("{0}/{1}",
                    currentActiveMonster.currentHP, currentActiveMonster.monsterData.baseHP);

            // Update Energy slider and text
            currentMonsterEnergy.maxValue = currentActiveMonster.monsterData.baseEnergy;
            currentMonsterEnergy.value = currentActiveMonster.currentEnergy;
            if (energyText != null)
                energyText.text = StringCache.GetCachedString("{0}/{1}",
                    currentActiveMonster.currentEnergy, currentActiveMonster.monsterData.baseEnergy);

        // Update element display
        if (monsterElementIcon != null)
                monsterElementIcon.color = ElementalSystem.GetElementColor(currentActiveMonster.monsterData.element);
            if (monsterElementText != null)
                monsterElementText.text = currentActiveMonster.monsterData.element.ToString();
        }
    }

    void SetupActionButtons()
    {
        if (currentActiveMonster == null || currentActiveMonster.monsterData == null)
        {
            Debug.LogError("Cannot setup action buttons - currentActiveMonster or monsterData is null!");
            return;
        }

        List<MonsterAction> actions = currentActiveMonster.monsterData.GetAvailableActions();
        Debug.Log($"Setting up {actions.Count} action buttons for {currentActiveMonster.monsterData.monsterName}");

        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (i < actions.Count && actions[i] != null)
            {
                actionButtons[i].gameObject.SetActive(true);

                // Get the main button image
                Image buttonImage = actionButtons[i].GetComponent<Image>();

                if (buttonImage != null)
                {
                    // Set the sprite from MonsterAction.icon
                    if (actions[i].icon != null)
                    {
                        buttonImage.sprite = actions[i].icon;
                        buttonImage.type = Image.Type.Simple;
                        buttonImage.preserveAspect = true;
                        buttonImage.color = Color.white; // Always white, no color coding

                        Debug.Log($"✅ Set icon for {actions[i].actionName}: {actions[i].icon.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"❌ No icon assigned for action: {actions[i].actionName}");
                        buttonImage.color = Color.white; // Keep white even if no icon
                    }

                    // Check if action can be used (only affects interactability, not color)
                    bool canUse = currentActiveMonster.CanUseAction(actions[i]);
                    actionButtons[i].interactable = canUse;
                }
                else
                {
                    Debug.LogError($"No Image component found on action button {i}!");
                }
            }
            else
            {
                actionButtons[i].gameObject.SetActive(false);
            }
        }
    }



    void ResetActionButtonColors()
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i].gameObject.activeInHierarchy)
            {
                ColorBlock colors = actionButtons[i].colors;
                colors.normalColor = Color.white;
                actionButtons[i].colors = colors;
            }
        }
    }

    public void ShowActionTooltip(MonsterAction action)
    {
        if (action != null)
        {
            tooltipTitle.text = action.actionName;
            tooltipDescription.text = action.description;
            tooltipPower.text = $"Power: {action.basePower}";
            tooltipCost.text = $"Cost: {action.energyCost} Energy";
            tooltipType.text = $"Type: {action.type} ({action.category})";

            // Show cooldown info
            if (tooltipCooldown != null)
            {
                if (action.cooldownTurns > 0)
                {
                    int remainingCooldown = 0;
                    if (currentActiveMonster.actionCooldowns.ContainsKey(action))
                        remainingCooldown = currentActiveMonster.actionCooldowns[action];

                    tooltipCooldown.text = remainingCooldown > 0 ?
                        $"Cooldown: {remainingCooldown} turns remaining" :
                        $"Cooldown: {action.cooldownTurns} turns";
                }
                else
                {
                    tooltipCooldown.text = "No cooldown";
                }
            }

            // Add status effect descriptions
            string effects = "";
            foreach (var statusEffect in action.statusEffects)
            {
                effects += $"\n• {statusEffect.effectName} ({statusEffect.duration} turns)";
            }

            // Add stat modifier descriptions
            foreach (var statMod in action.statModifiers)
            {
                string modText = statMod.modifierAmount > 0 ? "+" : "";
                effects += $"\n• {modText}{statMod.modifierAmount} {statMod.statType}";
            }

            if (!string.IsNullOrEmpty(effects))
            {
                tooltipDescription.text += effects;
            }

            tooltipPanel.SetActive(true);
        }
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    // ✅ REPLACE this method in your CombatUI.cs
    void OnActionButtonClicked(int actionIndex)
    {
        if (currentActiveMonster == null || currentActiveMonster.monsterData == null)
        {
            Debug.LogError("Cannot execute action - currentActiveMonster or monsterData is null!");
            return;
        }

        List<MonsterAction> actions = currentActiveMonster.monsterData.GetAvailableActions();
        if (actionIndex < actions.Count)
        {
            selectedAction = actions[actionIndex];

            // Show tooltip for selected action
            ShowActionTooltip(selectedAction);

            // Highlight selected button
            HighlightSelectedButton(actionIndex);

            // Handle different target types
            switch (selectedAction.targetType)
            {
                case TargetType.Self:
                    Debug.Log($"Using {selectedAction.actionName} on self");
                    ExecuteAction(currentActiveMonster);
                    break;

                case TargetType.Single:
                    // ✅ NEW: Use input-based target selection instead of UI buttons
                    List<Monster> enemyTargets = GetAllEnemies();
                    if (enemyTargets.Count > 0)
                    {
                        TargetSelectionManager.Instance.StartTargetSelection(selectedAction, enemyTargets);

                        // Hide action UI during target selection
                        attackSelectionPanel.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning("No enemy targets available!");
                    }
                    break;

                case TargetType.AllEnemies:
                    Debug.Log($"Using {selectedAction.actionName} on all enemies");
                    ExecuteActionOnAllTargets(GetAllEnemies());
                    break;

                case TargetType.AllAllies:
                    Debug.Log($"Using {selectedAction.actionName} on all allies");
                    ExecuteActionOnAllTargets(GetAllAllies());
                    break;

                case TargetType.Random:
                    var possibleTargets = GetAllEnemies();
                    if (possibleTargets.Count > 0)
                    {
                        Monster randomTarget = possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)];
                        Debug.Log($"Using {selectedAction.actionName} on random target: {randomTarget.monsterData.monsterName}");
                        ExecuteAction(randomTarget);
                    }
                    break;
            }
        }
    }

    // ✅ ADD this new method to CombatUI.cs
    void OnEnable()
    {
        // Subscribe to target selection events
        TargetSelectionManager.OnTargetConfirmed += OnTargetConfirmed;
    }

    void OnDisable()
    {
        // Unsubscribe from target selection events
        TargetSelectionManager.OnTargetConfirmed -= OnTargetConfirmed;
    }

    // ✅ ADD this new method to CombatUI.cs
    private void OnTargetConfirmed(Monster target)
    {
        // Show action UI again after target is confirmed
        if (currentActiveMonster != null)
        {
            attackSelectionPanel.SetActive(false); // Action is being executed
            tooltipPanel.SetActive(false);
            targetSelectionPanel.SetActive(false); // Hide old target UI
            ResetActionButtonColors();
        }
    }


    void HighlightSelectedButton(int selectedIndex)
    {
        ResetActionButtonColors();

        if (selectedIndex < actionButtons.Length && actionButtons[selectedIndex].gameObject.activeInHierarchy)
        {
            ColorBlock colors = actionButtons[selectedIndex].colors;
            colors.normalColor = Color.yellow;
            actionButtons[selectedIndex].colors = colors;
        }
    }

    void ShowTargetSelection()
    {
        // Get available enemy targets
        availableTargets.Clear();
        availableTargets.AddRange(GetAllEnemies());

        // Setup target buttons
        for (int i = 0; i < targetButtons.Length; i++)
        {
            if (i < availableTargets.Count)
            {
                targetButtons[i].gameObject.SetActive(true);
                TextMeshProUGUI buttonText = targetButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = $"{availableTargets[i].monsterData.monsterName}\n({availableTargets[i].monsterData.element})";

                // Show elemental advantage
                float advantage = ElementalSystem.GetElementalAdvantage(
                    currentActiveMonster.monsterData.element,
                    availableTargets[i].monsterData.element);

                Image buttonImage = targetButtons[i].GetComponent<Image>();
                if (advantage > 1.0f)
                    buttonImage.color = Color.green; // Advantage
                else if (advantage < 1.0f)
                    buttonImage.color = Color.red; // Disadvantage
                else
                    buttonImage.color = Color.white; // Neutral
            }
            else
            {
                targetButtons[i].gameObject.SetActive(false);
            }
        }

        targetSelectionPanel.SetActive(true);
        Debug.Log($"Select target for {selectedAction.actionName}");
    }

    void OnTargetButtonClicked(int targetIndex)
    {
        if (targetIndex < availableTargets.Count)
        {
            Monster selectedTarget = availableTargets[targetIndex];

            Debug.Log($"Target selected: {selectedTarget.monsterData.monsterName}");
            Debug.Log($"Executing {selectedAction.actionName} on {selectedTarget.monsterData.monsterName}");

            ExecuteAction(selectedTarget);
        }
    }

    void ExecuteAction(Monster target)
    {
        Debug.Log($"UI: ExecuteAction called - {currentActiveMonster.monsterData.monsterName} uses {selectedAction.actionName}");

        // Execute the action through combat manager
        CombatManager.Instance.PlayerUseAction(selectedAction, target);

        // Clear current selection state
        selectedAction = null;
        targetSelectionPanel.SetActive(false);
        tooltipPanel.SetActive(false);
        ResetActionButtonColors();

        Debug.Log("UI: Action executed, waiting for CombatManager to handle next turn");
    }

    void ExecuteActionOnAllTargets(List<Monster> targets)
    {
        if (targets.Count == 0) return;

        Debug.Log($"UI: ExecuteActionOnAllTargets called - {currentActiveMonster.monsterData.monsterName} uses {selectedAction.actionName} on {targets.Count} targets");

        // Use the new multi-target method
        CombatManager.Instance.PlayerUseActionOnMultipleTargets(selectedAction, targets);

        // Clear current selection state immediately
        selectedAction = null;
        targetSelectionPanel.SetActive(false);
        tooltipPanel.SetActive(false);
        ResetActionButtonColors();

        // Hide the action UI since turn is ending
        HidePlayerTurn();

        Debug.Log("UI: Multi-target action executed and UI hidden");
    }



    private List<Monster> GetAllEnemies()
    {
        return CombatManager.Instance.enemyMonsters.Where(m => m.isAlive).ToList();
    }

    private List<Monster> GetAllAllies()
    {
        return CombatManager.Instance.playerMonsters.Where(m => m.isAlive).ToList();
    }

    public void UpdateUI()
    {
        if (!enableSmartUpdates)
        {
            // Original behavior
            if (currentActiveMonster != null)
            {
                UpdateCombatInfo();
                SetupActionButtons();
            }
            return;
        }

        // Smart updates - only update when values actually change
        if (currentActiveMonster != null)
        {
            bool needsUpdate = false;

            // Check if monster changed
            if (lastActiveMonster != currentActiveMonster)
            {
                lastActiveMonster = currentActiveMonster;
                needsUpdate = true;
            }

            // Check if HP changed
            if (lastHPValue != currentActiveMonster.currentHP)
            {
                lastHPValue = currentActiveMonster.currentHP;
                needsUpdate = true;
            }

            // Check if Energy changed
            if (lastEnergyValue != currentActiveMonster.currentEnergy)
            {
                lastEnergyValue = currentActiveMonster.currentEnergy;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                UpdateCombatInfo();
                SetupActionButtons();
            }
        }
    }

    // Button event handlers for testing
    public void OnMouseEnterActionButton(int index)
    {
        if (currentActiveMonster != null && currentActiveMonster.monsterData != null)
        {
            var actions = currentActiveMonster.monsterData.GetAvailableActions();
            if (index < actions.Count)
            {
                ShowActionTooltip(actions[index]);
            }
        }
    }

    public void OnMouseExitActionButton()
    {
        // Keep tooltip visible when button is selected
        if (selectedAction == null)
        {
            HideTooltip();
        }
    }
}
