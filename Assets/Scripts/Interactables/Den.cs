using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A den interactable that provides safety for controllable animals.
/// When a controllable animal is on the same tile as a den, predators cannot target them.
/// </summary>
public class Den : MonoBehaviour
{
    [Header("Den Settings")]
    private Vector2Int _gridPosition;
    
    // Track which animals are currently in this den
    private HashSet<Animal> _animalsInDen = new HashSet<Animal>();
    
    public Vector2Int GridPosition => _gridPosition;
    
    /// <summary>
    /// Initializes the den at the specified grid position.
    /// </summary>
    public void Initialize(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
        
        // Position the den in world space
        if (EnvironmentManager.Instance != null)
        {
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
            transform.position = worldPos;
        }
        else
        {
            transform.position = new Vector3(_gridPosition.x, _gridPosition.y, 0);
        }
    }
    
    /// <summary>
    /// Checks if an animal is currently in this den.
    /// </summary>
    public bool IsAnimalInDen(Animal animal)
    {
        return _animalsInDen.Contains(animal);
    }
    
    /// <summary>
    /// Called when an animal enters this den.
    /// </summary>
    public void OnAnimalEnter(Animal animal)
    {
        if (animal != null && animal.IsControllable)
        {
            _animalsInDen.Add(animal);
            Debug.Log($"Animal '{animal.name}' entered den at ({_gridPosition.x}, {_gridPosition.y})");
            
            // Handle food delivery: check for food items in inventory
            ProcessFoodDelivery(animal);
        }
    }
    
    /// <summary>
    /// Processes food delivery when an animal enters the den.
    /// For each food item, increases animal count by 1, removes the food, and adds points.
    /// </summary>
    private void ProcessFoodDelivery(Animal animal)
    {
        if (animal == null)
        {
            return;
        }
        
        // Check for "Food" items in inventory
        int foodCount = animal.GetItemCount("Food");
        
        if (foodCount > 0)
        {
            // Remove all food from inventory
            int removedCount = animal.RemoveAllItemsFromInventory("Food");
            
            // Increase animal count by the number of food items
            animal.IncreaseAnimalCount(removedCount);
            
            // Add points to PointsManager
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.AddPoints(removedCount);
            }
            
            Debug.Log($"Animal '{animal.name}' delivered {removedCount} food to den. Animal count increased by {removedCount}.");
        }
    }
    
    /// <summary>
    /// Called when an animal leaves this den.
    /// </summary>
    public void OnAnimalLeave(Animal animal)
    {
        if (_animalsInDen.Remove(animal))
        {
            Debug.Log($"Animal '{animal.name}' left den at ({_gridPosition.x}, {_gridPosition.y})");
        }
    }
    
    /// <summary>
    /// Checks if there is a den at the specified grid position.
    /// </summary>
    public static bool IsDenAtPosition(Vector2Int gridPosition)
    {
        if (InteractableManager.Instance == null)
        {
            return false;
        }
        
        return InteractableManager.Instance.GetDenAtPosition(gridPosition) != null;
    }
    
    /// <summary>
    /// Checks if a controllable animal is in a den at the specified position.
    /// </summary>
    public static bool IsControllableAnimalInDen(Animal animal)
    {
        if (animal == null || !animal.IsControllable)
        {
            return false;
        }
        
        if (InteractableManager.Instance == null)
        {
            return false;
        }
        
        Den den = InteractableManager.Instance.GetDenAtPosition(animal.GridPosition);
        return den != null && den.IsAnimalInDen(animal);
    }
}

