using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// XP Animation card that displays a monster and animates XP gain (FIXED VERSION)
/// </summary>
public class XPMonsterCard : MonoBehaviour
{
    [Header("UI References")]
    public Transform monsterCardContainer;
    public Slider xpSlider;
    public Image xpSliderFill;
    public TextMeshProUGUI xpGainText;
    public TextMeshProUGUI xpProgressText;
    public GameObject levelUpEffect;

    [Header("Animation Settings")]
    public float xpAnimationDuration = 2.0f;
    public AnimationCurve xpAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Private data
    private CollectedMonster monster;
    private int xpToAdd;
    private int startingXP;
    private int startingLevel;
    private bool willLevelUp;
    private VictoryRewardManager rewardManager;

    // UniversalMonsterCard instance
    private UniversalMonsterCard monsterCard;
    private GameObject monsterCardInstance;

    /// <summary>
    /// Setup the XP card with monster data
    /// </summary>
    public void Setup(CollectedMonster monster, int xpAmount, float animationDuration)
    {
        this.monster = monster;
        this.xpToAdd = xpAmount;
        this.xpAnimationDuration = animationDuration;
        this.startingXP = monster.currentExperience;
        this.startingLevel = monster.currentLevel;
        this.rewardManager = FindFirstObjectByType<VictoryRewardManager>();

        AutoFindUIComponents();
        SetupUniversalMonsterCard();
        SetupXPDisplay();
        CalculateLevelUpPrediction();
    }

    /// <summary>
    /// Auto-find UI components with extensive debugging
    /// </summary>
    private void AutoFindUIComponents()
    {
        // Find monster card container
        if (monsterCardContainer == null)
        {
            Transform container = transform.Find("MonsterCardContainer");
            monsterCardContainer = container ?? transform;
        }

        // ✅ CRITICAL: Find XP slider with multiple fallbacks
        if (xpSlider == null)
        {
            // Method 1: GetComponentInChildren
            xpSlider = GetComponentInChildren<Slider>();

            if (xpSlider == null)
            {
                // Method 2: Search by name patterns
                string[] sliderNames = { "XPSlider", "ExperienceSlider", "EXPSlider", "Slider" };
                foreach (string name in sliderNames)
                {
                    Transform sliderTransform = transform.Find(name) ??
                                              transform.Find($"XPInfoPanel/{name}") ??
                                              transform.Find($"UI/{name}");
                    if (sliderTransform != null)
                    {
                        xpSlider = sliderTransform.GetComponent<Slider>();
                        if (xpSlider != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (xpSlider == null)
            {
                // Method 3: Search all children recursively
                Slider[] allSliders = GetComponentsInChildren<Slider>(true);
                if (allSliders.Length > 0)
                {
                    xpSlider = allSliders[0];
                }
            }
        }

        // ✅ Find XP slider fill
        if (xpSliderFill == null && xpSlider != null)
        {
            if (xpSlider.fillRect != null)
            {
                xpSliderFill = xpSlider.fillRect.GetComponent<Image>();
            }

            if (xpSliderFill == null)
            {
                Transform fillArea = xpSlider.transform.Find("Fill Area");
                if (fillArea != null)
                {
                    Transform fill = fillArea.Find("Fill");
                    if (fill != null)
                    {
                        xpSliderFill = fill.GetComponent<Image>();
                    }
                }
            }
        }

        // Find text components
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var text in texts)
        {
            string textName = text.name.ToLower();

            if (textName.Contains("xpgain") && xpGainText == null)
            {
                xpGainText = text;
            }
            else if (textName.Contains("xpprogress") && xpProgressText == null)
            {
                xpProgressText = text;
            }
        }
    }

    /// <summary>
    /// Create and setup the UniversalMonsterCard instance
    /// </summary>
    private void SetupUniversalMonsterCard()
    {
        if (rewardManager?.universalMonsterCardPrefab == null)
        {
            return;
        }

        if (monsterCardContainer == null)
        {
            return;
        }

        if (monsterCardInstance != null)
        {
            DestroyImmediate(monsterCardInstance);
        }

        monsterCardInstance = Instantiate(rewardManager.universalMonsterCardPrefab, monsterCardContainer);
        monsterCard = monsterCardInstance.GetComponent<UniversalMonsterCard>();

        if (monsterCard != null)
        {
            // Use working setup from MonsterInventoryUI pattern
            SetupMonsterCardManually();

            Button cardButton = monsterCardInstance.GetComponent<Button>();
            if (cardButton != null)
            {
                cardButton.interactable = false;
            }

            monsterCardInstance.transform.localScale = Vector3.one * 0.9f;

            RectTransform cardRect = monsterCardInstance.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.sizeDelta = new Vector2(250, 120);
            }
        }
    }

    /// <summary>
    /// Manual setup of monster card display
    /// </summary>
    private void SetupMonsterCardManually()
    {
        if (monsterCardInstance == null || monster?.monsterData == null) return;

        var nameTexts = monsterCardInstance.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var nameText in nameTexts)
        {
            if (nameText.name.ToLower().Contains("name"))
            {
                nameText.text = monster.monsterData.monsterName;
                break;
            }
        }

        foreach (var levelText in nameTexts)
        {
            if (levelText.name.ToLower().Contains("level"))
            {
                levelText.text = $"Level {monster.currentLevel}";
                break;
            }
        }

        var starDisplay = monsterCardInstance.GetComponentInChildren<StarDisplay>();
        if (starDisplay != null)
        {
            starDisplay.SetStarLevel(monster.currentStarLevel);
        }

        var images = monsterCardInstance.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            if (image.name.ToLower().Contains("icon") || image.name.ToLower().Contains("monster"))
            {
                image.color = GetElementColor(monster.monsterData.element);
                break;
            }
        }
    }

