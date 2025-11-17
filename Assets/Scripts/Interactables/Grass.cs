using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactable that periodically grows grass on its tile.
/// Can be harvested twice: first harvest changes to growing sprite, second harvest deletes it.
/// </summary>
public class Grass : Interactable
{
	[Header("Grass Settings")]
	[Tooltip("Average number of turns between growth attempts.")]
	[SerializeField] private int _averageTurnsBetweenSpawns = 10;
	[Tooltip("Random variance applied to the turns between spawns (0 = none, 0.25 = ±25%).")]
	[SerializeField, Range(0f, 1f)] private float _turnsVariance = 0.25f;
	[Tooltip("Food item prefab that defines the hunger restoration value for this grass.")]
	[SerializeField] private FoodItem _foodItemPrefab;

	[Header("Visuals")]
	[Tooltip("Sprite shown when grass is fully grown.")]
	[SerializeField] private Sprite _fullSprite;
	[Tooltip("Sprite shown when grass is growing (after first harvest).")]
	[SerializeField] private Sprite _growingSprite;
	[Tooltip("SpriteRenderer component for displaying the grass sprite.")]
	[SerializeField] private SpriteRenderer _spriteRenderer;
	[Tooltip("GameObject that shows/hides when the player is on the same tile (e.g., interaction indicator)")]
	[SerializeField] private GameObject _interactionIndicator;

	[Header("Season Growth Multipliers")]
	[Tooltip("Multiplier applied to growth rate during Spring. Higher values = faster growth (fewer turns).")]
	[SerializeField] private float _springGrowthMultiplier = 1.5f;
	[Tooltip("Multiplier applied to growth rate during Summer. Higher values = faster growth (fewer turns).")]
	[SerializeField] private float _summerGrowthMultiplier = 1.2f;
	[Tooltip("Multiplier applied to growth rate during Fall. Higher values = faster growth (fewer turns).")]
	[SerializeField] private float _fallGrowthMultiplier = 0.8f;
	[Tooltip("Multiplier applied to growth rate during Winter. Higher values = faster growth (fewer turns).")]
	[SerializeField] private float _winterGrowthMultiplier = 0.5f;

	[Header("Spreading Settings")]
	[Tooltip("Average number of turns between spread attempts when grass is fully grown.")]
	[SerializeField] private int _averageTurnsBetweenSpreads = 15;
	[Tooltip("Random variance applied to the turns between spreads (0 = none, 0.25 = ±25%).")]
	[SerializeField, Range(0f, 1f)] private float _spreadVariance = 0.25f;

	private enum GrassState
	{
		Growing,  // Growing state (after first harvest)
		Full      // Fully grown state
	}

	private int _turnsSinceLastSpawn;
	private int _turnsUntilNextSpawn;
	private int _turnsSinceLastSpread;
	private int _turnsUntilNextSpread;
	private bool _initialized;
	private GrassState _currentState;
	private bool _isPlayerOnTile;

	/// <summary>
	/// Amount of hunger restored when an animal eats this grass.
	/// Gets the value from the referenced food item prefab.
	/// </summary>
	public int HungerRestored
	{
		get
		{
			if (_foodItemPrefab != null)
			{
				return _foodItemPrefab.HungerRestored;
			}
			return 0; // Default to 0 if no food item prefab is set
		}
	}

	/// <summary>
	/// Checks if the grass is fully grown and ready to be eaten.
	/// </summary>
	public bool IsFullyGrown()
	{
		return _currentState == GrassState.Full;
	}

	/// <summary>
	/// Harvests the grass for an animal (changes from Full to Growing state).
	/// This is called when an animal eats the grass.
	/// </summary>
	public void HarvestForAnimal()
	{
		if (_currentState == GrassState.Full)
		{
			// Change to growing state (animal has eaten it)
			_currentState = GrassState.Growing;
			UpdateSprite();
			_turnsSinceLastSpawn = 0;
			CalculateNextSpawnTime();
		}
	}

	private void Awake()
	{
		if (_spriteRenderer == null)
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
		}

