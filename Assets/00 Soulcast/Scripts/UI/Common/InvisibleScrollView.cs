using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(ScrollRect))]
public class InvisibleScrollView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
{
    [Header("Scroll Settings")]
    [SerializeField] private bool enableHorizontalScroll = true;
    [SerializeField] private bool enableVerticalScroll = true;
    [SerializeField] private bool enableMouseWheelScroll = true;
    [SerializeField] private bool enableKeyboardScroll = true;
    [SerializeField] private bool enableTouchScroll = true;

    [Header("Scroll Sensitivity")]
    [Range(0.1f, 5.0f)]
    [SerializeField] private float mouseWheelSensitivity = 1.0f;
    [Range(0.1f, 5.0f)]
    [SerializeField] private float keyboardScrollSpeed = 2.0f;
    [Range(0.1f, 5.0f)]
    [SerializeField] private float touchScrollSensitivity = 1.0f;

    [Header("Smooth Scrolling")]
    [SerializeField] private bool enableSmoothScrolling = true;
    [Range(0.1f, 2.0f)]
    [SerializeField] private float smoothScrollDuration = 0.3f;
    [SerializeField] private AnimationCurve smoothScrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scroll Boundaries")]
    [SerializeField] private bool enableScrollBounds = true;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float horizontalScrollPadding = 0.1f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float verticalScrollPadding = 0.1f;

    [Header("Auto-hide Content")]
    [SerializeField] private bool hideScrollbarsOnStart = true;
    [SerializeField] private bool showScrollIndicators = false;
    [SerializeField] private GameObject scrollIndicatorPrefab;

    [Header("Performance")]
    [SerializeField] private bool enableVirtualization = false;
    [SerializeField] private int visibleItemBuffer = 2;

    // Private components
    private ScrollRect scrollRect;
    private RectTransform contentTransform;
    private RectTransform viewportTransform;
    private CanvasGroup canvasGroup;

    // State tracking
    private bool isMouseOver = false;
    private bool isScrolling = false;
    private Vector2 targetScrollPosition;
    private Coroutine smoothScrollCoroutine;

    // Input tracking
    private Vector2 lastTouchPosition;
    private bool isTouching = false;

    // Scroll indicators
    private GameObject horizontalIndicator;
    private GameObject verticalIndicator;

    // Events
    public System.Action<Vector2> OnScrollPositionChanged;
    public System.Action OnScrollStart;
    public System.Action OnScrollEnd;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        SetupInvisibleScrollbars();
        if (showScrollIndicators)
            CreateScrollIndicators();
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleTouchInput();
        UpdateScrollIndicators();
    }

    #region Initialization

    private void InitializeComponents()
    {
        scrollRect = GetComponent<ScrollRect>();
        contentTransform = scrollRect.content;
        viewportTransform = scrollRect.viewport;

        if (viewportTransform == null)
            viewportTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Configure ScrollRect
        scrollRect.horizontal = enableHorizontalScroll;
        scrollRect.vertical = enableVerticalScroll;
        scrollRect.movementType = enableScrollBounds ? ScrollRect.MovementType.Clamped : ScrollRect.MovementType.Unrestricted;

        // Add scroll event listener
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

        targetScrollPosition = scrollRect.normalizedPosition;
    }

    private void SetupInvisibleScrollbars()
    {
        if (!hideScrollbarsOnStart) return;

        // Hide horizontal scrollbar
        if (scrollRect.horizontalScrollbar != null)
        {
            var hScrollbar = scrollRect.horizontalScrollbar;
            hScrollbar.gameObject.SetActive(false);
            // Keep reference but make invisible
            var hCanvasGroup = hScrollbar.GetComponent<CanvasGroup>();
            if (hCanvasGroup == null)
                hCanvasGroup = hScrollbar.gameObject.AddComponent<CanvasGroup>();
            hCanvasGroup.alpha = 0f;
            hCanvasGroup.interactable = false;
        }

        // Hide vertical scrollbar
        if (scrollRect.verticalScrollbar != null)
        {
            var vScrollbar = scrollRect.verticalScrollbar;
            vScrollbar.gameObject.SetActive(false);
            // Keep reference but make invisible
            var vCanvasGroup = vScrollbar.GetComponent<CanvasGroup>();
            if (vCanvasGroup == null)
                vCanvasGroup = vScrollbar.gameObject.AddComponent<CanvasGroup>();
            vCanvasGroup.alpha = 0f;
            vCanvasGroup.interactable = false;
        }

        Debug.Log("🚫 ScrollView scrollbars hidden while maintaining functionality");
    }

    #endregion

    #region Input Handling

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!enableMouseWheelScroll || !isMouseOver) return;

        Vector2 scrollDelta = eventData.scrollDelta * mouseWheelSensitivity;

        if (enableVerticalScroll)
        {
            float verticalScroll = scrollDelta.y * 0.1f;
            ScrollVertically(verticalScroll);
        }

        if (enableHorizontalScroll)
        {
            float horizontalScroll = scrollDelta.x * 0.1f;
            ScrollHorizontally(horizontalScroll);
        }
    }

    private void HandleKeyboardInput()
    {
        if (!enableKeyboardScroll || !isMouseOver) return;

        float horizontalInput = 0f;
        float verticalInput = 0f;

        // Arrow keys and WASD
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            horizontalInput = 1f;

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            verticalInput = 1f;
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            verticalInput = -1f;

        // Page Up/Down
        if (Input.GetKey(KeyCode.PageUp))
            verticalInput = 3f;
        else if (Input.GetKey(KeyCode.PageDown))
            verticalInput = -3f;

        // Home/End
        if (Input.GetKeyDown(KeyCode.Home))
            ScrollToTop();
        else if (Input.GetKeyDown(KeyCode.End))
            ScrollToBottom();

        // Apply input
        if (horizontalInput != 0f || verticalInput != 0f)
        {
            float scrollSpeed = keyboardScrollSpeed * Time.deltaTime;

            if (enableHorizontalScroll && horizontalInput != 0f)
                ScrollHorizontally(horizontalInput * scrollSpeed);

            if (enableVerticalScroll && verticalInput != 0f)
                ScrollVertically(verticalInput * scrollSpeed);
        }
    }

    private void HandleTouchInput()
    {
        if (!enableTouchScroll) return;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    isTouching = true;
                    lastTouchPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                    if (isTouching)
                    {
                        Vector2 deltaPosition = touch.position - lastTouchPosition;
                        deltaPosition *= touchScrollSensitivity * 0.01f;

                        if (enableHorizontalScroll)
                            ScrollHorizontally(-deltaPosition.x);
                        if (enableVerticalScroll)
                            ScrollVertically(deltaPosition.y);

                        lastTouchPosition = touch.position;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isTouching = false;
                    break;
            }
        }
