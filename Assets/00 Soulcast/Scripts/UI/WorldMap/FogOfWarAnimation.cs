using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System;

public class FogOfWarAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private AnimationType animationType = AnimationType.FadeWithSwirl;
    [SerializeField] private float animationDuration = 1.2f;
    [SerializeField] private Ease fadeEase = Ease.InOutQuad;

    [Header("Fog Components")]
    [SerializeField] private Image fogImage;
    [SerializeField] private CanvasGroup fogCanvasGroup;

    [Header("Swirl Effect Settings")]
    [SerializeField] private float swirlSpeed = 360f;
    [SerializeField] private int swirlRotations = 2;
    [SerializeField] private Vector3 swirlScale = new Vector3(1.2f, 1.2f, 1f);

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem fogParticles;
    [SerializeField] private bool useParticleEffect = true;

    [Header("Wave Effect")]
    [SerializeField] private float waveAmplitude = 50f;
    [SerializeField] private float waveFrequency = 2f;
    [SerializeField] private int waveCount = 3;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fogAppearSound;
    [SerializeField] private AudioClip fogDisappearSound;

    public enum AnimationType
    {
        SimpleFade,
        FadeWithSwirl,
        WaveEffect,
        ParticleCloud,
        CombinedEffect
    }

    private Sequence currentSequence;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private bool isAnimating = false;

    // Public properties voor external access
    public Vector3 OriginalScale => originalScale;
    public bool UseParticleEffect => useParticleEffect;
    public ParticleSystem FogParticles => fogParticles;
    public AudioClip FogAppearSound => fogAppearSound;
    public AudioClip FogDisappearSound => fogDisappearSound;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        // Auto-assign components als ze niet zijn ingesteld
        if (fogImage == null)
            fogImage = GetComponent<Image>();

        if (fogCanvasGroup == null)
        {
            fogCanvasGroup = GetComponent<CanvasGroup>();
            if (fogCanvasGroup == null)
                fogCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Setup initial state
        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    public void PlayFullTransitionAnimation(Action onComplete = null)
    {
        if (isAnimating) return;

        // Start de animatie via een MonoBehaviour die actief is
        var activeManager = FindFirstObjectByType<WorldMapManager>();
        if (activeManager != null)
        {
            activeManager.StartCoroutine(SafeFullTransitionCoroutine(onComplete));
        }
        else
        {
            // Fallback: activeer eerst dit GameObject
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);
            StartCoroutine(FullTransitionCoroutine(onComplete));
        }
    }

    private System.Collections.IEnumerator SafeFullTransitionCoroutine(Action onComplete = null)
    {
        // Activeer dit GameObject
        gameObject.SetActive(true);

        // Start de animatie
        yield return StartCoroutine(FullTransitionCoroutine(onComplete));
    }

    public void PlayFogAppearAnimation(Action onComplete = null)
    {
        if (isAnimating) return;

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        StartCoroutine(FogAppearCoroutine(onComplete));
    }

    public void PlayFogDisappearAnimation(Action onComplete = null)
    {
        if (isAnimating) return;

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        StartCoroutine(FogDisappearCoroutine(onComplete));
    }

    private IEnumerator FogAppearCoroutine(Action onComplete = null)
    {
        isAnimating = true;
        gameObject.SetActive(true);

        // Play sound
        PlaySound(fogAppearSound);

        // Start particle effect
        if (useParticleEffect && fogParticles != null)
            fogParticles.Play();

        // Reset to initial state
        SetAlpha(0f);
        rectTransform.localScale = originalScale;

        // Create animation sequence based on type
        currentSequence = CreateAppearSequence();

        yield return currentSequence.WaitForCompletion();

        onComplete?.Invoke();
        isAnimating = false;
    }

    private IEnumerator FogDisappearCoroutine(Action onComplete = null)
    {
        isAnimating = true;

        // Play sound
        PlaySound(fogDisappearSound);

        // Create disappear sequence
        currentSequence = CreateDisappearSequence();

        yield return currentSequence.WaitForCompletion();

        // Stop particle effect
        if (fogParticles != null)
            fogParticles.Stop();

        gameObject.SetActive(false);
        onComplete?.Invoke();
        isAnimating = false;
    }

    private IEnumerator FullTransitionCoroutine(Action onComplete = null)
    {
        yield return StartCoroutine(FogAppearCoroutine());
        yield return new WaitForSeconds(0.3f); // Pause at full opacity
        yield return StartCoroutine(FogDisappearCoroutine());

        onComplete?.Invoke();
    }

    public Sequence CreateAppearSequence()
    {
        Sequence sequence = DOTween.Sequence();

        switch (animationType)
        {
            case AnimationType.SimpleFade:
                sequence.Append(fogCanvasGroup.DOFade(1f, animationDuration).SetEase(fadeEase));
                break;

            case AnimationType.FadeWithSwirl:
                // Scale and rotate while fading in
                rectTransform.localScale = Vector3.zero;
                sequence.Append(rectTransform.DOScale(swirlScale, animationDuration * 0.6f).SetEase(Ease.OutBack));
                sequence.Join(rectTransform.DORotate(new Vector3(0, 0, swirlSpeed * swirlRotations), animationDuration * 0.8f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuart));
                sequence.Join(fogCanvasGroup.DOFade(1f, animationDuration * 0.4f).SetEase(fadeEase));
                sequence.Append(rectTransform.DOScale(originalScale, animationDuration * 0.4f).SetEase(Ease.InOutQuad));
                break;

            case AnimationType.WaveEffect:
                sequence.Append(CreateWaveEffect(true));
                break;

            case AnimationType.ParticleCloud:
                sequence.Append(fogCanvasGroup.DOFade(1f, animationDuration).SetEase(fadeEase));
                if (useParticleEffect && fogParticles != null)
                {
                    var emission = fogParticles.emission;
                    emission.rateOverTime = 50f;
                }
                break;

            case AnimationType.CombinedEffect:
                // Combine multiple effects
                rectTransform.localScale = Vector3.zero;
                sequence.Append(rectTransform.DOScale(originalScale, animationDuration * 0.5f).SetEase(Ease.OutBack));
                sequence.Join(fogCanvasGroup.DOFade(1f, animationDuration * 0.7f).SetEase(fadeEase));
                sequence.Join(CreateWaveEffect(true));
                break;
        }

        return sequence;
    }

    public Sequence CreateDisappearSequence()
    {
        Sequence sequence = DOTween.Sequence();

        switch (animationType)
        {
            case AnimationType.SimpleFade:
                sequence.Append(fogCanvasGroup.DOFade(0f, animationDuration * 0.8f).SetEase(fadeEase));
                break;

            case AnimationType.FadeWithSwirl:
                sequence.Append(rectTransform.DOScale(swirlScale, animationDuration * 0.3f).SetEase(Ease.InQuad));
                sequence.Join(rectTransform.DORotate(new Vector3(0, 0, -swirlSpeed), animationDuration * 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.InQuart));
                sequence.Join(fogCanvasGroup.DOFade(0f, animationDuration * 0.8f).SetEase(fadeEase));
                sequence.Append(rectTransform.DOScale(Vector3.zero, animationDuration * 0.2f).SetEase(Ease.InBack));
                break;

            case AnimationType.WaveEffect:
                sequence.Append(CreateWaveEffect(false));
                break;

            case AnimationType.ParticleCloud:
                sequence.Append(fogCanvasGroup.DOFade(0f, animationDuration * 0.8f).SetEase(fadeEase));
                break;

            case AnimationType.CombinedEffect:
                sequence.Append(CreateWaveEffect(false));
                sequence.Join(fogCanvasGroup.DOFade(0f, animationDuration * 0.8f).SetEase(fadeEase));
                sequence.Join(rectTransform.DOScale(Vector3.zero, animationDuration * 0.6f).SetEase(Ease.InBack));
                break;
        }

        return sequence;
    }

    private Sequence CreateWaveEffect(bool appearing)
    {
        Sequence waveSequence = DOTween.Sequence();

        for (int i = 0; i < waveCount; i++)
        {
            float delay = i * (animationDuration / waveCount) * 0.3f;
            float targetAlpha = appearing ? 1f : 0f;

            waveSequence.Insert(delay, fogCanvasGroup.DOFade(targetAlpha, animationDuration * 0.7f).SetEase(fadeEase));

            // Add scale wave effect
            Vector3 waveScale = originalScale + Vector3.one * (waveAmplitude / 100f);
            if (appearing)
            {
                waveSequence.Insert(delay, rectTransform.DOScale(waveScale, animationDuration * 0.3f).SetEase(Ease.OutElastic));
                waveSequence.Insert(delay + animationDuration * 0.3f, rectTransform.DOScale(originalScale, animationDuration * 0.4f).SetEase(Ease.InOutQuad));
            }
        }

        return waveSequence;
    }

    public void SetAlpha(float alpha)
    {
        if (fogCanvasGroup != null)
            fogCanvasGroup.alpha = alpha;
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void StopAnimation()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        if (fogParticles != null)
            fogParticles.Stop();

        isAnimating = false;
    }

    private void OnDestroy()
    {
        StopAnimation();
    }

    // Public methods voor directe controle
    public void SetAnimationType(AnimationType type)
    {
        animationType = type;
    }

    public void SetAnimationDuration(float duration)
    {
        animationDuration = duration;
    }

    public bool IsAnimating => isAnimating;

    // Editor testing methods
    [ContextMenu("Test Appear Animation")]
    private void TestAppearAnimation()
    {
        if (Application.isPlaying)
            PlayFogAppearAnimation();
    }

    [ContextMenu("Test Disappear Animation")]
    private void TestDisappearAnimation()
    {
        if (Application.isPlaying)
            PlayFogDisappearAnimation();
    }

    [ContextMenu("Test Full Transition")]
    private void TestFullTransition()
    {
        if (Application.isPlaying)
            PlayFullTransitionAnimation();
    }
}
