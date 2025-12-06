using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages interactable objects in the game, such as dens.
/// Handles spawning and tracking interactables from level data.
/// </summary>
public class InteractableManager : Singleton<InteractableManager>
{
    /// <summary>
    /// Event fired when a den is built by the player (not during level load).
    /// </summary>
    public event Action<Den> OnDenBuiltByPlayer;
    
    /// <summary>
    /// Notifies subscribers that a den was built by the player.
    /// Called from SticksItem when player places a den.
    /// </summary>
    public void NotifyDenBuiltByPlayer(Den den)
    {
        OnDenBuiltByPlayer?.Invoke(den);
    }
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
	
	[Header("Tree Prefab")]
	[Tooltip("Prefab to use when spawning trees. Must have a Tree component.")]
	[SerializeField] private GameObject _treePrefab;
	
	[Header("BeeTree Prefab")]
	[Tooltip("Prefab to use when spawning bee trees. Must have a BeeTree component.")]
	[SerializeField] private GameObject _beeTreePrefab;
	
	[Header("GrassPatch Prefab")]
	[Tooltip("Prefab to use when spawning grass patches. Must have a GrassPatch component.")]
	[SerializeField] private GameObject _grassPatchPrefab;
    
    [Header("Pooling Settings")]
    [Tooltip("Initial pool size for each interactable type. Pre-pooled at game start.")]
    [SerializeField] private int initialPoolSizePerType = 10;
    
    [Tooltip("Maximum pool size per type. If exceeded, objects will be destroyed instead of pooled.")]
    [SerializeField] private int maxPoolSizePerType = 25;
    
    // Pools for each interactable type
    private Queue<Den> _denPool = new Queue<Den>();
    private Queue<RabbitSpawner> _rabbitSpawnerPool = new Queue<RabbitSpawner>();
    private Queue<WormSpawner> _wormSpawnerPool = new Queue<WormSpawner>();
    private Queue<Bush> _bushPool = new Queue<Bush>();
    private Queue<Grass> _grassPool = new Queue<Grass>();
    private Queue<Tree> _treePool = new Queue<Tree>();
    private Queue<BeeTree> _beeTreePool = new Queue<BeeTree>();
    private Queue<GrassPatch> _grassPatchPool = new Queue<GrassPatch>();
    
    private List<Interactable> _allInteractables = new List<Interactable>();
    private List<Den> _dens = new List<Den>();
	private List<RabbitSpawner> _rabbitSpawners = new List<RabbitSpawner>();
	private List<PredatorDen> _predatorDens = new List<PredatorDen>();
	private List<WormSpawner> _wormSpawners = new List<WormSpawner>();
	private List<Bush> _bushes = new List<Bush>();
	private List<Grass> _grasses = new List<Grass>();
	private List<Tree> _trees = new List<Tree>();
	private List<BeeTree> _beeTrees = new List<BeeTree>();
	private List<GrassPatch> _grassPatches = new List<GrassPatch>();

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

	/// <summary>
	/// Gets all trees in the scene.
	/// </summary>
	public List<Tree> Trees => _trees.Where(t => t != null).ToList();

	/// <summary>
	/// Gets all bee trees in the scene.
	/// </summary>
	public List<BeeTree> BeeTrees => _beeTrees.Where(bt => bt != null).ToList();

	/// <summary>
	/// Gets all grass patches in the scene.
	/// </summary>
	public List<GrassPatch> GrassPatches => _grassPatches.Where(gp => gp != null).ToList();
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Start()
    {
        // Pre-pool interactables at game start/level loading
        PrePoolInteractables();
    }
    
    /// <summary>
    /// Pre-pools all interactable types at game start to avoid runtime instantiation.
    /// </summary>
    private void PrePoolInteractables()
    {
        PrePoolType(_denPrefab, _denPool, initialPoolSizePerType, "Den");
        PrePoolType(_rabbitSpawnerPrefab, _rabbitSpawnerPool, initialPoolSizePerType, "RabbitSpawner");
        PrePoolType(_wormSpawnerPrefab, _wormSpawnerPool, initialPoolSizePerType, "WormSpawner");
        PrePoolType(_bushPrefab, _bushPool, initialPoolSizePerType, "Bush");
        PrePoolType(_grassPrefab, _grassPool, initialPoolSizePerType, "Grass");
        PrePoolType(_treePrefab, _treePool, initialPoolSizePerType, "Tree");
        PrePoolType(_beeTreePrefab, _beeTreePool, initialPoolSizePerType, "BeeTree");
        PrePoolType(_grassPatchPrefab, _grassPatchPool, initialPoolSizePerType, "GrassPatch");
        
        Debug.Log($"InteractableManager: Pre-pooled {initialPoolSizePerType} of each interactable type.");
    }
    
