using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for animals that wander around randomly in a specific area.
/// Handles movement, turn-based updates, and sprite flipping.
/// </summary>
public abstract class WanderingAnimal : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum distance the animal can move in one turn (in world units)")]
    [SerializeField] protected float _maxMoveDistance = 2f;
    
    [Tooltip("Minimum distance the animal can move in one turn (in world units)")]
    [SerializeField] protected float _minMoveDistance = 0.5f;

    protected Bounds _movementBounds;
    protected Vector3 _targetPosition;
    protected Coroutine _moveCoroutine;
    protected bool _isMoving = false;
    protected Vector3 _centerPosition; // Center point for radius-based movement
    protected bool _centerPositionSet = false; // Track if center position has been explicitly set

    protected virtual void OnEnable()
    {
        // Subscribe to turn events so animal moves when animals move
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced += OnTurnAdvanced;
        }
    }

    protected virtual void OnDisable()
    {
        // Unsubscribe from turn events
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= OnTurnAdvanced;
        }
    }

    protected virtual void Start()
    {
        // Initialize movement bounds only if center position hasn't been explicitly set
        // (e.g., by SetMovementRadius for bees that should stay near a specific tree)
        if (!_centerPositionSet)
        {
            InitializeMovementBounds();
        }
        
        // Set initial target position
        _targetPosition = transform.position;
    }

    /// <summary>
    /// Initializes the movement bounds. Override in derived classes to set custom bounds.
    /// </summary>
    protected virtual void InitializeMovementBounds()
    {
        // Default: use entire grid bounds (excluding outer borders)
        if (EnvironmentManager.Instance == null)
        {
            return;
        }

        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        
        // Exclude outer borders (1 cell from each edge)
        Vector3 minBorder = EnvironmentManager.Instance.GridToWorldPosition(new Vector2Int(1, 1));
        Vector3 maxBorder = EnvironmentManager.Instance.GridToWorldPosition(new Vector2Int(gridSize.x - 2, gridSize.y - 2));
        
        Vector3 center = (minBorder + maxBorder) * 0.5f;
        Vector3 size = maxBorder - minBorder;
        _movementBounds = new Bounds(center, size);
        _centerPosition = center;
    }

    /// <summary>
    /// Sets a center position and radius for movement. Used for animals that should stay near a specific point.
    /// </summary>
    public virtual void SetMovementRadius(Vector3 center, float radius)
    {
        _centerPosition = center;
        _movementBounds = new Bounds(center, new Vector3(radius * 2, radius * 2, 0));
        _centerPositionSet = true; // Mark that center position has been explicitly set
    }

    /// <summary>
    /// Called when a turn advances. Moves the animal to a new random position.
    /// </summary>
    protected virtual void OnTurnAdvanced(int turnCount)
    {
        // Only move if not already moving
        if (!_isMoving)
        {
            ChooseNewTarget();
            StartMove();
        }
    }

    /// <summary>
    /// Chooses a new random target position within movement bounds.
    /// </summary>
    protected virtual void ChooseNewTarget()
    {
        // Choose a random direction
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float moveDistance = Random.Range(_minMoveDistance, _maxMoveDistance);
        
        Vector3 newTarget = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * moveDistance;
        
        // Clamp to movement bounds if they're set
        if (_movementBounds.size != Vector3.zero)
        {
            newTarget.x = Mathf.Clamp(newTarget.x, _movementBounds.min.x, _movementBounds.max.x);
            newTarget.y = Mathf.Clamp(newTarget.y, _movementBounds.min.y, _movementBounds.max.y);
        }
        
        // Ensure target is not on the border by checking grid position
        if (EnvironmentManager.Instance != null)
        {
            Vector2Int gridPos = EnvironmentManager.Instance.WorldToGridPosition(newTarget);
            Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
            
            // If on border, push inward
            if (gridPos.x <= 0 || gridPos.x >= gridSize.x - 1 || gridPos.y <= 0 || gridPos.y >= gridSize.y - 1)
            {
                // Clamp grid position to be at least 1 cell from each edge
                gridPos.x = Mathf.Clamp(gridPos.x, 1, gridSize.x - 2);
                gridPos.y = Mathf.Clamp(gridPos.y, 1, gridSize.y - 2);
                
                // Convert back to world position
                newTarget = EnvironmentManager.Instance.GridToWorldPosition(gridPos);
            }
        }
        
        _targetPosition = newTarget;
        
        // Update sprite facing direction based on movement
        UpdateFacingDirection(newTarget);
    }

    /// <summary>
    /// Updates the sprite facing direction based on horizontal movement.
    /// </summary>
    protected virtual void UpdateFacingDirection(Vector3 targetPosition)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            float horizontalMovement = targetPosition.x - transform.position.x;
            // Flip sprite: true = facing left, false = facing right
            spriteRenderer.flipX = horizontalMovement < 0;
        }
    }

    /// <summary>
    /// Starts moving to the target position.
    /// </summary>
    protected virtual void StartMove()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }
        _moveCoroutine = StartCoroutine(MoveToTargetCoroutine());
    }

    /// <summary>
    /// Coroutine that smoothly moves the animal to the target position.
    /// </summary>
    protected virtual IEnumerator MoveToTargetCoroutine()
    {
        _isMoving = true;
        Vector3 startPosition = transform.position;
        float duration = Globals.MoveDurationSeconds;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Use smooth interpolation
            transform.position = Vector3.Lerp(startPosition, _targetPosition, t);
            
            yield return null;
        }

        // Ensure we're exactly at the target
        transform.position = _targetPosition;
        _isMoving = false;
        _moveCoroutine = null;
    }
}
