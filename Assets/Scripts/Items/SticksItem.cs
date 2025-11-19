using UnityEngine;

/// <summary>
/// Sticks item that can be picked up and stored in inventory.
/// Using it has no effect for now.
/// </summary>
public class SticksItem : Item
{
    /// <summary>
    /// When sticks are used, they have no effect (for now).
    /// Returns false to indicate the item was not consumed.
    /// </summary>
    public override bool OnUse(ControllableAnimal user)
    {
        if (user == null)
        {
            return false;
        }
        
        // Sticks have no effect for now
        Debug.Log("SticksItem: Using sticks has no effect.");
        return false; // Item is not consumed
    }
}

