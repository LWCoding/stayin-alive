using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the state of the game grid. Stores tile types, positions, and occupancy.
/// This is a singleton that can be accessed from anywhere in the game.
/// </summary>
public class GridManager : Singleton<GridManager>
{
    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 20;
    [SerializeField] private int _gridHeight = 20;

    // The main grid state - stores all cells
    private GridCell[,] _grid;

    // Events for when the grid changes (useful for updating visuals)
    public Action<Vector2Int, TileType> OnTileTypeChanged;
    public Action<Vector2Int, int> OnCellOccupied;
    public Action<Vector2Int> OnCellCleared;

    protected override void Awake()
    {
        base.Awake();
        InitializeGrid();
    }

    /// <summary>
    /// Initializes the grid with empty tiles.
    /// </summary>
    public void InitializeGrid()
    {
        _grid = new GridCell[_gridWidth, _gridHeight];
        
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _grid[x, y] = new GridCell(TileType.Empty, x, y);
            }
        }
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
    /// Marks a cell as occupied by an animal.
    /// </summary>
    public void SetCellOccupied(Vector2Int position, int animalId)
    {
        if (IsValidPosition(position.x, position.y))
        {
            _grid[position.x, position.y].SetOccupant(animalId);
            OnCellOccupied?.Invoke(position, animalId);
        }
    }

    /// <summary>
    /// Clears the occupant from a cell.
    /// </summary>
    public void ClearCellOccupant(Vector2Int position)
    {
        if (IsValidPosition(position.x, position.y))
        {
            _grid[position.x, position.y].ClearOccupant();
            OnCellCleared?.Invoke(position);
        }
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
    /// Checks if a cell is walkable (not an obstacle and optionally not occupied).
    /// </summary>
    public bool IsWalkable(Vector2Int position, bool checkOccupied = false)
    {
        if (!IsValidPosition(position))
            return false;

        GridCell cell = GetCell(position);
        if (cell.TileType == TileType.Obstacle)
            return false;

        if (checkOccupied && cell.IsOccupied)
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
    /// Clears the entire grid (sets all tiles to Empty and clears occupants).
    /// </summary>
    public void ClearGrid()
    {
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _grid[x, y].TileType = TileType.Empty;
                _grid[x, y].ClearOccupant();
            }
        }
    }
}

