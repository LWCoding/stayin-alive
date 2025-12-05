using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tree interactable that spawns bees and grass around it when the game starts.
/// Bees fly around within a specific radius of the tree, and grass spawns within the same radius.
/// </summary>
public class BeeTree : Interactable
{
    [Header("Bee Spawn Settings")]
    [Tooltip("Prefab to use when spawning bees. Must have a Bee component.")]
    [SerializeField] private GameObject _beePrefab;
    
    [Tooltip("Number of bees to spawn around this tree")]
    [SerializeField] private int _beeCount = 3;
    
    [Header("Tree Radius")]
    [Tooltip("Radius around the tree for bee roaming and grass spawning (in world units)")]
    [SerializeField] private float _treeRadius = 3f;

    [Header("Grass Spawn Settings")]
    [Tooltip("Number of grass interactables to spawn around this tree")]
    [SerializeField] private int _grassCount = 3;

    private List<Bee> _spawnedBees = new List<Bee>();
    private List<Grass> _spawnedGrass = new List<Grass>();
    private bool _beesSpawned = false;
    private bool _grassSpawned = false;
    private bool _isInitialized = false;

    private void Start()
    {
        // Only spawn bees if already initialized (for manually placed trees in scene)
        // For procedurally spawned trees, Initialize() will be called and spawn bees
        if (_isInitialized && !_beesSpawned)
        {
            SpawnBees();
        }
        
        // Only spawn grass if already initialized (for manually placed trees in scene)
        // For procedurally spawned trees, Initialize() will be called and spawn grass
        if (_isInitialized && !_grassSpawned)
        {
            SpawnGrass();
        }
    }

    /// <summary>
    /// Initializes the bee tree at the specified grid position.
    /// </summary>
    public override void Initialize(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
        UpdateWorldPosition();
        _isInitialized = true;
        
        // Spawn bees after initialization (if not already spawned)
        if (!_beesSpawned)
        {
            SpawnBees();
        }
        
        // Spawn grass after initialization (if not already spawned)
        if (!_grassSpawned)
        {
            SpawnGrass();
        }
    }

    private void UpdateWorldPosition()
    {
        if (EnvironmentManager.Instance != null)
        {
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
            transform.position = worldPos;
        }
        else
        {
            transform.position = new Vector3(_gridPosition.x, _gridPosition.y, transform.position.z);
        }
    }

    /// <summary>
    /// Spawns bees around this tree.
    /// </summary>
    private void SpawnBees()
    {
        if (_beesSpawned)
        {
            return; // Already spawned
        }

        if (_beePrefab == null)
        {
            Debug.LogWarning($"BeeTree: Cannot spawn bees - bee prefab not assigned!");
            return;
        }

        if (_beeCount <= 0)
        {
            return;
        }

        // Use tree's world position as center
        Vector3 centerPosition = transform.position;

        // Determine parent for bees
        Transform parent = transform;

        // Spawn bees at random positions around the tree
        for (int i = 0; i < _beeCount; i++)
        {
            // Instantiate bee at tree position first (will be repositioned by SetCenterAndRadius)
            GameObject beeObj = Instantiate(_beePrefab, centerPosition, Quaternion.identity, parent);
            Bee bee = beeObj.GetComponent<Bee>();

            if (bee == null)
            {
                Debug.LogWarning($"BeeTree: Spawned GameObject does not have a Bee component! Destroying it.");
                Destroy(beeObj);
                continue;
            }

            // Configure bee's movement radius - this will also set initial position near the tree
            bee.SetCenterAndRadius(centerPosition, _treeRadius);
            _spawnedBees.Add(bee);
        }

        _beesSpawned = true;
        Debug.Log($"BeeTree: Spawned {_spawnedBees.Count} bees around tree at ({_gridPosition.x}, {_gridPosition.y})");
    }

