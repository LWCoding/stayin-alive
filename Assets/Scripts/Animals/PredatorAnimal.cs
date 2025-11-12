using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A predator animal that moves one step towards the nearest other animal each turn.
/// </summary>
public class PredatorAnimal : Animal
{
    private int _stallTurnsRemaining = 0;

    public override void TakeTurn()
    {
        // If stalled, skip this turn and decrement stall counter
        if (_stallTurnsRemaining > 0)
        {
            _stallTurnsRemaining--;
            Debug.Log($"Predator '{name}' is stalled. {_stallTurnsRemaining} turns remaining.");
            return;
        }

        Vector2Int? preyGrid = FindNearestPreyGrid();
        if (preyGrid.HasValue)
        {
            MoveOneStepTowards(preyGrid.Value);
        }

        // Attempt to hunt if we share a tile with another animal after moving
        TryHuntAtCurrentPosition();
    }

    private Vector2Int? FindNearestPreyGrid()
    {
        if (AnimalManager.Instance == null)
        {
            return null;
        }

        List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
        Animal nearest = null;
        int bestDistance = int.MaxValue;
        Vector2Int myPos = GridPosition;

        for (int i = 0; i < animals.Count; i++)
        {
            Animal other = animals[i];
            if (other == null || other == this)
            {
                continue;
            }

            Vector2Int otherPos = other.GridPosition;
            int distance = Mathf.Abs(otherPos.x - myPos.x) + Mathf.Abs(otherPos.y - myPos.y); // Manhattan distance
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = other;
            }
        }

        return nearest != null ? nearest.GridPosition : (Vector2Int?)null;
    }

    private void TryHuntAtCurrentPosition()
    {
        if (AnimalManager.Instance == null)
        {
            return;
        }

        var animals = AnimalManager.Instance.GetAllAnimals();
        for (int i = 0; i < animals.Count; i++)
        {
            var other = animals[i];
            if (other == null || other == this)
            {
                continue;
            }
            if (other.GridPosition == GridPosition)
            {
                // Reduce the prey's animal count by one
                other.ReduceAnimalCount();
                
                // Stall this predator for 2 turns
                _stallTurnsRemaining = 2;
                Debug.Log($"Predator '{name}' hunted '{other.name}'. Stalled for 2 turns.");
                break;
            }
        }
    }
}


