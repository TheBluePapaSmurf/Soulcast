using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using TMPro;

public class TimingCircle : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual Components")]
    public Image outerCircle;
    public Image innerCircle;
    public Image perfectZone;

    [Header("Individual Feedback")]
    public CanvasGroup feedbackCanvasGroup;      // ✅ NIEUW
    public TextMeshProUGUI feedbackText;         // ✅ NIEUW
    public float feedbackDuration = 0.8f;        // ✅ NIEUW

    [Header("Animation Settings")]
    public float impactDuration = 0.3f;
    public float impactIntensity = 1.2f;
    public AnimationCurve impactCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

    [Header("Feedback Colors")]
    public Color perfectTextColor = Color.green;
    public Color missTextColor = Color.red;

    // ✅ Events
    public event Action<TimingResult> OnTimingComplete;

    public enum TimingResult { Miss, Perfect }

    // ✅ Configuration (set by TimingCircleManager)
    private float shrinkDuration = 2f;
    private float perfectZoneSize = 0.8f;
    private float perfectZoneTolerance = 0.05f;
    private Color normalColor = Color.red;
    private Color perfectColor = Color.green;

    // ✅ State
    private bool isTimingActive = false;
    private bool isCompleted = false;

    void Awake()
    {
        // Auto-find components if not assigned
        if (outerCircle == null || innerCircle == null)
        {
            Image[] images = GetComponentsInChildren<Image>();

            foreach (Image img in images)
            {
                string imgName = img.name.ToLower();
                if (imgName.Contains("outer") && outerCircle == null)
                    outerCircle = img;
                else if (imgName.Contains("inner") && innerCircle == null)
                    innerCircle = img;
                else if (imgName.Contains("perfect") && perfectZone == null)
                    perfectZone = img;
            }

            // Fallback assignment
            if (innerCircle == null && images.Length > 1)
            {
                outerCircle = images[0];
                innerCircle = images[1];
                if (images.Length > 2) perfectZone = images[2];
            }
        }

        // ✅ Auto-find feedback components
        if (feedbackCanvasGroup == null)
        {
            feedbackCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (feedbackText == null)
        {
            feedbackText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // ✅ Setup initial feedback state
        if (feedbackCanvasGroup != null)
        {
            feedbackCanvasGroup.alpha = 0f;
        }

        Debug.Log($"TimingCircle components: Outer={outerCircle != null}, Inner={innerCircle != null}, Perfect={perfectZone != null}, Feedback={feedbackText != null}");
    }

    // ✅ Configure timing circle (called by TimingCircleManager)
    public void Configure(float shrinkDuration, float perfectZoneSize, float perfectZoneTolerance, Color normalColor, Color perfectColor)
    {
        this.shrinkDuration = shrinkDuration;
        this.perfectZoneSize = perfectZoneSize;
        this.perfectZoneTolerance = perfectZoneTolerance;
        this.normalColor = normalColor;
        this.perfectColor = perfectColor;

        Debug.Log($"TimingCircle configured: Duration={shrinkDuration}, PerfectZone={perfectZoneSize}");
    }

    // ✅ Start timing challenge
    public void StartTimingChallenge()
    {
        if (innerCircle == null)
        {
            Debug.LogError("InnerCircle not found! Cannot start timing challenge.");
            CompleteWithResult(TimingResult.Miss);
            return;
        }

        isTimingActive = true;
        isCompleted = false;

        // Setup initial state
        innerCircle.color = normalColor;
        innerCircle.transform.localScale = Vector3.one;

        // Hide feedback initially
        if (feedbackCanvasGroup != null)
        {
            feedbackCanvasGroup.alpha = 0f;
        }

        Debug.Log("TimingCircle: Starting timing challenge");

        // Start shrink animation
        StartCoroutine(ShrinkAnimation());
    }

    // ✅ Shrink animation
    private IEnumerator ShrinkAnimation()
    {
        float elapsedTime = 0f;

        while (elapsedTime < shrinkDuration && isTimingActive && !isCompleted)
        {
            float t = elapsedTime / shrinkDuration;
            float scale = Mathf.Lerp(1f, 0f, t);
            innerCircle.transform.localScale = Vector3.one * scale;

            // Update color based on perfect zone
            UpdateCircleColor(scale);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // Auto-complete as miss if not clicked
        if (!isCompleted)
        {
            Debug.Log("TimingCircle: Timed out - MISS");
            CompleteWithResult(TimingResult.Miss);
        }
    }

    // ✅ Update circle color based on current scale
    private void UpdateCircleColor(float currentScale)
    {
        float perfectZoneMin = perfectZoneSize - perfectZoneTolerance;
        float perfectZoneMax = perfectZoneSize + perfectZoneTolerance;

        if (currentScale <= perfectZoneMax && currentScale >= perfectZoneMin)
        {
            innerCircle.color = perfectColor;
        }
        else
        {
            innerCircle.color = normalColor;
        }
    }

    // ✅ Handle mouse/touch click
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isTimingActive || isCompleted) return;

        Debug.Log("TimingCircle: Click detected!");

        // Check timing
        float currentScale = innerCircle.transform.localScale.x;
        float perfectZoneMin = perfectZoneSize - perfectZoneTolerance;
        float perfectZoneMax = perfectZoneSize + perfectZoneTolerance;

        TimingResult result;
        if (currentScale <= perfectZoneMax && currentScale >= perfectZoneMin)
        {
            result = TimingResult.Perfect;
            Debug.Log($"TimingCircle: PERFECT! (scale: {currentScale})");
        }
        else
        {
            result = TimingResult.Miss;
            Debug.Log($"TimingCircle: MISS! (scale: {currentScale})");
        }

        CompleteWithResult(result);
    }

    // ✅ Complete timing with result
    private void CompleteWithResult(TimingResult result)
    {
        if (isCompleted) return;

        isCompleted = true;
        isTimingActive = false;

        Debug.Log($"TimingCircle: Completed with result: {result}");

        // ✅ Show individual feedback
        StartCoroutine(ShowIndividualFeedback(result));

        // Play impact animation
        StartCoroutine(PlayImpactAnimation());

        // Notify manager
        OnTimingComplete?.Invoke(result);
    }

    // ✅ NEW: Show individual feedback per circle
    private IEnumerator ShowIndividualFeedback(TimingResult result)
    {
        if (feedbackText == null || feedbackCanvasGroup == null) yield break;

        // Setup feedback text
        switch (result)
        {
            case TimingResult.Perfect:
                feedbackText.text = "PERFECT!";
                feedbackText.color = perfectTextColor;
                break;
            case TimingResult.Miss:
                feedbackText.text = "MISS!";
                feedbackText.color = missTextColor;
                break;
        }

        // Fade in quickly
        float fadeInDuration = 0.1f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            float t = elapsedTime / fadeInDuration;
            feedbackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        feedbackCanvasGroup.alpha = 1f;

        // Hold feedback visible
        yield return new WaitForSecondsRealtime(feedbackDuration * 0.4f);

        // Fade out
        float fadeOutDuration = feedbackDuration * 0.6f;
        elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            float t = elapsedTime / fadeOutDuration;
            feedbackCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        feedbackCanvasGroup.alpha = 0f;
    }

    // ✅ Impact animation on click/completion
    private IEnumerator PlayImpactAnimation()
    {
        Vector3 originalScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < impactDuration)
        {
            float t = elapsedTime / impactDuration;
            float curveValue = impactCurve.Evaluate(t);
            float scaleMultiplier = 1f + (curveValue * (impactIntensity - 1f));
            transform.localScale = originalScale * scaleMultiplier;

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // ✅ Force completion (for cleanup)
    public void ForceComplete()
    {
        if (!isCompleted)
        {
            CompleteWithResult(TimingResult.Miss);
        }
    }
}
