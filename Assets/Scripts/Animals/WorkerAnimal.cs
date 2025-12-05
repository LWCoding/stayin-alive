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
    }

    private void OnDisable()
    {
        // Unsubscribe from food consumption events
        if (this != null)
        {
            // OnFoodConsumed -= OnAnimalFoodConsumed;
        }
    }

    private void Update()
    {
        TryDepositAtHome();
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
                Item duplicatedItem = ItemManager.Instance.CreateItemForStorage(workerItems[i].ItemType);
                DenSystemManager.Instance.AddItemToDenInventory(duplicatedItem);
                foodCount++;
              }
              
            }
            
            else {
              otherItemCount++;
            }
            
            DenSystemManager.Instance.AddItemToDenInventory(workerItems[i]);
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

