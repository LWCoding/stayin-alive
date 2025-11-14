using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Cinemachine;

/// <summary>
/// Component that procedurally generates levels using Perlin Noise.
/// Generates environments with the same tile types as LevelLoader (Empty, Water, Grass, Obstacle).
/// </summary>
public class ProceduralLevelLoader : MonoBehaviour
{
    [Header("Level Generation Settings")]
    [Tooltip("Width of the generated level")]
    [SerializeField] private int _levelWidth = 30;
    
    [Tooltip("Height of the generated level")]
    [SerializeField] private int _levelHeight = 20;
    
    [Header("Perlin Noise Settings")]
    [Tooltip("Scale of the Perlin noise for terrain generation (higher = more variation)")]
    [SerializeField] private float _terrainNoiseScale = 0.1f;
    
    [Tooltip("Scale of the Perlin noise for obstacle generation (higher = more variation)")]
    [SerializeField] private float _obstacleNoiseScale = 0.15f;
    
    [Tooltip("Threshold for water tiles (noise value below this becomes water)")]
    [Range(0f, 1f)]
    [SerializeField] private float _waterThreshold = 0.3f;
    
    [Tooltip("Threshold for grass tiles (noise value between waterThreshold and this becomes grass)")]
    [Range(0f, 1f)]
    [SerializeField] private float _grassThreshold = 0.7f;
    
    [Tooltip("Threshold for obstacles (obstacle noise value above this creates obstacles)")]
    [Range(0f, 1f)]
    [SerializeField] private float _obstacleThreshold = 0.85f;
    
    [Tooltip("Random seed for level generation (0 = random each time)")]
    [SerializeField] private int _randomSeed = 0;
    
    [Tooltip("Add border walls made of obstacles")]
    [SerializeField] private bool _addBorderWalls = true;
    
    [Header("Animal Spawning Settings")]
    [Tooltip("Name of the controllable animal to spawn")]
    [SerializeField] private string _controllableAnimalName = "KangarooRat";
    
    [Tooltip("Number of controllable animals to spawn")]
    [SerializeField] private int _controllableAnimalCount = 4;
    
    [Tooltip("Number of predator patches to spawn")]
    [SerializeField] private int _predatorPatchCount = 3;
    
    [Tooltip("Number of predators per patch")]
    [SerializeField] private int _predatorsPerPatch = 2;
    
    [Tooltip("Radius of predator patches (how spread out predators are in each patch)")]
    [SerializeField] private int _predatorPatchRadius = 3;
    
    [Tooltip("Names of predator animals (e.g., 'Wolf', 'Hawk')")]
    [SerializeField] private string[] _predatorNames = new string[] { "Wolf", "Hawk" };
    
    [Tooltip("Number of rabbits to spawn")]
    [SerializeField] private int _rabbitCount = 8;
    
    [Tooltip("Number of rabbits per group")]
    [SerializeField] private int _rabbitsPerGroup = 2;

	[Tooltip("Number of rabbit spawner interactables to spawn")]
	[SerializeField] private int _rabbitSpawnerCount = 2;

	[Tooltip("Number of worm spawner interactables to spawn")]
	[SerializeField] private int _wormSpawnerCount = 2;
    
    [Tooltip("Number of food items to spawn")]
    [SerializeField] private int _foodItemCount = 5;

    /// <summary>
    /// Generates and applies a procedurally generated level.
    /// </summary>
    public void LoadAndApplyLevel()
    {
        LevelData levelData = GenerateLevelData();

        if (levelData == null)
        {
            Debug.LogError("ProceduralLevelLoader: Failed to generate level data!");
            return;
        }

        // Apply environment using EnvironmentManager
        if (EnvironmentManager.Instance == null)
        {
            Debug.LogError("ProceduralLevelLoader: EnvironmentManager instance not found!");
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
			InteractableManager.Instance.SpawnPredatorDensFromLevelData(levelData.PredatorDens);
			InteractableManager.Instance.SpawnWormSpawnersFromLevelData(levelData.WormSpawners);
        }
        else
        {
			Debug.LogWarning("ProceduralLevelLoader: InteractableManager instance not found! Interactables will not be spawned.");
        }

        // Spawn animals using AnimalManager
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.SpawnAnimalsFromLevelData(levelData.Animals);
        }
        else
        {
            Debug.LogWarning("ProceduralLevelLoader: AnimalManager instance not found! Animals will not be spawned.");
        }

