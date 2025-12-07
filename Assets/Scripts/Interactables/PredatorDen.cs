using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A den interactable that defines a territory for predators.
/// Predators attached to this den will wander within a radius of it.
/// </summary>
public class PredatorDen : Interactable
{
    [Header("Predator Den Settings")]
    [Tooltip("Radius in grid cells that predators attached to this den will wander within")]
    [SerializeField] private int _territoryRadius = 5;
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    
    private string _predatorType = null;
    private readonly List<PredatorAnimal> _attachedPredators = new List<PredatorAnimal>();
    private int _attachedPredatorCount = 0;
    
    public int TerritoryRadius => _territoryRadius;
    public string PredatorType => _predatorType;
    public int AttachedPredatorCount => _attachedPredatorCount;
    
    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    
    public override string GetKnowledgeTitle() {
      return "PredatorDen";
    }
    
    /// <summary>
    /// Initializes the predator den at the specified grid position.
    /// </summary>
    public override void Initialize(Vector2Int gridPosition)
    {
        Initialize(gridPosition, null);
    }
    
    /// <summary>
    /// Initializes the predator den at the specified grid position with a specific predator type.
    /// </summary>
    /// <param name="gridPosition">Grid position for the den</param>
    /// <param name="predatorType">Type of predator this den is for (e.g., "Wolf", "Hawk")</param>
    public virtual void Initialize(Vector2Int gridPosition, string predatorType)
    {
        _gridPosition = gridPosition;
        _predatorType = predatorType;
        
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
    /// Sets the sprite for this predator den. Called by predators when they associate with the den.
    /// </summary>
    /// <param name="sprite">The sprite to display for this den</param>
    public void SetSprite(Sprite sprite)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.enabled = sprite != null;
        }
    }
    
    /// <summary>
    /// Gets a random position within the territory radius of this den.
    /// </summary>
    /// <returns>A random walkable position within territory, or null if none found</returns>
    public Vector2Int? GetRandomPositionInTerritory()
    {
        if (EnvironmentManager.Instance == null)
        {
            return null;
        }
        
        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        
        // Try to find a random position within territory
        int maxAttempts = 50;
        for (int attempts = 0; attempts < maxAttempts; attempts++)
        {
            // Pick a random direction and distance within radius
            int distance = Random.Range(0, _territoryRadius + 1);
            int angle = Random.Range(0, 360);
            
            // Convert angle to direction
            float radians = angle * Mathf.Deg2Rad;
            int dx = Mathf.RoundToInt(Mathf.Cos(radians) * distance);
            int dy = Mathf.RoundToInt(Mathf.Sin(radians) * distance);
            
            Vector2Int targetPos = _gridPosition + new Vector2Int(dx, dy);
            
            // Clamp to grid bounds
            targetPos.x = Mathf.Clamp(targetPos.x, 0, gridSize.x - 1);
            targetPos.y = Mathf.Clamp(targetPos.y, 0, gridSize.y - 1);
            
            // Check if this position is valid and walkable
            if (EnvironmentManager.Instance.IsValidPosition(targetPos) && 
                EnvironmentManager.Instance.IsWalkable(targetPos))
            {
                return targetPos;
            }
        }
        
        // If we couldn't find a good position, try completely random positions within radius
        for (int attempts = 0; attempts < 30; attempts++)
        {
            int dx = Random.Range(-_territoryRadius, _territoryRadius + 1);
            int dy = Random.Range(-_territoryRadius, _territoryRadius + 1);
            
            Vector2Int targetPos = _gridPosition + new Vector2Int(dx, dy);
            
            // Clamp to grid bounds
            targetPos.x = Mathf.Clamp(targetPos.x, 0, gridSize.x - 1);
            targetPos.y = Mathf.Clamp(targetPos.y, 0, gridSize.y - 1);
            
            // Check distance is within radius (Manhattan distance)
            int manhattanDistance = Mathf.Abs(targetPos.x - _gridPosition.x) + Mathf.Abs(targetPos.y - _gridPosition.y);
            if (manhattanDistance > _territoryRadius)
            {
                continue;
            }
            
            if (EnvironmentManager.Instance.IsValidPosition(targetPos) && 
                EnvironmentManager.Instance.IsWalkable(targetPos))
            {
                return targetPos;
            }
        }
        
        // If all attempts failed, return null (will try again next turn)
        return null;
    }
    
    /// <summary>
    /// Checks if a position is within the territory radius of this den.
    /// </summary>
    public bool IsPositionInTerritory(Vector2Int position)
    {
        int manhattanDistance = Mathf.Abs(position.x - _gridPosition.x) + Mathf.Abs(position.y - _gridPosition.y);
        return manhattanDistance <= _territoryRadius;
    }

    /// <summary>
    /// Tracks a predator attaching to this den so we can cache how many remain alive.
    /// </summary>
    public void OnPredatorAttached(PredatorAnimal predator)
    {
        if (predator == null)
        {
            return;
        }

        if (_attachedPredators.Contains(predator))
        {
            return;
        }

        _attachedPredators.Add(predator);
        _attachedPredatorCount++;
    }

    /// <summary>
    /// Tracks a predator detaching from this den.
    /// </summary>
    public void OnPredatorDetached(PredatorAnimal predator)
    {
        if (predator == null)
        {
            return;
        }

        if (_attachedPredators.Remove(predator))
        {
            _attachedPredatorCount = Mathf.Max(0, _attachedPredatorCount - 1);
        }
    }

    /// <summary>
    /// Removes destroyed predators from the cached list and updates the count.
    /// </summary>
    protected void CleanupAttachedPredators()
    {
        int removedCount = _attachedPredators.RemoveAll(predator => predator == null);
        if (removedCount > 0)
        {
            _attachedPredatorCount = Mathf.Max(0, _attachedPredatorCount - removedCount);
        }
    }

    /// <summary>
    /// Attempts to spawn a new predator for this den.
    /// </summary>
    protected PredatorAnimal TrySpawnPredator(int count = 1)
    {
        if (string.IsNullOrEmpty(_predatorType))
        {
            Debug.LogWarning("PredatorDen: Cannot spawn predator - predator type is not set.");
            return null;
        }

        if (AnimalManager.Instance == null)
        {
            Debug.LogWarning("PredatorDen: AnimalManager instance not found. Cannot spawn predator.");
            return null;
        }

        if (EnvironmentManager.Instance == null)
        {
            Debug.LogWarning("PredatorDen: EnvironmentManager instance not found. Cannot spawn predator.");
            return null;
        }

        if (!EnvironmentManager.Instance.IsValidPosition(_gridPosition))
        {
            Debug.LogWarning($"PredatorDen: Grid position {_gridPosition} is not valid for spawning predators.");
            return null;
        }

        Animal spawned = AnimalManager.Instance.SpawnAnimal(_predatorType, _gridPosition, count);
        if (spawned is PredatorAnimal predator)
        {
            predator.SetPredatorDen(this);
            Debug.Log($"PredatorDen: Spawned '{_predatorType}' at ({_gridPosition.x}, {_gridPosition.y}) to maintain population.");
            return predator;
        }

        Debug.LogWarning($"PredatorDen: Spawned animal for '{_predatorType}' is not a PredatorAnimal.");
        return null;
    }

    /// <summary>
    /// Ensures that at least one predator is attached to this den.
    /// </summary>
    protected void EnsurePredatorPopulation()
    {
        if (string.IsNullOrEmpty(_predatorType))
        {
            return;
        }

        CleanupAttachedPredators();

        if (_attachedPredatorCount > 0)
        {
            return;
        }

        TrySpawnPredator();
    }
}

