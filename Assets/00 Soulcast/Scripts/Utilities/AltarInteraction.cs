using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class AltarInteraction : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform gachaButtonRow;
    public Canvas parentCanvas;

    [Header("🆕 Main Buttons Management")]
    [SerializeField] private GameObject mainButtons;
    [SerializeField] private bool autoHideMainButtons = true;
    [SerializeField] private bool animateMainButtons = true;
    [SerializeField] private float mainButtonsAnimationDuration = 0.3f;
    [SerializeField] private Ease mainButtonsAnimationEase = Ease.OutCubic;

    [Header("🎮 GachaUI Integration")]
    [SerializeField] private GachaUI gachaUI;
    [SerializeField] private bool autoFindGachaUI = true;
    [SerializeField] private bool autoHideGachaButtonsOnSummon = true;

    [Header("🔒 Interaction Control")]
    [SerializeField] private bool allowInteraction = true;
    [SerializeField] private bool showInteractionBlockedFeedback = true;
    [SerializeField] private float blockedClickCooldown = 0.5f;

    [Header("🎬 Magic Aura Effect")]
    [Tooltip("CFXR3 Magic Aura effect to sync with GachaButtonRow")]
    public GameObject magicAuraEffect;
    [SerializeField] private bool autoFindMagicAura = true;

    [Header("Layout Fix")]
    [Tooltip("Drag the invisible button that should ignore layout here")]
    public RectTransform layoutIgnoreButton;

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
    private LayoutElement layoutIgnoreButtonElement;

    // MainButtons management
    private CanvasGroup mainButtonsCanvasGroup;
    private Vector3 mainButtonsOriginalScale;

    // UI state tracking
    private bool gachaUIIsOpen = false;
    private Button summonButton;

    // Interaction blocking
    private bool isInteractionBlocked = false;
    private float lastBlockedClickTime = 0f;

    public static AltarInteraction Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindAnyObjectByType<Camera>();

        if (gachaButtonRow == null)
            gachaButtonRow = GameObject.Find("GachaButtonRow")?.GetComponent<RectTransform>();

        if (parentCanvas == null)
            parentCanvas = FindAnyObjectByType<Canvas>();

        SetupMainButtonsReference();
        SetupGachaUIIntegration();
        SetupMagicAuraEffect();
        SetupAudioSource();

        InitializeUI();
        EnableClickInput();
    }

    void SetupMainButtonsReference()
    {
        if (mainButtons == null)
        {
            mainButtons = GameObject.Find("MainButtons");
            if (mainButtons == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform canvasTransform = canvas.transform;
                    for (int i = 0; i < canvasTransform.childCount; i++)
                    {
                        Transform child = canvasTransform.GetChild(i);
                        if (child.name == "MainButtons")
                        {
                            mainButtons = child.gameObject;
                            break;
                        }
                    }
                }
            }
        }

        if (mainButtons != null)
        {
            mainButtonsOriginalScale = mainButtons.transform.localScale;
            mainButtonsCanvasGroup = mainButtons.GetComponent<CanvasGroup>();
            if (mainButtonsCanvasGroup == null)
            {
                mainButtonsCanvasGroup = mainButtons.AddComponent<CanvasGroup>();
            }

            if (showDebugLogs) Debug.Log($"🎮 MainButtons reference setup complete: {mainButtons.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ MainButtons GameObject not found! Auto-hide feature will be disabled.");
            autoHideMainButtons = false;
        }
    }

    void SetupGachaUIIntegration()
    {
        if (autoFindGachaUI && gachaUI == null)
        {
            gachaUI = FindAnyObjectByType<GachaUI>();
            if (gachaUI == null)
            {
                Debug.LogWarning("⚠️ GachaUI not found! Button management may not work correctly.");
            }
        }

        if (gachaButtonRow != null)
        {
            Transform summonBtnTransform = gachaButtonRow.Find("SummonBtn");
            if (summonBtnTransform != null)
            {
                summonButton = summonBtnTransform.GetComponent<Button>();
                if (summonButton != null)
                {
                    summonButton.onClick.AddListener(OnSummonButtonClicked);
                    if (showDebugLogs) Debug.Log("✅ Summon button listener added");
                }
            }
        }

        if (showDebugLogs) Debug.Log($"🎮 GachaUI integration setup - GachaUI: {gachaUI != null}, SummonButton: {summonButton != null}");
    }

    void SetupMagicAuraEffect()
    {
        if (autoFindMagicAura && magicAuraEffect == null)
        {
            Transform magicAuraTransform = transform.Find("CFXR3 Magic Aura A (Runic)");
            if (magicAuraTransform != null)
            {
                magicAuraEffect = magicAuraTransform.gameObject;
                if (showDebugLogs) Debug.Log($"✅ Auto-found Magic Aura: {magicAuraEffect.name}");
            }
        }

        if (showDebugLogs) Debug.Log($"🎬 Magic Aura Effect setup - Found: {magicAuraEffect != null}");
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

    void OnSummonButtonClicked()
    {
        if (!autoHideGachaButtonsOnSummon) return;

        var cutsceneManager = SummonCutsceneManager.Instance;
        if (cutsceneManager != null && cutsceneManager.IsCutscenePlaying)
        {
            Debug.Log("⚠️ Cannot open Gacha UI: Cutscene is already playing");
            return;
        }

        if (showDebugLogs) Debug.Log("🎮 Summon button clicked - hiding GachaButtonRow and opening GachaUI");

        SetInteractionEnabled(false);
        HideGachaUI();

        if (gachaUI != null)
        {
            gachaUI.OpenGachaUI();
            gachaUIIsOpen = true;
        }
    }

    public void OnGachaUIClosed()
    {
        if (gachaUIIsOpen)
        {
            if (showDebugLogs) Debug.Log("🎮 GachaUI closed - re-enabling altar interaction and showing MainButtons");
            gachaUIIsOpen = false;
            SetInteractionEnabled(true);
            ShowMainButtons();
        }
    }

    public void SetInteractionEnabled(bool enabled)
    {
        isInteractionBlocked = !enabled;
        allowInteraction = enabled;

        if (showDebugLogs)
        {
            Debug.Log($"🔒 Altar interaction {(enabled ? "ENABLED" : "DISABLED")}");
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
            Debug.Log("🔒 Altar interaction is currently blocked (GachaUI is open)");
        }
    }

    private void Update()
    {
        if (isUIVisible && gachaButtonRow != null && gachaButtonRow.gameObject.activeSelf && !isInteractionBlocked)
        {
            CheckForCloseClick();
        }
    }

    void InitializeUI()
    {
        if (gachaButtonRow != null)
        {
            if (layoutIgnoreButton != null)
            {
                layoutIgnoreButtonElement = layoutIgnoreButton.GetComponent<LayoutElement>();
                if (layoutIgnoreButtonElement == null)
                {
                    layoutIgnoreButtonElement = layoutIgnoreButton.gameObject.AddComponent<LayoutElement>();
                }
            }

            bool wasActive = gachaButtonRow.gameObject.activeSelf;
            gachaButtonRow.gameObject.SetActive(true);

            horizontalLayoutGroup = gachaButtonRow.GetComponent<HorizontalLayoutGroup>();

            buttonTransforms.Clear();
            originalLayoutPositions.Clear();
            buttonLayoutElements.Clear();

            if (horizontalLayoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gachaButtonRow);
                Canvas.ForceUpdateCanvases();
            }

            for (int i = 0; i < gachaButtonRow.childCount; i++)
            {
                Transform child = gachaButtonRow.GetChild(i);
                RectTransform buttonRect = child.GetComponent<RectTransform>();

                if (buttonRect != null)
                {
                    if (layoutIgnoreButton != null && buttonRect == layoutIgnoreButton)
                    {
                        continue;
                    }

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

            gachaButtonRow.gameObject.SetActive(wasActive);
        }
    }

    void PlayClickEffects()
    {
        if (popupSoundClip != null && audioSource != null)
        {
            audioSource.volume = popupVolume;
            audioSource.PlayOneShot(popupSoundClip);
            if (showDebugLogs) Debug.Log("🔊 Playing popup sound");
        }
    }

    void HideMainButtons()
    {
        if (!autoHideMainButtons || mainButtons == null || mainButtonsCanvasGroup == null) return;

        if (showDebugLogs) Debug.Log("🔽 Hiding MainButtons");

        if (animateMainButtons)
        {
            mainButtonsCanvasGroup.DOFade(0f, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase);

            mainButtons.transform.DOScale(0.8f, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase)
                .OnComplete(() => {
                    mainButtons.SetActive(false);
                    if (showDebugLogs) Debug.Log("✅ MainButtons hidden");
                });
        }
        else
        {
            mainButtons.SetActive(false);
            if (showDebugLogs) Debug.Log("✅ MainButtons hidden instantly");
        }
    }

    void ShowMainButtons()
    {
        if (!autoHideMainButtons || mainButtons == null || mainButtonsCanvasGroup == null) return;

        if (showDebugLogs) Debug.Log("🔼 Showing MainButtons");

        if (animateMainButtons)
        {
            mainButtons.SetActive(true);
            mainButtonsCanvasGroup.alpha = 0f;
            mainButtons.transform.localScale = Vector3.one * 0.8f;

            mainButtonsCanvasGroup.DOFade(1f, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase);

            mainButtons.transform.DOScale(mainButtonsOriginalScale, mainButtonsAnimationDuration)
                .SetEase(mainButtonsAnimationEase)
                .OnComplete(() => {
                    if (showDebugLogs) Debug.Log("✅ MainButtons shown");
                });
        }
        else
        {
            mainButtons.SetActive(true);
            mainButtonsCanvasGroup.alpha = 1f;
            mainButtons.transform.localScale = mainButtonsOriginalScale;
            if (showDebugLogs) Debug.Log("✅ MainButtons shown instantly");
        }
    }

    void DisableLayoutControlForAnimatedButtons()
    {
        for (int i = 0; i < buttonLayoutElements.Count; i++)
        {
            buttonLayoutElements[i].ignoreLayout = true;
        }

        if (layoutIgnoreButtonElement != null)
        {
            layoutIgnoreButtonElement.ignoreLayout = true;
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

        if (layoutIgnoreButtonElement != null)
        {
            layoutIgnoreButtonElement.ignoreLayout = true;
        }

        if (horizontalLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gachaButtonRow);
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

        if (summonButton != null)
        {
            summonButton.onClick.RemoveListener(OnSummonButtonClicked);
        }

        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(false);
        }

        if (autoHideMainButtons && !isUIVisible && !gachaUIIsOpen)
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
                ToggleGachaUI();
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
                if (showDebugLogs) Debug.Log("Closing GachaButtonRow: Clicked on UI element");
                HideGachaUI();
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

        if (showDebugLogs) Debug.Log("Closing GachaButtonRow: Clicked outside");
        HideGachaUI();
    }

    void OnMouseDown()
    {
        if (isInteractionBlocked || !allowInteraction)
        {
            ShowBlockedInteractionFeedback();
            return;
        }

        PlayClickEffects();
        ToggleGachaUI();
    }

    public void ToggleGachaUI()
    {
        if (gachaButtonRow == null) return;

        if (!isUIVisible)
        {
            ShowGachaUI();
        }
        else
        {
            HideGachaUI();
        }
    }

    public void ShowGachaUI()
    {
        if (gachaButtonRow == null || isUIVisible || buttonTransforms.Count == 0) return;

        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        HideMainButtons();
        StartCoroutine(ShowGachaUICoroutine());
    }

    IEnumerator ShowGachaUICoroutine()
    {
        DisableLayoutControlForAnimatedButtons();

        gachaButtonRow.gameObject.SetActive(true);

        // 🎬 Activate Magic Aura at the same time as GachaButtonRow
        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(true);
            if (showDebugLogs) Debug.Log("🎬 Magic Aura activated with GachaButtonRow");
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
                        .OnComplete(() => {
                            if (showDebugLogs) Debug.Log($"Button {buttonIndex} reached target: {originalLayoutPositions[buttonIndex]}");
                        })
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
                        .OnComplete(() => {
                            if (showDebugLogs) Debug.Log($"Button {buttonIndex} reached target: {originalLayoutPositions[buttonIndex]}");
                        })
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

    public void HideGachaUI()
    {
        if (gachaButtonRow == null || !isUIVisible || buttonTransforms.Count == 0) return;

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
            gachaButtonRow.gameObject.SetActive(false);

            // 🎬 Deactivate Magic Aura at the same time as GachaButtonRow
            if (magicAuraEffect != null)
            {
                magicAuraEffect.SetActive(false);
                if (showDebugLogs) Debug.Log("🎬 Magic Aura deactivated with GachaButtonRow");
            }

            RestoreLayoutControl();
            isUIVisible = false;

            if (!gachaUIIsOpen)
            {
                ShowMainButtons();
            }
        });
    }

    // Essential Context Menu Methods Only
    [ContextMenu("Show UI Instantly")]
    public void ShowUIInstantly()
    {
        if (gachaButtonRow == null) return;

        if (autoHideMainButtons && mainButtons != null)
        {
            mainButtons.SetActive(false);
        }

        gachaButtonRow.gameObject.SetActive(true);

        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(true);
            if (showDebugLogs) Debug.Log("🎬 Magic Aura activated instantly with GachaButtonRow");
        }

        RestoreLayoutControl();
        isUIVisible = true;
    }

    [ContextMenu("Hide UI Instantly")]
    public void HideUIInstantly()
    {
        if (gachaButtonRow == null) return;

        gachaButtonRow.gameObject.SetActive(false);

        if (magicAuraEffect != null)
        {
            magicAuraEffect.SetActive(false);
            if (showDebugLogs) Debug.Log("🎬 Magic Aura deactivated instantly with GachaButtonRow");
        }

        RestoreLayoutControl();
        isUIVisible = false;

        if (autoHideMainButtons && mainButtons != null && !gachaUIIsOpen)
        {
            mainButtons.SetActive(true);
            if (mainButtonsCanvasGroup != null)
            {
                mainButtonsCanvasGroup.alpha = 1f;
            }
            mainButtons.transform.localScale = mainButtonsOriginalScale;
        }
    }
}