#endif
    }

    #endregion

    #region Scroll Methods

    private void ScrollHorizontally(float delta)
    {
        if (!enableHorizontalScroll) return;

        float newX = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + delta);

        if (enableSmoothScrolling)
        {
            targetScrollPosition.x = newX;
            StartSmoothScroll();
        }
        else
        {
            scrollRect.horizontalNormalizedPosition = newX;
        }
    }

    private void ScrollVertically(float delta)
    {
        if (!enableVerticalScroll) return;

        float newY = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta);

        if (enableSmoothScrolling)
        {
            targetScrollPosition.y = newY;
            StartSmoothScroll();
        }
        else
        {
            scrollRect.verticalNormalizedPosition = newY;
        }
    }

    private void StartSmoothScroll()
    {
        if (smoothScrollCoroutine != null)
            StopCoroutine(smoothScrollCoroutine);

        smoothScrollCoroutine = StartCoroutine(SmoothScrollCoroutine());
    }

    private IEnumerator SmoothScrollCoroutine()
    {
        Vector2 startPosition = scrollRect.normalizedPosition;
        float elapsedTime = 0f;

        while (elapsedTime < smoothScrollDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / smoothScrollDuration;
            t = smoothScrollCurve.Evaluate(t);

            scrollRect.normalizedPosition = Vector2.Lerp(startPosition, targetScrollPosition, t);
            yield return null;
        }

        scrollRect.normalizedPosition = targetScrollPosition;
        smoothScrollCoroutine = null;
    }

    #endregion

    #region Public API

    public void ScrollToTop()
    {
        if (enableSmoothScrolling)
        {
            targetScrollPosition.y = 1f;
            StartSmoothScroll();
        }
        else
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ScrollToBottom()
    {
        if (enableSmoothScrolling)
        {
            targetScrollPosition.y = 0f;
            StartSmoothScroll();
        }
        else
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ScrollToLeft()
    {
        if (enableSmoothScrolling)
        {
            targetScrollPosition.x = 0f;
            StartSmoothScroll();
        }
        else
        {
            scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    public void ScrollToRight()
    {
        if (enableSmoothScrolling)
        {
            targetScrollPosition.x = 1f;
            StartSmoothScroll();
        }
        else
        {
            scrollRect.horizontalNormalizedPosition = 1f;
        }
    }

    public void ScrollToPosition(Vector2 normalizedPosition)
    {
        normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
        normalizedPosition.y = Mathf.Clamp01(normalizedPosition.y);

        if (enableSmoothScrolling)
        {
            targetScrollPosition = normalizedPosition;
            StartSmoothScroll();
        }
        else
        {
            scrollRect.normalizedPosition = normalizedPosition;
        }
    }

    public void ScrollToElement(RectTransform target)
    {
        if (target == null || contentTransform == null) return;

        Vector2 contentSize = contentTransform.rect.size;
        Vector2 viewportSize = viewportTransform.rect.size;
        Vector2 targetPosition = (Vector2)contentTransform.InverseTransformPoint(target.position);

        // Calculate normalized position
        Vector2 normalizedPos = new Vector2(
            (targetPosition.x + contentSize.x * 0.5f) / (contentSize.x - viewportSize.x),
            (-targetPosition.y + contentSize.y * 0.5f) / (contentSize.y - viewportSize.y)
        );

        ScrollToPosition(normalizedPos);
    }

    public void SetScrollSensitivity(float mouseSensitivity, float keyboardSpeed, float touchSensitivity)
    {
        mouseWheelSensitivity = Mathf.Clamp(mouseSensitivity, 0.1f, 5.0f);
        keyboardScrollSpeed = Mathf.Clamp(keyboardSpeed, 0.1f, 5.0f);
        touchScrollSensitivity = Mathf.Clamp(touchSensitivity, 0.1f, 5.0f);
    }

    public void EnableScroll(bool horizontal, bool vertical)
    {
        enableHorizontalScroll = horizontal;
        enableVerticalScroll = vertical;
        scrollRect.horizontal = horizontal;
        scrollRect.vertical = vertical;
    }

    public void SetSmoothScrolling(bool enabled, float duration = 0.3f)
    {
        enableSmoothScrolling = enabled;
        smoothScrollDuration = Mathf.Clamp(duration, 0.1f, 2.0f);
    }

    #endregion

    #region Scroll Indicators

    private void CreateScrollIndicators()
    {
        if (scrollIndicatorPrefab == null) return;

        // Create horizontal indicator
        if (enableHorizontalScroll)
        {
            horizontalIndicator = Instantiate(scrollIndicatorPrefab, transform);
            horizontalIndicator.name = "Horizontal Scroll Indicator";
            // Position and configure horizontal indicator
        }

        // Create vertical indicator  
        if (enableVerticalScroll)
        {
            verticalIndicator = Instantiate(scrollIndicatorPrefab, transform);
            verticalIndicator.name = "Vertical Scroll Indicator";
            // Position and configure vertical indicator
        }
    }

    private void UpdateScrollIndicators()
    {
        if (!showScrollIndicators) return;

        // Update horizontal indicator
        if (horizontalIndicator != null && enableHorizontalScroll)
        {
            float alpha = isScrolling ? 1f : 0.3f;
            var canvasGroup = horizontalIndicator.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }

        // Update vertical indicator
        if (verticalIndicator != null && enableVerticalScroll)
        {
            float alpha = isScrolling ? 1f : 0.3f;
            var canvasGroup = verticalIndicator.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }
    }

    #endregion

    #region Event Handlers

    private void OnScrollValueChanged(Vector2 position)
    {
        targetScrollPosition = position;

        // Update scrolling state
        bool wasScrolling = isScrolling;
        isScrolling = true;

        if (!wasScrolling)
            OnScrollStart?.Invoke();

        OnScrollPositionChanged?.Invoke(position);

        // Stop scrolling detection
        StopCoroutine(nameof(StopScrollingDetection));
        StartCoroutine(nameof(StopScrollingDetection));
    }

    private IEnumerator StopScrollingDetection()
    {
        yield return new WaitForSeconds(0.1f);
        isScrolling = false;
        OnScrollEnd?.Invoke();
    }

    #endregion

    #region Context Menu (Editor Only)

    [ContextMenu("Scroll to Top")]
    private void ContextScrollToTop()
    {
        if (Application.isPlaying)
            ScrollToTop();
    }

    [ContextMenu("Scroll to Bottom")]
    private void ContextScrollToBottom()
    {
        if (Application.isPlaying)
            ScrollToBottom();
    }

    [ContextMenu("Scroll to Center")]
    private void ContextScrollToCenter()
    {
        if (Application.isPlaying)
            ScrollToPosition(Vector2.one * 0.5f);
    }

    [ContextMenu("Reset Scroll Position")]
    private void ContextResetScrollPosition()
    {
        if (Application.isPlaying)
            ScrollToPosition(Vector2.zero);
    }

    #endregion

    #region Gizmos (Editor Only)

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (scrollRect == null) return;

        // Draw scroll bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, viewportTransform.rect.size);

        // Draw content bounds
        if (contentTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(contentTransform.position, contentTransform.rect.size);
        }
    }
#endif

    #endregion
}
