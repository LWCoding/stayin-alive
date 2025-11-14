using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages fog of war by placing darkness tiles on top of the environment.
/// Reveals tiles around controllable animals based on their fog of war radius.
/// </summary>
public class FogOfWarManager : Singleton<FogOfWarManager>
{
    [Header("Fog Settings")]
    [SerializeField] [Tooltip("Darkness tile to use for fog of war")]
    private TileBase _darknessTile;

    [Header("References")]
    [SerializeField] [Tooltip("Tilemap for fog of war. Must be assigned via Inspector. Should be on a separate layer above the environment tiles.")]
    private Tilemap _fogTilemap;

    // Track which tiles are revealed (true = revealed, false = hidden)
    private bool[,] _revealedTiles;
    private int _gridWidth;
    private int _gridHeight;

    protected override void Awake()
    {
        base.Awake();

        // Validate that Tilemap is assigned
        if (_fogTilemap == null)
        {
            Debug.LogError("FogOfWarManager: Fog tilemap is not assigned! Please assign a Tilemap component in the Inspector.");
        }

        // Subscribe to grid initialization in Awake to ensure we catch the event even if grid is initialized early
        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.OnGridInitialized += OnGridInitialized;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from events
        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.OnGridInitialized -= OnGridInitialized;
        }
    }

    /// <summary>
    /// Called when the grid is initialized. Stores grid dimensions.
    /// Fog will be initialized when InitializeFog() is called after level is loaded.
    /// </summary>
    private void OnGridInitialized(int width, int height)
    {
        _gridWidth = width;
        _gridHeight = height;
        _revealedTiles = new bool[_gridWidth, _gridHeight];
        
        // Note: Fog initialization will happen after level is fully loaded
        // to ensure it covers all tiles properly
    }

    /// <summary>
    /// Initializes fog of war by placing darkness tiles on all grid positions.
    /// This ensures black fog tiles cover the entire loaded level.
    /// </summary>
    public void InitializeFog()
    {
        if (_fogTilemap == null)
        {
            Debug.LogError("FogOfWarManager: Fog tilemap is null! Cannot initialize fog of war.");
            return;
        }

        if (_darknessTile == null)
        {
            Debug.LogWarning("FogOfWarManager: Darkness tile is not assigned! Fog of war will not be visible.");
            return;
        }

        // Get grid dimensions from EnvironmentManager if not already set
        if (_gridWidth <= 0 || _gridHeight <= 0)
        {
            if (EnvironmentManager.Instance != null)
            {
                Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
                _gridWidth = gridSize.x;
                _gridHeight = gridSize.y;
            }
            
            // If still invalid, can't initialize
            if (_gridWidth <= 0 || _gridHeight <= 0)
            {
                Debug.LogWarning("FogOfWarManager: Grid dimensions are invalid! Cannot initialize fog of war. Make sure the grid is initialized first.");
                return;
            }
        }

        // Initialize revealed tiles array if not already initialized
        if (_revealedTiles == null)
        {
            _revealedTiles = new bool[_gridWidth, _gridHeight];
        }

        // Clear existing fog tiles to ensure clean state
        _fogTilemap.ClearAllTiles();

        // Set all tiles to darkness (black fog covering everything)
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                _revealedTiles[x, y] = false;
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                _fogTilemap.SetTile(tilePosition, _darknessTile);
            }
        }

        Debug.Log($"FogOfWarManager: Initialized fog of war with black tiles covering {_gridWidth}x{_gridHeight} grid.");
    }

    /// <summary>
    /// Updates fog of war based on controllable animals' positions and radii.
    /// Should be called after animals move each turn.
    /// </summary>
    public void UpdateFogOfWar()
    {
        if (_fogTilemap == null || _darknessTile == null)
        {
            return;
        }

        if (_revealedTiles == null)
        {
            Debug.LogWarning("FogOfWarManager: Fog of war not initialized! Grid may not be initialized yet.");
            return;
        }

        // Get all controllable animals
        if (AnimalManager.Instance == null)
        {
            return;
        }

        List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
        HashSet<Vector2Int> tilesToReveal = new HashSet<Vector2Int>();

        // Collect all tiles that should be revealed
        foreach (Animal animal in animals)
        {
            if (animal == null || !animal.IsControllable)
            {
                continue;
            }

            // Use global fog of war radius for controllable animals
            int radius = Globals.FogOfWarRadius;
            Vector2Int animalPosition = animal.GridPosition;

            // Reveal tiles in radius around the animal
            RevealTilesInRadius(animalPosition, radius, tilesToReveal);
        }

        // Update fog tilemap: remove darkness from revealed tiles
        foreach (Vector2Int position in tilesToReveal)
        {
            if (IsValidPosition(position))
            {
                _revealedTiles[position.x, position.y] = true;
                Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
                _fogTilemap.SetTile(tilePosition, null); // Remove darkness tile
            }
        }
    }

    /// <summary>
    /// Reveals tiles in a circular radius around a center position.
    /// </summary>
    private void RevealTilesInRadius(Vector2Int center, int radius, HashSet<Vector2Int> revealedTiles)
    {
        // Use a circular radius check
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Check if tile is within circular radius
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= radius)
                {
                    Vector2Int tilePos = center + new Vector2Int(x, y);
                    if (IsValidPosition(tilePos))
                    {
                        revealedTiles.Add(tilePos);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a position is valid (within grid bounds).
    /// </summary>
    private bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < _gridWidth && 
               position.y >= 0 && position.y < _gridHeight;
    }

    /// <summary>
    /// Manually reveals a specific tile (for testing or special cases).
    /// </summary>
    public void RevealTile(Vector2Int position)
    {
        if (!IsValidPosition(position))
        {
            return;
        }

        if (_fogTilemap == null)
        {
            return;
        }

        _revealedTiles[position.x, position.y] = true;
        Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
        _fogTilemap.SetTile(tilePosition, null);
    }

    /// <summary>
    /// Manually hides a specific tile (for testing or special cases).
    /// </summary>
    public void HideTile(Vector2Int position)
    {
        if (!IsValidPosition(position))
        {
            return;
        }

        if (_fogTilemap == null || _darknessTile == null)
        {
            return;
        }

        _revealedTiles[position.x, position.y] = false;
        Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
        _fogTilemap.SetTile(tilePosition, _darknessTile);
    }

    /// <summary>
    /// Checks if a tile is currently revealed.
    /// </summary>
    public bool IsTileRevealed(Vector2Int position)
    {
        if (!IsValidPosition(position))
        {
            return false;
        }

        return _revealedTiles[position.x, position.y];
    }
}

