using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactable that periodically spawns worms in an area around it.
/// Spawn cadence is turn-based and worms only spawn on grass tiles.
/// </summary>
public class WormSpawner : MonoBehaviour
{
	[Header("Spawner Settings")]
	[Tooltip("Name of the worm item to spawn.")]
	[SerializeField] private string _wormItemName = "Worm";
	[Tooltip("Number of turns between spawn attempts.")]
	[SerializeField] private int _turnsBetweenSpawns = 8;
	[Tooltip("Radius of the spawn area around the spawner (in grid tiles).")]
	[SerializeField] private int _spawnRadius = 3;

	private Vector2Int _gridPosition;
	private int _turnsSinceLastSpawn;
	private bool _initialized;

	/// <summary>
	/// Grid position used for spawning and tile placement.
	/// </summary>
	public Vector2Int GridPosition => _gridPosition;

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
	public void Initialize(Vector2Int gridPosition)
	{
		_gridPosition = gridPosition;
		_turnsSinceLastSpawn = 0;
		_initialized = true;

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
			return;
		}

		_turnsSinceLastSpawn++;

		if (_turnsSinceLastSpawn < _turnsBetweenSpawns)
		{
			return;
		}

		if (TrySpawnWormItem())
		{
			_turnsSinceLastSpawn = 0;
		}
	}

	private bool TrySpawnWormItem()
	{
		if (EnvironmentManager.Instance == null)
		{
			Debug.LogWarning("WormSpawner: EnvironmentManager instance not found. Cannot spawn worms.");
			return false;
		}

		if (ItemTilemapManager.Instance == null)
		{
			Debug.LogWarning("WormSpawner: ItemTilemapManager instance not found. Cannot spawn worms.");
			return false;
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
			if (ItemTilemapManager.Instance.HasItemAt(spawnPos))
			{
				continue;
			}

			ItemTilemapManager.Instance.PlaceItem(_wormItemName, spawnPos);
			Debug.Log($"WormSpawner: Spawned worm item at ({spawnPos.x}, {spawnPos.y}).");
			return true;
		}

		return false;
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

				// Check if position is walkable
				if (!EnvironmentManager.Instance.IsWalkable(candidatePos))
				{
					continue;
				}

				// Check if position is grass
				TileType tileType = EnvironmentManager.Instance.GetTileType(candidatePos);
				if (tileType != TileType.Grass)
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
}

