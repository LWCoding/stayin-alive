using UnityEngine;
using Pathfinding;

/// <summary>
/// Custom traversal provider that filters out water tiles for animals that cannot go on water.
/// Implements ITraversalProvider to integrate with A* pathfinding.
/// Thread-safe implementation that caches grid data and avoids Unity API calls from worker threads.
/// </summary>
public class WaterTraversalProvider : ITraversalProvider
{
    private readonly bool _canGoOnWater;
    
    // Thread-safe cached grid data (snapshot taken at construction)
    // These arrays are only read from worker threads, so they're safe
    private readonly TileType[,] _gridSnapshot;
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    
    // Cached reference to avoid repeated null checks (set once at construction)
    private readonly bool _hasGridData;

    /// <summary>
    /// Creates a new WaterTraversalProvider.
    /// Takes a snapshot of the grid data at construction time for thread-safe access.
    /// </summary>
    /// <param name="canGoOnWater">Whether the animal can traverse water tiles.</param>
    public WaterTraversalProvider(bool canGoOnWater)
    {
        _canGoOnWater = canGoOnWater;
        
        // Cache grid data at construction time (on main thread)
        EnvironmentManager envManager = EnvironmentManager.Instance;
        if (envManager != null)
        {
            Vector2Int gridSize = envManager.GetGridSize();
            _gridWidth = gridSize.x;
            _gridHeight = gridSize.y;
            
            if (_gridWidth > 0 && _gridHeight > 0)
            {
                // Create a snapshot of the tile data
                _gridSnapshot = new TileType[_gridWidth, _gridHeight];
                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        _gridSnapshot[x, y] = envManager.GetTileType(x, y);
                    }
                }
                _hasGridData = true;
            }
            else
            {
                _gridSnapshot = null;
                _hasGridData = false;
            }
        }
        else
        {
            _gridSnapshot = null;
            _gridWidth = 0;
            _gridHeight = 0;
            _hasGridData = false;
        }
    }

    /// <summary>
    /// Determines if a node can be traversed based on water walkability.
    /// Thread-safe: only reads from cached snapshot data and uses node coordinates directly.
    /// </summary>
    public bool CanTraverse(Path path, GraphNode node)
    {
        // First check default traversal (walkable and tags)
        if (!DefaultITraversalProvider.CanTraverse(path, node))
        {
            return false;
        }

        // If the animal can go on water, allow all walkable nodes
        if (_canGoOnWater)
        {
            return true;
        }

        // If no grid data is available, fall back to allowing traversal
        if (!_hasGridData || _gridSnapshot == null)
        {
            return true;
        }

        // Get grid coordinates directly from the node (thread-safe, no Unity API calls)
        int x, y;
        if (node is GridNodeBase gridNode)
        {
            // Use grid coordinates directly from GridNode (most efficient and thread-safe)
            x = gridNode.XCoordinateInGrid;
            y = gridNode.ZCoordinateInGrid; // Note: A* uses Z for Y in 2D
        }
        else
        {
            // Fallback: convert from world position using math (no Unity API calls)
            // This is less accurate but still thread-safe
            Vector3 worldPos = (Vector3)node.position;
            x = Mathf.RoundToInt(worldPos.x);
            y = Mathf.RoundToInt(worldPos.y);
        }

        // Bounds check
        if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
        {
            // Out of bounds - allow traversal (or could return false, depends on design)
            return true;
        }

        // Check if this is a water tile (reading from cached snapshot - thread-safe)
        TileType tileType = _gridSnapshot[x, y];
        if (tileType == TileType.Water)
        {
            // Animal cannot go on water, so this node is not traversable
            return false;
        }

        // Not a water tile, so it's traversable
        return true;
    }

    /// <summary>
    /// Gets the traversal cost for a node. Uses default implementation.
    /// </summary>
    public uint GetTraversalCost(Path path, GraphNode node)
    {
        return DefaultITraversalProvider.GetTraversalCost(path, node);
    }
}

