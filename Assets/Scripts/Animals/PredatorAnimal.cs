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
    
    [Header("Visual Indicators")]
    [Tooltip("Visual indicator shown above the predator's head when tracking/hunting prey.")]
    [SerializeField] private GameObject _trackingIndicator;
    
    [Header("Den Visuals")]
    [Tooltip("Sprite to set on the predator den when this predator associates with it.")]
    [SerializeField] private Sprite _denSprite;

    protected int _stallTurnsRemaining = 0;
    protected Vector2Int? _wanderingDestination = null;
    protected Vector2Int? _huntingDestination = null;
    protected PredatorDen _predatorDen = null;
    protected bool _isEatingStallActive = false;
    private ControllableAnimal _controllableTargetInSight = null;

    /// <summary>
    /// Gets the priority level of this predator.
    /// </summary>
    public int Priority => _priority;
    protected int DetectionRadius => _detectionRadius;
    protected bool IsEatingStallActive => _isEatingStallActive;

    private void Awake()
    {
        // Hide tracking indicator by default
        UpdateTrackingIndicator();
    }
    
    private void Start()
    {
        // Find the nearest predator den to this predator's spawn position
        FindNearestPredatorDen();
    }
    
    /// <summary>
    /// Finds the nearest predator den to this predator that matches its type and associates with it.
    /// </summary>
    private void FindNearestPredatorDen()
    {
        if (InteractableManager.Instance == null)
        {
            return;
        }
        
        // Get this predator's type name
        string myPredatorType = null;
        if (AnimalData != null && !string.IsNullOrEmpty(AnimalData.animalName))
        {
            myPredatorType = AnimalData.animalName;
        }
        
        if (string.IsNullOrEmpty(myPredatorType))
        {
            Debug.LogWarning($"PredatorAnimal '{name}': Cannot find predator type (AnimalData is null or animalName is empty)");
            return;
        }
        
        List<PredatorDen> allDens = InteractableManager.Instance.GetAllPredatorDens();
        if (allDens == null || allDens.Count == 0)
        {
            return;
        }
        
        Vector2Int myPos = GridPosition;
        PredatorDen nearest = null;
        int bestDistance = int.MaxValue;
        
        foreach (PredatorDen den in allDens)
        {
            if (den == null)
            {
                continue;
            }
            
            // Only consider dens that match this predator's type
            if (den.PredatorType != myPredatorType)
            {
                continue;
            }
            
            Vector2Int denPos = den.GridPosition;
            int distance = Mathf.Abs(denPos.x - myPos.x) + Mathf.Abs(denPos.y - myPos.y);
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = den;
            }
        }
        
        if (nearest != null)
        {
            _predatorDen = nearest;
            
            // Set the den's sprite if we have one
            if (_denSprite != null)
            {
                nearest.SetSprite(_denSprite);
            }
            
            Debug.Log($"PredatorAnimal '{name}' (type: {myPredatorType}) associated with predator den at ({nearest.GridPosition.x}, {nearest.GridPosition.y})");
        }
        else
        {
            Debug.LogWarning($"PredatorAnimal '{name}' (type: {myPredatorType}): No matching predator den found!");
        }
    }
    
    /// <summary>
    /// Sets the predator den for this predator. Used when spawning predators near specific dens.
    /// Only sets the den if it matches this predator's type.
    /// </summary>
    public void SetPredatorDen(PredatorDen predatorDen)
    {
        if (predatorDen == null)
        {
            _predatorDen = null;
            return;
        }
        
        // Verify the den type matches this predator's type
        string myPredatorType = null;
        if (AnimalData != null && !string.IsNullOrEmpty(AnimalData.animalName))
        {
            myPredatorType = AnimalData.animalName;
        }
        
        if (string.IsNullOrEmpty(myPredatorType))
        {
            Debug.LogWarning($"PredatorAnimal '{name}': Cannot set predator den (AnimalData is null or animalName is empty)");
            return;
        }
        
        if (predatorDen.PredatorType != myPredatorType)
        {
            Debug.LogWarning($"PredatorAnimal '{name}' (type: {myPredatorType}): Cannot associate with den of type '{predatorDen.PredatorType}'");
            return;
        }
        
        _predatorDen = predatorDen;
        
        // Set the den's sprite if we have one
        if (_denSprite != null)
        {
            predatorDen.SetSprite(_denSprite);
        }
    }

    public override void TakeTurn()
    {
        // If stalled, skip this turn and decrement stall counter
        if (_stallTurnsRemaining > 0)
        {
            _stallTurnsRemaining--;
            if (_stallTurnsRemaining == 0)
            {
                _isEatingStallActive = false;
            }
            // Hide tracking indicator while stalled
            UpdateTrackingIndicator();
            return;
        }

        if (TryPerformSpecialTurnAction())
        {
            UpdateTrackingIndicator();
            return;
        }

        // Check if we detect any prey within radius
        Vector2Int? preyGrid = FindNearestPreyGrid();
        
        if (preyGrid.HasValue)
        {
            // If we detect prey, cancel wandering and hunt
            _wanderingDestination = null;
            _huntingDestination = preyGrid.Value;
            if (!MoveOneStepTowards(preyGrid.Value))
            {
                // If move failed, try to find a new prey or recalculate path
                Vector2Int? newPreyGrid = FindNearestPreyGrid();
                if (newPreyGrid.HasValue)
                {
                    _huntingDestination = newPreyGrid.Value;
                    MoveOneStepTowards(newPreyGrid.Value);
                }
                else
                {
                    // No prey found, return to territory and start wandering
                    _huntingDestination = null;
                    _wanderingDestination = ChooseWanderingDestination();
                    if (_wanderingDestination.HasValue)
                    {
                        MoveOneStepTowards(_wanderingDestination.Value);
                    }
                }
            }
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
                    _huntingDestination = detectedPrey.Value;
                    if (!MoveOneStepTowards(detectedPrey.Value))
                    {
                        // If move failed, try to find a new prey
                        Vector2Int? newPreyGrid = FindNearestPreyGrid();
                        if (newPreyGrid.HasValue)
                        {
                            _huntingDestination = newPreyGrid.Value;
                            MoveOneStepTowards(newPreyGrid.Value);
                        }
                        else
                        {
                            // Lost sight of prey, return to territory
                            _huntingDestination = null;
                            _wanderingDestination = ChooseWanderingDestination();
                            if (_wanderingDestination.HasValue)
                            {
                                MoveOneStepTowards(_wanderingDestination.Value);
                            }
                        }
                    }
                }
                else
                {
                    // Continue wandering
                    _huntingDestination = null;
                    bool moved = MoveOneStepTowards(_wanderingDestination.Value);
                    if (!moved)
                    {
                        // If move failed, try to find a new destination and move immediately
                        // Try multiple times to find a valid destination
                        for (int attempts = 0; attempts < 3; attempts++)
                        {
                            _wanderingDestination = ChooseWanderingDestination();
                            if (_wanderingDestination.HasValue)
                            {
                                if (MoveOneStepTowards(_wanderingDestination.Value))
                                {
                                    break; // Successfully moved
                                }
                            }
                        }
                    }
                }
            }
        }

        // Attempt to hunt if we share a tile with another animal after moving
        TryHuntAtCurrentPosition();
        
        // Allow subclasses to react after the standard predator logic completes
        OnStandardTurnComplete();
        
        // Update tracking indicator visibility
        UpdateTrackingIndicator();
    }

    /// <summary>
    /// Allows subclasses to perform a special action instead of the standard predator behavior.
    /// Return true to skip the default TakeTurn logic for this turn.
    /// </summary>
    protected virtual bool TryPerformSpecialTurnAction()
    {
        return false;
    }

    /// <summary>
    /// Called after the standard predator logic runs (before the tracking indicator updates).
    /// Subclasses can override to add custom behavior.
    /// </summary>
    protected virtual void OnStandardTurnComplete()
    {
    }

    /// <summary>
    /// Override to allow predators to move onto dens.
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
        
        // Check if the next position is valid and walkable (dens are allowed)
        if (EnvironmentManager.Instance.IsValidPosition(nextGrid) &&
            EnvironmentManager.Instance.IsWalkable(nextGrid))
        {
            // Check if there's another animal at this position
            // Predators can move onto valid hunt targets (prey or lower priority predators)
            // They can also move onto den tiles even when controllable animals are inside (but cannot hunt them)
            Animal animalAtPosition = GetAnimalAtPosition(nextGrid);
            if (animalAtPosition != null)
            {
                // Only allow movement if the animal is a valid hunt target
                // Note: Controllable animals in dens are still considered valid targets for movement,
                // but hunting is prevented in TryHuntAtCurrentPosition when on a den tile
                if (!IsValidTarget(animalAtPosition))
                {
                    return false; // Not a valid target, treat as failed move
                }
                // If it's a valid target, allow the move (hunting will happen in TryHuntAtCurrentPosition)
            }
            
            SetGridPosition(nextGrid);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the animal at the specified grid position, if any.
    /// </summary>
    protected Animal GetAnimalAtPosition(Vector2Int position)
    {
        if (AnimalManager.Instance == null)
        {
            return null;
        }

        List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
        for (int i = 0; i < animals.Count; i++)
        {
            Animal other = animals[i];
            if (other == null || other == this)
            {
                continue;
            }

            if (other.GridPosition == position)
            {
                return other;
            }
        }

        return null;
    }

    protected virtual Vector2Int? FindNearestPreyGrid()
    {
        if (AnimalManager.Instance == null)
        {
            UpdateChasingAudio(null);
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

            // Skip controllable animals that are in a den or bush
            if (other.IsControllable && (Den.IsControllableAnimalInDen(other) || Bush.IsControllableAnimalInBush(other)))
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

        UpdateChasingAudio(nearest);
        return nearest != null ? nearest.GridPosition : (Vector2Int?)null;
    }

    /// <summary>
    /// Determines if an animal is a valid target for this predator based on priority rules.
    /// - Non-predator animals (prey) are always valid targets
    /// - Predators with lower priority are valid targets
    /// - Predators with same or higher priority are ignored
    /// </summary>
    protected bool IsValidTarget(Animal target)
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

    private void UpdateChasingAudio(Animal detectedPrey)
    {
        ControllableAnimal controllable = detectedPrey as ControllableAnimal;

        if (controllable != null)
        {
            if (_controllableTargetInSight != controllable)
            {
                _controllableTargetInSight = controllable;
                PlayChasingAudio();
            }
        }
        else if (_controllableTargetInSight != null)
        {
            _controllableTargetInSight = null;
        }
    }

    private void PlayChasingAudio()
    {
        if (AnimalData == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.SFXType chasingType = AnimalData.chasingSFX;
        if (chasingType == AudioManager.SFXType.None)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(chasingType);
    }

    protected virtual Vector2Int? ChooseWanderingDestination()
    {
        if (EnvironmentManager.Instance == null)
        {
            return null;
        }

        // If we have a predator den, wander within its territory
        if (_predatorDen != null)
        {
            // Try multiple times to get a valid territory position that accounts for water walkability
            for (int attempts = 0; attempts < 10; attempts++)
            {
                Vector2Int? territoryPos = _predatorDen.GetRandomPositionInTerritory();
                if (territoryPos.HasValue && IsTileTraversable(territoryPos.Value))
                {
                    return territoryPos.Value;
                }
            }
        }
        
        // Fallback: wander randomly if no den is assigned or no valid territory position found
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
            
            // Check if this position is traversable (accounts for water walkability)
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

    protected virtual void TryHuntAtCurrentPosition()
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
                // Skip controllable animals that are in a den or bush (they are safe)
                if (other.IsControllable && (Den.IsControllableAnimalInDen(other) || Bush.IsControllableAnimalInBush(other)))
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
                _isEatingStallActive = _stallTurnsRemaining > 0;
                break;
            }
        }
    }

    /// <summary>
    /// Checks if a tile is traversable by this predator, accounting for water walkability.
    /// </summary>
    private bool IsTileTraversable(Vector2Int position)
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

        // Check if this is a water tile and if the predator can go on water
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
        Color gizmoColor = Color.yellow;
        
        if (_huntingDestination.HasValue)
        {
            destination = _huntingDestination;
            gizmoColor = Color.red; // Red for hunting
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
    /// Updates the visibility of the tracking indicator based on whether the predator is hunting and not stalled.
    /// </summary>
    protected void UpdateTrackingIndicator()
    {
        if (_trackingIndicator != null)
        {
            bool shouldShow = ShouldShowTrackingIndicator();
            _trackingIndicator.SetActive(shouldShow);
        }
    }

    protected virtual bool ShouldShowTrackingIndicator()
    {
        return _huntingDestination.HasValue && _stallTurnsRemaining == 0;
    }
}


