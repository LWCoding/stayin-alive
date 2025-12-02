using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactable that spawns a set amount of rabbits initially and acts as a hideable den for them.
/// Rabbits can hide in this spawner when touched or when fleeing from predators.
/// </summary>
public class RabbitSpawner : Interactable, IHideable
{
	[Header("Spawner Settings")]
	[Tooltip("Name of the animal to spawn. Must match an AnimalData asset.")]
	[SerializeField] private string _rabbitAnimalName = "Rabbit";
	
	[Header("Territory Settings")]
	[Tooltip("Radius in grid cells that rabbits attached to this spawner will wander within")]
	[SerializeField] private int _territoryRadius = 10;

	[Header("Visual Settings")]
	[Tooltip("Sprite to display when rabbits are hiding in the den")]
	[SerializeField] private Sprite _occupiedSprite;
	
	[Tooltip("Sprite to display when no rabbits are hiding in the den")]
	[SerializeField] private Sprite _unoccupiedSprite;

	// Constants for spawning behavior
	private const int PERIODIC_SPAWN_INTERVAL = 10; // Turns between periodic spawns when rabbits are hiding
	private const int MIN_GROUP_SIZE = 1; // Minimum rabbits per group
	private const int MAX_GROUP_SIZE = 4; // Maximum rabbits per group (exclusive in Random.Range)
	private const int HUNGER_RANDOM_MIN = -5; // Minimum hunger offset from threshold
	private const int HUNGER_RANDOM_MAX = 15; // Maximum hunger offset from threshold

	private bool _initialized;
	private bool _hasSpawnedInitialRabbits = false;

	// Track rabbits currently hiding in this spawner
	private List<Animal> _hidingRabbits = new List<Animal>();
	
	// Track all rabbits attached to this spawner (for cached count)
	private List<Animal> _attachedRabbits = new List<Animal>();
	
	// Cached count of alive rabbits attached to this spawner
	private int _attachedRabbitCount = 0;
	
	// Track turns since last periodic spawn (only increments when rabbits are hiding)
	private int _turnsSinceLastSpawn = 0;
	
	// Track turns since extinction (increments when _attachedRabbitCount is 0)
	private int _turnsSinceExtinction = 0;
	
	// Reference to the SpriteRenderer component
	private SpriteRenderer _spriteRenderer;
	
	public int TerritoryRadius => _territoryRadius;

