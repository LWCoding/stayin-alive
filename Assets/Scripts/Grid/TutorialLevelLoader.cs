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
    
    [Header("Rock Patch")]
    [Tooltip("Start point for rock patch rectangle (set x to -1 to disable)")]
    [SerializeField] private Vector2Int _rockPatchStart = new Vector2Int(-1, 5);
    [Tooltip("End point for rock patch rectangle")]
    [SerializeField] private Vector2Int _rockPatchEnd = new Vector2Int(-1, 5);
    
    [Header("Procedural Generation (Upper Area)")]
    [Tooltip("Y level above which Perlin noise generation is applied (set to -1 to disable)")]
    [SerializeField] private int _proceduralYThreshold = 15;
    
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
    
    [Tooltip("Random seed for Perlin noise generation (0 = random each time)")]
    [SerializeField] private int _perlinSeed = 0;
    
    [Header("Procedural Spawn Settings")]
    [Tooltip("List of interactables to spawn randomly in procedural area")]
    [SerializeField] private List<InteractableSpawnConfig> _proceduralInteractableSpawnConfigs = new List<InteractableSpawnConfig>();
    
    [Tooltip("List of items to spawn randomly in procedural area")]
    [SerializeField] private List<ItemSpawnConfig> _proceduralItemSpawnConfigs = new List<ItemSpawnConfig>();
    
    [Tooltip("Number of predator patches to spawn in procedural area")]
    [SerializeField] private int _proceduralPredatorPatchCount = 1;
    
    [Tooltip("Number of predators per patch in procedural area")]
    [SerializeField] private int _proceduralPredatorsPerPatch = 2;
    
    [Tooltip("Radius of predator patches in procedural area")]
    [SerializeField] private int _proceduralPredatorPatchRadius = 3;
    
    [Tooltip("Names of predator animals for procedural area")]
    [SerializeField] private string[] _proceduralPredatorNames = new string[] { "Wolf", "Hawk" };

	[System.Serializable]
	public struct InteractableSpawnConfig
	{
		[Tooltip("Type of interactable to spawn")]
		public InteractableType interactableType;
		
		[Tooltip("Number of this interactable type to spawn")]
		[Min(0)]
		public int count;
		
		[Tooltip("Predator type (only used for PredatorDen)")]
		public string predatorType;
	}

	[System.Serializable]
	public struct ItemSpawnConfig
	{
		[Tooltip("Type of item to spawn")]
		public ItemId itemType;
		
		[Tooltip("Number of this item type to spawn")]
		[Min(0)]
		public int count;
	}

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

    [Header("Bush Placements")]
    [Tooltip("Manually placed bushes")]
    [SerializeField] private List<Vector2Int> _bushPositions = new List<Vector2Int>();

    [Header("Grass Placements")]
    [Tooltip("Manually placed grass interactables")]
    [SerializeField] private List<Vector2Int> _grassPositions = new List<Vector2Int>();

    [Header("Tree Placements")]
    [Tooltip("Manually placed trees")]
    [SerializeField] private List<Vector2Int> _treePositions = new List<Vector2Int>();

    [Header("Item Placements")]
    [Tooltip("Manually placed items")]
    [SerializeField] private List<ItemPlacement> _itemPlacements = new List<ItemPlacement>();

    [System.Serializable]
    public class ItemPlacement
    {
        [Tooltip("Type of item")]
        public ItemId itemType = ItemId.Grass;
        
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
            InteractableManager.Instance.SpawnInteractablesFromLevelData(levelData.Interactables);
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
                    WorkerManager.Instance.CreateWorker(_controllableAnimalPosition);
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
		
		// Extract and spawn predator dens from Interactables list
		if (levelData.Interactables != null)
		{
			List<(int x, int y, string predatorType)> predatorDens = new List<(int x, int y, string predatorType)>();
			foreach (var interactable in levelData.Interactables)
			{
				if (interactable.Type == InteractableType.PredatorDen)
				{
					predatorDens.Add((interactable.X, interactable.Y, interactable.PredatorType));
				}
			}
			PredatorAnimal.SpawnPredatorDensFromLevelData(predatorDens);
		}

        // Spawn items using ItemManager
        if (ItemManager.Instance != null)
        {
            // Clear all existing items first
            ItemManager.Instance.ClearAllItems();
            
            // Spawn items from level data
            if (levelData.Items != null)
            {
                foreach (var (itemType, x, y) in levelData.Items)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
                    {
                        ItemManager.Instance.SpawnItem(itemType, gridPos);
                    }
                    else
                    {
                        Debug.LogWarning($"TutorialLevelLoader: Item '{itemType}' at ({x}, {y}) is out of bounds!");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("TutorialLevelLoader: ItemManager instance not found! Items will not be spawned.");
        }

        // Reset stored den food
        if (DenSystemManager.Instance != null)
        {
            DenSystemManager.Instance.ResetDenFood();
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

		int interactableCount = levelData.Interactables != null ? levelData.Interactables.Count : 0;
		Debug.Log($"TutorialLevelLoader: Successfully loaded tutorial level with {levelData.Tiles.Count} tiles, {levelData.Animals.Count} animals, {levelData.Items.Count} items, and {interactableCount} interactables");
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
        
        // Apply rock patch between two points (if enabled)
        if (_rockPatchStart.x >= 0 && _rockPatchEnd.x >= 0)
        {
            int startX = Mathf.Min(_rockPatchStart.x, _rockPatchEnd.x);
            int endX = Mathf.Max(_rockPatchStart.x, _rockPatchEnd.x);
            int startY = Mathf.Min(_rockPatchStart.y, _rockPatchEnd.y);
            int endY = Mathf.Max(_rockPatchStart.y, _rockPatchEnd.y);
            
            int rockCount = 0;
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (IsValidGridPosition(x, y))
                    {
                        // Find and update the tile at this position to Obstacle
                        for (int i = 0; i < levelData.Tiles.Count; i++)
                        {
                            var (tileX, tileY, _) = levelData.Tiles[i];
                            if (tileX == x && tileY == y)
                            {
                                levelData.Tiles[i] = (x, y, TileType.Obstacle);
                                rockCount++;
                                break;
                            }
                        }
                    }
                }
            }
            Debug.Log($"TutorialLevelLoader: Created rock patch from ({startX}, {startY}) to ({endX}, {endY}) with {rockCount} tiles");
        }
        
        // Apply Perlin noise generation to tiles above Y threshold (if enabled)
        if (_proceduralYThreshold >= 0)
        {
            // Set random seed if specified
            if (_perlinSeed != 0)
            {
                Random.InitState(_perlinSeed);
            }
            
            // Generate random offsets for Perlin noise
            float terrainOffsetX = Random.Range(0f, 1000f);
            float terrainOffsetY = Random.Range(0f, 1000f);
            float obstacleOffsetX = Random.Range(0f, 1000f);
            float obstacleOffsetY = Random.Range(0f, 1000f);
            
            int proceduralTilesGenerated = 0;
            
            // Apply Perlin noise only to tiles at or above the Y threshold
            for (int i = 0; i < levelData.Tiles.Count; i++)
            {
                var (x, y, currentTileType) = levelData.Tiles[i];
                
                // Only apply to tiles at or above threshold
                if (y < _proceduralYThreshold)
                    continue;
                
                // Skip border walls
                if (_addBorderWalls && (x == 0 || x == _levelWidth - 1 || y == 0 || y == _levelHeight - 1))
                    continue;
                
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
                TileType newTileType;
                if (obstacleNoise > _obstacleThreshold)
                {
                    newTileType = TileType.Obstacle;
                }
                else if (terrainNoise < _waterThreshold)
                {
                    newTileType = TileType.Water;
                }
                else if (terrainNoise < _grassThreshold)
                {
                    newTileType = TileType.Grass;
                }
                else
                {
                    newTileType = TileType.Empty;
                }
                
                // Update the tile
                levelData.Tiles[i] = (x, y, newTileType);
                proceduralTilesGenerated++;
            }
            
            Debug.Log($"TutorialLevelLoader: Generated {proceduralTilesGenerated} procedural tiles above Y={_proceduralYThreshold} using Perlin noise");
        }

        // Initialize lists BEFORE any spawns are added
        levelData.Animals = new List<(string animalName, int x, int y, int count)>();
        levelData.Items = new List<(ItemId itemType, int x, int y)>();
        levelData.Interactables = new List<InteractableData>();
        levelData.FoodCount = 0;
        
        // Generate spawns in procedural area (after lists are initialized)
        if (_proceduralYThreshold >= 0)
        {
            GenerateProceduralSpawns(levelData);
        }

        // Place den
        if (IsValidGridPosition(_denPosition.x, _denPosition.y))
        {
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
                levelData.Interactables.Add(new InteractableData(InteractableType.RabbitSpawner, pos.x, pos.y));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid rabbit spawner position ({pos.x}, {pos.y}). Skipping.");
            }
        }

        // Place bushes
        foreach (var pos in _bushPositions)
        {
            if (IsValidGridPosition(pos.x, pos.y))
            {
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
                levelData.Interactables.Add(new InteractableData(InteractableType.Grass, pos.x, pos.y));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid grass position ({pos.x}, {pos.y}). Skipping.");
            }
        }

        // Place trees
        foreach (var pos in _treePositions)
        {
            if (IsValidGridPosition(pos.x, pos.y))
            {
                levelData.Interactables.Add(new InteractableData(InteractableType.Tree, pos.x, pos.y));
            }
            else
            {
                Debug.LogWarning($"TutorialLevelLoader: Invalid tree position ({pos.x}, {pos.y}). Skipping.");
            }
        }

        // Place items
        foreach (var placement in _itemPlacements)
        {
            if (IsValidGridPosition(placement.position.x, placement.position.y))
            {
                levelData.Items.Add((placement.itemType, placement.position.x, placement.position.y));
                
                // Note: Food items are tracked separately - if you have a Food item type, uncomment this
                // if (placement.itemType == ItemType.Food)
                // {
                //     levelData.FoodCount++;
                // }
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
    /// Gets the tile type at a specific position from level data.
    /// </summary>
    private TileType GetTileTypeAt(LevelData levelData, int x, int y)
    {
        foreach (var (tx, ty, tt) in levelData.Tiles)
        {
            if (tx == x && ty == y)
            {
                return tt;
            }
        }
        return TileType.Empty;
    }

    /// <summary>
    /// Checks if a position is already occupied by an interactable or item.
    /// </summary>
    private bool IsOccupied(Vector2Int pos, LevelData levelData)
    {
        foreach (var interactable in levelData.Interactables)
        {
            if (interactable.X == pos.x && interactable.Y == pos.y)
                return true;
        }
        foreach (var (_, ix, iy) in levelData.Items)
        {
            if (ix == pos.x && iy == pos.y)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Spawns interactables based on the procedural spawn configuration list.
    /// </summary>
    private void SpawnProceduralInteractablesFromConfig(LevelData levelData, List<Vector2Int> proceduralPositions, List<Vector2Int> proceduralGrassPositions)
    {
        foreach (var config in _proceduralInteractableSpawnConfigs)
        {
            if (config.count <= 0)
                continue;

            // Determine if this interactable requires grass tiles
            bool requiresGrass = config.interactableType == InteractableType.Bush ||
                                 config.interactableType == InteractableType.Grass ||
                                 config.interactableType == InteractableType.Tree ||
                                 config.interactableType == InteractableType.BeeTree;

            // Use appropriate position list
            List<Vector2Int> validPositions = requiresGrass 
                ? new List<Vector2Int>(proceduralGrassPositions) 
                : new List<Vector2Int>(proceduralPositions);

            // Filter out occupied positions
            validPositions.RemoveAll(pos => IsOccupied(pos, levelData));

            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"TutorialLevelLoader: No valid positions available for spawning {config.interactableType} in procedural area!");
                continue;
            }

            // Spawn the interactables
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = validPositions.Count * 2;

            while (spawned < config.count && validPositions.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                int index = Random.Range(0, validPositions.Count);
                Vector2Int spawnPos = validPositions[index];

                // Add to Interactables list
                levelData.Interactables.Add(new InteractableData(config.interactableType, spawnPos.x, spawnPos.y, config.predatorType));

                spawned++;
                validPositions.RemoveAt(index);
                proceduralPositions.Remove(spawnPos);
                proceduralGrassPositions.Remove(spawnPos);
            }

            if (spawned < config.count)
            {
                Debug.LogWarning($"TutorialLevelLoader: Only spawned {spawned} of {config.count} {config.interactableType} interactables in procedural area!");
            }
        }
    }

    /// <summary>
    /// Spawns items based on the procedural spawn configuration list.
    /// </summary>
    private void SpawnProceduralItemsFromConfig(LevelData levelData, List<Vector2Int> proceduralPositions, List<Vector2Int> proceduralGrassPositions)
    {
        foreach (var config in _proceduralItemSpawnConfigs)
        {
            if (config.count <= 0)
                continue;

            // Determine if this item requires grass tiles (e.g., Sticks)
            bool requiresGrass = config.itemType == ItemId.Sticks;

            // Use appropriate position list
            List<Vector2Int> validPositions = requiresGrass 
                ? new List<Vector2Int>(proceduralGrassPositions) 
                : new List<Vector2Int>(proceduralPositions);

            // Filter out occupied positions
            validPositions.RemoveAll(pos => IsOccupied(pos, levelData));

            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"TutorialLevelLoader: No valid positions available for spawning {config.itemType} items in procedural area!");
                continue;
            }

            // Spawn the items
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = validPositions.Count * 2;

            while (spawned < config.count && validPositions.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                int index = Random.Range(0, validPositions.Count);
                Vector2Int spawnPos = validPositions[index];

                levelData.Items.Add((config.itemType, spawnPos.x, spawnPos.y));
                
                // Note: Food items are tracked separately - if you have a Food item type, uncomment this
                // if (config.itemType == ItemType.Food)
                // {
                //     levelData.FoodCount++;
                // }

                spawned++;
                validPositions.RemoveAt(index);
                proceduralPositions.Remove(spawnPos);
                proceduralGrassPositions.Remove(spawnPos);
            }

            if (spawned < config.count)
            {
                Debug.LogWarning($"TutorialLevelLoader: Only spawned {spawned} of {config.count} {config.itemType} items in procedural area!");
            }
        }
    }

    /// <summary>
    /// Generates procedural spawns (dens, interactables, items) in the area above Y threshold.
    /// </summary>
    private void GenerateProceduralSpawns(LevelData levelData)
    {
        // Collect walkable positions in procedural area only
        List<Vector2Int> proceduralPositions = new List<Vector2Int>();
        List<Vector2Int> proceduralGrassPositions = new List<Vector2Int>();
        
        foreach (var (x, y, tileType) in levelData.Tiles)
        {
            // Only consider positions at or above Y threshold
            if (y < _proceduralYThreshold)
                continue;
            
            // Skip obstacles
            if (tileType == TileType.Obstacle)
                continue;
            
            Vector2Int pos = new Vector2Int(x, y);
            
            // Add to walkable positions
            if (tileType != TileType.Water)
            {
                proceduralPositions.Add(pos);
            }
            
            // Track grass positions separately
            if (tileType == TileType.Grass)
            {
                proceduralGrassPositions.Add(pos);
            }
        }
        
        if (proceduralPositions.Count == 0)
        {
            Debug.LogWarning("TutorialLevelLoader: No walkable positions in procedural area. Skipping procedural spawns.");
            return;
        }
        
        Debug.Log($"TutorialLevelLoader: Found {proceduralPositions.Count} walkable positions in procedural area ({proceduralGrassPositions.Count} grass tiles)");
        
        // Spawn interactables from config list
        SpawnProceduralInteractablesFromConfig(levelData, proceduralPositions, proceduralGrassPositions);
        
        // Spawn items from config list
        SpawnProceduralItemsFromConfig(levelData, proceduralPositions, proceduralGrassPositions);
        
        // Spawn predator patches with dens
        if (_proceduralPredatorNames != null && _proceduralPredatorNames.Length > 0)
        {
            // Create a list to ensure at least one of each predator type is assigned
            List<string> predatorTypesToAssign = new List<string>(_proceduralPredatorNames);
            
            // Shuffle the list to randomize which patch gets which type
            for (int i = 0; i < predatorTypesToAssign.Count; i++)
            {
                int randomIndex = Random.Range(i, predatorTypesToAssign.Count);
                string temp = predatorTypesToAssign[i];
                predatorTypesToAssign[i] = predatorTypesToAssign[randomIndex];
                predatorTypesToAssign[randomIndex] = temp;
            }
            
            // Ensure we have enough patches for all predator types
            int minPatchCount = Mathf.Max(_proceduralPredatorPatchCount, _proceduralPredatorNames.Length);
            
            for (int patch = 0; patch < minPatchCount && proceduralPositions.Count > 0; patch++)
            {
                // Find center for predator den
                Vector2Int patchCenter = Vector2Int.zero;
                bool foundCenter = false;
                
                for (int attempt = 0; attempt < 100 && proceduralPositions.Count > 0; attempt++)
                {
                    int index = Random.Range(0, proceduralPositions.Count);
                    Vector2Int pos = proceduralPositions[index];
                    
                    if (!IsOccupied(pos, levelData))
                    {
                        patchCenter = pos;
                        foundCenter = true;
                        break;
                    }
                }
                
                if (!foundCenter)
                    continue;
                
                // Pick predator type for this patch
                string patchPredatorType = patch < predatorTypesToAssign.Count 
                    ? predatorTypesToAssign[patch] 
                    : _proceduralPredatorNames[Random.Range(0, _proceduralPredatorNames.Length)];
                
                // Spawn predator den
                levelData.Interactables.Add(new InteractableData(InteractableType.PredatorDen, patchCenter.x, patchCenter.y, patchPredatorType));
                proceduralPositions.Remove(patchCenter);
                
                // Spawn predators around den
                List<Vector2Int> patchPositions = new List<Vector2Int>();
                for (int dx = -_proceduralPredatorPatchRadius; dx <= _proceduralPredatorPatchRadius; dx++)
                {
                    for (int dy = -_proceduralPredatorPatchRadius; dy <= _proceduralPredatorPatchRadius; dy++)
                    {
                        Vector2Int candidatePos = patchCenter + new Vector2Int(dx, dy);
                        
                        if (candidatePos.y >= _proceduralYThreshold && proceduralPositions.Contains(candidatePos))
                        {
                            patchPositions.Add(candidatePos);
                        }
                    }
                }
                
                for (int i = 0; i < Mathf.Min(patchPositions.Count, _proceduralPredatorsPerPatch); i++)
                {
                    int randomIndex = Random.Range(i, patchPositions.Count);
                    Vector2Int predatorPos = patchPositions[randomIndex];
                    Vector2Int temp = patchPositions[i];
                    patchPositions[i] = patchPositions[randomIndex];
                    patchPositions[randomIndex] = temp;
                    
                    levelData.Animals.Add((patchPredatorType, predatorPos.x, predatorPos.y, 1));
                    proceduralPositions.Remove(predatorPos);
                }
            }
            
            // Log which predator types were spawned
            var spawnedTypes = new System.Collections.Generic.HashSet<string>();
            int predatorDenCount = 0;
            foreach (var interactable in levelData.Interactables)
            {
                if (interactable.Type == InteractableType.PredatorDen)
                {
                    predatorDenCount++;
                    if (!string.IsNullOrEmpty(interactable.PredatorType))
                    {
                        spawnedTypes.Add(interactable.PredatorType);
                    }
                }
            }
            Debug.Log($"TutorialLevelLoader: Spawned {predatorDenCount} predator patches. Types: {string.Join(", ", spawnedTypes)}");
        }
        
        Debug.Log($"TutorialLevelLoader: Spawned procedural content above Y={_proceduralYThreshold}");
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

                // Also check if it's a water tile - mark water as non-walkable in the base graph
                // Animals that can swim handle water logic in their own movement checks
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
        Debug.Log("TutorialLevelLoader: A* Pathfinding graph force-refreshed and nodes synced with EnvironmentManager.");
    }
}

