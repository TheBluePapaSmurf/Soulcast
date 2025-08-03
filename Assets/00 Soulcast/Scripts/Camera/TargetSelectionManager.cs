using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using System;

public class TargetSelectionManager : MonoBehaviour
{
    [Header("Target Selection Settings")]
    public bool enableInputTargetSelection = true;
    public float swipeThreshold = 50f;                          // Minimum swipe distance
    public float targetSwitchCooldown = 0.2f;                   // Prevent rapid switching

    [Header("Visual Feedback")]
    public GameObject targetIndicatorPrefab;                    // Optional: visual indicator on selected target
    public Color selectedTargetColor = Color.red;
    public float indicatorHeight = 2f;                          // Height above monster

    [Header("Audio Feedback")]
    public AudioClip targetSwitchSound;
    public AudioClip targetConfirmSound;

    // Events
    public static event Action<Monster> OnTargetChanged;
    public static event Action<Monster> OnTargetConfirmed;

    // Current selection state
    private List<Monster> availableTargets = new List<Monster>();
    private int currentTargetIndex = 0;
    private Monster currentSelectedTarget;
    private bool isTargetSelectionActive = false;
    private MonsterAction pendingAction;

    // Input handling
    private Vector2 swipeStartPos;
    private bool isSwipeStarted = false;
    private float lastTargetSwitchTime = 0f;

    // Visual indicators
    private GameObject currentTargetIndicator;
    private AudioSource audioSource;

    public static TargetSelectionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // ✅ Check if system is properly initialized
        if (!isTargetSelectionActive || !enableInputTargetSelection) return;

        // ✅ Extra safety check
        if (availableTargets == null || availableTargets.Count == 0)
        {
            Debug.LogWarning("Update: availableTargets is null or empty, ending target selection");
            EndTargetSelection();
            return;
        }

