using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public enum AltarType
{
    Gacha,
    Upgrade
}

public class AltarInteraction : MonoBehaviour
{
    [Header("Altar Configuration")]
    public AltarType altarType = AltarType.Gacha;

    [Header("UI References")]
    public RectTransform buttonRow;
    public Canvas parentCanvas;

    [Header("🆕 Main Buttons Management")]
    [SerializeField] private GameObject mainButtons;
    [SerializeField] private bool autoHideMainButtons = true;
    [SerializeField] private bool animateMainButtons = true;
    [SerializeField] private float mainButtonsAnimationDuration = 0.3f;
    [SerializeField] private Ease mainButtonsAnimationEase = Ease.OutCubic;

    [Header("🎮 System Integration")]
    [SerializeField] private GachaUI gachaUI;
    [SerializeField] private MonsterUpgradeManager upgradeManager;
    [SerializeField] private bool autoFindSystems = true;

    [Header("🔒 Interaction Control")]
    [SerializeField] private bool allowInteraction = true;
    [SerializeField] private bool showInteractionBlockedFeedback = true;
    [SerializeField] private float blockedClickCooldown = 0.5f;

    [Header("🎬 Magic Aura Effect")]
    [Tooltip("CFXR3 Magic Aura effect to sync with ButtonRow")]
    public GameObject magicAuraEffect;
    [SerializeField] private bool autoFindMagicAura = true;

    [Header("Audio")]
    [Tooltip("Audio clip for popup sound")]
    public AudioClip popupSoundClip;
    [Tooltip("Audio source to play sounds (if null, will use/create one on this object)")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    [Tooltip("Volume for the popup sound")]
    public float popupVolume = 1f;

    [Header("Close Behavior")]
    [Tooltip("Should the UI close when clicking on UI elements like buttons?")]
    public bool closeOnUIClick = false;
    [Tooltip("Should the UI close when clicking outside the UI area?")]
    public bool closeOnOutsideClick = true;

    [Header("Animation Settings")]
    public float slideInDuration = 0.5f;
    public float buttonDelayInterval = 0.5f;
    public Ease slideInEase = Ease.OutBack;
    public float hiddenYOffset = -300f;
    public bool animateInReverseOrder = false;

    [Header("Input")]
    public InputActionReference clickAction;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Private variables
    private Camera playerCamera;
    private bool isUIVisible = false;
    private List<RectTransform> buttonTransforms = new List<RectTransform>();
    private List<Vector2> originalLayoutPositions = new List<Vector2>();
    private List<LayoutElement> buttonLayoutElements = new List<LayoutElement>();
    private Sequence animationSequence;

    // Layout Group
    private HorizontalLayoutGroup horizontalLayoutGroup;

    // MainButtons management - SHARED ACROSS ALL ALTARS
    private static GameObject sharedMainButtons;
    private static CanvasGroup sharedMainButtonsCanvasGroup;
    private static Vector3 sharedMainButtonsOriginalScale;
    private static int activeAltarCount = 0;

    // UI state tracking
    private bool systemUIIsOpen = false;

    // Interaction blocking
    private bool isInteractionBlocked = false;
    private float lastBlockedClickTime = 0f;

    // Type-specific buttons
    private Button primaryActionButton;

    // ✅ REMOVED SINGLETON - Each altar is independent now

    void Awake()
    {
        // Count active altars
        activeAltarCount++;
        if (showDebugLogs) Debug.Log($"🏺 {altarType} Altar awakened. Total active altars: {activeAltarCount}");
    }

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindAnyObjectByType<Camera>();

        // Auto-find button row based on altar type
        if (buttonRow == null)
        {
            string buttonRowName = altarType == AltarType.Gacha ? "GachaButtonRow" : "UpgradeButtonRow";
            buttonRow = GameObject.Find(buttonRowName)?.GetComponent<RectTransform>();

            if (buttonRow == null && showDebugLogs)
            {
                Debug.LogWarning($"⚠️ {buttonRowName} not found! Please assign manually.");
            }
        }

        if (parentCanvas == null)
            parentCanvas = FindAnyObjectByType<Canvas>();

        SetupSharedMainButtonsReference();
        SetupSystemIntegration();
        SetupMagicAuraEffect();
        SetupAudioSource();

        InitializeUI();
        EnableClickInput();
    }

