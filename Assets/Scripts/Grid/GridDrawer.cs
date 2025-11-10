using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles drawing the grid state from GridManager onto a Unity Tilemap.
/// This script reads the state from GridManager and updates the visual representation.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class GridDrawer : MonoBehaviour
{
    [Header("Tile References")]
    [SerializeField] private TileBase _emptyTile;
    [SerializeField] private TileBase _waterTile;
    [SerializeField] private TileBase _grassTile;
    [SerializeField] private TileBase _obstacleTile;

    [Header("Settings")]
    [SerializeField] private bool _updateOnGridChange = true;
    [SerializeField] private bool _drawOnStart = true;

    private Tilemap _tilemap;
    private GridManager _gridManager;

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    private void Start()
    {
        _gridManager = GridManager.Instance;
        
        if (_gridManager == null)
        {
            Debug.LogError("GridDrawer: GridManager instance not found! Make sure GridManager exists in the scene.");
            return;
        }

        // Subscribe to grid change events
        if (_updateOnGridChange)
        {
            _gridManager.OnTileTypeChanged += OnTileTypeChanged;
            _gridManager.OnCellOccupied += OnCellOccupied;
            _gridManager.OnCellCleared += OnCellCleared;
        }

        // Draw the initial grid state
        if (_drawOnStart)
        {
            DrawEntireGrid();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (_gridManager != null)
        {
            _gridManager.OnTileTypeChanged -= OnTileTypeChanged;
            _gridManager.OnCellOccupied -= OnCellOccupied;
            _gridManager.OnCellCleared -= OnCellCleared;
        }
    }

    /// <summary>
    /// Draws the entire grid from GridManager.
    /// </summary>
    public void DrawEntireGrid()
    {
        if (_gridManager == null)
        {
            Debug.LogError("GridDrawer: GridManager instance not found!");
            return;
        }

        Vector2Int gridSize = _gridManager.GetGridSize();
        
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                DrawCell(x, y);
            }
        }
    }

    /// <summary>
    /// Draws a single cell at the specified position.
    /// </summary>
    public void DrawCell(Vector2Int position)
    {
        DrawCell(position.x, position.y);
    }

    /// <summary>
    /// Draws a single cell at the specified coordinates.
    /// </summary>
    public void DrawCell(int x, int y)
    {
        if (_gridManager == null)
        {
            return;
        }

        GridCell cell = _gridManager.GetCell(x, y);
        if (cell == null)
        {
            return;
        }

        Vector3Int tilePosition = new Vector3Int(x, y, 0);
        TileBase tileToSet = GetTileForType(cell.TileType);
        
        _tilemap.SetTile(tilePosition, tileToSet);
    }

    /// <summary>
    /// Gets the appropriate tile asset for a given tile type.
    /// </summary>
    private TileBase GetTileForType(TileType type)
    {
        switch (type)
        {
            case TileType.Water:
                return _waterTile;
            case TileType.Grass:
                return _grassTile;
            case TileType.Obstacle:
                return _obstacleTile;
            case TileType.Empty:
            default:
                return _emptyTile;
        }
    }

    /// <summary>
    /// Event handler for when a tile type changes.
    /// </summary>
    private void OnTileTypeChanged(Vector2Int position, TileType newType)
    {
        DrawCell(position);
    }

    /// <summary>
    /// Event handler for when a cell becomes occupied.
    /// You can extend this to draw animal sprites or indicators.
    /// </summary>
    private void OnCellOccupied(Vector2Int position, int animalId)
    {
        // For now, just redraw the cell
        // In the future, you might want to draw animal sprites here
        DrawCell(position);
    }

    /// <summary>
    /// Event handler for when a cell is cleared.
    /// </summary>
    private void OnCellCleared(Vector2Int position)
    {
        // Redraw the cell to remove any visual indicators
        DrawCell(position);
    }

    /// <summary>
    /// Clears all tiles from the tilemap.
    /// </summary>
    public void ClearTilemap()
    {
        _tilemap.ClearAllTiles();
    }

    /// <summary>
    /// Manually updates the visual representation of a specific area.
    /// </summary>
    public void UpdateArea(Vector2Int min, Vector2Int max)
    {
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                DrawCell(x, y);
            }
        }
    }
}

