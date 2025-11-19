using System.Collections.Generic;
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
    private readonly HashSet<int> _blockedNodeIndices;

    /// <summary>
    /// Creates a new WaterTraversalProvider.
    /// Takes a snapshot of the grid data at construction time for thread-safe access.
    /// </summary>
    /// <param name="canGoOnWater">Whether the animal can traverse water tiles.</param>
    public WaterTraversalProvider(bool canGoOnWater, HashSet<Vector2Int> blockedPositions = null)
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

                if (blockedPositions != null && blockedPositions.Count > 0)
                {
                    _blockedNodeIndices = new HashSet<int>(blockedPositions.Count);
                    foreach (Vector2Int pos in blockedPositions)
                    {
                        if (pos.x < 0 || pos.x >= _gridWidth || pos.y < 0 || pos.y >= _gridHeight)
                        {
                            continue;
                        }
                        _blockedNodeIndices.Add(GetNodeIndex(pos.x, pos.y));
                    }
                }
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

    private int GetNodeIndex(int x, int y)
    {
        return y * _gridWidth + x;
    }

    /// <summary>
    /// Determines if a node can be traversed based on water walkability.
    /// Thread-safe: only reads from cached snapshot data and uses node coordinates directly.
    /// </summary>
    public bool CanTraverse(Path path, GraphNode node)
    {
        // If no grid data is available, fall back to default traversal
        if (!_hasGridData || _gridSnapshot == null)
        {
            return DefaultITraversalProvider.CanTraverse(path, node);
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
            // Out of bounds - use default traversal check
            return DefaultITraversalProvider.CanTraverse(path, node);
        }

        if (_blockedNodeIndices != null)
        {
            int nodeIndex = GetNodeIndex(x, y);
            if (_blockedNodeIndices.Contains(nodeIndex))
            {
                return false;
            }
        }

        // Check if this is a water tile (reading from cached snapshot - thread-safe)
        TileType tileType = _gridSnapshot[x, y];
        if (tileType == TileType.Water)
        {
            // For water tiles, check if the animal can go on water
            if (_canGoOnWater)
            {
                // Animal can go on water - allow traversal even if node is marked non-walkable
                // Still check tags from default traversal
                return (path.enabledTags >> (int)node.Tag & 0x1) != 0;
            }
            else
            {
                // Animal cannot go on water, so this node is not traversable
                return false;
            }
        }

        // Not a water tile - use default traversal check (walkable and tags)
        return DefaultITraversalProvider.CanTraverse(path, node);
    }

    /// <summary>
    /// Gets the traversal cost for a node. Uses default implementation.
    /// </summary>
    public uint GetTraversalCost(Path path, GraphNode node)
    {
        return DefaultITraversalProvider.GetTraversalCost(path, node);
    }
}

