using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns butterflies randomly across the map at the start of the game.
/// </summary>
public class ButterflySpawner : MonoBehaviour
{
    [Header("Butterfly Settings")]
    [Tooltip("Prefab to use when spawning butterflies. Must have a Butterfly component.")]
    [SerializeField] private GameObject _butterflyPrefab;
    
    [Tooltip("Number of butterflies to spawn")]
    [SerializeField] private int _butterflyCount = 10;

    private bool _hasSpawned = false;

    private void Start()
    {
        // Start coroutine to wait for EnvironmentManager and subscribe to events
        StartCoroutine(WaitForEnvironmentManagerAndSubscribe());
    }

    private IEnumerator WaitForEnvironmentManagerAndSubscribe()
    {
        // Wait for EnvironmentManager singleton to be ready
        while (EnvironmentManager.Instance == null)
        {
            yield return null;
        }

        EnvironmentManager.Instance.OnGridInitialized += OnGridInitialized;
        
        // Check if grid is already initialized
        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        
        if (gridSize.x > 0 && gridSize.y > 0)
        {
            // Grid is already initialized, spawn immediately
            OnGridInitialized(gridSize.x, gridSize.y);
        }
        else
        {
            // Wait for grid to be initialized
            while (!_hasSpawned && (gridSize.x <= 0 || gridSize.y <= 0))
            {
                gridSize = EnvironmentManager.Instance.GetGridSize();
                yield return null;
            }
            
            // If we still haven't spawned and grid is now initialized, spawn
            if (!_hasSpawned && gridSize.x > 0 && gridSize.y > 0)
            {
                OnGridInitialized(gridSize.x, gridSize.y);
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from grid initialization event
        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.OnGridInitialized -= OnGridInitialized;
        }
    }

    private void OnGridInitialized(int width, int height)
    {
        // Spawn butterflies when grid is initialized (only once)
        if (!_hasSpawned)
        {
            SpawnButterflies();
            _hasSpawned = true;
        }
    }

    /// <summary>
    /// Spawns butterflies randomly across the map within grid bounds.
    /// </summary>
    private void SpawnButterflies()
    {
        if (_butterflyPrefab == null)
        {
            Debug.LogError("ButterflySpawner: Butterfly prefab is not assigned! Please assign it in the Inspector.");
            return;
        }

        if (EnvironmentManager.Instance == null)
        {
            Debug.LogWarning("ButterflySpawner: Cannot spawn butterflies - EnvironmentManager instance not found!");
            return;
        }

        Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
        
        // Validate grid size
        if (gridSize.x <= 0 || gridSize.y <= 0)
        {
            Debug.LogWarning($"ButterflySpawner: Invalid grid size ({gridSize.x}, {gridSize.y}). Cannot spawn butterflies.");
            return;
        }

        // Create a parent GameObject to keep butterflies organized in the hierarchy
        GameObject parentObj = new GameObject("Butterflies");
        Transform butterflyParent = parentObj.transform;

        List<Butterfly> spawnedButterflies = new List<Butterfly>();

        // Spawn butterflies at random positions within grid bounds
        for (int i = 0; i < _butterflyCount; i++)
        {
            // Pick a random grid position
            int x = Random.Range(0, gridSize.x);
            int y = Random.Range(0, gridSize.y);
            Vector2Int gridPos = new Vector2Int(x, y);

            // Convert to world position
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(gridPos);
            
            // Add some random offset within the cell for variety
            worldPos.x += Random.Range(-0.3f, 0.3f);
            worldPos.y += Random.Range(-0.3f, 0.3f);

            // Instantiate butterfly
            GameObject butterflyObj = Instantiate(_butterflyPrefab, worldPos, Quaternion.identity, butterflyParent);
            Butterfly butterfly = butterflyObj.GetComponent<Butterfly>();
            
            if (butterfly == null)
            {
                Debug.LogWarning($"ButterflySpawner: Spawned GameObject at ({x}, {y}) does not have a Butterfly component!");
                Destroy(butterflyObj);
                continue;
            }

            spawnedButterflies.Add(butterfly);
        }
    }
}
