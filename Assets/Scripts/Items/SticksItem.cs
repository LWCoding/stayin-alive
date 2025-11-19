using UnityEngine;

/// <summary>
/// Sticks item that can be picked up and stored in inventory.
/// Using it places a den on the tile the player is currently standing on, if it is possible to place a den there.
/// </summary>
public class SticksItem : Item
{
    /// <summary>
    /// When sticks are used, it places a den on the player's current tile.
    /// Returns true if the den was successfully placed (item is consumed), false otherwise.
    /// </summary>
    public override bool OnUse(ControllableAnimal user)
    {
        if (user == null)
        {
            return false;
        }
        
        // Get the player's current grid position
        Vector2Int denPosition = user.GridPosition;
        
        // Check if the position is valid for placing a den
        if (!IsValidPositionForDen(denPosition))
        {
            Debug.Log("SticksItem: Cannot place den at this position - tile is blocked or invalid.");
            return false; // Item is not consumed
        }
        
        // Spawn den at the player's position
        if (InteractableManager.Instance == null)
        {
            Debug.LogError("SticksItem: InteractableManager instance not found!");
            return false;
        }
        
        Den newDen = InteractableManager.Instance.SpawnDen(denPosition);
        if (newDen == null)
        {
            Debug.LogWarning("SticksItem: Failed to spawn den at player position.");
            return false; // Item is not consumed
        }
        
        // Automatically enter the den after building it
        newDen.OnAnimalEnter(user);
        
        Debug.Log($"SticksItem: Successfully placed den at ({denPosition.x}, {denPosition.y}).");
        return true; // Item is consumed
    }
    
    /// <summary>
    /// Checks if a position is valid for placing a den.
    /// Valid positions must be:
    /// - Within grid bounds
    /// - Not water or obstacle tiles
    /// - Not already occupied by an interactable
    /// - Not already occupied by an item
    /// </summary>
    private bool IsValidPositionForDen(Vector2Int position)
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

