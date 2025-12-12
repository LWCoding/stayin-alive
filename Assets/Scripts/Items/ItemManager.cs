using System.Collections.Generic;
using System.Linq;
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
    
    // Dictionary mapping item types (enum) to their ItemData
    private Dictionary<ItemId, ItemData> _itemDataDictionary = new Dictionary<ItemId, ItemData>();
    
    // Track all items in the scene
    private List<Item> _items = new List<Item>();
    
    /// list of all hunger values items have in order from least to greatest.
    /// Used in order to get items with the smallest hunger values
    private List<int> hungerOrder = new List<int>();
    public List<int> HungerOrder => hungerOrder.ToList();
    
    /// <summary>
    /// Gets all items in the scene.
    /// </summary>
    public List<Item> Items => _items.Where(i => i != null).ToList();
    
    protected override void Awake()
    {
        base.Awake();

        // Load all ItemData from Resources/Items/ folder
        LoadItemData();

        // Initialize the hunger restored list
        foreach (ItemData itemData in _itemDataDictionary.Values) {
          Item item = itemData.prefab.GetComponent<Item>();
          if (item == null || item is not FoodItem) {
            continue;
          }
          FoodItem foodItem = item as FoodItem;
          hungerOrder.Add(foodItem.HungerRestored);
        }
        hungerOrder.Sort();
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

            if (_itemDataDictionary.ContainsKey(itemData.itemId))
            {
                Debug.LogWarning($"ItemManager: Duplicate item type '{itemData.itemId}' found. Keeping first occurrence.");
                continue;
            }

            _itemDataDictionary[itemData.itemId] = itemData;
        }

        Debug.Log($"ItemManager: Loaded {_itemDataDictionary.Count} item data entries from Resources/Items/");
    }
    
    /// <summary>
    /// Gets an ItemData by ItemType enum. Returns null if not found.
    /// </summary>
    public ItemData GetItemData(ItemId itemType)
    {
        if (_itemDataDictionary.TryGetValue(itemType, out ItemData data))
        {
            return data;
        }

        Debug.LogWarning($"ItemManager: Item data not found for type '{itemType}'");
        return null;
    }
    
    /// <summary>
    /// Gets the item name string for a given ItemType. Returns the enum string as fallback if ItemData not found.
    /// </summary>
    public string GetItemName(ItemId itemType)
    {
        ItemData itemData = GetItemData(itemType);
        if (itemData != null && !string.IsNullOrEmpty(itemData.itemName))
        {
            return itemData.itemName;
        }
        
        // Fallback to enum string if ItemData not found or itemName is empty
        return itemType.ToString();
    }
    
    /// <summary>
    /// Spawns an item at the specified grid position.
    /// </summary>
    /// <param name="itemType">Type of the item to spawn</param>
    /// <param name="gridPosition">Grid position to spawn the item at</param>
    /// <returns>The spawned Item component, or null if ItemData or prefab is not found</returns>
    public Item SpawnItem(ItemId itemType, Vector2Int gridPosition)
    {
        ItemData itemData = GetItemData(itemType);
        if (itemData == null)
        {
            Debug.LogError($"ItemManager: Item data not found for '{itemType}'! Make sure the ItemData ScriptableObject exists in Resources/Items/ folder.");
            return null;
        }

        if (itemData.prefab == null)
        {
            Debug.LogError($"ItemManager: Prefab is not assigned for item '{itemType}'!");
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
            Debug.LogError($"ItemManager: Item prefab for '{itemType}' does not have an Item component!");
            Destroy(itemObj);
            return null;
        }
        
        // Initialize the item with the item type
        item.Initialize(gridPosition, itemType);
        _items.Add(item);
        
        Debug.Log($"ItemManager: Spawned item '{itemType}' at ({gridPosition.x}, {gridPosition.y})");
        
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
    /// Gets the inventory sprite for an item by type. Creates a temporary instance to get the sprite, then destroys it.
    /// </summary>
    /// <param name="itemType">Type of the item</param>
    /// <returns>The inventory sprite, or null if not found</returns>
    public Sprite GetItemSprite(ItemId itemType)
    {
        ItemData itemData = GetItemData(itemType);
        if (itemData == null || itemData.prefab == null)
        {
            Debug.LogWarning($"ItemManager: Cannot get sprite for item '{itemType}' - ItemData or prefab not found.");
            return null;
        }
        
        // Create a temporary instance to get the sprite
        GameObject tempItemObj = Instantiate(itemData.prefab);
        Item item = tempItemObj.GetComponent<Item>();
        
        if (item == null)
        {
            Debug.LogWarning($"ItemManager: Item prefab for '{itemType}' does not have an Item component!");
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
    /// <param name="itemType">Type of the item to create</param>
    /// <returns>The created Item, or null if ItemData or prefab is not found</returns>
    public Item CreateItemForStorage(ItemId itemType)
    {
        ItemData itemData = GetItemData(itemType);
        if (itemData == null || itemData.prefab == null)
        {
            Debug.LogWarning($"ItemManager: Cannot create item '{itemType}' - ItemData or prefab not found.");
            return null;
        }
        
        // Create an inactive instance for storage
        GameObject itemObj = Instantiate(itemData.prefab);
        itemObj.SetActive(false);
        itemObj.transform.position = new Vector3(0, 0, 1000);
        
        Item item = itemObj.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning($"ItemManager: Item prefab for '{itemType}' does not have an Item component!");
            Destroy(itemObj);
            return null;
        }
        
        // Initialize with the item type
        item.Initialize(Vector2Int.zero, itemType);
        
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
