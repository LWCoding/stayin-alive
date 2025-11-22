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
    
    [Header("Shake Settings")]
    [SerializeField] [Tooltip("Intensity of the shake when inventory is full")]
    private float _shakeIntensity = 10f;
    
    [SerializeField] [Tooltip("Duration of the shake animation in seconds")]
    private float _shakeDuration = 0.5f;
    
    [Header("Usage Description")]
    [SerializeField] [Tooltip("Text component that displays the usage description of the selected item")]
    private TextMeshProUGUI _usageDescriptionText;
    
    // List of all inventory slots
    private List<InventorySlot> _inventorySlots = new List<InventorySlot>();
    
    // Currently selected slot index (-1 if none selected)
    private int _selectedSlotIndex = -1;
    private int _activeUseSlotIndex = -1;
    
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
    public bool IsFull => CurrentItemCount >= Globals.MaxInventorySize;
    
    /// <summary>
    /// The currently selected slot index (0-8, or -1 if none selected).
    /// </summary>
    public int SelectedSlotIndex => _selectedSlotIndex;
    
    /// <summary>
    /// The currently selected slot, or null if none selected.
    /// </summary>
    public InventorySlot SelectedSlot => (_selectedSlotIndex >= 0 && _selectedSlotIndex < _inventorySlots.Count) ? _inventorySlots[_selectedSlotIndex] : null;
    
    /// <summary>
    /// The slot index currently being used (during item consumption). -1 when idle.
    /// </summary>
    public int ActiveUseSlotIndex => _activeUseSlotIndex;
    
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
    private void InitializeInventorySlots()
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
            Debug.Log($"InventoryManager: Cannot add item '{itemName}' - inventory is full ({CurrentItemCount}/{Globals.MaxInventorySize}).");
            
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
        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventorySlot slot = _inventorySlots[i];
            if (slot.IsEmpty)
            {
                if (slot.SetItem(itemName, itemSprite))
                {
                    Debug.Log($"InventoryManager: Added item '{itemName}' to inventory. ({CurrentItemCount}/{Globals.MaxInventorySize})");
                    
                    // Fire event for item added
                    OnItemAdded?.Invoke(itemName);
                    
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
                        if (!selectedSlot.IsEmpty && selectedSlot.ItemName == itemName)
                        {
                            UpdateUsageDescription();
                        }
                    }
                    
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
    public void UseItemInSlot(int slotIndex)
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
            _activeUseSlotIndex = slotIndex;
            try
            {
                itemUsed = ItemManager.Instance.UseItem(itemName, player);
            }
            finally
            {
                _activeUseSlotIndex = -1;
            }
        }
        else
        {
            Debug.LogWarning("InventoryManager: ItemManager instance not found! Cannot use item.");
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
            Debug.LogWarning($"InventoryManager: Item '{itemName}' could not be used.");
        }
    }
    
    /// <summary>
    /// Removes a specific number of items matching the provided name from the inventory.
    /// Optionally skips a slot (e.g., the slot currently being used).
    /// </summary>
    public bool RemoveItems(string itemName, int count, int excludeSlotIndex = -1)
    {
        if (count <= 0)
        {
            return true;
        }
        
        if (string.IsNullOrEmpty(itemName))
        {
            return false;
        }
        
        int removed = 0;
        
        for (int i = 0; i < _inventorySlots.Count && removed < count; i++)
        {
            if (i == excludeSlotIndex)
            {
                continue;
            }
            
            InventorySlot slot = _inventorySlots[i];
            if (!slot.IsEmpty && slot.ItemName == itemName)
            {
                slot.ClearSlot();
                removed++;
            }
        }
        
        return removed >= count;
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
            if (!activeSlot.IsEmpty && activeSlot.ItemName == itemName)
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
            if (!slot.IsEmpty && slot.ItemName == itemName)
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
        if (_inventoryContainer == null)
        {
            return;
        }
        
        // Play FullInventory sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.SFXType.FullInventory);
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
            float offsetX = UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity);
            float offsetY = UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity);
            
            // Apply shake offset
            _inventoryContainer.localPosition = _originalContainerPosition + new Vector3(offsetX, offsetY, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset to original position
        _inventoryContainer.localPosition = _originalContainerPosition;
        _shakeCoroutine = null;
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
        InventorySlot selectedSlot = SelectedSlot;
        if (selectedSlot == null || selectedSlot.IsEmpty)
        {
            ClearUsageDescription();
            return;
        }
        
        string description = "";
        string insufficientDescription = "";
        bool isSticksItem = false;
        
        if (ItemManager.Instance != null)
        {
            ItemUsageInfo usageInfo = ItemManager.Instance.GetItemUsageInfo(selectedSlot.ItemName);
            description = usageInfo.Description;
            insufficientDescription = usageInfo.InsufficientDescription;
            isSticksItem = usageInfo.IsSticksItem;
        }
        
        if (isSticksItem)
        {
            int availableSticks = GetItemCount(selectedSlot.ItemName);
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

     /// <summary>
    /// Returns a list of Item Objects representing the items in the player's inventory
    /// </summary>
    public List<Item> GetInventoryItems() { 
       List<Item> items = new List<Item>();
       foreach (InventorySlot slot in _inventorySlots) {
         Item item = slot.GetItem();
         if (item != null) {
           items.Add(item);
         }
       }
       return items;
    }
}

