using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages animal instantiation and lifecycle. Handles creating animals from level data.
/// Loads AnimalData ScriptableObjects from Resources/Animals/ folder.
/// </summary>
public class AnimalManager : Singleton<AnimalManager>
{
    [Header("Animal Settings")]
    [SerializeField] private Transform _animalParent;

    private List<Animal> _animals = new List<Animal>();
    private Dictionary<string, AnimalData> _animalDataDictionary = new Dictionary<string, AnimalData>();
    private Animal _currentlySelectedAnimal;

    // Track animals visible in the camera viewport
    private List<Animal> _animalsInViewport = new List<Animal>();
    
    // Track predators currently targeting the controllable player
    private List<PredatorAnimal> _predatorsTargetingPlayer = new List<PredatorAnimal>();
    
    // Cache the main camera to avoid repeated lookups
    private Camera _mainCamera;
    
    // Cache the player (ControllableAnimal) to avoid repeated loops through all animals
    private ControllableAnimal _cachedPlayer = null;
    
    // Track when viewport needs updating (throttle updates)
    private int _viewportUpdateFrameCounter = 0;
    private const int VIEWPORT_UPDATE_INTERVAL = 3; // Update every 3 frames instead of every frame

    public Animal CurrentlySelectedAnimal => _currentlySelectedAnimal;
    
    /// <summary>
    /// Gets the cached player (ControllableAnimal) reference. Returns null if no player exists.
    /// This avoids looping through all animals every time the player is needed.
    /// </summary>
    public ControllableAnimal GetPlayer()
    {
        // If cached player is null or destroyed, try to find it
        if (_cachedPlayer == null)
        {
            RefreshPlayerCache();
        }
        return _cachedPlayer;
    }
    
    /// <summary>
    /// Refreshes the cached player reference by searching through animals.
    /// Should be called when animals are added/removed or when player might have changed.
    /// </summary>
    private void RefreshPlayerCache()
    {
        _cachedPlayer = null;
        for (int i = 0; i < _animals.Count; i++)
        {
            Animal animal = _animals[i];
            if (animal != null && animal.IsControllable && animal is ControllableAnimal controllable)
            {
                _cachedPlayer = controllable;
                break;
            }
        }
    }
    
    /// <summary>
    /// Gets the list of animals currently visible in the camera viewport (not obscured by fog of war).
    /// </summary>
    public List<Animal> GetAnimalsInViewport()
    {
        return new List<Animal>(_animalsInViewport);
    }

    /// <summary>
    /// Checks if an animal is currently in the camera viewport (not obscured by fog of war).
    /// </summary>
    public bool IsAnimalInViewport(Animal animal)
    {
        if (animal == null || _animalsInViewport == null)
        {
            return false;
        }

        return _animalsInViewport.Contains(animal);
    }

    protected override void Awake()
    {
        base.Awake();
        
        // Create parent if not assigned
        if (_animalParent == null)
        {
            GameObject parentObj = new GameObject("Animals");
            _animalParent = parentObj.transform;
        }

        // Load all AnimalData from Resources/Animals/ folder
        LoadAnimalData();
        
        // Cache the main camera
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        // Ensure camera is cached (may not be available in Awake)
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
        
        // Initialize the viewport list
        UpdateAnimalsInViewport();
    }

    private void Update()
    {
        // Throttle viewport updates - only update every N frames instead of every frame
        _viewportUpdateFrameCounter++;
        if (_viewportUpdateFrameCounter >= VIEWPORT_UPDATE_INTERVAL)
        {
            _viewportUpdateFrameCounter = 0;
            UpdateAnimalsInViewport();
        }
    }

    /// <summary>
    /// Loads all AnimalData ScriptableObjects from the Resources/Animals/ folder.
    /// </summary>
    private void LoadAnimalData()
    {
        AnimalData[] animalDataArray = Resources.LoadAll<AnimalData>("Animals");
        
        if (animalDataArray == null || animalDataArray.Length == 0)
        {
            Debug.LogWarning("AnimalManager: No AnimalData found in Resources/Animals/ folder! Please create AnimalData ScriptableObjects and place them in Resources/Animals/.");
            return;
        }

        _animalDataDictionary.Clear();

        foreach (AnimalData animalData in animalDataArray)
        {
            if (animalData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(animalData.animalName))
            {
                Debug.LogWarning($"AnimalManager: Found AnimalData with null or empty name: {animalData.name}");
                continue;
            }

            if (_animalDataDictionary.ContainsKey(animalData.animalName))
            {
                Debug.LogWarning($"AnimalManager: Duplicate animal name '{animalData.animalName}' found. Keeping first occurrence.");
                continue;
            }

            _animalDataDictionary[animalData.animalName] = animalData;
        }

        Debug.Log($"AnimalManager: Loaded {_animalDataDictionary.Count} animal data entries from Resources/Animals/");
    }

    /// <summary>
    /// Gets an AnimalData by name. Returns null if not found.
    /// </summary>
    private AnimalData GetAnimalData(string animalName)
    {
        if (_animalDataDictionary.TryGetValue(animalName, out AnimalData data))
        {
            return data;
        }

        Debug.LogWarning($"AnimalManager: Animal data not found for name '{animalName}'");
        return null;
    }

