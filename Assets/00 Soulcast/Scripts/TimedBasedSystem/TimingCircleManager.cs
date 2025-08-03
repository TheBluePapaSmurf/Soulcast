using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class TimingCircleManager : MonoBehaviour
{
    public static TimingCircleManager Instance;

    [Header("Prefab & Spawning")]
    public GameObject timingCirclePrefab;                    // ✅ Prefab met TimingCircle script
    public Transform spawnParent;                           // ✅ Canvas for spawning
    public Vector2 spawnAreaPadding = new Vector2(150f, 150f);

    [Header("Original UI (Single Hit)")]
    public GameObject originalTimingCirclePanel;
    public Image originalInnerCircle;

    [Header("Feedback UI")]
    public TextMeshProUGUI feedbackText;
    public CanvasGroup feedbackCanvasGroup;
    public TextMeshProUGUI hitCounterText;

    [Header("Sequential Settings")]
    public float timeBetweenSequentialHits = 0.5f;
    public bool showCounterForMultiHit = true;

    [Header("Original Timing Settings")]
    public float originalShrinkDuration = 2f;
    public float perfectZoneSize = 0.8f;
    public float perfectZoneTolerance = 0.05f;
    public Color normalCircleColor = Color.red;
    public Color perfectZoneColor = Color.green;

    [Header("Audio & Feedback")]
    public AudioClip perfectHitSound;
    public AudioClip missSound;
    public float textAnimationDuration = 0.8f;
    public Color perfectTextColor = Color.green;
    public Color failTextColor = Color.red;

    // ✅ Events
    public static event Action<List<TimingCircle.TimingResult>> OnMultiTimingComplete;

    // ✅ State
    private List<TimingCircle.TimingResult> allTimingResults = new List<TimingCircle.TimingResult>();
    private int currentHitIndex = 0;
    private int totalHits = 1;
    private bool isSequentialActive = false;

    // Original single timing
    private bool isSingleTimingActive = false;
    private System.Action<TimingCircle.TimingResult> currentSingleCompletionAction;

    // Current spawned circle
    private TimingCircle currentTimingCircle = null;

    private AudioSource audioSource;
    private RectTransform canvasRectTransform;
    private Vector3 originalPanelScale;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();

            if (spawnParent != null)
            {
                canvasRectTransform = spawnParent.GetComponent<RectTransform>();
            }

            if (originalTimingCirclePanel != null)
            {
                originalPanelScale = originalTimingCirclePanel.transform.localScale;
            }

            if (feedbackCanvasGroup != null)
            {
                feedbackCanvasGroup.alpha = 0f;
            }

            Debug.Log("TimingCircleManager Instance set successfully");
        }
        else
        {
            Debug.LogWarning("Multiple TimingCircleManager instances found - destroying duplicate");
            Destroy(gameObject);
        }
    }

    // ✅ Main entry point
    public void StartMultiHitTimingChallenge(int hitCount)
    {
        Debug.Log($"Starting timing challenge: {hitCount} hits");

        totalHits = hitCount;
        currentHitIndex = 0;
        allTimingResults.Clear();

        if (hitCount == 1)
        {
            StartSingleTimingChallenge();
        }
        else
        {
            StartSequentialMultiHitChallenge();
        }
    }

    // ✅ Single hit challenge (original system)
    public void StartSingleTimingChallenge()
    {
        if (originalTimingCirclePanel != null)
        {
            originalTimingCirclePanel.SetActive(true);
        }

        StartCoroutine(ExecuteSingleTimingChallenge());
    }

    // ✅ Sequential multi-hit challenge
    public void StartSequentialMultiHitChallenge()
    {
        if (timingCirclePrefab == null || spawnParent == null)
        {
            Debug.LogError("TimingCirclePrefab or SpawnParent not assigned!");
            return;
        }

        // Hide original panel
        if (originalTimingCirclePanel != null)
        {
            originalTimingCirclePanel.SetActive(false);
        }

        isSequentialActive = true;
        StartCoroutine(ExecuteSequentialTimingSequence());
    }

    // ✅ Execute sequential timing sequence
    private IEnumerator ExecuteSequentialTimingSequence()
    {
        for (currentHitIndex = 0; currentHitIndex < totalHits; currentHitIndex++)
        {
            Debug.Log($"Starting sequential timing {currentHitIndex + 1}/{totalHits}");

            // Update counter
            if (showCounterForMultiHit && hitCounterText != null)
            {
                hitCounterText.text = $"Hit {currentHitIndex + 1}/{totalHits}";
            }

            // Spawn timing circle at random position
            Vector2 randomPosition = GetRandomSpawnPosition();
            GameObject spawnedObject = Instantiate(timingCirclePrefab, spawnParent);

            RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = randomPosition;
            }

            // Get TimingCircle component
            currentTimingCircle = spawnedObject.GetComponent<TimingCircle>();
            if (currentTimingCircle == null)
            {
                Debug.LogError("TimingCircle component not found on spawned prefab!");
                Destroy(spawnedObject);
                allTimingResults.Add(TimingCircle.TimingResult.Miss);
                continue;
            }

            // Configure timing circle
            currentTimingCircle.Configure(
                shrinkDuration: originalShrinkDuration,
                perfectZoneSize: perfectZoneSize,
                perfectZoneTolerance: perfectZoneTolerance,
                normalColor: normalCircleColor,
                perfectColor: perfectZoneColor
            );

            // Setup completion callback
            bool timingComplete = false;
            TimingCircle.TimingResult result = TimingCircle.TimingResult.Miss;

            currentTimingCircle.OnTimingComplete += (TimingCircle.TimingResult completionResult) => {
                result = completionResult;
                timingComplete = true;
            };

            // Start timing challenge
            currentTimingCircle.StartTimingChallenge();

            // Wait for completion
            yield return new WaitUntil(() => timingComplete);

            // Store result
            allTimingResults.Add(result);

            // Play feedback
            yield return StartCoroutine(PlaySequentialFeedback(result));

            // Cleanup
            if (currentTimingCircle != null)
            {
                Destroy(currentTimingCircle.gameObject);
                currentTimingCircle = null;
            }

            // Pause between hits (except last)
            if (currentHitIndex < totalHits - 1)
            {
                yield return new WaitForSecondsRealtime(timeBetweenSequentialHits);
            }
        }

        isSequentialActive = false;

        // Show final results
        yield return StartCoroutine(ShowFinalResults());

        // Send results
        OnMultiTimingComplete?.Invoke(allTimingResults);
    }

    // ✅ Get random spawn position
    private Vector2 GetRandomSpawnPosition()
    {
        if (canvasRectTransform == null) return Vector2.zero;

        Vector2 canvasSize = canvasRectTransform.sizeDelta;

        float minX = -canvasSize.x * 0.5f + spawnAreaPadding.x;
        float maxX = canvasSize.x * 0.5f - spawnAreaPadding.x;
        float minY = -canvasSize.y * 0.5f + spawnAreaPadding.y;
        float maxY = canvasSize.y * 0.5f - spawnAreaPadding.y;

        return new Vector2(
            UnityEngine.Random.Range(minX, maxX),
            UnityEngine.Random.Range(minY, maxY)
        );
    }

    // ✅ Play feedback for sequential hits
    private IEnumerator PlaySequentialFeedback(TimingCircle.TimingResult result)
    {
        // Audio feedback
        PlayTimingAudio(result);

        // Text feedback
        if (feedbackText != null && feedbackCanvasGroup != null)
        {
            switch (result)
            {
                case TimingCircle.TimingResult.Perfect:
                    feedbackText.text = "PERFECT!";
                    feedbackText.color = perfectTextColor;
                    break;
                case TimingCircle.TimingResult.Miss:
                    feedbackText.text = "MISS!";
                    feedbackText.color = failTextColor;
                    break;
            }

            yield return StartCoroutine(ShowTextFeedback(textAnimationDuration * 0.6f));
        }
    }

    // ✅ Show final results
    private IEnumerator ShowFinalResults()
    {
        int perfectCount = 0;
        foreach (var result in allTimingResults)
        {
            if (result == TimingCircle.TimingResult.Perfect) perfectCount++;
        }

        if (feedbackText != null && feedbackCanvasGroup != null)
        {
            feedbackText.text = $"Final: {perfectCount}/{allTimingResults.Count} PERFECT!";
            feedbackText.color = perfectCount > allTimingResults.Count / 2 ? perfectTextColor : failTextColor;

            yield return StartCoroutine(ShowTextFeedback(textAnimationDuration));
        }
    }

    // ✅ Text feedback animation
    private IEnumerator ShowTextFeedback(float duration)
    {
        if (feedbackCanvasGroup == null) yield break;

        // Fade in
        float fadeInDuration = 0.15f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            float t = elapsedTime / fadeInDuration;
            feedbackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        feedbackCanvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(duration * 0.4f);

        // Fade out
        float fadeOutDuration = duration * 0.6f;
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

    // ✅ Audio feedback
    private void PlayTimingAudio(TimingCircle.TimingResult result)
    {
        if (audioSource != null)
        {
            switch (result)
            {
                case TimingCircle.TimingResult.Perfect:
                    if (perfectHitSound) audioSource.PlayOneShot(perfectHitSound);
                    break;
                case TimingCircle.TimingResult.Miss:
                    if (missSound) audioSource.PlayOneShot(missSound);
                    break;
            }
        }
    }

    // ✅ Original single timing challenge
    private IEnumerator ExecuteSingleTimingChallenge()
    {
        isSingleTimingActive = true;

        if (originalInnerCircle != null)
        {
            originalInnerCircle.color = normalCircleColor;
            originalInnerCircle.transform.localScale = Vector3.one;
        }

        if (feedbackCanvasGroup != null)
        {
            feedbackCanvasGroup.alpha = 0f;
        }

        TimingCircle.TimingResult result = TimingCircle.TimingResult.Miss;
        bool timingComplete = false;

        System.Action<TimingCircle.TimingResult> completeAction = (TimingCircle.TimingResult completionResult) => {
            result = completionResult;
            timingComplete = true;
            isSingleTimingActive = false;
        };

        currentSingleCompletionAction = completeAction;

        // Start shrink animation
        StartCoroutine(ShrinkOriginalCircle(() => {
            if (isSingleTimingActive)
            {
                completeAction(TimingCircle.TimingResult.Miss);
            }
        }));

        // Wait for completion
        yield return new WaitUntil(() => timingComplete);

        currentSingleCompletionAction = null;
        allTimingResults.Add(result);

        // Play feedback
        yield return StartCoroutine(PlaySequentialFeedback(result));

        // Send result
        OnMultiTimingComplete?.Invoke(allTimingResults);
    }

    // ✅ Original circle shrink animation
    private IEnumerator ShrinkOriginalCircle(System.Action onComplete = null)
    {
        if (originalInnerCircle == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        originalInnerCircle.transform.localScale = Vector3.one;

        while (elapsedTime < originalShrinkDuration && isSingleTimingActive)
        {
            float t = elapsedTime / originalShrinkDuration;
            float scale = Mathf.Lerp(1f, 0f, t);
            originalInnerCircle.transform.localScale = Vector3.one * scale;

            // Update color
            float perfectZoneMin = perfectZoneSize - perfectZoneTolerance;
            float perfectZoneMax = perfectZoneSize + perfectZoneTolerance;

            originalInnerCircle.color = (scale <= perfectZoneMax && scale >= perfectZoneMin) ?
                perfectZoneColor : normalCircleColor;

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        onComplete?.Invoke();
    }

    // ✅ Check original timing (for mouse input)
    void Update()
    {
        if (isSingleTimingActive && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckOriginalTiming();
        }
    }

    // ✅ CheckOriginalTiming method (should be around line 430-440)
    private void CheckOriginalTiming()
    {
        if (!isSingleTimingActive || currentSingleCompletionAction == null || originalInnerCircle == null) return;

        float currentScale = originalInnerCircle.transform.localScale.x;
        float perfectZoneMin = perfectZoneSize - perfectZoneTolerance;
        float perfectZoneMax = perfectZoneSize + perfectZoneTolerance;

        TimingCircle.TimingResult result = (currentScale <= perfectZoneMax && currentScale >= perfectZoneMin) ?
            TimingCircle.TimingResult.Perfect : TimingCircle.TimingResult.Miss;

        currentSingleCompletionAction(result);
    }


    // ✅ Public interface
    public void StartTimingChallenge()
    {
        StartMultiHitTimingChallenge(1);
    }

    public void HideTimingUI()
    {
        // Hide original
        if (originalTimingCirclePanel != null)
        {
            originalTimingCirclePanel.SetActive(false);
        }

        // Cleanup current spawned circle
        if (currentTimingCircle != null)
        {
            Destroy(currentTimingCircle.gameObject);
            currentTimingCircle = null;
        }

        allTimingResults.Clear();
        isSingleTimingActive = false;
        isSequentialActive = false;
        currentSingleCompletionAction = null;

        if (feedbackCanvasGroup != null)
        {
            feedbackCanvasGroup.alpha = 0f;
        }

        if (hitCounterText != null)
        {
            hitCounterText.text = "";
        }
    }

    // ✅ Debug methods
    [ContextMenu("Test Single Hit")]
    public void TestSingleHit()
    {
        StartMultiHitTimingChallenge(1);
    }

    [ContextMenu("Test Triple Hit")]
    public void TestTripleHit()
    {
        StartMultiHitTimingChallenge(3);
    }
}