	private void Start()
	{
		// Get the SpriteRenderer component
		_spriteRenderer = GetComponent<SpriteRenderer>();
		
		EnsureInitializationFromWorld();
		UpdateWorldPosition();
		SubscribeToTurnEvents();
		
		// If TimeManager already fired turn 0 event before we subscribed, handle it now
		if (TimeManager.Instance != null && TimeManager.Instance.PlayerTurnCount == 0 && !_hasSpawnedInitialRabbits)
		{
			HandleTurnAdvanced(0);
		}
		
		// Initialize sprite state
		UpdateSprite();
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
		if (!_initialized)
		{
			return;
		}

		// Spawn all rabbits at once on turn 0 in groups of 1-3 and add them to hiding list
		if (currentTurn == 0 && !_hasSpawnedInitialRabbits)
		{
			int totalRabbits = Random.Range(Globals.RabbitSpawnMinCount, Globals.RabbitSpawnMaxCount + 1);
			int rabbitsSpawned = 0;
			
			// Spawn rabbits in groups of 1-3 until all are spawned
			while (rabbitsSpawned < totalRabbits)
			{
				int remainingRabbits = totalRabbits - rabbitsSpawned;
				int groupSize = Mathf.Min(Random.Range(MIN_GROUP_SIZE, MAX_GROUP_SIZE + 1), remainingRabbits); // 1-3 rabbits, but not more than remaining
				
				// Spawn the group
				Animal spawnedAnimal = TrySpawnRabbitGroup(groupSize);
				if (spawnedAnimal != null)
				{
					rabbitsSpawned += groupSize;
					// Add the spawned animal to hiding list (only once per group, even if count > 1)
					if (!_hidingRabbits.Contains(spawnedAnimal))
					{
						OnAnimalEnter(spawnedAnimal);
					}
				}
				else
				{
					// If spawning failed, break to avoid infinite loop
					break;
				}
			}
			
			_hasSpawnedInitialRabbits = true;
		}

		// Update hiding rabbits - decrement turns and bring them back after 3 turns
		UpdateHidingRabbits();
		
		// Clean up null rabbits from attached rabbits list and update cached count
		CleanupAttachedRabbits();
		
		// Clean up null rabbits from hiding list
		_hidingRabbits.RemoveAll(rabbit => rabbit == null);
		
		// Check if there are zero rabbits attached to this spawner
		if (_attachedRabbitCount == 0)
		{
			// Increment turns since extinction
			_turnsSinceExtinction++;
			
			// Check if we've waited long enough to respawn and if it's a valid breeding season
			if (_turnsSinceExtinction >= Globals.RabbitRespawnDelayTurns && IsValidBreedingSeason())
			{
				Debug.Log($"RabbitSpawner at ({_gridPosition.x}, {_gridPosition.y}): Respawn delay reached ({_turnsSinceExtinction} turns), spawning new rabbit to prevent extinction.");
				Animal spawnedAnimal = TrySpawnRabbitGroup(1); // Spawn a single rabbit
				if (spawnedAnimal != null)
				{
					// Add the spawned animal to hiding list
					if (!_hidingRabbits.Contains(spawnedAnimal))
					{
						OnAnimalEnter(spawnedAnimal);
					}
					// Reset extinction counter since we spawned a new rabbit
					_turnsSinceExtinction = 0;
				}
			}
			else if (_turnsSinceExtinction >= Globals.RabbitRespawnDelayTurns && !IsValidBreedingSeason())
			{
				Debug.Log($"RabbitSpawner at ({_gridPosition.x}, {_gridPosition.y}): Respawn delay reached ({_turnsSinceExtinction} turns), but current season is not suitable for breeding. Waiting for Spring or Summer.");
			}
			else
			{
				Debug.Log($"RabbitSpawner at ({_gridPosition.x}, {_gridPosition.y}): Zero rabbits detected, waiting for respawn ({_turnsSinceExtinction}/{Globals.RabbitRespawnDelayTurns} turns).");
			}
		}
		else
		{
			// Reset extinction counter if we have rabbits again
			_turnsSinceExtinction = 0;
		}
		
		// Periodic spawning: spawn new groups every PERIODIC_SPAWN_INTERVAL turns if there are rabbits hiding
		if (_hidingRabbits.Count > 0)
		{
			_turnsSinceLastSpawn++;
			
			// Spawn a new group every PERIODIC_SPAWN_INTERVAL turns
			if (_turnsSinceLastSpawn >= PERIODIC_SPAWN_INTERVAL)
			{
				int groupSize = Random.Range(MIN_GROUP_SIZE, MAX_GROUP_SIZE + 1); // 1-3 rabbits
				Animal spawnedAnimal = TrySpawnRabbitGroup(groupSize);
				if (spawnedAnimal != null)
				{
					// Add the spawned animal to hiding list
					if (!_hidingRabbits.Contains(spawnedAnimal))
					{
						OnAnimalEnter(spawnedAnimal);
					}
				}
				
				_turnsSinceLastSpawn = 0; // Reset counter
			}
		}
		// If no rabbits hiding, counter doesn't tick (preserves value for when rabbits return)
	}

