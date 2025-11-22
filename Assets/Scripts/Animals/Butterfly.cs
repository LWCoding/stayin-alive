using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Butterfly that flies around randomly. Only moves when other animals are moving (during turns).
/// Does not occupy a grid position and can move freely in world space.
/// </summary>
public class Butterfly : MonoBehaviour
{
    private static GameObject _butterflyPrefab;
    private static Transform _butterflyParent;
    [Header("Movement Settings")]
    [Tooltip("Maximum distance the butterfly can move in one turn (in world units)")]
    [SerializeField] private float _maxMoveDistance = 2f;
    
    [Tooltip("Minimum distance the butterfly can move in one turn (in world units)")]
    [SerializeField] private float _minMoveDistance = 0.5f;
    
    [Tooltip("Bounds for butterfly movement. If null, will use EnvironmentManager grid bounds")]
    [SerializeField] private Bounds _movementBounds;

    private Vector3 _targetPosition;
    private Coroutine _moveCoroutine;
    private bool _isMoving = false;

    private void OnEnable()
    {
        // Subscribe to turn events so butterfly moves when animals move
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced += OnTurnAdvanced;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from turn events
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= OnTurnAdvanced;
        }
    }

    private void Start()
    {
        // Initialize movement bounds if not set
        if (_movementBounds.size == Vector3.zero && EnvironmentManager.Instance != null)
        {
            Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
            Vector3 center = EnvironmentManager.Instance.GridToWorldPosition(new Vector2Int(gridSize.x / 2, gridSize.y / 2));
            Vector3 size = EnvironmentManager.Instance.GridToWorldPosition(new Vector2Int(gridSize.x, gridSize.y)) - 
                          EnvironmentManager.Instance.GridToWorldPosition(Vector2Int.zero);
            _movementBounds = new Bounds(center, size);
        }

        // Set initial target position
        _targetPosition = transform.position;
    }

    /// <summary>
    /// Called when a turn advances. Moves the butterfly to a new random position.
    /// </summary>
    private void OnTurnAdvanced(int turnCount)
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
    private void ChooseNewTarget()
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
        
        _targetPosition = newTarget;
        
        // Update sprite facing direction based on movement
        UpdateFacingDirection(newTarget);
    }

    /// <summary>
    /// Updates the sprite facing direction based on horizontal movement.
    /// </summary>
    private void UpdateFacingDirection(Vector3 targetPosition)
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
    private void StartMove()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }
        _moveCoroutine = StartCoroutine(MoveToTargetCoroutine());
    }

    /// <summary>
    /// Coroutine that smoothly moves the butterfly to the target position.
    /// </summary>
    private IEnumerator MoveToTargetCoroutine()
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

    /// <summary>
    /// Spawns butterflies randomly across the map within grid bounds.
    /// </summary>
    /// <param name="count">Number of butterflies to spawn</param>
    /// <param name="prefab">Butterfly prefab to instantiate</param>
    /// <param name="parent">Parent transform for butterflies (optional)</param>
    public static void SpawnButterflies(int count, GameObject prefab = null, Transform parent = null)
    {
        if (EnvironmentManager.Instance == null)
        {
            Debug.LogWarning("Butterfly: Cannot spawn butterflies - EnvironmentManager instance not found!");
            return;
        }

        // Load prefab from Resources if not provided
        if (prefab == null)
        {
            // Try different possible paths
            prefab = Resources.Load<GameObject>("Prefabs/Animals/Butterfly");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Animals/Butterfly");
            }
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Butterfly");
            }
        }

        if (prefab == null)
        {
            return;
        }

        // Create parent if not provided
        if (parent == null)
        {
            if (_butterflyParent == null)
            {
                GameObject parentObj = new GameObject("Butterflies");
                _butterflyParent = parentObj.transform;
            }
            parent = _butterflyParent;
        }

        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        List<Butterfly> spawnedButterflies = new List<Butterfly>();

        // Spawn butterflies at random positions within grid bounds
        for (int i = 0; i < count; i++)
        {
            // Pick a random grid position
            int x = Random.Range(0, gridSize.x);
            int y = Random.Range(0, gridSize.y);
            Vector2Int gridPos = new Vector2Int(x, y);

            // Convert to world position
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(gridPos);
            
            // Add some random offset within the cell for variety
            worldPos.x += Random.Range(-0.2f, 0.2f);
            worldPos.y += Random.Range(-0.2f, 0.2f);

            // Instantiate butterfly
            GameObject butterflyObj = Instantiate(prefab, worldPos, Quaternion.identity, parent);
            Butterfly butterfly = butterflyObj.GetComponent<Butterfly>();
            
            if (butterfly == null)
            {
                Debug.LogWarning($"Butterfly: Spawned GameObject at ({x}, {y}) does not have a Butterfly component!");
                Destroy(butterflyObj);
                continue;
            }

            spawnedButterflies.Add(butterfly);
        }

        Debug.Log($"Butterfly: Spawned {spawnedButterflies.Count} butterflies randomly across the map");
    }
}
