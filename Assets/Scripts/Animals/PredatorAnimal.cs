using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// A predator animal that moves one step towards the nearest other animal each turn.
/// </summary>
public class PredatorAnimal : Animal
{
    [Header("Predator Settings")]
    [Tooltip("Detection radius in grid cells. Only animals within this distance can be detected.")]
    [SerializeField] private int _detectionRadius = 5;
    [Tooltip("Priority level of this predator. Higher priority predators can hunt lower priority predators. Predators with the same priority ignore each other.")]
    [SerializeField] private int _priority = 0;

    [SerializeField] private int _stallTurnsAfterHunt = 5;

    private int _stallTurnsRemaining = 0;
    private Vector2Int? _wanderingDestination = null;

    /// <summary>
    /// Gets the priority level of this predator.
    /// </summary>
    public int Priority => _priority;

    public override void TakeTurn()
    {
        // If stalled, skip this turn and decrement stall counter
        if (_stallTurnsRemaining > 0)
        {
            _stallTurnsRemaining--;
            Debug.Log($"Predator '{name}' is stalled. {_stallTurnsRemaining} turns remaining.");
            return;
        }

        // Check if we detect any prey within radius
        Vector2Int? preyGrid = FindNearestPreyGrid();
        
        if (preyGrid.HasValue)
        {
            // If we detect prey, cancel wandering and hunt
            _wanderingDestination = null;
            MoveOneStepTowards(preyGrid.Value);
        }
        else
        {
            // No prey detected - wander
            if (!_wanderingDestination.HasValue || GridPosition == _wanderingDestination.Value)
            {
                // Need a new wandering destination
                _wanderingDestination = ChooseWanderingDestination();
            }
            
            if (_wanderingDestination.HasValue)
            {
                // Check if we detect prey while wandering - if so, cancel wandering
                Vector2Int? detectedPrey = FindNearestPreyGrid();
                if (detectedPrey.HasValue)
                {
                    _wanderingDestination = null;
                    MoveOneStepTowards(detectedPrey.Value);
                }
                else
                {
                    // Continue wandering
                    MoveOneStepTowards(_wanderingDestination.Value);
                }
            }
        }

        // Attempt to hunt if we share a tile with another animal after moving
        TryHuntAtCurrentPosition();
    }

    /// <summary>
    /// Override to allow predators to move onto dens.
    /// </summary>
    protected void MoveOneStepTowards(Vector2Int destinationGrid)
    {
        if (EnvironmentManager.Instance == null || AstarPath.active == null)
        {
            return;
        }

        Vector2Int startGrid = GridPosition;
        Vector2Int destGrid = destinationGrid;

        Vector3 startWorld = EnvironmentManager.Instance.GridToWorldPosition(startGrid);
        Vector3 destWorld = EnvironmentManager.Instance.GridToWorldPosition(destGrid);

        var path = ABPath.Construct(startWorld, destWorld, null);
        AstarPath.StartPath(path, true);
        path.BlockUntilCalculated();

        if (path.error || path.vectorPath == null || path.vectorPath.Count == 0)
        {
            return;
        }

        // Build axis-aligned path manually
        List<Vector2Int> axisAlignedGrid = new List<Vector2Int>();
        Vector2Int currentGrid = startGrid;
        axisAlignedGrid.Add(currentGrid);

        for (int i = 1; i < path.vectorPath.Count; i++)
        {
            Vector2Int targetGrid = EnvironmentManager.Instance.WorldToGridPosition(path.vectorPath[i]);
            if (targetGrid == currentGrid)
            {
                continue;
            }

            // Move one step at a time in axis-aligned directions
            while (currentGrid.x != targetGrid.x)
            {
                currentGrid.x += targetGrid.x > currentGrid.x ? 1 : -1;
                axisAlignedGrid.Add(currentGrid);
            }

            while (currentGrid.y != targetGrid.y)
            {
                currentGrid.y += targetGrid.y > currentGrid.y ? 1 : -1;
                axisAlignedGrid.Add(currentGrid);
            }
        }

        if (axisAlignedGrid.Count < 2)
        {
            return;
        }

        Vector2Int nextGrid = axisAlignedGrid[1];
        
        // Check if the next position is valid and walkable (dens are allowed)
        if (EnvironmentManager.Instance.IsValidPosition(nextGrid) &&
            EnvironmentManager.Instance.IsWalkable(nextGrid))
        {
            SetGridPosition(nextGrid);
        }
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

            // Skip controllable animals that are in a den
            if (other.IsControllable && Den.IsControllableAnimalInDen(other))
            {
                continue;
            }

            // Check if this animal is a valid target based on priority
            if (!IsValidTarget(other))
            {
                continue;
            }

            Vector2Int otherPos = other.GridPosition;
            int distance = Mathf.Abs(otherPos.x - myPos.x) + Mathf.Abs(otherPos.y - myPos.y); // Manhattan distance
            
            // Only consider animals within detection radius
            if (distance <= _detectionRadius && distance < bestDistance)
            {
                bestDistance = distance;
                nearest = other;
            }
        }

