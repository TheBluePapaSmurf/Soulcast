// Enhanced Monster.cs with Buff/Debuff System
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class Monster : MonoBehaviour
{
    [Header("Monster Configuration")]
    public MonsterData monsterData;

    [Header("3D Model")]
    public ModelController modelController;

    [Header("Current Stats")]
    public int currentHP;
    public int currentATK;
    public int currentDEF;
    public int currentSPD;
    public int currentEnergy;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float meleeAttackDistance = 1.5f;
    public Vector3 originalPosition;
    public Quaternion originalRotation;

    [Header("Combat State")]
    public bool isPlayerControlled = true;
    public bool isAlive = true;
    public bool hasTakenTurn = false;

    [Header("Wave Information")]
    public int waveIndex = 0; // Which wave this monster belongs to

    [Header("Action Cooldowns")]
    public Dictionary<MonsterAction, int> actionCooldowns = new Dictionary<MonsterAction, int>();

    [Header("Active Buff/Debuff Effects")]
    public List<ActiveBuffDebuffEffect> activeBuffDebuffEffects = new List<ActiveBuffDebuffEffect>();

    [Header("Performance Settings")]
    public bool useFixedTimeStep = true;
    public float movementTimeStep = 0.02f; // 50 FPS for movement

    public void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        InitializeMonster();
    }

    public void InitializeMonster()
    {
        if (monsterData != null)
        {
            currentHP = monsterData.baseHP;
            currentATK = monsterData.baseATK;
            currentDEF = monsterData.baseDEF;
            currentSPD = monsterData.baseSPD;
            currentEnergy = monsterData.baseEnergy;
            isAlive = true;
            hasTakenTurn = false;

            // Initialize action cooldowns
            actionCooldowns.Clear();
            var actions = monsterData.GetAvailableActions();
            foreach (var action in actions)
            {
                actionCooldowns[action] = 0;
            }

            Debug.Log($"Initialized {monsterData.monsterName} ({monsterData.element} element)");
        }
        else
        {
            Debug.LogError($"Monster {gameObject.name} has no MonsterData assigned!");
        }

        modelController = GetComponentInChildren<ModelController>();
        if (modelController != null)
        {
            Debug.Log($"Model controller found for {monsterData.monsterName}");
        }

        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    public bool CanUseAction(MonsterAction action)
    {
        if (action == null)
        {
            Debug.LogWarning($"{gameObject.name}: Trying to check null action!");
            return false;
        }

        if (!isAlive)
        {
            Debug.LogWarning($"{gameObject.name} is dead and cannot act!");
            return false;
        }

        if (hasTakenTurn)
        {
            Debug.LogWarning($"{gameObject.name} has already taken their turn!");
            return false;
        }

        // Check if prevented from acting by buff/debuff effects
        if (IsPreventedFromActing())
        {
            Debug.LogWarning($"{gameObject.name} is prevented from acting by an effect!");
            return false;
        }

        if (currentEnergy < action.energyCost)
        {
            Debug.LogWarning($"{gameObject.name} doesn't have enough energy for {action.actionName}! Has {currentEnergy}, needs {action.energyCost}");
            return false;
        }

        // Check if action is on cooldown
        if (actionCooldowns.ContainsKey(action) && actionCooldowns[action] > 0)
        {
            Debug.LogWarning($"{gameObject.name}: {action.actionName} is on cooldown for {actionCooldowns[action]} more turns!");
            return false;
        }

        return true;
    }

    public void UseAction(MonsterAction action, Monster target)
    {
        if (action == null)
        {
            Debug.LogError($"{gameObject.name}: Cannot use null action!");
            return;
        }

        if (!CanUseAction(action))
        {
            Debug.Log($"{monsterData.monsterName} cannot use {action.actionName}!");
            return;
        }

        // ONLY HANDLE MOVEMENT LOGIC - Remove all duplicate execution
        if (action.IsMeleeAttack && target != null)
        {
            // Move towards target for melee attacks
            StartCoroutine(PerformMeleeAttackWithMovement(action, target));
        }
        else
        {
            // Stay still for ranged attacks or non-attack actions
            StartCoroutine(PerformActionInPlace(action, target));
        }
    }

    private IEnumerator PerformMeleeAttackWithMovement(MonsterAction action, Monster target)
    {
        Debug.Log($"{monsterData.monsterName} starting melee attack movement");

        // 1. Move towards target
        yield return StartCoroutine(MoveToTarget(target));

        // 2. Execute the attack
        yield return StartCoroutine(ExecuteActionSequence(action, target));

        // 3. Return to original position
        yield return StartCoroutine(ReturnToOriginalPosition());

        // 4. Complete turn sequence
        CompleteTurn();
    }

    private IEnumerator PerformActionInPlace(MonsterAction action, Monster target)
    {
        // Execute the action without moving
        yield return StartCoroutine(ExecuteActionSequence(action, target));

        // Wait for animation to finish (if ranged attack with projectile)
        if (action.IsRangedAttack)
        {
            // Add extra time for visual impact
            yield return new WaitForSeconds(0.5f);
        }

        // Complete turn sequence
        CompleteTurn();
    }

    /// <summary>
    /// Notify UI components about buff/debuff changes
    /// </summary>
    private void NotifyBuffDebuffChanged()
    {
        // Find and update floating health bar
        var healthBar = GetComponentInChildren<FloatingHealthBar>();
        if (healthBar != null)
        {
            healthBar.OnBuffDebuffChanged();
        }

        // Notify combat UI if needed
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnMonsterStatsChanged();
        }
    }


    private void CompleteTurn()
    {
        hasTakenTurn = true;

        Debug.Log($"{monsterData.monsterName} completed their turn");

        // Notify combat manager that turn is complete
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnMonsterStatsChanged();
            CombatManager.Instance.OnTurnCompleted();
        }
    }

    // Movement methods
    private IEnumerator MoveToTarget(Monster target)
    {
        if (target == null) yield break;

        Vector3 targetPosition = target.transform.position;
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 meleePosition = targetPosition - direction * meleeAttackDistance;

        float moveDistance = Vector3.Distance(transform.position, meleePosition);
        float moveTime = moveDistance / moveSpeed;
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;

        // Use fixed timestep for consistent performance
        WaitForSeconds fixedWait = useFixedTimeStep ? new WaitForSeconds(movementTimeStep) : null;

        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;
            transform.position = Vector3.Lerp(startPosition, meleePosition, t);

            // Face the target while moving
            transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));

            elapsedTime += useFixedTimeStep ? movementTimeStep : Time.deltaTime;

            if (useFixedTimeStep)
                yield return fixedWait;
            else
                yield return null;
        }

        transform.position = meleePosition;
        Debug.Log($"{monsterData.monsterName} moved to melee range of {target.monsterData.monsterName}");
    }

    private IEnumerator ReturnToOriginalPosition()
    {
        // Trigger movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(true);
        }

        float returnDistance = Vector3.Distance(transform.position, originalPosition);
        float returnTime = returnDistance / moveSpeed;
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;

        // Calculate direction to face while walking back
        Vector3 directionToOriginal = (originalPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToOriginal);

        // Start rotating towards the walking direction immediately
        while (elapsedTime < returnTime)
        {
            float t = elapsedTime / returnTime;

            // Move towards original position
            transform.position = Vector3.Lerp(startPosition, originalPosition, t);

            // Face the direction we're walking to (not backwards)
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, t * 3f); // Faster rotation

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;

        // Final rotation should be the original spawn rotation, not the walking direction
        yield return StartCoroutine(RotateToOriginalFacing());

        // Stop movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(false);
        }

        Debug.Log($"{monsterData.monsterName} returned to original position and rotation");
    }

    // New helper method to smoothly rotate to original facing after reaching position
    private IEnumerator RotateToOriginalFacing()
    {
        float rotationTime = 0.5f; // Time to rotate to final facing
        float elapsedTime = 0f;
        Quaternion startRotation = transform.rotation;

        while (elapsedTime < rotationTime)
        {
            float t = elapsedTime / rotationTime;
            transform.rotation = Quaternion.Lerp(startRotation, originalRotation, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = originalRotation;
    }

    private IEnumerator ExecuteActionSequence(MonsterAction action, Monster target)
    {
        // Trigger attack animation
        if (modelController != null)
        {
            modelController.TriggerAttackAnimation(action, target);
        }

        // Wait for animation windup
        yield return new WaitForSeconds(0.5f);

        // Consume energy
        currentEnergy -= action.energyCost;

        // Set cooldown
        if (action.cooldownTurns > 0)
        {
            actionCooldowns[action] = action.cooldownTurns;
        }

        // Handle damage timing based on attack type
        if (action.IsRangedAttack && target != null)
        {
            // For ranged attacks, wait for projectile to hit
            float projectileTime = 0.8f; // Match the travelTime in ModelController
            yield return new WaitForSeconds(projectileTime);
        }
        else if (action.IsMeleeAttack && target != null)
        {
            // For melee attacks, shorter delay since we're already close
            yield return new WaitForSeconds(0.3f);
        }

        // Execute action based on type
        switch (action.type)
        {
            case ActionType.Attack:
                ExecuteAttackAction(action, target);
                break;
            case ActionType.Buff:
                ExecuteBuffAction(action, target);
                break;
            case ActionType.Debuff:
                ExecuteDebuffAction(action, target);
                break;
            case ActionType.Heal:
                ExecuteHealAction(action, target);
                break;
        }

        // Apply buff/debuff effects
        ApplyBuffDebuffEffects(action, target);

        // Self-heal if specified
        if (action.healsUser)
        {
            Heal(action.healAmount);
        }

        // Final timing for impact effects
        yield return new WaitForSeconds(0.2f);

        Debug.Log($"{monsterData.monsterName} used {action.actionName}!");
    }

    private void ExecuteAttackAction(MonsterAction action, Monster target)
    {
        if (target == null) return;

        // Start multi-hit attack coroutine
        StartCoroutine(ExecuteMultiHitAttack(action, target));
    }

    private IEnumerator ExecuteMultiHitAttack(MonsterAction action, Monster target)
    {
        int totalHits = action.hitCount;

        for (int hitIndex = 0; hitIndex < totalHits; hitIndex++)
        {
            // Check if target is still alive before each hit
            if (target == null || !target.isAlive)
            {
                Debug.Log($"Target defeated after {hitIndex} hits!");
                yield break;
            }

            // Calculate damage for this hit
            int hitDamage = CalculateHitDamage(action, target, hitIndex, totalHits);

            // Check for critical hit per individual hit
            bool isCritical = Random.Range(0f, 1f) < action.criticalChance;
            if (isCritical)
            {
                hitDamage = Mathf.RoundToInt(hitDamage * action.criticalMultiplier);
                Debug.Log($"Critical Hit! (Hit {hitIndex + 1}/{totalHits})");
            }

            // Deal damage for this hit
            target.TakeDamage(hitDamage, isCritical);

            Debug.Log($"{monsterData.monsterName} hit {target.monsterData.monsterName} for {hitDamage} damage! (Hit {hitIndex + 1}/{totalHits})");

            // Wait between hits (except for the last hit)
            if (hitIndex < totalHits - 1)
            {
                yield return new WaitForSeconds(action.timeBetweenHits);
            }
        }

        // Show final elemental advantage message after all hits
        string advantageText = ElementalSystem.GetElementAdvantageText(monsterData.element, target.monsterData.element);
        if (!string.IsNullOrEmpty(advantageText))
        {
            Debug.Log(advantageText);
        }
    }

    // Timing-specific movement methods
    public IEnumerator MoveToTargetForTimingAttack(Monster target)
    {
        if (target == null) yield break;

        Vector3 targetPosition = target.transform.position;
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 meleePosition = targetPosition - direction * meleeAttackDistance;

        // Trigger movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(true);
        }

        float moveDistance = Vector3.Distance(transform.position, meleePosition);
        float moveTime = moveDistance / moveSpeed;
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;

        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;
            transform.position = Vector3.Lerp(startPosition, meleePosition, t);

            // Face target while moving
            transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = meleePosition;

        // Stop movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(false);
        }

        Debug.Log($"{monsterData.monsterName} moved to melee range after timing challenge");
    }

    public IEnumerator MoveToMultiTargetPositionForTiming(List<Monster> targets)
    {
        if (targets.Count == 0) yield break;

        // Calculate center position
        Vector3 centerPosition = Vector3.zero;
        int validTargets = 0;

        foreach (Monster target in targets)
        {
            if (target != null && target.isAlive)
            {
                centerPosition += target.transform.position;
                validTargets++;
            }
        }

        if (validTargets == 0) yield break;

        centerPosition /= validTargets;
        Vector3 direction = (centerPosition - transform.position).normalized;
        Vector3 attackPosition = centerPosition - direction * 2f;

        // Trigger movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(true);
        }

        float moveDistance = Vector3.Distance(transform.position, attackPosition);
        float moveTime = moveDistance / moveSpeed;
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;

        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;
            transform.position = Vector3.Lerp(startPosition, attackPosition, t);
            transform.LookAt(new Vector3(centerPosition.x, transform.position.y, centerPosition.z));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = attackPosition;

        // Stop movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(false);
        }

        Debug.Log($"{monsterData.monsterName} moved to multi-target position after timing challenge");
    }

    public IEnumerator ReturnToOriginalPositionAfterTiming()
    {
        // Trigger movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(true);
        }

        float moveDistance = Vector3.Distance(transform.position, originalPosition);
        float moveTime = moveDistance / moveSpeed;
        float elapsedTime = 0f;

        Vector3 startPosition = transform.position;

        // Calculate direction to face while walking back (same as normal ReturnToOriginalPosition)
        Vector3 directionToOriginal = (originalPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToOriginal);

        // Start rotating towards the walking direction immediately
        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;

            // Move towards original position
            transform.position = Vector3.Lerp(startPosition, originalPosition, t);

            // Face the direction we're walking to (not backwards)
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, t * 3f); // Faster rotation

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;

        // Final rotation should be the original spawn rotation, not the walking direction
        yield return StartCoroutine(RotateToOriginalFacing());

        // Stop movement animation
        if (modelController != null)
        {
            modelController.TriggerMovementAnimation(false);
        }

        Debug.Log($"{monsterData.monsterName} returned to original position after timing attack with correct rotation");
    }

    private int CalculateHitDamage(MonsterAction action, Monster target, int hitIndex, int totalHits)
    {
        // Calculate base damage
        int baseDamage = action.basePower;
        if (action.usesAttackStat)
        {
            baseDamage += currentATK;
        }

        // Divide damage if specified
        if (action.divideDamagePerHit && totalHits > 1)
        {
            baseDamage = Mathf.RoundToInt((float)baseDamage / totalHits);
            // Ensure minimum 1 damage per hit
            baseDamage = Mathf.Max(1, baseDamage);
        }

        // Apply elemental advantage
        float elementalMultiplier = ElementalSystem.GetElementalAdvantage(monsterData.element, target.monsterData.element);
        int finalDamage = Mathf.RoundToInt(baseDamage * elementalMultiplier);

        // Apply defense (unless ignored)
        if (!action.ignoresDefense)
        {
            finalDamage = Mathf.Max(1, finalDamage - target.currentDEF);
        }

        return finalDamage;
    }

    private void ExecuteHealAction(MonsterAction action, Monster target)
    {
        if (target == null) target = this; // Default to self

        // Calculate heal amount with attack stat support
        int healAmount = action.basePower;

        if (action.usesAttackStat)
        {
            healAmount += currentATK; // Add monster's attack to healing
            Debug.Log($"💚 {monsterData.monsterName} heals for {healAmount} (base: {action.basePower} + ATK: {currentATK})");
        }
        else
        {
            Debug.Log($"💚 {monsterData.monsterName} heals for {healAmount} (fixed amount)");
        }

        // Apply the healing
        target.Heal(healAmount);
    }

    private void ExecuteBuffAction(MonsterAction action, Monster target)
    {
        if (target == null) target = this; // Default to self

        // Buff actions apply positive buff/debuff effects
        ApplyBuffDebuffEffects(action, target);
    }

    private void ExecuteDebuffAction(MonsterAction action, Monster target)
    {
        if (target == null) return;

        // Debuff actions apply negative buff/debuff effects
        ApplyBuffDebuffEffects(action, target);
    }

    private void ApplyBuffDebuffEffects(MonsterAction action, Monster target)
    {
        foreach (var effect in action.buffDebuffEffects)
        {
            Monster effectTarget = action.applySelfEffects ? this : target;

            if (effectTarget != null)
            {
                effect.ApplyTo(effectTarget);
            }
        }
    }

    // ===== BUFF/DEBUFF SYSTEM METHODS =====

    /// <summary>
    /// Add a buff/debuff effect to this monster
    /// </summary>
    public void AddBuffDebuffEffect(BuffDebuffEffect effect)
    {
        if (effect == null) return;

        // Check if effect can stack or if it already exists
        if (!effect.canStack)
        {
            // Remove existing instance of this effect
            RemoveBuffDebuffEffect(effect);
        }
        else
        {
            // Check max stacks
            int currentStacks = activeBuffDebuffEffects.Count(e => e.effect == effect);
            if (currentStacks >= effect.maxStacks)
            {
                Debug.Log($"Maximum stacks ({effect.maxStacks}) reached for {effect.effectName}");
                return;
            }
        }

        // Add new active effect
        ActiveBuffDebuffEffect activeEffect = new ActiveBuffDebuffEffect
        {
            effect = effect,
            remainingTurns = effect.isPermanent ? -1 : effect.duration
        };

        activeBuffDebuffEffects.Add(activeEffect);
        ApplyBuffDebuffModifiers(effect);

        // ✅ NEW: Notify UI
        NotifyBuffDebuffChanged();
    }

    /// <summary>
    /// Remove a specific buff/debuff effect
    /// </summary>
    public void RemoveBuffDebuffEffect(BuffDebuffEffect effect)
    {
        var effectsToRemove = activeBuffDebuffEffects.Where(e => e.effect == effect).ToList();

        foreach (var activeEffect in effectsToRemove)
        {
            activeBuffDebuffEffects.Remove(activeEffect);

            // Reverse stat modifications
            ReverseBuffDebuffModifiers(effect);
        }

        NotifyBuffDebuffChanged();
    }

    /// <summary>
    /// Apply stat modifications from buff/debuff
    /// </summary>
    private void ApplyBuffDebuffModifiers(BuffDebuffEffect effect)
    {
        foreach (var modifier in effect.statModifiers)
        {
            int actualModifierValue = modifier.CalculateModifierValue(GetBaseStat(modifier.statType));

            switch (modifier.statType)
            {
                case StatType.Attack:
                    currentATK += actualModifierValue;
                    break;
                case StatType.Defense:
                    currentDEF += actualModifierValue;
                    break;
                case StatType.Speed:
                    currentSPD += actualModifierValue;
                    break;
                case StatType.HP:
                    if (actualModifierValue > 0)
                        Heal(actualModifierValue);
                    else
                        TakeDamage(-actualModifierValue);
                    break;
                case StatType.Energy:
                    currentEnergy = Mathf.Max(0, currentEnergy + actualModifierValue);
                    break;
            }

            Debug.Log($"📊 {monsterData.monsterName} {modifier.GetDisplayText()} applied");
        }
    }

    /// <summary>
    /// Reverse stat modifications from buff/debuff
    /// </summary>
    private void ReverseBuffDebuffModifiers(BuffDebuffEffect effect)
    {
        foreach (var modifier in effect.statModifiers)
        {
            int actualModifierValue = modifier.CalculateModifierValue(GetBaseStat(modifier.statType));

            switch (modifier.statType)
            {
                case StatType.Attack:
                    currentATK -= actualModifierValue;
                    break;
                case StatType.Defense:
                    currentDEF -= actualModifierValue;
                    break;
                case StatType.Speed:
                    currentSPD -= actualModifierValue;
                    break;
                    // HP and Energy modifications are not reversed as they're instant effects
            }
        }
    }

    /// <summary>
    /// Get base stat value for calculations
    /// </summary>
    private int GetBaseStat(StatType statType)
    {
        return statType switch
        {
            StatType.Attack => monsterData.baseATK,
            StatType.Defense => monsterData.baseDEF,
            StatType.Speed => monsterData.baseSPD,
            StatType.HP => monsterData.baseHP,
            StatType.Energy => monsterData.baseEnergy,
            _ => 0
        };
    }

    /// <summary>
    /// Check if monster has a specific effect active
    /// </summary>
    public bool HasEffect(BuffDebuffEffect effect)
    {
        return activeBuffDebuffEffects.Any(e => e.effect == effect);
    }

    /// <summary>
    /// Check if monster is prevented from acting (stun, sleep, etc.)
    /// </summary>
    public bool IsPreventedFromActing()
    {
        return activeBuffDebuffEffects.Any(e => e.effect.preventsAction);
    }

    // ===== DAMAGE AND HEALING =====

    public void TakeDamage(int damage, bool isCritical = false)
    {
        currentHP = Mathf.Max(0, currentHP - damage);

        if (monsterData != null)
        {
            Debug.Log($"{monsterData.monsterName} takes {damage} damage! HP: {currentHP}/{monsterData.baseHP}");
        }

        // Check for effects that clear on damage
        var effectsToClear = activeBuffDebuffEffects.Where(e => e.effect.clearsOnDamage).ToList();
        foreach (var effect in effectsToClear)
        {
            RemoveBuffDebuffEffect(effect.effect);
        }

        // Trigger hit animation and particle damage numbers
        if (modelController != null)
        {
            modelController.TriggerHitAnimation();
            modelController.OnMonsterHealthChanged();

            // Show particle damage numbers instead of text mesh
            modelController.ShowParticleDamageNumber(damage, isCritical);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        currentHP = Mathf.Min(monsterData.baseHP, currentHP + healAmount);

        Debug.Log($"{monsterData.monsterName} heals for {healAmount}! HP: {currentHP}/{monsterData.baseHP}");

        // Show healing particle effect
        if (modelController != null)
        {
            modelController.ShowHealNumber(healAmount);
            modelController.OnMonsterHealthChanged();
        }
    }

    private void Die()
    {
        isAlive = false;

        // Clear all buff/debuff effects on death
        activeBuffDebuffEffects.Clear();

        // Trigger death animation and effects
        if (modelController != null)
        {
            modelController.TriggerDeathAnimation();
        }

        if (monsterData != null)
        {
            Debug.Log($"{monsterData.monsterName} has been defeated!");
        }
    }


    // ===== TURN MANAGEMENT =====

    public void StartNewTurn()
    {
        hasTakenTurn = false;

        if (monsterData != null)
        {
            currentEnergy = Mathf.Min(monsterData.baseEnergy, currentEnergy + 25);
        }

        // Reduce action cooldowns
        var actions = actionCooldowns.Keys.ToList();
        foreach (var action in actions)
        {
            if (actionCooldowns[action] > 0)
            {
                actionCooldowns[action]--;
            }
        }

        // Process buff/debuff effects
        ProcessBuffDebuffEffects();
    }

    private void ProcessBuffDebuffEffects()
    {
        var effectsToRemove = new List<ActiveBuffDebuffEffect>();

        foreach (var activeEffect in activeBuffDebuffEffects)
        {
            var effect = activeEffect.effect;

            // Apply per-turn effects with percentage calculation
            if (effect.damagePerTurnPercent > 0)
            {
                // Calculate damage as percentage of MAX HP
                int percentageDamage = Mathf.RoundToInt(monsterData.baseHP * (effect.damagePerTurnPercent / 100f));
                TakeDamage(percentageDamage);
                Debug.Log($"{monsterData.monsterName} takes {percentageDamage} damage ({effect.damagePerTurnPercent:F1}% of max HP) from {effect.effectName}");
            }
            else if (effect.damagePerTurnFlat > 0)
            {
                // Fallback to flat damage
                TakeDamage(effect.damagePerTurnFlat);
                Debug.Log($"{monsterData.monsterName} takes {effect.damagePerTurnFlat} flat damage from {effect.effectName}");
            }

            if (effect.healPerTurnPercent > 0)
            {
                // Calculate healing as percentage of MAX HP
                int percentageHealing = Mathf.RoundToInt(monsterData.baseHP * (effect.healPerTurnPercent / 100f));
                Heal(percentageHealing);
                Debug.Log($"{monsterData.monsterName} heals {percentageHealing} HP ({effect.healPerTurnPercent:F1}% of max HP) from {effect.effectName}");
            }
            else if (effect.healPerTurnFlat > 0)
            {
                // Fallback to flat healing
                Heal(effect.healPerTurnFlat);
                Debug.Log($"{monsterData.monsterName} heals {effect.healPerTurnFlat} flat HP from {effect.effectName}");
            }

            // Reduce duration (skip if permanent)
            if (activeEffect.remainingTurns > 0)
            {
                activeEffect.remainingTurns--;
                if (activeEffect.remainingTurns <= 0)
                {
                    effectsToRemove.Add(activeEffect);
                }
            }
        }

        // Remove expired effects
        foreach (var effect in effectsToRemove)
        {
            RemoveBuffDebuffEffect(effect.effect);
        }
    }

    // ===== UTILITY METHODS =====

    // Helper method to get all usable actions
    public List<MonsterAction> GetUsableActions()
    {
        var actions = monsterData.GetAvailableActions();
        return actions.Where(action => CanUseAction(action)).ToList();
    }

    public void SetActiveVisual(bool isActive)
    {
        if (modelController != null)
        {
            modelController.SetActiveIndicator(isActive);
        }
    }
}

[System.Serializable]
public class ActiveBuffDebuffEffect
{
    public BuffDebuffEffect effect;
    public int remainingTurns; // -1 for permanent
}
