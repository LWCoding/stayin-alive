using UnityEngine;
using Pathfinding;

/// <summary>
/// Custom traversal provider that filters out water tiles for animals that cannot go on water.
/// Implements ITraversalProvider to integrate with A* pathfinding.
/// </summary>
public class WaterTraversalProvider : ITraversalProvider
{
    private bool _canGoOnWater;
    private EnvironmentManager _environmentManager;

    /// <summary>
    /// Creates a new WaterTraversalProvider.
    /// </summary>
    /// <param name="canGoOnWater">Whether the animal can traverse water tiles.</param>
    public WaterTraversalProvider(bool canGoOnWater)
    {
        _canGoOnWater = canGoOnWater;
        _environmentManager = EnvironmentManager.Instance;
    }

    /// <summary>
    /// Determines if a node can be traversed based on water walkability.
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

        // If the animal cannot go on water, check if this node is a water tile
        if (_environmentManager == null)
        {
            // If EnvironmentManager is not available, fall back to default behavior
            return true;
        }

        // Convert node world position to grid position
        Vector3 worldPos = (Vector3)node.position;
        Vector2Int gridPos = _environmentManager.WorldToGridPosition(worldPos);

        // Check if this is a water tile
        TileType tileType = _environmentManager.GetTileType(gridPos);
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

