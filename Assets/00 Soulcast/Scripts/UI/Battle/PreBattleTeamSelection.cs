using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PreBattleTeamSelection : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private TextMeshProUGUI battleInfoText;
    [SerializeField] private TextMeshProUGUI battleDescriptionText;
    [SerializeField] private Button startBattleButton;
    [SerializeField] private Button backButton;

    [Header("Team Selection")]
    [SerializeField] private Transform teamSlotsContainer;
    [SerializeField] private GameObject teamSlotPrefab;
    [SerializeField] private int maxTeamSize = 4;

    [Header("Available Monsters")]
    [SerializeField] private Transform availableMonstersContainer;
    [SerializeField] private GameObject monsterCardPrefab;
    [SerializeField] private ScrollRect monstersScrollView;

    [Header("Battle Preview")]
    [SerializeField] private Transform battlePreviewContainer;
    [SerializeField] private GameObject wavePreviewPrefab;

    private CombatTemplate currentBattleConfig;
    private List<CollectedMonster> selectedTeam = new List<CollectedMonster>();
    private List<CollectedMonster> availableMonsters = new List<CollectedMonster>();
    private List<UniversalMonsterCard> monsterCards = new List<UniversalMonsterCard>();

    public System.Action<CombatTemplate, List<CollectedMonster>> OnBattleStart;
    public System.Action OnSelectionCancelled;

    private void Awake()
    {
        SetupEventHandlers();

        // ✅ Safe null check for selectionPanel
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("SelectionPanel not assigned! Using this GameObject as panel.");
            gameObject.SetActive(false);
        }
    }

    private void SetupEventHandlers()
    {
        startBattleButton?.onClick.AddListener(StartBattle);
        backButton?.onClick.AddListener(CancelSelection);
    }

    public void ShowTeamSelection(CombatTemplate combatTemplate)
    {
        currentBattleConfig = combatTemplate;
        selectedTeam.Clear();

        Debug.Log($"Showing team selection for combat: {combatTemplate?.combatName}");

        UpdateBattleInfo();
        LoadAvailableMonsters();
        SetupTeamSlots();
        SetupBattlePreview();

        // ✅ Activate panel
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        UpdateStartButtonState();
    }

    private void UpdateBattleInfo()
    {
        if (currentBattleConfig == null) return;

        if (battleInfoText != null)
            battleInfoText.text = $"{currentBattleConfig.combatName}\nWaves: {currentBattleConfig.TotalWaves} | Enemies: {currentBattleConfig.TotalEnemies}";

        if (battleDescriptionText != null)
            battleDescriptionText.text = currentBattleConfig.combatDescription;
    }


    private void LoadAvailableMonsters()
    {
        // Clear existing cards
        foreach (var card in monsterCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        monsterCards.Clear();

        // Get available monsters from MonsterCollectionManager
        if (MonsterCollectionManager.Instance != null)
        {
            availableMonsters = MonsterCollectionManager.Instance.GetAllMonsters();
        }
        else
        {
            Debug.LogWarning("MonsterCollectionManager.Instance is null!");
            availableMonsters = new List<CollectedMonster>();
        }

        // Create monster cards
        foreach (var monster in availableMonsters)
        {
            var cardObj = Instantiate(monsterCardPrefab, availableMonstersContainer);
            var card = cardObj.GetComponent<UniversalMonsterCard>();

            if (card != null)
            {
                // Setup for battle selection mode
                card.SetupForBattleSelection(
                    monster,
                    () => AddToTeam(monster),
                    () => RemoveFromTeam(monster)
                );

                // Show additional info for battle selection
                card.ShowRoleInfo(true);
                card.ShowStatsInfo(false);

                monsterCards.Add(card);
            }
        }

        Debug.Log($"Loaded {availableMonsters.Count} available monsters for selection");
    }

    private void SetupTeamSlots()
    {
        Debug.Log($"Setting up {maxTeamSize} team slots in container: {teamSlotsContainer?.name}");

        if (teamSlotsContainer == null)
        {
            Debug.LogError("TeamSlotsContainer is null!");
            return;
        }

        if (teamSlotPrefab == null)
        {
            Debug.LogError("TeamSlotPrefab is null!");
            return;
        }

        // Clear existing slots
        foreach (Transform child in teamSlotsContainer)
        {
            if (child != teamSlotsContainer)
                Destroy(child.gameObject);
        }

        // Create team slots
        for (int i = 0; i < maxTeamSize; i++)
        {
            var slotObj = Instantiate(teamSlotPrefab, teamSlotsContainer);

            // ✅ FIX: Explicitly activate the slot GameObject
            slotObj.SetActive(true);

            var slot = slotObj.GetComponent<TeamSlot>();

            if (slot != null)
            {
                int slotIndex = i;
                slot.Setup(slotIndex, () => RemoveFromTeamAtIndex(slotIndex));

                if (i < selectedTeam.Count)
                    slot.SetMonster(selectedTeam[i]);

                Debug.Log($"Created and activated TeamSlot {i}");
            }
            else
            {
                Debug.LogError($"TeamSlot {i} component not found on prefab!");
            }
        }

        Debug.Log($"Team slots setup completed. Container has {teamSlotsContainer.childCount} children");
    }

    private void SetupBattlePreview()
    {
        if (battlePreviewContainer == null || currentBattleConfig == null) return;

        // Clear existing preview
        foreach (Transform child in battlePreviewContainer)
        {
            if (child != battlePreviewContainer)
                Destroy(child.gameObject);
        }

        // Create wave previews
        for (int i = 0; i < currentBattleConfig.waves.Count; i++)
        {
            var waveObj = Instantiate(wavePreviewPrefab, battlePreviewContainer);
            var preview = waveObj.GetComponent<WavePreview>();

            if (preview != null)
                preview.Setup(currentBattleConfig.waves[i], i + 1);
        }
    }

    private void AddToTeam(CollectedMonster monster)
    {
        if (selectedTeam.Count >= maxTeamSize)
        {
            Debug.Log("Team is full!");
            return;
        }

        if (selectedTeam.Contains(monster))
        {
            Debug.Log("Monster already in team!");
            return;
        }

        selectedTeam.Add(monster);
        RefreshDisplays();

        Debug.Log($"Added {monster.monsterData.monsterName} to team. Team size: {selectedTeam.Count}");
    }

    private void RemoveFromTeam(CollectedMonster monster)
    {
        if (selectedTeam.Remove(monster))
        {
            RefreshDisplays();
            Debug.Log($"Removed {monster.monsterData.monsterName} from team. Team size: {selectedTeam.Count}");
        }
    }

    private void RemoveFromTeamAtIndex(int index)
    {
        if (index >= 0 && index < selectedTeam.Count)
        {
            var monster = selectedTeam[index];
            selectedTeam.RemoveAt(index);
            RefreshDisplays();

            Debug.Log($"Removed {monster.monsterData.monsterName} from team slot {index}");
        }
    }

    private void RefreshDisplays()
    {
        RefreshTeamDisplay();
        RefreshMonsterCards();
        UpdateStartButtonState();
    }

    private void RefreshTeamDisplay()
    {
        var slots = teamSlotsContainer.GetComponentsInChildren<TeamSlot>(includeInactive: true); // ✅ Include inactive children

        Debug.Log($"Refreshing {slots.Length} team slots");

        for (int i = 0; i < slots.Length; i++)
        {
            // ✅ FIX: Ensure slot GameObject is active
            slots[i].gameObject.SetActive(true);

            if (i < selectedTeam.Count)
            {
                slots[i].SetMonster(selectedTeam[i]);
                Debug.Log($"Set monster {selectedTeam[i].monsterData.monsterName} to slot {i}");
            }
            else
            {
                slots[i].ClearSlot();
                Debug.Log($"Cleared slot {i}");
            }
        }
    }

    private void RefreshMonsterCards()
    {
        foreach (var card in monsterCards)
        {
            if (card?.GetMonster() != null)
            {
                bool isSelected = selectedTeam.Contains(card.GetMonster());
                card.SetSelected(isSelected);
            }
        }
    }

    private void UpdateStartButtonState()
    {
        if (currentBattleConfig == null) return;

        bool canStart = selectedTeam.Count > 0 && selectedTeam.Count >= currentBattleConfig.recommendedTeamSize;

        if (startBattleButton != null)
        {
            startBattleButton.interactable = canStart;

            var buttonText = startBattleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (selectedTeam.Count == 0)
                    buttonText.text = "Select Monsters";
                else if (selectedTeam.Count < currentBattleConfig.recommendedTeamSize)
                    buttonText.text = $"Need {currentBattleConfig.recommendedTeamSize - selectedTeam.Count} More";
                else
                    buttonText.text = "Start Battle";
            }
        }
    }

    private void StartBattle()
    {
        if (selectedTeam.Count == 0) return;

        Debug.Log($"Starting battle with {selectedTeam.Count} monsters:");
        foreach (var monster in selectedTeam)
        {
            Debug.Log($"- {monster.monsterData.monsterName} (Lv.{monster.level}, {monster.currentStarLevel}★)");
        }

        SaveSelectedTeam();

        // ✅ NEW: Auto-win the battle for testing (as discussed earlier)
        AutoWinBattle();

        // Original battle start (commented out for now)
        // OnBattleStart?.Invoke(currentBattleConfig, selectedTeam);

        if (selectionPanel != null)
            selectionPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void AutoWinBattle()
    {
        if (currentBattleConfig == null)
        {
            Debug.LogError("Cannot auto-win: no battle configuration set!");
            return;
        }

        // Get current battle info from WorldMapManager/BattleSequenceMenu
        int currentRegion = PlayerPrefs.GetInt("CurrentRegion", 1);
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        int currentBattle = PlayerPrefs.GetInt("CurrentBattleSequence", 1);

        Debug.Log($"🏆 AUTO-WINNING Battle: Region {currentRegion}, Level {currentLevel}, Battle {currentBattle}");

        // Mark this battle as completed with 3 stars
        CompleteBattle(currentRegion, currentLevel, currentBattle, 3);

        // Show victory message
        StartCoroutine(ShowVictoryMessage());
    }

    // ✅ NEW: Battle completion system
    private void CompleteBattle(int regionId, int levelId, int battleId, int starsEarned)
    {
        // Mark individual battle as completed
        string battleKey = $"Region_{regionId}_Level_{levelId}_Battle_{battleId}";
        PlayerPrefs.SetInt($"{battleKey}_Completed", 1);
        PlayerPrefs.SetInt($"{battleKey}_Stars", starsEarned);

        Debug.Log($"✅ Battle {battleId} completed with {starsEarned} stars");

        // Give rewards (optional - you can customize this)
        GiveRewards(regionId, levelId, battleId, starsEarned);

        // Check if all battles in this level are completed
        bool allBattlesCompleted = CheckAllBattlesCompleted(regionId, levelId);

        if (allBattlesCompleted)
        {
            // Mark entire level as completed
            CompleteLevelProgression(regionId, levelId);
        }

        PlayerPrefs.Save();

        // Refresh any UI that shows battle progress
        RefreshWorldMapProgress();
    }

    // ✅ NEW: Check if all battles in level are completed
    private bool CheckAllBattlesCompleted(int regionId, int levelId)
    {
        // Get number of battles in this level from database
        var battleDatabase = Resources.Load<LevelDatabase>("Battle/Database/BattleDatabase");
        if (battleDatabase == null)
        {
            Debug.LogError("BattleDatabase not found!");
            return false;
        }

        var levelBattles = battleDatabase.GetLevelBattles(regionId, levelId);
        int totalBattles = levelBattles.Count;

        // Check each battle completion status
        int completedBattles = 0;
        for (int i = 1; i <= totalBattles; i++)
        {
            string battleKey = $"Region_{regionId}_Level_{levelId}_Battle_{i}";
            if (PlayerPrefs.GetInt($"{battleKey}_Completed", 0) == 1)
            {
                completedBattles++;
            }
        }

        bool allCompleted = completedBattles >= totalBattles;
        Debug.Log($"Level {levelId} progress: {completedBattles}/{totalBattles} battles completed");

        return allCompleted;
    }

    // ✅ NEW: Complete entire level progression
    private void CompleteLevelProgression(int regionId, int levelId)
    {
        // Mark level as completed
        string levelKey = $"Region_{regionId}_Level_{levelId}";
        PlayerPrefs.SetInt($"{levelKey}_Completed", 1);

        // Calculate average stars for the level
        var battleDatabase = Resources.Load<LevelDatabase>("Battle/Database/BattleDatabase");
        var levelBattles = battleDatabase.GetLevelBattles(regionId, levelId);

        int totalStars = 0;
        for (int i = 1; i <= levelBattles.Count; i++)
        {
            string battleKey = $"Region_{regionId}_Level_{levelId}_Battle_{i}";
            totalStars += PlayerPrefs.GetInt($"{battleKey}_Stars", 0);
        }

        int averageStars = levelBattles.Count > 0 ? Mathf.RoundToInt((float)totalStars / levelBattles.Count) : 0;
        PlayerPrefs.SetInt($"{levelKey}_Stars", averageStars);

        Debug.Log($"🎉 LEVEL {levelId} COMPLETED! Average stars: {averageStars}");
        Debug.Log($"🔓 Level {levelId + 1} is now unlocked!");
    }

    // ✅ NEW: Give rewards for completing battle
    private void GiveRewards(int regionId, int levelId, int battleId, int starsEarned)
    {
        // Basic rewards - you can customize this
        int coinReward = 50 * battleId * starsEarned; // More coins for later battles and more stars
        int expReward = 25 * battleId * starsEarned;

        // Add to current totals (assuming you have these stats)
        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        int currentExp = PlayerPrefs.GetInt("PlayerExperience", 0);

        PlayerPrefs.SetInt("PlayerCoins", currentCoins + coinReward);
        PlayerPrefs.SetInt("PlayerExperience", currentExp + expReward);

        Debug.Log($"💰 Rewards: +{coinReward} coins, +{expReward} experience");
    }

    // ✅ NEW: Refresh world map UI to show progress
    private void RefreshWorldMapProgress()
    {
        // Find and refresh battle sequence menu
        var battleSequenceMenu = FindFirstObjectByType<BattleSequenceMenu>();
        if (battleSequenceMenu != null && battleSequenceMenu.IsVisible)
        {
            // Force refresh the battle button states
            Debug.Log("Refreshing BattleSequenceMenu progress");
            // You might need to add a public refresh method to BattleSequenceMenu
        }

        // Find and refresh world map manager
        var worldMapManager = FindFirstObjectByType<WorldMapManager>();
        if (worldMapManager != null)
        {
            Debug.Log("Refreshing WorldMapManager progress");
            // You might need to add a public refresh method to WorldMapManager
        }
    }

    // ✅ NEW: Show victory message coroutine
    private System.Collections.IEnumerator ShowVictoryMessage()
    {
        Debug.Log("🎉 VICTORY! Battle completed successfully!");

        // Optional: Show a temporary victory UI
        if (battleInfoText != null)
        {
            string originalText = battleInfoText.text;
            battleInfoText.text = "🏆 VICTORY! 🏆\nBattle Completed!";
            battleInfoText.color = Color.green;

            yield return new WaitForSeconds(2f);

            battleInfoText.text = originalText;
            battleInfoText.color = Color.white;
        }

        // Close the team selection after victory
        yield return new WaitForSeconds(0.5f);
        CancelSelection();
    }

    private void SaveSelectedTeam()
    {
        string teamIDs = string.Join(",", selectedTeam.Select(m => m.uniqueID));
        PlayerPrefs.SetString("SelectedTeamIDs", teamIDs);
        Debug.Log($"Saved selected team: {teamIDs}");
    }

    private void CancelSelection()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        OnSelectionCancelled?.Invoke();
    }

    // Public properties
    public bool IsVisible => selectionPanel != null ? selectionPanel.activeSelf : gameObject.activeSelf;
    public List<CollectedMonster> SelectedTeam => new List<CollectedMonster>(selectedTeam);

    // ✅ NEW: Context menu for debugging
    [ContextMenu("Test Show Selection")]
    private void TestShowSelection()
    {
        if (Application.isPlaying)
        {
            // Create a dummy battle config for testing
            var dummyConfig = ScriptableObject.CreateInstance<CombatTemplate>();
            dummyConfig.combatName = "Test Battle";
            dummyConfig.combatDescription = "This is a test battle";
            dummyConfig.recommendedTeamSize = 2;

            ShowTeamSelection(dummyConfig);
        }
    }

    [ContextMenu("Debug Team Slots")]
    private void DebugTeamSlots()
    {
        if (Application.isPlaying && teamSlotsContainer != null)
        {
            Debug.Log($"Team Slots Container: {teamSlotsContainer.name}");
            Debug.Log($"Child count: {teamSlotsContainer.childCount}");

            for (int i = 0; i < teamSlotsContainer.childCount; i++)
            {
                var child = teamSlotsContainer.GetChild(i);
                Debug.Log($"Slot {i}: {child.name}, Active: {child.gameObject.activeSelf}");

                var slot = child.GetComponent<TeamSlot>();
                if (slot != null)
                {
                    Debug.Log($"  - TeamSlot component found, IsEmpty: {slot.IsEmpty}");
                }
                else
                {
                    Debug.Log($"  - NO TeamSlot component found!");
                }
            }
        }
    }

    // ✅ NEW: Context menu debug methods
    [ContextMenu("Auto Win Current Battle")]
    private void TestAutoWin()
    {
        if (Application.isPlaying && currentBattleConfig != null)
        {
            AutoWinBattle();
        }
        else
        {
            Debug.LogWarning("No battle configuration set or not playing!");
        }
    }

    [ContextMenu("Reset All Battle Progress")]
    private void ResetAllProgress()
    {
        if (Application.isPlaying)
        {
            // Reset all battle and level progress
            for (int region = 1; region <= 3; region++)
            {
                for (int level = 1; level <= 5; level++)
                {
                    // Reset level completion
                    PlayerPrefs.DeleteKey($"Region_{region}_Level_{level}_Completed");
                    PlayerPrefs.DeleteKey($"Region_{region}_Level_{level}_Stars");

                    // Reset battle completion (up to 10 battles per level)
                    for (int battle = 1; battle <= 10; battle++)
                    {
                        PlayerPrefs.DeleteKey($"Region_{region}_Level_{level}_Battle_{battle}_Completed");
                        PlayerPrefs.DeleteKey($"Region_{region}_Level_{level}_Battle_{battle}_Stars");
                    }
                }
            }

            PlayerPrefs.Save();
            Debug.Log("🔄 All battle progress reset!");

            RefreshWorldMapProgress();
        }
    }

    [ContextMenu("Unlock All Battles")]
    private void UnlockAllBattles()
    {
        if (Application.isPlaying)
        {
            // Complete all battles with 3 stars
            for (int region = 1; region <= 3; region++)
            {
                for (int level = 1; level <= 5; level++)
                {
                    // Complete level
                    PlayerPrefs.SetInt($"Region_{region}_Level_{level}_Completed", 1);
                    PlayerPrefs.SetInt($"Region_{region}_Level_{level}_Stars", 3);

                    // Complete all battles in level
                    for (int battle = 1; battle <= 10; battle++)
                    {
                        PlayerPrefs.SetInt($"Region_{region}_Level_{level}_Battle_{battle}_Completed", 1);
                        PlayerPrefs.SetInt($"Region_{region}_Level_{level}_Stars", 3);
                    }
                }
            }

            PlayerPrefs.Save();
            Debug.Log("🔓 All battles unlocked with 3 stars!");

            RefreshWorldMapProgress();
        }
    }

    [ContextMenu("Show Current Progress")]
    private void ShowCurrentProgress()
    {
        if (Application.isPlaying)
        {
            Debug.Log("=== CURRENT BATTLE PROGRESS ===");

            for (int region = 1; region <= 2; region++)
            {
                for (int level = 1; level <= 3; level++)
                {
                    string levelKey = $"Region_{region}_Level_{level}";
                    bool levelCompleted = PlayerPrefs.GetInt($"{levelKey}_Completed", 0) == 1;
                    int levelStars = PlayerPrefs.GetInt($"{levelKey}_Stars", 0);

                    Debug.Log($"Region {region}, Level {level}: {(levelCompleted ? "✅" : "❌")} ({levelStars}★)");

                    // Show individual battles
                    for (int battle = 1; battle <= 3; battle++)
                    {
                        string battleKey = $"Region_{region}_Level_{level}_Battle_{battle}";
                        bool battleCompleted = PlayerPrefs.GetInt($"{battleKey}_Completed", 0) == 1;
                        int battleStars = PlayerPrefs.GetInt($"{battleKey}_Stars", 0);

                        Debug.Log($"  Battle {battle}: {(battleCompleted ? "✅" : "❌")} ({battleStars}★)");
                    }
                }
            }
        }
    }

}
