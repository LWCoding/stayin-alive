using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles drawing the grid state from EnvironmentManager onto a Unity Tilemap.
/// This script reads the state from EnvironmentManager and updates the visual representation.
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
    private EnvironmentManager _environmentManager;

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    private void Start()
    {
        _environmentManager = EnvironmentManager.Instance;
        
        if (_environmentManager == null)
        {
            Debug.LogError("GridDrawer: EnvironmentManager instance not found! Make sure EnvironmentManager exists in the scene.");
            return;
        }

        // Subscribe to grid change events
        if (_updateOnGridChange)
        {
            _environmentManager.OnTileTypeChanged += OnTileTypeChanged;
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
        if (_environmentManager != null)
        {
            _environmentManager.OnTileTypeChanged -= OnTileTypeChanged;
        }
    }

    /// <summary>
    /// Draws the entire grid from GridManager.
    /// </summary>
    public void DrawEntireGrid()
    {
        if (_environmentManager == null)
        {
            Debug.LogError("GridDrawer: EnvironmentManager instance not found!");
            return;
        }

        Vector2Int gridSize = _environmentManager.GetGridSize();
        
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
        if (_environmentManager == null)
        {
            return;
        }

        GridCell cell = _environmentManager.GetCell(x, y);
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

