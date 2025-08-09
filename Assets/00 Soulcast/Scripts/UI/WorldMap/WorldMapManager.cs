using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WorldMapManager : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject regionMapContainer;
    [SerializeField] private GameObject levelMapContainer;
    [SerializeField] private GameObject fogOfWarOverlay;

    [Header("Battle Sequence Menu")]
    [SerializeField] private BattleSequenceMenu battleSequenceMenu;

    // ✅ NEW: Battle System Integration
    [Header("Battle System")]
    [SerializeField] private PreBattleTeamSelection preBattleTeamSelection;

    [Header("UI Elements")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button backToRegionsButton;

    [Header("Region Buttons")]
    [SerializeField] private RegionButton[] regionButtons;

    [Header("Level Buttons")]
    [SerializeField] private LevelButton[] levelButtons;

    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private Ease transitionEase = Ease.OutCubic;
    [SerializeField] private float fogDuration = 1.5f;

    private int currentSelectedRegion = -1;
    private bool isTransitioning = false;

    private void Start()
    {
        InitializeWorldMap();
        SetupUICallbacks();
    }

    private void InitializeWorldMap()
    {
        // Toon regio view standaard
        ShowRegionView();

        // Setup region buttons
        for (int i = 0; i < regionButtons.Length; i++)
        {
            int regionIndex = i; // Lokale kopie voor closure
            regionButtons[i].SetRegionData(regionIndex + 1);
            regionButtons[i].OnRegionSelected += () => SelectRegion(regionIndex);
        }

        // Setup level buttons - Updated part
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i; // Lokale kopie voor closure

            // Initialize level with region data and battle menu reference
            levelButtons[i].InitializeLevel(1, levelIndex + 1, battleSequenceMenu); // Start met regio 1

            // Keep the old event for backward compatibility (optional)
            levelButtons[i].OnLevelSelected += () => Debug.Log($"Level {levelIndex + 1} selected (fallback)");
        }

        // Setup battle sequence menu
        if (battleSequenceMenu != null)
        {
            battleSequenceMenu.OnMenuClosed += OnBattleSequenceMenuClosed;
        }

        // ✅ NEW: Setup pre-battle team selection
        SetupPreBattleTeamSelection();
    }

    // ✅ NEW: Setup method for PreBattleTeamSelection
    private void SetupPreBattleTeamSelection()
    {
        if (preBattleTeamSelection != null)
        {
            // Setup callbacks for when battle starts or is cancelled
            preBattleTeamSelection.OnBattleStart += HandleBattleStart;
            preBattleTeamSelection.OnSelectionCancelled += HandleBattleSelectionCancelled;

            Debug.Log("PreBattleTeamSelection initialized successfully");
        }
        else
        {
            Debug.LogWarning("PreBattleTeamSelection is not assigned in WorldMapManager!");
        }
    }

    // ✅ NEW: Handle when battle actually starts
    private void HandleBattleStart(CombatTemplate config, System.Collections.Generic.List<CollectedMonster> team)
    {
        Debug.Log($"WorldMapManager: Starting battle '{config.combatName}' with {team.Count} monsters");

        // Here you can add additional logic before transitioning to battle scene
        // For example: save current world map state, play transition effects, etc.

        // Save current world map context
        SaveCurrentWorldMapState();

        // Optional: Show loading screen or transition effect
        StartBattleTransition(config, team);
    }

    // ✅ NEW: Handle when team selection is cancelled
    private void HandleBattleSelectionCancelled()
    {
        Debug.Log("WorldMapManager: Battle team selection was cancelled");

        // Optional: Re-enable world map interactions or play sound effect
        // The battle sequence menu should handle its own state
    }

    // ✅ NEW: Save current world map state before battle
    private void SaveCurrentWorldMapState()
    {
        // Save which region and level was selected for return after battle
        if (currentSelectedRegion >= 0)
        {
            PlayerPrefs.SetInt("LastSelectedRegion", currentSelectedRegion + 1);
            PlayerPrefs.SetString("LastWorldMapState", "LevelView");
        }
        else
        {
            PlayerPrefs.SetString("LastWorldMapState", "RegionView");
        }

        Debug.Log($"Saved world map state: Region {currentSelectedRegion + 1}");
    }

    // ✅ NEW: Handle battle transition
    private void StartBattleTransition(CombatTemplate config, System.Collections.Generic.List<CollectedMonster> team)
    {
        // You can add transition effects here before loading the battle scene
        // For now, we'll use the existing SceneTransitionManager

        int regionId = currentSelectedRegion + 1;
        int levelId = 1; // You might want to track this more specifically

        SceneTransitionManager.Instance?.LoadBattleLevel(regionId, levelId);
    }

    // ✅ NEW: Method to restore world map state after returning from battle
    public void RestoreWorldMapState()
    {
        string lastState = PlayerPrefs.GetString("LastWorldMapState", "RegionView");
        int lastRegion = PlayerPrefs.GetInt("LastSelectedRegion", 1);

        if (lastState == "LevelView" && lastRegion > 0)
        {
            // Restore to the level view of the last selected region
            currentSelectedRegion = lastRegion - 1;

            // Update level buttons with the correct region data
            for (int i = 0; i < levelButtons.Length; i++)
            {
                levelButtons[i].SetRegionData(lastRegion);
            }

            // Show level view directly
            regionMapContainer.SetActive(false);
            levelMapContainer.SetActive(true);
            levelMapContainer.transform.localScale = Vector3.one;

            Debug.Log($"Restored world map to Level View for Region {lastRegion}");
        }
        else
        {
            // Default to region view
            ShowRegionView();
            Debug.Log("Restored world map to Region View");
        }
    }

    private void SetupUICallbacks()
    {
        backButton.onClick.AddListener(ReturnToHub);
        backToRegionsButton.onClick.AddListener(ShowRegionView);
    }

    private void SelectRegion(int regionIndex)
    {
        if (isTransitioning) return;

        currentSelectedRegion = regionIndex;

        // Update alle level buttons met de nieuwe regio data
        for (int i = 0; i < levelButtons.Length; i++)
        {
            levelButtons[i].SetRegionData(regionIndex + 1);
        }

        StartCoroutine(TransitionToLevelView(regionIndex + 1));
    }

    private System.Collections.IEnumerator TransitionToLevelView(int regionNumber)
    {
        isTransitioning = true;

        Debug.Log($"Starting transition to region {regionNumber}");

        // Speel fog of war animatie
        yield return StartCoroutine(PlaySimpleFogTransition());

        Debug.Log("Fog animation completed, switching views...");

        // Verberg regio view
        regionMapContainer.SetActive(false);

        // Toon level view
        levelMapContainer.SetActive(true);
        levelMapContainer.transform.localScale = Vector3.zero;

        // Animate level container in
        levelMapContainer.transform.DOScale(Vector3.one, transitionDuration).SetEase(transitionEase);

        yield return new WaitForSeconds(transitionDuration);

        Debug.Log("Level view transition completed");

        isTransitioning = false;
    }

    private System.Collections.IEnumerator PlaySimpleFogTransition()
    {
        if (fogOfWarOverlay == null)
        {
            Debug.LogWarning("FogOfWarOverlay is null, skipping fog animation");
            yield break;
        }

        // Activeer fog overlay
        fogOfWarOverlay.SetActive(true);

        CanvasGroup fogCanvasGroup = fogOfWarOverlay.GetComponent<CanvasGroup>();
        if (fogCanvasGroup == null)
        {
            Debug.LogWarning("No CanvasGroup found on FogOfWarOverlay, adding one");
            fogCanvasGroup = fogOfWarOverlay.AddComponent<CanvasGroup>();
        }

        RectTransform fogRect = fogOfWarOverlay.GetComponent<RectTransform>();

        // Reset initial state
        fogCanvasGroup.alpha = 0f;
        fogRect.localScale = Vector3.one;

        Debug.Log("Starting fog appear animation");

        // Fade in with scale effect
        var appearSequence = DOTween.Sequence();
        appearSequence.Append(fogCanvasGroup.DOFade(1f, fogDuration * 0.4f).SetEase(Ease.OutQuad));
        appearSequence.Join(fogRect.DOScale(1.1f, fogDuration * 0.3f).SetEase(Ease.OutBack));
        appearSequence.Append(fogRect.DOScale(1f, fogDuration * 0.1f).SetEase(Ease.InOutQuad));

        yield return appearSequence.WaitForCompletion();

        Debug.Log("Fog appear completed, waiting...");

        // Hold for a moment
        yield return new WaitForSeconds(0.2f);

        Debug.Log("Starting fog disappear animation");

        // Fade out with scale effect
        var disappearSequence = DOTween.Sequence();
        disappearSequence.Append(fogRect.DOScale(1.2f, fogDuration * 0.2f).SetEase(Ease.InQuad));
        disappearSequence.Join(fogCanvasGroup.DOFade(0f, fogDuration * 0.5f).SetEase(Ease.InQuad));
        disappearSequence.Append(fogRect.DOScale(0.8f, fogDuration * 0.3f).SetEase(Ease.InBack));

        yield return disappearSequence.WaitForCompletion();

        Debug.Log("Fog disappear completed");

        // Deactiveer fog overlay
        fogOfWarOverlay.SetActive(false);

        // Reset transform
        fogRect.localScale = Vector3.one;
    }

    public void ShowRegionView()
    {
        if (isTransitioning) return;

        Debug.Log("Returning to region view");

        isTransitioning = true;

        // Verberg level view
        if (levelMapContainer.activeSelf)
        {
            levelMapContainer.transform.DOScale(Vector3.zero, transitionDuration).SetEase(transitionEase)
                .OnComplete(() => levelMapContainer.SetActive(false));
        }

        // Toon regio view
        regionMapContainer.SetActive(true);
        regionMapContainer.transform.localScale = Vector3.zero;
        regionMapContainer.transform.DOScale(Vector3.one, transitionDuration).SetEase(transitionEase)
            .OnComplete(() => {
                isTransitioning = false;
                Debug.Log("Region view transition completed");
            });
    }

    private void ReturnToHub()
    {
        if (isTransitioning) return;

        Debug.Log("Returning to Hub");
        SceneTransitionManager.Instance?.LoadHubScene();
    }

    private void OnBattleSequenceMenuClosed()
    {
        // Optional: doe iets wanneer het battle menu wordt gesloten
        Debug.Log("Battle sequence menu closed");
    }

    // ✅ NEW: Context menu for testing battle integration
    [ContextMenu("Test Battle Integration")]
    private void TestBattleIntegration()
    {
        if (Application.isPlaying && preBattleTeamSelection != null)
        {
            Debug.Log("Testing battle integration...");

            // Check if MonsterCollectionManager has monsters
            if (MonsterCollectionManager.Instance != null)
            {
                var monsters = MonsterCollectionManager.Instance.GetAllMonsters();
                Debug.Log($"Available monsters: {monsters.Count}");
            }
            else
            {
                Debug.LogWarning("MonsterCollectionManager.Instance is null!");
            }
        }
    }

    // Debug methods
    [ContextMenu("Force Show Level View")]
    private void ForceShowLevelView()
    {
        regionMapContainer.SetActive(false);
        levelMapContainer.SetActive(true);
        Debug.Log("Forced level view active");
    }

    [ContextMenu("Force Show Region View")]
    private void ForceShowRegionView()
    {
        levelMapContainer.SetActive(false);
        regionMapContainer.SetActive(true);
        Debug.Log("Forced region view active");
    }

    [ContextMenu("Test Fog Animation")]
    private void TestFogAnimation()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(PlaySimpleFogTransition());
        }
    }
}
