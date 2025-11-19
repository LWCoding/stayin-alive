using UnityEngine;

/// <summary>
/// A worker animal that extends PreyAnimal with built-in resource-carrying capabilities.
/// Workers can carry food and deposit it at their home den to increase the player's food-in-den amount.
/// This replaces the need for a separate WorkerResourceCarrier component.
/// </summary>
public class WorkerAnimal : PreyAnimal
{
    private int _currentCarriedFood = 0;

    private void OnEnable()
    {
        // Subscribe to food consumption events to track carried food
        if (this != null)
        {
            OnFoodConsumed += OnAnimalFoodConsumed;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from food consumption events
        if (this != null)
        {
            OnFoodConsumed -= OnAnimalFoodConsumed;
        }
    }

    private void Update()
    {
        TryDepositAtHome();
    }

    private void OnAnimalFoodConsumed(int hungerRestored)
    {
        // Each time food is consumed, increment carried food by 1
        _currentCarriedFood += 1;
    }

    private void TryDepositAtHome()
    {
        if (_currentCarriedFood <= 0)
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

        DepositCarriedFood(homeDen);
    }

    private void DepositCarriedFood(Den den)
    {
        if (_currentCarriedFood <= 0)
        {
            return;
        }

        int totalFood = _currentCarriedFood;

        if (DenSystemManager.Instance != null)
        {
            DenSystemManager.Instance.AddFoodToDen(totalFood);
        }

        _currentCarriedFood = 0;
    }
}

