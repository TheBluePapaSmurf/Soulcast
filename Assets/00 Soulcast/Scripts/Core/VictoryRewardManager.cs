// Create: Assets/00 Soulcast/Scripts/UI/Battle/VictoryRewardManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VictoryRewardManager : MonoBehaviour
{
    public static VictoryRewardManager Instance { get; private set; }

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPopup;
    [SerializeField] private TextMeshProUGUI victoryTitle;
    [SerializeField] private TextMeshProUGUI battleNameText;
    [SerializeField] private GameObject[] victoryStars; // 3 star objects
    [SerializeField] private Button continueToRewardsButton;
    [SerializeField] private AudioSource victoryAudioSource;
    [SerializeField] private AudioClip victorySound;

    [Header("Reward Screen")]
    [SerializeField] private GameObject rewardScreen;
    [SerializeField] private TextMeshProUGUI rewardTitle;
    [SerializeField] private Transform rewardItemsContainer;
    [SerializeField] private GameObject rewardItemPrefab;
    [SerializeField] private Button claimRewardsButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;

    [Header("Currency Display")]
    [SerializeField] private GameObject soulCoinsReward;
    [SerializeField] private TextMeshProUGUI soulCoinsText;
    [SerializeField] private Image soulCoinsIcon;

    [Header("Animation Settings")]
    [SerializeField] private float victoryPopupDelay = 1f;
    [SerializeField] private float starAnimationDelay = 0.5f;
    [SerializeField] private float rewardItemDelay = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Current reward data
    private CombatResult currentRewards;
    private BattleSetupData currentBattleData;
    private int starsEarned = 3;
    private bool rewardsClaimed = false;

    // Events
    public System.Action OnVictoryShown;
    public System.Action OnRewardsShown;
    public System.Action OnRewardsClaimed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SetupEventHandlers();
            HideAllUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupEventHandlers()
    {
        continueToRewardsButton?.onClick.AddListener(ShowRewardScreen);
        claimRewardsButton?.onClick.AddListener(ClaimRewardsAndExit);
    }

    private void HideAllUI()
    {
        victoryPopup?.SetActive(false);
        rewardScreen?.SetActive(false);
        rewardsClaimed = false;
    }

    // ✅ MAIN API: Show victory sequence
    public void ShowVictorySequence(BattleSetupData battleData, CombatResult rewards, int stars = 3)
    {
        currentBattleData = battleData;
        currentRewards = rewards;
        starsEarned = stars;
        rewardsClaimed = false;

        if (debugMode)
        {
            Debug.Log($"🏆 Showing victory sequence for {battleData.combatTemplate?.combatName}");
            Debug.Log($"⭐ Stars earned: {stars}");
            Debug.Log($"💰 Soul Coins: {rewards.soulCoinsEarned:N0}");
            Debug.Log($"🎁 Runes: {rewards.runesEarned?.Count ?? 0}");
        }

        StartCoroutine(VictorySequence());
    }

    // ✅ Victory sequence coroutine
    private IEnumerator VictorySequence()
    {
        // Wait a moment for combat to fully end
        yield return new WaitForSeconds(victoryPopupDelay);

        // Show victory popup
        ShowVictoryPopup();

        // Wait for user to continue
        yield return new WaitUntil(() => rewardScreen.activeSelf);

        // Show rewards
        yield return StartCoroutine(AnimateRewardItems());
    }

    // ✅ Show victory popup with stars
    private void ShowVictoryPopup()
    {
        if (victoryPopup == null) return;

        victoryPopup.SetActive(true);

        // Set battle name
        if (battleNameText != null && currentBattleData.combatTemplate != null)
        {
            battleNameText.text = currentBattleData.combatTemplate.combatName;
        }

        // Set victory title
        if (victoryTitle != null)
        {
            string[] victoryMessages = { "Victory!", "Excellent!", "Outstanding!", "Perfect!" };
            victoryTitle.text = victoryMessages[Mathf.Min(starsEarned, victoryMessages.Length - 1)];
        }

        // Play victory sound
        if (victoryAudioSource != null && victorySound != null)
        {
            victoryAudioSource.PlayOneShot(victorySound);
        }

        // Animate stars
        StartCoroutine(AnimateVictoryStars());

        OnVictoryShown?.Invoke();

        if (debugMode)
        {
            Debug.Log("🎉 Victory popup shown");
        }
    }

    // ✅ Animate victory stars
    private IEnumerator AnimateVictoryStars()
    {
        if (victoryStars == null) yield break;

        // Hide all stars initially
        foreach (var star in victoryStars)
        {
            if (star != null) star.SetActive(false);
        }

        // Show stars based on earned amount
        for (int i = 0; i < Mathf.Min(starsEarned, victoryStars.Length); i++)
        {
            if (victoryStars[i] != null)
            {
                victoryStars[i].SetActive(true);

                // Scale animation
                var star = victoryStars[i];
                star.transform.localScale = Vector3.zero;

                float animTime = 0.5f;
                float elapsed = 0f;

                while (elapsed < animTime)
                {
                    float t = elapsed / animTime;
                    float scale = Mathf.Lerp(0f, 1f, AnimationCurve.EaseInOut(0, 0, 1, 1).Evaluate(t));
                    star.transform.localScale = Vector3.one * scale;

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                star.transform.localScale = Vector3.one;
                yield return new WaitForSeconds(starAnimationDelay);
            }
        }

        // Enable continue button
        if (continueToRewardsButton != null)
        {
            continueToRewardsButton.interactable = true;
        }
    }

    // ✅ Show reward screen
    private void ShowRewardScreen()
    {
        if (rewardScreen == null) return;

        victoryPopup?.SetActive(false);
        rewardScreen.SetActive(true);

        // Set reward title
        if (rewardTitle != null)
        {
            rewardTitle.text = "Battle Rewards";
        }

        // Reset claim button
        if (claimRewardsButton != null)
        {
            claimRewardsButton.interactable = false;
        }

        if (claimButtonText != null)
        {
            claimButtonText.text = "Claiming Rewards...";
        }

        OnRewardsShown?.Invoke();

        if (debugMode)
        {
            Debug.Log("🎁 Reward screen shown");
        }
    }

    private IEnumerator AnimateRewardItems()
    {
        if (rewardItemsContainer == null) yield return null;

        // Clear existing reward items
        foreach (Transform child in rewardItemsContainer)
        {
            Destroy(child.gameObject);
        }

        // Show soul coins first
        if (currentRewards.soulCoinsEarned > 0)
        {
            yield return StartCoroutine(ShowSoulCoinsReward());
        }

        // Show rune rewards
        if (currentRewards.runesEarned != null && currentRewards.runesEarned.Count > 0)
        {
            yield return StartCoroutine(ShowRuneRewards());
        }

        // ✅ COMMENT OUT: Show monster XP rewards until implemented
        /*
        if (currentRewards.monsterExperienceGained != null && currentRewards.monsterExperienceGained.Count > 0)
        {
            yield return StartCoroutine(ShowMonsterXpRewards());
        }
        */

        // Enable claim button
        if (claimRewardsButton != null)
        {
            claimRewardsButton.interactable = true;
        }

        if (claimButtonText != null)
        {
            claimButtonText.text = "Claim Rewards";
        }

        if (debugMode)
        {
            Debug.Log("✨ All reward animations completed");
        }
    }

    // ✅ Show soul coins reward
    private IEnumerator ShowSoulCoinsReward()
    {
        if (soulCoinsReward != null)
        {
            soulCoinsReward.SetActive(true);

            if (soulCoinsText != null)
            {
                soulCoinsText.text = $"+{currentRewards.soulCoinsEarned:N0}";
            }

            // Scale animation
            soulCoinsReward.transform.localScale = Vector3.zero;
            yield return StartCoroutine(ScaleInAnimation(soulCoinsReward.transform));

            yield return new WaitForSeconds(rewardItemDelay);
        }
    }

    // ✅ Show rune rewards
    private IEnumerator ShowRuneRewards()
    {
        if (currentRewards.runesEarned == null || currentRewards.runesEarned.Count == 0)
            yield break;

        foreach (var rune in currentRewards.runesEarned)
        {
            if (rune != null)
            {
                var rewardItem = CreateRewardItem(rune);
                if (rewardItem != null)
                {
                    yield return StartCoroutine(ScaleInAnimation(rewardItem.transform));
                    yield return new WaitForSeconds(rewardItemDelay);

                    if (debugMode)
                    {
                        Debug.Log($"🎁 Displayed rune reward: {rune.runeName} ({rune.rarity})");
                        Debug.Log($"   Main Stat: {rune.mainStat.statType} {rune.mainStat.value}{(rune.mainStat.isPercentage ? "%" : "")}");
                        Debug.Log($"   Sub Stats: {rune.subStats.Count}");
                    }
                }
            }
        }
    }

    // ✅ COMMENT OUT: ShowMonsterXpRewards method until implemented
    /*
    private IEnumerator ShowMonsterXpRewards()
    {
        foreach (var xpReward in currentRewards.monsterExperienceGained)
        {
            var rewardItem = CreateXpRewardItem(xpReward.Key, xpReward.Value);
            if (rewardItem != null)
            {
                yield return StartCoroutine(ScaleInAnimation(rewardItem.transform));
                yield return new WaitForSeconds(rewardItemDelay);
            }
        }
    }
    */

    // ✅ Create reward item for runes
    private GameObject CreateRewardItem(RuneData rune)
    {
        if (rewardItemPrefab == null || rewardItemsContainer == null) return null;

        var rewardItem = Instantiate(rewardItemPrefab, rewardItemsContainer);

        // Setup rune reward item (customize based on your prefab structure)
        var texts = rewardItem.GetComponentsInChildren<TextMeshProUGUI>();
        var images = rewardItem.GetComponentsInChildren<Image>();

        // Set rune name and info
        if (texts.Length > 0)
        {
            texts[0].text = rune.runeName;
        }

        if (texts.Length > 1)
        {
            // Show main stat info
            string mainStatText = $"{rune.mainStat.statType}: {rune.mainStat.value:F0}{(rune.mainStat.isPercentage ? "%" : "")}";
            texts[1].text = mainStatText;
        }

        if (texts.Length > 2)
        {
            // Show rarity and slot
            texts[2].text = $"{rune.rarity} • {rune.runeSlotPosition}";
        }

        // Set rune icon
        if (images.Length > 0 && rune.runeSprite != null)
        {
            images[0].sprite = rune.runeSprite;
        }

        // Set background color based on rarity
        if (images.Length > 1)
        {
            images[1].color = GetRarityColor(rune.rarity);
        }

        rewardItem.transform.localScale = Vector3.zero;
        return rewardItem;
    }

    private Color GetRarityColor(RuneRarity rarity)
    {
        switch (rarity)
        {
            case RuneRarity.Common: return Color.gray;
            case RuneRarity.Uncommon: return Color.green;
            case RuneRarity.Rare: return Color.blue;
            case RuneRarity.Epic: return Color.magenta;
            case RuneRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    // ✅ COMMENT OUT: CreateXpRewardItem method until implemented
    /*
    private GameObject CreateXpRewardItem(string monsterId, int xpGained)
    {
        if (rewardItemPrefab == null || rewardItemsContainer == null) return null;

        var rewardItem = Instantiate(rewardItemPrefab, rewardItemsContainer);

        // Setup XP reward item
        var nameText = rewardItem.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = $"+{xpGained} XP";
        }

        rewardItem.transform.localScale = Vector3.zero;
        return rewardItem;
    }
    */

    private IEnumerator ScaleInAnimation(Transform target)
    {
        float animTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < animTime)
        {
            float t = elapsed / animTime;

            // ✅ FIX: Custom ease-out-back calculation instead of AnimationCurve.EaseOutBack
            float scale = EaseOutBack(t);
            target.localScale = Vector3.one * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    // ✅ ADD: Custom ease-out-back function
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }


    // ✅ Claim rewards and exit
    private void ClaimRewardsAndExit()
    {
        if (rewardsClaimed) return;

        rewardsClaimed = true;

        if (debugMode)
        {
            Debug.Log("💎 Claiming rewards and exiting battle...");
        }

        // Apply rewards to player inventory
        ApplyRewardsToPlayer();

        // Update battle progression
        UpdateBattleProgression();

        // Save game
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        OnRewardsClaimed?.Invoke();

        // Return to world map after short delay
        StartCoroutine(ReturnToWorldMap());
    }

    // ✅ UPDATE VictoryRewardManager.cs - Add UI refresh after claiming rewards

    private void ApplyRewardsToPlayer()
    {
        // Add soul coins
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddSoulCoins(currentRewards.soulCoinsEarned);
        }

        // Add runes
        if (RuneCollectionManager.Instance != null && currentRewards.runesEarned != null)
        {
            foreach (var rune in currentRewards.runesEarned)
            {
                RuneCollectionManager.Instance.AddRune(rune);
            }

            if (debugMode)
            {
                Debug.Log($"✅ Added {currentRewards.runesEarned.Count} runes to collection");
                Debug.Log("🔄 UI will be refreshed automatically when returning to HUB scene");
            }
        }

        if (debugMode)
        {
            Debug.Log("✅ All rewards applied to player inventory");
        }
    }


    // ✅ Update battle progression
    private void UpdateBattleProgression()
    {
        if (BattleProgressionManager.Instance != null && currentBattleData != null)
        {
            BattleProgressionManager.Instance.CompleteBattle(
                currentBattleData.regionId,
                currentBattleData.levelId,
                currentBattleData.battleSequenceId,
                starsEarned,
                Time.time
            );
        }
    }

    // ✅ Return to world map
    private IEnumerator ReturnToWorldMap()
    {
        yield return new WaitForSeconds(1f);

        // Clear battle data
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.ClearBattleData();
        }

        // Return to world map
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ReturnToWorldMapAfterBattle();
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager.Instance is null! Cannot return to world map.");
        }
    }

    // ✅ DEBUG METHODS
    [ContextMenu("Test Victory Sequence")]
    private void TestVictorySequence()
    {
        if (Application.isPlaying)
        {
            // Create test data
            var testBattleData = new BattleSetupData
            {
                combatTemplate = ScriptableObject.CreateInstance<CombatTemplate>(),
                regionId = 1,
                levelId = 1,
                battleSequenceId = 1
            };
            testBattleData.combatTemplate.combatName = "Test Battle";

            var testRewards = new CombatResult
            {
                soulCoinsEarned = 500,
                runesEarned = new List<RuneData>(),
                monsterExperienceGained = new Dictionary<string, int>()
            };

            ShowVictorySequence(testBattleData, testRewards, 3);
        }
    }

    [ContextMenu("Hide All UI")]
    private void TestHideAllUI()
    {
        if (Application.isPlaying)
        {
            HideAllUI();
        }
    }
}
