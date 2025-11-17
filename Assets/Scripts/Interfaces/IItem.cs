/// <summary>
/// Interface for items that can be picked up and used by the player.
/// Implement this interface to define custom item behavior.
/// </summary>
public interface IItem
{
    /// <summary>
    /// Called when the item is picked up by the player.
    /// </summary>
    /// <param name="picker">The ControllableAnimal that picked up this item</param>
    void OnPickup(ControllableAnimal picker);
    
    /// <summary>
    /// Called when the item is used from the inventory.
    /// </summary>
    /// <param name="user">The ControllableAnimal that used this item</param>
    /// <returns>True if the item was successfully used and should be consumed, false otherwise</returns>
    bool OnUse(ControllableAnimal user);
    
    /// <summary>
    /// Gets the name identifier for this item. Must match the ItemDatabase entry.
    /// </summary>
    string ItemName { get; }
}