        // Spawn items using ItemTilemapManager
        if (ItemTilemapManager.Instance != null)
        {
            ItemTilemapManager.Instance.PlaceItemsFromLevelData(levelData.Items);
        }
        else
        {
            Debug.LogWarning("ProceduralLevelLoader: ItemTilemapManager instance not found! Items will not be spawned.");
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

		Debug.Log($"ProceduralLevelLoader: Successfully generated level with {levelData.Tiles.Count} tiles, {levelData.Animals.Count} animals, {levelData.Items.Count} items, {levelData.Dens.Count} dens, {levelData.RabbitSpawners.Count} rabbit spawners, {levelData.WormSpawners.Count} worm spawners, and {levelData.PredatorDens.Count} predator dens");
    }

    /// <summary>
    /// Generates level data using Perlin Noise.
    /// </summary>
    private LevelData GenerateLevelData()
    {
        LevelData levelData = new LevelData();
        levelData.Width = _levelWidth;
        levelData.Height = _levelHeight;

        // Set random seed if specified
        if (_randomSeed != 0)
        {
            Random.InitState(_randomSeed);
        }

        // Generate random offsets for Perlin noise to ensure different levels each time
        float terrainOffsetX = Random.Range(0f, 1000f);
        float terrainOffsetY = Random.Range(0f, 1000f);
        float obstacleOffsetX = Random.Range(0f, 1000f);
        float obstacleOffsetY = Random.Range(0f, 1000f);

        // Generate tiles using Perlin Noise
        for (int y = 0; y < _levelHeight; y++)
        {
            for (int x = 0; x < _levelWidth; x++)
            {
                TileType tileType = TileType.Empty;

                // Check if this is a border position
                if (_addBorderWalls && (x == 0 || x == _levelWidth - 1 || y == 0 || y == _levelHeight - 1))
                {
                    tileType = TileType.Obstacle;
                }
                else
                {
                    // Sample Perlin noise for terrain
                    float terrainNoise = Mathf.PerlinNoise(
                        terrainOffsetX + x * _terrainNoiseScale,
                        terrainOffsetY + y * _terrainNoiseScale
                    );

                    // Sample Perlin noise for obstacles
                    float obstacleNoise = Mathf.PerlinNoise(
                        obstacleOffsetX + x * _obstacleNoiseScale,
                        obstacleOffsetY + y * _obstacleNoiseScale
                    );

                    // Determine tile type based on noise values
                    if (obstacleNoise > _obstacleThreshold)
                    {
                        tileType = TileType.Obstacle;
                    }
                    else if (terrainNoise < _waterThreshold)
                    {
                        tileType = TileType.Water;
                    }
                    else if (terrainNoise < _grassThreshold)
                    {
                        tileType = TileType.Grass;
                    }
                    else
                    {
                        tileType = TileType.Empty;
                    }
                }

                levelData.Tiles.Add((x, y, tileType));
            }
        }

        // Initialize lists
		levelData.Animals = new List<(string animalName, int x, int y, int count)>();
		levelData.Items = new List<(string itemName, int x, int y)>();
		levelData.Dens = new List<(int x, int y)>();
		levelData.RabbitSpawners = new List<(int x, int y)>();
		levelData.PredatorDens = new List<(int x, int y, string predatorType)>();
		levelData.WormSpawners = new List<(int x, int y)>();
        levelData.FoodCount = 0;

        // Generate spawn positions for animals, dens, and items
        GenerateSpawnPositions(levelData);

        Debug.Log($"ProceduralLevelLoader: Generated level with {levelData.Tiles.Count} tiles, size: {levelData.Width}x{levelData.Height}");
        return levelData;
    }

