using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A den interactable that provides safety for controllable animals.
/// When a controllable animal is on the same tile as a den, predators cannot target them.
/// </summary>
public class Den : MonoBehaviour
{
    [Header("Den Settings")]
    private Vector2Int _gridPosition;
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite _unoccupiedSprite;
    [SerializeField] private Sprite _occupiedSprite;
    [SerializeField] private GameObject _playerInDenObject;
    
    // Track which animals are currently in this den (can contain duplicates)
    private List<Animal> _animalsInDen = new List<Animal>();
    
    // Coroutine for passive time progression
    private Coroutine _timeProgressionCoroutine;
    
    public Vector2Int GridPosition => _gridPosition;
    
    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Hide the player in den object by default
        if (_playerInDenObject != null)
        {
            _playerInDenObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Initializes the den at the specified grid position.
    /// </summary>
    public void Initialize(Vector2Int gridPosition)
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
    /// Checks if an animal is currently in this den.
    /// </summary>
    public bool IsAnimalInDen(Animal animal)
    {
        return _animalsInDen.Contains(animal);
    }

    public int NumberAnimalsInDen() {
      return _animalsInDen.Count;
    }
    
    /// <summary>
    /// Called when an animal enters this den.
    /// </summary>
    public void OnAnimalEnter(Animal animal)
    {
        if (animal != null && animal.IsControllable)
        {
          
            _animalsInDen.Add(animal);
            Debug.Log($"Animal '{animal.name}' entered den at ({_gridPosition.x}, {_gridPosition.y})");
            
            animal.SetVisualVisibility(false);
            animal.SetGridPosition(_gridPosition);
            
            // Handle food delivery: check for food items in inventory
            ProcessFoodDelivery(animal);
            
            // Start passive time progression if not already running
            if (_timeProgressionCoroutine == null)
            {
                _timeProgressionCoroutine = StartCoroutine(PassiveTimeProgression());
            }
            
            UpdateDenVisualState();
        }
    }
    
    /// <summary>
    /// Processes food delivery when an animal enters the den.
    /// Empties all inventory slots and adds +1 point per item to PointsManager.
    /// </summary>
    private void ProcessFoodDelivery(Animal animal)
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
                // Clear all items from inventory
                int clearedCount = InventoryManager.Instance.ClearAllItems();
                
                // Add points to PointsManager (+1 point per item)
                if (PointsManager.Instance != null)
                {
                    PointsManager.Instance.AddPoints(clearedCount);
                }
                
                Debug.Log($"Animal '{animal.name}' deposited {clearedCount} items at den. Added {clearedCount} points.");
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
        if (_animalsInDen.Remove(animal))
        {
            Debug.Log($"Animal '{animal.name}' left den at ({_gridPosition.x}, {_gridPosition.y})");
            
            animal.SetVisualVisibility(true);
            
            // Stop passive time progression if no animals are left in the den
            if (_animalsInDen.Count == 0 && _timeProgressionCoroutine != null)
            {
                StopCoroutine(_timeProgressionCoroutine);
                _timeProgressionCoroutine = null;
            }
            
            UpdateDenVisualState();
        }
    }
    
    /// <summary>
    /// Coroutine that passively progresses time while animals are in the den.
    /// Calls NextTurn() on TimeManager at intervals determined by Globals.DenTimeProgressionDelay.
    /// </summary>
    private IEnumerator PassiveTimeProgression()
    {
        while (_animalsInDen.Count > 0)
        {
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
        
        if (InteractableManager.Instance == null)
        {
            return false;
        }
        
        Den den = InteractableManager.Instance.GetDenAtPosition(animal.GridPosition);
        return den != null && den.IsAnimalInDen(animal);
    }
    
    private void UpdateDenVisualState()
    {
        bool hasAnimals = _animalsInDen.Count > 0;
        
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
        
        // Update player in den object visibility
        if (_playerInDenObject != null)
        {
            _playerInDenObject.SetActive(hasAnimals);
        }
    }
}

