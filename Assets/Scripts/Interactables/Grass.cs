using UnityEngine;

/// <summary>
/// Interactable that periodically grows grass (spawns a "Grass" item) on its tile.
/// Spawn cadence is turn-based. Once grass is grown, it stops growing until the grass is harvested.
/// </summary>
public class Grass : MonoBehaviour
{
	[Header("Grass Settings")]
	[Tooltip("Name of the item to spawn when grass grows. Should be 'Grass'.")]
	[SerializeField] private string _grassItemName = "Grass";
	[Tooltip("Average number of turns between spawn attempts.")]
	[SerializeField] private int _averageTurnsBetweenSpawns = 10;
	[Tooltip("Random variance applied to the turns between spawns (0 = none, 0.25 = ±25%).")]
	[SerializeField, Range(0f, 1f)] private float _turnsVariance = 0.25f;

	private Vector2Int _gridPosition;
	private int _turnsSinceLastSpawn;
	private int _turnsUntilNextSpawn;
	private bool _initialized;
	private bool _hadGrassLastTurn;

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
		CalculateNextSpawnTime();
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
			InteractableManager.Instance.RemoveGrass(this);
		}
	}

	/// <summary>
	/// Initializes the grass at the specified grid position.
	/// </summary>
	public void Initialize(Vector2Int gridPosition)
	{
		_gridPosition = gridPosition;
		_turnsSinceLastSpawn = 0;
		_hadGrassLastTurn = false;
		_initialized = true;

		UpdateWorldPosition();
		CalculateNextSpawnTime();
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
		_hadGrassLastTurn = false;
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
		if (!_initialized || _averageTurnsBetweenSpawns <= 0)
		{
			return;
		}

		// Reset on fresh level start
		if (currentTurn == 0)
		{
			_turnsSinceLastSpawn = 0;
			_hadGrassLastTurn = false;
			CalculateNextSpawnTime();
			return;
		}

		// Check if there's already grass on this tile
		bool hasGrassNow = HasGrassOnTile();

		// If grass was just harvested (had grass last turn, but not now), reset the counter
		if (_hadGrassLastTurn && !hasGrassNow)
		{
			_turnsSinceLastSpawn = 0;
			CalculateNextSpawnTime();
		}

		// Update state for next turn
		_hadGrassLastTurn = hasGrassNow;

		// Don't increment counter while grass is present - wait for it to be harvested
		if (hasGrassNow)
		{
			return;
		}

		_turnsSinceLastSpawn++;

		if (_turnsSinceLastSpawn < _turnsUntilNextSpawn)
		{
			return;
		}

		// Try to spawn grass
		if (TrySpawnGrass())
		{
			_turnsSinceLastSpawn = 0;
			CalculateNextSpawnTime();
		}
	}

	/// <summary>
	/// Calculates the number of turns until the next spawn attempt, with variance applied.
	/// </summary>
	private void CalculateNextSpawnTime()
	{
		float variance = Mathf.Clamp01(_turnsVariance);
		float randomFactor = variance > 0f ? Random.Range(1f - variance, 1f + variance) : 1f;
		_turnsUntilNextSpawn = Mathf.Max(1, Mathf.RoundToInt(_averageTurnsBetweenSpawns * randomFactor));
	}

	/// <summary>
	/// Checks if there's already a "Grass" item on this tile.
	/// </summary>
	private bool HasGrassOnTile()
	{
		if (ItemManager.Instance == null)
		{
			return false;
		}

		Item item = ItemManager.Instance.GetItemAtPosition(_gridPosition);
		if (item != null && item.ItemName == _grassItemName)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Attempts to spawn a "Grass" item on this tile.
	/// </summary>
	private bool TrySpawnGrass()
	{
		if (ItemManager.Instance == null)
		{
			Debug.LogWarning("Grass: ItemManager instance not found. Cannot spawn grass.");
			return false;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogWarning("Grass: EnvironmentManager instance not found. Cannot spawn grass.");
			return false;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(_gridPosition))
		{
			Debug.LogWarning($"Grass: Grid position {_gridPosition} is not valid for spawning.");
			return false;
		}

		// Check if there's already grass on this tile (double-check)
		if (HasGrassOnTile())
		{
			return false;
		}

		// Spawn the grass item
		Item grassItem = ItemManager.Instance.SpawnItem(_grassItemName, _gridPosition);
		if (grassItem != null)
		{
			Debug.Log($"Grass: Grew grass at ({_gridPosition.x}, {_gridPosition.y}).");
			return true;
		}

		return false;
	}
}

