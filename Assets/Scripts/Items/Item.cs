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
    
    [SerializeField] [Tooltip("Grid position of this item")]
    private Vector2Int _gridPosition;
    
    [SerializeField] [Tooltip("Sprite used when displaying this item in UI (e.g., inventory).")]
    private Sprite _inventorySprite;
    
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
}