    /// <summary>
    /// Setup XP display with working implementation from MonsterInventoryUI
    /// </summary>
    private void SetupXPDisplay()
    {
        // Setup XP gain text
        if (xpGainText != null)
        {
            xpGainText.text = $"+{xpToAdd} XP";
            xpGainText.color = Color.yellow;
        }

        // ✅ Setup XP slider using MonsterInventoryUI logic
        if (xpSlider != null)
        {
            // Get experience data (same as MonsterInventoryUI)
            int currentXP = monster.currentExperience;
            int requiredXP = monster.GetExperienceRequiredForNextLevel();
            float xpProgress = monster.GetExperienceProgress();

            // Set slider properties
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;
            xpSlider.value = xpProgress;

            // Test immediate slider update
            StartCoroutine(TestSliderUpdate());

            // Set slider color
            if (xpSliderFill != null)
            {
                Color initialColor = GetExperienceBarColor(xpProgress);
                xpSliderFill.color = initialColor;
            }
        }

        // Setup XP progress text
        UpdateXPProgressText(startingXP, monster.GetExperienceRequiredForNextLevel());
    }

    /// <summary>
    /// Test slider update to verify it's working
    /// </summary>
    private IEnumerator TestSliderUpdate()
    {
        if (xpSlider == null) yield break;

        float originalValue = xpSlider.value;

        // Test animation from 0 to 1
        float testDuration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < testDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / testDuration;

            xpSlider.value = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        // Reset to original
        xpSlider.value = originalValue;
    }

    /// <summary>
    /// Calculate if monster will level up and how many times (ENHANCED)
    /// </summary>
    private void CalculateLevelUpPrediction()
    {
        int totalXPAfter = startingXP + xpToAdd;
        int currentXP = startingXP;
        int currentLevel = startingLevel;
        int levelUpsCount = 0;

        // Simulate level ups
        while (currentLevel < monster.GetMaxLevel())
        {
            int requiredXP = monster.GetExperienceRequiredForNextLevel();

            if (totalXPAfter >= requiredXP)
            {
                levelUpsCount++;
                currentLevel++;
            }
            else
            {
                break;
            }
        }

        willLevelUp = levelUpsCount > 0;
    }