    void SetupSharedMainButtonsReference()
    {
        // Use shared reference for MainButtons across all altars
        if (sharedMainButtons == null)
        {
            sharedMainButtons = GameObject.Find("MainButtons");

            if (sharedMainButtons != null)
            {
                sharedMainButtonsOriginalScale = sharedMainButtons.transform.localScale;
                sharedMainButtonsCanvasGroup = sharedMainButtons.GetComponent<CanvasGroup>();
                if (sharedMainButtonsCanvasGroup == null)
                {
                    sharedMainButtonsCanvasGroup = sharedMainButtons.AddComponent<CanvasGroup>();
                }

                if (showDebugLogs) Debug.Log($"🎮 Shared MainButtons reference setup for {altarType} altar");
            }
            else
            {
                Debug.LogWarning("⚠️ MainButtons GameObject not found! Auto-hide feature will be disabled.");
                autoHideMainButtons = false;
            }
        }

        // Use the shared references
        mainButtons = sharedMainButtons;
    }

    void SetupSystemIntegration()
    {
        if (autoFindSystems)
        {
            if (altarType == AltarType.Gacha && gachaUI == null)
            {
                gachaUI = FindAnyObjectByType<GachaUI>();
            }

            if (altarType == AltarType.Upgrade && upgradeManager == null)
            {
                upgradeManager = FindAnyObjectByType<MonsterUpgradeManager>();
            }
        }

        // Setup primary action button based on altar type
        if (buttonRow != null)
        {
            string buttonName = altarType == AltarType.Gacha ? "SummonBtn" : "UpgradeBtn";
            Transform buttonTransform = buttonRow.Find(buttonName);

            if (buttonTransform != null)
            {
                primaryActionButton = buttonTransform.GetComponent<Button>();
                if (primaryActionButton != null)
                {
                    primaryActionButton.onClick.AddListener(OnPrimaryActionButtonClicked);
                    if (showDebugLogs) Debug.Log($"✅ {buttonName} button listener added for {altarType} altar");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ {buttonName} not found in {buttonRow?.name}!");
            }
        }

        if (showDebugLogs) Debug.Log($"🎮 {altarType} altar integration setup complete");
    }

    void SetupMagicAuraEffect()
    {
        if (autoFindMagicAura && magicAuraEffect == null)
        {
            Transform magicAuraTransform = transform.Find("CFXR3 Magic Aura A (Runic)");
            if (magicAuraTransform != null)
            {
                magicAuraEffect = magicAuraTransform.gameObject;
                if (showDebugLogs) Debug.Log($"✅ Auto-found Magic Aura for {altarType} altar");
            }
        }
    }

    void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.volume = popupVolume;
            }
        }
    }

    void OnPrimaryActionButtonClicked()
    {
        if (altarType == AltarType.Gacha)
        {
            OnSummonButtonClicked();
        }
        else if (altarType == AltarType.Upgrade)
        {
            OnUpgradeButtonClicked();
        }
    }

    void OnSummonButtonClicked()
    {
        var cutsceneManager = SummonCutsceneManager.Instance;
        if (cutsceneManager != null && cutsceneManager.IsCutscenePlaying)
        {
            Debug.Log("⚠️ Cannot open Gacha UI: Cutscene is already playing");
            return;
        }

        if (showDebugLogs) Debug.Log("🎮 Summon button clicked - hiding GachaButtonRow and opening GachaUI");

        SetInteractionEnabled(false);
        HideButtonRowUI();

        if (gachaUI != null)
        {
            gachaUI.OpenGachaUI();
            systemUIIsOpen = true;
        }
    }

    void OnUpgradeButtonClicked()
    {
        if (showDebugLogs) Debug.Log("🔮 Upgrade button clicked - hiding UpgradeButtonRow and opening MonsterUpgradePanel");

        SetInteractionEnabled(false);
        HideButtonRowUI();

        if (upgradeManager != null)
        {
            upgradeManager.OpenUpgradePanel();
            systemUIIsOpen = true;
        }
        else
        {
            Debug.LogWarning("⚠️ MonsterUpgradeManager not found!");
            // ✅ RESET STATE if manager not found
            SetInteractionEnabled(true);
            ShowMainButtons();
        }
    }

    public void OnSystemUIClosed()
    {
        if (systemUIIsOpen || isInteractionBlocked)
        {
            if (showDebugLogs) Debug.Log($"🎮 {altarType} system UI closed - re-enabling altar interaction");
            systemUIIsOpen = false;
            SetInteractionEnabled(true);
            ShowMainButtons();
        }
    }

    // ✅ NEW: Force reset method for debugging
    [ContextMenu("Force Reset State")]
    public void ForceResetState()
    {
        systemUIIsOpen = false;
        isInteractionBlocked = false;
        allowInteraction = true;
        isUIVisible = false;

        if (buttonRow != null)
        {
            buttonRow.gameObject.SetActive(false);
        }

        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(false);
        }

        ShowMainButtons();

        if (showDebugLogs) Debug.Log($"🔄 {altarType} altar state forcefully reset");
    }

    public void SetInteractionEnabled(bool enabled)
    {
        isInteractionBlocked = !enabled;
        allowInteraction = enabled;

        if (showDebugLogs)
        {
            Debug.Log($"🔒 {altarType} altar interaction {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    public bool IsInteractionEnabled => allowInteraction && !isInteractionBlocked;

    void ShowBlockedInteractionFeedback()
    {
        if (!showInteractionBlockedFeedback) return;

        float currentTime = Time.time;
        if (currentTime - lastBlockedClickTime < blockedClickCooldown) return;

        lastBlockedClickTime = currentTime;

        if (showDebugLogs)
        {
            Debug.Log($"🔒 {altarType} altar interaction is currently blocked");
        }
    }

    private void Update()
    {
        if (isUIVisible && buttonRow != null && buttonRow.gameObject.activeSelf && !isInteractionBlocked)
        {
            CheckForCloseClick();
        }
    }

    void InitializeUI()
    {
        if (buttonRow != null)
        {
            bool wasActive = buttonRow.gameObject.activeSelf;
            buttonRow.gameObject.SetActive(true);

            horizontalLayoutGroup = buttonRow.GetComponent<HorizontalLayoutGroup>();

            buttonTransforms.Clear();
            originalLayoutPositions.Clear();
            buttonLayoutElements.Clear();

            if (horizontalLayoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRow);
                Canvas.ForceUpdateCanvases();
            }

            for (int i = 0; i < buttonRow.childCount; i++)
            {
                Transform child = buttonRow.GetChild(i);
                RectTransform buttonRect = child.GetComponent<RectTransform>();

                if (buttonRect != null)
                {
                    buttonTransforms.Add(buttonRect);
                    originalLayoutPositions.Add(buttonRect.anchoredPosition);

                    if (showDebugLogs) Debug.Log($"Button {i} ({buttonRect.name}) original position: {buttonRect.anchoredPosition}");

                    LayoutElement layoutElement = buttonRect.GetComponent<LayoutElement>();
                    if (layoutElement == null)
                    {
                        layoutElement = buttonRect.gameObject.AddComponent<LayoutElement>();
                    }
                    buttonLayoutElements.Add(layoutElement);
                }
            }

            buttonRow.gameObject.SetActive(wasActive);
        }
    }

    void PlayClickEffects()
    {
        if (popupSoundClip != null && audioSource != null)
        {
            audioSource.volume = popupVolume;
            audioSource.PlayOneShot(popupSoundClip);
            if (showDebugLogs) Debug.Log($"🔊 Playing {altarType} altar popup sound");
        }
    }

    void HideMainButtons()
    {
        if (!autoHideMainButtons || sharedMainButtons == null || sharedMainButtonsCanvasGroup == null) return;

        if (showDebugLogs) Debug.Log($"🔽 Hiding MainButtons (requested by {altarType} altar)");

        if (animateMainButtons)
        {
            sharedMainButtonsCanvasGroup.DOFade(0f, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase);

            sharedMainButtons.transform.DOScale(0.8f, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase)
                .OnComplete(() => {
                    sharedMainButtons.SetActive(false);
                    if (showDebugLogs) Debug.Log("✅ MainButtons hidden");
                });
        }
        else
        {
            sharedMainButtons.SetActive(false);
            if (showDebugLogs) Debug.Log("✅ MainButtons hidden instantly");
        }
    }

    void ShowMainButtons()
    {
        if (!autoHideMainButtons || sharedMainButtons == null || sharedMainButtonsCanvasGroup == null) return;

        // ✅ CHECK: Only show if no other altar has UI open
        bool anyAltarHasUIOpen = false;
        var allAltars = FindObjectsByType<AltarInteraction>(FindObjectsSortMode.None);
        foreach (var altar in allAltars)
        {
            if (altar != this && (altar.systemUIIsOpen || altar.isUIVisible))
            {
                anyAltarHasUIOpen = true;
                break;
            }
        }

        if (anyAltarHasUIOpen)
        {
            if (showDebugLogs) Debug.Log($"🔄 Skipping MainButtons show - another altar has UI open");
            return;
        }

        if (showDebugLogs) Debug.Log($"🔼 Showing MainButtons (requested by {altarType} altar)");

        if (animateMainButtons)
        {
            sharedMainButtons.SetActive(true);
            sharedMainButtonsCanvasGroup.alpha = 0f;
            sharedMainButtons.transform.localScale = Vector3.one * 0.8f;

            sharedMainButtonsCanvasGroup.DOFade(1f, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase);

            sharedMainButtons.transform.DOScale(sharedMainButtonsOriginalScale, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase)
                .OnComplete(() => {
                    if (showDebugLogs) Debug.Log("✅ MainButtons shown");
                });
        }
        else
        {
            sharedMainButtons.SetActive(true);
            sharedMainButtonsCanvasGroup.alpha = 1f;
            sharedMainButtons.transform.localScale = sharedMainButtonsOriginalScale;
            if (showDebugLogs) Debug.Log("✅ MainButtons shown instantly");
        }
    }

    void DisableLayoutControlForAnimatedButtons()
    {
        for (int i = 0; i < buttonLayoutElements.Count; i++)
        {
            buttonLayoutElements[i].ignoreLayout = true;
        }
    }

    void SetButtonsToHiddenPositions()
    {
        for (int i = 0; i < buttonTransforms.Count; i++)
        {
            Vector2 hiddenPos = new Vector2(
                originalLayoutPositions[i].x,
                originalLayoutPositions[i].y + hiddenYOffset
            );
            buttonTransforms[i].anchoredPosition = hiddenPos;

            if (showDebugLogs) Debug.Log($"Button {i} hidden position: {hiddenPos}");
        }
    }

    void RestoreLayoutControl()
    {
        for (int i = 0; i < buttonLayoutElements.Count; i++)
        {
            buttonLayoutElements[i].ignoreLayout = false;
        }

        if (horizontalLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRow);
        }
    }

    void EnableClickInput()
    {
        if (clickAction != null)
        {
            clickAction.action.performed += OnClick;
            clickAction.action.Enable();
        }
    }

    void OnDestroy()
    {
        activeAltarCount--;
        if (showDebugLogs) Debug.Log($"🏺 {altarType} Altar destroyed. Remaining altars: {activeAltarCount}");

        // Reset shared references if this was the last altar
        if (activeAltarCount <= 0)
        {
            sharedMainButtons = null;
            sharedMainButtonsCanvasGroup = null;
        }
    }

    void OnDisable()
    {
        if (clickAction != null)
        {
            clickAction.action.performed -= OnClick;
            clickAction.action.Disable();
        }

        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        if (primaryActionButton != null)
        {
            primaryActionButton.onClick.RemoveListener(OnPrimaryActionButtonClicked);
        }

        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(false);
        }

        // ✅ IMPROVED: Only show main buttons if no UI is open and no other altar is active
        if (autoHideMainButtons && !isUIVisible && !systemUIIsOpen)
        {
            ShowMainButtons();
        }
    }

    void OnClick(InputAction.CallbackContext context)
    {
        if (playerCamera == null) return;

        if (isInteractionBlocked || !allowInteraction)
        {
            ShowBlockedInteractionFeedback();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                PlayClickEffects();
                ToggleButtonRowUI();
            }
        }
    }

    void CheckForCloseClick()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        bool isOverUI = EventSystem.current.IsPointerOverGameObject();

        if (isOverUI)
        {
            if (closeOnUIClick)
            {
                if (showDebugLogs) Debug.Log($"Closing {altarType} ButtonRow: Clicked on UI element");
                HideButtonRowUI();
            }
            return;
        }

        if (!closeOnOutsideClick)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                return;
            }
        }

        if (showDebugLogs) Debug.Log($"Closing {altarType} ButtonRow: Clicked outside");
        HideButtonRowUI();
    }

    void OnMouseDown()
    {
        if (isInteractionBlocked || !allowInteraction)
        {
            ShowBlockedInteractionFeedback();
            return;
        }

        PlayClickEffects();
        ToggleButtonRowUI();
    }

    public void ToggleButtonRowUI()
    {
        if (buttonRow == null) return;

        if (!isUIVisible)
        {
            ShowButtonRowUI();
        }
        else
        {
            HideButtonRowUI();
        }
    }

    public void ShowButtonRowUI()
    {
        if (buttonRow == null || isUIVisible || buttonTransforms.Count == 0) return;

        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        HideMainButtons();
        StartCoroutine(ShowButtonRowUICoroutine());
    }

    IEnumerator ShowButtonRowUICoroutine()
    {
        DisableLayoutControlForAnimatedButtons();

        buttonRow.gameObject.SetActive(true);

        // Activate Magic Aura
        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(true);
            if (showDebugLogs) Debug.Log($"🎬 Magic Aura activated for {altarType} altar");
        }

        SetButtonsToHiddenPositions();
        yield return null;

        animationSequence = DOTween.Sequence();

        if (animateInReverseOrder)
        {
            for (int i = buttonTransforms.Count - 1; i >= 0; i--)
            {
                int buttonIndex = i;

                if (buttonIndex < buttonTransforms.Count - 1)
                {
                    animationSequence.AppendInterval(buttonDelayInterval);
                }

                animationSequence.Append(
                    buttonTransforms[buttonIndex].DOAnchorPos(originalLayoutPositions[buttonIndex], slideInDuration)
                        .SetEase(slideInEase)
                );
            }
        }
        else
        {
            for (int i = 0; i < buttonTransforms.Count; i++)
            {
                int buttonIndex = i;

                if (buttonIndex > 0)
                {
                    animationSequence.AppendInterval(buttonDelayInterval);
                }

                animationSequence.Append(
                    buttonTransforms[buttonIndex].DOAnchorPos(originalLayoutPositions[buttonIndex], slideInDuration)
                        .SetEase(slideInEase)
                );
            }
        }

        animationSequence.OnComplete(() => {
            for (int i = 0; i < buttonTransforms.Count; i++)
            {
                buttonTransforms[i].anchoredPosition = originalLayoutPositions[i];
            }

            RestoreLayoutControl();
            isUIVisible = true;
        });
    }

    public void HideButtonRowUI()
    {
        if (buttonRow == null || !isUIVisible || buttonTransforms.Count == 0) return;

        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        for (int i = 0; i < buttonLayoutElements.Count; i++)
        {
            buttonLayoutElements[i].ignoreLayout = true;
        }

        animationSequence = DOTween.Sequence();

        for (int i = buttonTransforms.Count - 1; i >= 0; i--)
        {
            int buttonIndex = i;

            Vector2 hiddenPos = new Vector2(
                originalLayoutPositions[buttonIndex].x,
                originalLayoutPositions[buttonIndex].y + hiddenYOffset
            );

            if (buttonIndex < buttonTransforms.Count - 1)
            {
                animationSequence.AppendInterval(buttonDelayInterval * 0.3f);
            }

            animationSequence.Append(
                buttonTransforms[buttonIndex].DOAnchorPos(hiddenPos, slideInDuration * 0.7f)
                    .SetEase(Ease.InBack)
            );
        }

        animationSequence.OnComplete(() => {
            buttonRow.gameObject.SetActive(false);

            // Deactivate Magic Aura
            if (magicAuraEffect != null)
            {
                magicAuraEffect.SetActive(false);
                if (showDebugLogs) Debug.Log($"🎬 Magic Aura deactivated for {altarType} altar");
            }

            RestoreLayoutControl();
            isUIVisible = false;

            // ✅ IMPROVED: Always try to show main buttons when hiding UI
            if (!systemUIIsOpen)
            {
                ShowMainButtons();
            }
        });
    }

    // Legacy compatibility methods
    /// <summary>
    /// Legacy method for Gacha UI compatibility
    /// </summary>
    public void OnGachaUIClosed()
    {
        OnSystemUIClosed();
    }

    /// <summary>
    /// Legacy method for Upgrade UI compatibility  
    /// </summary>
    public void OnUpgradePanelClosed()
    {
        OnSystemUIClosed();
    }

    // Context Menu Methods
    [ContextMenu("Show UI Instantly")]
    public void ShowUIInstantly()
    {
        if (buttonRow == null) return;

        if (autoHideMainButtons && sharedMainButtons != null)
        {
            sharedMainButtons.SetActive(false);
        }

        buttonRow.gameObject.SetActive(true);

        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(true);
        }

        RestoreLayoutControl();
        isUIVisible = true;
    }

    [ContextMenu("Hide UI Instantly")]
    public void HideUIInstantly()
    {
        if (buttonRow == null) return;

        buttonRow.gameObject.SetActive(false);

        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(false);
        }

        RestoreLayoutControl();
        isUIVisible = false;

        if (autoHideMainButtons && sharedMainButtons != null && !systemUIIsOpen)
        {
            sharedMainButtons.SetActive(true);
            if (sharedMainButtonsCanvasGroup != null)
            {
                sharedMainButtonsCanvasGroup.alpha = 1f;
            }
            sharedMainButtons.transform.localScale = sharedMainButtonsOriginalScale;
        }
    }
}