    /// <summary>
    /// Clears all animals from the scene.
    /// </summary>
    public void ClearAllAnimals()
    {
        ClearSelection();
        
        // Clear cached player reference
        _cachedPlayer = null;

        foreach (Animal animal in _animals)
        {
            if (animal != null)
            {
                Destroy(animal.gameObject);
            }
        }
        _animals.Clear();
    }

    /// <summary>
    /// Ensures that only one animal occupies a grid tile by reverting the most recent mover if needed.
    /// </summary>
    /// <param name="movedAnimal">The animal that recently changed tiles.</param>
    public void ResolveTileConflictsForAnimal(Animal movedAnimal)
    {
        if (movedAnimal == null)
        {
            return;
        }

        Vector2Int movedPosition = movedAnimal.GridPosition;
        bool shouldRevert = movedAnimal.EncounteredAnimalDuringMove;

        for (int i = _animals.Count - 1; i >= 0; i--)
        {
            Animal other = _animals[i];

            if (other == null)
            {
                _animals.RemoveAt(i);
                continue;
            }

            if (other == movedAnimal)
            {
                continue;
            }

            if (other.GridPosition == movedPosition)
            {
                shouldRevert = true;
                break;
            }
        }

        if (shouldRevert)
        {
            movedAnimal.ResetToPreviousGridPosition();
        }

        movedAnimal.ClearEncounteredAnimalDuringMoveFlag();
    }

    /// <summary>
    /// Checks whether there is another animal already occupying the specified grid position.
    /// </summary>
    public bool HasOtherAnimalAtPosition(Animal origin, Vector2Int position)
    {
        for (int i = _animals.Count - 1; i >= 0; i--)
        {
            Animal other = _animals[i];

            if (other == null)
            {
                _animals.RemoveAt(i);
                continue;
            }

            if (other == origin)
            {
                continue;
            }

            if (other.GridPosition == position)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Spawns an animal at the specified grid position by name.
    /// </summary>
    /// <param name="animalName">Name of the animal type to spawn</param>
    /// <param name="gridPosition">Grid position to spawn at</param>
    /// <param name="count">Number of animals in this group (defaults to 1)</param>
    /// <returns>The spawned Animal component, or null if animal data or prefab is not found</returns>
    public Animal SpawnAnimal(string animalName, Vector2Int gridPosition, int count = 1)
    {
        AnimalData animalData = GetAnimalData(animalName);
        if (animalData == null)
        {
            Debug.LogError($"AnimalManager: Animal data not found for '{animalName}'! Make sure the AnimalData ScriptableObject exists in Resources/Animals/ folder.");
            return null;
        }

        if (animalData.prefab == null)
        {
            Debug.LogError($"AnimalManager: Prefab is not assigned for animal '{animalName}'!");
            return null;
        }

        if (EnvironmentManager.Instance == null)
        {
            Debug.LogError("AnimalManager: EnvironmentManager instance not found!");
            return null;
        }

        // Instantiate the animal using the prefab from AnimalData
        GameObject animalObj = Instantiate(animalData.prefab, _animalParent);
        Animal animal = animalObj.GetComponent<Animal>();
        
        if (animal == null)
        {
            Debug.LogError($"AnimalManager: Animal prefab for '{animalName}' does not have an Animal component!");
            Destroy(animalObj);
            return null;
        }

        // Initialize the animal with its data
        animal.Initialize(animalData, gridPosition);
        
        // Set the animal count
        animal.SetAnimalCount(count);
        
        _animals.Add(animal);
        
        // If this is a controllable animal and we don't have a cached player, cache it
        if (_cachedPlayer == null && animal is ControllableAnimal controllable)
        {
            _cachedPlayer = controllable;
        }

        return animal;
    }

    /// <summary>
    /// Spawns multiple animals from level data.
    /// </summary>
    public void SpawnAnimalsFromLevelData(List<(string animalName, int x, int y, int count)> animals)
    {
        ClearAllAnimals();

        foreach (var (animalName, x, y, count) in animals)
        {
            Vector2Int gridPos = new Vector2Int(x, y);
            if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
            {
                SpawnAnimal(animalName, gridPos, count);
            }
            else
            {
                Debug.LogWarning($"AnimalManager: Animal '{animalName}' at ({x}, {y}) is out of bounds!");
            }
        }
    }

    /// <summary>
    /// Gets all animals in the scene. Filters out null references (destroyed animals).
    /// </summary>
    public List<Animal> GetAllAnimals()
    {
        // Filter out null references in case any animals were destroyed but not yet removed
        List<Animal> validAnimals = new List<Animal>();
        for (int i = _animals.Count - 1; i >= 0; i--)
        {
            if (_animals[i] == null)
            {
                _animals.RemoveAt(i);
            }
            else
            {
                validAnimals.Add(_animals[i]);
            }
        }
        return validAnimals;
    }

    /// <summary>
    /// Gets an animal by its name.
    /// </summary>
    /// <param name="animalName">Name of the animal to find</param>
    /// <returns>The first Animal with the given name, or null if not found</returns>
    public Animal GetAnimalByName(string animalName)
    {
        return _animals.Find(a => a != null && a.AnimalData != null && a.AnimalData.animalName == animalName);
    }

    /// <summary>
    /// Gets all animals of a specific type by name.
    /// </summary>
    /// <param name="animalName">Name of the animal type to find</param>
    /// <returns>List of all Animals with the given name</returns>
    public List<Animal> GetAnimalsByName(string animalName)
    {
        return _animals.FindAll(a => a != null && a.AnimalData != null && a.AnimalData.animalName == animalName);
    }

    /// <summary>
    /// Sets the currently selected animal.
    /// </summary>
    public void SetSelectedAnimal(Animal animal)
    {
        _currentlySelectedAnimal = animal;
    }

    /// <summary>
    /// Clears the selection state if the specified animal is selected.
    /// </summary>
    public void ClearSelectedAnimal(Animal animal)
    {
        if (animal != null && animal == _currentlySelectedAnimal)
        {
            _currentlySelectedAnimal = null;
        }
    }

    /// <summary>
    /// Clears any selected animal.
    /// </summary>
    public void ClearSelection()
    {
        _currentlySelectedAnimal = null;
    }

    /// <summary>
    /// Removes an animal from the animals list. Called when an animal is destroyed.
    /// </summary>
    public void RemoveAnimal(Animal animal)
    {
        if (animal != null && _animals != null)
        {
            _animals.Remove(animal);
        }
        
        // If the removed animal was the cached player, clear the cache
        if (animal == _cachedPlayer)
        {
            _cachedPlayer = null;
            // Try to find a new player if one exists
            RefreshPlayerCache();
        }
        
        // Also remove from viewport list if present
        if (animal != null && _animalsInViewport != null)
        {
            _animalsInViewport.Remove(animal);
        }
        
        // Also remove from predators targeting player list if it's a predator
        if (animal is PredatorAnimal predator)
        {
            RegisterPredatorTargetingPlayer(predator, false);
        }
    }
    
    /// <summary>
    /// Registers or unregisters a predator as targeting the controllable player.
    /// Updates music based on whether any predators are targeting.
    /// </summary>
    /// <param name="predator">The predator to register/unregister</param>
    /// <param name="isTargeting">True to add to list, false to remove</param>
    public void RegisterPredatorTargetingPlayer(PredatorAnimal predator, bool isTargeting)
    {
        if (predator == null || _predatorsTargetingPlayer == null)
        {
            return;
        }
        
        if (isTargeting)
        {
            // Add if not already present
            if (!_predatorsTargetingPlayer.Contains(predator))
            {
                _predatorsTargetingPlayer.Add(predator);
                UpdateMusicBasedOnTargetingList();
            }
        }
        else
        {
            // Remove if present
            if (_predatorsTargetingPlayer.Remove(predator))
            {
                UpdateMusicBasedOnTargetingList();
            }
        }
    }
    
    /// <summary>
    /// Updates the music based on whether any predators are targeting the controllable player.
    /// Plays Spring music if no predators are targeting, Danger music otherwise.
    /// </summary>
    private void UpdateMusicBasedOnTargetingList()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }
        
        // Clean up any null references (destroyed predators)
        _predatorsTargetingPlayer.RemoveAll(p => p == null);
        
        if (_predatorsTargetingPlayer.Count == 0)
        {
            AudioManager.Instance.PlayMusic(AudioManager.MusicType.Spring);
        }
        else
        {
            AudioManager.Instance.PlayMusic(AudioManager.MusicType.Danger);
        }
    }

