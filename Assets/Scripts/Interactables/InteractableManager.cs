using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages interactable objects in the game, such as dens.
/// Handles spawning and tracking interactables from level data.
/// </summary>
public class InteractableManager : Singleton<InteractableManager>
{
    [Header("Interactable Settings")]
    [SerializeField] private Transform _interactableParent;
    
    /// <summary>
    /// Gets the interactable parent transform.
    /// </summary>
    public Transform InteractableParent => _interactableParent;
    
    [Header("Den Prefab")]
    [Tooltip("Prefab to use when spawning dens. Must have a Den component.")]
    [SerializeField] private GameObject _denPrefab;
    
	[Header("Rabbit Spawner Prefab")]
	[Tooltip("Prefab to use when spawning rabbit spawners. Must have a RabbitSpawner component.")]
	[SerializeField] private GameObject _rabbitSpawnerPrefab;
	
	[Header("Worm Spawner Prefab")]
	[Tooltip("Prefab to use when spawning worm spawners. Must have a WormSpawner component.")]
	[SerializeField] private GameObject _wormSpawnerPrefab;
	
	[Header("Bush Prefab")]
	[Tooltip("Prefab to use when spawning bushes. Must have a Bush component.")]
	[SerializeField] private GameObject _bushPrefab;
	
	[Header("Grass Prefab")]
	[Tooltip("Prefab to use when spawning grass interactables. Must have a Grass component.")]
	[SerializeField] private GameObject _grassPrefab;
	
    private List<Interactable> _allInteractables = new List<Interactable>();
    private List<Den> _dens = new List<Den>();
	private List<RabbitSpawner> _rabbitSpawners = new List<RabbitSpawner>();
	private List<PredatorDen> _predatorDens = new List<PredatorDen>();
	private List<WormSpawner> _wormSpawners = new List<WormSpawner>();
	private List<Bush> _bushes = new List<Bush>();
	private List<Grass> _grasses = new List<Grass>();

	/// <summary>
	/// Gets all dens in the scene.
	/// </summary>
	public List<Den> Dens => _dens.Where(d => d != null).ToList();

	/// <summary>
	/// Gets all rabbit spawners in the scene.
	/// </summary>
	public List<RabbitSpawner> RabbitSpawners => _rabbitSpawners.Where(s => s != null).ToList();

	/// <summary>
	/// Gets all predator dens in the scene.
	/// </summary>
	public List<PredatorDen> PredatorDens => _predatorDens.Where(d => d != null).ToList();

	/// <summary>
	/// Gets all worm spawners in the scene.
	/// </summary>
	public List<WormSpawner> WormSpawners => _wormSpawners.Where(s => s != null).ToList();

	/// <summary>
	/// Gets all bushes in the scene.
	/// </summary>
	public List<Bush> Bushes => _bushes.Where(b => b != null).ToList();

	/// <summary>
	/// Gets all grass interactables in the scene.
	/// </summary>
	public List<Grass> Grasses => _grasses.Where(g => g != null).ToList();
    
    protected override void Awake()
    {
        base.Awake();
    }

	/// <summary>
	/// Checks if there is any interactable at the specified grid position.
	/// </summary>
	/// <param name="gridPosition">Grid position to check</param>
	/// <returns>True if any interactable exists at the position, false otherwise</returns>
	public bool HasInteractableAtPosition(Vector2Int gridPosition)
	{
		// Filter out null references
		for (int i = _allInteractables.Count - 1; i >= 0; i--)
		{
			if (_allInteractables[i] == null)
			{
				_allInteractables.RemoveAt(i);
				continue;
			}

			if (_allInteractables[i].GridPosition == gridPosition)
			{
				return true;
			}
		}

		return false;
	}
    
    /// <summary>
    /// Clears all interactables from the scene.
    /// </summary>
    public void ClearAllInteractables()
    {
        foreach (Interactable interactable in _allInteractables)
        {
            if (interactable != null)
            {
                Destroy(interactable.gameObject);
            }
        }
        _allInteractables.Clear();
        _dens.Clear();
		_rabbitSpawners.Clear();
		_predatorDens.Clear();
		_wormSpawners.Clear();
		_bushes.Clear();
		_grasses.Clear();
    }
    
