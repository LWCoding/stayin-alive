using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the state of the game environment (grid). Stores tile types and positions.
/// This is a singleton that can be accessed from anywhere in the game.
/// </summary>
public class EnvironmentManager : Singleton<EnvironmentManager>
{
    [Header("Grid Reference")]
    [SerializeField] private Grid _gridComponent;

    // The main grid state - stores all cells
    private GridCell[,] _grid;
    private int _gridWidth;
    private int _gridHeight;
    
    public Vector2 GridSize => new Vector2(_gridWidth, _gridHeight);

    // Events for when the grid changes (useful for updating visuals)
    public Action<Vector2Int, TileType> OnTileTypeChanged;
    
    // Event for when the grid is initialized (useful for camera setup, etc.)
    public Action<int, int> OnGridInitialized;

    [SerializeField]
    private Camera MiniMapRenderer;


    protected override void Awake()
    {
        base.Awake();
        
        // Try to find Grid component if not assigned
        if (_gridComponent == null)
        {
            _gridComponent = FindObjectOfType<Grid>();
            if (_gridComponent == null)
            {
                Debug.LogWarning("EnvironmentManager: Grid component not found! World position conversion may not work correctly.");
            }
        }
        
        // Grid will be initialized when a level is loaded
    }

    /// <summary>
    /// Initializes the grid with empty tiles of the specified size.
    /// </summary>
    public void InitializeGrid(int width, int height)
    {
        _gridWidth = width;
        _gridHeight = height;
        _grid = new GridCell[_gridWidth, _gridHeight];
        
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _grid[x, y] = new GridCell(TileType.Empty, x, y);
            }
        }
        
        // Set position and size of MiniMapRendererCamera so it covers the grid properly
        MiniMapRenderer.transform.position = new Vector3(_gridWidth / 2f, _gridHeight / 2f, 0);
        MiniMapRenderer.orthographicSize = _gridWidth/2f;
        
        // Notify listeners that the grid has been initialized
        OnGridInitialized?.Invoke(width, height);
    }

    /// <summary>
    /// Gets the grid dimensions.
    /// </summary>
    public Vector2Int GetGridSize()
    {
        return new Vector2Int(_gridWidth, _gridHeight);
    }

    /// <summary>
    /// Gets a cell at the specified position. Returns null if out of bounds.
    /// </summary>
    public GridCell GetCell(Vector2Int position)
    {
        return GetCell(position.x, position.y);
    }

    /// <summary>
    /// Gets a cell at the specified coordinates. Returns null if out of bounds or grid not initialized.
    /// </summary>
    public GridCell GetCell(int x, int y)
    {
        if (_grid == null)
        {
            return null;
        }
        
        if (IsValidPosition(x, y))
        {
            return _grid[x, y];
        }
        return null;
    }

    /// <summary>
    /// Sets the tile type at the specified position.
    /// </summary>
    public void SetTileType(Vector2Int position, TileType type)
    {
        SetTileType(position.x, position.y, type);
    }

    /// <summary>
    /// Sets the tile type at the specified coordinates.
    /// </summary>
    public void SetTileType(int x, int y, TileType type)
    {
        if (IsValidPosition(x, y))
        {
            _grid[x, y].TileType = type;
            OnTileTypeChanged?.Invoke(new Vector2Int(x, y), type);
        }
    }

    /// <summary>
    /// Gets the tile type at the specified position.
    /// </summary>
    public TileType GetTileType(Vector2Int position)
    {
        return GetTileType(position.x, position.y);
    }

    /// <summary>
    /// Gets the tile type at the specified coordinates.
    /// </summary>
    public TileType GetTileType(int x, int y)
    {
        if (IsValidPosition(x, y))
        {
            return _grid[x, y].TileType;
        }
        return TileType.Empty;
    }

    /// <summary>
    /// Sets a water tile at the specified position.
    /// </summary>
    public void SetWaterTile(Vector2Int position)
    {
        SetTileType(position, TileType.Water);
    }

    /// <summary>
    /// Sets a grass tile at the specified position.
    /// </summary>
    public void SetGrassTile(Vector2Int position)
    {
        SetTileType(position, TileType.Grass);
    }

    /// <summary>
    /// Sets multiple tiles of a specific type in a list of positions.
    /// </summary>
    public void SetTiles(List<Vector2Int> positions, TileType type)
    {
        foreach (Vector2Int pos in positions)
        {
            SetTileType(pos, type);
        }
    }

    /// <summary>
    /// Converts a grid position to world position.
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return GridToWorldPosition(gridPosition.x, gridPosition.y);
    }

    /// <summary>
    /// Converts grid coordinates to world position.
    /// </summary>
    public Vector3 GridToWorldPosition(int x, int y)
    {
        if (_gridComponent != null)
        {
            Vector3Int cellPosition = new Vector3Int(x, y, 0);
            return _gridComponent.CellToWorld(cellPosition) + _gridComponent.cellSize * 0.5f;
        }
        
        // Fallback if no grid component
        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// Converts a world position into a grid position.
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        if (_gridComponent != null)
        {
            Vector3Int cellPosition = _gridComponent.WorldToCell(worldPosition);
            return new Vector2Int(cellPosition.x, cellPosition.y);
        }

        // Fallback if no grid component
        return new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.y));
    }

    /// <summary>
    /// Checks if a position is valid (within grid bounds).
    /// </summary>
    public bool IsValidPosition(Vector2Int position)
    {
        return IsValidPosition(position.x, position.y);
    }

    /// <summary>
    /// Checks if coordinates are valid (within grid bounds).
    /// </summary>
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
    }

    /// <summary>
    /// Checks if a cell is walkable (not an obstacle).
    /// </summary>
    public bool IsWalkable(Vector2Int position)
    {
        if (!IsValidPosition(position))
            return false;

        GridCell cell = GetCell(position);
        if (cell.TileType == TileType.Obstacle)
            return false;

        return true;
    }

    /// <summary>
    /// Gets all neighboring cells (4-directional).
    /// </summary>
    public List<GridCell> GetNeighbors(Vector2Int position)
    {
        List<GridCell> neighbors = new List<GridCell>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = position + dir;
            GridCell neighbor = GetCell(neighborPos);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Gets all neighboring cells (8-directional including diagonals).
    /// </summary>
    public List<GridCell> GetNeighbors8Directional(Vector2Int position)
    {
        List<GridCell> neighbors = new List<GridCell>();
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = position + dir;
            GridCell neighbor = GetCell(neighborPos);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Clears the entire grid (sets all tiles to Empty).
    /// Triggers visual update events for all tiles.
    /// </summary>
    public void ClearGrid()
    {
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _grid[x, y].TileType = TileType.Empty;
                // Trigger visual update event
                OnTileTypeChanged?.Invoke(new Vector2Int(x, y), TileType.Empty);
            }
        }
    }

}