        HandleKeyboardInput();
        HandleTouchInput();
    }

    // ✅ Start target selection (called when action needs target)
    public void StartTargetSelection(MonsterAction action, List<Monster> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning("StartTargetSelection: No targets available for selection");
            return;
        }

        // ✅ Check if action is valid
        if (action == null)
        {
            Debug.LogError("StartTargetSelection: action is null!");
            return;
        }

        pendingAction = action;
        availableTargets = targets.Where(t => t != null && t.isAlive && t.monsterData != null).ToList();

        if (availableTargets.Count == 0)
        {
            Debug.LogWarning("StartTargetSelection: No valid alive targets available after filtering");
            return;
        }

        isTargetSelectionActive = true;
        currentTargetIndex = 0;

        SetCurrentTarget(0);

        Debug.Log($"Started target selection for {action.actionName} with {availableTargets.Count} targets");
    }

    // ✅ End target selection
    public void EndTargetSelection()
    {
        isTargetSelectionActive = false;
        pendingAction = null;
        availableTargets.Clear();
        currentSelectedTarget = null;

        HideTargetIndicator();

        Debug.Log("Target selection ended");
    }

    // ✅ Handle keyboard input (A/D, Arrow Keys)
    private void HandleKeyboardInput()
    {
        if (Time.time - lastTargetSwitchTime < targetSwitchCooldown) return;

        bool switchLeft = false;
        bool switchRight = false;
        bool confirm = false;

        // Check keyboard input
        if (Keyboard.current != null)
        {
            switchLeft = Keyboard.current.aKey.wasPressedThisFrame ||
                        Keyboard.current.leftArrowKey.wasPressedThisFrame;

            switchRight = Keyboard.current.dKey.wasPressedThisFrame ||
                         Keyboard.current.rightArrowKey.wasPressedThisFrame;

            confirm = Keyboard.current.spaceKey.wasPressedThisFrame ||
                     Keyboard.current.enterKey.wasPressedThisFrame;
        }

        // Handle mouse click for confirmation
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            confirm = true;
        }

        // Process input
        if (switchLeft)
        {
            SwitchToPreviousTarget();
        }
        else if (switchRight)
        {
            SwitchToNextTarget();
        }

        if (confirm)
        {
            ConfirmCurrentTarget();
        }
    }

    // ✅ Handle touch/swipe input
    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            swipeStartPos = touch.position.ReadValue();
            isSwipeStarted = true;
        }
        else if (touch.press.wasReleasedThisFrame && isSwipeStarted)
        {
            Vector2 swipeEndPos = touch.position.ReadValue();
            Vector2 swipeDelta = swipeEndPos - swipeStartPos;

            if (Mathf.Abs(swipeDelta.x) > swipeThreshold)
            {
                if (Time.time - lastTargetSwitchTime >= targetSwitchCooldown)
                {
                    if (swipeDelta.x > 0)
                    {
                        SwitchToNextTarget();  // Swipe right
                    }
                    else
                    {
                        SwitchToPreviousTarget();  // Swipe left
                    }
                }
            }
            else if (swipeDelta.magnitude < swipeThreshold * 0.5f)
            {
                // Small movement = tap = confirm
                ConfirmCurrentTarget();
            }

            isSwipeStarted = false;
        }
    }

    // ✅ Switch to next target
    private void SwitchToNextTarget()
    {
        if (availableTargets.Count <= 1) return;

        currentTargetIndex = (currentTargetIndex + 1) % availableTargets.Count;
        SetCurrentTarget(currentTargetIndex);
        lastTargetSwitchTime = Time.time;

        PlayTargetSwitchSound();
    }

    // ✅ Switch to previous target
    private void SwitchToPreviousTarget()
    {
        if (availableTargets.Count <= 1) return;

        currentTargetIndex = (currentTargetIndex - 1 + availableTargets.Count) % availableTargets.Count;
        SetCurrentTarget(currentTargetIndex);
        lastTargetSwitchTime = Time.time;

        PlayTargetSwitchSound();
    }

    // ✅ Set current target and update visuals
    private void SetCurrentTarget(int index)
    {
        if (index < 0 || index >= availableTargets.Count)
        {
            Debug.LogWarning($"SetCurrentTarget: Invalid index {index}, availableTargets count: {availableTargets.Count}");
            return;
        }

        currentSelectedTarget = availableTargets[index];

        // ✅ Check if target is valid
        if (currentSelectedTarget == null)
        {
            Debug.LogError($"SetCurrentTarget: Target at index {index} is null!");
            return;
        }

        // ✅ Check if target monsterData is valid
        if (currentSelectedTarget.monsterData == null)
        {
            Debug.LogError($"SetCurrentTarget: Target {currentSelectedTarget.name} has null monsterData!");
            return;
        }

        // Update visual indicator
        ShowTargetIndicator(currentSelectedTarget);

        // Focus camera on new target
        if (DynamicCombatCamera.Instance != null)
        {
            DynamicCombatCamera.Instance.FocusOnTarget(currentSelectedTarget);
        }

        // Fire event
        OnTargetChanged?.Invoke(currentSelectedTarget);

        Debug.Log($"Target switched to: {currentSelectedTarget.monsterData.monsterName} ({index + 1}/{availableTargets.Count})");
    }

    // ✅ Confirm current target selection
    private void ConfirmCurrentTarget()
    {
        // ✅ Check alle mogelijke null references
        if (currentSelectedTarget == null || pendingAction == null)
        {
            Debug.LogWarning("ConfirmCurrentTarget: currentSelectedTarget or pendingAction is null");
            return;
        }

        // ✅ Check CombatManager.Instance
        if (CombatManager.Instance == null)
        {
            Debug.LogError("ConfirmCurrentTarget: CombatManager.Instance is null!");
            return;
        }

        // ✅ Check currentSelectedTarget.monsterData
        if (currentSelectedTarget.monsterData == null)
        {
            Debug.LogError("ConfirmCurrentTarget: currentSelectedTarget.monsterData is null!");
            return;
        }

        // ✅ Store references BEFORE EndTargetSelection clears them
        Monster selectedTarget = currentSelectedTarget;
        MonsterAction actionToExecute = pendingAction;
        string targetName = selectedTarget.monsterData.monsterName;

        PlayTargetConfirmSound();

        // Fire event
        OnTargetConfirmed?.Invoke(selectedTarget);

        // Execute action through CombatManager
        CombatManager.Instance.PlayerUseAction(actionToExecute, selectedTarget);

        // ✅ Log BEFORE EndTargetSelection (which sets currentSelectedTarget to null)
        Debug.Log($"Target confirmed: {targetName}");

        // End target selection (this sets currentSelectedTarget to null)
        EndTargetSelection();
    }

    // ✅ Show visual indicator on target
    private void ShowTargetIndicator(Monster target)
    {
        HideTargetIndicator();

        if (targetIndicatorPrefab != null && target != null)
        {
            Vector3 indicatorPos = target.transform.position + Vector3.up * indicatorHeight;
            currentTargetIndicator = Instantiate(targetIndicatorPrefab, indicatorPos, Quaternion.identity);

            // Make indicator follow target
            StartCoroutine(FollowTarget(currentTargetIndicator, target));
        }

        // Optional: Change target's color/material temporarily
        // This can be implemented based on your monster visual setup
    }

    // ✅ Hide target indicator
    private void HideTargetIndicator()
    {
        if (currentTargetIndicator != null)
        {
            Destroy(currentTargetIndicator);
            currentTargetIndicator = null;
        }
    }

    // ✅ Coroutine to make indicator follow target
    private System.Collections.IEnumerator FollowTarget(GameObject indicator, Monster target)
    {
        while (indicator != null && target != null && target.gameObject != null)
        {
            indicator.transform.position = target.transform.position + Vector3.up * indicatorHeight;
            yield return null;
        }
    }

    // ✅ Audio feedback
    private void PlayTargetSwitchSound()
    {
        if (audioSource != null && targetSwitchSound != null)
        {
            audioSource.PlayOneShot(targetSwitchSound);
        }
    }

    private void PlayTargetConfirmSound()
    {
        if (audioSource != null && targetConfirmSound != null)
        {
            audioSource.PlayOneShot(targetConfirmSound);
        }
    }

    // ✅ Public methods for external control
    public Monster GetCurrentSelectedTarget()
    {
        return currentSelectedTarget;
    }

    public bool IsTargetSelectionActive()
    {
        return isTargetSelectionActive;
    }

    public int GetTargetCount()
    {
        return availableTargets.Count;
    }

    public int GetCurrentTargetIndex()
    {
        return currentTargetIndex;
    }

    // ✅ Force select specific target by index
    public void SelectTargetByIndex(int index)
    {
        if (isTargetSelectionActive && index >= 0 && index < availableTargets.Count)
        {
            currentTargetIndex = index;
            SetCurrentTarget(index);
        }
    }

    // ✅ Cancel target selection
    public void CancelTargetSelection()
    {
        EndTargetSelection();

        // Return camera to player monster
        if (DynamicCombatCamera.Instance != null && CombatManager.Instance.currentActiveMonster != null)
        {
            DynamicCombatCamera.Instance.FocusOnMonster(CombatManager.Instance.currentActiveMonster);
        }

        Debug.Log("Target selection cancelled");
    }
}