    /// <summary>
    /// Animate the XP gain with level up reset functionality
    /// </summary>
    public IEnumerator AnimateXPGain()
    {
        if (!gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (monster?.monsterData == null)
        {
            yield break;
        }
        // Play XP gain sound
        if (rewardManager?.xpGainSound != null)
        {
            AudioSource.PlayClipAtPoint(rewardManager.xpGainSound, Camera.main.transform.position, 0.5f);
        }

        // Animate XP text appearance
        if (xpGainText != null)
        {
            xpGainText.transform.localScale = Vector3.zero;
            xpGainText.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(0.5f);

        // ✅ NEW: Level-aware XP animation with reset on level up
        yield return StartCoroutine(AnimateXPWithLevelUps());
    }

    /// <summary>
    /// Animate XP with automatic level up detection and slider reset
    /// </summary>
    private IEnumerator AnimateXPWithLevelUps()
    {
        if (xpSlider == null)
        {
            yield break;
        }

        int remainingXP = xpToAdd;
        int currentXP = startingXP;
        int currentLevel = startingLevel;
        bool hasLeveledUp = false;

        while (remainingXP > 0)
        {
            // Get current level requirements
            int requiredXPForNextLevel = monster.GetExperienceRequiredForNextLevel();
            int currentXPInLevel = currentXP - GetExperienceRequiredForCurrentLevel(currentLevel);
            int xpRangeForLevel = requiredXPForNextLevel - GetExperienceRequiredForCurrentLevel(currentLevel);

            // Calculate how much XP we can add before hitting next level
            int xpUntilNextLevel = requiredXPForNextLevel - currentXP;
            int xpToAnimateThisLevel = Mathf.Min(remainingXP, xpUntilNextLevel);

            // Calculate starting and ending slider values for this level
            float startSliderValue = (float)currentXPInLevel / xpRangeForLevel;
            float endSliderValue = (float)(currentXPInLevel + xpToAnimateThisLevel) / xpRangeForLevel;

            startSliderValue = Mathf.Clamp01(startSliderValue);
            endSliderValue = Mathf.Clamp01(endSliderValue);

            // Set initial slider value for this level
            xpSlider.value = startSliderValue;

            // Animate XP for this level segment
            float levelAnimationDuration = (xpAnimationDuration * xpToAnimateThisLevel) / xpToAdd;
            levelAnimationDuration = Mathf.Max(0.5f, levelAnimationDuration); // Minimum duration

            yield return StartCoroutine(AnimateSingleLevelSegment(
                currentXP,
                currentXP + xpToAnimateThisLevel,
                startSliderValue,
                endSliderValue,
                levelAnimationDuration,
                currentLevel
            ));

            // Update values
            currentXP += xpToAnimateThisLevel;
            remainingXP -= xpToAnimateThisLevel;

            // Check if we reached next level
            if (currentXP >= requiredXPForNextLevel && currentLevel < monster.GetMaxLevel())
            {
                // ✅ LEVEL UP!
                currentLevel++;
                hasLeveledUp = true;

                // Show level up animation
                yield return StartCoroutine(ShowLevelUpAnimation(currentLevel));

                // ✅ RESET SLIDER TO 0 for next level
                if (remainingXP > 0) // Only reset if we have more XP to animate
                {
                    xpSlider.value = 0f;

                    // Brief pause to show the reset
                    yield return new WaitForSeconds(0.3f);

                    // Update XP progress text for new level
                    UpdateXPProgressText(currentXP, monster.GetExperienceRequiredForNextLevel());
                }
            }

            // Safety check to prevent infinite loops
            if (remainingXP > 0 && xpToAnimateThisLevel == 0)
            {
                break;
            }
        }

        // Final values
        UpdateXPProgressText(currentXP, monster.GetExperienceRequiredForNextLevel());

        // If no level ups occurred during animation, show level up effects at the end
        if (willLevelUp && !hasLeveledUp)
        {
            yield return StartCoroutine(ShowLevelUpAnimation(currentLevel + 1));
        }
    }

    /// <summary>
    /// Animate a single level segment of XP gain
    /// </summary>
    private IEnumerator AnimateSingleLevelSegment(int startXP, int endXP, float startSliderValue, float endSliderValue, float duration, int level)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curvedProgress = xpAnimationCurve.Evaluate(progress);

            // Interpolate XP and slider values
            int currentDisplayXP = Mathf.RoundToInt(Mathf.Lerp(startXP, endXP, curvedProgress));
            float currentSliderValue = Mathf.Lerp(startSliderValue, endSliderValue, curvedProgress);

            // Update slider
            xpSlider.value = currentSliderValue;

            // Update slider color
            if (xpSliderFill != null)
            {
                xpSliderFill.color = GetExperienceBarColor(currentSliderValue);
            }

            // Update XP text
            UpdateXPProgressText(currentDisplayXP, monster.GetExperienceRequiredForNextLevel());

            yield return null;
        }

        // Ensure final values
        xpSlider.value = endSliderValue;
        UpdateXPProgressText(endXP, monster.GetExperienceRequiredForNextLevel());
    }