    /// <summary>
    /// Spawns a den at the specified grid position.
    /// </summary>
    /// <param name="gridPosition">Grid position to spawn the den at</param>
    /// <returns>The spawned Den component, or null if prefab is not assigned</returns>
    public Den SpawnDen(Vector2Int gridPosition)
    {
        if (_denPrefab == null)
        {
            Debug.LogError("InteractableManager: Den prefab is not assigned! Please assign a den prefab in the Inspector.");
            return null;
        }
        
        if (EnvironmentManager.Instance == null)
        {
            Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
            return null;
        }
        
        if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
        {
            Debug.LogWarning($"InteractableManager: Cannot spawn den at invalid position ({gridPosition.x}, {gridPosition.y}).");
            return null;
        }
        
        // Check if there's already an interactable at this position
        if (HasInteractableAtPosition(gridPosition))
        {
            Debug.LogWarning($"InteractableManager: Cannot spawn den at ({gridPosition.x}, {gridPosition.y}) - an interactable already exists there.");
            return null;
        }
        
        // Instantiate the den prefab
        GameObject denObj = Instantiate(_denPrefab, _interactableParent);
        Den den = denObj.GetComponent<Den>();
        
        if (den == null)
        {
            Debug.LogError("InteractableManager: Den prefab does not have a Den component!");
            Destroy(denObj);
            return null;
        }
        
        // Initialize the den
        den.Initialize(gridPosition);
        
        _dens.Add(den);
        _allInteractables.Add(den);
        
        return den;
    }
    
