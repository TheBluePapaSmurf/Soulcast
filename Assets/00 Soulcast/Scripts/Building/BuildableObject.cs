using UnityEngine;

public class BuildableObject : MonoBehaviour
{
    [Header("Grid Properties")]
    public Vector2Int gridPosition;

    [Header("Building Data")]
    public BuildingData buildingData;

    [Header("Auto Scaling")]
    public bool enableAutoScaling = true;
    public Vector3 baseScale = Vector3.one; // Original prefab scale
    public float scaleMultiplier = 0.9f; // 90% van cell size voor padding

    [Header("Visual")]
    public bool showBounds = true;
    public Color boundsColor = Color.green;

    private bool isPlaced = false;
    private GridManager gridManager;
    private Vector3 originalScale;

    public bool IsPlaced => isPlaced;

    void Awake()
    {
        // Bewaar de originele scale
        originalScale = transform.localScale;

        // Vind GridManager
        gridManager = FindAnyObjectByType<GridManager>();
    }


    void Start()
    {
        // Apply auto scaling if enabled
        if (enableAutoScaling && gridManager != null)
        {
            ApplyAutoScaling();
        }
    }

    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
        isPlaced = true;

        // Update world position based on grid
        if (gridManager != null && buildingData != null)
        {
            Vector2Int gridSize = buildingData.GetGridSize(); // ✅ Get from BuildingData
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPosition);

            // Center the object in its grid area
            Vector3 centerOffset = new Vector3(
                (gridSize.x - 1) * gridManager.cellSize * 0.5f,
                0,
                (gridSize.y - 1) * gridManager.cellSize * 0.5f
            );
            transform.position = worldPos + centerOffset;
        }
    }


    public void SetBuildingData(BuildingData data)
    {
        buildingData = data;

        // Apply auto scaling when building data is set
        if (enableAutoScaling && gridManager != null)
        {
            ApplyAutoScaling();
        }
    }


    public void ApplyAutoScaling()
    {
        if (gridManager == null || !enableAutoScaling || buildingData == null) return;

        float cellSize = gridManager.cellSize;

        // ✅ Get gridSize directly from BuildingData
        Vector2Int gridSize = buildingData.GetGridSize();

        // Bereken de target size op basis van grid cells
        float targetWidth = gridSize.x * cellSize * scaleMultiplier;
        float targetDepth = gridSize.y * cellSize * scaleMultiplier;

        // Rest van de methode blijft hetzelfde...
        Bounds objectBounds = GetObjectBounds();

        if (objectBounds.size.x > 0 && objectBounds.size.z > 0)
        {
            float scaleX = targetWidth / objectBounds.size.x;
            float scaleZ = targetDepth / objectBounds.size.z;
            float uniformScale = Mathf.Min(scaleX, scaleZ);

            Vector3 newScale = baseScale * uniformScale;
            transform.localScale = newScale;

            Debug.Log($"Auto-scaled {gameObject.name} to {newScale} (Grid: {gridSize}, Cell Size: {cellSize})");
        }
    }


    /// <summary>
    /// Krijg de bounds van dit object
    /// </summary>
    Bounds GetObjectBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            // Fallback naar collider als er geen renderers zijn
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            // Ultimate fallback
            return new Bounds(transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    /// <summary>
    /// Reset scale naar origineel
    /// </summary>
    public void ResetToOriginalScale()
    {
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Handmatig scale instellen
    /// </summary>
    public void SetCustomScale(Vector3 scale)
    {
        enableAutoScaling = false;
        transform.localScale = scale;
    }

    /// <summary>
    /// Update auto scaling (bijv. als grid settings veranderen)
    /// </summary>
    public void UpdateAutoScaling()
    {
        if (enableAutoScaling)
        {
            ApplyAutoScaling();
        }
    }

    public Vector2Int[] GetOccupiedGridPositions()
    {
        if (buildingData == null) return new Vector2Int[0];

        // ✅ Get gridSize from BuildingData
        Vector2Int gridSize = buildingData.GetGridSize();

        Vector2Int[] positions = new Vector2Int[gridSize.x * gridSize.y];
        int index = 0;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                positions[index] = new Vector2Int(gridPosition.x + x, gridPosition.y + y);
                index++;
            }
        }

        return positions;
    }


    void OnDrawGizmos()
    {
        if (showBounds && buildingData != null)
        {
            Gizmos.color = boundsColor;

            // ✅ Get gridSize from BuildingData
            Vector2Int gridSize = buildingData.GetGridSize();

            if (gridManager != null && gridSize.x > 0 && gridSize.y > 0)
            {
                // Teken grid bounds
                Vector3 center = transform.position;
                Vector3 size = new Vector3(
                    gridSize.x * gridManager.cellSize,
                    0.1f,
                    gridSize.y * gridManager.cellSize
                );
                Gizmos.DrawWireCube(center, size);
            }

            else
            {
                // Fallback gizmo
                Vector3 center = transform.position;
                Vector3 size = new Vector3(gridSize.x, 0.1f, gridSize.y);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }

    // Debug functies
    [ContextMenu("Apply Auto Scaling")]
    void DebugApplyAutoScaling()
    {
        ApplyAutoScaling();
    }

    [ContextMenu("Reset to Original Scale")]
    void DebugResetScale()
    {
        ResetToOriginalScale();
    }
}
