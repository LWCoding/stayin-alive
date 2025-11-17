using UnityEngine;

/// <summary>
/// Interactable that periodically spawns rabbits on its tile.
/// Spawn cadence is turn-based and the amount spawned varies by season.
/// </summary>
public class RabbitSpawner : Interactable
{
	[Header("Spawner Settings")]
	[Tooltip("Name of the animal to spawn. Must match an AnimalData asset.")]
	[SerializeField] private string _rabbitAnimalName = "Rabbit";
	[Tooltip("Number of turns between spawn attempts.")]
	[SerializeField] private int _baseTurnsBetweenSpawns = 10;
	[Tooltip("Baseline number of rabbits spawned each time before seasonal adjustments.")]
	[SerializeField] private int _baseSpawnAmount = 2;
	[Tooltip("Random variance applied to the spawn amount (0 = none, 0.25 = ±25%).")]
	[SerializeField, Range(0f, 1f)] private float _spawnAmountVariance = 0.25f;

	[Header("Season Spawn Multipliers")]
	[SerializeField, Tooltip("Multiplier applied to spawn count during Spring.")]
	private float _springSpawnMultiplier = 1.5f;
	[SerializeField, Tooltip("Multiplier applied to spawn count during Summer.")]
	private float _summerSpawnMultiplier = 1.2f;
	[SerializeField, Tooltip("Multiplier applied to spawn count during Fall.")]
	private float _fallSpawnMultiplier = 0.8f;
	[SerializeField, Tooltip("Multiplier applied to spawn count during Winter.")]
	private float _winterSpawnMultiplier = 0.5f;

	private int _turnsSinceLastSpawn;
	private bool _initialized;

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
			InteractableManager.Instance.RemoveRabbitSpawner(this);
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
		if (!_initialized || _baseTurnsBetweenSpawns <= 0)
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

		if (_turnsSinceLastSpawn < _baseTurnsBetweenSpawns)
		{
			return;
		}

		int spawnAmount = CalculateSpawnAmount();
		if (spawnAmount <= 0)
		{
			_turnsSinceLastSpawn = 0;
			return;
		}

		if (TrySpawnRabbits(spawnAmount))
		{
			_turnsSinceLastSpawn = 0;
		}
	}

	private int CalculateSpawnAmount()
	{
		float multiplier = GetSeasonMultiplier();
		float variance = Mathf.Clamp01(_spawnAmountVariance);
		float randomFactor = variance > 0f ? Random.Range(1f - variance, 1f + variance) : 1f;

		int amount = Mathf.RoundToInt(_baseSpawnAmount * multiplier * randomFactor);
		return Mathf.Max(1, amount);
	}

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

	private bool TrySpawnRabbits(int amount)
	{
		if (AnimalManager.Instance == null)
		{
			Debug.LogWarning("RabbitSpawner: AnimalManager instance not found. Cannot spawn rabbits.");
			return false;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogWarning("RabbitSpawner: EnvironmentManager instance not found. Cannot spawn rabbits.");
			return false;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(_gridPosition) || !EnvironmentManager.Instance.IsWalkable(_gridPosition))
		{
			Debug.LogWarning($"RabbitSpawner: Grid position {_gridPosition} is not valid or walkable for spawning.");
			return false;
		}

		Animal existingRabbit = FindRabbitAtGridPosition();

		// Abort if another animal already occupies this tile
		if (AnimalManager.Instance.HasOtherAnimalAtPosition(existingRabbit, _gridPosition))
		{
			return false;
		}

		if (existingRabbit != null)
		{
			existingRabbit.IncreaseAnimalCount(amount);
			Debug.Log($"RabbitSpawner: Increased rabbit count by {amount} at ({_gridPosition.x}, {_gridPosition.y}).");
			return true;
		}

		Animal spawned = AnimalManager.Instance.SpawnAnimal(_rabbitAnimalName, _gridPosition, amount);
		if (spawned != null)
		{
			Debug.Log($"RabbitSpawner: Spawned {amount} rabbits at ({_gridPosition.x}, {_gridPosition.y}).");
			return true;
		}

		return false;
	}

	private Animal FindRabbitAtGridPosition()
	{
		var rabbits = AnimalManager.Instance.GetAnimalsByName(_rabbitAnimalName);
		for (int i = 0; i < rabbits.Count; i++)
		{
			Animal rabbit = rabbits[i];
			if (rabbit != null && rabbit.GridPosition == _gridPosition)
			{
				return rabbit;
			}
		}

		return null;
	}
}


