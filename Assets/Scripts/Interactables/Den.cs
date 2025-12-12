using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A den interactable that provides safety for controllable animals.
/// When a controllable animal is on the same tile as a den, predators cannot target them.
/// </summary>
public class Den : Interactable, IHideable
{
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite _unoccupiedSprite;
    [SerializeField] private Sprite _occupiedSprite;
    [SerializeField] private GameObject _occupancyIndicator;
    [SerializeField] private GameObject _eToManageObject;
    
    [SerializeField]
    private Camera denCamera;
    
    private static RenderTexture renderTexture;
    
    public RenderTexture DenRenderTexture => renderTexture;
    
    // Track which animals are currently in this den (can contain duplicates)
    private List<Animal> _animalsInDen = new List<Animal>();
    
    // Coroutine for passive time progression
    private Coroutine _timeProgressionCoroutine;

    private List<Animal> denWorkers = new List<Animal>();

    public int DenId => GridPosition.x * 10000 + GridPosition.y;

    public void GiveRenderTexture() {
      denCamera.enabled = true;
    }

    public void TakeRenderTexture() {
      denCamera.enabled = false;
    }

    public override string GetKnowledgeTitle() {
      return "Den";
    }
    
    // Returns true if added properly, false if the den is full
    public bool AddWorker(Animal animal) {
      // Only add if not at capacity
      if (denWorkers.Count < Globals.MaxWorkersPerDen) {
        denWorkers.Add(animal);
        return true;
      }

      Debug.LogWarning($"Den at ({GridPosition.x}, {GridPosition.y}) is at maximum capacity ({Globals.MaxWorkersPerDen} workers).");

      return false;
    }

    public void RemoveWorker(Animal animal) {
      denWorkers.Remove(animal);
    }

    public int WorkerCount() {
      return denWorkers.Count;
    }
    
    public bool IsFull() {
      return denWorkers.Count >= Globals.MaxWorkersPerDen;
    }
    
    public DenSystemManager.DenInformation GetDenInfo() {
      return DenSystemManager.ConstructDenInformation(this);
    }
    
    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Hide the occupancy indicator and E to manage object by default
        if (_occupancyIndicator != null)
        {
            _occupancyIndicator.SetActive(false);
        }
        
        if (_eToManageObject != null)
        {
            _eToManageObject.SetActive(false);
        }