    /// <summary>
    /// Spawns grass interactables around this tree at valid grid positions within the tree radius.
    /// </summary>
    private void SpawnGrass()
    {
        if (_grassSpawned)
        {
            return; // Already spawned
        }

        if (InteractableManager.Instance == null)
        {
            Debug.LogWarning($"BeeTree: Cannot spawn grass - InteractableManager instance not found!");
            return;
        }

        if (EnvironmentManager.Instance == null)
        {
            Debug.LogWarning($"BeeTree: Cannot spawn grass - EnvironmentManager instance not found!");
            return;
        }

        if (_grassCount <= 0)
        {
            return;
        }

        // Get tree's world position for distance calculations
        Vector3 treeWorldPos = transform.position;
        
        // Calculate radius in grid units (approximate - assumes 1 grid unit = 1 world unit)
        // We'll check all grid positions within a square area that encompasses the radius
        int radiusInGridUnits = Mathf.CeilToInt(_treeRadius);
        
        // Get all valid positions within the radius
        List<Vector2Int> validPositions = new List<Vector2Int>();
        
        // Check all grid positions in a square area around the tree
        for (int x = _gridPosition.x - radiusInGridUnits; x <= _gridPosition.x + radiusInGridUnits; x++)
        {
            for (int y = _gridPosition.y - radiusInGridUnits; y <= _gridPosition.y + radiusInGridUnits; y++)
            {
                Vector2Int candidatePos = new Vector2Int(x, y);
                
                // Skip the tree's own position
                if (candidatePos == _gridPosition)
                {
                    continue;
                }
                
                // Check if position is valid
                if (!EnvironmentManager.Instance.IsValidPosition(candidatePos))
                {
                    continue;
                }

                // Check if tile is water or obstacle
                TileType tileType = EnvironmentManager.Instance.GetTileType(candidatePos);
                if (tileType == TileType.Water || tileType == TileType.Obstacle)
                {
                    continue;
                }

                // Check if there's already an interactable at this position
                if (InteractableManager.Instance.HasInteractableAtPosition(candidatePos))
                {
                    continue;
                }

                // Check if position is within the radius (using world positions for accurate distance)
                Vector3 candidateWorldPos = EnvironmentManager.Instance.GridToWorldPosition(candidatePos);
                float distance = Vector3.Distance(treeWorldPos, candidateWorldPos);
                
                if (distance <= _treeRadius)
                {
                    validPositions.Add(candidatePos);
                }
            }
        }

        // Shuffle valid positions to randomize grass placement
        for (int i = 0; i < validPositions.Count; i++)
        {
            Vector2Int temp = validPositions[i];
            int randomIndex = Random.Range(i, validPositions.Count);
            validPositions[i] = validPositions[randomIndex];
            validPositions[randomIndex] = temp;
        }

        // Spawn grass at valid positions (up to _grassCount)
        int grassSpawned = 0;
        for (int i = 0; i < validPositions.Count && grassSpawned < _grassCount; i++)
        {
            Grass grass = InteractableManager.Instance.SpawnGrass(validPositions[i]);
            if (grass != null)
            {
                // Apply 50% faster growth for grass near bee trees (1.5x multiplier)
                grass.SetBeeTreeProximityMultiplier(1.5f);
                _spawnedGrass.Add(grass);
                grassSpawned++;
            }
        }

        _grassSpawned = true;
        Debug.Log($"BeeTree: Spawned {_spawnedGrass.Count} grass around tree at ({_gridPosition.x}, {_gridPosition.y})");
    }

    private void OnDestroy()
    {
        // Clean up spawned bees when tree is destroyed
        foreach (Bee bee in _spawnedBees)
        {
            if (bee != null)
            {
                Destroy(bee.gameObject);
            }
        }
        _spawnedBees.Clear();
        
        // Clean up spawned grass when tree is destroyed
        foreach (Grass grass in _spawnedGrass)
        {
            if (grass != null)
            {
                Destroy(grass.gameObject);
            }
        }
        _spawnedGrass.Clear();
    }
}
