// Create: Assets/00 Soulcast/Scripts/UI/Battle/VictoryRewardManager.cs

using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryRewardManager : MonoBehaviour
{
    public static VictoryRewardManager Instance { get; private set; }

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPopup;
    [SerializeField] private TextMeshProUGUI victoryTitle;
    [SerializeField] private TextMeshProUGUI battleNameText;
    [SerializeField] private GameObject[] victoryStars;
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

    [Header("XP Animation System")]
    public GameObject xpAnimationPanel;
    public Transform monsterXPContainer;
    public GameObject xpMonsterCardPrefab;
    public GameObject universalMonsterCardPrefab;
    public GameObject levelUpPopupPrefab;

    [Header("XP Animation Settings")]
    public float xpAnimationDuration = 2.0f;
    public float monsterCardSpacing = 0.3f;
    public AudioClip xpGainSound;
    public AudioClip levelUpSound;

    [Header("Scene Management")]
    [SerializeField] private string worldMapSceneName = "WorldMap";
    [SerializeField] private string fallbackSceneName = "MainMenu";

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

    public void ShowVictorySequence(BattleSetupData battleData, CombatResult rewards, int stars = 3)
    {
        currentBattleData = battleData;
        currentRewards = rewards;
        starsEarned = stars;
        rewardsClaimed = false;

        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        yield return new WaitForSeconds(victoryPopupDelay);
        ShowVictoryPopup();
        yield return new WaitUntil(() => rewardScreen.activeSelf);
        yield return StartCoroutine(AnimateRewardItems());
    }

    private void ShowVictoryPopup()
    {
        if (victoryPopup == null) return;

        victoryPopup.SetActive(true);

        if (battleNameText != null && currentBattleData.combatTemplate != null)
        {
            battleNameText.text = currentBattleData.combatTemplate.combatName;
        }

        if (victoryTitle != null)
        {
            string[] victoryMessages = { "Victory!", "Excellent!", "Outstanding!", "Perfect!" };
            victoryTitle.text = victoryMessages[Mathf.Min(starsEarned, victoryMessages.Length - 1)];
        }

        StartCoroutine(AnimateStars());
    }

    private IEnumerator AnimateStars()
    {
        if (victoryStars == null || victoryStars.Length == 0) yield break;

        for (int i = 0; i < victoryStars.Length; i++)
        {
            if (victoryStars[i] == null) continue;

            if (i < starsEarned)
            {
                victoryStars[i].SetActive(true);
                victoryStars[i].transform.localScale = Vector3.zero;

                while (victoryStars[i].transform.localScale.x < 1f)
                {
                    victoryStars[i].transform.localScale = Vector3.Lerp(
                        victoryStars[i].transform.localScale,
                        Vector3.one,
                        Time.deltaTime * 5f
                    );
                    yield return null;
                }

                victoryStars[i].transform.localScale = Vector3.one;
                yield return new WaitForSeconds(starAnimationDelay);
            }
        }

        if (continueToRewardsButton != null)
        {
            continueToRewardsButton.interactable = true;
        }
    }

    private void ShowRewardScreen()
    {
        if (rewardScreen == null) return;

        victoryPopup?.SetActive(false);
        rewardScreen.SetActive(true);

        if (rewardTitle != null)
        {
            rewardTitle.text = "Battle Rewards";
        }

        if (claimRewardsButton != null)
        {
            claimRewardsButton.interactable = false;
        }

        if (claimButtonText != null)
        {
            claimButtonText.text = "Claiming Rewards...";
        }

        OnRewardsShown?.Invoke();
    }

    private IEnumerator AnimateRewardItems()
    {
        if (rewardItemsContainer == null) yield return null;

        foreach (Transform child in rewardItemsContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentRewards.soulCoinsEarned > 0)
        {
            yield return StartCoroutine(ShowSoulCoinsReward());
        }

        if (currentRewards.runesEarned != null && currentRewards.runesEarned.Count > 0)
        {
            yield return StartCoroutine(ShowRuneRewards());
        }

        if (currentRewards.experienceEarned > 0)
        {
            yield return StartCoroutine(ShowXPAnimation());
        }

        EnableClaimButton();
    }

    private IEnumerator ShowSoulCoinsReward()
    {
        if (rewardItemPrefab == null || rewardItemsContainer == null) yield break;

        var rewardItem = Instantiate(rewardItemPrefab, rewardItemsContainer);
        var texts = rewardItem.GetComponentsInChildren<TextMeshProUGUI>();
        var images = rewardItem.GetComponentsInChildren<Image>();

        foreach (var text in texts)
        {
            if (text.name.ToLower().Contains("amount"))
            {
                text.text = currentRewards.soulCoinsEarned.ToString("N0");
            }
            else if (text.name.ToLower().Contains("name"))
            {
                text.text = "Soul Coins";
            }
        }

        rewardItem.transform.localScale = Vector3.zero;
        rewardItem.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(rewardItemDelay);
    }

    private IEnumerator ShowRuneRewards()
    {
        if (currentRewards.runesEarned == null || currentRewards.runesEarned.Count == 0) yield break;

        foreach (var rune in currentRewards.runesEarned)
        {
            if (rewardItemPrefab != null && rewardItemsContainer != null)
            {
                var rewardItem = Instantiate(rewardItemPrefab, rewardItemsContainer);
                var texts = rewardItem.GetComponentsInChildren<TextMeshProUGUI>();

                foreach (var text in texts)
                {
                    if (text.name.ToLower().Contains("name"))
                    {
                        text.text = rune.GetDisplayName();
                    }
                    else if (text.name.ToLower().Contains("amount"))
                    {
                        text.text = "1";
                    }
                }

                rewardItem.transform.localScale = Vector3.zero;
                rewardItem.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

                yield return new WaitForSeconds(rewardItemDelay);
            }
        }
    }

    private IEnumerator ShowXPAnimation()
    {
        if (xpAnimationPanel != null)
        {
            xpAnimationPanel.SetActive(true);
            yield return null;
        }

        if (rewardItemsContainer?.parent != null)
        {
            rewardItemsContainer.parent.gameObject.SetActive(false);
        }

        if (BattleDataManager.Instance != null && BattleDataManager.Instance.HasValidBattleData())
        {
            var battleData = BattleDataManager.Instance.GetCurrentBattleData();
            var selectedTeamIDs = battleData.selectedTeamIDs;

            if (selectedTeamIDs != null && selectedTeamIDs.Count > 0)
            {
                int xpPerMonster = currentRewards.experienceEarned / selectedTeamIDs.Count;
                yield return StartCoroutine(AnimateMonsterXPGains(selectedTeamIDs, xpPerMonster));
            }
        }

        if (xpAnimationPanel != null)
        {
            xpAnimationPanel.SetActive(false);
        }

        if (rewardItemsContainer?.parent != null)
        {
            rewardItemsContainer.parent.gameObject.SetActive(true);
        }

        ApplyXPRewardsToPlayer();
    }

    private IEnumerator AnimateMonsterXPGains(List<string> monsterIDs, int xpPerMonster)
    {
        List<XPMonsterCard> xpCards = new List<XPMonsterCard>();

        if (monsterXPContainer != null)
        {
            monsterXPContainer.gameObject.SetActive(true);

            foreach (Transform child in monsterXPContainer)
            {
                Destroy(child.gameObject);
            }
        }

        yield return null;

        for (int i = 0; i < monsterIDs.Count; i++)
        {
            string monsterID = monsterIDs[i];
            var monster = MonsterCollectionManager.Instance?.GetMonsterByID(monsterID);

            if (monster != null && xpMonsterCardPrefab != null && monsterXPContainer != null)
            {
                GameObject cardObj = Instantiate(xpMonsterCardPrefab, monsterXPContainer);
                cardObj.SetActive(true);

                Transform[] allChildren = cardObj.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    child.gameObject.SetActive(true);
                }

                XPMonsterCard xpCard = cardObj.GetComponent<XPMonsterCard>();
                if (xpCard != null)
                {
                    xpCard.Setup(monster, xpPerMonster, xpAnimationDuration);
                    xpCards.Add(xpCard);

                    cardObj.transform.localScale = Vector3.zero;
                    cardObj.transform.DOScale(Vector3.one, 0.5f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(i * 0.2f);
                }
            }
        }

        yield return new WaitForSeconds(monsterIDs.Count * 0.2f + 1.0f);

        List<Coroutine> xpAnimations = new List<Coroutine>();

        for (int i = 0; i < xpCards.Count; i++)
        {
            var xpCard = xpCards[i];

            if (xpCard != null && xpCard.gameObject.activeInHierarchy)
            {
                Coroutine animation = StartCoroutine(xpCard.AnimateXPGain());
                xpAnimations.Add(animation);
            }

            yield return new WaitForSeconds(monsterCardSpacing);
        }

        foreach (var animation in xpAnimations)
        {
            if (animation != null)
            {
                yield return animation;
            }
        }
    }

    private void EnableClaimButton()
    {
        if (claimRewardsButton != null)
        {
            claimRewardsButton.interactable = true;
        }

        if (claimButtonText != null)
        {
            claimButtonText.text = "Claim Rewards";
        }
    }

    private void ClaimRewardsAndExit()
    {
        if (rewardsClaimed) return;

        rewardsClaimed = true;
        ApplyNonXPRewardsToPlayer();
        UpdateBattleProgression();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        OnRewardsClaimed?.Invoke();
        StartCoroutine(ReturnToWorldMap());
    }

    private void ApplyNonXPRewardsToPlayer()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddSoulCoins(currentRewards.soulCoinsEarned);
        }

        if (RuneCollectionManager.Instance != null && currentRewards.runesEarned != null)
        {
            foreach (var rune in currentRewards.runesEarned)
            {
                RuneCollectionManager.Instance.AddRune(rune);
            }
        }
    }

    private void ApplyXPRewardsToPlayer()
    {
        if (MonsterCollectionManager.Instance != null && currentRewards.experienceEarned > 0)
        {
            if (BattleDataManager.Instance != null && BattleDataManager.Instance.HasValidBattleData())
            {
                var battleData = BattleDataManager.Instance.GetCurrentBattleData();
                var selectedTeamIDs = battleData.selectedTeamIDs;

                if (selectedTeamIDs != null && selectedTeamIDs.Count > 0)
                {
                    int xpPerMonster = currentRewards.experienceEarned / selectedTeamIDs.Count;

                    foreach (string monsterID in selectedTeamIDs)
                    {
                        if (!string.IsNullOrEmpty(monsterID))
                        {
                            MonsterCollectionManager.Instance.AddExperienceToMonster(monsterID, xpPerMonster);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Update battle progression using PlayerPrefs (no ProgressionManager dependency)
    /// </summary>
    private void UpdateBattleProgression()
    {
        if (currentBattleData?.combatTemplate != null)
        {
            string battleName = currentBattleData.combatTemplate.combatName;

            // Save best star rating
            string starKey = $"Battle_{battleName}_Stars";
            int currentBestStars = PlayerPrefs.GetInt(starKey, 0);

            if (starsEarned > currentBestStars)
            {
                PlayerPrefs.SetInt(starKey, starsEarned);
            }

            // Mark battle as completed
            string completionKey = $"Battle_{battleName}_Completed";
            PlayerPrefs.SetInt(completionKey, 1);

            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Return to world map (no SceneTransitionManager dependency)
    /// </summary>
    private IEnumerator ReturnToWorldMap()
    {
        yield return new WaitForSeconds(1f);

        try
        {
            // Try primary scene name
            UnityEngine.SceneManagement.SceneManager.LoadScene(worldMapSceneName);
        }
        catch
        {
            try
            {
                // Try fallback scene name
                UnityEngine.SceneManagement.SceneManager.LoadScene(fallbackSceneName);
            }
            catch
            {
                // Final fallback: scene index 0
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }
    }
}
