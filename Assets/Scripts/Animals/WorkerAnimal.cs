using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A worker animal that extends PreyAnimal with built-in resource-carrying capabilities.
/// Workers can carry food and deposit it at their home den to increase the player's food-in-den amount.
/// This replaces the need for a separate WorkerResourceCarrier component.
/// </summary>
public class WorkerAnimal : PreyAnimal
{
    private int _currentCarriedItems => workerItems.Count;

    private List<Item> workerItems = new List<Item>();

    public List<Item> WorkerItemsCopy => workerItems.ToList();
    
    
    private void OnEnable()
    {
        // Subscribe to food consumption events to track carried food
        if (this != null)
        {
            // OnFoodConsumed += OnAnimalFoodConsumed;
        }
        
        // Subscribe to turn advancement to check if worker should deposit items
        SubscribeToTurnEvents();
    }

    private void OnDisable()
    {
        // Unsubscribe from food consumption events
        if (this != null)
        {
            // OnFoodConsumed -= OnAnimalFoodConsumed;
        }
        
        // Unsubscribe from turn events
        UnsubscribeFromTurnEvents();
    }

    private void SubscribeToTurnEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced += OnTurnAdvanced;
        }
    }

    private void UnsubscribeFromTurnEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= OnTurnAdvanced;
        }
    }

    private void OnTurnAdvanced(int turnCount)
    {
        // Check if worker should deposit items at home after turn advancement
        // This happens after player moves and all animals have taken their turn
        TryDepositAtHome();
    }
    
    public override void SetGridPosition(Vector2Int gridPosition)
    {
        Vector2Int previousPosition = GridPosition;
        base.SetGridPosition(gridPosition);
        
        // Check if worker arrived at home after position change
        // This handles cases where worker moves directly to home position
        if (previousPosition != gridPosition)
        {
            TryDepositAtHome();
        }
    }
    

    private void TryDepositAtHome()
    {
        if (_currentCarriedItems <= 0)
        {
            return;
        }

        Den homeDen = HomeHideable as Den;
        if (homeDen == null)
        {
            return;
        }

        bool isInHome = ReferenceEquals(CurrentHideable, homeDen) ||
                        GridPosition == homeDen.GridPosition;

        if (!isInHome)
        {
            return;
        }

        DepositCarriedItems(homeDen);
    }

    public void AddItem(Item item) {
      workerItems.Add(item);
    }

    private void DepositCarriedItems(Den den) {
      if (_currentCarriedItems <= 0) {
        return;
      }

      int totalItems = _currentCarriedItems;

      int foodCount = 0;
      int otherItemCount = 0;

      if (DenSystemManager.Instance != null) {
        for (int i = 0; i < totalItems; i++) {
          if (workerItems[i] != null) {
            if (workerItems[i] is FoodItem) {
              foodCount++;
              
              // Workers have a chance to deposit extra food based on number of assigned workers
              float rng = Random.Range(0f, 1f);
              if (rng <= WorkerManager.Instance.CurrentWorkerBonusFoodDropRate) {
                ItemId duplicatedItemId = workerItems[i].ItemId;
                GlobalInventoryManager.Instance.AddItemIdToDen(duplicatedItemId);
                foodCount++;
              }
              
            }
            
            else {
              otherItemCount++;
            }
            
            GlobalInventoryManager.Instance.AddItemIdToDen(workerItems[i].ItemId);
            Destroy(workerItems[i].gameObject);
          }
        }

        workerItems.Clear();

        if (this is KangRatWorker) {
          if (foodCount > 0 && otherItemCount > 0) {
            DenSystemManager.Instance.LogHolder.SpawnLog(LogEntryGuiController.DenLogType.ADD_FOOD_AND_OTHER_ITEM,
              foodCount, otherItemCount);
            return;
          }

          if (foodCount > 0) {
            DenSystemManager.Instance.LogHolder.SpawnLog(LogEntryGuiController.DenLogType.ADD_FOOD, foodCount);
            return;
          }

          if (otherItemCount > 0) {
            DenSystemManager.Instance.LogHolder.SpawnLog(LogEntryGuiController.DenLogType.ADD_OTHER_ITEM, otherItemCount);
            return;
          }
        }
      }
    }
}

