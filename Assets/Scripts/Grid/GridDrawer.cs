using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles drawing the grid state from EnvironmentManager onto a Unity Tilemap.
/// This script reads the state from EnvironmentManager and updates the visual representation.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class GridDrawer : MonoBehaviour
{
    [System.Serializable]
    private class WeightedTile
    {
        public TileBase Tile;
        [Min(0f)]
        public float Weight = 1f;
    }

    [Header("Tile References")]
    [SerializeField] private TileBase _emptyTile;
    [SerializeField] private WeightedTile[] _waterTiles;
    [SerializeField] private WeightedTile[] _grassTiles;
    [SerializeField] private WeightedTile[] _obstacleTiles;

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
    /// Randomly selects from available tiles for water and grass types.
    /// </summary>
    private TileBase GetTileForType(TileType type)
    {
        switch (type)
        {
            case TileType.Water:
                return GetWeightedTile(_waterTiles);
            case TileType.Grass:
                return GetWeightedTile(_grassTiles);
            case TileType.Obstacle:
                return GetWeightedTile(_obstacleTiles);
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

    private TileBase GetWeightedTile(WeightedTile[] tiles)
    {
        if (tiles == null || tiles.Length == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        foreach (var tile in tiles)
        {
            if (tile.Tile != null && tile.Weight > 0f)
            {
                float weight = tile.Weight <= 0f ? 1f : tile.Weight;
                totalWeight += weight;
            }
        }

        if (totalWeight <= 0f)
        {
            // fallback to uniform random among non-null entries
            var validTiles = System.Array.FindAll(tiles, t => t.Tile != null);
            if (validTiles.Length == 0)
            {
                return null;
            }

            return validTiles[Random.Range(0, validTiles.Length)].Tile;
        }

        float pick = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var tile in tiles)
        {
            if (tile.Tile == null || tile.Weight <= 0f)
            {
                continue;
            }

            float weight = tile.Weight <= 0f ? 1f : tile.Weight;
            cumulative += weight;
            if (pick <= cumulative)
            {
                return tile.Tile;
            }
        }

        // should not reach here, but return last valid tile as a safeguard
        for (int i = tiles.Length - 1; i >= 0; i--)
        {
            if (tiles[i].Tile != null)
            {
                return tiles[i].Tile;
            }
        }

        return null;
    }
}

