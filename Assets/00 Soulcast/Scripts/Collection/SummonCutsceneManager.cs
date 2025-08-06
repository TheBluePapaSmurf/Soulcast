using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SummonCutsceneManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform altarTransform;
    [SerializeField] private Transform monsterSpawnPoint;
    [SerializeField] private Camera summonCamera;

    [Header("Camera Animation")]
    [SerializeField] private float cameraOrbitRadius = 5f;
    [SerializeField] private float cameraOrbitHeight = 2f;
    [SerializeField] private float cameraOrbitSpeed = 30f; // degrees per second
    [SerializeField] private float totalOrbitDuration = 3f;
    [SerializeField] private AnimationCurve cameraMovementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("🎬 Info Panel Camera Settings")]
    [SerializeField] private float infoPanelCameraDistance = 8f;
    [SerializeField] private float infoPanelCameraHeight = 2f;
    [SerializeField] private float infoPanelCameraOffset = 3f; // How far right to move camera
    [SerializeField] private float infoPanelLookOffset = -2f; // 🆕 NEW: How far left of monster to look at
    [SerializeField] private float infoPanelTransitionDuration = 1.5f;
    [SerializeField] private AnimationCurve infoPanelTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);


    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem chargeEffect;
    [SerializeField] private ParticleSystem summonCompleteEffect;
    [SerializeField] private ParticleSystem ambientAltarEffect;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip summonChargeSound;
    [SerializeField] private AudioClip summonCompleteSound;
    [SerializeField] private AudioClip monsterAppearSound;

    [Header("UI Panels")]
    [SerializeField] private GameObject summonInfoPanel;
    [SerializeField] private MonsterSummonInfoUI monsterInfoUI;
    [SerializeField] private CanvasGroup mainUICanvasGroup; // For fading out main UI

    [Header("Timing")]
    [SerializeField] private float chargeEffectDuration = 2f;
    [SerializeField] private float modelSpawnDelay = 0.5f;
    [SerializeField] private float infoPanelDelay = 1f;

    [Header("Monster Display")]
    [SerializeField] private float monsterScale = 1.5f;
    [SerializeField] private Vector3 monsterRotationOffset = Vector3.zero;
    [SerializeField] private bool faceCamera = true; // 🔧 CHANGED: Face camera instead of rotate
    [SerializeField] private bool smoothCameraFacing = true; // 🆕 NEW
    [SerializeField] private float cameraFacingSpeed = 2f; // 🆕 NEW

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool skipCameraAnimation = false;

    [Header("Component Management")]
    [SerializeField] private bool autoManageComponentStates = true;
    [SerializeField] private bool disableMainCameraDuringCutscene = true;
    public bool IsCutscenePlaying => isCutscenePlaying;


    // Private variables
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Camera originalMainCamera;
    private GameObject currentSpawnedMonster;
    private bool isCutscenePlaying = false;
    private System.Action onCutsceneComplete;

    // Original states for restoration
    private bool originalSummonCameraState;
    private bool originalMainCameraState;
    private bool[] originalParticleStates = new bool[3];

    public static SummonCutsceneManager Instance { get; private set; }

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
        InitializeCutsceneManager();
    }

    void InitializeCutsceneManager()
    {
        // Find altar if not assigned
        if (altarTransform == null)
        {
            GameObject altar = GameObject.FindGameObjectWithTag("Altar");
            if (altar == null)
            {
                altar = GameObject.Find("Altar");
            }
            if (altar != null)
            {
                altarTransform = altar.transform;
            }
        }

        // Set monster spawn point if not assigned
        if (monsterSpawnPoint == null && altarTransform != null)
        {
            monsterSpawnPoint = altarTransform;
        }

        // Find main camera if summon camera not assigned
        if (summonCamera == null)
        {
            summonCamera = Camera.main;
        }
        else
        {
            if (autoManageComponentStates)
            {
                originalSummonCameraState = summonCamera.gameObject.activeSelf;
                if (summonCamera.gameObject.activeSelf)
                {
                    summonCamera.gameObject.SetActive(false);
                }
            }
        }

        // Find and store main camera reference
        if (originalMainCamera == null)
        {
            originalMainCamera = Camera.main;
            if (originalMainCamera != null && autoManageComponentStates)
            {
                originalMainCameraState = originalMainCamera.gameObject.activeSelf;
            }
        }

        if (autoManageComponentStates)
        {
            StoreOriginalParticleStates();
            DisableAllParticleSystems();
        }

        // Store original camera state for summon camera
        if (summonCamera != null)
        {
            originalCameraPosition = summonCamera.transform.position;
            originalCameraRotation = summonCamera.transform.rotation;
        }

        // Initialize UI
        if (summonInfoPanel != null)
        {
            summonInfoPanel.SetActive(false);
        }

        if (debugMode)
        {
            Debug.Log($"🎬 SummonCutsceneManager initialized - Altar: {altarTransform != null}, Camera: {summonCamera != null}");
            Debug.Log($"🔧 Component management: {autoManageComponentStates}, Main camera found: {originalMainCamera != null}");
        }
    }

    void StoreOriginalParticleStates()
    {
        originalParticleStates[0] = chargeEffect != null ? chargeEffect.gameObject.activeSelf : false;
        originalParticleStates[1] = summonCompleteEffect != null ? summonCompleteEffect.gameObject.activeSelf : false;
        originalParticleStates[2] = ambientAltarEffect != null ? ambientAltarEffect.gameObject.activeSelf : false;
    }

    void DisableAllParticleSystems()
    {
        if (chargeEffect != null && chargeEffect.gameObject.activeSelf)
        {
            chargeEffect.gameObject.SetActive(false);
        }
        if (summonCompleteEffect != null && summonCompleteEffect.gameObject.activeSelf)
        {
            summonCompleteEffect.gameObject.SetActive(false);
        }
        if (ambientAltarEffect != null && ambientAltarEffect.gameObject.activeSelf)
        {
            ambientAltarEffect.gameObject.SetActive(false);
        }
    }

    public void StartSummonCutscene(GachaMonster summonedMonster, System.Action onComplete = null)
    {
        if (isCutscenePlaying)
        {
            Debug.LogWarning("⚠️ Cutscene already playing, ignoring new request");
            return;
        }

        if (summonedMonster?.monsterData == null)
        {
            Debug.LogError("❌ Cannot start cutscene - no monster data provided");
            onComplete?.Invoke();
            return;
        }

        onCutsceneComplete = onComplete;
        StartCoroutine(PlaySummonCutscene(summonedMonster));
    }

    private IEnumerator PlaySummonCutscene(GachaMonster summonedMonster)
    {
        isCutscenePlaying = true;

        if (debugMode) Debug.Log("🎬 Starting summon cutscene sequence");

        // Step 0: Setup camera and components
        yield return StartCoroutine(SetupCutsceneComponents());

        // Step 1: Fade out main UI
        yield return StartCoroutine(FadeMainUI(false, 0.5f));

        // Step 2: Start camera orbit animation
        Coroutine cameraAnimation = null;
        if (!skipCameraAnimation)
        {
            cameraAnimation = StartCoroutine(AnimateCameraOrbit());
        }

        // Step 3: Play charge effect and sound
        yield return StartCoroutine(PlayChargeEffect());

        // Step 4: Spawn monster model
        yield return StartCoroutine(SpawnMonsterModel(summonedMonster));

        // Step 5: Complete camera animation if still running
        if (cameraAnimation != null)
        {
            yield return cameraAnimation;
        }

        // Step 6: Play completion effects
        yield return StartCoroutine(PlayCompletionEffects());

        // 🆕 NEW: Step 7: Move camera for info panel display
        yield return StartCoroutine(TransitionCameraForInfoPanel());

        // Step 8: Show monster info panel
        yield return StartCoroutine(ShowMonsterInfoPanel(summonedMonster));

        // Cutscene is now waiting for user input via the confirm button
        if (debugMode) Debug.Log("🎬 Cutscene sequence complete, waiting for user confirmation");
    }

    private IEnumerator SetupCutsceneComponents()
    {
        if (!autoManageComponentStates) yield break;

        if (debugMode) Debug.Log("🔧 Setting up cutscene components...");

        // Disable main camera if required
        if (disableMainCameraDuringCutscene && originalMainCamera != null && originalMainCamera.gameObject.activeSelf)
        {
            originalMainCamera.gameObject.SetActive(false);
            if (debugMode) Debug.Log("📷 Main camera disabled");
        }

        // Enable summon camera
        if (summonCamera != null && !summonCamera.gameObject.activeSelf)
        {
            summonCamera.gameObject.SetActive(true);
            if (debugMode) Debug.Log("📷 Summon camera enabled");
        }

        // Enable particle systems
        if (chargeEffect != null && !chargeEffect.gameObject.activeSelf)
        {
            chargeEffect.gameObject.SetActive(true);
        }
        if (summonCompleteEffect != null && !summonCompleteEffect.gameObject.activeSelf)
        {
            summonCompleteEffect.gameObject.SetActive(true);
        }
        if (ambientAltarEffect != null && !ambientAltarEffect.gameObject.activeSelf)
        {
            ambientAltarEffect.gameObject.SetActive(true);
        }

        if (debugMode) Debug.Log("✨ Particle systems enabled");

        yield return null;
    }

    private IEnumerator FadeMainUI(bool fadeIn, float duration)
    {
        if (mainUICanvasGroup == null) yield break;

        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            mainUICanvasGroup.alpha = currentAlpha;
            yield return null;
        }

        mainUICanvasGroup.alpha = endAlpha;
        mainUICanvasGroup.interactable = fadeIn;
        mainUICanvasGroup.blocksRaycasts = fadeIn;
    }

    private IEnumerator AnimateCameraOrbit()
    {
        if (summonCamera == null || altarTransform == null)
        {
            if (debugMode) Debug.LogWarning("⚠️ Cannot animate camera - missing references");
            yield break;
        }

        if (debugMode) Debug.Log("📷 Starting camera orbit animation");

        Vector3 altarPosition = altarTransform.position;
        float elapsedTime = 0f;

        Vector3 startPosition = summonCamera.transform.position;
        Quaternion startRotation = summonCamera.transform.rotation;

        while (elapsedTime < totalOrbitDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / totalOrbitDuration;
            float curveValue = cameraMovementCurve.Evaluate(normalizedTime);

            // Calculate orbit position
            float angle = curveValue * 360f * (cameraOrbitSpeed / 360f);
            float x = altarPosition.x + Mathf.Cos(angle * Mathf.Deg2Rad) * cameraOrbitRadius;
            float z = altarPosition.z + Mathf.Sin(angle * Mathf.Deg2Rad) * cameraOrbitRadius;
            float y = altarPosition.y + cameraOrbitHeight;

            Vector3 newPosition = new Vector3(x, y, z);
            summonCamera.transform.position = newPosition;

            // Look at altar
            Vector3 lookDirection = (altarPosition - newPosition).normalized;
            summonCamera.transform.rotation = Quaternion.LookRotation(lookDirection);

            yield return null;
        }

        if (debugMode) Debug.Log("📷 Camera orbit animation complete");
    }

    private IEnumerator PlayChargeEffect()
    {
        if (debugMode) Debug.Log("✨ Starting charge effects");

        // Play charge sound
        if (audioSource != null && summonChargeSound != null)
        {
            audioSource.PlayOneShot(summonChargeSound);
        }

        // Start charge particle effect
        if (chargeEffect != null)
        {
            chargeEffect.transform.position = monsterSpawnPoint.position;
            chargeEffect.Play();
            if (debugMode) Debug.Log("✨ Charge particle effect started");
        }

        // Start ambient altar effect
        if (ambientAltarEffect != null)
        {
            ambientAltarEffect.transform.position = altarTransform.position;
            ambientAltarEffect.Play();
            if (debugMode) Debug.Log("✨ Ambient altar effect started");
        }

        yield return new WaitForSeconds(chargeEffectDuration);

        // Stop charge effect
        if (chargeEffect != null)
        {
            chargeEffect.Stop();
            if (debugMode) Debug.Log("✨ Charge effect stopped");
        }
    }

    // 🔧 UPDATED: Spawn monster with camera facing instead of rotation
    private IEnumerator SpawnMonsterModel(GachaMonster summonedMonster)
    {
        yield return new WaitForSeconds(modelSpawnDelay);

        // Clean up previous monster
        if (currentSpawnedMonster != null)
        {
            DestroyImmediate(currentSpawnedMonster);
        }

        // Spawn new monster model
        if (summonedMonster.monsterData.modelPrefab != null)
        {
            Vector3 spawnPosition = monsterSpawnPoint.position;
            Quaternion spawnRotation = monsterSpawnPoint.rotation * Quaternion.Euler(monsterRotationOffset);

            currentSpawnedMonster = Instantiate(summonedMonster.monsterData.modelPrefab, spawnPosition, spawnRotation);
            currentSpawnedMonster.transform.localScale = Vector3.one * monsterScale;

            // 🔧 CHANGED: Add camera facing component instead of rotator
            if (faceCamera && summonCamera != null)
            {
                var cameraFacer = currentSpawnedMonster.AddComponent<MonsterCameraFacer>();
                cameraFacer.targetCamera = summonCamera;
                cameraFacer.smooth = smoothCameraFacing;
                cameraFacer.rotationSpeed = cameraFacingSpeed;

                if (debugMode) Debug.Log("👁️ Added camera facing component to monster");
            }

            // Play monster appear sound
            if (audioSource != null && monsterAppearSound != null)
            {
                audioSource.PlayOneShot(monsterAppearSound);
            }

            if (debugMode) Debug.Log($"🐉 Spawned monster model: {summonedMonster.monsterData.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No model prefab assigned for monster: {summonedMonster.monsterData.name}");
        }
    }

    private IEnumerator PlayCompletionEffects()
    {
        if (debugMode) Debug.Log("🎉 Playing completion effects");

        // Play completion sound
        if (audioSource != null && summonCompleteSound != null)
        {
            audioSource.PlayOneShot(summonCompleteSound);
        }

        // Play completion particle effect
        if (summonCompleteEffect != null)
        {
            summonCompleteEffect.transform.position = monsterSpawnPoint.position;
            summonCompleteEffect.Play();
            if (debugMode) Debug.Log("✨ Completion particle effect started");
        }

        yield return new WaitForSeconds(0.5f);
    }

    // 🎬 ENHANCED: Transition camera for info panel display with configurable look target
    private IEnumerator TransitionCameraForInfoPanel()
    {
        if (summonCamera == null || altarTransform == null) yield break;

        if (debugMode) Debug.Log("📷 Transitioning camera for info panel display");

        Vector3 altarPosition = altarTransform.position;
        Vector3 startPosition = summonCamera.transform.position;
        Quaternion startRotation = summonCamera.transform.rotation;

        // Calculate target position: further to the right, back, and higher
        Vector3 targetPosition = altarPosition + new Vector3(infoPanelCameraOffset, infoPanelCameraHeight, -infoPanelCameraDistance);

        // 🆕 NEW: Configurable look target - look to the left of the monster
        Vector3 lookTarget = altarPosition + new Vector3(infoPanelLookOffset, 0f, 0f);
        Vector3 lookDirection = (lookTarget - targetPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        if (debugMode)
        {
            Debug.Log($"📷 Camera target position: {targetPosition}");
            Debug.Log($"📷 Camera look target: {lookTarget}");
            Debug.Log($"📷 Monster position: {altarPosition}");
            Debug.Log($"📷 Camera offset right: {infoPanelCameraOffset}, Look offset left: {infoPanelLookOffset}");
        }

        float elapsedTime = 0f;

        while (elapsedTime < infoPanelTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / infoPanelTransitionDuration;
            float curveValue = infoPanelTransitionCurve.Evaluate(normalizedTime);

            // Smoothly transition to new position and rotation
            summonCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            summonCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);

            yield return null;
        }

        // Ensure final position and rotation
        summonCamera.transform.position = targetPosition;
        summonCamera.transform.rotation = targetRotation;

        if (debugMode) Debug.Log("📷 Camera transition for info panel complete - monster should be on left side");
    }


    private IEnumerator ShowMonsterInfoPanel(GachaMonster summonedMonster)
    {
        yield return new WaitForSeconds(infoPanelDelay);

        if (summonInfoPanel != null)
        {
            summonInfoPanel.SetActive(true);

            // Setup monster info UI
            if (monsterInfoUI != null)
            {
                monsterInfoUI.DisplayMonsterInfo(summonedMonster);
            }

            if (debugMode) Debug.Log("📋 Monster info panel displayed");
        }
    }

    public void OnConfirmMonsterInfo()
    {
        StartCoroutine(EndCutscene());
    }

    private IEnumerator EndCutscene()
    {
        if (debugMode) Debug.Log("🎬 Ending summon cutscene");

        // Hide monster info panel
        if (summonInfoPanel != null)
        {
            summonInfoPanel.SetActive(false);
        }

        // Stop all particle effects
        StopAllParticleEffects();

        // Clean up spawned monster
        if (currentSpawnedMonster != null)
        {
            Destroy(currentSpawnedMonster);
            currentSpawnedMonster = null;
        }

        // Return camera to original position
        yield return StartCoroutine(ReturnCameraToOriginal());

        // Restore component states
        yield return StartCoroutine(RestoreComponentStates());

        // Fade main UI back in
        yield return StartCoroutine(FadeMainUI(true, 0.5f));

        isCutscenePlaying = false;

        // Invoke completion callback
        onCutsceneComplete?.Invoke();
        onCutsceneComplete = null;

        if (debugMode) Debug.Log("✅ Summon cutscene ended");
    }

    private IEnumerator ReturnCameraToOriginal()
    {
        if (summonCamera == null) yield break;

        Vector3 startPosition = summonCamera.transform.position;
        Quaternion startRotation = summonCamera.transform.rotation;
        float duration = 1f;
        float elapsedTime = 0f;

        if (debugMode) Debug.Log("📷 Returning camera to original position");

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;
            float curveValue = cameraMovementCurve.Evaluate(normalizedTime);

            summonCamera.transform.position = Vector3.Lerp(startPosition, originalCameraPosition, curveValue);
            summonCamera.transform.rotation = Quaternion.Lerp(startRotation, originalCameraRotation, curveValue);

            yield return null;
        }

        summonCamera.transform.position = originalCameraPosition;
        summonCamera.transform.rotation = originalCameraRotation;
    }

    private IEnumerator RestoreComponentStates()
    {
        if (!autoManageComponentStates) yield break;

        if (debugMode) Debug.Log("🔧 Restoring original component states");

        // Restore summon camera state
        if (summonCamera != null)
        {
            summonCamera.gameObject.SetActive(originalSummonCameraState);
            if (debugMode) Debug.Log($"📷 Summon camera restored to: {originalSummonCameraState}");
        }

        // Restore main camera state
        if (originalMainCamera != null && disableMainCameraDuringCutscene)
        {
            originalMainCamera.gameObject.SetActive(originalMainCameraState);
            if (debugMode) Debug.Log($"📷 Main camera restored to: {originalMainCameraState}");
        }

        // Restore particle system states
        if (chargeEffect != null)
        {
            chargeEffect.gameObject.SetActive(originalParticleStates[0]);
        }
        if (summonCompleteEffect != null)
        {
            summonCompleteEffect.gameObject.SetActive(originalParticleStates[1]);
        }
        if (ambientAltarEffect != null)
        {
            ambientAltarEffect.gameObject.SetActive(originalParticleStates[2]);
        }

        if (debugMode) Debug.Log("✨ Particle systems restored to original states");

        yield return null;
    }

    private void StopAllParticleEffects()
    {
        if (chargeEffect != null) chargeEffect.Stop();
        if (summonCompleteEffect != null) summonCompleteEffect.Stop();
        if (ambientAltarEffect != null) ambientAltarEffect.Stop();
    }

    public void ForceEndCutscene()
    {
        if (isCutscenePlaying)
        {
            StopAllCoroutines();
            StartCoroutine(EndCutscene());
        }
    }

    [ContextMenu("Test Cutscene")]
    public void TestCutscene()
    {
        var testMonster = new GachaMonster();
        StartSummonCutscene(testMonster);
    }

    [ContextMenu("Check Component States")]
    public void CheckComponentStates()
    {
        Debug.Log("🔧 Current Component States:");
        Debug.Log($"📷 Summon Camera Active: {summonCamera?.gameObject.activeSelf}");
        Debug.Log($"📷 Main Camera Active: {originalMainCamera?.gameObject.activeSelf}");
        Debug.Log($"✨ Charge Effect Active: {chargeEffect?.gameObject.activeSelf}");
        Debug.Log($"✨ Complete Effect Active: {summonCompleteEffect?.gameObject.activeSelf}");
        Debug.Log($"✨ Ambient Effect Active: {ambientAltarEffect?.gameObject.activeSelf}");
    }

    [ContextMenu("Reset Component States")]
    public void ResetComponentStates()
    {
        if (autoManageComponentStates)
        {
            StartCoroutine(RestoreComponentStates());
            Debug.Log("🔄 Component states reset to original");
        }
    }
}

// 🆕 NEW: Camera facing component (replaces MonsterDisplayRotator)
public class MonsterCameraFacer : MonoBehaviour
{
    [Header("Camera Facing Settings")]
    public Camera targetCamera;
    public bool smooth = true;
    public float rotationSpeed = 2f;
    public bool onlyYAxis = true; // Only rotate on Y axis to prevent tilting

    void Update()
    {
        if (targetCamera == null) return;

        // Calculate direction to camera
        Vector3 directionToCamera = targetCamera.transform.position - transform.position;

        // If only Y axis, zero out vertical component
        if (onlyYAxis)
        {
            directionToCamera.y = 0;
        }

        if (directionToCamera.magnitude > 0.01f)
        {
            // Calculate target rotation
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);

            // Apply rotation (smooth or instant)
            if (smooth)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }
    }
}