    /// <summary>
    /// Updates the list of animals visible in the camera viewport.
    /// Only includes animals that are:
    /// 1. In the camera's viewport (world space)
    /// 2. Not obscured by fog of war (their tile is revealed)
    /// </summary>
    private void UpdateAnimalsInViewport()
    {
        // Clear the current list
        _animalsInViewport.Clear();
        
        // Ensure we have FogOfWarManager
        if (FogOfWarManager.Instance == null)
        {
            return;
        }

        // Get all animals in the scene
        List<Animal> allAnimals = GetAllAnimals();

        // Check each animal
        for (int i = 0; i < allAnimals.Count; i++)
        {
            Animal animal = allAnimals[i];
            if (animal == null)
            {
                continue;
            }

            // Check if animal is in the camera viewport (world space)
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(animal.transform.position);
            bool isInViewport = viewportPos.x >= 0f && viewportPos.x <= 1f &&
                               viewportPos.y >= 0f && viewportPos.y <= 1f &&
                               viewportPos.z > 0f; // In front of camera

            if (!isInViewport)
            {
                continue; // Animal is not in viewport
            }

            // Check if the animal's tile is revealed (not obscured by fog of war)
            Vector2Int animalGridPos = animal.GridPosition;
            bool isTileRevealed = FogOfWarManager.Instance.IsTileRevealed(animalGridPos);

            if (!isTileRevealed)
            {
                continue; // Animal is obscured by fog of war
            }

            // Animal passes all checks - add to the list
            _animalsInViewport.Add(animal);
        }
    }
}

