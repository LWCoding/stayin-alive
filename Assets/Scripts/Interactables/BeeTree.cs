using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tree interactable that spawns bees around it when the game starts.
/// Bees fly around within a specific radius of the tree.
/// </summary>
public class BeeTree : Interactable
{
    [Header("Bee Spawn Settings")]
    [Tooltip("Prefab to use when spawning bees. Must have a Bee component.")]
    [SerializeField] private GameObject _beePrefab;
    
    [Tooltip("Number of bees to spawn around this tree")]
    [SerializeField] private int _beeCount = 3;
    
    [Tooltip("Radius around the tree that bees can fly (in world units)")]
    [SerializeField] private float _beeRadius = 3f;

    private List<Bee> _spawnedBees = new List<Bee>();
    private bool _beesSpawned = false;
    private bool _isInitialized = false;

    private void Start()
    {
        // Only spawn bees if already initialized (for manually placed trees in scene)
        // For procedurally spawned trees, Initialize() will be called and spawn bees
        if (_isInitialized && !_beesSpawned)
        {
            SpawnBees();
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
            bee.SetCenterAndRadius(centerPosition, _beeRadius);
            _spawnedBees.Add(bee);
        }

        _beesSpawned = true;
        Debug.Log($"BeeTree: Spawned {_spawnedBees.Count} bees around tree at ({_gridPosition.x}, {_gridPosition.y})");
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
    }
}
