using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Cinemachine;

/// <summary>
/// Component that loads a tutorial level with manually specified placements.
/// Allows precise control over where all interactables, items, animals, and the den are placed.
/// </summary>
public class TutorialLevelLoader : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("Width of the tutorial level")]
    [SerializeField] private int _levelWidth = 20;
    
    [Tooltip("Height of the tutorial level")]
    [SerializeField] private int _levelHeight = 15;
    
    [Tooltip("Default tile type for the entire level (before specific tile placements)")]
    [SerializeField] private TileType _defaultTileType = TileType.Empty;
    
    [Tooltip("Add border walls made of obstacles")]
    [SerializeField] private bool _addBorderWalls = true;

    [Header("Tile Placements")]
    [Tooltip("Custom tile placements (will override default tile type at these positions)")]
    [SerializeField] private List<TilePlacement> _tilePlacements = new List<TilePlacement>();

    [System.Serializable]
    public class TilePlacement
    {
        public int x;
        public int y;
        public TileType tileType = TileType.Empty;
    }

    [Header("Den Placement")]
    [Tooltip("Position of the player's den")]
    [SerializeField] private Vector2Int _denPosition = new Vector2Int(2, 2);

    [Header("Controllable Animal")]
    [Tooltip("Name of the controllable animal to spawn")]
    [SerializeField] private string _controllableAnimalName = "KangarooRat";
    
    [Tooltip("Position to spawn the controllable animal")]
    [SerializeField] private Vector2Int _controllableAnimalPosition = new Vector2Int(2, 2);
    
    [Tooltip("Number of controllable animals to spawn")]
    [SerializeField] private int _controllableAnimalCount = 4;

    [Header("Predator Placements")]
    [Tooltip("Manually placed predators")]
    [SerializeField] private List<PredatorPlacement> _predatorPlacements = new List<PredatorPlacement>();

    [System.Serializable]
    public class PredatorPlacement
    {
        [Tooltip("Name of the predator (e.g., 'Wolf', 'Hawk')")]
        public string predatorName = "Wolf";
        
        [Tooltip("Grid position (x, y)")]
        public Vector2Int position = Vector2Int.zero;
        
        [Tooltip("Number of predators at this position")]
        [Min(1)]
        public int count = 1;
    }

    [Header("Predator Den Placements")]
    [Tooltip("Manually placed predator dens")]
    [SerializeField] private List<PredatorDenPlacement> _predatorDenPlacements = new List<PredatorDenPlacement>();

    [System.Serializable]
    public class PredatorDenPlacement
    {
        [Tooltip("Grid position (x, y)")]
        public Vector2Int position = Vector2Int.zero;
        
        [Tooltip("Type of predator associated with this den (e.g., 'Wolf', 'Hawk')")]
        public string predatorType = "Wolf";
    }

    [Header("Rabbit Placements")]
    [Tooltip("Manually placed rabbits")]
    [SerializeField] private List<RabbitPlacement> _rabbitPlacements = new List<RabbitPlacement>();

    [System.Serializable]
    public class RabbitPlacement
    {
        [Tooltip("Grid position (x, y)")]
        public Vector2Int position = Vector2Int.zero;
        
        [Tooltip("Number of rabbits at this position")]
        [Min(1)]
        public int count = 2;
    }

    [Header("Rabbit Spawner Placements")]
    [Tooltip("Manually placed rabbit spawners")]
    [SerializeField] private List<Vector2Int> _rabbitSpawnerPositions = new List<Vector2Int>();

    [Header("Worm Spawner Placements")]
    [Tooltip("Manually placed worm spawners")]
    [SerializeField] private List<Vector2Int> _wormSpawnerPositions = new List<Vector2Int>();

    [Header("Bush Placements")]
    [Tooltip("Manually placed bushes")]
    [SerializeField] private List<Vector2Int> _bushPositions = new List<Vector2Int>();

    [Header("Grass Placements")]
    [Tooltip("Manually placed grass interactables")]
    [SerializeField] private List<Vector2Int> _grassPositions = new List<Vector2Int>();

    [Header("Item Placements")]
    [Tooltip("Manually placed items")]
    [SerializeField] private List<ItemPlacement> _itemPlacements = new List<ItemPlacement>();

    [System.Serializable]
    public class ItemPlacement
    {
        [Tooltip("Name of the item (e.g., 'Food')")]
        public string itemName = "Food";
        
        [Tooltip("Grid position (x, y)")]
        public Vector2Int position = Vector2Int.zero;
    }

    /// <summary>
    /// Loads and applies the tutorial level with manually specified placements.
    /// </summary>
    public void LoadAndApplyLevel()
    {
        LevelData levelData = GenerateLevelData();

        if (levelData == null)
        {
            Debug.LogError("TutorialLevelLoader: Failed to generate level data!");
            return;
        }

        // Apply environment using EnvironmentManager
        if (EnvironmentManager.Instance == null)
        {
            Debug.LogError("TutorialLevelLoader: EnvironmentManager instance not found!");
            return;
        }

        // Initialize grid with level dimensions
        EnvironmentManager.Instance.InitializeGrid(levelData.Width, levelData.Height);

        // Apply tiles to the grid
        foreach (var (x, y, tileType) in levelData.Tiles)
        {
            if (EnvironmentManager.Instance.IsValidPosition(x, y))
            {
                EnvironmentManager.Instance.SetTileType(x, y, tileType);
            }
        }

        // Spawn interactables using InteractableManager (before animals so animals can register on spawn)
        if (InteractableManager.Instance != null)
        {
            InteractableManager.Instance.ClearAllInteractables();
            InteractableManager.Instance.SpawnDensFromLevelData(levelData.Dens);
            InteractableManager.Instance.SpawnRabbitSpawnersFromLevelData(levelData.RabbitSpawners);
            InteractableManager.Instance.SpawnWormSpawnersFromLevelData(levelData.WormSpawners);
            InteractableManager.Instance.SpawnBushesFromLevelData(levelData.Bushes);
            InteractableManager.Instance.SpawnGrassesFromLevelData(levelData.Grasses);
        }
        else
        {
            Debug.LogWarning("TutorialLevelLoader: InteractableManager instance not found! Interactables will not be spawned.");
        }

        // Spawn animals using AnimalManager (before predator dens so den prefabs are registered)
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.SpawnAnimalsFromLevelData(levelData.Animals);
            
            // Spawn unassigned workers at the player's position
            if (DenSystemManager.Instance != null && !string.IsNullOrEmpty(_controllableAnimalName) && IsValidGridPosition(_controllableAnimalPosition.x, _controllableAnimalPosition.y))
            {
                for (int i = 0; i < _controllableAnimalCount; i++)
                {
                    DenSystemManager.Instance.CreateWorkerAtPosition(_controllableAnimalPosition);
                }
            }
        }
        else
        {
            Debug.LogWarning("TutorialLevelLoader: AnimalManager instance not found! Animals will not be spawned.");
        }
		
		// Spawn predator dens using PredatorAnimal (after animals are loaded so den prefabs are registered)
		if (InteractableManager.Instance != null)
		{
			// Set the interactable parent for PredatorAnimal to use
			PredatorAnimal.SetInteractableParent(InteractableManager.Instance.InteractableParent);
		}
		
		// Spawn predator dens from level data
		PredatorAnimal.SpawnPredatorDensFromLevelData(levelData.PredatorDens);

        // Spawn items using ItemManager
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.SpawnItemsFromLevelData(levelData.Items);
        }
        else
        {
            Debug.LogWarning("TutorialLevelLoader: ItemManager instance not found! Items will not be spawned.");
        }

        // Reset points in PointsManager
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.ResetPoints();
        }

        // Register any animals that are already on dens (safety check)
        if (InteractableManager.Instance != null && AnimalManager.Instance != null)
        {
            InteractableManager.Instance.RegisterAnimalsOnDens();
        }

        // Initialize fog of war to cover entire level with black tiles
        // This must happen after level tiles are loaded to ensure fog overlaps on top
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.InitializeFog();
            // Then update fog to reveal initial animal positions
            FogOfWarManager.Instance.UpdateFogOfWar();
        }

        // Force-refresh A* Pathfinding graph and sync walkability with EnvironmentManager
        RefreshAStarGraphs();

        // Set virtual camera to follow the controllable animal
        SetupCameraFollow();

		// Count interactables by type for debug log
		int densCount = 0, rabbitSpawnersCount = 0, wormSpawnersCount = 0, predatorDensCount = 0, bushesCount = 0, grassesCount = 0;
		foreach (var interactable in levelData.Interactables)
		{
			switch (interactable.Type)
			{
				case InteractableType.Den: densCount++; break;
				case InteractableType.RabbitSpawner: rabbitSpawnersCount++; break;
				case InteractableType.WormSpawner: wormSpawnersCount++; break;
				case InteractableType.PredatorDen: predatorDensCount++; break;
				case InteractableType.Bush: bushesCount++; break;
				case InteractableType.Grass: grassesCount++; break;
			}
		}
		Debug.Log($"TutorialLevelLoader: Successfully loaded tutorial level with {levelData.Tiles.Count} tiles, {levelData.Animals.Count} animals, {levelData.Items.Count} items, {levelData.Interactables.Count} interactables ({densCount} dens, {rabbitSpawnersCount} rabbit spawners, {wormSpawnersCount} worm spawners, {predatorDensCount} predator dens, {bushesCount} bushes, {grassesCount} grasses)");
    }

    /// <summary>
    /// Generates level data from manually specified placements.
    /// </summary>
    private LevelData GenerateLevelData()
    {
        LevelData levelData = new LevelData();
        levelData.Width = _levelWidth;
        levelData.Height = _levelHeight;

        // Initialize all tiles with default tile type
        for (int y = 0; y < _levelHeight; y++)
        {
            for (int x = 0; x < _levelWidth; x++)
            {
                TileType tileType = _defaultTileType;

                // Check if this is a border position
                if (_addBorderWalls && (x == 0 || x == _levelWidth - 1 || y == 0 || y == _levelHeight - 1))
                {
                    tileType = TileType.Obstacle;
                }

                levelData.Tiles.Add((x, y, tileType));
            }
        }

        // Apply custom tile placements (overrides default)
        foreach (var placement in _tilePlacements)
        {
            if (IsValidGridPosition(placement.x, placement.y))
            {
                // Find and update the tile at this position
                for (int i = 0; i < levelData.Tiles.Count; i++)
                {
                    var (x, y, _) = levelData.Tiles[i];
                    if (x == placement.x && y == placement.y)
                    {
                        levelData.Tiles[i] = (x, y, placement.tileType);
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid tile placement at ({placement.x}, {placement.y}). Skipping.");
            }
        }

        // Initialize lists
        levelData.Animals = new List<(string animalName, int x, int y, int count)>();
        levelData.Items = new List<(string itemName, int x, int y)>();
        levelData.Interactables = new List<InteractableData>();
        levelData.FoodCount = 0;
        
        // Initialize legacy lists for backward compatibility
        levelData.Dens = new List<(int x, int y)>();
        levelData.RabbitSpawners = new List<(int x, int y)>();
        levelData.PredatorDens = new List<(int x, int y, string predatorType)>();
        levelData.WormSpawners = new List<(int x, int y)>();
        levelData.Bushes = new List<(int x, int y)>();
        levelData.Grasses = new List<(int x, int y)>();

        // Place den
        if (IsValidGridPosition(_denPosition.x, _denPosition.y))
        {
            levelData.Dens.Add((_denPosition.x, _denPosition.y));
            levelData.Interactables.Add(new InteractableData(InteractableType.Den, _denPosition.x, _denPosition.y));
        }
        else
        {
            Debug.LogWarning($"TutorialLevelLoader: Invalid den position ({_denPosition.x}, {_denPosition.y}). Skipping.");
        }

        // Place controllable animal with count 1 (followers will be based on unassigned workers)
        if (!string.IsNullOrEmpty(_controllableAnimalName) && IsValidGridPosition(_controllableAnimalPosition.x, _controllableAnimalPosition.y))
        {
            levelData.Animals.Add((_controllableAnimalName, _controllableAnimalPosition.x, _controllableAnimalPosition.y, 1));
        }
        else if (!string.IsNullOrEmpty(_controllableAnimalName))
        {
            Debug.LogWarning($"TutorialLevelLoader: Invalid controllable animal position ({_controllableAnimalPosition.x}, {_controllableAnimalPosition.y}). Skipping.");
        }

        // Place predators
        foreach (var placement in _predatorPlacements)
        {
            if (IsValidGridPosition(placement.position.x, placement.position.y))
            {
                if (!string.IsNullOrEmpty(placement.predatorName))
                {
                    levelData.Animals.Add((placement.predatorName, placement.position.x, placement.position.y, placement.count));
                }
                else
                {
                    Debug.LogWarning($"TutorialLevelLoader: Predator placement at ({placement.position.x}, {placement.position.y}) has empty name. Skipping.");
                }
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid predator position ({placement.position.x}, {placement.position.y}). Skipping.");
            }
        }

        // Place predator dens
        foreach (var placement in _predatorDenPlacements)
        {
            if (IsValidGridPosition(placement.position.x, placement.position.y))
            {
                if (!string.IsNullOrEmpty(placement.predatorType))
                {
                    levelData.PredatorDens.Add((placement.position.x, placement.position.y, placement.predatorType));
                    levelData.Interactables.Add(new InteractableData(InteractableType.PredatorDen, placement.position.x, placement.position.y, placement.predatorType));
                }
                else
                {
                    Debug.LogWarning($"TutorialLevelLoader: Predator den at ({placement.position.x}, {placement.position.y}) has empty predator type. Skipping.");
                }
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid predator den position ({placement.position.x}, {placement.position.y}). Skipping.");
            }
        }

        // Place rabbits
        foreach (var placement in _rabbitPlacements)
        {
            if (IsValidGridPosition(placement.position.x, placement.position.y))
            {
                levelData.Animals.Add(("Rabbit", placement.position.x, placement.position.y, placement.count));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid rabbit position ({placement.position.x}, {placement.position.y}). Skipping.");
            }
        }

        // Place rabbit spawners
        foreach (var pos in _rabbitSpawnerPositions)
        {
            if (IsValidGridPosition(pos.x, pos.y))
            {
                levelData.RabbitSpawners.Add((pos.x, pos.y));
                levelData.Interactables.Add(new InteractableData(InteractableType.RabbitSpawner, pos.x, pos.y));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid rabbit spawner position ({pos.x}, {pos.y}). Skipping.");
            }
        }

        // Place worm spawners
        foreach (var pos in _wormSpawnerPositions)
        {
            if (IsValidGridPosition(pos.x, pos.y))
            {
                levelData.WormSpawners.Add((pos.x, pos.y));
                levelData.Interactables.Add(new InteractableData(InteractableType.WormSpawner, pos.x, pos.y));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid worm spawner position ({pos.x}, {pos.y}). Skipping.");
            }
        }

        // Place bushes
        foreach (var pos in _bushPositions)
        {
            if (IsValidGridPosition(pos.x, pos.y))
            {
                levelData.Bushes.Add((pos.x, pos.y));
                levelData.Interactables.Add(new InteractableData(InteractableType.Bush, pos.x, pos.y));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid bush position ({pos.x}, {pos.y}). Skipping.");
            }
        }

        // Place grasses
        foreach (var pos in _grassPositions)
        {
            if (IsValidGridPosition(pos.x, pos.y))
            {
                levelData.Grasses.Add((pos.x, pos.y));
                levelData.Interactables.Add(new InteractableData(InteractableType.Grass, pos.x, pos.y));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid grass position ({pos.x}, {pos.y}). Skipping.");
            }
        }

        // Place items
        foreach (var placement in _itemPlacements)
        {
            if (IsValidGridPosition(placement.position.x, placement.position.y))
            {
                if (!string.IsNullOrEmpty(placement.itemName))
                {
                    levelData.Items.Add((placement.itemName, placement.position.x, placement.position.y));
                    if (placement.itemName == "Food")
                    {
                        levelData.FoodCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"TutorialLevelLoader: Item placement at ({placement.position.x}, {placement.position.y}) has empty name. Skipping.");
                }
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid item position ({placement.position.x}, {placement.position.y}). Skipping.");
            }
        }

        Debug.Log($"TutorialLevelLoader: Generated level with {levelData.Tiles.Count} tiles, size: {levelData.Width}x{levelData.Height}");
        return levelData;
    }

    /// <summary>
    /// Checks if a grid position is valid for the current level dimensions.
    /// </summary>
    private bool IsValidGridPosition(int x, int y)
    {
        return x >= 0 && x < _levelWidth && y >= 0 && y < _levelHeight;
    }

    /// <summary>
    /// Sets up the virtual camera to follow the controllable animal.
    /// Teleports the camera to the animal's position immediately.
    /// </summary>
    private void SetupCameraFollow()
    {
        // Find the controllable animal
        if (AnimalManager.Instance == null)
        {
            Debug.LogWarning("TutorialLevelLoader: AnimalManager instance not found! Cannot set up camera follow.");
            return;
        }

        List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
        Animal controllableAnimal = null;

        foreach (Animal animal in animals)
        {
            if (animal != null && animal.IsControllable)
            {
                controllableAnimal = animal;
                break;
            }
        }

        if (controllableAnimal == null)
        {
            Debug.LogWarning("TutorialLevelLoader: No controllable animal found! Camera will not follow.");
            return;
        }

        // Find the Cinemachine Virtual Camera in the scene
        CinemachineVirtualCamera virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        
        if (virtualCamera == null)
        {
            Debug.LogWarning("TutorialLevelLoader: CinemachineVirtualCamera not found in scene! Camera will not follow.");
            return;
        }

        // Set the follow target to the controllable animal's transform
        virtualCamera.Follow = controllableAnimal.transform;
        virtualCamera.LookAt = controllableAnimal.transform;

        // Teleport camera immediately to the animal's position
        // Get the world position of the animal
        Vector3 animalWorldPos = controllableAnimal.transform.position;
        
        // Set camera position (preserve Z for orthographic camera)
        Transform cameraTransform = virtualCamera.transform;
        Vector3 cameraPos = new Vector3(animalWorldPos.x, animalWorldPos.y, cameraTransform.position.z);
        cameraTransform.position = cameraPos;

        Debug.Log($"TutorialLevelLoader: Virtual camera set to follow controllable animal at {controllableAnimal.GridPosition}");
    }

    /// <summary>
    /// Forces A* graphs to rebuild and aligns GridGraph node walkability with EnvironmentManager.
    /// Marks water tiles and obstacles as non-walkable in the base graph.
    /// </summary>
    private void RefreshAStarGraphs()
    {
        if (AstarPath.active == null)
        {
            Debug.LogWarning("TutorialLevelLoader: AstarPath.active is null. A* Pathfinding graph was not re-scanned.");
            return;
        }

        // Full scan first to (re)create nodes according to current graph settings
        AstarPath.active.Scan();

        // If we have a GridGraph, explicitly set node walkability from EnvironmentManager
        var gridGraph = AstarPath.active.data?.gridGraph;
        if (gridGraph != null && EnvironmentManager.Instance != null)
        {
            gridGraph.GetNodes((GraphNode node) =>
            {
                Vector3 world = (Vector3)node.position;
                Vector2Int grid = EnvironmentManager.Instance.WorldToGridPosition(world);
                
                if (!EnvironmentManager.Instance.IsValidPosition(grid))
                {
                    node.Walkable = false;
                    return;
                }
                
                // Check if position is walkable (not an obstacle)
                bool isWalkable = EnvironmentManager.Instance.IsWalkable(grid);
                
                // Also check if it's a water tile - mark water as non-walkable in base graph
                // (WaterTraversalProvider will handle allowing water for animals that can swim)
                TileType tileType = EnvironmentManager.Instance.GetTileType(grid);
                bool isWater = (tileType == TileType.Water);
                
                // Node is walkable only if it's not an obstacle AND not water
                node.Walkable = isWalkable && !isWater;
            });

            // Connected components are handled by the hierarchical graph automatically in recent versions
        }

        // Ensure any pending work is completed
        AstarPath.active.FlushWorkItems();
        AstarPath.active.FlushGraphUpdates();
        Debug.Log("TutorialLevelLoader: A* Pathfinding graph force-refreshed and nodes synced with EnvironmentManager (water tiles marked as non-walkable).");
    }
}

