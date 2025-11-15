using UnityEngine;

/// <summary>
/// Food item that restores the player's hunger to maximum when used.
/// </summary>
public class FoodItem : Item
{
    /// <summary>
    /// When food is used, restore hunger to maximum.
    /// </summary>
    public override bool OnUse(ControllableAnimal user)
    {
        if (user == null)
        {
            return false;
        }
        
        // Restore hunger to maximum
        user.IncreaseHunger(user.MaxHunger);
        
        Debug.Log($"FoodItem: Restored hunger to maximum for {user.name}.");
        return true; // Item is consumed
    }
}

