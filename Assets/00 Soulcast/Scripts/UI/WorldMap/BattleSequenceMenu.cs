using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BattleSequenceMenu : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private RectTransform contentPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backToLevelsButton;
    [SerializeField] private TextMeshProUGUI levelTitle;

    [Header("Battle Sequence Buttons")]
    [SerializeField] private BattleSequenceButton[] battleButtons;

    [Header("Animation Settings")]
    [SerializeField] private float slideInDuration = 0.5f;
    [SerializeField] private float slideOutDuration = 0.4f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("Menu Position")]
    [SerializeField] private Vector2 hiddenPosition = new Vector2(1920f, 0f);
    [SerializeField] private Vector2 visiblePosition = new Vector2(0f, 0f);

    private int currentLevel = -1;
    private int currentRegion = -1;
    private bool isAnimating = false;
    private CanvasGroup canvasGroup;
    private WorldMapManager worldMapManager;

    public System.Action OnMenuClosed;

    private void Awake()
    {
        InitializeComponents();
        SetupEventHandlers();

        // Start hidden maar houd GameObject actief voor coroutines
        HideMenuInstant();
    }

    private void InitializeComponents()
    {
        // Find WorldMapManager voor coroutine backup
        worldMapManager = FindFirstObjectByType<WorldMapManager>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Auto-assign menuPanel if not set
        if (menuPanel == null)
            menuPanel = transform.GetChild(0)?.gameObject;

        // Setup battle buttons
        for (int i = 0; i < battleButtons.Length; i++)
        {
            if (battleButtons[i] != null)
            {
                int battleIndex = i; // Local copy for closure
                battleButtons[i].SetBattleData(battleIndex + 1);
                battleButtons[i].OnBattleSequenceSelected += () => SelectBattleSequence(battleIndex + 1);
            }
        }
    }

    private void SetupEventHandlers()
    {
        closeButton?.onClick.AddListener(HideMenu);
        backToLevelsButton?.onClick.AddListener(HideMenu);
    }

    public void ShowMenu(int regionId, int levelId)
    {
        if (isAnimating) return;

        currentRegion = regionId;
        currentLevel = levelId;

        Debug.Log($"ShowMenu called for Region {regionId}, Level {levelId}");

        // Update UI
        UpdateMenuContent();

        // Start coroutine via active manager or keep this GameObject active
        if (worldMapManager != null && worldMapManager.gameObject.activeInHierarchy)
        {
            worldMapManager.StartCoroutine(ShowMenuCoroutine());
        }
        else
        {
            // Ensure this GameObject is active
            gameObject.SetActive(true);
            StartCoroutine(ShowMenuCoroutine());
        }
    }

    public void HideMenu()
    {
        if (isAnimating) return;

        if (worldMapManager != null && worldMapManager.gameObject.activeInHierarchy)
        {
            worldMapManager.StartCoroutine(HideMenuCoroutine());
        }
        else
        {
            StartCoroutine(HideMenuCoroutine());
        }
    }

    private void UpdateMenuContent()
    {
        // Update title
        if (levelTitle != null)
            levelTitle.text = $"Regio {currentRegion} - Level {currentLevel}";

        // Update battle buttons based on progress
        UpdateBattleButtonStates();
    }

    private void UpdateBattleButtonStates()
    {
        for (int i = 0; i < battleButtons.Length; i++)
        {
            if (battleButtons[i] != null)
            {
                string battleKey = $"Region_{currentRegion}_Level_{currentLevel}_Battle_{i + 1}";

                bool isCompleted = PlayerPrefs.GetInt($"{battleKey}_Completed", 0) == 1;
                bool isUnlocked = i == 0 || PlayerPrefs.GetInt($"Region_{currentRegion}_Level_{currentLevel}_Battle_{i}_Completed", 0) == 1;
                int stars = PlayerPrefs.GetInt($"{battleKey}_Stars", 0);

                battleButtons[i].UpdateBattleState(isUnlocked, isCompleted, stars);
            }
        }
    }

    private void SelectBattleSequence(int battleSequenceId)
    {
        Debug.Log($"Starting Battle Sequence {battleSequenceId} in Region {currentRegion}, Level {currentLevel}");

        // ✅ STEP 1: Get the combat template from database
        var database = Resources.Load<LevelDatabase>("Battle/Database/BattleDatabase");
        if (database == null)
        {
            Debug.LogError("LevelDatabase not found! Make sure BattleDatabase.asset exists in Resources/Battle/Database/");
            return;
        }

        var combatTemplate = database.GetBattleConfiguration(currentRegion, currentLevel, battleSequenceId);
        if (combatTemplate == null)
        {
            Debug.LogError($"No combat template found for Region {currentRegion}, Level {currentLevel}, Combat {battleSequenceId}");
            return;
        }

        // ✅ STEP 2: Find PreBattleTeamSelection and show it
        var teamSelection = FindFirstObjectByType<PreBattleTeamSelection>();
        if (teamSelection == null)
        {
            Debug.LogError("PreBattleTeamSelection not found in scene! Make sure it exists in the WorldMap scene.");
            return;
        }

        // ✅ STEP 3: Save battle data for later use
        PlayerPrefs.SetInt("CurrentRegion", currentRegion);
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CurrentBattleSequence", battleSequenceId);
        PlayerPrefs.Save();

        // ✅ STEP 4: Show team selection with the combat template
        teamSelection.ShowTeamSelection(combatTemplate);

        // ✅ STEP 5: Hide this menu
        HideMenu();

        Debug.Log($"✅ Opened team selection for: {combatTemplate.combatName}");
    }


    private System.Collections.IEnumerator ShowMenuCoroutine()
    {
        isAnimating = true;

        Debug.Log("Starting show menu animation");

        // Enable menu panel
        if (menuPanel != null)
            menuPanel.SetActive(true);

        canvasGroup.alpha = 0f;
        if (contentPanel != null)
            contentPanel.anchoredPosition = hiddenPosition;

        // Fade in background
        var fadeSequence = DOTween.Sequence();
        fadeSequence.Append(canvasGroup.DOFade(1f, slideInDuration * 0.3f));

        // Slide in content
        if (contentPanel != null)
        {
            var slideSequence = DOTween.Sequence();
            slideSequence.Append(contentPanel.DOAnchorPos(visiblePosition, slideInDuration).SetEase(slideEase));

            yield return slideSequence.WaitForCompletion();
        }

        yield return fadeSequence.WaitForCompletion();

        Debug.Log("Show menu animation completed");
        isAnimating = false;
    }

    private System.Collections.IEnumerator HideMenuCoroutine()
    {
        isAnimating = true;

        Debug.Log("Starting hide menu animation");

        // Slide out content
        if (contentPanel != null)
        {
            var slideSequence = DOTween.Sequence();
            slideSequence.Append(contentPanel.DOAnchorPos(hiddenPosition, slideOutDuration).SetEase(slideEase));
            yield return slideSequence.WaitForCompletion();
        }

        // Fade out background
        var fadeSequence = DOTween.Sequence();
        fadeSequence.Append(canvasGroup.DOFade(0f, slideOutDuration * 0.7f));
        yield return fadeSequence.WaitForCompletion();

        // Disable menu panel
        if (menuPanel != null)
            menuPanel.SetActive(false);

        isAnimating = false;

        Debug.Log("Hide menu animation completed");

        // Notify that menu was closed
        OnMenuClosed?.Invoke();
    }

    private void HideMenuInstant()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        canvasGroup.alpha = 0f;

        if (contentPanel != null)
            contentPanel.anchoredPosition = hiddenPosition;
    }

    // Public properties
    public bool IsVisible => menuPanel != null && menuPanel.activeSelf;
    public bool IsAnimating => isAnimating;

    // Context menu for testing
    [ContextMenu("Test Show Menu")]
    private void TestShowMenu()
    {
        if (Application.isPlaying)
            ShowMenu(1, 1);
    }

    [ContextMenu("Test Hide Menu")]
    private void TestHideMenu()
    {
        if (Application.isPlaying)
            HideMenu();
    }
}
