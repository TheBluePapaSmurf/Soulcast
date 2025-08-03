using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("The main camera to control")]
    public Camera controlledCamera;

    [Header("Positions")]
    [Tooltip("Default camera position and rotation")]
    public Transform defaultCameraTransform;
    [Tooltip("Camera position and rotation when focusing on altar")]
    public Transform altarCameraTransform;

    [Header("Animation")]
    public float transitionDuration = 1.5f;
    public Ease transitionEase = Ease.OutCubic;

    [Header("FOV Animation")]
    public bool animateFOV = true;
    public float altarFOV = 60f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float originalFOV;
    private bool isAtAltar = false;
    private Sequence cameraSequence;

    public static CameraController Instance { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Get camera reference if not assigned
        if (controlledCamera == null)
            controlledCamera = Camera.main;

        if (controlledCamera == null)
            controlledCamera = FindAnyObjectByType<Camera>();

        // Store original camera state
        if (controlledCamera != null)
        {
            originalPosition = controlledCamera.transform.position;
            originalRotation = controlledCamera.transform.rotation;
            originalFOV = controlledCamera.fieldOfView;
        }

        // Create default transform if not assigned
        if (defaultCameraTransform == null)
        {
            GameObject defaultTransform = new GameObject("DefaultCameraTransform");
            defaultTransform.transform.position = originalPosition;
            defaultTransform.transform.rotation = originalRotation;
            defaultCameraTransform = defaultTransform.transform;
        }
    }

    public void MoveToAltar(System.Action onComplete = null)
    {
        if (controlledCamera == null || altarCameraTransform == null || isAtAltar)
        {
            onComplete?.Invoke();
            return;
        }

        // Kill any ongoing camera animation
        if (cameraSequence != null)
        {
            cameraSequence.Kill();
        }

        isAtAltar = true;

        // Create camera animation sequence
        cameraSequence = DOTween.Sequence();

        // Animate position
        cameraSequence.Append(
            controlledCamera.transform.DOMove(altarCameraTransform.position, transitionDuration)
                .SetEase(transitionEase)
        );

        // Animate rotation (join with position)
        cameraSequence.Join(
            controlledCamera.transform.DORotateQuaternion(altarCameraTransform.rotation, transitionDuration)
                .SetEase(transitionEase)
        );

        // Animate FOV if enabled
        if (animateFOV)
        {
            cameraSequence.Join(
                controlledCamera.DOFieldOfView(altarFOV, transitionDuration)
                    .SetEase(transitionEase)
            );
        }

        // Call completion callback
        cameraSequence.OnComplete(() => {
            Debug.Log("Camera transition to altar completed");
            onComplete?.Invoke();
        });
    }

    public void ReturnToDefault(System.Action onComplete = null)
    {
        if (controlledCamera == null || defaultCameraTransform == null || !isAtAltar)
        {
            onComplete?.Invoke();
            return;
        }

        // Kill any ongoing camera animation
        if (cameraSequence != null)
        {
            cameraSequence.Kill();
        }

        isAtAltar = false;

        // Create camera animation sequence
        cameraSequence = DOTween.Sequence();

        // Animate position
        cameraSequence.Append(
            controlledCamera.transform.DOMove(defaultCameraTransform.position, transitionDuration)
                .SetEase(transitionEase)
        );

        // Animate rotation (join with position)
        cameraSequence.Join(
            controlledCamera.transform.DORotateQuaternion(defaultCameraTransform.rotation, transitionDuration)
                .SetEase(transitionEase)
        );

        // Animate FOV back to original
        if (animateFOV)
        {
            cameraSequence.Join(
                controlledCamera.DOFieldOfView(originalFOV, transitionDuration)
                    .SetEase(transitionEase)
            );
        }

        // Call completion callback
        cameraSequence.OnComplete(() => {
            Debug.Log("Camera transition to default completed");
            onComplete?.Invoke();
        });
    }

    public bool IsAtAltar()
    {
        return isAtAltar;
    }

    void OnDisable()
    {
        // Clean up animations
        if (cameraSequence != null)
        {
            cameraSequence.Kill();
        }
    }

    // Test methods
    [ContextMenu("Test Move To Altar")]
    public void TestMoveToAltar()
    {
        MoveToAltar(() => Debug.Log("Test: Moved to altar"));
    }

    [ContextMenu("Test Return To Default")]
    public void TestReturnToDefault()
    {
        ReturnToDefault(() => Debug.Log("Test: Returned to default"));
    }
}
