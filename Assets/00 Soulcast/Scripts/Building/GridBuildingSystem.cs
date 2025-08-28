using UnityEngine;
using UnityEngine.InputSystem;

public class GridBuildingSystem : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public ShopUIManager shopUIManager;
    public Camera playerCamera;

    [Header("Building Mode")]
    public LayerMask groundLayerMask = 1;
    public Material previewMaterial;
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    // Current building state
    private bool isBuildingMode = false;
    private BuildingData currentBuildingData;
    private GameObject previewObject;
    private Vector2Int lastGridPosition = Vector2Int.one * -1;

    // Input
    private bool leftClickPressed = false;
    private bool rightClickPressed = false;

    // Events
    public System.Action<BuildingData> OnBuildingModeStarted;
    public System.Action OnBuildingModeEnded;
    public System.Action<BuildingData, Vector2Int> OnBuildingPlaced;

    private ShopItem pendingPurchaseItem;

    public static GridBuildingSystem Instance { get; private set; }

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
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (shopUIManager == null)
            shopUIManager = FindFirstObjectByType<ShopUIManager>();
    }

    void Update()
    {
        HandleInput();

        if (isBuildingMode)
        {
            UpdateBuildingPreview();
            HandleBuildingPlacement();
        }
    }

    void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            leftClickPressed = mouse.leftButton.wasPressedThisFrame;
            rightClickPressed = mouse.rightButton.wasPressedThisFrame;
        }

        // Cancel building mode with right click or escape
        if (isBuildingMode && (rightClickPressed || Keyboard.current?.escapeKey.wasPressedThisFrame == true))
        {
            ExitBuildingMode();
        }
    }

    public bool StartBuildingModeWithPurchase(BuildingData buildingData, ShopItem shopItem, ShopUIManager shopManager)
    {
        if (buildingData == null || !buildingData.IsValidForGridPlacement())
        {
            Debug.LogError("Cannot start building mode: Invalid building data");
            return false;
        }

        // Bewaar purchase info voor later
        pendingPurchaseItem = shopItem;
        shopUIManager = shopManager;

        currentBuildingData = buildingData;
        isBuildingMode = true;

        CreatePreviewObject();

        OnBuildingModeStarted?.Invoke(buildingData);
        Debug.Log($"Started building mode for: {buildingData.buildingName} (Payment pending)");

        return true;
    }

    public void ExitBuildingMode()
    {
        // Als we building mode verlaten zonder te plaatsen, annuleer de purchase
        if (isBuildingMode && pendingPurchaseItem != null && shopUIManager != null)
        {
            shopUIManager.OnBuildingPlacementCancelled(pendingPurchaseItem);
        }

        isBuildingMode = false;
        currentBuildingData = null;
        pendingPurchaseItem = null;
        shopUIManager = null;

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        OnBuildingModeEnded?.Invoke();
        Debug.Log("Exited building mode");
    }

    void CreatePreviewObject()
    {
        if (currentBuildingData.buildingPrefab != null)
        {
            previewObject = Instantiate(currentBuildingData.buildingPrefab);

            // Make it a preview (disable colliders, make semi-transparent)
            MakeObjectPreview(previewObject);
        }
    }

    void MakeObjectPreview(GameObject obj)
    {
        // Disable all colliders
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Make semi-transparent
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = previewMaterial;
            }
            renderer.materials = materials;
        }

        // Add BuildableObject if not present
        BuildableObject buildable = obj.GetComponent<BuildableObject>();
        if (buildable == null)
        {
            buildable = obj.AddComponent<BuildableObject>();
        }

        buildable.SetBuildingData(currentBuildingData);

        // ✅ NIEUW: Apply auto scaling voor preview
        buildable.ApplyAutoScaling();
    }

    void UpdateBuildingPreview()
    {
        if (previewObject == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
        {
            Vector2Int gridPos = gridManager.WorldToGridPosition(hit.point);
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);

            previewObject.transform.position = worldPos;
            previewObject.SetActive(true);

            // Update preview material based on placement validity
            bool canPlace = CanPlaceBuilding(gridPos);
            UpdatePreviewMaterial(canPlace);

            lastGridPosition = gridPos;
        }
        else
        {
            previewObject.SetActive(false);
        }
    }

    void UpdatePreviewMaterial(bool canPlace)
    {
        if (previewObject == null) return;

        Material materialToUse = canPlace ? validPlacementMaterial : invalidPlacementMaterial;

        if (materialToUse != null)
        {
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = materialToUse;
                }
                renderer.materials = materials;
            }
        }
    }

    void HandleBuildingPlacement()
    {
        if (leftClickPressed && lastGridPosition != Vector2Int.one * -1)
        {
            if (CanPlaceBuilding(lastGridPosition))
            {
                PlaceBuilding(lastGridPosition);
            }
        }
    }

    bool CanPlaceBuilding(Vector2Int gridPosition)
    {
        if (currentBuildingData == null || gridManager == null)
            return false;

        Vector2Int buildingSize = currentBuildingData.GetGridSize();

        // Check if all cells are valid and unoccupied
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);
                if (!gridManager.IsValidGridPosition(cellPos))
                {
                    return false;
                }
            }
        }

        return true;
    }

    void PlaceBuilding(Vector2Int gridPosition)
    {
        if (!CanPlaceBuilding(gridPosition)) return;

        Vector3 worldPosition = gridManager.GridToWorldPosition(gridPosition);
        GameObject placedBuilding = Instantiate(currentBuildingData.buildingPrefab, worldPosition, Quaternion.identity);

        // Setup BuildableObject
        BuildableObject buildableComponent = placedBuilding.GetComponent<BuildableObject>();
        if (buildableComponent == null)
        {
            buildableComponent = placedBuilding.AddComponent<BuildableObject>();
        }

        buildableComponent.SetBuildingData(currentBuildingData);
        buildableComponent.SetGridPosition(gridPosition);
        buildableComponent.ApplyAutoScaling();

        // Mark grid cells as occupied
        Vector2Int buildingSize = currentBuildingData.GetGridSize();
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);
                gridManager.OccupyGridCell(cellPos, placedBuilding);
            }
        }

        // Play placement sound
        if (currentBuildingData.placementSound != null)
        {
            AudioSource.PlayClipAtPoint(currentBuildingData.placementSound, worldPosition);
        }

        // ✅ IMPORTANT: Proces de pending purchase NU pas
        if (pendingPurchaseItem != null && shopUIManager != null)
        {
            shopUIManager.OnBuildingSuccessfullyPlaced(pendingPurchaseItem);
        }

        OnBuildingPlaced?.Invoke(currentBuildingData, gridPosition);
        Debug.Log($"Placed {currentBuildingData.buildingName} at {gridPosition}");

        ExitBuildingMode();
    }


    public bool IsInBuildingMode()
    {
        return isBuildingMode;
    }

    public BuildingData GetCurrentBuildingData()
    {
        return currentBuildingData;
    }
}
