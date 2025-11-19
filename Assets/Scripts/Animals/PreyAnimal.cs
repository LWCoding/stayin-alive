using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// A prey animal that automatically moves and flees from predators when detected.
/// </summary>
public class PreyAnimal : Animal
{
    [Header("Prey Settings")]
    [Tooltip("Detection radius in grid cells. Predators within this distance will cause the prey to flee.")]
    [SerializeField] private int _detectionRadius = 5;
    [Tooltip("Distance to flee from predators. The prey will try to move this many cells away from detected predators.")]
    [SerializeField] private int _fleeDistance = 3;
    
    [Header("Food Settings")]
    [Tooltip("Detection radius for finding food. Only food within this distance will be considered.")]
    [SerializeField] private int _grassDetectionRadius = 10;
    [Tooltip("Target food type name. This is used for identification purposes.")]
    [SerializeField] private string _targetFood = "Grass";

    protected Vector2Int? _wanderingDestination = null;
    private Vector2Int? _fleeDestination = null;
    private int _turnCounter = 0;

    protected bool HasHomeHideable => HomeHideable != null;
    protected bool IsAtHomeHideable => HasHomeHideable && GridPosition == HomeHideable.GridPosition;
    protected bool IsHidingInHome => HasHomeHideable && ReferenceEquals(CurrentHideable, HomeHideable);
    
    /// <summary>
    /// Gets whether this prey animal is at critical hunger (hunger below critical threshold).
    /// At critical hunger, the animal will seek food regardless of predators.
    /// Override in subclasses to provide specific critical hunger logic.
    /// </summary>
    protected virtual bool IsCriticallyHungry
    {
        get
        {
            if (AnimalData == null)
            {
                return false;
            }
            return CurrentHunger < AnimalData.criticalHungerThreshold;
        }
    }