    /// <summary>
    /// Generic method to pre-pool a specific type of interactable.
    /// </summary>
    private void PrePoolType<T>(GameObject prefab, Queue<T> pool, int count, string typeName) where T : Interactable
    {
        if (prefab == null)
        {
            Debug.LogWarning($"InteractableManager: Cannot pre-pool {typeName} - prefab is not assigned.");
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, _interactableParent);
            obj.SetActive(false);
            
            T interactable = obj.GetComponent<T>();
            if (interactable == null)
            {
                Debug.LogError($"InteractableManager: {typeName} prefab does not have a {typeof(T).Name} component!");
                Destroy(obj);
                continue;
            }
            
            pool.Enqueue(interactable);
        }
    }
    
    /// <summary>
    /// Gets an interactable from the pool, or creates a new one if pool is empty.
    /// </summary>
    private T GetFromPool<T>(GameObject prefab, Queue<T> pool, string typeName) where T : Interactable
    {
        T interactable = null;
        
        // Try to get from pool
        while (pool.Count > 0)
        {
            interactable = pool.Dequeue();
            if (interactable != null && interactable.gameObject != null)
            {
                break;
            }
            interactable = null;
        }
        
        // If pool is empty, create new one
        if (interactable == null)
        {
            if (prefab == null)
            {
                Debug.LogError($"InteractableManager: {typeName} prefab is not assigned!");
                return null;
            }
            
            GameObject obj = Instantiate(prefab, _interactableParent);
            interactable = obj.GetComponent<T>();
            if (interactable == null)
            {
                Debug.LogError($"InteractableManager: {typeName} prefab does not have a {typeof(T).Name} component!");
                Destroy(obj);
                return null;
            }
        }
        
        interactable.gameObject.SetActive(true);
        return interactable;
    }
    
    /// <summary>
    /// Returns an interactable to the pool for reuse.
    /// </summary>
    private void ReturnToPool<T>(T interactable, Queue<T> pool, int maxPoolSize) where T : Interactable
    {
        if (interactable == null || interactable.gameObject == null)
        {
            return;
        }
        
        // Remove from tracking lists directly (to avoid circular calls)
        _allInteractables.Remove(interactable);
        
        // Remove from specific type lists
        if (interactable is Den den)
        {
            _dens.Remove(den);
        }
        else if (interactable is RabbitSpawner rabbitSpawner)
        {
            _rabbitSpawners.Remove(rabbitSpawner);
        }
        else if (interactable is WormSpawner wormSpawner)
        {
            _wormSpawners.Remove(wormSpawner);
        }
        else if (interactable is Bush bush)
        {
            _bushes.Remove(bush);
        }
        else if (interactable is Grass grass)
        {
            _grasses.Remove(grass);
        }
        else if (interactable is Tree tree)
        {
            _trees.Remove(tree);
        }
        else if (interactable is BeeTree beeTree)
        {
            _beeTrees.Remove(beeTree);
        }
        else if (interactable is GrassPatch grassPatch)
        {
            _grassPatches.Remove(grassPatch);
        }
        
        // Deactivate and reset
        interactable.gameObject.SetActive(false);
        
        // Return to pool if under max size
        if (pool.Count < maxPoolSize)
        {
            pool.Enqueue(interactable);
        }
        else
        {
            // Pool is full, destroy the object
            Destroy(interactable.gameObject);
        }
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
			
			// Check if this is a multi-tile interactable (like GrassPatch) that occupies this position
			if (_allInteractables[i] is GrassPatch grassPatch)
			{
				List<Vector2Int> occupiedPositions = grassPatch.GetOccupiedPositions();
				if (occupiedPositions.Contains(gridPosition))
				{
					return true;
				}
			}
		}

		return false;
	}
	
	/// <summary>
	/// Gets the interactable at the specified grid position, if any.
	/// </summary>
	/// <param name="gridPosition">Grid position to check</param>
	/// <returns>The interactable at that position, or null if no interactable exists there</returns>
	public Interactable GetInteractableAtPosition(Vector2Int gridPosition)
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
				return _allInteractables[i];
			}
			
			// Check if this is a multi-tile interactable (like GrassPatch) that occupies this position
			if (_allInteractables[i] is GrassPatch grassPatch)
			{
				List<Vector2Int> occupiedPositions = grassPatch.GetOccupiedPositions();
				if (occupiedPositions.Contains(gridPosition))
				{
					return _allInteractables[i];
				}
			}
		}

		return null;
	}
	
	/// <summary>
	/// Removes an interactable from the manager's tracking lists.
	/// </summary>
	/// <param name="interactable">The interactable to remove</param>
	public void RemoveInteractable(Interactable interactable)
	{
		if (interactable == null)
		{
			return;
		}
		
		_allInteractables.Remove(interactable);
		
		// Remove from specific type lists
		if (interactable is Den den)
		{
			_dens.Remove(den);
		}
		else if (interactable is RabbitSpawner rabbitSpawner)
		{
			_rabbitSpawners.Remove(rabbitSpawner);
		}
		else if (interactable is PredatorDen predatorDen)
		{
			_predatorDens.Remove(predatorDen);
		}
		else if (interactable is WormSpawner wormSpawner)
		{
			_wormSpawners.Remove(wormSpawner);
		}
		else if (interactable is Bush bush)
		{
			_bushes.Remove(bush);
		}
		else if (interactable is Grass grass)
		{
			_grasses.Remove(grass);
		}
		else if (interactable is Tree tree)
		{
			_trees.Remove(tree);
		}
		else if (interactable is BeeTree beeTree)
		{
			_beeTrees.Remove(beeTree);
		}
		else if (interactable is GrassPatch grassPatch)
		{
			_grassPatches.Remove(grassPatch);
		}
	}
	
	/// <summary>
	/// Checks if an area is available for placing a multi-tile interactable.
	/// Returns true if all positions in the area are valid and not occupied (or can be cleared).
	/// </summary>
	/// <param name="positions">List of grid positions to check</param>
	/// <param name="checkTileTypes">If true, also checks that tiles are not water or obstacles</param>
	/// <returns>True if the area is available, false otherwise</returns>
	public bool IsAreaAvailable(List<Vector2Int> positions, bool checkTileTypes = true)
	{
		if (EnvironmentManager.Instance == null)
		{
			return false;
		}
		
		foreach (Vector2Int pos in positions)
		{
			// Check if position is valid
			if (!EnvironmentManager.Instance.IsValidPosition(pos))
			{
				return false;
			}
			
			// Check tile types if requested
			if (checkTileTypes)
			{
				TileType tileType = EnvironmentManager.Instance.GetTileType(pos);
				if (tileType == TileType.Water || tileType == TileType.Obstacle)
				{
					return false;
				}
			}
		}
		
		return true;
	}
    
    /// <summary>
    /// Clears all interactables from the scene.
    /// Returns them to the pool instead of destroying them.
    /// </summary>
    public void ClearAllInteractables()
    {
        // Create copies of lists to avoid modification during iteration
        List<Den> densCopy = new List<Den>(_dens);
        List<RabbitSpawner> rabbitSpawnersCopy = new List<RabbitSpawner>(_rabbitSpawners);
        List<WormSpawner> wormSpawnersCopy = new List<WormSpawner>(_wormSpawners);
        List<Bush> bushesCopy = new List<Bush>(_bushes);
        List<Grass> grassesCopy = new List<Grass>(_grasses);
        List<Tree> treesCopy = new List<Tree>(_trees);
        List<BeeTree> beeTreesCopy = new List<BeeTree>(_beeTrees);
        List<GrassPatch> grassPatchesCopy = new List<GrassPatch>(_grassPatches);
        
        // Return all interactables to their respective pools
        foreach (Den den in densCopy)
        {
            if (den != null)
            {
                ReturnToPool(den, _denPool, maxPoolSizePerType);
            }
        }
        
        foreach (RabbitSpawner spawner in rabbitSpawnersCopy)
        {
            if (spawner != null)
            {
                ReturnToPool(spawner, _rabbitSpawnerPool, maxPoolSizePerType);
            }
        }
        
        foreach (WormSpawner spawner in wormSpawnersCopy)
        {
            if (spawner != null)
            {
                ReturnToPool(spawner, _wormSpawnerPool, maxPoolSizePerType);
            }
        }
        
        foreach (Bush bush in bushesCopy)
        {
            if (bush != null)
            {
                ReturnToPool(bush, _bushPool, maxPoolSizePerType);
            }
        }
        
        foreach (Grass grass in grassesCopy)
        {
            if (grass != null)
            {
                ReturnToPool(grass, _grassPool, maxPoolSizePerType);
            }
        }
        
        foreach (Tree tree in treesCopy)
        {
            if (tree != null)
            {
                ReturnToPool(tree, _treePool, maxPoolSizePerType);
            }
        }
        
        foreach (BeeTree beeTree in beeTreesCopy)
        {
            if (beeTree != null)
            {
                ReturnToPool(beeTree, _beeTreePool, maxPoolSizePerType);
            }
        }
        
        foreach (GrassPatch grassPatch in grassPatchesCopy)
        {
            if (grassPatch != null)
            {
                ReturnToPool(grassPatch, _grassPatchPool, maxPoolSizePerType);
            }
        }
        
        // Clear tracking lists (ReturnToPool already handles this, but clear to be safe)
        _allInteractables.Clear();
        _dens.Clear();
		_rabbitSpawners.Clear();
		_predatorDens.Clear();
		_wormSpawners.Clear();
        _bushes.Clear();
		_grasses.Clear();
		_trees.Clear();
		_beeTrees.Clear();
		_grassPatches.Clear();
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
        
        // Get den from pool
        Den den = GetFromPool(_denPrefab, _denPool, "Den");
        
        if (den == null)
        {
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

		// Get rabbit spawner from pool
		RabbitSpawner spawner = GetFromPool(_rabbitSpawnerPrefab, _rabbitSpawnerPool, "RabbitSpawner");

		if (spawner == null)
		{
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
    /// Returns the den to the pool if possible.
    /// </summary>
    public void RemoveDen(Den den)
    {
        if (den != null && _dens != null)
        {
            ReturnToPool(den, _denPool, maxPoolSizePerType);
        }
    }

	/// <summary>
	/// Removes a rabbit spawner from the spawner list. Called when a spawner is destroyed.
	/// Returns the spawner to the pool if possible.
	/// </summary>
	public void RemoveRabbitSpawner(RabbitSpawner spawner)
	{
		if (spawner != null && _rabbitSpawners != null)
		{
			ReturnToPool(spawner, _rabbitSpawnerPool, maxPoolSizePerType);
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

		// Get worm spawner from pool
		WormSpawner spawner = GetFromPool(_wormSpawnerPrefab, _wormSpawnerPool, "WormSpawner");

		if (spawner == null)
		{
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
	/// Returns the spawner to the pool if possible.
	/// </summary>
	public void RemoveWormSpawner(WormSpawner spawner)
	{
		if (spawner != null && _wormSpawners != null)
		{
			ReturnToPool(spawner, _wormSpawnerPool, maxPoolSizePerType);
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

		// Get bush from pool
		Bush bush = GetFromPool(_bushPrefab, _bushPool, "Bush");

		if (bush == null)
		{
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
	/// Returns the bush to the pool if possible.
	/// </summary>
	public void RemoveBush(Bush bush)
	{
		if (bush != null && _bushes != null)
		{
			ReturnToPool(bush, _bushPool, maxPoolSizePerType);
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

		// Get grass from pool
		Grass grass = GetFromPool(_grassPrefab, _grassPool, "Grass");

		if (grass == null)
		{
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
	/// Returns the grass to the pool if possible.
	/// </summary>
	public void RemoveGrass(Grass grass)
	{
		if (grass != null && _grasses != null)
		{
			ReturnToPool(grass, _grassPool, maxPoolSizePerType);
		}
	}

	/// <summary>
	/// Registers an existing grass object with the manager (for prefab children that weren't spawned through SpawnGrass).
	/// </summary>
	/// <param name="grass">The grass object to register</param>
	public void RegisterGrass(Grass grass)
	{
		if (grass != null && !_grasses.Contains(grass))
		{
			_grasses.Add(grass);
			if (!_allInteractables.Contains(grass))
			{
				_allInteractables.Add(grass);
			}
		}
	}

	/// <summary>
	/// Spawns a tree at the specified grid position.
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the tree at</param>
	/// <returns>The spawned Tree component, or null if prefab is not assigned</returns>
	public Tree SpawnTree(Vector2Int gridPosition)
	{
		if (_treePrefab == null)
		{
			Debug.LogError("InteractableManager: Tree prefab is not assigned! Please assign a tree prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn tree at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		// Get tree from pool
		Tree tree = GetFromPool(_treePrefab, _treePool, "Tree");

		if (tree == null)
		{
			return null;
		}

		tree.Initialize(gridPosition);
		_trees.Add(tree);
		_allInteractables.Add(tree);

		return tree;
	}

	/// <summary>
	/// Spawns trees from level data.
	/// </summary>
	public void SpawnTreesFromLevelData(List<(int x, int y)> trees)
	{
		if (trees == null)
		{
			return;
		}

		foreach (var (x, y) in trees)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnTree(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: Tree at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Gets the tree at the specified grid position, if any.
	/// </summary>
	public Tree GetTreeAtPosition(Vector2Int gridPosition)
	{
		for (int i = _trees.Count - 1; i >= 0; i--)
		{
			if (_trees[i] == null)
			{
				_trees.RemoveAt(i);
				continue;
			}

			if (_trees[i].GridPosition == gridPosition)
			{
				return _trees[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Removes a tree from the trees list. Called when a tree is destroyed.
	/// Returns the tree to the pool if possible.
	/// </summary>
	public void RemoveTree(Tree tree)
	{
		if (tree != null && _trees != null)
		{
			ReturnToPool(tree, _treePool, maxPoolSizePerType);
		}
	}

	/// <summary>
	/// Spawns a bee tree at the specified grid position.
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the bee tree at</param>
	/// <returns>The spawned BeeTree component, or null if prefab is not assigned</returns>
	public BeeTree SpawnBeeTree(Vector2Int gridPosition)
	{
		if (_beeTreePrefab == null)
		{
			Debug.LogError("InteractableManager: BeeTree prefab is not assigned! Please assign a bee tree prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn bee tree at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		// Check if there's already an interactable at this position
		if (HasInteractableAtPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn bee tree at ({gridPosition.x}, {gridPosition.y}) - an interactable already exists there.");
			return null;
		}

		// Get bee tree from pool
		BeeTree beeTree = GetFromPool(_beeTreePrefab, _beeTreePool, "BeeTree");

		if (beeTree == null)
		{
			return null;
		}

		beeTree.Initialize(gridPosition);
		_beeTrees.Add(beeTree);
		_allInteractables.Add(beeTree);

		return beeTree;
	}

	/// <summary>
	/// Spawns bee trees from level data.
	/// </summary>
	public void SpawnBeeTreesFromLevelData(List<(int x, int y)> beeTrees)
	{
		if (beeTrees == null)
		{
			return;
		}

		foreach (var (x, y) in beeTrees)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnBeeTree(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: BeeTree at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Gets the bee tree at the specified grid position, if any.
	/// </summary>
	public BeeTree GetBeeTreeAtPosition(Vector2Int gridPosition)
	{
		for (int i = _beeTrees.Count - 1; i >= 0; i--)
		{
			if (_beeTrees[i] == null)
			{
				_beeTrees.RemoveAt(i);
				continue;
			}

			if (_beeTrees[i].GridPosition == gridPosition)
			{
				return _beeTrees[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Removes a bee tree from the bee trees list. Called when a bee tree is destroyed.
	/// Returns the bee tree to the pool if possible.
	/// </summary>
	public void RemoveBeeTree(BeeTree beeTree)
	{
		if (beeTree != null && _beeTrees != null)
		{
			ReturnToPool(beeTree, _beeTreePool, maxPoolSizePerType);
		}
	}

	/// <summary>
	/// Spawns a grass patch at the specified grid position (center of the patch).
	/// </summary>
	/// <param name="gridPosition">Grid position to spawn the grass patch at (center position)</param>
	/// <returns>The spawned GrassPatch component, or null if prefab is not assigned</returns>
	public GrassPatch SpawnGrassPatch(Vector2Int gridPosition)
	{
		if (_grassPatchPrefab == null)
		{
			Debug.LogError("InteractableManager: GrassPatch prefab is not assigned! Please assign a grass patch prefab in the Inspector.");
			return null;
		}

		if (EnvironmentManager.Instance == null)
		{
			Debug.LogError("InteractableManager: EnvironmentManager instance not found!");
			return null;
		}

		if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
		{
			Debug.LogWarning($"InteractableManager: Cannot spawn grass patch at invalid position ({gridPosition.x}, {gridPosition.y}).");
			return null;
		}

		// Get grass patch from pool
		GrassPatch grassPatch = GetFromPool(_grassPatchPrefab, _grassPatchPool, "GrassPatch");

		if (grassPatch == null)
		{
			return null;
		}

		grassPatch.Initialize(gridPosition);
		_grassPatches.Add(grassPatch);
		_allInteractables.Add(grassPatch);

		return grassPatch;
	}

	/// <summary>
	/// Spawns grass patches from level data.
	/// </summary>
	public void SpawnGrassPatchesFromLevelData(List<(int x, int y)> grassPatches)
	{
		if (grassPatches == null)
		{
			return;
		}

		foreach (var (x, y) in grassPatches)
		{
			Vector2Int gridPos = new Vector2Int(x, y);
			if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				SpawnGrassPatch(gridPos);
			}
			else
			{
				Debug.LogWarning($"InteractableManager: GrassPatch at ({x}, {y}) is out of bounds!");
			}
		}
	}

	/// <summary>
	/// Gets the grass patch at the specified grid position, if any.
	/// Checks if the position is within any grass patch's area.
	/// </summary>
	public GrassPatch GetGrassPatchAtPosition(Vector2Int gridPosition)
	{
		for (int i = _grassPatches.Count - 1; i >= 0; i--)
		{
			if (_grassPatches[i] == null)
			{
				_grassPatches.RemoveAt(i);
				continue;
			}

			// Check if position is within this patch's area
			List<Vector2Int> occupiedPositions = _grassPatches[i].GetOccupiedPositions();
			if (occupiedPositions.Contains(gridPosition))
			{
				return _grassPatches[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Removes a grass patch from the grass patches list. Called when a grass patch is destroyed.
	/// Returns the grass patch to the pool if possible.
	/// </summary>
	public void RemoveGrassPatch(GrassPatch grassPatch)
	{
		if (grassPatch != null && _grassPatches != null)
		{
			ReturnToPool(grassPatch, _grassPatchPool, maxPoolSizePerType);
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
	/// Handles Winter season grass reduction. Loops through all grass and applies a chance
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

		// 80% chance to reduce grass level
		if (UnityEngine.Random.Range(0f, 1f) < 0.8f)
		{
			grass.ReduceLevelWithoutHarvest();
		}
		}
	}

	/// <summary>
	/// Spawns interactables from the Interactables list in level data.
	/// </summary>
	public void SpawnInteractablesFromLevelData(List<InteractableData> interactables)
	{
		if (interactables == null)
		{
			return;
		}

		foreach (var interactableData in interactables)
		{
			Vector2Int gridPos = new Vector2Int(interactableData.X, interactableData.Y);
			
			if (EnvironmentManager.Instance != null && !EnvironmentManager.Instance.IsValidPosition(gridPos))
			{
				Debug.LogWarning($"InteractableManager: Interactable at ({interactableData.X}, {interactableData.Y}) is out of bounds!");
				continue;
			}

			switch (interactableData.Type)
			{
				case InteractableType.Den:
					SpawnDen(gridPos);
					break;
				case InteractableType.RabbitSpawner:
					SpawnRabbitSpawner(gridPos);
					break;
				case InteractableType.Bush:
					SpawnBush(gridPos);
					break;
				case InteractableType.Grass:
					SpawnGrass(gridPos);
					break;
				case InteractableType.Tree:
					SpawnTree(gridPos);
					break;
				case InteractableType.BeeTree:
					SpawnBeeTree(gridPos);
					break;
				case InteractableType.GrassPatch:
					SpawnGrassPatch(gridPos);
					break;
				case InteractableType.PredatorDen:
					// Predator dens are handled by PredatorAnimal.SpawnPredatorDensFromLevelData
					// This method is kept for now but won't spawn predator dens here
					break;
			}
		}
	}
}

