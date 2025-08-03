using UnityEngine;
using System.Collections;

public class DynamicCombatCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera combatCamera;
    public Transform cameraTarget;

    [Header("Movement Settings")]
    public float transitionSpeed = 2f;
    public bool smoothDamping = true;
    public float dampingTime = 0.3f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // State
    private Transform currentTargetTransform;
    private Vector3 velocity = Vector3.zero;
    private Vector3 angularVelocity = Vector3.zero;

    public static DynamicCombatCamera Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (combatCamera == null)
                combatCamera = Camera.main;

            if (cameraTarget == null)
            {
                GameObject targetObj = new GameObject("CameraTarget");
                cameraTarget = targetObj.transform;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Set to overview mode initially
        SetOverviewMode();
    }

    void Update()
    {
        UpdateCameraPosition();
    }

    // ✅ SIMPLE: Focus on monster using camera targets
    public void FocusOnMonster(Monster monster)
    {
        if (monster == null || CameraTargetManager.Instance == null) return;

        Transform target = CameraTargetManager.Instance.GetCameraTargetForMonster(monster);
        if (target != null)
        {
            SetCameraTarget(target);
            Debug.Log($"Focusing camera on {monster.monsterData.monsterName}");
        }
    }

    // ✅ SIMPLE: Focus on target using camera targets
    public void FocusOnTarget(Monster target)
    {
        if (target == null || CameraTargetManager.Instance == null) return;

        Transform cameraTargetTransform = CameraTargetManager.Instance.GetCameraTargetForMonster(target);
        if (cameraTargetTransform != null)
        {
            SetCameraTarget(cameraTargetTransform);
            Debug.Log($"Focusing camera on target: {target.monsterData.monsterName}");
        }
    }

    // ✅ SIMPLE: Set overview mode
    public void SetOverviewMode()
    {
        if (CameraTargetManager.Instance != null)
        {
            Transform overviewTarget = CameraTargetManager.Instance.GetOverviewTarget();
            if (overviewTarget != null)
            {
                SetCameraTarget(overviewTarget);
                Debug.Log("Camera set to overview mode");
            }
        }
    }

    // ✅ SIMPLE: Set camera to follow specific target transform
    private void SetCameraTarget(Transform target)
    {
        currentTargetTransform = target;
    }

    // ✅ SIMPLE: Update camera position to follow target
    private void UpdateCameraPosition()
    {
        if (currentTargetTransform == null) return;

        if (smoothDamping)
        {
            // Smooth movement
            cameraTarget.position = Vector3.SmoothDamp(
                cameraTarget.position,
                currentTargetTransform.position,
                ref velocity,
                dampingTime
            );

            // Smooth rotation
            cameraTarget.rotation = Quaternion.Slerp(
                cameraTarget.rotation,
                currentTargetTransform.rotation,
                transitionSpeed * Time.deltaTime
            );
        }
        else
        {
            // Linear movement
            cameraTarget.position = Vector3.Lerp(
                cameraTarget.position,
                currentTargetTransform.position,
                transitionSpeed * Time.deltaTime
            );

            cameraTarget.rotation = Quaternion.Lerp(
                cameraTarget.rotation,
                currentTargetTransform.rotation,
                transitionSpeed * Time.deltaTime
            );
        }

        // Apply to actual camera
        combatCamera.transform.position = cameraTarget.position;
        combatCamera.transform.rotation = cameraTarget.rotation;
    }

    // ✅ Smooth transition with custom duration
    public void TransitionToTarget(Transform target, float duration = 1f)
    {
        if (target != null)
        {
            StartCoroutine(SmoothTransitionToTarget(target, duration));
        }
    }

    private IEnumerator SmoothTransitionToTarget(Transform target, float duration)
    {
        Vector3 startPos = cameraTarget.position;
        Quaternion startRot = cameraTarget.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curveValue = transitionCurve.Evaluate(t);

            cameraTarget.position = Vector3.Lerp(startPos, target.position, curveValue);
            cameraTarget.rotation = Quaternion.Lerp(startRot, target.rotation, curveValue);

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetCameraTarget(target);
    }


    // ✅ Debug methods
    [ContextMenu("Test Focus First Player")]
    public void TestFocusFirstPlayer()
    {
        if (CombatManager.Instance != null && CombatManager.Instance.playerMonsters.Count > 0)
        {
            FocusOnMonster(CombatManager.Instance.playerMonsters[0]);
        }
    }

    [ContextMenu("Test Focus First Enemy")]
    public void TestFocusFirstEnemy()
    {
        if (CombatManager.Instance != null && CombatManager.Instance.enemyMonsters.Count > 0)
        {
            FocusOnTarget(CombatManager.Instance.enemyMonsters[0]);
        }
    }
}