	private void UpdateHidingRabbits()
	{
		// Check if there are any nearby predators (within safe distance)
		bool hasNearbyPredators = HasNearbyPredators();

		// If there are nearby predators, keep all rabbits hiding
		if (hasNearbyPredators)
		{
			return;
		}

		// Only let hungry rabbits leave the den - fed rabbits should stay
		// Create a list to avoid modifying during enumeration
		List<Animal> rabbitsToRemove = new List<Animal>();

		foreach (Animal rabbit in _hidingRabbits)
		{
			if (rabbit == null)
			{
				// Rabbit was destroyed, remove it
				rabbitsToRemove.Add(rabbit);
				continue;
			}

			// Only let hungry rabbits leave - fed rabbits stay in the den
			RabbitPrey rabbitPrey = rabbit as RabbitPrey;
			if (rabbitPrey != null && !rabbitPrey.IsHungry)
			{
				// Fed rabbit - keep it in the den
				continue;
			}

			// Try to move the hungry rabbit towards its intended destination
			// If successful, remove from hiding list
			if (TryMoveRabbitTowardsDestination(rabbit))
			{
				rabbitsToRemove.Add(rabbit);
			}
			// If move failed, keep rabbit in den (will try again next turn)
		}

		// Remove rabbits that successfully left
		foreach (Animal rabbit in rabbitsToRemove)
		{
			OnAnimalLeave(rabbit);
		}
	}

