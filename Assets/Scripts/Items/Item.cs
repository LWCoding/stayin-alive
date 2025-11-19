using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for items that can be placed in the world as prefabs.
/// Implements IItem interface and provides basic functionality.
/// </summary>
public abstract class Item : MonoBehaviour, IItem
{
    [Header("Item Settings")]
    [SerializeField] [Tooltip("Unique name identifier for this item.")]
    private string _itemName;
    
    [HideInInspector] [Tooltip("Grid position of this item")]
    private Vector2Int _gridPosition;
    
    [SerializeField] [Tooltip("Sprite used when displaying this item in UI (e.g., inventory).")]
    private Sprite _inventorySprite;
    
    [SerializeField] [TextArea(3, 5)] [Tooltip("Description of how to use this item. Displayed in the inventory when the item is selected.")]
    private string _usageDescription = "";
    
    [Header("Interaction")]
    [SerializeField] [Tooltip("GameObject that shows/hides when the player is on the same tile (e.g., interaction indicator)")]
    private GameObject _interactionIndicator;
    
    /// <summary>
    /// The grid position of this item.
    /// </summary>
    public Vector2Int GridPosition => _gridPosition;
    
    /// <summary>
    /// The name identifier for this item.
    /// </summary>
    public string ItemName => _itemName;
    
    /// <summary>
    /// The sprite used for displaying this item in the inventory UI.
    /// </summary>
    public Sprite InventorySprite => _inventorySprite;
    
    /// <summary>
    /// The usage description for this item.
    /// </summary>
    public string UsageDescription => _usageDescription;
    
    bool _isPlayerOnTile = false;

    /// <summary>
    /// Initializes the item at the specified grid position.
    /// </summary>
    public virtual void Initialize(Vector2Int gridPosition)
    {
        Initialize(gridPosition, _itemName);
    }
    
    /// <summary>
    /// Initializes the item at the specified grid position with a specific item name.
    /// </summary>
    public virtual void Initialize(Vector2Int gridPosition, string itemName)
    {
        _gridPosition = gridPosition;
        
        // Set item name if provided (ensures it matches the lookup key)
        if (!string.IsNullOrEmpty(itemName))
        {
            _itemName = itemName;
        }
        
        // Set world position based on grid position
        if (EnvironmentManager.Instance != null)
        {
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(gridPosition);
            transform.position = worldPos;
        }
        
        // Hide interaction indicator by default
        if (_interactionIndicator != null)
        {
            _interactionIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        SubscribeToTimeManager();
    }
    
    private void SubscribeToTimeManager()
    {
        TimeManager.Instance.OnTurnAdvanced += OnTurnAdvanced;
    }
    
    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= OnTurnAdvanced;
        }
    }
    
    /// <summary>
    /// Called when a turn advances (after all animals have moved).
    /// Checks if the player is on the same tile, updates the interaction indicator,
    /// and handles E key press for item pickup.
    /// </summary>
    private void OnTurnAdvanced(int turnCount)
    {
        // Check if player is on the same tile
        _isPlayerOnTile = IsPlayerOnSameTile();
        
        // Show/hide interaction indicator
        if (_interactionIndicator != null)
        {
            _interactionIndicator.SetActive(_isPlayerOnTile);
        }
    }

    private void Update() 
    {
        // If player is on the same tile and E key is pressed, attempt pickup
        if (_isPlayerOnTile && Input.GetKeyDown(KeyCode.E))
        {
            AttemptPickup();
        }
    }
    
    /// <summary>
    /// Called when the item is picked up. Override to add custom pickup behavior.
    /// </summary>
    public virtual void OnPickup(ControllableAnimal picker)
    {
        // Default: do nothing on pickup
    }
    
    /// <summary>
    /// Called when the item is used. Override to add custom use behavior.
    /// </summary>
    public abstract bool OnUse(ControllableAnimal user);
    
    /// <summary>
    /// Called when the item should be destroyed (after being picked up).
    /// </summary>
    public virtual void DestroyItem()
    {
        // Notify ItemManager that this item is being destroyed
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.RemoveItem(this);
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Checks if the player (ControllableAnimal) is on the same tile as this item.
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
    /// Attempts to pick up this item when the player presses E.
    /// </summary>
    private void AttemptPickup()
    {
        // Get the player
        ControllableAnimal player = GetPlayer();
        if (player == null)
        {
            Debug.LogWarning("Item: Cannot pick up item - no controllable animal found.");
            return;
        }
        
        // Try to add item to inventory
        if (InventoryManager.Instance != null)
        {
            bool added = InventoryManager.Instance.AddItem(_itemName);
            
            if (added)
            {
                // Call OnPickup on the item
                OnPickup(player);
                
                // Picking up an item costs one turn
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.NextTurn();
                }
                
                // Remove item from world (destroy the GameObject)
                DestroyItem();
            }
            else
            {
                // Item was not added (inventory full), so it remains
                Debug.Log($"Cannot pick up '{_itemName}' - inventory is full!");
            }
        }
        else
        {
            Debug.LogWarning("Item: InventoryManager instance not found! Cannot add item to inventory.");
        }
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
}

