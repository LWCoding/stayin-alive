using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages items in the game as prefabs, similar to InteractableManager.
/// Handles spawning and tracking items from level data.
/// </summary>
public class ItemManager : Singleton<ItemManager>
{
    [Header("Item Settings")]
    [SerializeField] private Transform _itemParent;
    
    [Header("Item Prefabs")]
    [Tooltip("Dictionary mapping item names to their prefabs. Set in Inspector.")]
    [SerializeField] private List<ItemPrefabEntry> _itemPrefabs = new List<ItemPrefabEntry>();
    
    [System.Serializable]
    private class ItemPrefabEntry
    {
        public string itemName;
        public GameObject prefab;
    }
    
    // Dictionary for quick lookup
    private Dictionary<string, GameObject> _itemPrefabLookup = new Dictionary<string, GameObject>();
    
    // Track all items in the scene
    private List<Item> _items = new List<Item>();
    
    protected override void Awake()
    {
        base.Awake();

        // Build prefab lookup dictionary
        BuildPrefabLookup();
    }
    
    /// <summary>
    /// Builds the prefab lookup dictionary from the serialized list.
    /// </summary>
    private void BuildPrefabLookup()
    {
        _itemPrefabLookup.Clear();
        
        foreach (ItemPrefabEntry entry in _itemPrefabs)
        {
            if (string.IsNullOrEmpty(entry.itemName) || entry.prefab == null)
            {
                continue;
            }
            
            if (_itemPrefabLookup.ContainsKey(entry.itemName))
            {
                Debug.LogWarning($"ItemManager: Duplicate item name '{entry.itemName}' found in prefab list. Keeping first occurrence.");
                continue;
            }
            
            _itemPrefabLookup[entry.itemName] = entry.prefab;
        }
        
        Debug.Log($"ItemManager: Built prefab lookup with {_itemPrefabLookup.Count} entries.");
    }
    
