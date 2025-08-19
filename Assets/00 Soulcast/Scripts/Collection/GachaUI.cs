using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GachaUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainGachaPanel;
    public GameObject inventoryMainPanel;
    public MonsterInventoryUI inventoryUI;

    [Header("Pool Selection UI")]
    [SerializeField] private Button unknownPoolButton;
    [SerializeField] private Button mythicalPoolButton;
    [SerializeField] private GameObject unknownPoolInfo;
    [SerializeField] private GameObject mythicalPoolInfo;
    [SerializeField] private TextMeshProUGUI selectedPoolTitle;
    [SerializeField] private TextMeshProUGUI selectedPoolDescription;

    [Header("Summon Buttons")]
    public Button singleSummonButton;
    public Button multiSummonButton;
    public Button collectionButton;

    [Header("Cost Display")]
    public TextMeshProUGUI singleSummonCostText;
    public TextMeshProUGUI multiSummonCostText;

    [Header("Currency Display")]
    public TextMeshProUGUI currencyText; // Local currency display (optional)
    [SerializeField] private bool useLocalCurrencyDisplay = false; // Use TopPanel instead
    [SerializeField] private bool showSoulCoinsInGachaUI = true;

    [Header("Other UI")]
    public TextMeshProUGUI titleText;
    public GameObject mainPanel;
    public GameObject resultPanel;
    public SummonResultUI summonResultUI;
    public Button closeButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip summonSound;
    public AudioClip multiSummonSound;
    public AudioClip poolSwitchSound;

    [Header("Camera Integration")]
    public bool useCameraTransition = true;

    [Header("🎬 Cutscene Settings")] // 🆕 NEW
    [SerializeField] private bool useCutsceneForSingleSummon = true;
    [SerializeField] private bool useCutsceneForMultiSummon = false; // Keep false for performance
    [SerializeField] private bool showCutsceneDebugLogs = true;

    [Header("Manager Connection")]
    [SerializeField] private bool autoFindManagers = true;
    [SerializeField] private float managerSearchTimeout = 10f;
    [SerializeField] private bool showDebugLogs = false;

    // Manager references
    private GachaManager gachaManager;
    private CurrencyManager currencyManager;
    private PlayerInventory playerInventory;
    private MonsterCollectionManager monsterCollectionManager;
    private CurrencyDisplayUI topPanelCurrencyUI;
    public SummonCutsceneManager cutsceneManager; // 🆕 NEW

    // State
    private int selectedPoolIndex = GachaManager.UNKNOWN_POOL_INDEX;
    private bool isGachaUIOpen = false;
    private bool managersInitialized = false;

    void Start()
    {
        StartCoroutine(InitializeGachaUI());
    }

    private IEnumerator InitializeGachaUI()
    {
        if (showDebugLogs) Debug.Log("🎮 GachaUI: Starting initialization...");

        yield return StartCoroutine(WaitForManagers());

        InitializeReferences();
        SetupButtons();
        SetupPoolSelection();
        SelectPool(GachaManager.UNKNOWN_POOL_INDEX);
        UpdateUI();

        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(false);
        }

        managersInitialized = true;
        if (showDebugLogs) Debug.Log("✅ GachaUI: Initialization complete!");
    }

    private IEnumerator WaitForManagers()
    {
        float timeout = managerSearchTimeout;
        float elapsedTime = 0f;

        while (elapsedTime < timeout)
        {
            bool managersReady = true;

            if (autoFindManagers)
            {
                if (gachaManager == null)
                {
                    gachaManager = FindAnyObjectByType<GachaManager>();
                    if (gachaManager == null) managersReady = false;
                }

                if (currencyManager == null)
                {
                    currencyManager = CurrencyManager.Instance;
                    if (currencyManager == null) managersReady = false;
                }

                if (playerInventory == null)
                {
                    playerInventory = PlayerInventory.Instance;
                    if (playerInventory == null) managersReady = false;
                }

                if (monsterCollectionManager == null)
                {
                    monsterCollectionManager = MonsterCollectionManager.Instance;
                    if (monsterCollectionManager == null) managersReady = false;
                }

                if (topPanelCurrencyUI == null)
                {
                    topPanelCurrencyUI = FindAnyObjectByType<CurrencyDisplayUI>();
                }

                // 🆕 NEW: Find cutscene manager
                if (cutsceneManager == null)
                {
                    cutsceneManager = SummonCutsceneManager.Instance;
                    if (cutsceneManager == null)
                    {
                        cutsceneManager = FindAnyObjectByType<SummonCutsceneManager>();
                    }
                }
            }

            if (managersReady) break;

            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        if (showCutsceneDebugLogs)
        {
            Debug.Log($"🎬 GachaUI: Cutscene Manager found: {cutsceneManager != null}");
        }
    }

    void InitializeReferences()
    {
        if (showDebugLogs)
        {
            Debug.Log($"🔧 GachaUI References - GachaManager: {gachaManager != null}, " +
                     $"CurrencyManager: {currencyManager != null}, " +
                     $"CutsceneManager: {cutsceneManager != null}");
        }
    }

    void SetupButtons()
    {
        if (singleSummonButton != null)
        {
            singleSummonButton.onClick.AddListener(OnSingleSummonClicked);
        }

        if (multiSummonButton != null)
        {
            multiSummonButton.onClick.AddListener(OnMultiSummonClicked);
        }

        if (collectionButton != null)
        {
            collectionButton.onClick.AddListener(OnCollectionClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGachaUI);
        }
    }

    void SetupPoolSelection()
    {
        if (unknownPoolButton != null)
        {
            unknownPoolButton.onClick.AddListener(() => SelectPool(GachaManager.UNKNOWN_POOL_INDEX));
        }

        if (mythicalPoolButton != null)
        {
            mythicalPoolButton.onClick.AddListener(() => SelectPool(GachaManager.MYTHICAL_POOL_INDEX));
        }
    }

    public void SelectPool(int poolIndex)
    {
        if (gachaManager == null) return;

        selectedPoolIndex = poolIndex;
        UpdatePoolButtonVisuals();
        UpdatePoolInfoDisplay();
        UpdateCostDisplay();

        if (audioSource != null && poolSwitchSound != null)
        {
            audioSource.PlayOneShot(poolSwitchSound);
        }

        if (showDebugLogs)
        {
            string poolName = poolIndex == GachaManager.UNKNOWN_POOL_INDEX ? "Unknown" : "Mythical";
            Debug.Log($"🎯 Selected {poolName} Pool");
        }
    }

    void UpdatePoolButtonVisuals()
    {
        if (unknownPoolButton != null)
        {
            var colors = unknownPoolButton.colors;
            colors.normalColor = selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX ?
                new Color(0.2f, 0.8f, 0.2f) : Color.white;
            unknownPoolButton.colors = colors;
        }

        if (mythicalPoolButton != null)
        {
            var colors = mythicalPoolButton.colors;
            colors.normalColor = selectedPoolIndex == GachaManager.MYTHICAL_POOL_INDEX ?
                new Color(0.8f, 0.2f, 0.8f) : Color.white;
            mythicalPoolButton.colors = colors;
        }
    }

    void UpdatePoolInfoDisplay()
    {
        if (gachaManager == null) return;

        var pool = gachaManager.GetPool(selectedPoolIndex);
        if (pool == null) return;

        if (selectedPoolTitle != null)
        {
            selectedPoolTitle.text = pool.poolName;
        }

        if (selectedPoolDescription != null)
        {
            selectedPoolDescription.text = pool.description;
        }

        if (unknownPoolInfo != null)
        {
            unknownPoolInfo.SetActive(selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX);
        }

        if (mythicalPoolInfo != null)
        {
            mythicalPoolInfo.SetActive(selectedPoolIndex == GachaManager.MYTHICAL_POOL_INDEX);
        }
    }

    void UpdateCostDisplay()
    {
        if (gachaManager == null) return;

        int singleCost, multiCost;

        if (selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX)
        {
            singleCost = gachaManager.UnknownSingleCost;
            multiCost = gachaManager.UnknownMultiCost;
        }
        else
        {
            singleCost = gachaManager.MythicalSingleCost;
            multiCost = gachaManager.MythicalMultiCost;
        }

        if (singleSummonCostText != null)
        {
            singleSummonCostText.text = $"Soul Cast Cost: {singleCost:N0}";
        }

        if (multiSummonCostText != null)
        {
            multiSummonCostText.text = $"Multi Cast Cost: {multiCost:N0}";
        }
    }

    void Update()
    {
        if (managersInitialized)
        {
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (useLocalCurrencyDisplay && showSoulCoinsInGachaUI && currencyManager != null && currencyText != null)
        {
            currencyText.text = $"Soul Coins: {currencyManager.GetSoulCoins():N0}";
        }

        if (topPanelCurrencyUI != null)
        {
            topPanelCurrencyUI.RefreshDisplay();
        }

        if (gachaManager != null && currencyManager != null)
        {
            bool canAffordSingle, canAffordMulti;

            if (selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX)
            {
                canAffordSingle = gachaManager.CanAffordUnknownSummon(false);
                canAffordMulti = gachaManager.CanAffordUnknownSummon(true);
            }
            else
            {
                canAffordSingle = gachaManager.CanAffordMythicalSummon(false);
                canAffordMulti = gachaManager.CanAffordMythicalSummon(true);
            }

            if (singleSummonButton != null)
            {
                singleSummonButton.interactable = canAffordSingle;
            }

            if (multiSummonButton != null)
            {
                multiSummonButton.interactable = canAffordMulti;
            }

            UpdateButtonVisualFeedback(canAffordSingle, canAffordMulti);
        }
    }

    void UpdateButtonVisualFeedback(bool canAffordSingle, bool canAffordMulti)
    {
        if (singleSummonButton != null)
        {
            var singleColors = singleSummonButton.colors;
            singleColors.disabledColor = canAffordSingle ? Color.white : new Color(0.7f, 0.3f, 0.3f);
            singleSummonButton.colors = singleColors;
        }

        if (multiSummonButton != null)
        {
            var multiColors = multiSummonButton.colors;
            multiColors.disabledColor = canAffordMulti ? Color.white : new Color(0.7f, 0.3f, 0.3f);
            multiSummonButton.colors = multiColors;
        }
    }

    // 🎬 UPDATED: Single summon with cutscene integration
    public void OnSingleSummonClicked()
    {
        if (gachaManager == null || !managersInitialized) return;

        if (!CheckCurrentAffordability(false))
        {
            ShowInsufficientFundsMessage(false);
            return;
        }

        if (showCutsceneDebugLogs) Debug.Log("🎬 Starting single summon with cutscene...");

        // 🎬 NEW: Use cutscene for single summon
        if (useCutsceneForSingleSummon && cutsceneManager != null)
        {
            StartCoroutine(PerformSingleSummonWithCutscene());
        }
        else
        {
            // Fallback to normal summon
            StartCoroutine(PerformSummonWithAnimation(false));
        }
    }

    // 🔧 FIXED: Multi summon affordability check
    public void OnMultiSummonClicked()
    {
        if (gachaManager == null || !managersInitialized) return;

        if (!CheckCurrentAffordability(true)) // 🔧 FIXED: Use true for multi
        {
            ShowInsufficientFundsMessage(true); // 🔧 FIXED: Use true for multi
            return;
        }

        // Multi summon always uses normal animation (no cutscene for performance)
        StartCoroutine(PerformSummonWithAnimation(true));
    }

    // 🎬 NEW: Single summon with cutscene
    IEnumerator PerformSingleSummonWithCutscene()
    {
        SetButtonsInteractable(false);

        if (showCutsceneDebugLogs) Debug.Log("🎬 Performing summon through GachaManager...");

        // Perform the actual summon first
        GachaSummonResult result;
        if (selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX)
        {
            result = gachaManager.PerformUnknownSummon(false);
        }
        else
        {
            result = gachaManager.PerformMythicalSummon(false);
        }

        if (result.success && result.summonedMonsters.Count > 0)
        {
            if (showCutsceneDebugLogs) Debug.Log($"🎬 Summon successful! Starting cutscene for {result.summonedMonsters[0].monsterData.name}");

            // Hide main gacha panel for cutscene
            if (mainGachaPanel != null)
            {
                mainGachaPanel.SetActive(false);
            }

            // Start cutscene with the summoned monster
            bool cutsceneCompleted = false;
            cutsceneManager.StartSummonCutscene(result.summonedMonsters[0], () => {
                // Callback when cutscene ends
                cutsceneCompleted = true;
                if (showCutsceneDebugLogs) Debug.Log("🎬 Cutscene completed, returning to gacha UI");

                // Return to gacha UI
                if (mainGachaPanel != null)
                {
                    mainGachaPanel.SetActive(true);
                }

                SetButtonsInteractable(true);

                // Refresh currency display
                if (topPanelCurrencyUI != null)
                {
                    topPanelCurrencyUI.RefreshDisplay();
                }
            });

            // Wait for cutscene to complete
            while (!cutsceneCompleted)
            {
                yield return null;
            }
        }
        else
        {
            // Handle summon failure
            ShowErrorMessage(result.errorMessage);
            SetButtonsInteractable(true);
        }
    }

    bool CheckCurrentAffordability(bool isMulti)
    {
        if (currencyManager == null) return false;

        if (selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX)
        {
            return gachaManager.CanAffordUnknownSummon(isMulti);
        }
        else
        {
            return gachaManager.CanAffordMythicalSummon(isMulti);
        }
    }

    void ShowInsufficientFundsMessage(bool isMulti)
    {
        int requiredCost;
        if (selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX)
        {
            requiredCost = isMulti ? gachaManager.UnknownMultiCost : gachaManager.UnknownSingleCost;
        }
        else
        {
            requiredCost = isMulti ? gachaManager.MythicalMultiCost : gachaManager.MythicalSingleCost;
        }

        int currentSoulCoins = currencyManager.GetSoulCoins();
        int needed = requiredCost - currentSoulCoins;

        string message = $"Insufficient Soul Coins!\nRequired: {requiredCost:N0}\nCurrent: {currentSoulCoins:N0}\nNeed: {needed:N0} more";

        if (showDebugLogs)
        {
            Debug.Log($"💰 {message}");
        }

        ShowErrorMessage(message);
    }

    // 🔧 UPDATED: Normal summon animation (for multi-summon and fallback)
    IEnumerator PerformSummonWithAnimation(bool isMulti)
    {
        SetButtonsInteractable(false);

        if (audioSource != null)
        {
            AudioClip clipToPlay = isMulti ? multiSummonSound : summonSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        if (titleText != null)
        {
            string poolName = selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX ? "Unknown" : "Mythical";
            titleText.text = isMulti ? $"Summoning 10 {poolName} Monsters..." : $"Summoning {poolName} Monster...";
        }

        yield return new WaitForSeconds(1.5f);

        // Perform pool-specific summon
        GachaSummonResult result;
        if (selectedPoolIndex == GachaManager.UNKNOWN_POOL_INDEX)
        {
            result = gachaManager.PerformUnknownSummon(isMulti);
        }
        else
        {
            result = gachaManager.PerformMythicalSummon(isMulti);
        }

        if (titleText != null)
        {
            titleText.text = "Monster Summoning";
        }

        if (result.success)
        {
            ShowSummonResults(result);

            if (topPanelCurrencyUI != null)
            {
                topPanelCurrencyUI.RefreshDisplay();
            }
        }
        else
        {
            ShowErrorMessage(result.errorMessage);
        }

        SetButtonsInteractable(true);
    }

    public void OpenGachaUI()
    {
        // Check if UI is already open
        if (isGachaUIOpen)
        {
            if (showDebugLogs) Debug.Log("⚠️ Gacha UI is already open");
            return;
        }

        // Check if cutscene is playing to prevent conflicts
        if (cutsceneManager != null && cutsceneManager.IsCutscenePlaying)
        {
            if (showDebugLogs) Debug.Log("⚠️ Cannot open Gacha UI: Cutscene is currently playing");
            return;
        }

        // 🆕 NEW: Disable altar interaction when opening GachaUI
        var gachaAltars = FindObjectsByType<AltarInteraction>(FindObjectsSortMode.None);
        foreach (var altar in gachaAltars)
        {
            if (altar.altarType == AltarType.Gacha)
            {
                altar.SetInteractionEnabled(false);
            }
        }

        if (showDebugLogs) Debug.Log("🎮 Opening Gacha UI...");

        if (topPanelCurrencyUI != null)
        {
            topPanelCurrencyUI.RefreshDisplay();
        }

        if (useCameraTransition && CameraController.Instance != null)
        {
            CameraController.Instance.MoveToAltar(() => {
                // Double-check that no cutscene started during camera transition
                if (cutsceneManager != null && cutsceneManager.IsCutscenePlaying)
                {
                    if (showDebugLogs) Debug.Log("⚠️ Cutscene started during camera transition, not opening Gacha UI");

                    // 🆕 NEW: Re-enable interaction if we can't open UI
                    var gachaAltars = FindObjectsByType<AltarInteraction>(FindObjectsSortMode.None);
                    foreach (var altar in gachaAltars)
                    {
                        if (altar.altarType == AltarType.Gacha)
                        {
                            altar.SetInteractionEnabled(true);
                        }
                    }
                    return;
                }
                ShowGachaPanel();
            });
        }
        else
        {
            ShowGachaPanel();
        }
    }

    public void CloseGachaUI()
    {
        Debug.Log($"🔴 CloseGachaUI called! isGachaUIOpen: {isGachaUIOpen}");

        if (!isGachaUIOpen)
        {
            Debug.LogWarning("⚠️ CloseGachaUI: UI is already closed, returning early");
            return;
        }

        if (showDebugLogs) Debug.Log("🎮 Closing Gacha UI...");
        HideGachaPanel();

        if (useCameraTransition && CameraController.Instance != null)
        {
            CameraController.Instance.ReturnToDefault();
        }
        else
        {
            Debug.LogWarning("⚠️ AltarInteraction.Instance is null!");
        }

        Debug.Log("🔄 Notifying Gacha Altars that GachaUI is closed");
        var gachaAltars = FindObjectsByType<AltarInteraction>(FindObjectsSortMode.None);
        foreach (var altar in gachaAltars)
        {
            if (altar.altarType == AltarType.Gacha)
            {
                altar.OnSystemUIClosed(); // This will re-enable interaction
            }
        }
    }


    void ShowGachaPanel()
    {
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(true);
            isGachaUIOpen = true;
            if (showDebugLogs) Debug.Log("✅ Gacha UI Panel opened");
        }
    }

    void HideGachaPanel()
    {
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(false);
            isGachaUIOpen = false;
            if (showDebugLogs) Debug.Log("✅ Gacha UI Panel closed");
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    void ShowSummonResults(GachaSummonResult result)
    {
        if (summonResultUI != null)
        {
            resultPanel.SetActive(true);
            StartCoroutine(DisplayResultsAfterFrame(result));
        }
        else
        {
            Debug.LogError("❌ SummonResultUI not assigned!");
        }
    }

    IEnumerator DisplayResultsAfterFrame(GachaSummonResult result)
    {
        yield return null;
        summonResultUI.DisplayResults(result);
    }

    void ShowErrorMessage(string message)
    {
        Debug.LogError($"❌ Summon Error: {message}");
    }

    public void OnCollectionClicked()
    {
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(false);
        }

        if (inventoryMainPanel != null)
        {
            inventoryMainPanel.SetActive(true);

            if (inventoryUI != null)
            {
                inventoryUI.LoadMonsterCollection();
            }
        }
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (singleSummonButton != null) singleSummonButton.interactable = interactable;
        if (multiSummonButton != null) multiSummonButton.interactable = interactable;
        if (collectionButton != null) collectionButton.interactable = interactable;
        if (unknownPoolButton != null) unknownPoolButton.interactable = interactable;
        if (mythicalPoolButton != null) mythicalPoolButton.interactable = interactable;
    }

    public void ReturnToMainPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    [ContextMenu("Refresh Manager Connections")]
    public void RefreshManagerConnections()
    {
        managersInitialized = false;
        StopAllCoroutines();
        StartCoroutine(InitializeGachaUI());
    }

    [ContextMenu("Refresh Currency Display")]
    public void RefreshCurrencyDisplay()
    {
        if (topPanelCurrencyUI != null)
        {
            topPanelCurrencyUI.RefreshDisplay();
        }
    }

    // 🎬 NEW: Context menu for testing cutscene
    [ContextMenu("Test Single Summon with Cutscene")]
    public void TestSingleSummonWithCutscene()
    {
        if (Application.isPlaying)
        {
            OnSingleSummonClicked();
        }
        else
        {
            Debug.Log("⚠️ This test only works in Play mode");
        }
    }
}