		// Hide interaction indicator by default
		if (_interactionIndicator != null)
		{
			_interactionIndicator.SetActive(false);
		}
	}

	private void OnEnable()
	{
		SubscribeToTurnEvents();
	}

	private void Start()
	{
		EnsureInitializationFromWorld();
		UpdateWorldPosition();
		CalculateNextSpawnTime();
		CalculateNextSpreadTime();
		UpdateSprite();
	}

	private void Update()
	{
		// Check if player is on the same tile
		_isPlayerOnTile = IsPlayerOnSameTile();

		// Show/hide interaction indicator
		if (_interactionIndicator != null)
		{
			_interactionIndicator.SetActive(_isPlayerOnTile);
		}

		// If player is on the same tile and E key is pressed, attempt harvest
		if (_isPlayerOnTile && Input.GetKeyDown(KeyCode.E))
		{
			AttemptHarvest();
		}
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
	public override void Initialize(Vector2Int gridPosition)
	{
		_gridPosition = gridPosition;
		_turnsSinceLastSpawn = 0;
		_turnsSinceLastSpread = 0;
		_currentState = GrassState.Full;
		_initialized = true;

		UpdateWorldPosition();
		CalculateNextSpawnTime();
		CalculateNextSpreadTime();
		UpdateSprite();
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
		_turnsSinceLastSpread = 0;
		_currentState = GrassState.Full;
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

		// Check if this grass tile is revealed by fog of war
		// If not revealed, don't process any growth or spreading -- this is for balancing :)
		if (FogOfWarManager.Instance != null && !FogOfWarManager.Instance.IsTileRevealed(_gridPosition))
		{
			return;
		}

		// Reset on fresh level start
		if (currentTurn == 0)
		{
			_turnsSinceLastSpawn = 0;
			_turnsSinceLastSpread = 0;
			_currentState = GrassState.Full;
			CalculateNextSpawnTime();
			CalculateNextSpreadTime();
			UpdateSprite();
			return;
		}

		// Handle spreading when grass is fully grown
		if (_currentState == GrassState.Full)
		{
			_turnsSinceLastSpread++;
			if (_turnsSinceLastSpread >= _turnsUntilNextSpread)
			{
				TrySpreadToAdjacentTiles();
				_turnsSinceLastSpread = 0;
				CalculateNextSpreadTime();
			}
			return;
		}

		// Handle growth when grass is in growing state
		// On turn 1, immediately try to grow to full
		if (currentTurn == 1)
		{
			if (GrowToFull())
			{
				_turnsSinceLastSpawn = 0;
				CalculateNextSpawnTime();
			}
			return;
		}

		_turnsSinceLastSpawn++;

		if (_turnsSinceLastSpawn < _turnsUntilNextSpawn)
		{
			return;
		}

		// Try to grow to full
		if (GrowToFull())
		{
			_turnsSinceLastSpawn = 0;
			CalculateNextSpawnTime();
		}
	}

	/// <summary>
	/// Calculates the number of turns until the next growth attempt, with variance and season multiplier applied.
	/// </summary>
	private void CalculateNextSpawnTime()
	{
		float seasonMultiplier = GetSeasonMultiplier();
		float variance = Mathf.Clamp01(_turnsVariance);
		float randomFactor = variance > 0f ? Random.Range(1f - variance, 1f + variance) : 1f;
		
		// Divide by multiplier: higher multiplier = fewer turns (faster growth)
		float adjustedTurns = _averageTurnsBetweenSpawns / Mathf.Max(0.01f, seasonMultiplier) * randomFactor;
		_turnsUntilNextSpawn = Mathf.Max(1, Mathf.RoundToInt(adjustedTurns));
	}

	/// <summary>
	/// Gets the growth rate multiplier for the current season.
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
				return _springGrowthMultiplier;
			case TimeManager.Season.Summer:
				return _summerGrowthMultiplier;
			case TimeManager.Season.Fall:
				return _fallGrowthMultiplier;
			case TimeManager.Season.Winter:
				return _winterGrowthMultiplier;
			default:
				return 1f;
		}
	}

	/// <summary>
	/// Grows the grass from Growing state to Full state.
	/// </summary>
	private bool GrowToFull()
	{
		if (_currentState != GrassState.Growing)
		{
			return false;
		}

		_currentState = GrassState.Full;
		UpdateSprite();
		return true;
	}

	/// <summary>
	/// Updates the sprite based on the current state.
	/// </summary>
	private void UpdateSprite()
	{
		if (_spriteRenderer == null)
		{
			return;
		}

		switch (_currentState)
		{
			case GrassState.Full:
				if (_fullSprite != null)
				{
					_spriteRenderer.sprite = _fullSprite;
				}
				break;
			case GrassState.Growing:
				if (_growingSprite != null)
				{
					_spriteRenderer.sprite = _growingSprite;
				}
				break;
		}
	}

	/// <summary>
	/// Attempts to harvest the grass when the player presses E.
	/// </summary>
	private void AttemptHarvest()
	{
		// Get the player
		ControllableAnimal player = GetPlayer();
		if (player == null)
		{
			Debug.LogWarning("Grass: Cannot harvest - no controllable animal found.");
			return;
		}

		// Try to add grass item to inventory
		if (InventoryManager.Instance != null)
		{
			bool added = InventoryManager.Instance.AddItem("Grass");
			
			if (added)
			{
				if (_currentState == GrassState.Full)
				{
					// First harvest: change to growing state
					_currentState = GrassState.Growing;
					UpdateSprite();
					_turnsSinceLastSpawn = 0;
					CalculateNextSpawnTime();
				}
				else if (_currentState == GrassState.Growing)
				{
					Destroy(gameObject);
				}

				// Harvesting costs one turn
				if (TimeManager.Instance != null)
				{
					TimeManager.Instance.NextTurn();
				}
			}
			else
			{
				// Inventory full
				Debug.Log("Cannot harvest grass - inventory is full!");
			}
		}
		else
		{
			Debug.LogWarning("Grass: InventoryManager instance not found! Cannot add grass to inventory.");
		}
	}

	/// <summary>
	/// Checks if the player (ControllableAnimal) is on the same tile as this grass.
	/// Uses cached player reference to avoid looping through all animals.
	/// </summary>
	private bool IsPlayerOnSameTile()
	{
		if (AnimalManager.Instance == null)
		{
			return false;
		}
		
		// Use cached player reference instead of looping through all animals
		ControllableAnimal player = AnimalManager.Instance.GetPlayer();
		if (player != null && player.GridPosition == _gridPosition)
		{
			return true;
		}
		
		return false;
	}

	/// <summary>
	/// Gets the player (ControllableAnimal) if one exists.
	/// Uses cached player reference to avoid looping through all animals.
	/// </summary>
	private ControllableAnimal GetPlayer()
	{
		if (AnimalManager.Instance == null)
		{
			return null;
		}
		
		// Use cached player reference instead of looping through all animals
		return AnimalManager.Instance.GetPlayer();
	}

	/// <summary>
	/// Calculates the number of turns until the next spread attempt, with variance and season multiplier applied.
	/// </summary>
	private void CalculateNextSpreadTime()
	{
		float seasonMultiplier = GetSeasonMultiplier();
		float variance = Mathf.Clamp01(_spreadVariance);
		float randomFactor = variance > 0f ? Random.Range(1f - variance, 1f + variance) : 1f;
		
		// Divide by multiplier: higher multiplier = fewer turns (faster spread)
		float adjustedTurns = _averageTurnsBetweenSpreads / Mathf.Max(0.01f, seasonMultiplier) * randomFactor;
		_turnsUntilNextSpread = Mathf.Max(1, Mathf.RoundToInt(adjustedTurns));
	}

	/// <summary>
	/// Attempts to spread grass to adjacent tiles that are valid (not water, no interactables).
	/// </summary>
	private void TrySpreadToAdjacentTiles()
	{
		if (InteractableManager.Instance == null)
		{
			Debug.LogWarning("Grass: InteractableManager instance not found. Cannot spread grass.");
			return;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogWarning("Grass: EnvironmentManager instance not found. Cannot spread grass.");
			return;
		}

		// Get adjacent tiles (4-directional)
		Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
		
		foreach (Vector2Int dir in directions)
		{
			Vector2Int adjacentPos = _gridPosition + dir;
			
			// Check if the adjacent tile is valid for spreading
			if (IsValidTileForSpreading(adjacentPos))
			{
				// Spawn a new grass interactable at the adjacent position
				Grass newGrass = InteractableManager.Instance.SpawnGrass(adjacentPos);
				if (newGrass != null)
				{
					// Only spread to one tile per attempt to prevent rapid expansion
					return;
				}
			}
		}
	}

	/// <summary>
	/// Checks if a tile is valid for grass spreading (not water, not obstacle/wall, no interactables, valid position).
	/// </summary>
	private bool IsValidTileForSpreading(Vector2Int position)
	{
		if (EnvironmentManager.Instance == null)
		{
			return false;
		}

		// Check if position is valid
		if (!EnvironmentManager.Instance.IsValidPosition(position))
		{
			return false;
		}

		// Check if tile is water or obstacle (walls)
		TileType tileType = EnvironmentManager.Instance.GetTileType(position);
		if (tileType == TileType.Water || tileType == TileType.Obstacle)
		{
			return false;
		}

		// Check if there's already an interactable at this position
		if (InteractableManager.Instance == null)
		{
			return false;
		}

		// Check for any type of interactable
		if (InteractableManager.Instance.HasInteractableAtPosition(position))
		{
			return false;
		}

		return true;
	}
}
