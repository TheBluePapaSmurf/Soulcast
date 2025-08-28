using UnityEngine;

public class BuildableObject : MonoBehaviour
{
    [Header("Grid Properties")]
    public Vector2Int gridSize = Vector2Int.one;
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

        // Als we BuildingData hebben, gebruik dan die grid size
        if (buildingData != null)
        {
            gridSize = buildingData.GetGridSize();
        }
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
        if (gridManager != null)
        {
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
        if (data != null)
        {
            gridSize = data.GetGridSize();

            // Apply auto scaling when building data is set
            if (enableAutoScaling && gridManager != null)
            {
                ApplyAutoScaling();
            }
        }
    }

    /// <summary>
    /// Pas automatische scaling toe op basis van grid cell size
    /// </summary>
    public void ApplyAutoScaling()
    {
        if (gridManager == null || !enableAutoScaling) return;

        float cellSize = gridManager.cellSize;

        // Bereken de target size op basis van grid cells
        float targetWidth = gridSize.x * cellSize * scaleMultiplier;
        float targetDepth = gridSize.y * cellSize * scaleMultiplier;

        // Krijg de huidige bounds van het object
        Bounds objectBounds = GetObjectBounds();

        if (objectBounds.size.x > 0 && objectBounds.size.z > 0)
        {
            // Bereken scale factoren
            float scaleX = targetWidth / objectBounds.size.x;
            float scaleZ = targetDepth / objectBounds.size.z;

            // Gebruik de kleinste scale factor om proportioneel te blijven
            float uniformScale = Mathf.Min(scaleX, scaleZ);

            // Pas de nieuwe scale toe
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
        if (showBounds)
        {
            Gizmos.color = boundsColor;

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

                // Teken cell divisions
                Gizmos.color = Color.yellow;
                for (int x = 0; x <= gridSize.x; x++)
                {
                    Vector3 start = center + new Vector3(
                        (x - gridSize.x * 0.5f) * gridManager.cellSize,
                        0,
                        -gridSize.y * 0.5f * gridManager.cellSize
                    );
                    Vector3 end = start + new Vector3(0, 0, gridSize.y * gridManager.cellSize);
                    Gizmos.DrawLine(start, end);
                }

                for (int y = 0; y <= gridSize.y; y++)
                {
                    Vector3 start = center + new Vector3(
                        -gridSize.x * 0.5f * gridManager.cellSize,
                        0,
                        (y - gridSize.y * 0.5f) * gridManager.cellSize
                    );
                    Vector3 end = start + new Vector3(gridSize.x * gridManager.cellSize, 0, 0);
                    Gizmos.DrawLine(start, end);
                }
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
