using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOffset = Vector3.zero;

    [Header("Visual")]
    public Material gridMaterial;
    public bool showGridOnStart = false;  // Grid niet standaard tonen

    [Header("Building Mode Visual")]
    public Color normalGridColor = Color.white;
    public Color buildingModeColor = new Color(0, 1, 0, 0.8f);  // Groen tijdens building

    private GridCell[,] grid;
    private Camera playerCamera;
    private GameObject gridVisualParent;
    private LineRenderer[] gridLines;
    private bool isGridVisible = false;

    [System.Serializable]
    public class GridCell
    {
        public Vector3 worldPosition;
        public bool isOccupied;
        public GameObject occupyingObject;

        public GridCell(Vector3 position)
        {
            worldPosition = position;
            isOccupied = false;
            occupyingObject = null;
        }
    }

    void Start()
    {
        playerCamera = Camera.main;
        InitializeGrid();
        CreateVisualGrid();

        // Toon grid alleen als showGridOnStart true is
        SetGridVisibility(showGridOnStart);

        // Luister naar building mode events
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnBuildingModeStarted += OnBuildingModeStarted;
            GridBuildingSystem.Instance.OnBuildingModeEnded += OnBuildingModeEnded;
        }
    }

    void OnDestroy()
    {
        // Clean up events
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnBuildingModeStarted -= OnBuildingModeStarted;
            GridBuildingSystem.Instance.OnBuildingModeEnded -= OnBuildingModeEnded;
        }
    }

    void OnBuildingModeStarted(BuildingData buildingData)
    {
        ShowGrid();
        SetGridColor(buildingModeColor);
        Debug.Log("Grid shown for building mode");
    }

    void OnBuildingModeEnded()
    {
        HideGrid();
        Debug.Log("Grid hidden after building mode");
    }

    void InitializeGrid()
    {
        grid = new GridCell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 cellPosition = new Vector3(
                    x * cellSize + gridOffset.x,
                    gridOffset.y,
                    z * cellSize + gridOffset.z
                );
                grid[x, z] = new GridCell(cellPosition);
            }
        }
    }

    void CreateVisualGrid()
    {
        // Maak parent object voor alle grid lijnen
        gridVisualParent = new GameObject("Grid Visual");
        gridVisualParent.transform.SetParent(transform);

        // Bereken hoeveel lijnen we nodig hebben
        int totalLines = (gridHeight + 1) + (gridWidth + 1);
        gridLines = new LineRenderer[totalLines];
        int lineIndex = 0;

        // Create horizontal lines
        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = new Vector3(gridOffset.x, gridOffset.y, z * cellSize + gridOffset.z);
            Vector3 end = new Vector3(gridWidth * cellSize + gridOffset.x, gridOffset.y, z * cellSize + gridOffset.z);
            gridLines[lineIndex] = CreateLine(start, end, gridVisualParent.transform, $"GridLine_H_{z}");
            lineIndex++;
        }

        // Create vertical lines
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(x * cellSize + gridOffset.x, gridOffset.y, gridOffset.z);
            Vector3 end = new Vector3(x * cellSize + gridOffset.x, gridOffset.y, gridHeight * cellSize + gridOffset.z);
            gridLines[lineIndex] = CreateLine(start, end, gridVisualParent.transform, $"GridLine_V_{x}");
            lineIndex++;
        }

        Debug.Log($"Created {totalLines} grid lines");
    }

    LineRenderer CreateLine(Vector3 start, Vector3 end, Transform parent, string name)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent);

        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.material = gridMaterial;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        return lr;
    }

    /// <summary>
    /// Toon de grid
    /// </summary>
    public void ShowGrid()
    {
        SetGridVisibility(true);
    }

    /// <summary>
    /// Verberg de grid
    /// </summary>
    public void HideGrid()
    {
        SetGridVisibility(false);
    }

    /// <summary>
    /// Toggle grid zichtbaarheid
    /// </summary>
    public void ToggleGrid()
    {
        SetGridVisibility(!isGridVisible);
    }

    /// <summary>
    /// Stel grid zichtbaarheid in
    /// </summary>
    public void SetGridVisibility(bool visible)
    {
        isGridVisible = visible;

        if (gridVisualParent != null)
        {
            gridVisualParent.SetActive(visible);
        }

        Debug.Log($"Grid visibility set to: {visible}");
    }

    /// <summary>
    /// Verander de kleur van alle grid lijnen
    /// </summary>
    public void SetGridColor(Color color)
    {
        if (gridLines == null) return;

        foreach (var line in gridLines)
        {
            if (line != null && line.material != null)
            {
                line.material.color = color;
            }
        }
    }

    /// <summary>
    /// Reset grid kleur naar normaal
    /// </summary>
    public void ResetGridColor()
    {
        SetGridColor(normalGridColor);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOffset.x) / cellSize);
        int z = Mathf.RoundToInt((worldPosition.z - gridOffset.z) / cellSize);
        return new Vector2Int(x, z);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x * cellSize + gridOffset.x,
            gridOffset.y,
            gridPosition.y * cellSize + gridOffset.z
        );
    }

    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
               gridPosition.y >= 0 && gridPosition.y < gridHeight &&
               !grid[gridPosition.x, gridPosition.y].isOccupied;
    }

    public void OccupyGridCell(Vector2Int gridPosition, GameObject occupyingObject)
    {
        if (gridPosition.x >= 0 && gridPosition.x < gridWidth &&
            gridPosition.y >= 0 && gridPosition.y < gridHeight)
        {
            grid[gridPosition.x, gridPosition.y].isOccupied = true;
            grid[gridPosition.x, gridPosition.y].occupyingObject = occupyingObject;
        }
    }

    public bool RemoveBuilding(Vector2Int gridPosition)
    {
        if (gridPosition.x < 0 || gridPosition.x >= gridWidth ||
            gridPosition.y < 0 || gridPosition.y >= gridHeight ||
            !grid[gridPosition.x, gridPosition.y].isOccupied)
            return false;

        if (grid[gridPosition.x, gridPosition.y].occupyingObject != null)
        {
            BuildableObject buildable = grid[gridPosition.x, gridPosition.y].occupyingObject.GetComponent<BuildableObject>();
            if (buildable != null)
            {
                Vector2Int[] occupiedPositions = buildable.GetOccupiedGridPositions();
                foreach (var pos in occupiedPositions)
                {
                    ClearGridCell(pos);
                }
            }
            else
            {
                ClearGridCell(gridPosition);
            }

            Destroy(grid[gridPosition.x, gridPosition.y].occupyingObject);
        }

        return true;
    }

    void ClearGridCell(Vector2Int gridPosition)
    {
        if (gridPosition.x >= 0 && gridPosition.x < gridWidth &&
            gridPosition.y >= 0 && gridPosition.y < gridHeight)
        {
            grid[gridPosition.x, gridPosition.y].isOccupied = false;
            grid[gridPosition.x, gridPosition.y].occupyingObject = null;
        }
    }

    public bool IsGridVisible()
    {
        return isGridVisible;
    }

    // Debug functie om grid handmatig te testen
    [ContextMenu("Test Show Grid")]
    void TestShowGrid()
    {
        ShowGrid();
    }

    [ContextMenu("Test Hide Grid")]
    void TestHideGrid()
    {
        HideGrid();
    }
}