    /// <summary>
    /// Generates spawn positions for animals, dens, and items.
    /// </summary>
    private void GenerateSpawnPositions(LevelData levelData)
    {
        // First, collect all walkable positions (not obstacles, preferably not water)
        List<Vector2Int> walkablePositions = new List<Vector2Int>();
        List<Vector2Int> preferredPositions = new List<Vector2Int>(); // Grass or Empty (not water)
        
        for (int y = 0; y < _levelHeight; y++)
        {
            for (int x = 0; x < _levelWidth; x++)
            {
                // Find the tile type for this position
                TileType tileType = TileType.Empty;
                foreach (var (tx, ty, tt) in levelData.Tiles)
                {
                    if (tx == x && ty == y)
                    {
                        tileType = tt;
                        break;
                    }
                }
                
                // Skip obstacles and border walls
                if (tileType == TileType.Obstacle)
                    continue;
                
                Vector2Int pos = new Vector2Int(x, y);
                walkablePositions.Add(pos);
                
                // Prefer grass or empty tiles (not water) for spawning
                if (tileType == TileType.Grass || tileType == TileType.Empty)
                {
                    preferredPositions.Add(pos);
                }
            }
        }
        
        if (walkablePositions.Count == 0)
        {
            Debug.LogWarning("ProceduralLevelLoader: No walkable positions found! Cannot spawn animals.");
            return;
        }
        
        // Use preferred positions if available, otherwise fall back to all walkable positions
        List<Vector2Int> spawnPositions = preferredPositions.Count > 0 ? preferredPositions : walkablePositions;
        
        // 1. Spawn controllable animal at a random walkable position
        if (!string.IsNullOrEmpty(_controllableAnimalName) && spawnPositions.Count > 0)
        {
            Vector2Int controllablePos = spawnPositions[Random.Range(0, spawnPositions.Count)];
            levelData.Animals.Add((_controllableAnimalName, controllablePos.x, controllablePos.y, _controllableAnimalCount));
            
            // Place den at the same position as the controllable animal
            levelData.Dens.Add((controllablePos.x, controllablePos.y));
            
            // Remove this position from available spawn positions to avoid overlap
            spawnPositions.Remove(controllablePos);
            walkablePositions.Remove(controllablePos);
        }
        
		// Spawn rabbit spawners at random positions
		if (_rabbitSpawnerCount > 0 && spawnPositions.Count > 0)
		{
			int spawnersSpawned = 0;
			int attempts = 0;
			int maxAttempts = spawnPositions.Count * 2;

			while (spawnersSpawned < _rabbitSpawnerCount && spawnPositions.Count > 0 && attempts < maxAttempts)
			{
				attempts++;
				int index = Random.Range(0, spawnPositions.Count);
				Vector2Int spawnerPos = spawnPositions[index];

				levelData.RabbitSpawners.Add((spawnerPos.x, spawnerPos.y));
				spawnersSpawned++;

				// Remove so animals/items don't overlap
				spawnPositions.RemoveAt(index);
				walkablePositions.Remove(spawnerPos);
			}
		}

		// Spawn worm spawners at random positions (preferably on grass tiles)
		if (_wormSpawnerCount > 0 && spawnPositions.Count > 0)
		{
			// Collect grass positions for worm spawners
			List<Vector2Int> grassPositions = new List<Vector2Int>();
			foreach (Vector2Int pos in spawnPositions)
			{
				// Find the tile type for this position
				TileType tileType = TileType.Empty;
				foreach (var (tx, ty, tt) in levelData.Tiles)
				{
					if (tx == pos.x && ty == pos.y)
					{
						tileType = tt;
						break;
					}
				}

				if (tileType == TileType.Grass)
				{
					grassPositions.Add(pos);
				}
			}

			// Use grass positions if available, otherwise fall back to all spawn positions
			List<Vector2Int> wormSpawnerPositions = grassPositions.Count > 0 ? grassPositions : spawnPositions;

			int spawnersSpawned = 0;
			int attempts = 0;
			int maxAttempts = wormSpawnerPositions.Count * 2;

			while (spawnersSpawned < _wormSpawnerCount && wormSpawnerPositions.Count > 0 && attempts < maxAttempts)
			{
				attempts++;
				int index = Random.Range(0, wormSpawnerPositions.Count);
				Vector2Int spawnerPos = wormSpawnerPositions[index];

				levelData.WormSpawners.Add((spawnerPos.x, spawnerPos.y));
				spawnersSpawned++;

				// Remove so animals/items don't overlap
				wormSpawnerPositions.RemoveAt(index);
				spawnPositions.Remove(spawnerPos);
				walkablePositions.Remove(spawnerPos);
			}
		}

        // 2. Spawn predators in patches with predator dens
        if (_predatorNames != null && _predatorNames.Length > 0 && spawnPositions.Count > 0)
        {
            for (int patch = 0; patch < _predatorPatchCount; patch++)
            {
                if (spawnPositions.Count == 0)
                    break;
                
                // Pick a random center position for this patch (this will be the predator den location)
                // Use spawnPositions to avoid water tiles
                Vector2Int patchCenter = spawnPositions[Random.Range(0, spawnPositions.Count)];
                
                // Pick ONE predator type for this entire patch (all predators in this patch will be the same type)
                string patchPredatorType = _predatorNames[Random.Range(0, _predatorNames.Length)];
                
                // Spawn a predator den at the patch center with the selected predator type
                levelData.PredatorDens.Add((patchCenter.x, patchCenter.y, patchPredatorType));
                
                // Remove patch center from available positions (den occupies it)
                spawnPositions.Remove(patchCenter);
                walkablePositions.Remove(patchCenter);
                
                // Spawn predators around the center
                List<Vector2Int> patchPositions = new List<Vector2Int>();
                
                // Collect positions within patch radius
                for (int dx = -_predatorPatchRadius; dx <= _predatorPatchRadius; dx++)
                {
                    for (int dy = -_predatorPatchRadius; dy <= _predatorPatchRadius; dy++)
                    {
                        Vector2Int candidatePos = patchCenter + new Vector2Int(dx, dy);
                        
                        // Check if position is valid and walkable
                        if (candidatePos.x >= 0 && candidatePos.x < _levelWidth &&
                            candidatePos.y >= 0 && candidatePos.y < _levelHeight)
                        {
                            // Find the tile type for this position
                            TileType tileType = TileType.Empty;
                            foreach (var (tx, ty, tt) in levelData.Tiles)
                            {
                                if (tx == candidatePos.x && ty == candidatePos.y)
                                {
                                    tileType = tt;
                                    break;
                                }
                            }
                            
                            // Only add positions that are not obstacles and not water
                            if (tileType != TileType.Obstacle && tileType != TileType.Water)
                            {
                                patchPositions.Add(candidatePos);
                            }
                        }
                    }
                }
                
                // Shuffle patch positions and spawn predators (all of the same type for this patch)
                for (int i = 0; i < patchPositions.Count && i < _predatorsPerPatch; i++)
                {
                    int randomIndex = Random.Range(i, patchPositions.Count);
                    Vector2Int predatorPos = patchPositions[randomIndex];
                    
                    // Swap to avoid duplicates
                    Vector2Int temp = patchPositions[i];
                    patchPositions[i] = patchPositions[randomIndex];
                    patchPositions[randomIndex] = temp;
                    
                    // Use the same predator type for all predators in this patch
                    levelData.Animals.Add((patchPredatorType, predatorPos.x, predatorPos.y, 1));
                    
                    // Remove from available positions
                    walkablePositions.Remove(predatorPos);
                    if (spawnPositions.Contains(predatorPos))
                    {
                        spawnPositions.Remove(predatorPos);
                    }
                }
            }
        }
        
        // 3. Spawn rabbits randomly throughout the world
        if (_rabbitCount > 0 && spawnPositions.Count > 0)
        {
            int rabbitsSpawned = 0;
            int attempts = 0;
            int maxAttempts = spawnPositions.Count * 2;
            
            while (rabbitsSpawned < _rabbitCount && spawnPositions.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int rabbitPos = spawnPositions[Random.Range(0, spawnPositions.Count)];
                
                // Spawn a group of rabbits at this position
                levelData.Animals.Add(("Rabbit", rabbitPos.x, rabbitPos.y, _rabbitsPerGroup));
                rabbitsSpawned += _rabbitsPerGroup;
                
                // Remove from available positions
                spawnPositions.Remove(rabbitPos);
            }
        }
        
        // 4. Spawn food items randomly
        if (_foodItemCount > 0 && spawnPositions.Count > 0)
        {
            int foodSpawned = 0;
            int attempts = 0;
            int maxAttempts = spawnPositions.Count * 2;
            
            while (foodSpawned < _foodItemCount && spawnPositions.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int foodPos = spawnPositions[Random.Range(0, spawnPositions.Count)];
                
                levelData.Items.Add(("Food", foodPos.x, foodPos.y));
                levelData.FoodCount++;
                foodSpawned++;
                
                // Remove from available positions
                spawnPositions.Remove(foodPos);
            }
        }
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
            Debug.LogWarning("ProceduralLevelLoader: AnimalManager instance not found! Cannot set up camera follow.");
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
            Debug.LogWarning("ProceduralLevelLoader: No controllable animal found! Camera will not follow.");
            return;
        }