    /// <summary>
    /// Spawns an item at the specified grid position.
    /// </summary>
    /// <param name="itemName">Name of the item to spawn (must match ItemDatabase and prefab entry)</param>
    /// <param name="gridPosition">Grid position to spawn the item at</param>
    /// <returns>The spawned Item component, or null if prefab is not found</returns>
    public Item SpawnItem(string itemName, Vector2Int gridPosition)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogError("ItemManager: Cannot spawn item with null or empty name.");
            return null;
        }
        
        if (!_itemPrefabLookup.TryGetValue(itemName, out GameObject prefab))
        {
            Debug.LogError($"ItemManager: Prefab not found for item '{itemName}'. Please add it to the item prefabs list in the Inspector.");
            return null;
        }
        
        if (EnvironmentManager.Instance == null)
        {
            Debug.LogError("ItemManager: EnvironmentManager instance not found!");
            return null;
        }
        
        if (!EnvironmentManager.Instance.IsValidPosition(gridPosition))
        {
            Debug.LogWarning($"ItemManager: Cannot spawn item at invalid position ({gridPosition.x}, {gridPosition.y}).");
            return null;
        }
        
        // Instantiate the item prefab
        GameObject itemObj = Instantiate(prefab, _itemParent);
        Item item = itemObj.GetComponent<Item>();
        
        if (item == null)
        {
            Debug.LogError($"ItemManager: Item prefab for '{itemName}' does not have an Item component!");
            Destroy(itemObj);
            return null;
        }
        
        // Initialize the item with the item name to ensure it matches the lookup key
        item.Initialize(gridPosition, itemName);
        
        _items.Add(item);
        
        Debug.Log($"ItemManager: Spawned item '{itemName}' at ({gridPosition.x}, {gridPosition.y})");
        
        return item;
    }
    
    /// <summary>
    /// Spawns multiple items from level data.
    /// </summary>
    public void SpawnItemsFromLevelData(List<(string itemName, int x, int y)> items)
    {
        ClearAllItems();
        
        if (items == null)
        {
            return;
        }
        
        foreach (var (itemName, x, y) in items)
        {
            Vector2Int gridPos = new Vector2Int(x, y);
            if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
            {
                SpawnItem(itemName, gridPos);
            }
            else
            {
                Debug.LogWarning($"ItemManager: Item '{itemName}' at ({x}, {y}) is out of bounds!");
            }
        }
    }
    
    /// <summary>
    /// Gets the item at the specified grid position, if any.
    /// </summary>
    public Item GetItemAtPosition(Vector2Int gridPosition)
    {
        // Filter out null references
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i] == null)
            {
                _items.RemoveAt(i);
            }
        }
        
        foreach (Item item in _items)
        {
            if (item != null && item.GridPosition == gridPosition)
            {
                return item;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets all items in the scene.
    /// </summary>
    public List<Item> GetAllItems()
    {
        // Filter out null references
        List<Item> validItems = new List<Item>();
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i] == null)
            {
                _items.RemoveAt(i);
            }
            else
            {
                validItems.Add(_items[i]);
            }
        }
        return validItems;
    }
    
    /// <summary>
    /// Removes an item from the items list. Called when an item is destroyed.
    /// </summary>
    public void RemoveItem(Item item)
    {
        if (item != null && _items != null)
        {
            _items.Remove(item);
        }
    }
    
    /// <summary>
    /// Gets the inventory sprite for an item by name. Creates a temporary instance to get the sprite, then destroys it.
    /// </summary>
    /// <param name="itemName">Name of the item</param>
    /// <returns>The inventory sprite, or null if not found</returns>
    public Sprite GetItemSprite(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return null;
        }
        
        if (!_itemPrefabLookup.TryGetValue(itemName, out GameObject prefab))
        {
            Debug.LogWarning($"ItemManager: Cannot get sprite for item '{itemName}' - prefab not found.");
            return null;
        }
        
        // Create a temporary instance to get the sprite
        GameObject tempItemObj = Instantiate(prefab);
        Item item = tempItemObj.GetComponent<Item>();
        
        if (item == null)
        {
            Debug.LogWarning($"ItemManager: Item prefab for '{itemName}' does not have an Item component!");
            Destroy(tempItemObj);
            return null;
        }
        
        Sprite sprite = item.InventorySprite;
        
        // Destroy the temporary instance
        Destroy(tempItemObj);
        
        return sprite;
    }
    
    /// <summary>
    /// Gets usage-related metadata for an item by name, including optional conditional descriptions.
    /// </summary>
    public ItemUsageInfo GetItemUsageInfo(string itemName)
    {
        ItemUsageInfo usageInfo = new ItemUsageInfo();
        
        if (string.IsNullOrEmpty(itemName))
        {
            return usageInfo;
        }
        
        if (!_itemPrefabLookup.TryGetValue(itemName, out GameObject prefab))
        {
            Debug.LogWarning($"ItemManager: Cannot get usage info for item '{itemName}' - prefab not found.");
            return usageInfo;
        }
        
        GameObject tempItemObj = Instantiate(prefab);
        Item item = tempItemObj.GetComponent<Item>();
        
        if (item == null)
        {
            Debug.LogWarning($"ItemManager: Item prefab for '{itemName}' does not have an Item component!");
            Destroy(tempItemObj);
            return usageInfo;
        }
        
        usageInfo.Description = item.UsageDescription ?? "";
        
        if (item is SticksItem sticksItem)
        {
            usageInfo.IsSticksItem = true;
            usageInfo.InsufficientDescription = sticksItem.InsufficientSticksDescription ?? "";
        }
        
        Destroy(tempItemObj);
        
        return usageInfo;
    }
    
    /// <summary>
    /// Gets the usage description for an item by name. Provided for backward compatibility.
    /// </summary>
    public string GetItemUsageDescription(string itemName)
    {
        return GetItemUsageInfo(itemName).Description;
    }

    public Item GetItemFromName(string itemName) {
      if (string.IsNullOrEmpty(itemName))
      {
        return null;
      }
        
      if (!_itemPrefabLookup.TryGetValue(itemName, out GameObject prefab))
      {
        Debug.LogWarning($"ItemManager: Cannot use item '{itemName}' - prefab not found.");
        return null;
      }
        
      // Create a temporary instance to call OnUse
      GameObject tempItemObj = Instantiate(prefab);
      Item item = tempItemObj.GetComponent<Item>();
      // Just beyond the clipping plane of camera!
      tempItemObj.transform.position += new Vector3(0, 0, 1001);
      return item;
    }
    
    /// <summary>
    /// Uses an item by name. Creates a temporary instance to call OnUse, then destroys it.
    /// </summary>
    /// <param name="itemName">Name of the item to use</param>
    /// <param name="user">The ControllableAnimal using the item</param>
    /// <returns>True if the item was successfully used, false otherwise</returns>
    public bool UseItem(string itemName, ControllableAnimal user)
    {
        if (string.IsNullOrEmpty(itemName) || user == null)
        {
            return false;
        }
        
        if (!_itemPrefabLookup.TryGetValue(itemName, out GameObject prefab))
        {
            Debug.LogWarning($"ItemManager: Cannot use item '{itemName}' - prefab not found.");
            return false;
        }
        
        // Create a temporary instance to call OnUse
        GameObject tempItemObj = Instantiate(prefab);
        Item item = tempItemObj.GetComponent<Item>();
        
        if (item == null)
        {
            Debug.LogWarning($"ItemManager: Item prefab for '{itemName}' does not have an Item component!");
            Destroy(tempItemObj);
            return false;
        }
        
        // Call OnUse
        bool wasUsed = item.OnUse(user);
        
        // Destroy the temporary instance
        Destroy(tempItemObj);
        
        return wasUsed;
    }
    
    /// <summary>
    /// Clears all items from the scene.
    /// </summary>
    public void ClearAllItems()
    {
        foreach (Item item in _items)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        _items.Clear();
    }
}

/// <summary>
/// Metadata describing how an item should present its usage information.
/// </summary>
public struct ItemUsageInfo
{
    public string Description;
    public string InsufficientDescription;
    public bool IsSticksItem;
}

