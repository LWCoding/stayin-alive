using UnityEngine;

/// <summary>
/// Grass seed item that can be picked up and stored in inventory.
/// Using it has no effect for now.
/// </summary>
public class GrassSeedItem : Item
{
    /// <summary>
    /// When grass seed is used, it has no effect (for now).
    /// Returns false to indicate the item was not consumed.
    /// </summary>
    public override bool OnUse(ControllableAnimal user)
    {
        if (user == null)
        {
            return false;
        }
        
        // Grass seed has no effect for now
        Debug.Log("GrassSeedItem: Using grass seed has no effect.");
        return false; // Item is not consumed
    }
}

