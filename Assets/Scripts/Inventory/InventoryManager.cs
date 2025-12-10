using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the player's inventory system using UI slots.
/// Handles item pickup, storage, and display.
/// </summary>
public class InventoryManager : Singleton<InventoryManager>
{
    /// <summary>
    /// Event fired when an item is successfully added to inventory.
    /// </summary>
    public event Action<string> OnItemAdded;
    
    [Header("UI References")]
    [SerializeField] [Tooltip("Transform container where inventory slots will be instantiated")]
    private Transform _inventoryContainer;
    
    [SerializeField] [Tooltip("Prefab for inventory slot UI")]
    private GameObject _inventorySlotPrefab;
    
    [Header("Usage Description")]
    [SerializeField] [Tooltip("Text component that displays the usage description of the selected item")]
    private TextMeshProUGUI _usageDescriptionText;
    
    // List of all inventory slots
    private List<InventorySlot> _inventorySlots = new List<InventorySlot>();
    public List<InventorySlot> GetInventorySlots() => new List<InventorySlot>(_inventorySlots);

    // Currently selected slot index (-1 if none selected)
    private int _selectedSlotIndex = -1;
    private int _activeUseSlotIndex = -1;
    
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
        ResetInventorySlots();
        
        // Clear usage description text at start
        ClearUsageDescription();
    }
    
    private void Update()
    {
        // Don't process inventory input if the Den Admin Menu is open
        if (DenSystemManager.Instance != null && DenSystemManager.Instance.PanelOpen)
        {
            return;
        }
        
        // Handle keyboard input for slot selection and item usage (keys 1-9)
        for (int i = 0; i < 9; i++)
        {
            // Check if key 1-9 is pressed (KeyCode.Alpha1 through KeyCode.Alpha9)
            KeyCode keyCode = KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(keyCode))
            {
                // If this slot is already selected and has an item, use it
                if (_selectedSlotIndex == i && _selectedSlotIndex >= 0 && _selectedSlotIndex < _inventorySlots.Count)
                {
                    InventorySlot slot = _inventorySlots[_selectedSlotIndex];
                    if (!slot.IsEmpty)
                    {
                        UseItemInSlot(_selectedSlotIndex);
                        break;
                    }
                }
                
                // Otherwise, select the slot
                SelectSlot(i);
                break;
            }
        }
    }
    
    /// <summary>
    /// Initializes the inventory slots by instantiating prefabs.
    /// </summary>
    public void ResetInventorySlots()
    {
        if (_inventoryContainer == null || _inventorySlotPrefab == null)
        {
            return;
        }
        
        // Clear existing slots
        ClearAllSlots();
        
        // Instantiate slots
        for (int i = 0; i < Globals.MaxInventorySize; i++)
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
            
            // Set the slot number (1-9) for display
            // Only show numbers 1-9, even if maxInventorySize is larger
            if (i < 9)
            {
                slot.SetSlotNumber(i + 1);
            }
            
            _inventorySlots.Add(slot);
        }
        
        Debug.Log($"InventoryManager: Initialized {_inventorySlots.Count} inventory slots.");
    }
    
    /// <summary>
    /// Attempts to add an item to the inventory. Returns true if successful, false if inventory is full.
    /// Creates a persistent copy of the item for storage in the inventory.
    /// </summary>
    public bool AddItem(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("InventoryManager: Cannot add null item.");
            return false;
        }
        
        if (CurrentItemCount >= Globals.MaxInventorySize)
        {
            Debug.Log($"InventoryManager: Cannot add item '{item.ItemName}' - inventory is full ({CurrentItemCount}/{Globals.MaxInventorySize}).");
            
            // Trigger shake animation
            ShakeInventory();
            
            return false;
        }

        // Create a persistent copy of the item for the inventory
        // Items in inventory should be inactive GameObjects that don't exist in the world
        // Use ItemManager to create a storage item from the item's name
        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager: ItemManager instance not found. Cannot create inventory item.");
            return false;
        }
        
        Item inventoryItem = ItemManager.Instance.CreateItemForStorage(item.ItemType);
        if (inventoryItem == null)
        {
            Debug.LogWarning($"InventoryManager: Failed to create inventory item '{item.ItemName}'.");
            return false;
        }
        
        // Find the first empty slot
        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventorySlot slot = _inventorySlots[i];
            if (slot.IsEmpty)
            {
                if (slot.SetItem(inventoryItem))
                {
                    Debug.Log($"InventoryManager: Added item '{item.ItemName}' to inventory. ({CurrentItemCount}/{Globals.MaxInventorySize})");
                    
                    // Fire event for item added
                    OnItemAdded?.Invoke(item.ItemName);
                    
                    // If the item was added to the currently selected slot, update the usage description
                    if (i == _selectedSlotIndex)
                    {
                        UpdateUsageDescription();
                        return true;
                    }
                    
                    // If the player currently has this item selected elsewhere (e.g., holding sticks),
                    // refresh the usage description so any count-dependent messaging stays accurate.
                    if (_selectedSlotIndex >= 0 &&
                        _selectedSlotIndex < _inventorySlots.Count)
                    {
                        InventorySlot selectedSlot = _inventorySlots[_selectedSlotIndex];
                        Item selectedItem = selectedSlot.GetItem();
                        if (selectedItem != null && selectedItem.ItemName == item.ItemName)
                        {
                            UpdateUsageDescription();
                        }
                    }
                    
                    return true;
                }
            }
        }
        
        Debug.LogWarning($"InventoryManager: Failed to add item '{item.ItemName}' - no empty slots found (this should not happen).");
        return false;
    }
    
    /// <summary>
    /// Attempts to add an item to the inventory by type. Creates a new Item instance from the prefab.
    /// This is a convenience method for cases where only the item type is available.
    /// </summary>
    public bool AddItem(ItemId itemType)
    {
        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager: ItemManager instance not found. Cannot create item from type.");
            return false;
        }
        
        // Create a new Item instance from the prefab for storage
        Item item = ItemManager.Instance.CreateItemForStorage(itemType);
        if (item == null)
        {
            Debug.LogWarning($"InventoryManager: Cannot create item '{itemType}' from prefab.");
            return false;
        }
        
        // Add the item (this will create an inventory copy)
        bool added = AddItem(item);
        
        // Clean up the temporary item we created
        if (item != null && item.gameObject != null)
        {
            Destroy(item.gameObject);
        }
        
        return added;
    }
    
    
    /// <summary>
    /// Transfers all items from inventory slots to another system (e.g., den inventory).
    /// Extracts items without destroying them so they can be moved to the destination.
    /// Returns the list of extracted items.
    /// </summary>
    public List<Item> TransferAllItems()
    {
        List<Item> transferredItems = new List<Item>();
        
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (!slot.IsEmpty)
            {
                Item item = slot.ExtractItem();
                if (item != null)
                {
                    transferredItems.Add(item);
                }
            }
        }
        
        Debug.Log($"InventoryManager: Transferred {transferredItems.Count} items from inventory.");
        return transferredItems;
    }
    
    /// <summary>
    /// Gets the count of a specific item in the inventory.
    /// </summary>
    public int GetItemCount(string itemName)
    {
        int count = 0;
        foreach (InventorySlot slot in _inventorySlots)
        {
            Item item = slot.GetItem();
            if (item != null && item.ItemName == itemName)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Selects a slot by index (0-8). Deselects the previously selected slot.
    /// </summary>
    private void SelectSlot(int index)
    {
        // Don't allow selection if the Den Admin Menu is open
        if (DenSystemManager.Instance != null && DenSystemManager.Instance.PanelOpen)
        {
            return;
        }
        
        // Validate index
        if (index < 0 || index >= _inventorySlots.Count)
        {
            Debug.LogWarning($"InventoryManager: Cannot select slot {index} - index out of range (0-{_inventorySlots.Count - 1}).");
            return;
        }
        
        // Deselect previous slot
        if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _inventorySlots.Count)
        {
            _inventorySlots[_selectedSlotIndex].SetSelected(false);
        }
        
        // Select new slot
        _selectedSlotIndex = index;
        _inventorySlots[_selectedSlotIndex].SetSelected(true);
        
        // Update usage description text
        UpdateUsageDescription();
    }
    
    /// <summary>
    /// Deselects the currently selected slot.
    /// </summary>
    public void DeselectSlot()
    {
        if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _inventorySlots.Count)
        {
            _inventorySlots[_selectedSlotIndex].SetSelected(false);
            _selectedSlotIndex = -1;
        }
        
        // Clear usage description text
        ClearUsageDescription();
    }
    
    /// <summary>
    /// Uses the item in the specified slot. Calls the item's OnUse method if it implements IItem.
    /// </summary>
    private void UseItemInSlot(int slotIndex)
    {
        // Don't allow item usage if the Den Admin Menu is open
        if (DenSystemManager.Instance != null && DenSystemManager.Instance.PanelOpen)
        {
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= _inventorySlots.Count)
        {
            Debug.LogWarning($"InventoryManager: Cannot use item in slot {slotIndex} - index out of range.");
            return;
        }
        
        InventorySlot slot = _inventorySlots[slotIndex];
        if (slot.IsEmpty)
        {
            Debug.LogWarning($"InventoryManager: Cannot use item in slot {slotIndex} - slot is empty.");
            return;
        }
        
        Item item = slot.GetItem();
        if (item == null)
        {
            Debug.LogWarning($"InventoryManager: Cannot use item in slot {slotIndex} - item is null.");
            return;
        }
        
        // Get the ControllableAnimal (player) using cached reference
        ControllableAnimal player = null;
        if (AnimalManager.Instance != null)
        {
            // Use cached player reference instead of looping through all animals
            player = AnimalManager.Instance.GetPlayer();
        }
        
        if (player == null)
        {
            Debug.LogWarning("InventoryManager: Cannot use item - no controllable animal found.");
            return;
        }
        
        // Use the item directly
        bool itemUsed = false;
        
        _activeUseSlotIndex = slotIndex;
        try
        {
            itemUsed = item.OnUse(player);
        }
        finally
        {
            _activeUseSlotIndex = -1;
        }
        
        // If item was successfully used, remove it from inventory
        if (itemUsed)
        {
            slot.ClearSlot();
            // Update usage description since the slot is now empty
            UpdateUsageDescription();
        }
        else
        {
            Debug.LogWarning($"InventoryManager: Item '{item.ItemName}' could not be used.");
        }
    }
    
    /// <summary>
    /// Consumes a specific number of items matching the provided name, prioritizing the slot currently being used.
    /// This is intended for items (like sticks) whose usage consumes multiple copies at once.
    /// </summary>
    /// <param name="itemName">The name of the item to consume.</param>
    /// <param name="totalCount">The total number of items that should be removed.</param>
    /// <returns>True if the requested amount was consumed, false otherwise.</returns>
    public bool ConsumeItemsForActiveUse(string itemName, int totalCount)
    {
        if (totalCount <= 0)
        {
            return true;
        }
        
        if (string.IsNullOrEmpty(itemName))
        {
            return false;
        }
        
        int consumed = 0;
        
        // Consume the actively used slot first (if it contains the right item)
        if (_activeUseSlotIndex >= 0 && _activeUseSlotIndex < _inventorySlots.Count)
        {
            InventorySlot activeSlot = _inventorySlots[_activeUseSlotIndex];
            Item activeItem = activeSlot.GetItem();
            if (activeItem != null && activeItem.ItemName == itemName)
            {
                activeSlot.ClearSlot();
                consumed++;
            }
        }
        
        // Consume remaining required items from the rest of the inventory
        for (int i = 0; i < _inventorySlots.Count && consumed < totalCount; i++)
        {
            if (i == _activeUseSlotIndex)
            {
                continue;
            }
            
            InventorySlot slot = _inventorySlots[i];
            Item item = slot.GetItem();
            if (item != null && item.ItemName == itemName)
            {
                slot.ClearSlot();
                consumed++;
            }
        }
        
        return consumed >= totalCount;
    }
    
    /// <summary>
    /// Clears all inventory slots (destroys the GameObjects).
    /// </summary>
    private void ClearAllSlots()
    {
        // Deselect before clearing
        DeselectSlot();
        
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        _inventorySlots.Clear();
    }
    
    /// <summary>
    /// Shakes the inventory UI to indicate that the inventory is full.
    /// </summary>
    private void ShakeInventory()
    {
        // Play FullInventory sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.SFXType.FullInventory);
        }
        
        // Shake each slot independently
        foreach (InventorySlot slot in _inventorySlots)
        {
            if (slot != null)
            {
                slot.Shake();
            }
        }
    }
    
    /// <summary>
    /// Updates the usage description text based on the currently selected slot's item.
    /// </summary>
    private void UpdateUsageDescription()
    {
        if (_usageDescriptionText == null)
        {
            return;
        }
        
        // Get the selected slot
        InventorySlot selectedSlot = (_selectedSlotIndex >= 0 && _selectedSlotIndex < _inventorySlots.Count) ? _inventorySlots[_selectedSlotIndex] : null;
        if (selectedSlot == null || selectedSlot.IsEmpty)
        {
            ClearUsageDescription();
            return;
        }
        
        // Get the Item object directly from the slot
        Item item = selectedSlot.GetItem();
        if (item == null)
        {
            ClearUsageDescription();
            return;
        }
        
        string description = item.UsageDescription ?? "";
        string insufficientDescription = "";
        bool isSticksItem = false;
        
        // Check if it's a SticksItem to get the insufficient description
        if (item is SticksItem sticksItem)
        {
            isSticksItem = true;
            insufficientDescription = sticksItem.InsufficientSticksDescription ?? "";
        }
        
        if (isSticksItem)
        {
            Item selectedItem = selectedSlot.GetItem();
            string itemName = selectedItem != null ? selectedItem.ItemName : null;
            int availableSticks = GetItemCount(itemName);
            int missing = SticksItem.GetStickDeficit(availableSticks);
            
            if (missing > 0 && !string.IsNullOrEmpty(insufficientDescription))
            {
                _usageDescriptionText.text = string.Format(insufficientDescription, missing);
                return;
            }
        }
        
        _usageDescriptionText.text = description;
    }
    
    /// <summary>
    /// Clears the usage description text.
    /// </summary>
    private void ClearUsageDescription()
    {
        if (_usageDescriptionText != null)
        {
            _usageDescriptionText.text = "";
        }
    }

}

