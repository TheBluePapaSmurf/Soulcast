using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]
public class CurrencyDisplayElement
{
    [Header("UI Components")]
    public TextMeshProUGUI currencyText;
    public Image currencyIcon;
    public Button addButton; // Optional: For purchasing currency

    [Header("Animation Settings")]
    public bool animateOnChange = true;
    public float animationDuration = 0.3f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Formatting")]
    public string prefix = "";
    public string suffix = "";
    public bool useThousandsSeparator = true;

    [HideInInspector] public int previousValue;
    [HideInInspector] public Coroutine animationCoroutine;
}

public class CurrencyDisplayUI : MonoBehaviour
{
    [Header("Currency Elements")]
    [SerializeField] private CurrencyDisplayElement soulCoinsDisplay;
    [SerializeField] private CurrencyDisplayElement crystalsDisplay;

    [Header("Auto-Connect Settings")]
    [SerializeField] private bool autoFindCurrencyManager = true;
    [SerializeField] private bool initializeOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private CurrencyManager currencyManager;
    private bool isInitialized = false;

    void Start()
    {
        if (initializeOnStart)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        // Find CurrencyManager
        if (autoFindCurrencyManager)
        {
            StartCoroutine(FindCurrencyManagerCoroutine());
        }
        else
        {
            ConnectToCurrencyManager();
        }
    }

    private IEnumerator FindCurrencyManagerCoroutine()
    {
        // Wait for CurrencyManager to be created by InitGameManager
        float timeout = 10f; // 10 second timeout
        float elapsedTime = 0f;

        while (currencyManager == null && elapsedTime < timeout)
        {
            currencyManager = CurrencyManager.Instance;

            if (currencyManager == null)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }
        }

        if (currencyManager != null)
        {
            ConnectToCurrencyManager();
        }
        else
        {
            Debug.LogError("❌ CurrencyDisplayUI: Could not find CurrencyManager after 10 seconds!");
        }
    }

    private void ConnectToCurrencyManager()
    {
        if (currencyManager == null)
        {
            currencyManager = CurrencyManager.Instance;
        }

        if (currencyManager == null)
        {
            Debug.LogError("❌ CurrencyDisplayUI: CurrencyManager not found!");
            return;
        }

        // Subscribe to events
        CurrencyManager.OnSoulCoinsChanged += UpdateSoulCoinsDisplay;
        CurrencyManager.OnCrystalsChanged += UpdateCrystalsDisplay;

        // Initialize displays with current values
        UpdateSoulCoinsDisplay(currencyManager.GetSoulCoins());
        UpdateCrystalsDisplay(currencyManager.GetCrystals());

        // Connect optional purchase buttons
        if (soulCoinsDisplay.addButton != null)
        {
            soulCoinsDisplay.addButton.onClick.AddListener(() => OnPurchaseSoulCoins());
        }

        if (crystalsDisplay.addButton != null)
        {
            crystalsDisplay.addButton.onClick.AddListener(() => OnPurchaseCrystals());
        }

        isInitialized = true;

        if (showDebugLogs) Debug.Log("✅ CurrencyDisplayUI: Successfully connected to CurrencyManager");
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        CurrencyManager.OnSoulCoinsChanged -= UpdateSoulCoinsDisplay;
        CurrencyManager.OnCrystalsChanged -= UpdateCrystalsDisplay;
    }

    private void UpdateSoulCoinsDisplay(int newAmount)
    {
        UpdateCurrencyDisplay(soulCoinsDisplay, newAmount);
    }

    private void UpdateCrystalsDisplay(int newAmount)
    {
        UpdateCurrencyDisplay(crystalsDisplay, newAmount);
    }

    private void UpdateCurrencyDisplay(CurrencyDisplayElement element, int newAmount)
    {
        if (element.currencyText == null) return;

        if (element.animateOnChange && element.previousValue != newAmount && element.previousValue != 0)
        {
            // Stop any existing animation
            if (element.animationCoroutine != null)
            {
                StopCoroutine(element.animationCoroutine);
            }

            // Start new animation
            element.animationCoroutine = StartCoroutine(AnimateCurrencyChange(element, element.previousValue, newAmount));
        }
        else
        {
            // Direct update without animation
            element.currencyText.text = FormatCurrencyText(element, newAmount);
        }

        element.previousValue = newAmount;
    }

    private IEnumerator AnimateCurrencyChange(CurrencyDisplayElement element, int fromValue, int toValue)
    {
        float elapsedTime = 0f;

        while (elapsedTime < element.animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / element.animationDuration;
            float curveValue = element.animationCurve.Evaluate(normalizedTime);

            int currentValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, curveValue));
            element.currencyText.text = FormatCurrencyText(element, currentValue);

            yield return null;
        }

        // Ensure final value is exact
        element.currencyText.text = FormatCurrencyText(element, toValue);
        element.animationCoroutine = null;
    }

    private string FormatCurrencyText(CurrencyDisplayElement element, int amount)
    {
        string formattedAmount;

        if (element.useThousandsSeparator)
        {
            formattedAmount = amount.ToString("N0"); // Adds commas/periods based on system locale
        }
        else
        {
            formattedAmount = amount.ToString();
        }

        return element.prefix + formattedAmount + element.suffix;
    }

    // Optional: Purchase button handlers
    private void OnPurchaseSoulCoins()
    {
        if (showDebugLogs) Debug.Log("💰 Purchase Soul Coins button clicked");
        // TODO: Implement Soul Coins purchase logic
        // Example: Open shop, show purchase options, etc.
    }

    private void OnPurchaseCrystals()
    {
        if (showDebugLogs) Debug.Log("💎 Purchase Crystals button clicked");
        // TODO: Implement Crystals purchase logic
        // Example: Open IAP store, show crystal packages, etc.
    }

    // Public methods for manual updates
    public void RefreshDisplay()
    {
        if (currencyManager != null)
        {
            UpdateSoulCoinsDisplay(currencyManager.GetSoulCoins());
            UpdateCrystalsDisplay(currencyManager.GetCrystals());
        }
    }

    public void ForceReconnect()
    {
        isInitialized = false;
        Initialize();
    }
}
