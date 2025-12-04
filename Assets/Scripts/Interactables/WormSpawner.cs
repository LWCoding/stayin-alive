using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactable that periodically spawns worms in an area around it.
/// Spawn cadence is turn-based and worms only spawn on grass tiles.
/// </summary>
public class WormSpawner : Interactable
{
	[Header("Spawner Settings")]
	[Tooltip("Type of the worm item to spawn.")]
	[SerializeField] private ItemType _wormItemType = ItemType.Worm;
	[Tooltip("Number of turns between spawn attempts.")]
	[SerializeField] private int _turnsBetweenSpawns = 8;
	[Tooltip("Radius of the spawn area around the spawner (in grid tiles).")]
	[SerializeField] private int _spawnRadius = 3;
	[Tooltip("Maximum number of worms that can exist around this spawner at once.")]
	[SerializeField] private int _maxWorms = 2;

	[Header("Season Spawn Multipliers")]
	[Tooltip("Multiplier applied to spawn rate during Spring. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _springSpawnMultiplier = 1.2f;
	[Tooltip("Multiplier applied to spawn rate during Summer. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _summerSpawnMultiplier = 1.5f;
	[Tooltip("Multiplier applied to spawn rate during Fall. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _fallSpawnMultiplier = 1.0f;
	[Tooltip("Multiplier applied to spawn rate during Winter. Higher values = faster spawning (fewer turns).")]
	[SerializeField] private float _winterSpawnMultiplier = 0.6f;

	private int _turnsSinceLastSpawn;
	private bool _initialized;
	// Track all worms spawned by this spawner
	private List<Item> _spawnedWorms = new List<Item>();

	private void OnEnable()
	{
		SubscribeToTurnEvents();
	}

	private void Start()
	{
		EnsureInitializationFromWorld();
		UpdateWorldPosition();
	}

	private void OnDisable()
	{
		UnsubscribeFromTurnEvents();
	}

	private void OnDestroy()
	{
		UnsubscribeFromTurnEvents();

		if (InteractableManager.Instance != null)
		{
			InteractableManager.Instance.RemoveWormSpawner(this);
		}
	}

	/// <summary>
	/// Initializes the spawner at the specified grid position.
	/// </summary>
	public override void Initialize(Vector2Int gridPosition)
	{
		_gridPosition = gridPosition;
		_turnsSinceLastSpawn = 0;
		_initialized = true;
		_spawnedWorms.Clear();

		UpdateWorldPosition();
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

	private void EnsureInitializationFromWorld()
	{
		if (_initialized)
		{
			return;
		}

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
		_initialized = true;
		_spawnedWorms.Clear();
	}

	private void UpdateWorldPosition()
	{
		if (!_initialized)
		{
			return;
		}

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

	private void HandleTurnAdvanced(int currentTurn)
	{
		if (!_initialized || _turnsBetweenSpawns <= 0)
		{
			return;
		}

		// Reset on fresh level start
		if (currentTurn == 0)
		{
			_turnsSinceLastSpawn = 0;
			_spawnedWorms.Clear();
			return;
		}

		// Clean up destroyed/null worm references
		CleanupDestroyedWorms();

		_turnsSinceLastSpawn++;

		// Get season-adjusted spawn rate (divide by multiplier: higher multiplier = fewer turns)
		float seasonMultiplier = GetSeasonMultiplier();
		int adjustedTurnsBetweenSpawns = Mathf.Max(1, Mathf.RoundToInt(_turnsBetweenSpawns / Mathf.Max(0.01f, seasonMultiplier)));

		if (_turnsSinceLastSpawn < adjustedTurnsBetweenSpawns)
		{
			return;
		}

		if (TrySpawnWormItem())
		{
			_turnsSinceLastSpawn = 0;
		}
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

	private bool TrySpawnWormItem()
	{
		if (EnvironmentManager.Instance == null)
		{
			Debug.LogWarning("WormSpawner: EnvironmentManager instance not found. Cannot spawn worms.");
			return false;
		}

		if (ItemManager.Instance == null)
		{
			Debug.LogWarning("WormSpawner: ItemManager instance not found. Cannot spawn worms.");
			return false;
		}

		// Clean up destroyed/null worm references before checking count
		CleanupDestroyedWorms();

		// Check if we're at max capacity
		if (_spawnedWorms.Count >= _maxWorms)
		{
			return false; // Already at max worms, don't spawn more
		}

		// Get all valid grass positions in the spawn area
		List<Vector2Int> validGrassPositions = GetValidGrassPositionsInArea();
		
		if (validGrassPositions.Count == 0)
		{
			Debug.LogWarning($"WormSpawner: No valid grass positions found in spawn area around ({_gridPosition.x}, {_gridPosition.y}).");
			return false;
		}

		// Shuffle positions to randomize spawn locations
		ShuffleList(validGrassPositions);

		foreach (Vector2Int spawnPos in validGrassPositions)
		{
			// Skip if there's already an item at this position
			Item existingItem = ItemManager.Instance.GetItemAtPosition(spawnPos);
			if (existingItem != null)
			{
				// Specifically check if it's a worm - ensure we don't spawn on top of other worms
				if (existingItem.ItemType == _wormItemType)
				{
					continue; // Don't spawn worm on top of another worm
				}
				// If it's a different item, also skip (can't place worm on top of other items)
				continue;
			}

			// Spawn the worm and track it
			Item spawnedWorm = ItemManager.Instance.SpawnItem(_wormItemType, spawnPos);
			if (spawnedWorm != null)
			{
				_spawnedWorms.Add(spawnedWorm);
				Debug.Log($"WormSpawner: Spawned worm item at ({spawnPos.x}, {spawnPos.y}). Total worms: {_spawnedWorms.Count}/{_maxWorms}");
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Removes null or destroyed worm references from the tracking list.
	/// Called when worms are picked up or destroyed.
	/// </summary>
	private void CleanupDestroyedWorms()
	{
		for (int i = _spawnedWorms.Count - 1; i >= 0; i--)
		{
			Item worm = _spawnedWorms[i];
			
			// Check if the item reference is null (Unity sets this when GameObject is destroyed)
			if (worm == null)
			{
				_spawnedWorms.RemoveAt(i);
				continue;
			}
			
			// Verify the item still exists in ItemManager (picked up items are removed)
			if (ItemManager.Instance != null)
			{
				Item itemAtPos = ItemManager.Instance.GetItemAtPosition(worm.GridPosition);
				if (itemAtPos != worm)
				{
					// Item is no longer at its position (was picked up or destroyed)
					_spawnedWorms.RemoveAt(i);
				}
			}
		}
	}

	/// <summary>
	/// Gets all valid grass positions within the spawn radius.
	/// </summary>
	private List<Vector2Int> GetValidGrassPositionsInArea()
	{
		List<Vector2Int> validPositions = new List<Vector2Int>();

		if (EnvironmentManager.Instance == null)
		{
			return validPositions;
		}

		// Check all positions within the spawn radius
		for (int dx = -_spawnRadius; dx <= _spawnRadius; dx++)
		{
			for (int dy = -_spawnRadius; dy <= _spawnRadius; dy++)
			{
				Vector2Int candidatePos = _gridPosition + new Vector2Int(dx, dy);

				// Check if position is valid
				if (!EnvironmentManager.Instance.IsValidPosition(candidatePos))
				{
					continue;
				}

				if (!IsValidTileForWorm(candidatePos))
				{
					continue;
				}

				validPositions.Add(candidatePos);
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

	private bool IsValidTileForWorm(Vector2Int candidatePos)
	{
		if (EnvironmentManager.Instance == null)
		{
			return false;
		}

		TileType tileType = EnvironmentManager.Instance.GetTileType(candidatePos);
		if (tileType != TileType.Grass)
		{
			return false;
		}

		if (ItemManager.Instance != null && ItemManager.Instance.GetItemAtPosition(candidatePos) != null)
		{
			return false;
		}

		if (InteractableManager.Instance != null && InteractableManager.Instance.HasInteractableAtPosition(candidatePos))
		{
			return false;
		}

		return true;
	}
}

