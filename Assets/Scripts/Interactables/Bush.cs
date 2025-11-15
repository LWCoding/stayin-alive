using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A bush interactable that provides hiding for controllable animals.
/// When a controllable animal is on the same tile as a bush, they are hidden and time progresses.
/// </summary>
public class Bush : MonoBehaviour, IHideable
{
    [Header("Bush Settings")]
    private Vector2Int _gridPosition;
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _hidingSprite;
    [SerializeField] private GameObject _hidingIndicator;
    
    // Track which animals are currently in this bush
    private HashSet<Animal> _animalsInBush = new HashSet<Animal>();
    
    // Coroutine for passive time progression
    private Coroutine _timeProgressionCoroutine;
    
    public Vector2Int GridPosition => _gridPosition;
    
    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Hide the hiding indicator by default
        if (_hidingIndicator != null)
        {
            _hidingIndicator.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (InteractableManager.Instance != null)
        {
            InteractableManager.Instance.RemoveBush(this);
        }
    }
    
    /// <summary>
    /// Initializes the bush at the specified grid position.
    /// </summary>
    public void Initialize(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
        
        // Position the bush in world space
        if (EnvironmentManager.Instance != null)
        {
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
            transform.position = worldPos;
        }
        else
        {
            transform.position = new Vector3(_gridPosition.x, _gridPosition.y, 0);
        }
        
        UpdateBushVisualState();
    }
    
    /// <summary>
    /// Checks if an animal is currently in this bush.
    /// </summary>
    public bool IsAnimalInBush(Animal animal)
    {
        return _animalsInBush.Contains(animal);
    }

    /// <summary>
    /// IHideable implementation: Checks if an animal is in this hideable location.
    /// </summary>
    public bool IsAnimalInHideable(Animal animal)
    {
        return IsAnimalInBush(animal);
    }
    
    /// <summary>
    /// Called when an animal enters this bush.
    /// </summary>
    public void OnAnimalEnter(Animal animal)
    {
        if (animal != null && animal.IsControllable)
        {
            _animalsInBush.Add(animal);
            Debug.Log($"Animal '{animal.name}' entered bush at ({_gridPosition.x}, {_gridPosition.y})");
            
            animal.SetCurrentHideable(this);
            animal.SetVisualVisibility(false);
            
            // Start passive time progression if not already running
            if (_timeProgressionCoroutine == null)
            {
                _timeProgressionCoroutine = StartCoroutine(PassiveTimeProgression());
            }
            
            UpdateBushVisualState();
        }
    }
    
    /// <summary>
    /// Called when an animal leaves this bush.
    /// </summary>
    public void OnAnimalLeave(Animal animal)
    {
        if (_animalsInBush.Remove(animal))
        {
            Debug.Log($"Animal '{animal.name}' left bush at ({_gridPosition.x}, {_gridPosition.y})");
            
            // Clear the hideable reference if this animal is leaving this bush
            if (animal != null && ReferenceEquals(animal.CurrentHideable, this))
            {
                animal.SetCurrentHideable(null);
            }
            
            // Only make animal visible if they're not entering another hideable location
            if (animal != null && animal.CurrentHideable == null)
            {
                animal.SetVisualVisibility(true);
            }
            
            // Stop passive time progression if no animals are left in the bush
            if (_animalsInBush.Count == 0 && _timeProgressionCoroutine != null)
            {
                StopCoroutine(_timeProgressionCoroutine);
                _timeProgressionCoroutine = null;
            }
            
            UpdateBushVisualState();
        }
    }
    
    /// <summary>
    /// Coroutine that passively progresses time while animals are in the bush.
    /// Calls NextTurn() on TimeManager at intervals determined by Globals.DenTimeProgressionDelay.
    /// </summary>
    private IEnumerator PassiveTimeProgression()
    {
        while (_animalsInBush.Count > 0)
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
    /// Checks if a controllable animal is in a bush at the specified position.
    /// </summary>
    public static bool IsControllableAnimalInBush(Animal animal)
    {
        if (animal == null || !animal.IsControllable)
        {
            return false;
        }
        
        // Use the animal's CurrentHideable reference for efficiency
        return animal.CurrentHideable is Bush;
    }
    
    private void UpdateBushVisualState()
    {
        bool hasAnimals = _animalsInBush.Count > 0;
        
        // Update sprite renderer
        if (_spriteRenderer != null)
        {
            Sprite targetSprite = hasAnimals ? _hidingSprite : _normalSprite;
            
            if (targetSprite != null)
            {
                _spriteRenderer.sprite = targetSprite;
            }
            
            _spriteRenderer.enabled = targetSprite != null || _spriteRenderer.sprite != null;
        }
        
        // Update hiding indicator visibility
        if (_hidingIndicator != null)
        {
            _hidingIndicator.SetActive(hasAnimals);
        }
    }
}