	/// <summary>
	/// Checks if there are any predators within RabbitPredatorDetectionRadius of this spawner.
	/// </summary>
	private bool HasNearbyPredators()
	{
		if (AnimalManager.Instance == null)
		{
			return false;
		}

		List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
		Vector2Int spawnerPos = _gridPosition;
		int detectionRadius = Globals.RabbitPredatorDetectionRadius;

		for (int i = 0; i < animals.Count; i++)
		{
			Animal animal = animals[i];
			if (animal == null)
			{
				continue;
			}

			// Only consider predators
			if (!(animal is PredatorAnimal))
			{
				continue;
			}

			Vector2Int predatorPos = animal.GridPosition;
			int distance = Mathf.Abs(predatorPos.x - spawnerPos.x) + Mathf.Abs(predatorPos.y - spawnerPos.y); // Manhattan distance

			// If any predator is within detection radius, return true
			if (distance <= detectionRadius)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Spawns a group of rabbits (1-3) at the spawner position and returns the spawned Animal, or null if spawning failed.
	/// Creates a separate Animal instance for each group, allowing multiple Animal instances at the same position.
	/// </summary>
	private Animal TrySpawnRabbitGroup(int groupSize)
	{
		if (AnimalManager.Instance == null)
		{
			Debug.LogWarning("RabbitSpawner: AnimalManager instance not found. Cannot spawn rabbits.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogWarning("RabbitSpawner: EnvironmentManager instance not found. Cannot spawn rabbits.");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(_gridPosition) || !EnvironmentManager.Instance.IsWalkable(_gridPosition))
		{
			Debug.LogWarning($"RabbitSpawner: Grid position {_gridPosition} is not valid or walkable for spawning.");
			return null;
		}

		// Spawn a new Animal instance at the spawner position (allows multiple instances at same position)
		Animal spawned = AnimalManager.Instance.SpawnAnimal(_rabbitAnimalName, _gridPosition, groupSize);
		if (spawned != null)
		{
			// Assign this spawner to the rabbit
			// SetRabbitSpawner will automatically call OnRabbitAttached to track the rabbit
			RabbitPrey rabbitPrey = spawned as RabbitPrey;
			if (rabbitPrey != null)
			{
				rabbitPrey.SetRabbitSpawner(this);
				
				// Set hunger to just below threshold so rabbit will want to hunt for food
				spawned.SetHunger(rabbitPrey.HungerThreshold + Random.Range(HUNGER_RANDOM_MIN, HUNGER_RANDOM_MAX + 1));
			}

			// Spawn heart particles above the spawner to indicate breeding/spawning
			SpawnHeartParticles();

			return spawned;
		}

		return null;
	}

	#region IHideable Implementation

	/// <summary>
	/// IHideable implementation: Called when a rabbit enters this spawner to hide.
	/// </summary>
	public void OnAnimalEnter(Animal animal)
	{
		if (animal == null)
		{
			return;
		}

		// Only allow rabbits to hide here
		if (!(animal is RabbitPrey))
		{
			return;
		}

		// Check if this rabbit belongs to this spawner
		RabbitPrey rabbit = animal as RabbitPrey;
		if (rabbit != null && rabbit.RabbitSpawner != this)
		{
			return;
		}

		// Add rabbit to hiding list (only if not already in it)
		if (!_hidingRabbits.Contains(animal))
		{
			_hidingRabbits.Add(animal);
		}
		animal.SetCurrentHideable(this);
		animal.SetVisualVisibility(false);

		Debug.Log($"Rabbit '{animal.name}' entered rabbit spawner at ({_gridPosition.x}, {_gridPosition.y}).");
		
		// Update sprite to show occupied state
		UpdateSprite();
	}

	/// <summary>
	/// IHideable implementation: Called when a rabbit leaves this spawner.
	/// </summary>
	public void OnAnimalLeave(Animal animal)
	{
		if (animal == null)
		{
			return;
		}

		if (_hidingRabbits.Remove(animal))
		{
			Debug.Log($"Rabbit '{animal.name}' left rabbit spawner at ({_gridPosition.x}, {_gridPosition.y}).");

			// Clear the hideable reference if this animal is leaving this spawner
			if (animal != null && ReferenceEquals(animal.CurrentHideable, this))
			{
				animal.SetCurrentHideable(null);
			}

			// Make animal visible again if they're not entering another hideable location
			if (animal != null && animal.CurrentHideable == null)
			{
				animal.SetVisualVisibility(true);
			}
			
			// Update sprite to potentially show unoccupied state
			UpdateSprite();
		}
	}

	/// <summary>
	/// Tries to move the rabbit towards its intended destination (food or wandering).
	/// Returns true if the rabbit successfully moved, false otherwise.
	/// </summary>
	private bool TryMoveRabbitTowardsDestination(Animal rabbit)
	{
		if (rabbit == null || EnvironmentManager.Instance == null)
		{
			return false;
		}

		// Get the rabbit's intended destination
		RabbitPrey rabbitPrey = rabbit as RabbitPrey;
		if (rabbitPrey == null)
		{
			return false;
		}

		Vector2Int? destination = rabbitPrey.GetIntendedDestination();
		if (!destination.HasValue)
		{
			// No destination set, can't move
			return false;
		}

		Vector2Int currentPos = rabbit.GridPosition;
		Vector2Int targetPos = destination.Value;

		// Calculate direction towards destination
		Vector2Int direction = targetPos - currentPos;

		// Normalize to get one step towards destination
		Vector2Int oneStepDirection = Vector2Int.zero;
		if (direction.x != 0 || direction.y != 0)
		{
			// Move one step in the direction of the destination
			if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
			{
				// Move horizontally first
				oneStepDirection = new Vector2Int(direction.x > 0 ? 1 : -1, 0);
			}
			else if (direction.y != 0)
			{
				// Move vertically
				oneStepDirection = new Vector2Int(0, direction.y > 0 ? 1 : -1);
			}
		}

		// If already at spawner position, we need to move away from it
		if (currentPos == _gridPosition && oneStepDirection == Vector2Int.zero)
		{
			// Try to move in the direction of the destination anyway
			if (direction.x != 0 || direction.y != 0)
			{
				if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
				{
					oneStepDirection = new Vector2Int(direction.x > 0 ? 1 : -1, 0);
				}
				else
				{
					oneStepDirection = new Vector2Int(0, direction.y > 0 ? 1 : -1);
				}
			}
		}

		// If still no direction, try any adjacent position
		if (oneStepDirection == Vector2Int.zero)
		{
			Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
			foreach (Vector2Int dir in directions)
			{
				Vector2Int testPos = currentPos + dir;
				if (IsValidMovePosition(rabbit, testPos))
				{
					rabbit.SetGridPosition(testPos);
					return true;
				}
			}
			return false;
		}

		Vector2Int nextPos = currentPos + oneStepDirection;

		// Check if this position is valid for moving
		if (IsValidMovePosition(rabbit, nextPos))
		{
			rabbit.SetGridPosition(nextPos);
			return true;
		}

		// If primary direction failed, try perpendicular directions
		if (oneStepDirection.x != 0)
		{
			// Try up and down
			if (IsValidMovePosition(rabbit, currentPos + Vector2Int.up))
			{
				rabbit.SetGridPosition(currentPos + Vector2Int.up);
				return true;
			}
			if (IsValidMovePosition(rabbit, currentPos + Vector2Int.down))
			{
				rabbit.SetGridPosition(currentPos + Vector2Int.down);
				return true;
			}
		}
		else if (oneStepDirection.y != 0)
		{
			// Try left and right
			if (IsValidMovePosition(rabbit, currentPos + Vector2Int.left))
			{
				rabbit.SetGridPosition(currentPos + Vector2Int.left);
				return true;
			}
			if (IsValidMovePosition(rabbit, currentPos + Vector2Int.right))
			{
				rabbit.SetGridPosition(currentPos + Vector2Int.right);
				return true;
			}
		}

		// Could not move in any direction
		return false;
	}

	/// <summary>
	/// Checks if a position is valid for the rabbit to move to.
	/// </summary>
	private bool IsValidMovePosition(Animal rabbit, Vector2Int targetPos)
	{
		if (rabbit == null || EnvironmentManager.Instance == null)
		{
			return false;
		}

		// Check if the target position is valid and walkable
		if (!EnvironmentManager.Instance.IsValidPosition(targetPos))
		{
			return false;
		}

		if (!EnvironmentManager.Instance.IsWalkable(targetPos))
		{
			return false;
		}

		// Check if the target position is water and if this rabbit can go on water
		TileType tileType = EnvironmentManager.Instance.GetTileType(targetPos);
		if (tileType == TileType.Water && !rabbit.CanGoOnWater)
		{
			return false;
		}

		// Check if there's another animal at this position
		if (AnimalManager.Instance != null && AnimalManager.Instance.HasOtherAnimalAtPosition(rabbit, targetPos))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// IHideable implementation: Checks if a rabbit is currently hiding in this spawner.
	/// </summary>
	public bool IsAnimalInHideable(Animal animal)
	{
		return animal != null && _hidingRabbits.Contains(animal);
	}

	#endregion

	/// <summary>
	/// Gets a random position within the territory radius of this spawner.
	/// </summary>
	/// <returns>A random walkable position within territory, or null if none found</returns>
	public Vector2Int? GetRandomPositionInTerritory()
	{
		if (EnvironmentManager.Instance == null)
		{
			return null;
		}
		
		Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
		
		// Try to find a random position within territory
		int maxAttempts = 50;
		for (int attempts = 0; attempts < maxAttempts; attempts++)
		{
			// Pick a random direction and distance within radius
			int distance = Random.Range(0, _territoryRadius + 1);
			int angle = Random.Range(0, 360);
			
			// Convert angle to direction
			float radians = angle * Mathf.Deg2Rad;
			int dx = Mathf.RoundToInt(Mathf.Cos(radians) * distance);
			int dy = Mathf.RoundToInt(Mathf.Sin(radians) * distance);
			
			Vector2Int targetPos = _gridPosition + new Vector2Int(dx, dy);
			
			// Clamp to grid bounds
			targetPos.x = Mathf.Clamp(targetPos.x, 0, gridSize.x - 1);
			targetPos.y = Mathf.Clamp(targetPos.y, 0, gridSize.y - 1);
			
			// Check if this position is valid and walkable
			if (EnvironmentManager.Instance.IsValidPosition(targetPos) && 
				EnvironmentManager.Instance.IsWalkable(targetPos))
			{
				return targetPos;
			}
		}
		
		// If we couldn't find a good position, try completely random positions within radius
		for (int attempts = 0; attempts < 30; attempts++)
		{
			int dx = Random.Range(-_territoryRadius, _territoryRadius + 1);
			int dy = Random.Range(-_territoryRadius, _territoryRadius + 1);
			
			Vector2Int targetPos = _gridPosition + new Vector2Int(dx, dy);
			
			// Clamp to grid bounds
			targetPos.x = Mathf.Clamp(targetPos.x, 0, gridSize.x - 1);
			targetPos.y = Mathf.Clamp(targetPos.y, 0, gridSize.y - 1);
			
			// Check distance is within radius (Manhattan distance)
			int manhattanDistance = Mathf.Abs(targetPos.x - _gridPosition.x) + Mathf.Abs(targetPos.y - _gridPosition.y);
			if (manhattanDistance > _territoryRadius)
			{
				continue;
			}
			
			if (EnvironmentManager.Instance.IsValidPosition(targetPos) && 
				EnvironmentManager.Instance.IsWalkable(targetPos))
			{
				return targetPos;
			}
		}
		
		// If all attempts failed, return null (will try again next turn)
		return null;
	}
	
	/// <summary>
	/// Checks if a position is within the territory radius of this spawner.
	/// </summary>
	public bool IsPositionInTerritory(Vector2Int position)
	{
		int manhattanDistance = Mathf.Abs(position.x - _gridPosition.x) + Mathf.Abs(position.y - _gridPosition.y);
		return manhattanDistance <= _territoryRadius;
	}

	/// <summary>
	/// Updates the sprite based on whether there are rabbits hiding in the den.
	/// </summary>
	private void UpdateSprite()
	{
		if (_spriteRenderer == null)
		{
			return;
		}

		// Switch sprite based on whether there are rabbits hiding
		if (_hidingRabbits.Count > 0)
		{
			// Occupied - there are rabbits hiding
			if (_occupiedSprite != null)
			{
				_spriteRenderer.sprite = _occupiedSprite;
			}
		}
		else
		{
			// Unoccupied - no rabbits hiding
			if (_unoccupiedSprite != null)
			{
				_spriteRenderer.sprite = _unoccupiedSprite;
			}
		}
	}

	/// <summary>
	/// Checks if the current season is Spring or Summer, which are valid breeding seasons for rabbits.
	/// </summary>
	private bool IsValidBreedingSeason()
	{
		if (TimeManager.Instance == null)
		{
			return false;
		}

		TimeManager.Season currentSeason = TimeManager.Instance.CurrentSeason;
		return currentSeason == TimeManager.Season.Spring || currentSeason == TimeManager.Season.Summer;
	}

	/// <summary>
	/// Called when a rabbit is attached to this spawner (via SetRabbitSpawner).
	/// Updates the cached count and tracking list.
	/// </summary>
	public void OnRabbitAttached(Animal rabbit)
	{
		if (rabbit == null)
		{
			return;
		}

		// Only add if not already in the list
		if (!_attachedRabbits.Contains(rabbit))
		{
			_attachedRabbits.Add(rabbit);
			_attachedRabbitCount++;
		}
	}

	/// <summary>
	/// Cleans up null rabbits from the attached rabbits list and updates the cached count.
	/// </summary>
	private void CleanupAttachedRabbits()
	{
		int removedCount = _attachedRabbits.RemoveAll(rabbit => rabbit == null);
		_attachedRabbitCount -= removedCount;
		
		// Ensure count doesn't go negative (safety check)
		if (_attachedRabbitCount < 0)
		{
			_attachedRabbitCount = 0;
		}
	}

	/// <summary>
	/// Spawns heart particles above the rabbit spawner to indicate breeding/spawning.
	/// Each particle spawns with random X and Y position offsets for visual variety.
	/// </summary>
	private void SpawnHeartParticles()
	{
		if (ParticleEffectManager.Instance == null)
		{
			return;
		}

		// Calculate base particle position slightly above the spawner
		Vector3 basePosition = transform.position;
		basePosition.y += 0.5f;

		// Spawn heart particles (2-4 hearts) with random position offsets
		int heartCount = Random.Range(2, 5);
		const float randomOffsetRange = 0.7f; // Random offset range in world units
		
		for (int i = 0; i < heartCount; i++)
		{
			// Add random X and Y offsets to each particle
			float randomX = Random.Range(-randomOffsetRange, randomOffsetRange);
			float randomY = Random.Range(-randomOffsetRange * 0.5f, randomOffsetRange * 0.5f); // Less vertical spread
			Vector3 randomPosition = basePosition + new Vector3(randomX, randomY, 0f);
			
			// Spawn each particle individually with its random position
			ParticleEffectManager.Instance.SpawnParticleEffect("Heart", randomPosition, 1);
		}
	}
}


