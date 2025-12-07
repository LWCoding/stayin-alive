using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A den for hawk predators that periodically spawns sticks in an area around it.
/// Spawn cadence is turn-based and sticks only spawn on walkable tiles.
/// </summary>
public class HawkDen : PredatorDen
{
	[Header("Stick Spawner Settings")]
	[Tooltip("Type of the sticks item to spawn.")]
	[SerializeField] private ItemType _sticksItemType = ItemType.Sticks;
	[Tooltip("Number of turns between spawn attempts.")]
	[SerializeField] private int _turnsBetweenSpawns = 10;
	[Tooltip("Minimum distance from the den that sticks can spawn (in grid tiles).")]
	[SerializeField] private int _minSpawnRadius = 2;
	[Tooltip("Maximum distance from the den that sticks can spawn (in grid tiles).")]
	[SerializeField] private int _maxSpawnRadius = 5;
	[Tooltip("Maximum number of sticks that can exist around this den at once.")]
	[SerializeField] private int _maxSticks = 3;

	[Header("Season Spawn Multipliers")]
	[Tooltip("Multiplier applied to spawn rate during Spring. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _springSpawnMultiplier = 1.0f;
	[Tooltip("Multiplier applied to spawn rate during Summer. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _summerSpawnMultiplier = 1.0f;
	[Tooltip("Multiplier applied to spawn rate during Fall. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _fallSpawnMultiplier = 1.0f;
	[Tooltip("Multiplier applied to spawn rate during Winter. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _winterSpawnMultiplier = 1.0f;

	private int _turnsSinceLastSpawn;
	private bool _spawnerInitialized;
	// Track all sticks spawned by this den
	private List<Item> _spawnedSticks = new List<Item>();

  public override string GetKnowledgeTitle() {
    return "HawkDen";
  }
  
	private void OnEnable()
	{
		SubscribeToTurnEvents();
	}

	private void Start()
	{
		EnsureSpawnerInitialization();
	}

	private void OnDisable()
	{
		UnsubscribeFromTurnEvents();
	}

	private void OnDestroy()
	{
		UnsubscribeFromTurnEvents();
	}

	/// <summary>
	/// Initializes the hawk den at the specified grid position.
	/// </summary>
	public override void Initialize(Vector2Int gridPosition)
	{
		base.Initialize(gridPosition, "Hawk");
		_turnsSinceLastSpawn = 0;
		_spawnerInitialized = true;
		_spawnedSticks.Clear();
	}

	/// <summary>
	/// Initializes the hawk den at the specified grid position with a specific predator type.
	/// </summary>
	public override void Initialize(Vector2Int gridPosition, string predatorType)
	{
		base.Initialize(gridPosition, predatorType);
		_turnsSinceLastSpawn = 0;
		_spawnerInitialized = true;
		_spawnedSticks.Clear();
	}

	private void SubscribeToTurnEvents()
	{
		if (TimeManager.Instance != null)
		{
			TimeManager.Instance.OnTurnAdvanced -= HandleTurnAdvanced;
			TimeManager.Instance.OnTurnAdvanced += HandleTurnAdvanced;
		}
	}

	private void UnsubscribeFromTurnEvents()
	{
		if (TimeManager.Instance != null)
		{
			TimeManager.Instance.OnTurnAdvanced -= HandleTurnAdvanced;
		}
	}

	private void EnsureSpawnerInitialization()
	{
		if (_spawnerInitialized)
		{
			return;
		}

		// If not initialized via Initialize(), try to get position from world position
		if (EnvironmentManager.Instance != null)
		{
			_gridPosition = EnvironmentManager.Instance.WorldToGridPosition(transform.position);
		}
		else
		{
			Vector3 pos = transform.position;
			_gridPosition = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
		}

		_turnsSinceLastSpawn = 0;
		_spawnerInitialized = true;
		_spawnedSticks.Clear();
	}

	private void HandleTurnAdvanced(int currentTurn)
	{
		if (!_spawnerInitialized || _turnsBetweenSpawns <= 0)
		{
			return;
		}

		// Reset on fresh level start
        if (currentTurn == 0)
        {
            _turnsSinceLastSpawn = 0;
			_spawnedSticks.Clear();
            EnsurePredatorPopulation();
            return;
        }

		// Clean up destroyed/null stick references
		CleanupDestroyedSticks();

		_turnsSinceLastSpawn++;

		// Get season-adjusted spawn rate (divide by multiplier: higher multiplier = fewer turns)
		float seasonMultiplier = GetSeasonMultiplier();
		int adjustedTurnsBetweenSpawns = Mathf.Max(1, Mathf.RoundToInt(_turnsBetweenSpawns / Mathf.Max(0.01f, seasonMultiplier)));

		if (_turnsSinceLastSpawn < adjustedTurnsBetweenSpawns)
		{
			return;
		}

		if (TrySpawnSticksItem())
		{
			_turnsSinceLastSpawn = 0;
		}

        EnsurePredatorPopulation();
	}

	/// <summary>
	/// Gets the spawn rate multiplier for the current season.
	/// </summary>
	private float GetSeasonMultiplier()
	{
		if (TimeManager.Instance == null)
		{
			return 1f;
		}

		switch (TimeManager.Instance.CurrentSeason)
		{
			case TimeManager.Season.Spring:
				return _springSpawnMultiplier;
			case TimeManager.Season.Summer:
				return _summerSpawnMultiplier;
			case TimeManager.Season.Fall:
				return _fallSpawnMultiplier;
			case TimeManager.Season.Winter:
				return _winterSpawnMultiplier;
			default:
				return 1f;
		}
	}

	private bool TrySpawnSticksItem()
	{
		if (EnvironmentManager.Instance == null)
		{
			Debug.LogWarning("HawkDen: EnvironmentManager instance not found. Cannot spawn sticks.");
			return false;
		}

		if (ItemManager.Instance == null)
		{
			Debug.LogWarning("HawkDen: ItemManager instance not found. Cannot spawn sticks.");
			return false;
		}

		// Clean up destroyed/null stick references before checking count
		CleanupDestroyedSticks();

		// Check if we're at max capacity
		if (_spawnedSticks.Count >= _maxSticks)
		{
			return false; // Already at max sticks, don't spawn more
		}

		// Get all valid walkable positions in the spawn area
		List<Vector2Int> validPositions = GetValidWalkablePositionsInArea();
		
		if (validPositions.Count == 0)
		{
			Debug.LogWarning($"HawkDen: No valid walkable positions found in spawn area around ({_gridPosition.x}, {_gridPosition.y}).");
			return false;
		}

		// Shuffle positions to randomize spawn locations
		ShuffleList(validPositions);

		foreach (Vector2Int spawnPos in validPositions)
		{
			// Skip if there's already an item at this position
			Item existingItem = ItemManager.Instance.GetItemAtPosition(spawnPos);
			if (existingItem != null)
			{
				// Specifically check if it's a stick - ensure we don't spawn on top of other sticks
				if (existingItem.ItemType == _sticksItemType)
				{
					continue; // Don't spawn stick on top of another stick
				}
				// If it's a different item, also skip (can't place stick on top of other items)
				continue;
			}

			// Spawn the stick and track it
			Item spawnedStick = ItemManager.Instance.SpawnItem(_sticksItemType, spawnPos);
			if (spawnedStick != null)
			{
				_spawnedSticks.Add(spawnedStick);
				Debug.Log($"HawkDen: Spawned sticks item at ({spawnPos.x}, {spawnPos.y}). Total sticks: {_spawnedSticks.Count}/{_maxSticks}");
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Removes null or destroyed stick references from the tracking list.
	/// Called when sticks are picked up or destroyed.
	/// </summary>
	private void CleanupDestroyedSticks()
	{
		for (int i = _spawnedSticks.Count - 1; i >= 0; i--)
		{
			Item stick = _spawnedSticks[i];
			
			// Check if the item reference is null (Unity sets this when GameObject is destroyed)
			if (stick == null)
			{
				_spawnedSticks.RemoveAt(i);
				continue;
			}
			
			// Verify the item still exists in ItemManager (picked up items are removed)
			if (ItemManager.Instance != null)
			{
				Item itemAtPos = ItemManager.Instance.GetItemAtPosition(stick.GridPosition);
				if (itemAtPos != stick)
				{
					// Item is no longer at its position (was picked up or destroyed)
					_spawnedSticks.RemoveAt(i);
				}
			}
		}
	}

	/// <summary>
	/// Gets all valid walkable positions within the spawn radius (between min and max).
	/// Excludes water tiles to prevent sticks from spawning in water.
	/// </summary>
	private List<Vector2Int> GetValidWalkablePositionsInArea()
	{
		List<Vector2Int> validPositions = new List<Vector2Int>();

		if (EnvironmentManager.Instance == null)
		{
			return validPositions;
		}

		// Ensure min radius is not greater than max radius
		int minRadius = Mathf.Min(_minSpawnRadius, _maxSpawnRadius);
		int maxRadius = Mathf.Max(_minSpawnRadius, _maxSpawnRadius);

		// Check all positions within the maximum spawn radius
		for (int dx = -maxRadius; dx <= maxRadius; dx++)
		{
			for (int dy = -maxRadius; dy <= maxRadius; dy++)
			{
				Vector2Int candidatePos = _gridPosition + new Vector2Int(dx, dy);

				// Check if position is valid
				if (!EnvironmentManager.Instance.IsValidPosition(candidatePos))
				{
					continue;
				}

				// Check if position is walkable
				if (!EnvironmentManager.Instance.IsWalkable(candidatePos))
				{
					continue;
				}

				TileType tileType = EnvironmentManager.Instance.GetTileType(candidatePos);
				if (tileType != TileType.Grass)
				{
					continue;
				}

				// Calculate Manhattan distance from den
				int distance = Mathf.Abs(candidatePos.x - _gridPosition.x) + Mathf.Abs(candidatePos.y - _gridPosition.y);
				
				// Only add positions that are within the min and max radius
				if (distance >= minRadius && distance <= maxRadius)
				{
					validPositions.Add(candidatePos);
				}
			}
		}

		return validPositions;
	}

	/// <summary>
	/// Shuffles a list using Fisher-Yates algorithm.
	/// </summary>
	private void ShuffleList<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			T temp = list[i];
			list[i] = list[j];
			list[j] = temp;
		}
	}
}

