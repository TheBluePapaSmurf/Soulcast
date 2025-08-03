using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems; // ADD THIS for UI detection

public class AltarInteraction : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform gachaButtonRow;
    public Canvas parentCanvas;

    [Header("Layout Fix")]
    [Tooltip("Drag the invisible button that should ignore layout here")]
    public RectTransform layoutIgnoreButton;

    [Header("Effects")]
    [Tooltip("VFX effect to play continuously while UI is visible")]
    public ParticleSystem continuousVFX;
    [Tooltip("VFX effect to play once when altar is clicked")]
    public ParticleSystem clickVFX;
    [Tooltip("Alternative: GameObject with effects to activate")]
    public GameObject clickEffectObject;
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
    public float hiddenYOffset = -300f; // Only Y offset, X stays same as layout
    public bool animateInReverseOrder = false;

    [Header("Input")]
    public InputActionReference clickAction;

    private Camera playerCamera;
    private bool isUIVisible = false;
    private List<RectTransform> buttonTransforms = new List<RectTransform>();
    private List<Vector2> originalLayoutPositions = new List<Vector2>();
    private List<LayoutElement> buttonLayoutElements = new List<LayoutElement>();
    private Sequence animationSequence;

    // Layout Group (kept active)
    private HorizontalLayoutGroup horizontalLayoutGroup;
    private LayoutElement layoutIgnoreButtonElement;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindAnyObjectByType<Camera>();

        if (gachaButtonRow == null)
            gachaButtonRow = GameObject.Find("GachaButtonRow")?.GetComponent<RectTransform>();

        if (parentCanvas == null)
            parentCanvas = FindAnyObjectByType<Canvas>();

        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.volume = popupVolume;
            }
        }

        // Ensure continuous VFX is stopped at start
        if (continuousVFX != null)
        {
            continuousVFX.Stop();
        }

        InitializeUI();
        EnableClickInput();
    }

    private void Update()
    {
        CheckForCloseClick();
    }

    void InitializeUI()
    {
        if (gachaButtonRow != null)
        {
            // Get reference to layout ignore button's LayoutElement
            if (layoutIgnoreButton != null)
            {
                layoutIgnoreButtonElement = layoutIgnoreButton.GetComponent<LayoutElement>();
                if (layoutIgnoreButtonElement == null)
                {
                    layoutIgnoreButtonElement = layoutIgnoreButton.gameObject.AddComponent<LayoutElement>();
                }
            }

            // BELANGRIJK: Activeer tijdelijk de container om layout posities te kunnen berekenen
            bool wasActive = gachaButtonRow.gameObject.activeSelf;
            gachaButtonRow.gameObject.SetActive(true);

            // Get the horizontal layout group (keep it enabled)
            horizontalLayoutGroup = gachaButtonRow.GetComponent<HorizontalLayoutGroup>();

            // Get all child buttons and add LayoutElements for position control
            buttonTransforms.Clear();
            originalLayoutPositions.Clear();
            buttonLayoutElements.Clear();

            // Force layout to calculate positions first
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
                    // Skip the layout ignore button - don't animate it
                    if (layoutIgnoreButton != null && buttonRect == layoutIgnoreButton)
                    {
                        continue;
                    }

                    buttonTransforms.Add(buttonRect);

                    // Store ORIGINAL layout-controlled positions and DON'T change them
                    originalLayoutPositions.Add(buttonRect.anchoredPosition);

                    Debug.Log($"Button {i} ({buttonRect.name}) original position: {buttonRect.anchoredPosition}");

                    // Ensure each button has a LayoutElement for layout control
                    LayoutElement layoutElement = buttonRect.GetComponent<LayoutElement>();
                    if (layoutElement == null)
                    {
                        layoutElement = buttonRect.gameObject.AddComponent<LayoutElement>();
                    }
                    buttonLayoutElements.Add(layoutElement);
                }
            }

            // Zet terug naar originele staat (meestal false)
            gachaButtonRow.gameObject.SetActive(wasActive);
        }
    }

    void PlayClickEffects()
    {
        // Play one-time click VFX
        if (clickVFX != null)
        {
            clickVFX.Play();
            Debug.Log("Playing click VFX");
        }

        // Activate effect object
        if (clickEffectObject != null)
        {
            clickEffectObject.SetActive(true);

            Debug.Log("Activated click effect object");
        }

        // Play popup sound
        if (popupSoundClip != null && audioSource != null)
        {
            audioSource.volume = popupVolume;
            audioSource.PlayOneShot(popupSoundClip);
            Debug.Log("Playing popup sound");
        }
    }

    void StartContinuousVFX()
    {
        // Start continuous VFX when UI becomes visible
        if (continuousVFX != null)
        {
            continuousVFX.Play();
            Debug.Log("Started continuous VFX");
        }
    }

    void StopContinuousVFX()
    {
        // Stop continuous VFX when UI is fully hidden
        if (continuousVFX != null)
        {
            continuousVFX.Stop();
            Debug.Log("Stopped continuous VFX");
        }
    }

    void DisableLayoutControlForAnimatedButtons()
    {
        // Disable layout control for animated buttons only
        for (int i = 0; i < buttonLayoutElements.Count; i++)
        {
            buttonLayoutElements[i].ignoreLayout = true;
        }

        // Enable ignoreLayout for the layout ignore button (your fix)
        if (layoutIgnoreButtonElement != null)
        {
            layoutIgnoreButtonElement.ignoreLayout = true;
        }
    }

    void SetButtonsToHiddenPositions()
    {
        // Set to hidden position - buttons are already ignoring layout
        for (int i = 0; i < buttonTransforms.Count; i++)
        {
            Vector2 hiddenPos = new Vector2(
                originalLayoutPositions[i].x,  // Keep exact X from ORIGINAL layout positions
                originalLayoutPositions[i].y + hiddenYOffset  // Offset Y only
            );
            buttonTransforms[i].anchoredPosition = hiddenPos;

            Debug.Log($"Button {i} hidden position: {hiddenPos}");
        }
    }

    void RestoreLayoutControl()
    {
        // Re-enable layout control for animated buttons
        for (int i = 0; i < buttonLayoutElements.Count; i++)
        {
            buttonLayoutElements[i].ignoreLayout = false;
        }

        // Keep the layout ignore button ignoring layout (your fix stays active)
        if (layoutIgnoreButtonElement != null)
        {
            layoutIgnoreButtonElement.ignoreLayout = true;
        }

        // Force layout rebuild to restore proper positions
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

        // Kill any ongoing animations
        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        // Stop continuous VFX when script is disabled
        StopContinuousVFX();
    }

    void OnClick(InputAction.CallbackContext context)
    {
        if (playerCamera == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                // Play one-time effects when altar is clicked
                PlayClickEffects();
                ToggleGachaUI();
            }
        }
    }

    // FIXED: Improved close click detection
    void CheckForCloseClick()
    {
        if (!isUIVisible || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // Check if mouse is over any UI element (including our buttons)
        bool isOverUI = EventSystem.current.IsPointerOverGameObject();

        if (isOverUI)
        {
            // Mouse is over a UI element
            if (closeOnUIClick)
            {
                Debug.Log("Closing UI: Clicked on UI element");
                HideGachaUI();
            }
            else
            {
                Debug.Log("Not closing UI: Clicked on UI element, but closeOnUIClick is disabled");
            }
            return;
        }

        // Mouse is not over UI, check for outside clicks
        if (!closeOnOutsideClick)
        {
            Debug.Log("Not closing UI: Outside click detected, but closeOnOutsideClick is disabled");
            return;
        }

        // Check if clicking on the altar itself (don't close on altar clicks)
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                // Clicked on the altar itself, don't close
                Debug.Log("Not closing UI: Clicked on altar");
                return;
            }
        }

        // Clicked outside UI and not on altar
        Debug.Log("Closing UI: Clicked outside");
        HideGachaUI();
    }

    void OnMouseDown()
    {
        // Play one-time effects when altar is clicked via OnMouseDown
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

        // Kill any ongoing animation
        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        // Start coroutine voor stabiele layout handling
        StartCoroutine(ShowGachaUICoroutine());
    }

    IEnumerator ShowGachaUICoroutine()
    {
        // Start continuous VFX IMMEDIATELY when show starts
        StartContinuousVFX();

        // Disable layout control for animated buttons + enable ignore for layout fix button
        DisableLayoutControlForAnimatedButtons();

        // Now safely activate the container 
        gachaButtonRow.gameObject.SetActive(true);

        // Set buttons to hidden positions immediately
        SetButtonsToHiddenPositions();

        // Wait one frame for UI to stabilize in hidden positions
        yield return null;

        // Start animatie sequence
        animationSequence = DOTween.Sequence();

        // Animate each button with delay (normal or reverse order)
        if (animateInReverseOrder)
        {
            // Animate in reverse order: last button first
            for (int i = buttonTransforms.Count - 1; i >= 0; i--)
            {
                int buttonIndex = i; // Local copy voor closure

                // Add delay for each button after the first (in reverse order)
                if (buttonIndex < buttonTransforms.Count - 1)
                {
                    animationSequence.AppendInterval(buttonDelayInterval);
                }

                // Animate to ORIGINAL layout positions
                animationSequence.Append(
                    buttonTransforms[buttonIndex].DOAnchorPos(originalLayoutPositions[buttonIndex], slideInDuration)
                        .SetEase(slideInEase)
                        .OnComplete(() => {
                            Debug.Log($"Button {buttonIndex} reached target: {originalLayoutPositions[buttonIndex]}");
                        })
                );
            }
        }
        else
        {
            // Animate in normal order: first button first
            for (int i = 0; i < buttonTransforms.Count; i++)
            {
                int buttonIndex = i; // Local copy voor closure

                // Add delay for each button after the first
                if (buttonIndex > 0)
                {
                    animationSequence.AppendInterval(buttonDelayInterval);
                }

                // Animate to ORIGINAL layout positions
                animationSequence.Append(
                    buttonTransforms[buttonIndex].DOAnchorPos(originalLayoutPositions[buttonIndex], slideInDuration)
                        .SetEase(slideInEase)
                        .OnComplete(() => {
                            Debug.Log($"Button {buttonIndex} reached target: {originalLayoutPositions[buttonIndex]}");
                        })
                );
            }
        }

        // Set UI as visible when animation completes
        animationSequence.OnComplete(() => {
            // Set final positions manually first
            for (int i = 0; i < buttonTransforms.Count; i++)
            {
                buttonTransforms[i].anchoredPosition = originalLayoutPositions[i];
            }

            // THEN restore layout control (keeping layout ignore button ignored)
            RestoreLayoutControl();
            isUIVisible = true;

            // VFX continues to loop here while UI is visible
        });
    }

    public void HideGachaUI()
    {
        if (gachaButtonRow == null || !isUIVisible || buttonTransforms.Count == 0) return;

        // Kill any ongoing animation
        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        // Disable layout control for animation
        for (int i = 0; i < buttonLayoutElements.Count; i++)
        {
            buttonLayoutElements[i].ignoreLayout = true;
        }

        // Create reverse animation sequence
        animationSequence = DOTween.Sequence();

        // Hide animation always uses reverse order for nice visual effect
        for (int i = buttonTransforms.Count - 1; i >= 0; i--)
        {
            int buttonIndex = i; // Local copy voor closure

            // Hidden position uses ORIGINAL layout positions
            Vector2 hiddenPos = new Vector2(
                originalLayoutPositions[buttonIndex].x,  // Keep exact X from ORIGINAL layout positions
                originalLayoutPositions[buttonIndex].y + hiddenYOffset  // Offset Y only
            );

            // Add delay for each button after the first
            if (buttonIndex < buttonTransforms.Count - 1)
            {
                animationSequence.AppendInterval(buttonDelayInterval * 0.3f); // Faster hide delay
            }

            // Animate this button sliding out
            animationSequence.Append(
                buttonTransforms[buttonIndex].DOAnchorPos(hiddenPos, slideInDuration * 0.7f)
                    .SetEase(Ease.InBack)
            );
        }

        // Hide the container and reset state when animation completes
        animationSequence.OnComplete(() => {
            gachaButtonRow.gameObject.SetActive(false);
            RestoreLayoutControl();
            isUIVisible = false;

            // STOP continuous VFX when hide animation is completely finished
            StopContinuousVFX();
        });
    }

    // Method to instantly show/hide without animation (useful for testing)
    [ContextMenu("Show UI Instantly")]
    public void ShowUIInstantly()
    {
        if (gachaButtonRow == null) return;

        gachaButtonRow.gameObject.SetActive(true);
        RestoreLayoutControl();
        isUIVisible = true;
        StartContinuousVFX(); // Start VFX for instant show
    }

    [ContextMenu("Hide UI Instantly")]
    public void HideUIInstantly()
    {
        if (gachaButtonRow == null) return;

        gachaButtonRow.gameObject.SetActive(false);
        RestoreLayoutControl();
        isUIVisible = false;
        StopContinuousVFX(); // Stop VFX for instant hide
    }

    // Runtime method to toggle animation order
    [ContextMenu("Toggle Animation Order")]
    public void ToggleAnimationOrder()
    {
        animateInReverseOrder = !animateInReverseOrder;
        Debug.Log($"Animation order set to: {(animateInReverseOrder ? "Reverse" : "Normal")}");
    }

    // Test methods for effects
    [ContextMenu("Test Click Effects")]
    public void TestClickEffects()
    {
        PlayClickEffects();
    }

    [ContextMenu("Test Continuous VFX Start")]
    public void TestStartContinuousVFX()
    {
        StartContinuousVFX();
    }

    [ContextMenu("Test Continuous VFX Stop")]
    public void TestStopContinuousVFX()
    {
        StopContinuousVFX();
    }
}