    /// <summary>
    /// Spawns multiple dens from level data.
    /// </summary>
	public void SpawnDensFromLevelData(List<(int x, int y)> dens)
	{
		if (dens == null)
		{
			return;
		}

		foreach (var (x, y) in dens)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnDen(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: Den at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Spawns a rabbit spawner at the specified grid position.
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the rabbit spawner at</param>
	/// <returns>The spawned RabbitSpawner component, or null if prefab is not assigned</returns>
	public RabbitSpawner SpawnRabbitSpawner(Vector2Int gridPosition)
	{
		if (_rabbitSpawnerPrefab == null)
		{
			Debug.LogError("InteractableManager: Rabbit spawner prefab is not assigned! Please assign a rabbit spawner prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn rabbit spawner at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		GameObject spawnerObj = Instantiate(_rabbitSpawnerPrefab, _interactableParent);
		RabbitSpawner spawner = spawnerObj.GetComponent<RabbitSpawner>();

		if (spawner == null)
		{
			Debug.LogError("InteractableManager: Rabbit spawner prefab does not have a RabbitSpawner component!");
			Destroy(spawnerObj);
			return null;
		}

		spawner.Initialize(gridPosition);
		_rabbitSpawners.Add(spawner);
		_allInteractables.Add(spawner);

		return spawner;
	}

	/// <summary>
	/// Spawns rabbit spawners from level data.
	/// </summary>
	public void SpawnRabbitSpawnersFromLevelData(List<(int x, int y)> spawners)
	{
		if (spawners == null)
		{
			return;
		}

		foreach (var (x, y) in spawners)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnRabbitSpawner(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: Rabbit spawner at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Gets the rabbit spawner at the specified grid position, if any.
	/// </summary>
	public RabbitSpawner GetRabbitSpawnerAtPosition(Vector2Int gridPosition)
	{
		for (int i = _rabbitSpawners.Count - 1; i >= 0; i--)
		{
			if (_rabbitSpawners[i] == null)
			{
				_rabbitSpawners.RemoveAt(i);
				continue;
			}

			if (_rabbitSpawners[i].GridPosition == gridPosition)
			{
				return _rabbitSpawners[i];
			}
		}

		return null;
	}
    
    /// <summary>
    /// Gets the den at the specified grid position, if any.
    /// </summary>
    /// <param name="gridPosition">Grid position to check</param>
    /// <returns>The Den at that position, or null if no den exists there</returns>
    public Den GetDenAtPosition(Vector2Int gridPosition)
    {
        // Filter out null references in case any dens were destroyed
        for (int i = _dens.Count - 1; i >= 0; i--)
        {
            if (_dens[i] == null)
            {
                _dens.RemoveAt(i);
            }
        }
        
        foreach (Den den in _dens)
        {
            if (den != null && den.GridPosition == gridPosition)
            {
                return den;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Removes a den from the dens list. Called when a den is destroyed.
    /// </summary>
    public void RemoveDen(Den den)
    {
        if (den != null && _dens != null)
        {
            _dens.Remove(den);
            _allInteractables.Remove(den);
        }
    }

	/// <summary>
	/// Removes a rabbit spawner from the spawner list. Called when a spawner is destroyed.
	/// </summary>
	public void RemoveRabbitSpawner(RabbitSpawner spawner)
	{
		if (spawner != null && _rabbitSpawners != null)
		{
			_rabbitSpawners.Remove(spawner);
			_allInteractables.Remove(spawner);
		}
	}


	/// <summary>
	/// Gets the predator den at the specified grid position, if any.
	/// </summary>
	public PredatorDen GetPredatorDenAtPosition(Vector2Int gridPosition)
	{
		for (int i = _predatorDens.Count - 1; i >= 0; i--)
		{
			if (_predatorDens[i] == null)
			{
				_predatorDens.RemoveAt(i);
				continue;
			}

			if (_predatorDens[i].GridPosition == gridPosition)
			{
				return _predatorDens[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Registers a predator den with the manager. Called when a den is spawned.
	/// </summary>
	public void RegisterPredatorDen(PredatorDen predatorDen)
	{
		if (predatorDen != null && _predatorDens != null)
		{
			if (!_predatorDens.Contains(predatorDen))
			{
				_predatorDens.Add(predatorDen);
				_allInteractables.Add(predatorDen);
			}
		}
	}

	/// <summary>
	/// Removes a predator den from the dens list. Called when a den is destroyed.
	/// </summary>
	public void RemovePredatorDen(PredatorDen predatorDen)
	{
		if (predatorDen != null && _predatorDens != null)
		{
			_predatorDens.Remove(predatorDen);
			_allInteractables.Remove(predatorDen);
		}
	}

	/// <summary>
	/// Spawns a worm spawner at the specified grid position.
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the worm spawner at</param>
	/// <returns>The spawned WormSpawner component, or null if prefab is not assigned</returns>
	public WormSpawner SpawnWormSpawner(Vector2Int gridPosition)
	{
		if (_wormSpawnerPrefab == null)
		{
			Debug.LogError("InteractableManager: Worm spawner prefab is not assigned! Please assign a worm spawner prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn worm spawner at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		GameObject spawnerObj = Instantiate(_wormSpawnerPrefab, _interactableParent);
		WormSpawner spawner = spawnerObj.GetComponent<WormSpawner>();

		if (spawner == null)
		{
			Debug.LogError("InteractableManager: Worm spawner prefab does not have a WormSpawner component!");
			Destroy(spawnerObj);
			return null;
		}

		spawner.Initialize(gridPosition);
		_wormSpawners.Add(spawner);
		_allInteractables.Add(spawner);

		return spawner;
	}

	/// <summary>
	/// Spawns worm spawners from level data.
	/// </summary>
	public void SpawnWormSpawnersFromLevelData(List<(int x, int y)> spawners)
	{
		if (spawners == null)
		{
			return;
		}

		foreach (var (x, y) in spawners)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnWormSpawner(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: Worm spawner at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Gets the worm spawner at the specified grid position, if any.
	/// </summary>
	public WormSpawner GetWormSpawnerAtPosition(Vector2Int gridPosition)
	{
		for (int i = _wormSpawners.Count - 1; i >= 0; i--)
		{
			if (_wormSpawners[i] == null)
			{
				_wormSpawners.RemoveAt(i);
				continue;
			}

			if (_wormSpawners[i].GridPosition == gridPosition)
			{
				return _wormSpawners[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Removes a worm spawner from the spawner list. Called when a spawner is destroyed.
	/// </summary>
	public void RemoveWormSpawner(WormSpawner spawner)
	{
		if (spawner != null && _wormSpawners != null)
		{
			_wormSpawners.Remove(spawner);
			_allInteractables.Remove(spawner);
		}
	}

	/// <summary>
	/// Spawns a bush at the specified grid position.
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the bush at</param>
	/// <returns>The spawned Bush component, or null if prefab is not assigned</returns>
	public Bush SpawnBush(Vector2Int gridPosition)
	{
		if (_bushPrefab == null)
		{
			Debug.LogError("InteractableManager: Bush prefab is not assigned! Please assign a bush prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn bush at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		GameObject bushObj = Instantiate(_bushPrefab, _interactableParent);
		Bush bush = bushObj.GetComponent<Bush>();

		if (bush == null)
		{
			Debug.LogError("InteractableManager: Bush prefab does not have a Bush component!");
			Destroy(bushObj);
			return null;
		}

		bush.Initialize(gridPosition);
		_bushes.Add(bush);
		_allInteractables.Add(bush);

		return bush;
	}

	/// <summary>
	/// Spawns bushes from level data.
	/// </summary>
	public void SpawnBushesFromLevelData(List<(int x, int y)> bushes)
	{
		if (bushes == null)
		{
			return;
		}

		foreach (var (x, y) in bushes)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnBush(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: Bush at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Gets the bush at the specified grid position, if any.
	/// </summary>
	public Bush GetBushAtPosition(Vector2Int gridPosition)
	{
		for (int i = _bushes.Count - 1; i >= 0; i--)
		{
			if (_bushes[i] == null)
			{
				_bushes.RemoveAt(i);
				continue;
			}

			if (_bushes[i].GridPosition == gridPosition)
			{
				return _bushes[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Removes a bush from the bushes list. Called when a bush is destroyed.
	/// </summary>
	public void RemoveBush(Bush bush)
	{
		if (bush != null && _bushes != null)
		{
			_bushes.Remove(bush);
			_allInteractables.Remove(bush);
		}
	}

	/// <summary>
	/// Spawns a grass interactable at the specified grid position.
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the grass at</param>
	/// <returns>The spawned Grass component, or null if prefab is not assigned</returns>
	public Grass SpawnGrass(Vector2Int gridPosition)
	{
		if (_grassPrefab == null)
		{
			Debug.LogError("InteractableManager: Grass prefab is not assigned! Please assign a grass prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn grass at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		GameObject grassObj = Instantiate(_grassPrefab, _interactableParent);
		Grass grass = grassObj.GetComponent<Grass>();

		if (grass == null)
		{
			Debug.LogError("InteractableManager: Grass prefab does not have a Grass component!");
			Destroy(grassObj);
			return null;
		}

		grass.Initialize(gridPosition);
		_grasses.Add(grass);
		_allInteractables.Add(grass);

		return grass;
	}

	/// <summary>
	/// Spawns grass interactables from level data.
	/// </summary>
	public void SpawnGrassesFromLevelData(List<(int x, int y)> grasses)
	{
		if (grasses == null)
		{
			return;
		}

		foreach (var (x, y) in grasses)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnGrass(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: Grass at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Gets the grass interactable at the specified grid position, if any.
	/// </summary>
	public Grass GetGrassAtPosition(Vector2Int gridPosition)
	{
		for (int i = _grasses.Count - 1; i >= 0; i--)
		{
			if (_grasses[i] == null)
			{
				_grasses.RemoveAt(i);
				continue;
			}

			if (_grasses[i].GridPosition == gridPosition)
			{
				return _grasses[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Removes a grass interactable from the grasses list. Called when a grass is destroyed.
	/// </summary>
	public void RemoveGrass(Grass grass)
	{
		if (grass != null && _grasses != null)
		{
			_grasses.Remove(grass);
			_allInteractables.Remove(grass);
		}
	}
    
	/// <summary>
	/// Registers any existing controllable animals that are currently on dens.
	/// This is a safety check to ensure animals that spawn on dens are properly registered.
	/// </summary>
	public void RegisterAnimalsOnDens()
	{
		if (AnimalManager.Instance == null)
		{
			return;
		}
		
		List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
		foreach (Animal animal in animals)
		{
			if (animal != null && animal.IsControllable)
			{
				Den den = GetDenAtPosition(animal.GridPosition);
				if (den != null && !den.IsAnimalInDen(animal))
				{
					// OnAnimalEnter will handle setting the CurrentHideable reference
					den.OnAnimalEnter(animal);
				}
			}
		}
	}

	/// <summary>
	/// Handles Winter season grass reduction. Loops through all grass and applies a 50% chance
	/// to reduce each grass to its next level (Full → Growing, Growing → Destroyed).
	/// </summary>
	public void HandleWinterGrassReduction()
	{
		// Get all grass and filter out null references
		List<Grass> allGrass = Grasses;
		
		foreach (Grass grass in allGrass)
		{
			if (grass == null)
			{
				continue;
			}

			// 50% chance to reduce grass level
			if (Random.Range(0f, 1f) < 0.5f)
			{
				grass.ReduceLevelWithoutHarvest();
			}
		}
	}
}