        // Find the Cinemachine Virtual Camera in the scene
        CinemachineVirtualCamera virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        
        if (virtualCamera == null)
        {
            Debug.LogWarning("ProceduralLevelLoader: CinemachineVirtualCamera not found in scene! Camera will not follow.");
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

        Debug.Log($"ProceduralLevelLoader: Virtual camera set to follow controllable animal at {controllableAnimal.GridPosition}");
    }

    /// <summary>
    /// Forces A* graphs to rebuild and aligns GridGraph node walkability with EnvironmentManager.
    /// </summary>
    private void RefreshAStarGraphs()
    {
        if (AstarPath.active == null)
        {
            Debug.LogWarning("ProceduralLevelLoader: AstarPath.active is null. A* Pathfinding graph was not re-scanned.");
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
                bool walk = EnvironmentManager.Instance.IsValidPosition(grid) && EnvironmentManager.Instance.IsWalkable(grid);
                node.Walkable = walk;
            });

            // Connected components are handled by the hierarchical graph automatically in recent versions
        }

        // Ensure any pending work is completed
        AstarPath.active.FlushWorkItems();
        AstarPath.active.FlushGraphUpdates();
        Debug.Log("ProceduralLevelLoader: A* Pathfinding graph force-refreshed and nodes synced with EnvironmentManager.");
    }
}

