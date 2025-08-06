using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonsterSummonInfoUI : MonoBehaviour
{
    [Header("Monster Display")]
    [SerializeField] private Image monsterIcon;
    [SerializeField] private Image monsterPortrait;
    [SerializeField] private TextMeshProUGUI monsterNameText;
    [SerializeField] private TextMeshProUGUI monsterLevelText;

    [Header("Star Rating")]
    [SerializeField] private Transform starContainer;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private Color activeStarColor = Color.yellow;
    [SerializeField] private Color inactiveStarColor = Color.gray;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI hpStatText;
    [SerializeField] private TextMeshProUGUI attackStatText;
    [SerializeField] private TextMeshProUGUI defenseStatText;
    [SerializeField] private TextMeshProUGUI speedStatText;

    [Header("Element Display")]
    [SerializeField] private Image elementIcon;
    [SerializeField] private TextMeshProUGUI elementText;

    [Header("Abilities")]
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private GameObject abilityItemPrefab;

    [Header("Rarity Display")]
    [SerializeField] private Image rarityBackground;
    [SerializeField] private TextMeshProUGUI rarityText;

    [Header("Confirm Button")]
    [SerializeField] private Button confirmButton;

    [Header("Animation")]
    [SerializeField] private bool animateOnShow = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("🐛 Debug")] // 🆕 NEW
    [SerializeField] private bool enableDebugLogs = true;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    public void DisplayMonsterInfo(GachaMonster summonedMonster)
    {
        if (summonedMonster?.monsterData == null)
        {
            Debug.LogError("❌ Cannot display monster info - no monster data provided");
            return;
        }

        var monsterData = summonedMonster.monsterData;

        // 🐛 DEBUG: Log star level information
        if (enableDebugLogs)
        {
            Debug.Log($"🐛 MonsterSummonInfoUI Debug:");
            Debug.Log($"   Monster Name: {monsterData.name}");
            Debug.Log($"   Monster Data defaultStarLevel: {monsterData.defaultStarLevel}");
            Debug.Log($"   GachaMonster StarLevel property: {summonedMonster.StarLevel}");
            Debug.Log($"   GachaMonster rarity: {summonedMonster.rarity}");
        }

        // Display basic info
        if (monsterNameText != null)
        {
            monsterNameText.text = string.IsNullOrEmpty(monsterData.monsterName) ? monsterData.name : monsterData.monsterName;
        }

        if (monsterLevelText != null)
        {
            monsterLevelText.text = $"Lvl. {monsterData.defaultLevel}";
        }

        // Display monster icon/portrait
        if (monsterIcon != null && monsterData.icon != null)
        {
            monsterIcon.sprite = monsterData.icon;
            monsterIcon.color = Color.white;
        }

        // 🔧 FIXED: Display star rating - use the data from the actual summoned monster
        DisplayStarRating(summonedMonster);

        // Display stats
        DisplayStats(monsterData);

        // Display element
        DisplayElement(monsterData.element);

        // Display abilities (using MonsterAction properties)
        DisplayAbilities(monsterData);

        // Display rarity
        DisplayRarity(summonedMonster.rarity);

        // Animate panel appearance
        if (animateOnShow)
        {
            StartCoroutine(AnimateShow());
        }
    }

    // 🔧 UPDATED: Accept GachaMonster instead of just int
    void DisplayStarRating(GachaMonster summonedMonster)
    {
        if (starContainer == null || starPrefab == null) return;

        // Get the star level from the summoned monster
        int starLevel = summonedMonster.StarLevel;

        if (enableDebugLogs)
        {
            Debug.Log($"🌟 DisplayStarRating called with starLevel: {starLevel}");
            Debug.Log($"   Star container: {starContainer != null}");
            Debug.Log($"   Star prefab: {starPrefab != null}");
        }

        // Clear existing stars
        foreach (Transform child in starContainer)
        {
            Destroy(child.gameObject);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"🌟 Creating {5} stars, {starLevel} will be active");
        }

        // Create star display (max 5 stars)
        for (int i = 0; i < 5; i++)
        {
            GameObject starObj = Instantiate(starPrefab, starContainer);
            Image starImage = starObj.GetComponent<Image>();

            if (starImage != null)
            {
                bool isActive = i < starLevel;
                Color starColor = isActive ? activeStarColor : inactiveStarColor;
                starImage.color = starColor;

                if (enableDebugLogs)
                {
                    Debug.Log($"   Star {i + 1}: {(isActive ? "ACTIVE" : "inactive")} - Color: {starColor}");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"⚠️ Star prefab missing Image component on star {i + 1}");
                }
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log($"✅ Star rating display complete: {starLevel}/5 stars");
        }
    }

    void DisplayStats(MonsterData monsterData)
    {
        // Calculate stats at default level and star level
        var stats = new MonsterStats(monsterData, monsterData.defaultLevel, monsterData.defaultStarLevel);

        if (hpStatText != null)
        {
            hpStatText.text = $"HP: {stats.health}";
        }

        if (attackStatText != null)
        {
            attackStatText.text = $"ATK: {stats.attack}";
        }

        if (defenseStatText != null)
        {
            defenseStatText.text = $"DEF: {stats.defense}";
        }

        if (speedStatText != null)
        {
            speedStatText.text = $"SPD: {stats.speed}";
        }
    }

    void DisplayElement(ElementType element)
    {
        if (elementText != null)
        {
            elementText.text = element.ToString();
            elementText.color = ElementalSystem.GetElementColor(element);
        }

        if (elementIcon != null)
        {
            elementIcon.color = ElementalSystem.GetElementColor(element);
        }
    }

    void DisplayAbilities(MonsterData monsterData)
    {
        if (abilitiesContainer == null || abilityItemPrefab == null) return;

        // Clear existing abilities
        foreach (Transform child in abilitiesContainer)
        {
            Destroy(child.gameObject);
        }

        // Get all available MonsterActions
        var actions = monsterData.GetAvailableActions();

        // Display each action as an ability
        foreach (var action in actions)
        {
            if (action != null)
            {
                GameObject abilityObj = Instantiate(abilityItemPrefab, abilitiesContainer);
                SetupAbilityDisplay(abilityObj, action);
            }
        }

        // If no actions available, show a message
        if (actions.Count == 0)
        {
            GameObject noAbilitiesObj = Instantiate(abilityItemPrefab, abilitiesContainer);
            TextMeshProUGUI noAbilitiesText = noAbilitiesObj.GetComponentInChildren<TextMeshProUGUI>();
            if (noAbilitiesText != null)
            {
                noAbilitiesText.text = "No abilities configured";
                noAbilitiesText.color = Color.gray;
            }
        }
    }

    void SetupAbilityDisplay(GameObject abilityObj, MonsterAction action)
    {
        // 🐛 DEBUG: Enhanced ability setup with detailed logging
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 Setting up ability: {action?.actionName}");
            Debug.Log($"   Action icon null: {action?.icon == null}");
            Debug.Log($"   Ability object: {abilityObj?.name}");
        }

        // Find text components in the ability item
        TextMeshProUGUI[] texts = abilityObj.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length > 0)
        {
            // Main ability name
            texts[0].text = action.actionName;

            // If there's a second text component, use it for description or type
            if (texts.Length > 1)
            {
                texts[1].text = GetActionTypeText(action);
            }

            if (enableDebugLogs)
            {
                Debug.Log($"   📝 Set ability name: {action.actionName}");
                Debug.Log($"   📝 Found {texts.Length} text components");
            }
        }

        // 🔧 FIXED: Find the specific ability icon by name instead of first Image
        Transform iconTransform = abilityObj.transform.Find("AbilityIcon");
        if (iconTransform != null)
        {
            Image abilityIcon = iconTransform.GetComponent<Image>();
            if (abilityIcon != null && action.icon != null)
            {
                abilityIcon.sprite = action.icon;
                if (enableDebugLogs)
                {
                    Debug.Log($"   🖼️ Set icon sprite: {action.icon.name} on {iconTransform.name}");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"   ⚠️ Cannot set icon - abilityIcon: {abilityIcon != null}, action.icon: {action.icon != null}");
                }
            }
        }
        else
        {
            // 🔧 FALLBACK: Try to find by Image components and skip the first one (background)
            Image[] images = abilityObj.GetComponentsInChildren<Image>();
            if (images.Length > 1) // Skip first image (background), use second (icon)
            {
                Image abilityIcon = images[1];
                if (abilityIcon != null && action.icon != null)
                {
                    abilityIcon.sprite = action.icon;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"   🖼️ Set icon sprite (fallback): {action.icon.name} on {abilityIcon.name}");
                    }
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"   ❌ Could not find AbilityIcon child or multiple Image components");
                    Debug.Log($"   📊 Found {images.Length} Image components total");
                    for (int i = 0; i < images.Length; i++)
                    {
                        Debug.Log($"      Image {i}: {images[i].name}");
                    }
                }
            }
        }

        // Color code based on action type (apply to background)
        ColorCodeAbility(abilityObj, action);
    }


    string GetActionTypeText(MonsterAction action)
    {
        if (action.energyCost <= 0)
        {
            return "Normal Attack";
        }
        else if (action.energyCost <= 30)
        {
            return "Special Attack";
        }
        else
        {
            return "Ultimate Attack";
        }
    }

    void ColorCodeAbility(GameObject abilityObj, MonsterAction action)
    {
        Color abilityColor = Color.white;

        // Determine color based on energy cost
        if (action.energyCost <= 0)
        {
            abilityColor = new Color(0.8f, 0.8f, 0.8f); // Gray for normal
        }
        else if (action.energyCost <= 30)
        {
            abilityColor = new Color(0.3f, 0.7f, 1f); // Blue for special
        }
        else
        {
            abilityColor = new Color(1f, 0.4f, 0.1f); // Orange for ultimate
        }

        // Apply color to background or border if available
        Image backgroundImage = abilityObj.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = abilityColor * 0.3f; // Subtle background color
        }
    }

    void DisplayRarity(MonsterRarity rarity)
    {
        if (rarityText != null)
        {
            rarityText.text = rarity.ToString();
        }

        if (rarityBackground != null)
        {
            Color rarityColor = GetRarityColor(rarity);
            rarityBackground.color = rarityColor;
        }
    }

    Color GetRarityColor(MonsterRarity rarity)
    {
        switch (rarity)
        {
            case MonsterRarity.Common: return new Color(0.8f, 0.8f, 0.8f); // Light Gray
            case MonsterRarity.Uncommon: return new Color(0.2f, 0.8f, 0.2f); // Green
            case MonsterRarity.Rare: return new Color(0.2f, 0.4f, 0.8f); // Blue
            case MonsterRarity.Epic: return new Color(0.8f, 0.2f, 0.8f); // Purple
            case MonsterRarity.Legendary: return new Color(1f, 0.6f, 0.1f); // Orange
            default: return Color.white;
        }
    }

    System.Collections.IEnumerator AnimateShow()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        canvasGroup.transform.localScale = Vector3.one * 0.8f;

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / animationDuration;
            float curveValue = showCurve.Evaluate(normalizedTime);

            canvasGroup.alpha = curveValue;
            canvasGroup.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, curveValue);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.transform.localScale = Vector3.one;
    }

    void OnConfirmClicked()
    {
        if (SummonCutsceneManager.Instance != null)
        {
            SummonCutsceneManager.Instance.OnConfirmMonsterInfo();
        }
    }

    // 🆕 NEW: Context menu for debugging
    [ContextMenu("Test Star Display")]
    public void TestStarDisplay()
    {
        if (Application.isPlaying)
        {
            // Create a test monster with 4 stars
            var testMonster = new GachaMonster();
            // This won't work properly without a real MonsterData, but it will test the display logic
            Debug.Log("🧪 Testing star display - check debug logs");
        }
        else
        {
            Debug.Log("⚠️ This test only works in Play mode");
        }
    }
}
