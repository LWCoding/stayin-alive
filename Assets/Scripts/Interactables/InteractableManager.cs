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
    
    private List<Den> _dens = new List<Den>();
    
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
        ClearAllInteractables();
        
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
                    den.OnAnimalEnter(animal);
                    animal.SetCurrentDen(den);
                }
            }
        }
    }
}

