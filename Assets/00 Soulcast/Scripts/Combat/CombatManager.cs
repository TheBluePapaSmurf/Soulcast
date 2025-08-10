using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    [Header("Combat Settings")]
    public List<Monster> playerMonsters = new List<Monster>();
    public List<Monster> enemyMonsters = new List<Monster>();
    public DifficultySettings currentDifficulty;

    [Header("Combat State")]
    public bool combatActive = false;
    public Monster currentActiveMonster;
    public int currentTurn = 0;

    [Header("Combat Log")]
    public TMPro.TextMeshProUGUI combatLogText;
    private Queue<string> combatLog = new Queue<string>();
    private const int maxLogEntries = 5;

    // Turn management
    private bool isPlayerTurnPhase = true;
    private List<Monster> currentTurnOrder = new List<Monster>();
    private int currentMonsterIndex = 0;

    public static CombatManager Instance { get; private set; }

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

    [System.Obsolete]
    void Start()
    {
        if (currentDifficulty == null)
        {
            // Create a basic difficulty if none assigned
            currentDifficulty = ScriptableObject.CreateInstance<DifficultySettings>();
            currentDifficulty.difficultyName = "Normal";
            currentDifficulty.hpMultiplier = 1.0f;
            currentDifficulty.damageMultiplier = 1.0f;
            currentDifficulty.strategicThinkingChance = 50;
            currentDifficulty.targetPriorityChance = 40;
            currentDifficulty.energyManagementChance = 60;
        }

        // Wait for monsters to be spawned
        StartCoroutine(WaitForGameSetup());
    }

    [System.Obsolete]
    private System.Collections.IEnumerator WaitForGameSetup()
    {
        Debug.Log("CombatManager: Waiting for monsters to be spawned...");

        // Wait until monsters exist in the scene
        while (FindObjectsOfType<Monster>().Length == 0)
        {
            yield return new WaitForEndOfFrame();
        }

        // Wait one more frame to ensure all monsters are fully initialized
        yield return new WaitForEndOfFrame();

        Debug.Log("CombatManager: Monsters found! Starting combat...");
        InitializeCombat();
    }

    [System.Obsolete]
    public void InitializeCombat()
    {
        Debug.Log("CombatManager: InitializeCombat called");

        // Clear any existing references
        playerMonsters.Clear();
        enemyMonsters.Clear();

        // Find all monsters in the scene
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        Debug.Log($"Found {allMonsters.Length} monsters in scene");

        foreach (Monster monster in allMonsters)
        {
            if (monster.isPlayerControlled)
            {
                playerMonsters.Add(monster);
                ApplyPlayerDifficultyModifiers(monster);
                Debug.Log($"Added player monster: {monster.monsterData.monsterName} ({monster.monsterData.element})");
            }
            else
            {
                enemyMonsters.Add(monster);
                ApplyEnemyDifficultyModifiers(monster);
                Debug.Log($"Added enemy monster: {monster.monsterData.monsterName} ({monster.monsterData.element})");
            }
        }

        Debug.Log($"Final count - Players: {playerMonsters.Count}, Enemies: {enemyMonsters.Count}");

        // Only start combat if we have monsters on both sides
        if (playerMonsters.Count > 0 && enemyMonsters.Count > 0)
        {
            StartCombat();
        }
        else
        {
            Debug.LogError($"Cannot start combat! Players: {playerMonsters.Count}, Enemies: {enemyMonsters.Count}");
        }
    }

    // NEW: Add this method to CombatManager.cs
    public void OnTurnCompleted()
    {
        if (currentActiveMonster == null) return;

        Debug.Log($"{currentActiveMonster.monsterData.monsterName} has completed their turn");

        // Small delay to let any final effects play out
        StartCoroutine(DelayedTurnEnd());
    }

    private IEnumerator DelayedTurnEnd()
    {
        // Brief pause to let any final visual effects complete
        yield return new WaitForSeconds(0.2f);

        EndCurrentTurn();
    }

    private void ApplyEnemyDifficultyModifiers(Monster enemy)
    {
        if (currentDifficulty == null) return;

        enemy.currentHP = Mathf.RoundToInt(enemy.currentHP * currentDifficulty.hpMultiplier);
        enemy.currentATK = Mathf.RoundToInt(enemy.currentATK * currentDifficulty.damageMultiplier);
        enemy.currentSPD = Mathf.RoundToInt(enemy.currentSPD * currentDifficulty.speedMultiplier);
        enemy.currentEnergy = Mathf.RoundToInt(enemy.currentEnergy * currentDifficulty.energyMultiplier);

        AddCombatLogEntry($"{enemy.monsterData.monsterName} enhanced by difficulty");
    }

    private void ApplyPlayerDifficultyModifiers(Monster player)
    {
        if (currentDifficulty == null) return;
        player.currentEnergy = Mathf.RoundToInt(player.currentEnergy * currentDifficulty.playerEnergyMultiplier);
    }

    public void StartCombat()
    {
        combatActive = true;
        StartNewRound();
        AddCombatLogEntry($"Combat started on {currentDifficulty.difficultyName} difficulty!");
        Debug.Log($"Combat Started on {currentDifficulty.difficultyName} difficulty!");
    }

    private void StartNewRound()
    {
        currentTurn++;
        isPlayerTurnPhase = true;
        currentMonsterIndex = 0;

        Debug.Log($"=== ROUND {currentTurn} - Player Phase ===");
        AddCombatLogEntry($"=== ROUND {currentTurn} ===");

        // Restore energy for all alive monsters
        foreach (Monster monster in playerMonsters.Where(m => m.isAlive))
        {
            monster.StartNewTurn();
        }
        foreach (Monster monster in enemyMonsters.Where(m => m.isAlive))
        {
            monster.StartNewTurn();
        }

        StartPlayerPhase();
    }

    private void StartPlayerPhase()
    {
        isPlayerTurnPhase = true;

        // Get alive player monsters with stable sorting
        var alivePlayerMonsters = playerMonsters.Where(m => m.isAlive).ToList();

        // Create stable sort: First by SPD (descending), then by original list order for ties
        currentTurnOrder.Clear();
        currentTurnOrder.AddRange(alivePlayerMonsters
            .Select((monster, index) => new { monster, originalIndex = playerMonsters.IndexOf(monster) })
            .OrderByDescending(x => x.monster.currentSPD)
            .ThenBy(x => x.originalIndex)
            .Select(x => x.monster));

        currentMonsterIndex = 0;

        Debug.Log("=== PLAYER PHASE ===");
        for (int i = 0; i < currentTurnOrder.Count; i++)
        {
            Debug.Log($"{i + 1}. {currentTurnOrder[i].monsterData.monsterName} - SPD: {currentTurnOrder[i].currentSPD}");
        }

        if (currentTurnOrder.Count == 0)
        {
            Debug.LogWarning("No alive player monsters! Starting enemy phase.");
            StartEnemyPhase();
            return;
        }

        StartNextTurn();
    }

    private void StartEnemyPhase()
    {
        isPlayerTurnPhase = false;

        // Get alive enemy monsters with stable sorting
        var aliveEnemyMonsters = enemyMonsters.Where(m => m.isAlive).ToList();

        currentTurnOrder.Clear();
        currentTurnOrder.AddRange(aliveEnemyMonsters
            .Select((monster, index) => new { monster, originalIndex = enemyMonsters.IndexOf(monster) })
            .OrderByDescending(x => x.monster.currentSPD)
            .ThenBy(x => x.originalIndex)
            .Select(x => x.monster));

        currentMonsterIndex = 0;

        Debug.Log("=== ENEMY PHASE ===");
        AddCombatLogEntry("Enemy Phase");
        for (int i = 0; i < currentTurnOrder.Count; i++)
        {
            Debug.Log($"{i + 1}. {currentTurnOrder[i].monsterData.monsterName} - SPD: {currentTurnOrder[i].currentSPD}");
        }

        // Hide UI during enemy phase
        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.HidePlayerTurn();
        }

        if (currentTurnOrder.Count == 0)
        {
            Debug.LogWarning("No alive enemy monsters! Starting new round.");
            StartNewRound();
            return;
        }

        StartNextTurn();
    }

    public void StartNextTurn()
    {
        if (!combatActive) return;

        // Check win conditions
        if (CheckCombatEnd()) return;

        if (currentTurnOrder.Count == 0)
        {
            Debug.LogWarning("Turn order is empty! Restarting phase.");
            if (isPlayerTurnPhase)
                StartPlayerPhase();
            else
                StartEnemyPhase();
            return;
        }

        // Check if current phase is complete
        if (currentMonsterIndex >= currentTurnOrder.Count)
        {
            Debug.Log($"Phase complete. Monster index: {currentMonsterIndex}, Turn order count: {currentTurnOrder.Count}");

            if (isPlayerTurnPhase)
            {
                Debug.Log("All players have acted. Starting enemy phase.");
                StartEnemyPhase();
                return;
            }
            else
            {
                Debug.Log("All enemies have acted. Starting new round.");
                StartNewRound();
                return;
            }
        }

        // Get current monster
        currentActiveMonster = currentTurnOrder[currentMonsterIndex];

        foreach (Monster monster in playerMonsters.Concat(enemyMonsters))
        {
            monster.SetActiveVisual(false);
        }

        // Set current monster as active
        if (currentActiveMonster != null)
        {
            currentActiveMonster.SetActiveVisual(true);
        }

        string phase = isPlayerTurnPhase ? "Player" : "Enemy";
        Debug.Log($">>> {currentActiveMonster.monsterData.monsterName}'s turn! ({phase} Phase) [Turn {currentMonsterIndex + 1}/{currentTurnOrder.Count}]");

        // Show UI for player monsters, execute AI for enemies
        if (isPlayerTurnPhase)
        {
            if (CombatUI.Instance != null)
            {
                Debug.Log($"Showing UI for {currentActiveMonster.monsterData.monsterName}");
                CombatUI.Instance.ShowPlayerTurn(currentActiveMonster);
            }
            else
            {
                Debug.LogError("CombatUI.Instance is null!");
            }
        }
        else
        {
            ExecuteEnemyAI();
        }
    }

    public void EndCurrentTurn()
    {
        Debug.Log($"{currentActiveMonster.monsterData.monsterName} ends their turn. Moving from index {currentMonsterIndex} to {currentMonsterIndex + 1}");
        currentMonsterIndex++;
        StartNextTurn();
    }

    private void ExecuteEnemyAI()
    {
        Debug.Log($"{currentActiveMonster.monsterData.monsterName} is thinking...");

        if (currentActiveMonster.monsterData == null)
        {
            Debug.LogError($"Enemy monster {currentActiveMonster.gameObject.name} has no MonsterData!");
            Invoke("EndCurrentTurn", 2f);
            return;
        }

        // Get usable actions
        List<MonsterAction> usableActions = currentActiveMonster.GetUsableActions();

        if (usableActions.Count == 0)
        {
            Debug.Log($"{currentActiveMonster.monsterData.monsterName} has no usable actions!");
            AddCombatLogEntry($"{currentActiveMonster.monsterData.monsterName} skips turn");
            Invoke("EndCurrentTurn", 2f);
            return;
        }

        // Make AI decision
        AIDecision decision = MakeAIDecision(usableActions);

        if (decision.action != null)
        {
            Debug.Log($"AI Decision: {currentActiveMonster.monsterData.monsterName} uses {decision.action.actionName} (Reasoning: {decision.reasoning})");
            AddCombatLogEntry($"{currentActiveMonster.monsterData.monsterName} uses {decision.action.actionName}");

            // Execute the action based on target type
            ExecuteAIAction(decision.action, decision.targets);
        }
        else
        {
            Debug.LogWarning($"AI failed to make a valid decision for {currentActiveMonster.monsterData.monsterName}");
            AddCombatLogEntry($"{currentActiveMonster.monsterData.monsterName} fails to act");
            // Only end turn immediately if no action was taken
            Invoke("EndCurrentTurn", 2f);
        }
    }

    private void ExecuteAIAction(MonsterAction action, List<Monster> targets)
    {
        switch (action.targetType)
        {
            case TargetType.Single:
                if (targets.Count > 0)
                    currentActiveMonster.UseAction(action, targets[0]);
                break;

            case TargetType.Self:
                currentActiveMonster.UseAction(action, currentActiveMonster);
                break;

            case TargetType.AllEnemies:
                foreach (Monster target in targets)
                {
                    currentActiveMonster.UseAction(action, target);
                }
                break;

            case TargetType.AllAllies:
                foreach (Monster target in targets)
                {
                    currentActiveMonster.UseAction(action, target);
                }
                break;

            case TargetType.Random:
                if (targets.Count > 0)
                {
                    Monster randomTarget = targets[Random.Range(0, targets.Count)];
                    currentActiveMonster.UseAction(action, randomTarget);
                }
                break;
        }
    }

    private AIDecision MakeAIDecision(List<MonsterAction> usableActions)
    {
        AIDecision decision = new AIDecision();
        bool useStrategy = Random.Range(0, 100) < currentDifficulty.strategicThinkingChance;

        if (useStrategy)
        {
            decision = MakeStrategicDecision(usableActions);
        }
        else
        {
            decision = MakeRandomDecision(usableActions);
        }
        return decision;
    }

    private AIDecision MakeStrategicDecision(List<MonsterAction> usableActions)
    {
        AIDecision decision = new AIDecision();

        // 1. Healing priority
        float healthPercent = (float)currentActiveMonster.currentHP / currentActiveMonster.monsterData.baseHP;
        if (healthPercent < 0.3f)
        {
            var healingActions = usableActions.Where(a => a.type == ActionType.Heal).ToList();
            if (healingActions.Count > 0)
            {
                decision.action = healingActions.OrderByDescending(a => a.basePower).First();
                decision.targets = new List<Monster> { currentActiveMonster };
                decision.reasoning = "Low health - healing";
                return decision;
            }
        }

        // 2. Ultimate attacks when available
        if (currentTurn >= 2)
        {
            var ultimateActions = usableActions.Where(a => a.category == ActionCategory.Ultimate).ToList();
            if (ultimateActions.Count > 0 && Random.Range(0, 100) < 30)
            {
                decision.action = ultimateActions[Random.Range(0, ultimateActions.Count)];
                decision.targets = GetTargetsForAction(decision.action);
                decision.reasoning = "Using ultimate attack";
                return decision;
            }
        }

        // 3. Buff early in combat
        if (currentTurn < 3)
        {
            var buffActions = usableActions.Where(a => a.type == ActionType.Buff).ToList();
            if (buffActions.Count > 0 && Random.Range(0, 100) < 40)
            {
                decision.action = buffActions[Random.Range(0, buffActions.Count)];
                decision.targets = GetTargetsForAction(decision.action);
                decision.reasoning = "Early game buff";
                return decision;
            }
        }

        // 4. Elemental advantage attacks
        var attackActions = usableActions.Where(a => a.type == ActionType.Attack).ToList();
        if (attackActions.Count > 0)
        {
            MonsterAction bestAttack = null;
            float bestAdvantage = 0f;

            foreach (var attack in attackActions)
            {
                var targets = GetTargetsForAction(attack);
                if (targets.Count > 0)
                {
                    float advantage = ElementalSystem.GetElementalAdvantage(
                        currentActiveMonster.monsterData.element,
                        targets[0].monsterData.element);

                    if (advantage > bestAdvantage)
                    {
                        bestAdvantage = advantage;
                        bestAttack = attack;
                    }
                }
            }

            if (bestAttack != null)
            {
                decision.action = bestAttack;
                decision.targets = GetTargetsForAction(bestAttack);
                decision.reasoning = "Elemental advantage attack";
                return decision;
            }
        }

        // 5. Fallback to any attack
        if (attackActions.Count > 0)
        {
            decision.action = attackActions.OrderByDescending(a => a.basePower).First();
            decision.targets = GetTargetsForAction(decision.action);
            decision.reasoning = "Basic attack";
        }

        return decision;
    }

    private AIDecision MakeRandomDecision(List<MonsterAction> usableActions)
    {
        AIDecision decision = new AIDecision();
        decision.action = usableActions[Random.Range(0, usableActions.Count)];
        decision.targets = GetTargetsForAction(decision.action);
        decision.reasoning = "Random choice";
        return decision;
    }

    private List<Monster> GetTargetsForAction(MonsterAction action)
    {
        List<Monster> targets = new List<Monster>();

        switch (action.targetType)
        {
            case TargetType.Single:
                targets.AddRange(ChooseStrategicTargets(1));
                break;

            case TargetType.Self:
                targets.Add(currentActiveMonster);
                break;

            case TargetType.AllEnemies:
                targets.AddRange(playerMonsters.Where(m => m.isAlive));
                break;

            case TargetType.AllAllies:
                targets.AddRange(enemyMonsters.Where(m => m.isAlive));
                break;

            case TargetType.Random:
                var allPossibleTargets = playerMonsters.Where(m => m.isAlive).ToList();
                if (allPossibleTargets.Count > 0)
                    targets.Add(allPossibleTargets[Random.Range(0, allPossibleTargets.Count)]);
                break;
        }

        return targets;
    }

    private List<Monster> ChooseStrategicTargets(int count)
    {
        var alivePlayerMonsters = playerMonsters.Where(m => m.isAlive).ToList();
        if (alivePlayerMonsters.Count == 0) return new List<Monster>();

        // Target low HP enemies first
        var lowHpTargets = alivePlayerMonsters.Where(m =>
            (float)m.currentHP / m.monsterData.baseHP < 0.4f).ToList();

        if (lowHpTargets.Count > 0)
        {
            return lowHpTargets.OrderBy(m => m.currentHP).Take(count).ToList();
        }

        // Fallback to random
        return alivePlayerMonsters.OrderBy(x => Random.value).Take(count).ToList();
    }

    public void SetDifficulty(DifficultySettings difficulty)
    {
        currentDifficulty = difficulty;
        Debug.Log($"Difficulty set to: {difficulty.difficultyName}");
    }

    private bool CheckCombatEnd()
    {
        bool playersAlive = playerMonsters.Any(m => m.isAlive);
        bool enemiesAlive = enemyMonsters.Any(m => m.isAlive);

        if (!playersAlive)
        {
            Debug.Log("Game Over! All player monsters defeated.");
            AddCombatLogEntry("DEFEAT! All players defeated.");
            EndCombat(false);
            return true;
        }

        if (!enemiesAlive)
        {
            Debug.Log("Victory! All enemy monsters defeated.");
            AddCombatLogEntry("VICTORY! All enemies defeated.");
            HandleVictory();
            EndCombat(true);
            return true;
        }

        return false;
    }

    private void EndCombat(bool playerWon)
    {
        combatActive = false;
        currentActiveMonster = null;

        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.HidePlayerTurn();
        }

        if (playerWon)
        {
            Debug.Log("Players win!");
        }
        else
        {
            Debug.Log("Players lose!");
        }
    }

    private void HandleVictory()
    {
        Debug.Log("🎉 === BATTLE VICTORY === 🎉");

        // Get battle data for rewards and progression
        if (BattleDataManager.Instance != null && BattleDataManager.Instance.HasValidBattleData())
        {
            var battleData = BattleDataManager.Instance.GetCurrentBattleData();

            // Give rewards
            GiveBattleRewards(battleData);

            // ✅ NEW: Update progression using BattleProgressionManager
            if (BattleProgressionManager.Instance != null)
            {
                BattleProgressionManager.Instance.CompleteBattle(
                    battleData.regionId,
                    battleData.levelId,
                    battleData.battleSequenceId,
                    3, // Stars earned (you can make this dynamic)
                    Time.time // Completion time
                );
            }
        }

        // Show victory UI and then return to world map
        StartCoroutine(ShowVictoryAndReturn());
    }

    private void GiveBattleRewards(BattleSetupData battleData)
    {
        var combatTemplate = battleData.combatTemplate;
        if (combatTemplate?.rewards == null) return;

        // Generate rewards using the new system
        var rewardResult = combatTemplate.rewards.GenerateRewards(
            battleData.regionId,
            battleData.levelId,
            battleData.battleSequenceId
        );

        Debug.Log($"💰 Soul Coins Earned: {rewardResult.soulCoinsEarned:N0}");
        Debug.Log($"🎁 Runes Earned: {rewardResult.runesEarned.Count}");

        foreach (var rune in rewardResult.runesEarned)
        {
            Debug.Log($"  - {rune.rarity} {rune.runeName}");
        }

        // Add rewards to player inventory
        AddRewardsToPlayer(rewardResult);
    }

    // ✅ NEW: Add rewards to player systems
    private void AddRewardsToPlayer(CombatResult rewardResult)
    {
        // Add soul coins
        int currentCoins = PlayerPrefs.GetInt("PlayerSoulCoins", 0);
        PlayerPrefs.SetInt("PlayerSoulCoins", currentCoins + rewardResult.soulCoinsEarned);

        // Add runes to inventory
        if (PlayerInventory.Instance != null)
        {
            foreach (var rune in rewardResult.runesEarned)
            {
                PlayerInventory.Instance.AddRune(rune);
            }
        }

        PlayerPrefs.Save();
    }

    private void MarkBattleAsCompleted(BattleSetupData battleData)
    {
        string battleKey = $"Region_{battleData.regionId}_Level_{battleData.levelId}_Battle_{battleData.battleSequenceId}";
        PlayerPrefs.SetInt($"{battleKey}_Completed", 1);
        PlayerPrefs.SetInt($"{battleKey}_Stars", 3); // Default to 3 stars for now
        PlayerPrefs.Save();

        Debug.Log($"✅ Battle marked as completed: {battleKey}");
    }

    // ✅ NEW: Show victory UI and return to world map
    private IEnumerator ShowVictoryAndReturn()
    {
        // Show victory UI for a few seconds
        // You can customize this to show actual victory screen
        Debug.Log("🏆 Showing victory screen...");

        yield return new WaitForSeconds(3f);

        // Return to world map
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ReturnToWorldMapAfterBattle();
        }
    }

    private CollectedMonster FindMatchingCollectedMonster(Monster battleMonster)
    {
        if (battleMonster?.monsterData == null) return null;

        // Find first monster of same type - you can improve this matching logic
        return PlayerInventory.Instance?.GetAllMonsters()
            ?.FirstOrDefault(cm => cm.monsterData == battleMonster.monsterData);
    }

    public void PlayerUseAction(MonsterAction action, Monster target)
    {
        if (currentActiveMonster != null && currentActiveMonster.isPlayerControlled)
        {
            AddCombatLogEntry($"{currentActiveMonster.monsterData.monsterName} uses {action.actionName}");

            // Apply energy cost and cooldown
            currentActiveMonster.currentEnergy -= action.energyCost;
            if (action.cooldownTurns > 0)
            {
                currentActiveMonster.actionCooldowns[action] = action.cooldownTurns;
            }

            // Start timing-based attack
            StartCoroutine(ExecuteTimingBasedAttack(currentActiveMonster, action, new List<Monster> { target }));
        }
    }

    public void PlayerUseActionOnMultipleTargets(MonsterAction action, List<Monster> targets)
    {
        if (currentActiveMonster != null && currentActiveMonster.isPlayerControlled)
        {
            AddCombatLogEntry($"{currentActiveMonster.monsterData.monsterName} uses {action.actionName} on multiple targets");

            // Apply energy cost and cooldown
            currentActiveMonster.currentEnergy -= action.energyCost;
            if (action.cooldownTurns > 0)
            {
                currentActiveMonster.actionCooldowns[action] = action.cooldownTurns;
            }

            // Start timing-based attack for multiple targets
            StartCoroutine(ExecuteTimingBasedAttack(currentActiveMonster, action, targets));
        }
    }

    // ✅ FIX: Camera gaat terug naar overview DIRECT na timing UI
    public IEnumerator ExecuteTimingBasedAttack(Monster attacker, MonsterAction action, List<Monster> targets)
    {
        Debug.Log($"=== Starting Timing-Based Attack: {action.actionName} with {action.hitCount} hits ===");

        // Check SlowMotionManager
        if (SlowMotionManager.Instance == null)
        {
            Debug.LogError("SlowMotionManager.Instance is null!");
            yield break;
        }

        // 1. Start slow motion
        SlowMotionManager.Instance.ActivateSlowMotion();

        // ✅ EERST camera transitie naar attacking monster (en wacht tot voltooid)
        if (DynamicCombatCamera.Instance != null)
        {
            Debug.Log($"Starting camera transition to attacking monster: {attacker.monsterData.monsterName}");

            // Start smooth transition naar attacking monster
            DynamicCombatCamera.Instance.FocusOnMonster(attacker);

            // ✅ WACHT tot camera transitie voltooid is
            yield return new WaitForSeconds(0.5f);

            Debug.Log("Camera transition completed - ready to start timing challenge");
        }

        // 2. Start multi-hit timing challenge
        TimingCircleManager.Instance.StartMultiHitTimingChallenge(action.hitCount);

        // 3. Wait for all timing results
        List<TimingCircle.TimingResult> allResults = null;
        bool timingComplete = false;

        System.Action<List<TimingCircle.TimingResult>> onAllTimingComplete = (results) => {
            allResults = results;
            timingComplete = true;
        };

        TimingCircleManager.OnMultiTimingComplete += onAllTimingComplete;
        yield return new WaitUntil(() => timingComplete);
        TimingCircleManager.OnMultiTimingComplete -= onAllTimingComplete;

        // 4. Stop slow motion
        SlowMotionManager.Instance.DeactivateSlowMotion();

        // 5. Hide timing UI
        TimingCircleManager.Instance.HideTimingUI();

        // ✅ FIX: Camera gaat DIRECT naar overview na timing UI (voor animaties)
        if (DynamicCombatCamera.Instance != null)
        {
            DynamicCombatCamera.Instance.SetOverviewMode();
            Debug.Log("Camera returned to overview mode after timing challenge - ready for attack animations");

            // Kleine pause om camera transitie te laten voltooien
            yield return new WaitForSeconds(0.3f);
        }

        Debug.Log("Starting attack animations from overview perspective...");

        // 6. Voor melee attacks: beweeg naar target(s)
        if (action.IsMeleeAttack && targets.Count > 0)
        {
            if (targets.Count == 1)
            {
                yield return StartCoroutine(attacker.MoveToTargetForTimingAttack(targets[0]));
            }
            else
            {
                yield return StartCoroutine(attacker.MoveToMultiTargetPositionForTiming(targets));
            }
        }

        // 7. Start attack animatie
        if (attacker.modelController != null)
        {
            Monster firstTarget = targets.FirstOrDefault(t => t != null && t.isAlive);
            if (firstTarget != null)
            {
                attacker.modelController.TriggerAttackAnimation(action, firstTarget);
            }
        }

        // 8. Wait for animation windup
        yield return new WaitForSeconds(0.5f);

        // 9. Wait for damage timing based on attack type
        if (action.IsRangedAttack)
        {
            yield return new WaitForSeconds(0.8f); // Projectile travel time
        }
        else if (action.IsMeleeAttack)
        {
            yield return new WaitForSeconds(0.3f); // Melee impact delay
        }

        // 10. Execute attack with timing results
        yield return ExecuteAttackWithMultipleTimingResults(attacker, action, targets, allResults);

        // 11. Voor melee attacks: return to original position
        if (action.IsMeleeAttack)
        {
            yield return StartCoroutine(attacker.ReturnToOriginalPositionAfterTiming());
        }

        // ✅ 12. Complete turn
        OnTurnCompleted();
    }


    // ✅ FIX: Verwijder de OnTurnCompleted call uit deze methode
    public IEnumerator ExecuteAttackWithMultipleTimingResults(Monster attacker, MonsterAction action, List<Monster> targets, List<TimingCircle.TimingResult> timingResults)
    {
        // Calculate overall performance
        int perfectHits = timingResults.FindAll(r => r == TimingCircle.TimingResult.Perfect).Count;
        int totalHits = timingResults.Count;

        Debug.Log($"Timing Results: {perfectHits}/{totalHits} Perfect hits");
        AddCombatLogEntry($"Timing: {perfectHits}/{totalHits} Perfect!");

        // Execute attacks on each target
        foreach (Monster target in targets)
        {
            if (target != null && target.isAlive)
            {
                yield return StartCoroutine(ExecuteMultiTimingAttack(action, target, timingResults));
            }
        }

        // ✅ REMOVED: OnTurnCompleted(); - Deze werd dubbel aangeroepen!
    }

    // Voeg deze methode toe aan je CombatManager klasse
    // ✅ FIX: Ook in ExecuteAttackWithTiming de dubbele call verwijderen
    public IEnumerator ExecuteAttackWithTiming(Monster attacker, MonsterAction action, List<Monster> targets, TimingCircle.TimingResult timingResult)
    {
        // Bereken damage multiplier gebaseerd op timing
        float damageMultiplier = GetTimingMultiplier(timingResult);

        // Log timing result
        string timingText = GetTimingResultText(timingResult);
        AddCombatLogEntry($"{timingText}! Damage: {(int)(damageMultiplier * 100)}%");

        // Voor multi-hit attacks: alle hits gebruiken dezelfde timing multiplier
        foreach (Monster target in targets)
        {
            if (target != null && target.isAlive)
            {
                yield return StartCoroutine(ExecuteTimingBasedMultiHit(action, target, damageMultiplier));
            }
        }

        // ✅ REMOVED: OnTurnCompleted(); - Deze methode wordt waarschijnlijk al vanuit ExecuteTimingBasedAttack aangeroepen
    }


    private IEnumerator ExecuteMultiTimingAttack(MonsterAction action, Monster target, List<TimingCircle.TimingResult> timingResults)
    {
        int totalHits = action.hitCount;

        for (int hitIndex = 0; hitIndex < totalHits; hitIndex++)
        {
            // Check if target is still alive
            if (target == null || !target.isAlive)
            {
                Debug.Log($"Target defeated after {hitIndex} hits!");
                yield break;
            }

            // Get timing result for this specific hit ✅ UPDATED
            TimingCircle.TimingResult hitTiming = hitIndex < timingResults.Count ?
                timingResults[hitIndex] : TimingCircle.TimingResult.Miss;

            // Calculate damage with individual timing
            float timingMultiplier = GetTimingMultiplier(hitTiming);
            int hitDamage = CalculateTimingBasedHitDamage(action, target, hitIndex, totalHits, timingMultiplier);

            // Check for critical hit
            bool isCritical = Random.Range(0f, 1f) < action.criticalChance;
            if (isCritical)
            {
                hitDamage = Mathf.RoundToInt(hitDamage * action.criticalMultiplier);
            }

            // Deal damage
            target.TakeDamage(hitDamage, isCritical);

            string timingText = hitTiming == TimingCircle.TimingResult.Perfect ? "PERFECT" : "MISS";
            Debug.Log($"Hit {hitIndex + 1}: {timingText} - {hitDamage} damage to {target.monsterData.monsterName}");

            // Wait between hits
            if (hitIndex < totalHits - 1)
            {
                yield return new WaitForSeconds(action.timeBetweenHits);
            }
        }
    }

    private IEnumerator ExecuteTimingBasedMultiHit(MonsterAction action, Monster target, float damageMultiplier)
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

            // Calculate damage for this hit with timing multiplier
            int hitDamage = CalculateTimingBasedHitDamage(action, target, hitIndex, totalHits, damageMultiplier);

            // Check for critical hit per individual hit
            bool isCritical = Random.Range(0f, 1f) < action.criticalChance;
            if (isCritical)
            {
                hitDamage = Mathf.RoundToInt(hitDamage * action.criticalMultiplier);
            }

            // Deal damage for this hit
            target.TakeDamage(hitDamage, isCritical);

            Debug.Log($"Timing hit {hitIndex + 1}/{totalHits}: {hitDamage} damage to {target.monsterData.monsterName}");

            // Wait between hits
            if (hitIndex < totalHits - 1)
            {
                yield return new WaitForSeconds(action.timeBetweenHits);
            }
        }
    }

    private int CalculateTimingBasedHitDamage(MonsterAction action, Monster target, int hitIndex, int totalHits, float timingMultiplier)
    {
        // Calculate base damage
        int baseDamage = action.basePower;
        if (action.usesAttackStat)
        {
            baseDamage += currentActiveMonster.currentATK;
        }

        // Apply timing multiplier EERST
        baseDamage = Mathf.RoundToInt(baseDamage * timingMultiplier);

        // Divide damage if specified (na timing multiplier)
        if (action.divideDamagePerHit && totalHits > 1)
        {
            baseDamage = Mathf.RoundToInt((float)baseDamage / totalHits);
            baseDamage = Mathf.Max(1, baseDamage);
        }

        // Apply elemental advantage
        float elementalMultiplier = ElementalSystem.GetElementalAdvantage(currentActiveMonster.monsterData.element, target.monsterData.element);
        int finalDamage = Mathf.RoundToInt(baseDamage * elementalMultiplier);

        // Apply defense
        if (!action.ignoresDefense)
        {
            finalDamage = Mathf.Max(1, finalDamage - target.currentDEF);
        }

        return finalDamage;
    }

    float GetTimingMultiplier(TimingCircle.TimingResult result)
    {
        switch (result)
        {
            case TimingCircle.TimingResult.Perfect: return 1.5f; // 150% damage
            case TimingCircle.TimingResult.Miss: return 0.8f;    // 80% damage
            default: return 1.0f;
        }
    }

    string GetTimingResultText(TimingCircle.TimingResult result)
    {
        switch (result)
        {
            case TimingCircle.TimingResult.Perfect: return "PERFECT HIT";
            case TimingCircle.TimingResult.Miss: return "Missed Timing";
            default: return "Hit";
        }
    }


    private IEnumerator ExecuteMultiTargetActionSequence(MonsterAction action, List<Monster> targets)
    {
        Debug.Log($"Starting multi-target sequence for {action.actionName} on {targets.Count} targets");

        // FOR MELEE ATTACKS: Move to center of targets first
        if (action.IsMeleeAttack && targets.Count > 0)
        {
            yield return StartCoroutine(MoveToMultiTargetPosition(targets));
        }

        // TRIGGER THE ATTACK ANIMATION
        if (currentActiveMonster.modelController != null)
        {
            // For multi-target, we trigger animation with the first target for direction
            Monster firstTarget = targets.FirstOrDefault(t => t != null && t.isAlive);
            if (firstTarget != null)
            {
                currentActiveMonster.modelController.TriggerAttackAnimation(action, firstTarget);
            }
        }

        // Wait for animation windup
        yield return new WaitForSeconds(0.5f);

        // Track how many attack sequences are still running
        int activeAttacks = 0;

        // Execute attack on each target with delays
        foreach (Monster target in targets)
        {
            if (target != null && target.isAlive)
            {
                activeAttacks++;
                float delay = targets.IndexOf(target) * 0.2f;
                StartCoroutine(ExecuteDelayedMultiTargetAttack(action, target, delay, () => {
                    activeAttacks--;
                }));
            }
        }

        // Wait for all attacks to complete
        while (activeAttacks > 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // FOR MELEE ATTACKS: Return to original position
        if (action.IsMeleeAttack)
        {
            yield return StartCoroutine(ReturnToOriginalPosition());
        }

        // Wait a bit longer for visual effects to finish
        yield return new WaitForSeconds(0.5f);

        // Mark turn as completed and end the turn
        currentActiveMonster.hasTakenTurn = true;

        Debug.Log($"{currentActiveMonster.monsterData.monsterName} completed multi-target turn");

        // End the turn automatically
        OnTurnCompleted();
    }

    private IEnumerator MoveToMultiTargetPosition(List<Monster> targets)
    {
        if (currentActiveMonster == null || targets.Count == 0) yield break;

        // Calculate center position of all targets
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

        // Move slightly back from center for better attack position
        Vector3 direction = (centerPosition - currentActiveMonster.transform.position).normalized;
        Vector3 attackPosition = centerPosition - direction * 2f; // 2 units back from center

        // Trigger movement animation
        if (currentActiveMonster.modelController != null)
        {
            currentActiveMonster.modelController.TriggerMovementAnimation(true);
        }

        // Move to attack position
        float moveDistance = Vector3.Distance(currentActiveMonster.transform.position, attackPosition);
        float moveTime = moveDistance / currentActiveMonster.moveSpeed;
        float elapsedTime = 0f;

        Vector3 startPosition = currentActiveMonster.transform.position;

        Debug.Log($"{currentActiveMonster.monsterData.monsterName} moving to multi-target attack position");

        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;
            currentActiveMonster.transform.position = Vector3.Lerp(startPosition, attackPosition, t);

            // Face the center while moving
            currentActiveMonster.transform.LookAt(new Vector3(centerPosition.x, currentActiveMonster.transform.position.y, centerPosition.z));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentActiveMonster.transform.position = attackPosition;

        // Stop movement animation
        if (currentActiveMonster.modelController != null)
        {
            currentActiveMonster.modelController.TriggerMovementAnimation(false);
        }

        Debug.Log($"{currentActiveMonster.monsterData.monsterName} reached multi-target attack position");
    }

    public IEnumerator ReturnToOriginalPosition()
    {
        if (currentActiveMonster == null) yield break;

        Vector3 originalPosition = currentActiveMonster.originalPosition;

        // Trigger movement animation
        if (currentActiveMonster.modelController != null)
        {
            currentActiveMonster.modelController.TriggerMovementAnimation(true);
        }

        float moveDistance = Vector3.Distance(currentActiveMonster.transform.position, originalPosition);
        float moveTime = moveDistance / currentActiveMonster.moveSpeed;
        float elapsedTime = 0f;

        Vector3 startPosition = currentActiveMonster.transform.position;

        Debug.Log($"{currentActiveMonster.monsterData.monsterName} returning to original position");

        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;
            currentActiveMonster.transform.position = Vector3.Lerp(startPosition, originalPosition, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentActiveMonster.transform.position = originalPosition;

        // Stop movement animation and face original direction
        if (currentActiveMonster.modelController != null)
        {
            currentActiveMonster.modelController.TriggerMovementAnimation(false);
        }

        // Face original rotation
        currentActiveMonster.transform.rotation = currentActiveMonster.originalRotation;

        Debug.Log($"{currentActiveMonster.monsterData.monsterName} returned to original position");
    }

    private IEnumerator ExecuteDelayedMultiTargetAttack(MonsterAction action, Monster target, float delay, System.Action onComplete)
    {
        yield return new WaitForSeconds(delay);

        // Handle damage timing based on attack type (like in the original ExecuteActionSequence)
        if (action.IsRangedAttack && target != null)
        {
            // For ranged attacks, wait for projectile to hit
            float projectileTime = 0.8f;
            yield return new WaitForSeconds(projectileTime);
        }
        else if (action.IsMeleeAttack && target != null)
        {
            // For melee attacks, shorter delay
            yield return new WaitForSeconds(0.3f);
        }

        yield return StartCoroutine(ExecuteMultiTargetAttack(action, target));

        // Notify that this attack is complete
        onComplete?.Invoke();
    }

    private System.Collections.IEnumerator ExecuteMultiTargetAttack(MonsterAction action, Monster target)
    {
        if (target == null || !target.isAlive) yield break;

        int totalHits = action.hitCount;

        for (int hitIndex = 0; hitIndex < totalHits; hitIndex++)
        {
            if (target == null || !target.isAlive)
            {
                Debug.Log($"Target {target?.monsterData?.monsterName} defeated during multi-hit!");
                yield break;
            }

            // Calculate damage for this hit
            int hitDamage = CalculateMultiTargetHitDamage(action, target, hitIndex, totalHits);

            // Check for critical hit
            bool isCritical = Random.Range(0f, 1f) < action.criticalChance;
            if (isCritical)
            {
                hitDamage = Mathf.RoundToInt(hitDamage * action.criticalMultiplier);
            }

            // Deal damage
            target.TakeDamage(hitDamage, isCritical);

            Debug.Log($"{currentActiveMonster.monsterData.monsterName} hit {target.monsterData.monsterName} for {hitDamage} damage! (Hit {hitIndex + 1}/{totalHits})");

            // Wait between hits
            if (hitIndex < totalHits - 1)
            {
                yield return new WaitForSeconds(action.timeBetweenHits);
            }
        }
    }

    private int CalculateMultiTargetHitDamage(MonsterAction action, Monster target, int hitIndex, int totalHits)
    {
        // Calculate base damage
        int baseDamage = action.basePower;
        if (action.usesAttackStat)
        {
            baseDamage += currentActiveMonster.currentATK;
        }

        // Divide damage if specified
        if (action.divideDamagePerHit && totalHits > 1)
        {
            baseDamage = Mathf.RoundToInt((float)baseDamage / totalHits);
            baseDamage = Mathf.Max(1, baseDamage);
        }

        // Apply elemental advantage
        float elementalMultiplier = ElementalSystem.GetElementalAdvantage(currentActiveMonster.monsterData.element, target.monsterData.element);
        int finalDamage = Mathf.RoundToInt(baseDamage * elementalMultiplier);

        // Apply defense
        if (!action.ignoresDefense)
        {
            finalDamage = Mathf.Max(1, finalDamage - target.currentDEF);
        }

        return finalDamage;
    }


    public void OnMonsterStatsChanged()
    {
        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.UpdateUI();
        }
    }

    public void AddCombatLogEntry(string entry)
    {
        combatLog.Enqueue($"• {entry}");

        if (combatLog.Count > maxLogEntries)
        {
            combatLog.Dequeue();
        }

        UpdateCombatLogDisplay();
    }

    private void UpdateCombatLogDisplay()
    {
        if (combatLogText != null)
        {
            combatLogText.text = string.Join("\n", combatLog.ToArray());
        }
    }
}

[System.Serializable]
public class AIDecision
{
    public MonsterAction action;
    public List<Monster> targets = new List<Monster>();
    public string reasoning;
}