        return nearest != null ? nearest.GridPosition : (Vector2Int?)null;
    }

    /// <summary>
    /// Determines if an animal is a valid target for this predator based on priority rules.
    /// - Non-predator animals (prey) are always valid targets
    /// - Predators with lower priority are valid targets
    /// - Predators with same or higher priority are ignored
    /// </summary>
    private bool IsValidTarget(Animal target)
    {
        // Non-predator animals are always valid targets (prey)
        if (!(target is PredatorAnimal))
        {
            return true;
        }

        // For predators, check priority
        PredatorAnimal targetPredator = target as PredatorAnimal;
        if (targetPredator == null)
        {
            return true; // Safety check, shouldn't happen
        }

        // Only target predators with lower priority
        // Ignore predators with same or higher priority
        return targetPredator.Priority < _priority;
    }

    private Vector2Int? ChooseWanderingDestination()
    {
        if (EnvironmentManager.Instance == null)
        {
            return null;
        }

        Vector2Int myPos = GridPosition;
        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        
        // Try to find a random position around the predator
        int maxWanderDistance = 8; // Maximum distance to wander
        int minWanderDistance = 2; // Minimum distance to wander
        
        for (int attempts = 0; attempts < 30; attempts++)
        {
            // Pick a random direction and distance
            int distance = Random.Range(minWanderDistance, maxWanderDistance + 1);
            int angle = Random.Range(0, 360);
            
            // Convert angle to direction (approximate, using integer grid)
            float radians = angle * Mathf.Deg2Rad;
            int dx = Mathf.RoundToInt(Mathf.Cos(radians) * distance);
            int dy = Mathf.RoundToInt(Mathf.Sin(radians) * distance);
            
            Vector2Int targetPos = myPos + new Vector2Int(dx, dy);
            
            // Clamp to grid bounds
            targetPos.x = Mathf.Clamp(targetPos.x, 0, gridSize.x - 1);
            targetPos.y = Mathf.Clamp(targetPos.y, 0, gridSize.y - 1);
            
            // Check if this position is valid and walkable (dens are allowed)
            if (EnvironmentManager.Instance.IsValidPosition(targetPos) && 
                EnvironmentManager.Instance.IsWalkable(targetPos))
            {
                return targetPos;
            }
        }
        
        // If we couldn't find a good position, try completely random positions
        for (int attempts = 0; attempts < 20; attempts++)
        {
            Vector2Int targetPos = new Vector2Int(
                Random.Range(0, gridSize.x),
                Random.Range(0, gridSize.y)
            );
            
            if (EnvironmentManager.Instance.IsValidPosition(targetPos) && 
                EnvironmentManager.Instance.IsWalkable(targetPos))
            {
                return targetPos;
            }
        }
        
        // If all attempts failed, return null (will try again next turn)
        return null;
    }

    private void TryHuntAtCurrentPosition()
    {
        if (AnimalManager.Instance == null)
        {
            return;
        }

        // If the predator is on a den, it cannot hunt any animals
        if (Den.IsDenAtPosition(GridPosition))
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
                // Skip controllable animals that are in a den (they are safe)
                if (other.IsControllable && Den.IsControllableAnimalInDen(other))
                {
                    continue;
                }

                // Check if this animal is a valid target based on priority
                if (!IsValidTarget(other))
                {
                    continue;
                }
                
                // Reduce the prey's animal count by one
                other.ReduceAnimalCount();
                
                // Stall this predator for the configured number of turns
                _stallTurnsRemaining = Mathf.Max(0, _stallTurnsAfterHunt);
                break;
            }
        }
    }
}


