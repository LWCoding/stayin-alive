using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages items in the game as prefabs, similar to InteractableManager.
/// Handles spawning and tracking items from level data.
/// Loads ItemData ScriptableObjects from Resources/Items/ folder.
/// </summary>
public class ItemManager : Singleton<ItemManager>
{
    [Header("Item Settings")]
    [SerializeField] private Transform _itemParent;
    
    // Dictionary mapping item names to their ItemData
    private Dictionary<string, ItemData> _itemDataDictionary = new Dictionary<string, ItemData>();
    
    // Track all items in the scene
    private List<Item> _items = new List<Item>();
    
    protected override void Awake()
    {
        base.Awake();

        // Load all ItemData from Resources/Items/ folder
        LoadItemData();
    }
    
    /// <summary>
    /// Loads all ItemData ScriptableObjects from the Resources/Items/ folder.
    /// </summary>
    private void LoadItemData()
    {
        ItemData[] itemDataArray = Resources.LoadAll<ItemData>("Items");
        
        if (itemDataArray == null || itemDataArray.Length == 0)
        {
            Debug.LogWarning("ItemManager: No ItemData found in Resources/Items/ folder! Please create ItemData ScriptableObjects and place them in Resources/Items/.");
            return;
        }

        _itemDataDictionary.Clear();

        foreach (ItemData itemData in itemDataArray)
        {
            if (itemData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(itemData.itemName))
            {
                Debug.LogWarning($"ItemManager: Found ItemData with null or empty name: {itemData.name}");
                continue;
            }

            if (_itemDataDictionary.ContainsKey(itemData.itemName))
            {
                Debug.LogWarning($"ItemManager: Duplicate item name '{itemData.itemName}' found. Keeping first occurrence.");
                continue;
            }

            _itemDataDictionary[itemData.itemName] = itemData;
        }

        Debug.Log($"ItemManager: Loaded {_itemDataDictionary.Count} item data entries from Resources/Items/");
    }
    
    /// <summary>
    /// Gets an ItemData by name. Returns null if not found.
    /// </summary>
    private ItemData GetItemData(string itemName)
    {
        if (_itemDataDictionary.TryGetValue(itemName, out ItemData data))
        {
            return data;
        }

        Debug.LogWarning($"ItemManager: Item data not found for name '{itemName}'");
        return null;
    }
    
    /// <summary>
    /// Spawns an item at the specified grid position.
    /// </summary>
    /// <param name="itemName">Name of the item to spawn (must match ItemData)</param>
    /// <param name="gridPosition">Grid position to spawn the item at</param>
    /// <returns>The spawned Item component, or null if ItemData or prefab is not found</returns>
    public Item SpawnItem(string itemName, Vector2Int gridPosition)
    {
        ItemData itemData = GetItemData(itemName);
        if (itemData == null)
        {
            Debug.LogError($"ItemManager: Item data not found for '{itemName}'! Make sure the ItemData ScriptableObject exists in Resources/Items/ folder.");
            return null;
        }

        if (itemData.prefab == null)
        {
            Debug.LogError($"ItemManager: Prefab is not assigned for item '{itemName}'!");
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
        GameObject itemObj = Instantiate(itemData.prefab, _itemParent);
        Item item = itemObj.GetComponent<Item>();
        
        if (item == null)
        {
            Debug.LogError($"ItemManager: Item prefab for '{itemName}' does not have an Item component!");
            Destroy(itemObj);
            return null;
        }
        
        // Initialize the item with the item name
        item.Initialize(gridPosition, itemName);
        _items.Add(item);
        
        Debug.Log($"ItemManager: Spawned item '{itemName}' at ({gridPosition.x}, {gridPosition.y})");
        
        return item;
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
        
        ItemData itemData = GetItemData(itemName);
        if (itemData == null || itemData.prefab == null)
        {
            Debug.LogWarning($"ItemManager: Cannot get sprite for item '{itemName}' - ItemData or prefab not found.");
            return null;
        }
        
        // Create a temporary instance to get the sprite
        GameObject tempItemObj = Instantiate(itemData.prefab);
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
    /// Creates an inactive Item instance for storage (inventory, den, etc.).
    /// The item is created as an inactive GameObject that won't exist in the world.
    /// </summary>
    /// <param name="itemName">Name of the item to create</param>
    /// <returns>The created Item, or null if ItemData or prefab is not found</returns>
    public Item CreateItemForStorage(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return null;
        }
        
        ItemData itemData = GetItemData(itemName);
        if (itemData == null || itemData.prefab == null)
        {
            Debug.LogWarning($"ItemManager: Cannot create item '{itemName}' - ItemData or prefab not found.");
            return null;
        }
        
        // Create an inactive instance for storage
        GameObject itemObj = Instantiate(itemData.prefab);
        itemObj.SetActive(false);
        itemObj.transform.position = new Vector3(0, 0, 1000);
        
        Item item = itemObj.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning($"ItemManager: Item prefab for '{itemName}' does not have an Item component!");
            Destroy(itemObj);
            return null;
        }
        
        return item;
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
