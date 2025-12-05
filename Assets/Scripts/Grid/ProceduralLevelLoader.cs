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

	[Header("Interactable Spawn Settings")]
	[Tooltip("List of interactables to spawn randomly on the map")]
	[SerializeField] private List<InteractableSpawnConfig> _interactableSpawnConfigs = new List<InteractableSpawnConfig>();

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

    [Header("Item Spawn Settings")]
	[Tooltip("List of items to spawn randomly on the map")]
	[SerializeField] private List<ItemSpawnConfig> _itemSpawnConfigs = new List<ItemSpawnConfig>();

	[System.Serializable]
	public struct ItemSpawnConfig
	{
		[Tooltip("Type of item to spawn")]
		public ItemType itemType;
		
		[Tooltip("Number of this item type to spawn")]
		[Min(0)]
		public int count;
	}

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
			InteractableManager.Instance.SpawnInteractablesFromLevelData(levelData.Interactables);
        }
        else
        {
			Debug.LogWarning("ProceduralLevelLoader: InteractableManager instance not found! Interactables will not be spawned.");
        }
		
        // Spawn animals using AnimalManager (before predator dens so den prefabs are registered)
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.SpawnAnimalsFromLevelData(levelData.Animals);
            
            // Spawn unassigned workers at the player's position
            if (DenSystemManager.Instance != null && !string.IsNullOrEmpty(_controllableAnimalName))
            {
                ControllableAnimal player = AnimalManager.Instance.GetPlayer();
                if (player != null)
                {
                    for (int i = 0; i < _controllableAnimalCount; i++)
                    {
                        DenSystemManager.Instance.CreateWorkerAtPosition(player.GridPosition);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("ProceduralLevelLoader: AnimalManager instance not found! Animals will not be spawned.");
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
                        Debug.LogWarning($"ProceduralLevelLoader: Item '{itemType}' at ({x}, {y}) is out of bounds!");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("ProceduralLevelLoader: ItemManager instance not found! Items will not be spawned.");
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
		Debug.Log($"ProceduralLevelLoader: Successfully generated level with {levelData.Tiles.Count} tiles, {levelData.Animals.Count} animals, {levelData.Items.Count} items, and {interactableCount} interactables");
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
		levelData.Items = new List<(ItemType itemType, int x, int y)>();
		levelData.Interactables = new List<InteractableData>();
        levelData.FoodCount = 0;

        // Generate spawn positions for animals, dens, and items
        GenerateSpawnPositions(levelData);

        Debug.Log($"ProceduralLevelLoader: Generated level with {levelData.Tiles.Count} tiles, size: {levelData.Width}x{levelData.Height}");
        return levelData;
    }

    /// <summary>
    /// Checks if a position is already occupied by any interactable.
    /// </summary>
    private bool IsPositionOccupiedByInteractable(Vector2Int pos, LevelData levelData)
    {
        foreach (var interactable in levelData.Interactables)
        {
            if (interactable.X == pos.x && interactable.Y == pos.y)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a position is already occupied by any item.
    /// </summary>
    private bool IsPositionOccupiedByItem(Vector2Int pos, LevelData levelData)
    {
        foreach (var (itemType, x, y) in levelData.Items)
        {
            if (x == pos.x && y == pos.y)
                return true;
        }
        return false;
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
    /// Spawns interactables based on the spawn configuration list.
    /// </summary>
    private void SpawnInteractablesFromConfig(LevelData levelData, List<Vector2Int> spawnPositions, List<Vector2Int> walkablePositions)
    {
        foreach (var config in _interactableSpawnConfigs)
        {
            if (config.count <= 0)
                continue;

            // Determine if this interactable requires grass tiles
            bool requiresGrass = config.interactableType == InteractableType.Bush ||
                                 config.interactableType == InteractableType.Grass ||
                                 config.interactableType == InteractableType.Tree;

            // Check if this is a multi-tile interactable
            bool isMultiTile = config.interactableType == InteractableType.GrassPatch;
            int patchSize = isMultiTile ? 3 : 1; // GrassPatch is 3x3

            // Collect valid positions for this interactable type
            List<Vector2Int> validPositions = new List<Vector2Int>();
            foreach (Vector2Int pos in spawnPositions)
            {
                // For multi-tile interactables, check if the entire area is available
                if (isMultiTile)
                {
                    // Calculate all positions that would be occupied by this patch
                    List<Vector2Int> occupiedPositions = GetPatchPositions(pos, patchSize);
                    
                    // Check if all positions in the patch are valid and available
                    bool areaAvailable = true;
                    foreach (Vector2Int occupiedPos in occupiedPositions)
                    {
                        // Check if position is valid
                        if (occupiedPos.x < 0 || occupiedPos.x >= _levelWidth ||
                            occupiedPos.y < 0 || occupiedPos.y >= _levelHeight)
                        {
                            areaAvailable = false;
                            break;
                        }
                        
                        // Check if position is already occupied by another interactable
                        if (IsPositionOccupiedByInteractable(occupiedPos, levelData))
                        {
                            areaAvailable = false;
                            break;
                        }
                        
                        // Check tile type (no water or obstacles)
                        TileType tileType = GetTileTypeAt(levelData, occupiedPos.x, occupiedPos.y);
                        if (tileType == TileType.Water || tileType == TileType.Obstacle)
                        {
                            areaAvailable = false;
                            break;
                        }
                    }
                    
                    if (!areaAvailable)
                        continue;
                }
                else
                {
                    // Single-tile interactable
                    // Skip if position is already occupied
                    if (IsPositionOccupiedByInteractable(pos, levelData))
                        continue;

                    // If this interactable requires grass, check tile type
                    if (requiresGrass)
                    {
                        TileType tileType = GetTileTypeAt(levelData, pos.x, pos.y);
                        if (tileType != TileType.Grass)
                            continue;
                    }
                }

                validPositions.Add(pos);
            }

            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"ProceduralLevelLoader: No valid positions available for spawning {config.interactableType}!");
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

                // Remove occupied positions from available spawn positions
                if (isMultiTile)
                {
                    List<Vector2Int> occupiedPositions = GetPatchPositions(spawnPos, patchSize);
                    foreach (Vector2Int occupiedPos in occupiedPositions)
                    {
                        spawnPositions.Remove(occupiedPos);
                        walkablePositions.Remove(occupiedPos);
                    }
                }
                else
                {
                    spawnPositions.Remove(spawnPos);
                    walkablePositions.Remove(spawnPos);
                }

                spawned++;
                validPositions.RemoveAt(index);
            }

            if (spawned < config.count)
            {
                Debug.LogWarning($"ProceduralLevelLoader: Only spawned {spawned} of {config.count} {config.interactableType} interactables!");
            }
        }
    }
    
    /// <summary>
    /// Gets all grid positions occupied by a patch of the specified size centered at the given position.
    /// </summary>
    /// <param name="centerPos">Center position of the patch</param>
    /// <param name="patchSize">Size of the patch (width and height in grid tiles)</param>
    /// <returns>List of all positions occupied by the patch</returns>
    private List<Vector2Int> GetPatchPositions(Vector2Int centerPos, int patchSize)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        int halfSize = patchSize / 2;
        
        // Center the patch around the center position
        for (int x = centerPos.x - halfSize; x <= centerPos.x + halfSize; x++)
        {
            for (int y = centerPos.y - halfSize; y <= centerPos.y + halfSize; y++)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }
        
        return positions;
    }

    /// <summary>
    /// Spawns items based on the spawn configuration list.
    /// </summary>
    private void SpawnItemsFromConfig(LevelData levelData, List<Vector2Int> spawnPositions)
    {
        foreach (var config in _itemSpawnConfigs)
        {
            if (config.count <= 0)
                continue;

            // Determine if this item requires grass tiles (e.g., Sticks)
            bool requiresGrass = config.itemType == ItemType.Sticks;

            // Collect valid positions for this item type
            List<Vector2Int> validPositions = new List<Vector2Int>();
            foreach (Vector2Int pos in spawnPositions)
            {
                // Skip if position is already occupied
                if (IsPositionOccupiedByInteractable(pos, levelData) || IsPositionOccupiedByItem(pos, levelData))
                    continue;

                // If this item requires grass, check tile type
                if (requiresGrass)
                {
                    TileType tileType = GetTileTypeAt(levelData, pos.x, pos.y);
                    if (tileType != TileType.Grass)
                        continue;
                }

                validPositions.Add(pos);
            }

            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"ProceduralLevelLoader: No valid positions available for spawning {config.itemType} items!");
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
                spawnPositions.Remove(spawnPos);
            }

            if (spawned < config.count)
            {
                Debug.LogWarning($"ProceduralLevelLoader: Only spawned {spawned} of {config.count} {config.itemType} items!");
            }
        }
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
        
		// Spawn interactables from config list
		SpawnInteractablesFromConfig(levelData, spawnPositions, walkablePositions);

		// Spawn items from config list
		SpawnItemsFromConfig(levelData, spawnPositions);

        // 2. Spawn predators in patches with predator dens
        if (_predatorNames != null && _predatorNames.Length > 0 && spawnPositions.Count > 0)
        {
            // Create a list to ensure at least one of each predator type is assigned
            List<string> predatorTypesToAssign = new List<string>(_predatorNames);
            
            // Shuffle the list to randomize which patch gets which type
            for (int i = 0; i < predatorTypesToAssign.Count; i++)
            {
                int randomIndex = Random.Range(i, predatorTypesToAssign.Count);
                string temp = predatorTypesToAssign[i];
                predatorTypesToAssign[i] = predatorTypesToAssign[randomIndex];
                predatorTypesToAssign[randomIndex] = temp;
            }
            
            for (int patch = 0; patch < _predatorPatchCount; patch++)
            {
                if (spawnPositions.Count == 0)
                    break;
                
                // Pick a random center position for this patch (this will be the predator den location)
                // Use spawnPositions to avoid water tiles, but also check for existing interactables
                Vector2Int patchCenter = Vector2Int.zero;
                bool foundValidPosition = false;
                int patchAttempts = 0;
                int maxPatchAttempts = spawnPositions.Count * 2;
                
                while (patchAttempts < maxPatchAttempts && spawnPositions.Count > 0)
                {
                    patchCenter = spawnPositions[Random.Range(0, spawnPositions.Count)];
                    patchAttempts++;
                    
                    if (!IsPositionOccupiedByInteractable(patchCenter, levelData))
                    {
                        foundValidPosition = true;
                        break;
                    }
                }
                
                if (!foundValidPosition)
                {
                    continue; // Skip this patch if no valid position found
                }
                
                // Pick ONE predator type for this entire patch (all predators in this patch will be the same type)
                // First ensure at least one of each type is assigned, then randomly assign remaining patches
                string patchPredatorType;
                if (patch < predatorTypesToAssign.Count)
                {
                    // Assign one of each type for the first N patches (where N = number of predator types)
                    patchPredatorType = predatorTypesToAssign[patch];
                }
                else
                {
                    // For remaining patches, randomly assign any predator type
                    patchPredatorType = _predatorNames[Random.Range(0, _predatorNames.Length)];
                }
                
                // Spawn a predator den at the patch center with the selected predator type
                levelData.Interactables.Add(new InteractableData(InteractableType.PredatorDen, patchCenter.x, patchCenter.y, patchPredatorType));
                
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
        
        // 3. Spawn controllable animal at a safe distance from spawners
        if (!string.IsNullOrEmpty(_controllableAnimalName) && spawnPositions.Count > 0)
        {
            const int MIN_DISTANCE_FROM_SPAWNERS = 10;
            Vector2Int controllablePos = Vector2Int.zero;
            bool foundValidPosition = false;
            int playerAttempts = 0;
            int maxPlayerAttempts = spawnPositions.Count * 3;
            
            // Try to find a position at least 10 tiles away from all rabbit spawners and predator dens
            while (playerAttempts < maxPlayerAttempts && spawnPositions.Count > 0)
            {
                int index = Random.Range(0, spawnPositions.Count);
                Vector2Int candidatePos = spawnPositions[index];
                playerAttempts++;
                
                bool tooCloseToSpawner = false;
                
                // Check distance to all interactables (rabbit spawners and predator dens)
                foreach (var interactable in levelData.Interactables)
                {
                    if (interactable.Type == InteractableType.RabbitSpawner || interactable.Type == InteractableType.PredatorDen)
                    {
                        int distance = Mathf.Abs(candidatePos.x - interactable.X) + Mathf.Abs(candidatePos.y - interactable.Y);
                        if (distance < MIN_DISTANCE_FROM_SPAWNERS)
                        {
                            tooCloseToSpawner = true;
                            break;
                        }
                    }
                }
                
                // If position is valid (far enough from spawners), use it
                if (!tooCloseToSpawner)
                {
                    controllablePos = candidatePos;
                    foundValidPosition = true;
                    break;
                }
            }
            
            // If we couldn't find a position with the distance requirement, just use any available position
            if (!foundValidPosition && spawnPositions.Count > 0)
            {
                controllablePos = spawnPositions[Random.Range(0, spawnPositions.Count)];
                foundValidPosition = true;
                Debug.LogWarning($"ProceduralLevelLoader: Could not find player spawn position {MIN_DISTANCE_FROM_SPAWNERS} tiles away from spawners. Using closest available position.");
            }
            
            if (foundValidPosition)
            {
                levelData.Animals.Add((_controllableAnimalName, controllablePos.x, controllablePos.y, 1));
                
                // Place den at the same position as the controllable animal
                levelData.Interactables.Add(new InteractableData(InteractableType.Den, controllablePos.x, controllablePos.y));
                
                // Remove this position from available spawn positions to avoid overlap
                spawnPositions.Remove(controllablePos);
                walkablePositions.Remove(controllablePos);
                
                Debug.Log($"ProceduralLevelLoader: Spawned player at ({controllablePos.x}, {controllablePos.y})");
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
    /// Marks water tiles and obstacles as non-walkable in the base graph.
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
        Debug.Log("ProceduralLevelLoader: A* Pathfinding graph force-refreshed and nodes synced with EnvironmentManager.");
    }
}

