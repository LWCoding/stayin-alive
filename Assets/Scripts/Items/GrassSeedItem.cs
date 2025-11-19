using UnityEngine;

/// <summary>
/// Grass seed item that can be picked up and stored in inventory.
/// Using it plants a half-grown grass tile on the tile the player is currently standing on.
/// </summary>
public class GrassSeedItem : Item
{
    /// <summary>
    /// When grass seed is used, it plants a half-grown grass tile on the player's current tile.
    /// Returns true if the grass was successfully planted (item is consumed), false otherwise.
    /// </summary>
    public override bool OnUse(ControllableAnimal user)
    {
        if (user == null)
        {
            return false;
        }
        
        // Get the player's current grid position
        Vector2Int plantPosition = user.GridPosition;
        
        // Check if the position is valid for planting grass
        if (!IsValidPositionForGrass(plantPosition))
        {
            Debug.Log("GrassSeedItem: Cannot plant grass at this position - tile is blocked or invalid.");
            return false; // Item is not consumed
        }
        
        // Spawn grass at the player's position
        if (InteractableManager.Instance == null)
        {
            Debug.LogError("GrassSeedItem: InteractableManager instance not found!");
            return false;
        }
        
        Grass newGrass = InteractableManager.Instance.SpawnGrass(plantPosition);
        if (newGrass == null)
        {
            Debug.LogWarning("GrassSeedItem: Failed to spawn grass at player position.");
            return false; // Item is not consumed
        }
        
        // Set grass to Growing (half-grown) state by harvesting it once
        // This changes it from Full to Growing state
        newGrass.HarvestForAnimal();
        
        Debug.Log($"GrassSeedItem: Successfully planted half-grown grass at ({plantPosition.x}, {plantPosition.y}).");
        return true; // Item is consumed
    }
    
    /// <summary>
    /// Checks if a position is valid for planting grass.
    /// Valid positions must be:
    /// - Within grid bounds
    /// - Not water or obstacle tiles
    /// - Not already occupied by an interactable
    /// - Not already occupied by an item
    /// </summary>
    private bool IsValidPositionForGrass(Vector2Int position)
    {
        if (EnvironmentManager.Instance == null)
        {
            return false;
        }
        
        // Check if position is valid (within grid bounds)
        if (!EnvironmentManager.Instance.IsValidPosition(position))
        {
            return false;
        }
        
        // Check if tile is water or obstacle (walls)
        TileType tileType = EnvironmentManager.Instance.GetTileType(position);
        if (tileType == TileType.Water || tileType == TileType.Obstacle)
        {
            return false;
        }
        
        // Check if there's already an interactable at this position
        if (InteractableManager.Instance != null && InteractableManager.Instance.HasInteractableAtPosition(position))
        {
            return false;
        }
        
        // Check if there's already an item at this position
        if (ItemManager.Instance != null && ItemManager.Instance.GetItemAtPosition(position) != null)
        {
            return false;
        }
        
        return true;
    }
}

