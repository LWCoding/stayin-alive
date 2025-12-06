using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enumeration of item types in the game.
/// Used instead of strings for type safety and better performance.
/// </summary>
public enum ItemType
{
    Grass,
    GrassSeeds,
    Worm,
    Sticks
}

/// <summary>
/// Base class for items that can be placed in the world as prefabs.
/// Implements IItem interface and provides basic functionality.
/// </summary>
public abstract class Item : MonoBehaviour, IItem
{
    [Header("Item Settings")]
    [SerializeField] [Tooltip("Type of this item.")]
    private ItemType _itemType;
    
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
    /// The type of this item.
    /// </summary>
    public ItemType ItemType => _itemType;
    
    /// <summary>
    /// The name identifier for this item (for backward compatibility with IItem interface).
    /// Gets the item name from ItemData, falling back to enum string if ItemData not found.
    /// </summary>
    public string ItemName
    {
        get
        {
            if (ItemManager.Instance != null)
            {
                return ItemManager.Instance.GetItemName(_itemType);
            }
            
            // Fallback to enum string if ItemManager not available
            return _itemType.ToString();
        }
    }
    
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
        Initialize(gridPosition, _itemType);
    }
    
    /// <summary>
    /// Initializes the item at the specified grid position with a specific item type.
    /// </summary>
    public virtual void Initialize(Vector2Int gridPosition, ItemType itemType)
    {
        _gridPosition = gridPosition;
        _itemType = itemType;
        
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
        
        // Initial check for player position
        _isPlayerOnTile = IsPlayerOnSameTile();
        if (_interactionIndicator != null)
        {
            _interactionIndicator.SetActive(_isPlayerOnTile);
        }
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
    /// Called when a turn advances (after player moves and all animals have moved).
    /// Checks if the player is on the same tile and updates the interaction indicator.
    /// Player movement happens before turn advancement, so this is safe from race conditions.
    /// </summary>
    private void OnTurnAdvanced(int turnCount)
    {
        // Check if player is on the same tile after turn advancement (player moves first)
        _isPlayerOnTile = IsPlayerOnSameTile();
        
        // Show/hide interaction indicator
        if (_interactionIndicator != null)
        {
            _interactionIndicator.SetActive(_isPlayerOnTile);
        }
    }

    private void Update() 
    {
        // Only check input - player position is updated on turn advancement to prevent race conditions
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
            // Pass the Item object itself - AddItem will create a persistent copy
            bool added = InventoryManager.Instance.AddItem(this);
            
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
                Debug.Log($"Cannot pick up '{ItemName}' - inventory is full!");
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

