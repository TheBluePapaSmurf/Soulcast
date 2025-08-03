using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MonsterDisplay3D : MonoBehaviour
{
    [Header("3D Display Setup")]
    public Camera display3DCamera;
    public Transform monsterSpawnPoint;
    public RenderTexture renderTexture;

    [Header("Display Settings")]
    public Vector3 monsterScale = Vector3.one;
    public float rotationSpeed = 30f;
    public bool autoRotate = false;

    [Header("Manual Rotation Controls")]
    public bool allowManualRotation = true;
    public float mouseSensitivity = 2f;
    public float touchSensitivity = 1f;
    public float rotationSmoothing = 5f;
    public bool invertY = false;

    [Header("Rotation Limits")]
    public bool limitVerticalRotation = true;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 30f;

    [Header("Lighting")]
    public Light displayLight;

    [Header("Input Detection Area")]
    public RectTransform inputDetectionArea;

    private GameObject currentMonster3D;
    private MonsterData currentMonsterData;

    // Rotation control variables
    private Vector2 currentRotation;
    private Vector2 targetRotation;
    private bool isDragging = false;
    private Vector2 lastInputPosition;

    // Input System variables
    private InputAction pointerPositionAction;
    private InputAction pointerPressAction;
    private InputAction pointerDeltaAction;

    void Start()
    {
        SetupRenderTexture();
        SetupCamera();
        SetupInputDetection();
        SetupInputActions();
    }

    void SetupInputActions()
    {
        // Create input actions for the new Input System
        pointerPositionAction = new InputAction("PointerPosition", InputActionType.PassThrough, "<Pointer>/position");
        pointerPressAction = new InputAction("PointerPress", InputActionType.Button, "<Pointer>/press");
        pointerDeltaAction = new InputAction("PointerDelta", InputActionType.PassThrough, "<Pointer>/delta");

        // Enable the actions
        pointerPositionAction.Enable();
        pointerPressAction.Enable();
        pointerDeltaAction.Enable();

        // Subscribe to input events
        pointerPressAction.started += OnPointerPressed;
        pointerPressAction.canceled += OnPointerReleased;
    }

    void SetupRenderTexture()
    {
        if (renderTexture != null && display3DCamera != null)
        {
            display3DCamera.targetTexture = renderTexture;
        }
        else if (display3DCamera != null)
        {
            renderTexture = new RenderTexture(512, 512, 16);
            renderTexture.name = "Monster3DDisplay_RT";
            renderTexture.Create();
            display3DCamera.targetTexture = renderTexture;
        }
    }

    void SetupCamera()
    {
        if (display3DCamera != null)
        {
            display3DCamera.clearFlags = CameraClearFlags.SolidColor;
            display3DCamera.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0f);
            display3DCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
        }
    }

    void SetupInputDetection()
    {
        if (inputDetectionArea == null)
        {
            var rawImage = FindAnyObjectByType<UnityEngine.UI.RawImage>();
            if (rawImage != null && rawImage.texture == renderTexture)
            {
                inputDetectionArea = rawImage.rectTransform;
                Debug.Log("Auto-assigned input detection area to Raw Image");
            }
        }
    }

    void Update()
    {
        HandleInput();
        UpdateRotation();
    }

    void HandleInput()
    {
        if (!allowManualRotation || currentMonster3D == null) return;

        if (isDragging)
        {
            HandleDragInput();
        }
    }

    void OnPointerPressed(InputAction.CallbackContext context)
    {
        if (!allowManualRotation || currentMonster3D == null) return;

        Vector2 pointerPosition = pointerPositionAction.ReadValue<Vector2>();

        // Check if pointer is over the input detection area
        if (inputDetectionArea != null && !IsPointerOverDetectionArea(pointerPosition))
            return;

        StartDragging(pointerPosition);
    }

    void OnPointerReleased(InputAction.CallbackContext context)
    {
        StopDragging();
    }

    void HandleDragInput()
    {
        if (!isDragging) return;

        Vector2 currentPointerPosition = pointerPositionAction.ReadValue<Vector2>();
        Vector2 delta = pointerDeltaAction.ReadValue<Vector2>();

        // Determine sensitivity based on input device
        float sensitivity = GetCurrentInputSensitivity();

        UpdateDragRotation(delta, sensitivity);
    }

    float GetCurrentInputSensitivity()
    {
        // Check if current input is from touch
        var currentPointer = Pointer.current;
        if (currentPointer is Touchscreen)
        {
            return touchSensitivity;
        }
        else
        {
            return mouseSensitivity;
        }
    }

    bool IsPointerOverDetectionArea(Vector2 screenPosition)
    {
        if (inputDetectionArea == null) return true;

        return RectTransformUtility.RectangleContainsScreenPoint(
            inputDetectionArea,
            screenPosition,
            null
        );
    }

    void StartDragging(Vector2 inputPosition)
    {
        isDragging = true;
        lastInputPosition = inputPosition;
        autoRotate = false;
    }

    void StopDragging()
    {
        isDragging = false;
    }

    void UpdateDragRotation(Vector2 delta, float sensitivity)
    {
        if (!isDragging) return;

        // Convert delta to rotation delta
        float deltaX = delta.x * sensitivity * 0.1f;
        float deltaY = delta.y * sensitivity * 0.1f;

        if (invertY) deltaY = -deltaY;

        // Update target rotation
        targetRotation.x += deltaX; // Horizontal rotation (Y-axis)
        targetRotation.y += deltaY; // Vertical rotation (X-axis)

        // Apply vertical rotation limits
        if (limitVerticalRotation)
        {
            targetRotation.y = Mathf.Clamp(targetRotation.y, minVerticalAngle, maxVerticalAngle);
        }
    }

    void UpdateRotation()
    {
        if (currentMonster3D == null) return;

        // Handle auto-rotation
        if (autoRotate && !isDragging)
        {
            targetRotation.x += rotationSpeed * Time.deltaTime;
        }

        // Smooth rotation interpolation
        currentRotation = Vector2.Lerp(currentRotation, targetRotation, rotationSmoothing * Time.deltaTime);

        // Apply rotation to monster
        currentMonster3D.transform.rotation = Quaternion.Euler(-currentRotation.y, currentRotation.x, 0);
    }

    public void DisplayMonster(MonsterData monsterData)
    {
        if (monsterData == null) return;

        currentMonsterData = monsterData;
        ClearCurrentMonster();
        CreateMonsterDisplay(monsterData);

        ResetRotation();
    }

    public void ResetRotation()
    {
        currentRotation = Vector2.zero;
        targetRotation = Vector2.zero;

        if (currentMonster3D != null)
        {
            currentMonster3D.transform.rotation = Quaternion.identity;
        }
    }

    public void SetAutoRotation(bool enabled)
    {
        autoRotate = enabled;
    }

    public void ToggleAutoRotation()
    {
        autoRotate = !autoRotate;
    }

    void ClearCurrentMonster()
    {
        if (currentMonster3D != null)
        {
            Destroy(currentMonster3D);
            currentMonster3D = null;
        }
    }

    void CreateMonsterDisplay(MonsterData monsterData)
    {
        GameObject monsterPrefab = GetMonster3DPrefab(monsterData);

        if (monsterPrefab != null)
        {
            currentMonster3D = Instantiate(monsterPrefab, monsterSpawnPoint.position, monsterSpawnPoint.rotation);
            currentMonster3D.transform.SetParent(monsterSpawnPoint);
            currentMonster3D.transform.localPosition = Vector3.zero;
            currentMonster3D.transform.localScale = monsterScale;
            currentMonster3D.transform.localRotation = Quaternion.identity;

            DisableCombatComponents(currentMonster3D);

            Debug.Log($"Successfully loaded 3D model for {monsterData.monsterName}");
        }
        else
        {
            CreateFallbackDisplay(monsterData);
        }
    }

    GameObject GetMonster3DPrefab(MonsterData monsterData)
    {
        if (monsterData.modelPrefab != null)
        {
            Debug.Log($"Found model prefab for {monsterData.monsterName}: {monsterData.modelPrefab.name}");
            return monsterData.modelPrefab;
        }

        Debug.LogWarning($"No modelPrefab assigned for {monsterData.monsterName}, trying Resources fallback");
        return FindMonster3DPrefabFromResources(monsterData);
    }

    GameObject FindMonster3DPrefabFromResources(MonsterData monsterData)
    {
        string[] possiblePaths = {
            $"Monsters/{monsterData.monsterName}/3D/{monsterData.monsterName}",
            $"3D Models/{monsterData.monsterName}",
            $"Monsters/{monsterData.name}/3D/{monsterData.name}",
            $"3D/{monsterData.monsterName}",
            $"Prefabs/{monsterData.monsterName}"
        };

        foreach (string path in possiblePaths)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"Found model at Resources path: {path}");
                return prefab;
            }
        }

        Debug.LogWarning($"No 3D model found for {monsterData.monsterName} in Resources folders");
        return null;
    }

    void CreateFallbackDisplay(MonsterData monsterData)
    {
        currentMonster3D = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentMonster3D.name = $"Fallback_{monsterData.monsterName}";

        currentMonster3D.transform.SetParent(monsterSpawnPoint);
        currentMonster3D.transform.localPosition = Vector3.zero;
        currentMonster3D.transform.localScale = monsterScale;
        currentMonster3D.transform.localRotation = Quaternion.identity;

        Renderer renderer = currentMonster3D.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material fallbackMaterial = new Material(Shader.Find("Standard"));
            fallbackMaterial.color = GetElementColor(monsterData.element);
            renderer.material = fallbackMaterial;
        }

        Debug.Log($"Using fallback display for {monsterData.monsterName} - no 3D model found");
    }

    Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return Color.red;
            case ElementType.Water: return Color.blue;
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f);
            default: return Color.gray;
        }
    }

    void DisableCombatComponents(GameObject monster)
    {
        MonoBehaviour[] components = monster.GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component is Monster || component.GetType().Name.Contains("Combat"))
            {
                component.enabled = false;
            }
        }

        Collider[] colliders = monster.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }

    public RenderTexture GetRenderTexture()
    {
        return renderTexture;
    }

    void OnDestroy()
    {
        // Clean up input actions
        if (pointerPositionAction != null)
        {
            pointerPositionAction.Disable();
            pointerPositionAction.Dispose();
        }

        if (pointerPressAction != null)
        {
            pointerPressAction.started -= OnPointerPressed;
            pointerPressAction.canceled -= OnPointerReleased;
            pointerPressAction.Disable();
            pointerPressAction.Dispose();
        }

        if (pointerDeltaAction != null)
        {
            pointerDeltaAction.Disable();
            pointerDeltaAction.Dispose();
        }

        ClearCurrentMonster();

        if (renderTexture != null && renderTexture.name == "Monster3DDisplay_RT")
        {
            renderTexture.Release();
        }
    }
}
