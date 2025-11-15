using System.Collections;
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
    
    [Header("Shake Settings")]
    [SerializeField] [Tooltip("Intensity of the shake when inventory is full")]
    private float _shakeIntensity = 10f;
    
    [SerializeField] [Tooltip("Duration of the shake animation in seconds")]
    private float _shakeDuration = 0.5f;
    
    // List of all inventory slots
    private List<InventorySlot> _inventorySlots = new List<InventorySlot>();
    
    // Currently selected slot index (-1 if none selected)
    private int _selectedSlotIndex = -1;
    
    // Store original position of inventory container for shake animation
    private Vector3 _originalContainerPosition;
    
    // Track if shake coroutine is running
    private Coroutine _shakeCoroutine;
    
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
    
    /// <summary>
    /// The currently selected slot index (0-8, or -1 if none selected).
    /// </summary>
    public int SelectedSlotIndex => _selectedSlotIndex;
    
    /// <summary>
    /// The currently selected slot, or null if none selected.
    /// </summary>
    public InventorySlot SelectedSlot => (_selectedSlotIndex >= 0 && _selectedSlotIndex < _inventorySlots.Count) ? _inventorySlots[_selectedSlotIndex] : null;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Validate references
        if (_inventoryContainer == null)
        {
            Debug.LogError("InventoryManager: Inventory container is not assigned! Please assign a Transform in the Inspector.");
        }
        else
        {
            // Store original position for shake animation
            _originalContainerPosition = _inventoryContainer.localPosition;
        }
        
        if (_inventorySlotPrefab == null)
        {
            Debug.LogError("InventoryManager: Inventory slot prefab is not assigned! Please assign the UIInventorySlot prefab in the Inspector.");
        }
        
        // Initialize inventory slots
        InitializeInventorySlots();
    }
    
    private void Update()
    {
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
    /// The corresponding sprite is resolved automatically using the ItemManager.
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
            
            // Trigger shake animation
            ShakeInventory();
            
            return false;
        }

        Sprite itemSprite = null;
        if (ItemManager.Instance != null)
        {
            itemSprite = ItemManager.Instance.GetItemSprite(itemName);
        }
        else
        {
            Debug.LogWarning("InventoryManager: ItemManager instance not found. Item will be added without a sprite.");
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
    /// Selects a slot by index (0-8). Deselects the previously selected slot.
    /// </summary>
    public void SelectSlot(int index)
    {
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
    }
    
    /// <summary>
    /// Uses the item in the specified slot. Calls the item's OnUse method if it implements IItem.
    /// </summary>
    public void UseItemInSlot(int slotIndex)
    {
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
        
        string itemName = slot.ItemName;
        
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
        
        // Use the item through ItemManager
        bool itemUsed = false;
        
        if (ItemManager.Instance != null)
        {
            itemUsed = ItemManager.Instance.UseItem(itemName, player);
        }
        else
        {
            Debug.LogWarning("InventoryManager: ItemManager instance not found! Cannot use item.");
        }
        
        // If item was successfully used, remove it from inventory
        if (itemUsed)
        {
            slot.ClearSlot();
        }
        else
        {
            Debug.LogWarning($"InventoryManager: Item '{itemName}' could not be used.");
        }
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
        if (_inventoryContainer == null)
        {
            return;
        }
        
        // Stop any existing shake coroutine
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            // Reset to original position before starting new shake
            _inventoryContainer.localPosition = _originalContainerPosition;
        }
        
        // Always capture the current position right before shaking to ensure we use the correct base position
        // This prevents teleportation issues if the UI was repositioned after Awake()
        _originalContainerPosition = _inventoryContainer.localPosition;
        
        // Start shake coroutine
        _shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }
    
    /// <summary>
    /// Coroutine that shakes the inventory container.
    /// </summary>
    private IEnumerator ShakeCoroutine()
    {
        if (_inventoryContainer == null)
        {
            yield break;
        }
        
        float elapsed = 0f;
        
        while (elapsed < _shakeDuration)
        {
            // Calculate random offset for shake
            float offsetX = Random.Range(-_shakeIntensity, _shakeIntensity);
            float offsetY = Random.Range(-_shakeIntensity, _shakeIntensity);
            
            // Apply shake offset
            _inventoryContainer.localPosition = _originalContainerPosition + new Vector3(offsetX, offsetY, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset to original position
        _inventoryContainer.localPosition = _originalContainerPosition;
        _shakeCoroutine = null;
    }
}

