using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GachaUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainGachaPanel;        // Assign MainGachaUI
    public GameObject inventoryMainPanel;    // Assign InventoryMainPanel
    public MonsterInventoryUI inventoryUI;

    [Header("UI References")]
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI titleText;
    public Button singleSummonButton;
    public Button multiSummonButton;
    public Button collectionButton;
    public TMP_Dropdown poolDropdown;

    [Header("Cost Display")]
    public TextMeshProUGUI singleSummonCostText;
    public TextMeshProUGUI multiSummonCostText;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject resultPanel;
    public SummonResultUI summonResultUI;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip summonSound;
    public AudioClip multiSummonSound;

    [Header("Camera Integration")]
    [Tooltip("Should the camera move to altar when opening Gacha UI?")]
    public bool useCameraTransition = true;

    [Header("Close Button")]
    [Tooltip("Button to close the Gacha UI and return camera")]
    public Button closeButton;

    [Header("Manager Connection")]
    [SerializeField] private bool autoFindManagers = true;
    [SerializeField] private float managerSearchTimeout = 10f;
    [SerializeField] private bool showDebugLogs = false;

    // 🆕 UPDATED: Use the new manager system
    private GachaManager gachaManager;
    private CurrencyManager currencyManager; // Use CurrencyManager instead of PlayerInventory for currency
    private PlayerInventory playerInventory;  // Still needed for adding monsters
    private MonsterCollectionManager monsterCollectionManager;

    private int selectedPoolIndex = 0;
    private bool isGachaUIOpen = false;
    private bool managersInitialized = false;

    void Start()
    {
        StartCoroutine(InitializeGachaUI());
    }

    // 🆕 NEW: Robust initialization system
    private IEnumerator InitializeGachaUI()
    {
        if (showDebugLogs) Debug.Log("🎮 GachaUI: Starting initialization...");

        // Wait for managers to be available
        yield return StartCoroutine(WaitForManagers());

        // Initialize UI components
        InitializeReferences();
        SetupButtons();
        SetupDropdown();
        UpdateUI();

        // Ensure the main gacha panel is hidden at start
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(false);
        }

        managersInitialized = true;
        if (showDebugLogs) Debug.Log("✅ GachaUI: Initialization complete!");
    }

    // 🆕 NEW: Wait for persistent managers to be loaded
    private IEnumerator WaitForManagers()
    {
        float timeout = managerSearchTimeout;
        float elapsedTime = 0f;

        while (elapsedTime < timeout)
        {
            // Check if all required managers are available
            bool managersReady = true;

            if (autoFindManagers)
            {
                // Find scene-specific manager (GachaManager)
                if (gachaManager == null)
                {
                    gachaManager = FindAnyObjectByType<GachaManager>();
                    if (gachaManager == null) managersReady = false;
                }

                // Find persistent managers
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
            }

            if (managersReady)
            {
                if (showDebugLogs) Debug.Log("✅ GachaUI: All managers found!");
                break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        // Final check and error reporting
        if (gachaManager == null)
        {
            Debug.LogError("❌ GachaUI: GachaManager not found! Make sure it exists in this scene.");
        }

        if (currencyManager == null)
        {
            Debug.LogError("❌ GachaUI: CurrencyManager not found! Make sure the init scene loaded properly.");
        }

        if (playerInventory == null)
        {
            Debug.LogError("❌ GachaUI: PlayerInventory not found! Make sure the init scene loaded properly.");
        }

        if (monsterCollectionManager == null)
        {
            Debug.LogError("❌ GachaUI: MonsterCollectionManager not found! Make sure the init scene loaded properly.");
        }
    }

    void InitializeReferences()
    {
        // Managers should already be found by WaitForManagers
        if (showDebugLogs)
        {
            Debug.Log($"🔧 GachaUI References - GachaManager: {gachaManager != null}, " +
                     $"CurrencyManager: {currencyManager != null}, " +
                     $"PlayerInventory: {playerInventory != null}, " +
                     $"MonsterCollection: {monsterCollectionManager != null}");
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

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGachaUI);
        }

        // Set cost text using GachaManager
        if (singleSummonCostText != null && gachaManager != null)
        {
            singleSummonCostText.text = $"Cost: {gachaManager.singleSummonCost}";
        }

        if (multiSummonCostText != null && gachaManager != null)
        {
            multiSummonCostText.text = $"Cost: {gachaManager.multiSummonCost}";
        }
    }

    void SetupDropdown()
    {
        if (poolDropdown == null || gachaManager == null) return;

        poolDropdown.ClearOptions();
        List<string> poolNames = gachaManager.GetPoolNames();
        poolDropdown.AddOptions(poolNames);
        poolDropdown.onValueChanged.AddListener(OnPoolSelectionChanged);
    }

    void Update()
    {
        if (managersInitialized)
        {
            UpdateUI();
        }
    }

    // 🆕 UPDATED: Use CurrencyManager for currency display
    void UpdateUI()
    {
        // Update currency display - use Crystals instead of Soul Coins for gacha
        if (currencyManager != null && currencyText != null)
        {
            currencyText.text = $"Crystals: {currencyManager.GetCrystals():N0}";
        }

        if (gachaManager != null && currencyManager != null)
        {
            // Update button interactability based on crystal currency
            bool canAffordSingle = currencyManager.CanAffordCrystals(gachaManager.singleSummonCost);
            bool canAffordMulti = currencyManager.CanAffordCrystals(gachaManager.multiSummonCost);

            if (singleSummonButton != null)
            {
                singleSummonButton.interactable = canAffordSingle;
            }

            if (multiSummonButton != null)
            {
                multiSummonButton.interactable = canAffordMulti;
            }
        }
    }

    // Method to open the Gacha UI with camera transition
    public void OpenGachaUI()
    {
        if (isGachaUIOpen) return;

        if (showDebugLogs) Debug.Log("🎮 Opening Gacha UI...");

        if (useCameraTransition && CameraController.Instance != null)
        {
            // Move camera to altar first, then show UI
            CameraController.Instance.MoveToAltar(() => {
                ShowGachaPanel();
            });
        }
        else
        {
            // Just show UI without camera movement
            ShowGachaPanel();
        }
    }

    // Method to close the Gacha UI with camera return
    public void CloseGachaUI()
    {
        if (!isGachaUIOpen) return;

        if (showDebugLogs) Debug.Log("🎮 Closing Gacha UI...");

        // Hide UI first
        HideGachaPanel();

        if (useCameraTransition && CameraController.Instance != null)
        {
            // Return camera to default position
            CameraController.Instance.ReturnToDefault();
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

        // Also hide result panel if open
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    public void OnSingleSummonClicked()
    {
        if (gachaManager == null || !managersInitialized) return;

        StartCoroutine(PerformSummonWithAnimation(false));
    }

    public void OnMultiSummonClicked()
    {
        if (gachaManager == null || !managersInitialized) return;

        StartCoroutine(PerformSummonWithAnimation(true));
    }

    IEnumerator PerformSummonWithAnimation(bool isMulti)
    {
        // Disable buttons during summon
        SetButtonsInteractable(false);

        // Play summon sound
        if (audioSource != null)
        {
            AudioClip clipToPlay = isMulti ? multiSummonSound : summonSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        // Show summoning animation
        if (titleText != null)
        {
            titleText.text = isMulti ? "Summoning 10 Monsters..." : "Summoning Monster...";
        }

        // Wait for animation
        yield return new WaitForSeconds(1.5f);

        // Perform actual summon
        GachaSummonResult result = isMulti
            ? gachaManager.PerformMultiSummon(selectedPoolIndex)
            : gachaManager.PerformSingleSummon(selectedPoolIndex);

        // Reset title
        if (titleText != null)
        {
            titleText.text = "Monster Summoning";
        }

        // Show results
        if (result.success)
        {
            ShowSummonResults(result);
        }
        else
        {
            ShowErrorMessage(result.errorMessage);
        }

        // Re-enable buttons
        SetButtonsInteractable(true);
    }

    void ShowSummonResults(GachaSummonResult result)
    {
        if (summonResultUI != null)
        {
            // Keep mainPanel visible, show result panel on top
            resultPanel.SetActive(true);

            // Display results
            StartCoroutine(DisplayResultsAfterFrame(result));
        }
        else
        {
            Debug.LogError("❌ SummonResultUI not assigned!");
        }
    }

    IEnumerator DisplayResultsAfterFrame(GachaSummonResult result)
    {
        // Wait one frame to ensure the ResultPanel is fully active
        yield return null;

        // Now display the results
        summonResultUI.DisplayResults(result);
    }

    void ShowErrorMessage(string message)
    {
        Debug.LogError($"❌ Summon Error: {message}");
        // You can add a popup or notification UI here
    }

    public void OnCollectionClicked()
    {
        // Hide the main gacha panel
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(false);
        }

        // Show the inventory panel
        if (inventoryMainPanel != null)
        {
            inventoryMainPanel.SetActive(true);

            // Refresh the inventory to show latest monsters
            if (inventoryUI != null)
            {
                inventoryUI.LoadMonsterCollection();
            }
        }
        else
        {
            Debug.LogError("❌ InventoryMainPanel reference not assigned in GachaUI!");
        }
    }

    void OnPoolSelectionChanged(int poolIndex)
    {
        selectedPoolIndex = poolIndex;
        if (showDebugLogs && gachaManager != null)
        {
            Debug.Log($"🎯 Selected pool: {gachaManager.GetPoolNames()[poolIndex]}");
        }
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (singleSummonButton != null) singleSummonButton.interactable = interactable;
        if (multiSummonButton != null) multiSummonButton.interactable = interactable;
        if (collectionButton != null) collectionButton.interactable = interactable;
    }

    public void ReturnToMainPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    // 🆕 NEW: Public method to refresh connections (useful for debugging)
    [ContextMenu("Refresh Manager Connections")]
    public void RefreshManagerConnections()
    {
        managersInitialized = false;
        StopAllCoroutines();
        StartCoroutine(InitializeGachaUI());
    }

    // Context menu for testing
    [ContextMenu("Test Open Gacha UI")]
    public void TestOpenGachaUI()
    {
        OpenGachaUI();
    }

    [ContextMenu("Test Close Gacha UI")]
    public void TestCloseGachaUI()
    {
        CloseGachaUI();
    }
}
