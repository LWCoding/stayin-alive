using UnityEngine;

/// <summary>
/// Food item that restores a certain amount of hunger when used.
/// </summary>
public class FoodItem : Item
{
    [Header("Food Settings")]
    [Tooltip("Amount of hunger restored when this food item is consumed.")]
    [SerializeField] private int _hungerRestored;
    
    /// <summary>
    /// Amount of hunger restored when this food item is consumed.
    /// </summary>
    public int HungerRestored => _hungerRestored;
    
    /// <summary>
    /// When food is used, restore the configured amount of hunger.
    /// </summary>
    public override bool OnUse(ControllableAnimal user)
    {
        if (user == null)
        {
            return false;
        }
        
        // Restore the configured amount of hunger
        user.IncreaseHunger(_hungerRestored);
        AudioManager.Instance.PlaySFX(AudioManager.SFXType.Eat);
        
        Debug.Log($"FoodItem: Restored {_hungerRestored} hunger for {user.name}. Current hunger: {user.CurrentHunger}/{user.MaxHunger}.");
        return true; // Item is consumed
    }
}