        // Set up the static, universal render texture for den specific mini maps
        if (renderTexture == null) {
          renderTexture = new RenderTexture(720, 720, 16, RenderTextureFormat.ARGB32);
          renderTexture.Create();
        }
        denCamera.targetTexture = renderTexture;
    }
    
    /// <summary>
    /// Initializes the den at the specified grid position.
    /// </summary>
    public override void Initialize(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
        
        // Position the den in world space
        if (EnvironmentManager.Instance != null)
        {
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
            transform.position = worldPos;
        }
        else
        {
            transform.position = new Vector3(_gridPosition.x, _gridPosition.y, 0);
        }
        
        UpdateDenVisualState();
    }
    
    /// <summary>
    /// Removes null or destroyed animal references from the tracking list.
    /// Called to ensure the den's occupancy state is accurate.
    /// </summary>
    private void CleanupDestroyedAnimals()
    {
        for (int i = _animalsInDen.Count - 1; i >= 0; i--)
        {
            Animal animal = _animalsInDen[i];
            
            // Check if the animal reference is null (Unity sets this when GameObject is destroyed)
            if (animal == null)
            {
                _animalsInDen.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Checks if an animal is currently in this den.
    /// </summary>
    public bool IsAnimalInDen(Animal animal)
    {
        return _animalsInDen.Contains(animal);
    }

    /// <summary>
    /// IHideable implementation: Checks if an animal is in this hideable location.
    /// </summary>
    public bool IsAnimalInHideable(Animal animal)
    {
        return IsAnimalInDen(animal);
    }
    
    /// <summary>
    /// Called when an animal enters this den.
    /// </summary>
    public void OnAnimalEnter(Animal animal)
    {
        if (animal == null) {
            return;
        }
        
        // Clean up any destroyed animals before adding new one
        CleanupDestroyedAnimals();
        
        // Allow entry if animal is controllable OR if this den is the animal's home
        bool canEnter = animal.IsControllable || ReferenceEquals(animal.HomeHideable, this);
        
        if (canEnter)
        {
          
            _animalsInDen.Add(animal);
            Debug.Log($"Animal '{animal.name}' entered den at ({_gridPosition.x}, {_gridPosition.y})");
            
            animal.SetCurrentHideable(this);
            animal.SetVisualVisibility(false);
            // animal.SetGridPosition(_gridPosition);
            
            // Handle food delivery: check for food items in inventory
            // ProcessFoodDelivery(animal); NOT ANYMORE HEHE!

            // Ensure inventory selection resets when the player enters the den
            if (animal.IsControllable && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.DeselectSlot();
            }
            
            // Update passive time progression state (only runs while player is inside)
            UpdatePassiveTimeProgressionState();

            UpdateDenVisualState();
        }
    }
    
    /// <summary>
    /// Processes food delivery when an animal enters the den.
    /// Empties all inventory slots and adds the items removed to the Den System Inventory
    /// </summary>
    public void ProcessFoodDelivery(Animal animal)
    {
        if (animal == null || !animal.IsControllable)
        {
            return;
        }
        
        // Use InventoryManager to get and clear all items
        if (InventoryManager.Instance != null)
        {
            // Count items before clearing
            int itemCount = InventoryManager.Instance.CurrentItemCount;
            
            if (itemCount > 0)
            {
                // Transfer all items from inventory to den
                List<Item> inventoryItems = InventoryManager.Instance.TransferAllItems();
                int inventoryItemsCount = inventoryItems.Count;
                
                // Add stored food to the den (+1 per item)
                if (GlobalInventoryManager.Instance != null)
                {
                  foreach (Item inventoryItem in inventoryItems) {
                    GlobalInventoryManager.Instance.AddItemIdToDen(inventoryItem.ItemId);
                    Destroy(inventoryItem.gameObject);
                  }
                }
                
                Debug.Log($"Animal '{animal.name}' deposited {inventoryItemsCount} items at den. Added {inventoryItemsCount} points.");
            }
        }
        else
        {
            Debug.LogWarning("Den: InventoryManager instance not found! Cannot process food delivery.");
        }
    }
    
    /// <summary>
    /// Called when an animal leaves this den.
    /// </summary>
    public void OnAnimalLeave(Animal animal)
    {
        // Clean up any destroyed animals before processing this leave
        CleanupDestroyedAnimals();
        
        if (_animalsInDen.Remove(animal))
        {
            Debug.Log($"Animal '{animal.name}' left den at ({_gridPosition.x}, {_gridPosition.y})");
            
            // Clear the hideable reference if this animal is leaving this den
            if (animal != null && ReferenceEquals(animal.CurrentHideable, this))
            {
                animal.SetCurrentHideable(null);
            }
            
            // Only make animal visible if they're not entering another hideable location
            if (animal != null && animal.CurrentHideable == null)
            {
                animal.SetVisualVisibility(true);
            }
            
            // Update passive time progression state (only runs while player is inside)
            UpdatePassiveTimeProgressionState();
            
            UpdateDenVisualState();
        }
    }
    
    /// <summary>
    /// Coroutine that passively progresses time while animals are in the den.
    /// Calls NextTurn() on TimeManager at intervals determined by Globals.DenTimeProgressionDelay.
    /// </summary>
    private IEnumerator PassiveTimeProgression()
    {
        while (true)
        {
            // Clean up destroyed animals before checking occupancy
            CleanupDestroyedAnimals();
            
            if (!HasControllableAnimalInside())
            {
                break;
            }
            
            yield return new WaitForSeconds(Globals.DenTimeProgressionDelay);
            
            // Only progress time if TimeManager exists and is not paused
            if (TimeManager.Instance != null && !TimeManager.Instance.IsPaused)
            {
                TimeManager.Instance.NextTurn();
            }
        }
        
        _timeProgressionCoroutine = null;
    }
    
    /// <summary>
    /// Checks if there is a den at the specified grid position.
    /// </summary>
    public static bool IsDenAtPosition(Vector2Int gridPosition)
    {
        if (InteractableManager.Instance == null)
        {
            return false;
        }
        
        return InteractableManager.Instance.GetDenAtPosition(gridPosition) != null;
    }
    
    /// <summary>
    /// Checks if a controllable animal is in a den at the specified position.
    /// </summary>
    public static bool IsControllableAnimalInDen(Animal animal)
    {
        if (animal == null || !animal.IsControllable)
        {
            return false;
        }
        
        // Use the animal's CurrentHideable reference for efficiency
        return animal.CurrentHideable is Den;
    }
    
    /// <summary>
    /// Updates the visual state of the den based on occupancy.
    /// Public so it can be called from TutorialManager when unlocking den management.
    /// </summary>
    public void UpdateDenVisualState()
    {
        // Clean up any destroyed/null animal references first
        CleanupDestroyedAnimals();
        
        bool hasAnimals = _animalsInDen.Count > 0;
        bool hasPlayer = HasControllableAnimalInside();
        
        // Update sprite renderer
        if (_spriteRenderer != null)
        {
            Sprite targetSprite = hasAnimals ? _occupiedSprite : _unoccupiedSprite;
            
            if (targetSprite != null)
            {
                _spriteRenderer.sprite = targetSprite;
            }
            
            _spriteRenderer.enabled = targetSprite != null || _spriteRenderer.sprite != null;
        }
        
        // Update occupancy indicator visibility - only show when player is in den
        if (_occupancyIndicator != null)
        {
            _occupancyIndicator.SetActive(hasPlayer);
        }
        
        // Update E to manage object visibility - only show when player is in den
        // AND if in tutorial mode, only show if den management is unlocked
        if (_eToManageObject != null)
        {
            bool shouldShow = hasPlayer;
            
            // Check if we're in tutorial mode and if den management is unlocked
            if (TutorialManager.Instance != null)
            {
                shouldShow = shouldShow && TutorialManager.Instance.IsDenManagementUnlocked;
            }
            
            _eToManageObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// Checks whether a controllable animal (player) is currently inside the den.
    /// </summary>
    private bool HasControllableAnimalInside()
    {
        for (int i = 0; i < _animalsInDen.Count; i++)
        {
            Animal animal = _animalsInDen[i];
            if (animal != null && animal.IsControllable)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Ensures the passive time progression coroutine matches whether the player is inside the den.
    /// </summary>
    private void UpdatePassiveTimeProgressionState()
    {
        bool hasPlayer = HasControllableAnimalInside();
        if (hasPlayer)
        {
            if (_timeProgressionCoroutine == null)
            {
                _timeProgressionCoroutine = StartCoroutine(PassiveTimeProgression());
            }
        }
        else if (_timeProgressionCoroutine != null)
        {
            StopCoroutine(_timeProgressionCoroutine);
            _timeProgressionCoroutine = null;
        }
    }
}

