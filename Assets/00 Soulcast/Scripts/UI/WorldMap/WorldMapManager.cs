using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WorldMapManager : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject regionMapContainer;
    [SerializeField] private GameObject levelMapContainer;
    [SerializeField] private GameObject fogOfWarOverlay;

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

        // Setup level buttons
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i; // Lokale kopie voor closure
            levelButtons[i].SetLevelData(levelIndex + 1);
            levelButtons[i].OnLevelSelected += () => SelectLevel(levelIndex);
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
        StartCoroutine(TransitionToLevelView(regionIndex + 1));
    }

    private void SelectLevel(int levelIndex)
    {
        if (isTransitioning) return;

        // Laad battle level
        SceneTransitionManager.Instance?.LoadBattleLevel(currentSelectedRegion + 1, levelIndex + 1);
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
