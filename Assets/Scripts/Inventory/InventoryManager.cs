using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's inventory system using UI slots.
/// Handles item pickup, storage, and display.
/// </summary>
public class InventoryManager : Singleton<InventoryManager>
{
    [Header("Inventory Settings")]
    [SerializeField] [Tooltip("Maximum number of inventory slots")]
    private int _maxInventorySize;
    
    [Header("UI References")]
    [SerializeField] [Tooltip("Transform container where inventory slots will be instantiated")]
    private Transform _inventoryContainer;
    
    [SerializeField] [Tooltip("Prefab for inventory slot UI")]
    private GameObject _inventorySlotPrefab;
    
    // List of all inventory slots
    private List<InventorySlot> _inventorySlots = new List<InventorySlot>();
    
    /// <summary>
    /// Current number of items in the inventory.
    /// </summary>
    public int CurrentItemCount
    {
        get
        {
            int count = 0;
            foreach (InventorySlot slot in _inventorySlots)
            {
                if (!slot.IsEmpty)
                {
                    count++;
                }
            }
            return count;
        }
    }
    
    /// <summary>
    /// Whether the inventory is full.
    /// </summary>
    public bool IsFull => CurrentItemCount >= _maxInventorySize;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Validate references
        if (_inventoryContainer == null)
        {
            Debug.LogError("InventoryManager: Inventory container is not assigned! Please assign a Transform in the Inspector.");
        }
        
        if (_inventorySlotPrefab == null)
        {
            Debug.LogError("InventoryManager: Inventory slot prefab is not assigned! Please assign the UIInventorySlot prefab in the Inspector.");
        }
        
        // Initialize inventory slots
        InitializeInventorySlots();
    }
    
    /// <summary>
    /// Initializes the inventory slots by instantiating prefabs.
    /// </summary>
    private void InitializeInventorySlots()
    {
        if (_inventoryContainer == null || _inventorySlotPrefab == null)
        {
            return;
        }
        
        // Clear existing slots
        ClearAllSlots();
        
        // Instantiate slots
        for (int i = 0; i < _maxInventorySize; i++)
        {
            GameObject slotObj = Instantiate(_inventorySlotPrefab, _inventoryContainer);
            slotObj.name = $"InventorySlot_{i}";
            
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null)
            {
                Debug.LogError($"InventoryManager: Inventory slot prefab does not have an InventorySlot component!");
                Destroy(slotObj);
                continue;
            }
            
            _inventorySlots.Add(slot);
        }
        
        Debug.Log($"InventoryManager: Initialized {_inventorySlots.Count} inventory slots.");
    }
    
    
    /// <summary>
    /// Attempts to add an item to the inventory. Returns true if successful, false if inventory is full.
    /// The corresponding sprite is resolved automatically using the ItemTilemapManager database.
    /// </summary>
    public bool AddItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("InventoryManager: Cannot add item with null or empty name.");
            return false;
        }
        
        if (IsFull)
        {
            Debug.Log($"InventoryManager: Cannot add item '{itemName}' - inventory is full ({CurrentItemCount}/{_maxInventorySize}).");
            return false;
        }

        Sprite itemSprite = null;
        if (ItemTilemapManager.Instance != null)
        {
            itemSprite = ItemTilemapManager.Instance.GetItemSprite(itemName);
        }
        else
        {
            Debug.LogWarning("InventoryManager: ItemTilemapManager instance not found. Item will be added without a sprite.");
        }
        
        // Find the first empty slot
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (slot.IsEmpty)
            {
                if (slot.SetItem(itemName, itemSprite))
                {
                    Debug.Log($"InventoryManager: Added item '{itemName}' to inventory. ({CurrentItemCount}/{_maxInventorySize})");
                    return true;
                }
            }
        }
        
        Debug.LogWarning($"InventoryManager: Failed to add item '{itemName}' - no empty slots found (this should not happen).");
        return false;
    }
    
    /// <summary>
    /// Clears all items from the inventory slots.
    /// Returns the number of items that were cleared.
    /// </summary>
    public int ClearAllItems()
    {
        int clearedCount = 0;
        
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                slot.ClearSlot();
                clearedCount++;
            }
        }
        
        Debug.Log($"InventoryManager: Cleared {clearedCount} items from inventory.");
        return clearedCount;
    }
    
    /// <summary>
    /// Gets the count of a specific item in the inventory.
    /// </summary>
    public int GetItemCount(string itemName)
    {
        int count = 0;
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (!slot.IsEmpty && slot.ItemName == itemName)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Removes all instances of a specific item from the inventory.
    /// Returns the number of items removed.
    /// </summary>
    public int RemoveAllItems(string itemName)
    {
        int removedCount = 0;
        
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (!slot.IsEmpty && slot.ItemName == itemName)
            {
                slot.ClearSlot();
                removedCount++;
            }
        }
        
        return removedCount;
    }
    
    /// <summary>
    /// Clears all inventory slots (destroys the GameObjects).
    /// </summary>
    private void ClearAllSlots()
    {
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        _inventorySlots.Clear();
    }
}