    public override void TakeTurn()
    {
        _turnCounter++;
        
        // Only move every other turn (on even turns: 2, 4, 6, etc.)
        bool shouldMove = (_turnCounter % 2 == 0);
        
        // Check if we detect any predators within radius
        PredatorAnimal nearestPredator = FindNearestPredator();
        
        if (nearestPredator != null)
        {
            // If we detect a predator, cancel wandering and flee (only if it's a move turn)
            if (shouldMove)
            {
                _wanderingDestination = null;
                FleeFromPredator(nearestPredator);
            }
        }
        else
        {
            // No predators detected - wander
            if (!_wanderingDestination.HasValue || GridPosition == _wanderingDestination.Value)
            {
                // Need a new wandering destination
                _wanderingDestination = ChooseWanderingDestination();
            }
            
            if (_wanderingDestination.HasValue && shouldMove)
            {
                // Check if we detect predators while wandering - if so, cancel wandering
                PredatorAnimal detectedPredator = FindNearestPredator();
                if (detectedPredator != null)
                {
                    _wanderingDestination = null;
                    FleeFromPredator(detectedPredator);
                }
                else
                {
                    // Continue wandering
                    if (!MoveOneStepTowards(_wanderingDestination.Value))
                    {
                        // If move failed, try to find a new destination and move immediately
                        _wanderingDestination = ChooseWanderingDestination();
                        if (_wanderingDestination.HasValue)
                        {
                            MoveOneStepTowards(_wanderingDestination.Value);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds the nearest predator within detection radius.
    /// </summary>
    protected virtual PredatorAnimal FindNearestPredator()
    {
        if (AnimalManager.Instance == null)
        {
            return null;
        }

        List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
        PredatorAnimal nearest = null;
        int bestDistance = int.MaxValue;
        Vector2Int myPos = GridPosition;

        for (int i = 0; i < animals.Count; i++)
        {
            Animal other = animals[i];
            if (other == null || other == this)
            {
                continue;
            }

            // Only consider predators
            if (!(other is PredatorAnimal))
            {
                continue;
            }

            PredatorAnimal predator = other as PredatorAnimal;
            if (predator == null)
            {
                continue;
            }

            Vector2Int predatorPos = predator.GridPosition;
            int distance = Mathf.Abs(predatorPos.x - myPos.x) + Mathf.Abs(predatorPos.y - myPos.y); // Manhattan distance
            
            // Only consider predators within detection radius
            if (distance <= _detectionRadius && distance < bestDistance)
            {
                bestDistance = distance;
                nearest = predator;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Calculates a flee destination away from the predator and moves one step towards it.
    /// </summary>
    protected void FleeFromPredator(PredatorAnimal predator)
    {
        if (EnvironmentManager.Instance == null)
        {
            return;
        }

        Vector2Int myPos = GridPosition;
        Vector2Int predatorPos = predator.GridPosition;

        // Calculate direction away from predator
        Vector2Int direction = myPos - predatorPos;
        
        // Normalize to get a unit direction (but keep it as integers)
        if (direction.x != 0 || direction.y != 0)
        {
            // Normalize the direction vector
            float magnitude = Mathf.Sqrt(direction.x * direction.x + direction.y * direction.y);
            if (magnitude > 0)
            {
                direction.x = Mathf.RoundToInt((direction.x / magnitude) * _fleeDistance);
                direction.y = Mathf.RoundToInt((direction.y / magnitude) * _fleeDistance);
            }
        }
        else
        {
            // If we're on the same tile, pick a random direction
            int angle = Random.Range(0, 360);
            float radians = angle * Mathf.Deg2Rad;
            direction.x = Mathf.RoundToInt(Mathf.Cos(radians) * _fleeDistance);
            direction.y = Mathf.RoundToInt(Mathf.Sin(radians) * _fleeDistance);
        }

        // Calculate target position
        Vector2Int targetPos = myPos + direction;
        
        // Clamp to grid bounds
        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        targetPos.x = Mathf.Clamp(targetPos.x, 0, gridSize.x - 1);
        targetPos.y = Mathf.Clamp(targetPos.y, 0, gridSize.y - 1);

        // Try to find a valid walkable position near the target
        Vector2Int? validFleePos = FindValidFleePosition(targetPos, myPos);
        if (validFleePos.HasValue)
        {
            _fleeDestination = validFleePos.Value;
            if (MoveOneStepTowards(validFleePos.Value))
            {
                return;
            }
        }

        // If we can't find a good flee position, try to move in any direction away
        // Try the 4 cardinal directions away from the predator
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1)   // Down
        };

        // Prefer directions that move away from the predator
        System.Array.Sort(directions, (a, b) =>
        {
            Vector2Int posA = myPos + a;
            Vector2Int posB = myPos + b;
            int distA = Mathf.Abs(posA.x - predatorPos.x) + Mathf.Abs(posA.y - predatorPos.y);
            int distB = Mathf.Abs(posB.x - predatorPos.x) + Mathf.Abs(posB.y - predatorPos.y);
            return distB.CompareTo(distA); // Sort descending (farther is better)
        });

        foreach (Vector2Int dir in directions)
        {
            Vector2Int testPos = myPos + dir;
            if (IsTileTraversable(testPos))
            {
                _fleeDestination = testPos;
                if (MoveOneStepTowards(testPos))
                {
                    return;
                }
            }
        }

        // If all moves failed, try to recalculate a new flee destination
        // Try a different angle/distance
        for (int attempts = 0; attempts < 5; attempts++)
        {
            int angle = Random.Range(0, 360);
            float radians = angle * Mathf.Deg2Rad;
            Vector2Int newDirection = new Vector2Int(
                Mathf.RoundToInt(Mathf.Cos(radians) * _fleeDistance),
                Mathf.RoundToInt(Mathf.Sin(radians) * _fleeDistance)
            );
            Vector2Int newTargetPos = myPos + newDirection;
            
            newTargetPos.x = Mathf.Clamp(newTargetPos.x, 0, gridSize.x - 1);
            newTargetPos.y = Mathf.Clamp(newTargetPos.y, 0, gridSize.y - 1);
            
            Vector2Int? newFleePos = FindValidFleePosition(newTargetPos, myPos);
            if (newFleePos.HasValue)
            {
                _fleeDestination = newFleePos.Value;
                if (MoveOneStepTowards(newFleePos.Value))
                {
                    return;
                }
            }
        }
        
        _fleeDestination = null;
    }

    /// <summary>
    /// Finds a valid walkable position near the target flee position.
    /// </summary>
    private Vector2Int? FindValidFleePosition(Vector2Int targetPos, Vector2Int currentPos)
    {
        if (EnvironmentManager.Instance == null)
        {
            return null;
        }

        // First check if the target position itself is valid
        if (IsTileTraversable(targetPos))
        {
            return targetPos;
        }

        // Try positions in a spiral pattern around the target
        int maxRadius = 3;
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // Only check positions on the edge of the current radius
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                    {
                        continue;
                    }

                    Vector2Int testPos = targetPos + new Vector2Int(dx, dy);
                    
                    // Skip if it's our current position
                    if (testPos == currentPos)
                    {
                        continue;
                    }

                    if (IsTileTraversable(testPos))
                    {
                        return testPos;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Moves one step towards the destination using pathfinding.
    /// </summary>
    protected bool MoveOneStepTowards(Vector2Int destinationGrid)
    {
        if (EnvironmentManager.Instance == null || AstarPath.active == null)
        {
            return false;
        }

        Vector2Int startGrid = GridPosition;
        Vector2Int destGrid = destinationGrid;

        Vector3 startWorld = EnvironmentManager.Instance.GridToWorldPosition(startGrid);
        Vector3 destWorld = EnvironmentManager.Instance.GridToWorldPosition(destGrid);

        var path = ABPath.Construct(startWorld, destWorld, null);
        // Set custom traversal provider to filter water tiles based on animal's water walkability
        path.traversalProvider = new WaterTraversalProvider(CanGoOnWater);
        AstarPath.StartPath(path, true);
        path.BlockUntilCalculated();

        if (path.error || path.vectorPath == null || path.vectorPath.Count == 0)
        {
            return false;
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
            return false;
        }

        Vector2Int nextGrid = axisAlignedGrid[1];
        
        // Check if the next position is valid and walkable
        if (IsTileTraversable(nextGrid))
        {
            // Check if there's another animal at this position
            // Prey animals should not move onto other animals (no interactions between prey)
            if (AnimalManager.Instance != null && 
                AnimalManager.Instance.HasOtherAnimalAtPosition(this, nextGrid))
            {
                return false; // Collision detected, treat as failed move
            }
            
            SetGridPosition(nextGrid);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Chooses a random wandering destination.
    /// </summary>
    protected virtual Vector2Int? ChooseWanderingDestination()
    {
        if (EnvironmentManager.Instance == null)
        {
            return null;
        }

        Vector2Int myPos = GridPosition;
        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        
        // Try to find a random position around the prey
        int maxWanderDistance = 6; // Maximum distance to wander
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
            
            // Check if this position is valid and walkable
            if (IsTileTraversable(targetPos))
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
            
            if (IsTileTraversable(targetPos))
            {
                return targetPos;
            }
        }
        
        // If all attempts failed, return null (will try again next turn)
        return null;
    }

    /// <summary>
    /// Draws gizmos when the object is selected in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (EnvironmentManager.Instance == null)
        {
            return;
        }

        Vector3 currentWorldPos = EnvironmentManager.Instance.GridToWorldPosition(GridPosition);
        
        // Draw destination point and line
        Vector2Int? destination = null;
        Color gizmoColor = Color.green;
        
        if (_fleeDestination.HasValue)
        {
            destination = _fleeDestination;
            gizmoColor = Color.red; // Red for fleeing
        }
        else if (_wanderingDestination.HasValue)
        {
            destination = _wanderingDestination;
            gizmoColor = Color.cyan; // Cyan for wandering
        }
        
        if (destination.HasValue)
        {
            Vector3 destWorldPos = EnvironmentManager.Instance.GridToWorldPosition(destination.Value);
            
            // Draw line from current position to destination
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(currentWorldPos, destWorldPos);
            
            // Draw destination point
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(destWorldPos, 0.3f);
        }
    }

    /// <summary>
    /// Determines if a tile can be traversed by this prey, factoring in water walkability.
    /// </summary>
    protected bool IsTileTraversable(Vector2Int position)
    {
        if (EnvironmentManager.Instance == null)
        {
            return false;
        }

        if (!EnvironmentManager.Instance.IsValidPosition(position))
        {
            return false;
        }

        if (!EnvironmentManager.Instance.IsWalkable(position))
        {
            return false;
        }

        if (!CanGoOnWater)
        {
            TileType tileType = EnvironmentManager.Instance.GetTileType(position);
            if (tileType == TileType.Water)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Assigns the hideable home this prey can use when hiding.
    /// Includes prey-specific logic to force exit from current home if changing.
    /// </summary>
    protected void SetHomeHideable(IHideable hideable)
    {
        if (ReferenceEquals(HomeHideable, hideable))
        {
            return;
        }

        if (!ReferenceEquals(hideable, HomeHideable) && IsHidingInHome)
        {
            ForceExitFromHome();
        }

        base.SetHome(hideable);
    }

    /// <summary>
    /// Sets a new home for this prey animal (overrides base to include prey-specific exit logic).
    /// Used when dynamically assigning homes, such as when a worker is assigned to a den.
    /// </summary>
    public new void SetHome(IHideable hideable)
    {
        SetHomeHideable(hideable);
    }

    /// <summary>
    /// Clears the home reference for this prey animal (overrides base to include prey-specific exit logic).
    /// Used when unassigning a prey from its home location.
    /// </summary>
    public new void ClearHome()
    {
        SetHomeHideable(null);
    }

    /// <summary>
    /// Attempts to hide in the assigned hideable home when on its grid position.
    /// </summary>
    protected bool TryHideInHome()
    {
        if (!HasHomeHideable)
        {
            return false;
        }

        if (IsHidingInHome)
        {
            return true;
        }

        if (!IsAtHomeHideable)
        {
            return false;
        }

        HomeHideable.OnAnimalEnter(this);
        return true;
    }

    /// <summary>
    /// Forces the prey to exit its hideable home immediately.
    /// </summary>
    protected void ForceExitFromHome()
    {
        if (!IsHidingInHome)
        {
            return;
        }

        HomeHideable.OnAnimalLeave(this);
        SetVisualVisibility(true);
        SetCurrentHideable(null);
    }

    /// <summary>
    /// Tries to flee toward the hideable home. Falls back to normal fleeing if unreachable.
    /// </summary>
    protected void TryFleeToHome(PredatorAnimal predator)
    {
        if (predator == null)
        {
            return;
        }

        if (!HasHomeHideable)
        {
            FleeFromPredator(predator);
            return;
        }

        Vector2Int homePos = HomeHideable.GridPosition;

        if (IsAtHomeHideable)
        {
            TryHideInHome();
            return;
        }

        if (MoveOneStepTowards(homePos))
        {
            if (IsAtHomeHideable)
            {
                TryHideInHome();
            }
            return;
        }

        FleeFromPredator(predator);
    }
    
    /// <summary>
    /// Finds the nearest fully grown grass within detection radius.
    /// If critically hungry, searches with infinite range (ignores detection radius).
    /// </summary>
    protected virtual Vector2Int? FindNearestFullyGrownGrass()
    {
        if (InteractableManager.Instance == null)
        {
            return null;
        }

        List<Grass> allGrasses = InteractableManager.Instance.Grasses;
        if (allGrasses == null || allGrasses.Count == 0)
        {
            return null;
        }

        Vector2Int myPos = GridPosition;
        Grass nearest = null;
        int bestDistance = int.MaxValue;
        
        // Check if at critical hunger - if so, ignore detection radius (infinite range)
        bool isCriticallyHungry = IsCriticallyHungry;

        for (int i = 0; i < allGrasses.Count; i++)
        {
            Grass grass = allGrasses[i];
            if (grass == null)
            {
                continue;
            }

            // Only consider grass that matches the target food type
            if (!DoesGrassMatchTargetFood(grass))
            {
                continue;
            }

            // Only consider fully grown grass
            if (!grass.IsFullyGrown())
            {
                continue;
            }

            Vector2Int grassPos = grass.GridPosition;
            int distance = Mathf.Abs(grassPos.x - myPos.x) + Mathf.Abs(grassPos.y - myPos.y); // Manhattan distance
            
            // If critically hungry, ignore detection radius. Otherwise, only consider grass within detection radius
            if (isCriticallyHungry)
            {
                // Infinite range when critically hungry - just find the nearest
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = grass;
                }
            }
            else
            {
                // Normal behavior - only consider grass within detection radius
                if (distance <= _grassDetectionRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = grass;
                }
            }
        }

        return nearest != null ? nearest.GridPosition : (Vector2Int?)null;
    }
    
    /// <summary>
    /// Checks if a grass interactable matches the target food type.
    /// Override in subclasses to provide custom food matching logic.
    /// </summary>
    protected virtual bool DoesGrassMatchTargetFood(Grass grass)
    {
        if (grass == null || string.IsNullOrEmpty(_targetFood))
        {
            return false;
        }

        // Check if the grass type name matches the target food
        string grassTypeName = grass.GetType().Name;
        if (grassTypeName.Equals(_targetFood, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Also check if the GameObject name contains the target food name
        if (grass.gameObject.name.Contains(_targetFood))
        {
            return true;
        }

        return false;
    }
}