    /// <summary>
    /// Show level up animation with level parameter
    /// </summary>
    private IEnumerator ShowLevelUpAnimation(int newLevel)
    {
        // Play level up sound
        if (rewardManager?.levelUpSound != null)
        {
            AudioSource.PlayClipAtPoint(rewardManager.levelUpSound, Camera.main.transform.position, 0.7f);
        }

        // ✅ Update the UniversalMonsterCard level display
        if (monsterCardInstance != null)
        {
            var levelTexts = monsterCardInstance.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in levelTexts)
            {
                if (text.name.ToLower().Contains("level") || text.text.Contains("Level"))
                {
                    text.text = $"Level {newLevel}";

                    // Animate level text
                    text.transform.DOPunchScale(Vector3.one * 0.3f, 0.6f, 10, 1f);
                    text.DOColor(Color.yellow, 0.3f).SetLoops(2, LoopType.Yoyo);
                    break;
                }
            }
        }

        // Show level up effect
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(true);
            yield return new WaitForSeconds(1.0f);
            levelUpEffect.SetActive(false);
        }

        // Create level up popup
        if (rewardManager?.levelUpPopupPrefab != null)
        {
            GameObject popup = Instantiate(rewardManager.levelUpPopupPrefab, transform.parent);
            popup.transform.position = transform.position + Vector3.up * 100f;

            // Animate popup
            popup.transform.localScale = Vector3.zero;
            popup.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

            // Auto-destroy popup
            Destroy(popup, 2.0f);
        }

        yield return new WaitForSeconds(0.5f); // Shorter delay for smoother multi-level ups
    }

    /// <summary>
    /// Get experience required for a specific level (helper method)
    /// </summary>
    private int GetExperienceRequiredForCurrentLevel(int level)
    {
        if (level <= 1)
            return 0;

        // Calculate XP required for current level
        int totalXP = 0;
        for (int i = 1; i < level; i++)
        {
            totalXP += GetExperienceRequiredForLevel(i);
        }

        return totalXP;
    }

    /// <summary>
    /// Get experience required for a specific level
    /// </summary>
    private int GetExperienceRequiredForLevel(int level)
    {
        // Standard RPG XP formula: baseXP * level^1.5
        int baseXP = 100;
        return Mathf.RoundToInt(baseXP * Mathf.Pow(level, 1.5f));
    }

    /// <summary>
    /// Update XP progress text display
    /// </summary>
    private void UpdateXPProgressText(int currentXP, int requiredXP)
    {
        if (xpProgressText != null)
        {
            xpProgressText.text = $"{currentXP:N0} / {requiredXP:N0} XP";
        }
    }

    /// <summary>
    /// Get color based on XP progress (same as MonsterInventoryUI)
    /// </summary>
    private Color GetExperienceBarColor(float progress)
    {
        if (progress < 0.3f)
            return Color.Lerp(Color.blue, Color.cyan, progress / 0.3f);
        else if (progress < 0.7f)
            return Color.Lerp(Color.cyan, Color.yellow, (progress - 0.3f) / 0.4f);
        else
            return Color.Lerp(Color.yellow, Color.green, (progress - 0.7f) / 0.3f);
    }

    /// <summary>
    /// Get color based on monster element
    /// </summary>
    private Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return new Color(1f, 0.3f, 0.3f);
            case ElementType.Water: return new Color(0.3f, 0.6f, 1f);
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f);
            case ElementType.Light: return new Color(1f, 1f, 0.6f);
            case ElementType.Dark: return new Color(0.6f, 0.3f, 0.8f);
            default: return Color.white;
        }
    }

    /// <summary>
    /// Clean up when destroyed
    /// </summary>
    private void OnDestroy()
    {
        transform.DOKill();
        if (monsterCardInstance != null)
        {
            monsterCardInstance.transform.DOKill();
        }
    }

    public void TestSliderAnimation()
    {
        if (xpSlider != null)
        {
            StartCoroutine(TestSliderAnimationCoroutine());
        }
    }

    private IEnumerator TestSliderAnimationCoroutine()
    {
        float duration = 2f;
        xpSlider.value = 0f;

        xpSlider.DOValue(1f, duration).OnUpdate(() => {});

        yield return new WaitForSeconds(duration);
    }
}
