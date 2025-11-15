using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages interactable objects in the game, such as dens.
/// Handles spawning and tracking interactables from level data.
/// </summary>
public class InteractableManager : Singleton<InteractableManager>
{
    [Header("Interactable Settings")]
    [SerializeField] private Transform _interactableParent;
    
    [Header("Den Prefab")]
    [Tooltip("Prefab to use when spawning dens. Must have a Den component.")]
    [SerializeField] private GameObject _denPrefab;
    
	[Header("Rabbit Spawner Prefab")]
	[Tooltip("Prefab to use when spawning rabbit spawners. Must have a RabbitSpawner component.")]
	[SerializeField] private GameObject _rabbitSpawnerPrefab;
	
	[Header("Predator Den Prefab")]
	[Tooltip("Prefab to use when spawning predator dens. Must have a PredatorDen component.")]
	[SerializeField] private GameObject _predatorDenPrefab;
	
	[Header("Worm Spawner Prefab")]
	[Tooltip("Prefab to use when spawning worm spawners. Must have a WormSpawner component.")]
	[SerializeField] private GameObject _wormSpawnerPrefab;
	
	[Header("Bush Prefab")]
	[Tooltip("Prefab to use when spawning bushes. Must have a Bush component.")]
	[SerializeField] private GameObject _bushPrefab;
	
    private List<Den> _dens = new List<Den>();
	private List<RabbitSpawner> _rabbitSpawners = new List<RabbitSpawner>();
	private List<PredatorDen> _predatorDens = new List<PredatorDen>();
	private List<WormSpawner> _wormSpawners = new List<WormSpawner>();
	private List<Bush> _bushes = new List<Bush>();
    
    protected override void Awake()
    {
        base.Awake();
        
        // Create parent if not assigned
        if (_interactableParent == null)
        {
            GameObject parentObj = new GameObject("Interactables");
            _interactableParent = parentObj.transform;
        }
    }
    
