using UnityEngine;

/// <summary>
/// Sticks item that can be picked up and stored in inventory.
/// Using it places a den on the tile the player is currently standing on, if it is possible to place a den there.
/// </summary>
public class SticksItem : Item
{

    private static int _configuredStickCostCap = Globals.MaxDenStickCost;

    [Header("Usage Messaging")]
    [SerializeField] [TextArea(2, 4)] [Tooltip("Shown when the player does not have enough sticks. Use {0} to display how many more are required.")]
    private string _insufficientSticksDescription = "Can be used for building dens. Requires {0} more sticks.";

    [HideInInspector]
    public string InsufficientSticksDescription => _insufficientSticksDescription;


    /// <summary>
    /// Gets the stick cost needed for the next den placement.
    /// </summary>
    public static int GetNextDenStickCost()
    {
        int densBuilt = 0;
        if (DenSystemManager.Instance != null)
        {
            densBuilt = DenSystemManager.Instance.DensBuiltWithSticks;
        }
        
        return Mathf.Clamp(densBuilt + 1, 1, _configuredStickCostCap);
    }

    /// <summary>
    /// Calculates how many more sticks are required, given the player's current inventory.
    /// </summary>
    public static int GetStickDeficit(int currentStickCount)
    {
        return Mathf.Max(0, GetNextDenStickCost() - currentStickCount);
    }

    private static void AdvanceCostProgression()
    {
        if (DenSystemManager.Instance != null)
        {
            DenSystemManager.Instance.IncrementDensBuiltWithSticks();
        }
    }
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
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("SticksItem: InventoryManager instance not found. Cannot validate stick requirements.");
            return false;
        }
        
        int sticksRequired = GetNextDenStickCost();
        int availableSticks = InventoryManager.Instance.GetItemCount(ItemName);
        
        if (availableSticks < sticksRequired)
        {
            int deficit = sticksRequired - availableSticks;
            Debug.Log($"SticksItem: Cannot place den - need {deficit} more stick(s).");
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
        
        // Ensure we have the interactable manager ready before consuming resources
        if (InteractableManager.Instance == null)
        {
            Debug.LogError("SticksItem: InteractableManager instance not found!");
            return false;
        }
        
        // Consume the exact number of sticks required for this den build
        bool sticksConsumed = InventoryManager.Instance.ConsumeItemsForActiveUse(ItemName, sticksRequired);
        if (!sticksConsumed)
        {
            Debug.LogWarning("SticksItem: Failed to consume the required sticks for den placement.");
            return false;
        }
        
        // Spawn den at the player's position
        Den newDen = InteractableManager.Instance.SpawnDen(denPosition);
        if (newDen == null)
        {
            Debug.LogWarning("SticksItem: Failed to spawn den at player position.");
            RefundConsumedSticks(sticksRequired);
            return false; // Item is not consumed
        }
        
        AdvanceCostProgression();
        
        // Notify that a den was built by player
        InteractableManager.Instance.NotifyDenBuiltByPlayer(newDen);
        
        // Automatically enter the den after building it
        newDen.OnAnimalEnter(user);
        
        Debug.Log($"SticksItem: Successfully placed den at ({denPosition.x}, {denPosition.y}).");
        return true; // Item is consumed
    }
    
    private void RefundConsumedSticks(int count)
    {
        if (InventoryManager.Instance == null || count <= 0)
        {
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            bool added = InventoryManager.Instance.AddItem(ItemName);
            if (!added)
            {
                Debug.LogWarning("SticksItem: Failed to refund sticks after a den placement error.");
                break;
            }
        }
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

