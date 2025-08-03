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

    private GachaManager gachaManager;
    private PlayerInventory playerInventory;
    private int selectedPoolIndex = 0;
    private bool isGachaUIOpen = false;

    void Start()
    {
        InitializeReferences();
        SetupButtons();
        SetupDropdown();
        UpdateUI();

        // Ensure the main gacha panel is hidden at start
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(false);
        }
    }

    void InitializeReferences()
    {
        gachaManager = GachaManager.Instance;
        playerInventory = PlayerInventory.Instance;

        if (gachaManager == null)
        {
            Debug.LogError("GachaManager not found! Make sure it exists in the scene.");
        }

        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory not found! Make sure it exists in the scene.");
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

        // Set cost text
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
        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerInventory != null && currencyText != null)
        {
            currencyText.text = $"Crystals: {playerInventory.GetSoulCoins()}";
        }

        if (gachaManager != null)
        {
            // Update button interactability based on currency
            bool canAffordSingle = gachaManager.CanAffordSummon(gachaManager.singleSummonCost);
            bool canAffordMulti = gachaManager.CanAffordSummon(gachaManager.multiSummonCost);

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

    // NEW: Method to open the Gacha UI with camera transition
    public void OpenGachaUI()
    {
        if (isGachaUIOpen) return;

        Debug.Log("Opening Gacha UI...");

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

    // NEW: Method to close the Gacha UI with camera return
    public void CloseGachaUI()
    {
        if (!isGachaUIOpen) return;

        Debug.Log("Closing Gacha UI...");

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
            Debug.Log("Gacha UI Panel opened");
        }
    }

    void HideGachaPanel()
    {
        if (mainGachaPanel != null)
        {
            mainGachaPanel.SetActive(false);
            isGachaUIOpen = false;
            Debug.Log("Gacha UI Panel closed");
        }

        // Also hide result panel if open
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    public void OnSingleSummonClicked()
    {
        if (gachaManager == null) return;

        StartCoroutine(PerformSummonWithAnimation(false));
    }

    public void OnMultiSummonClicked()
    {
        if (gachaManager == null) return;

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

        // Show summoning animation (you can customize this)
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
            // DON'T hide or disable mainPanel - keep it visible
            // Just show the result panel on top
            resultPanel.SetActive(true);

            // Display results
            StartCoroutine(DisplayResultsAfterFrame(result));
        }
        else
        {
            Debug.LogError("SummonResultUI not assigned!");
        }
    }

    // Add this new coroutine method to GachaUI.cs
    IEnumerator DisplayResultsAfterFrame(GachaSummonResult result)
    {
        // Wait one frame to ensure the ResultPanel is fully active
        yield return null;

        // Now display the results
        summonResultUI.DisplayResults(result);
    }

    void ShowErrorMessage(string message)
    {
        Debug.LogError($"Summon Error: {message}");
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
            Debug.LogError("InventoryMainPanel reference not assigned in GachaUI!");
        }
    }

    void OnPoolSelectionChanged(int poolIndex)
    {
        selectedPoolIndex = poolIndex;
        Debug.Log($"Selected pool: {gachaManager.GetPoolNames()[poolIndex]}");
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (singleSummonButton != null) singleSummonButton.interactable = interactable;
        if (multiSummonButton != null) multiSummonButton.interactable = interactable;
        if (collectionButton != null) collectionButton.interactable = interactable;
    }

    public void ReturnToMainPanel()
    {
        resultPanel.SetActive(false);
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