    /// <summary>
    /// Clears all interactables from the scene.
    /// </summary>
    public void ClearAllInteractables()
    {
        foreach (Den den in _dens)
        {
            if (den != null)
            {
                Destroy(den.gameObject);
            }
        }
        _dens.Clear();

		foreach (RabbitSpawner spawner in _rabbitSpawners)
		{
			if (spawner != null)
			{
				Destroy(spawner.gameObject);
			}
		}
		_rabbitSpawners.Clear();

		foreach (PredatorDen predatorDen in _predatorDens)
		{
			if (predatorDen != null)
			{
				Destroy(predatorDen.gameObject);
			}
		}
		_predatorDens.Clear();

		foreach (WormSpawner spawner in _wormSpawners)
		{
			if (spawner != null)
			{
				Destroy(spawner.gameObject);
			}
		}
		_wormSpawners.Clear();

		foreach (Bush bush in _bushes)
		{
			if (bush != null)
			{
				Destroy(bush.gameObject);
			}
		}
		_bushes.Clear();
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
        
        Debug.Log($"InteractableManager: Spawned den at ({gridPosition.x}, {gridPosition.y})");
        
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

		Debug.Log($"InteractableManager: Spawned rabbit spawner at ({gridPosition.x}, {gridPosition.y})");

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
	/// Gets all rabbit spawners in the scene.
	/// </summary>
	public List<RabbitSpawner> GetAllRabbitSpawners()
	{
		List<RabbitSpawner> validSpawners = new List<RabbitSpawner>();
		for (int i = _rabbitSpawners.Count - 1; i >= 0; i--)
		{
			if (_rabbitSpawners[i] == null)
			{
				_rabbitSpawners.RemoveAt(i);
			}
			else
			{
				validSpawners.Add(_rabbitSpawners[i]);
			}
		}

		return validSpawners;
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
    /// Gets all dens in the scene.
    /// </summary>
    public List<Den> GetAllDens()
    {
        // Filter out null references
        List<Den> validDens = new List<Den>();
        for (int i = _dens.Count - 1; i >= 0; i--)
        {
            if (_dens[i] == null)
            {
                _dens.RemoveAt(i);
            }
            else
            {
                validDens.Add(_dens[i]);
            }
        }
        return validDens;
    }
    
    /// <summary>
    /// Removes a den from the dens list. Called when a den is destroyed.
    /// </summary>
    public void RemoveDen(Den den)
    {
        if (den != null && _dens != null)
        {
            _dens.Remove(den);
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
		}
	}

	/// <summary>
	/// Spawns a predator den at the specified grid position with a specific predator type.
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the predator den at</param>
	/// <param name="predatorType">Type of predator this den is for (e.g., "Wolf", "Hawk")</param>
	/// <returns>The spawned PredatorDen component, or null if prefab is not assigned</returns>
	public PredatorDen SpawnPredatorDen(Vector2Int gridPosition, string predatorType)
	{
		if (_predatorDenPrefab == null)
		{
			Debug.LogError("InteractableManager: Predator den prefab is not assigned! Please assign a predator den prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn predator den at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		GameObject denObj = Instantiate(_predatorDenPrefab, _interactableParent);
		PredatorDen predatorDen = denObj.GetComponent<PredatorDen>();

		if (predatorDen == null)
		{
			Debug.LogError("InteractableManager: Predator den prefab does not have a PredatorDen component!");
			Destroy(denObj);
			return null;
		}

		predatorDen.Initialize(gridPosition, predatorType);
		_predatorDens.Add(predatorDen);

		Debug.Log($"InteractableManager: Spawned predator den for '{predatorType}' at ({gridPosition.x}, {gridPosition.y})");

		return predatorDen;
	}

	/// <summary>
	/// Spawns predator dens from level data.
	/// </summary>
	public void SpawnPredatorDensFromLevelData(List<(int x, int y, string predatorType)> predatorDens)
	{
		if (predatorDens == null)
		{
			return;
		}

		foreach (var (x, y, predatorType) in predatorDens)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnPredatorDen(gridPos, predatorType);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: Predator den at ({x}, {y}) is out of bounds!");
			}
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
	/// Gets all predator dens in the scene.
	/// </summary>
	public List<PredatorDen> GetAllPredatorDens()
	{
		List<PredatorDen> validDens = new List<PredatorDen>();
		for (int i = _predatorDens.Count - 1; i >= 0; i--)
		{
			if (_predatorDens[i] == null)
			{
				_predatorDens.RemoveAt(i);
			}
			else
			{
				validDens.Add(_predatorDens[i]);
			}
		}

		return validDens;
	}

	/// <summary>
	/// Removes a predator den from the dens list. Called when a den is destroyed.
	/// </summary>
	public void RemovePredatorDen(PredatorDen predatorDen)
	{
		if (predatorDen != null && _predatorDens != null)
		{
			_predatorDens.Remove(predatorDen);
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

		Debug.Log($"InteractableManager: Spawned worm spawner at ({gridPosition.x}, {gridPosition.y})");

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
	/// Gets all worm spawners in the scene.
	/// </summary>
	public List<WormSpawner> GetAllWormSpawners()
	{
		List<WormSpawner> validSpawners = new List<WormSpawner>();
		for (int i = _wormSpawners.Count - 1; i >= 0; i--)
		{
			if (_wormSpawners[i] == null)
			{
				_wormSpawners.RemoveAt(i);
			}
			else
			{
				validSpawners.Add(_wormSpawners[i]);
			}
		}

		return validSpawners;
	}

	/// <summary>
	/// Removes a worm spawner from the spawner list. Called when a spawner is destroyed.
	/// </summary>
	public void RemoveWormSpawner(WormSpawner spawner)
	{
		if (spawner != null && _wormSpawners != null)
		{
			_wormSpawners.Remove(spawner);
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

		Debug.Log($"InteractableManager: Spawned bush at ({gridPosition.x}, {gridPosition.y})");

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
	/// Gets all bushes in the scene.
	/// </summary>
	public List<Bush> GetAllBushes()
	{
		List<Bush> validBushes = new List<Bush>();
		for (int i = _bushes.Count - 1; i >= 0; i--)
		{
			if (_bushes[i] == null)
			{
				_bushes.RemoveAt(i);
			}
			else
			{
				validBushes.Add(_bushes[i]);
			}
		}

		return validBushes;
	}

	/// <summary>
	/// Removes a bush from the bushes list. Called when a bush is destroyed.
	/// </summary>
	public void RemoveBush(Bush bush)
	{
		if (bush != null && _bushes != null)
		{
			_bushes.Remove(bush);
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
            if (animal != null && animal.IsControllable && animal is ControllableAnimal controllable)
            {
                Den den = GetDenAtPosition(animal.GridPosition);
                if (den != null && !den.IsAnimalInDen(animal))
                {
                    den.OnAnimalEnter(animal);
                    controllable.SetCurrentDen(den);
                }
            }
        }
    }
}

