using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Serializable class to map item names to their tile assets.
/// </summary>
[System.Serializable]
public class ItemTileEntry
{
    [Tooltip("Name identifier for this item type (must match names used in level files)")]
    public string itemName;
    
    [Tooltip("Tile asset to use for this item type")]
    public TileBase tile;
}

/// <summary>
/// Manages items by placing them on a tilemap. Items are drawn as special tiles that can be picked up.
/// Similar to GridDrawer, uses serialized fields to define item tile types.
/// </summary>
public class ItemTilemapManager : Singleton<ItemTilemapManager>
{
    [Header("References")]
    [SerializeField] [Tooltip("Tilemap for items. Must be assigned via Inspector. Should be on a separate layer above the environment tiles.")]
    private Tilemap _itemTilemap;

    [Header("Item Tile Types")]
    [SerializeField] [Tooltip("List of item types and their corresponding tiles. Item names must match those used in level files.")]
    private List<ItemTileEntry> _itemTiles = new List<ItemTileEntry>();

    // Track which positions have items (itemName -> positions)
    private Dictionary<Vector2Int, string> _itemPositions = new Dictionary<Vector2Int, string>();
    
    // Dictionary to map item names to their tile assets (built from serialized list)
    private Dictionary<string, TileBase> _itemTileDictionary = new Dictionary<string, TileBase>();

    protected override void Awake()
    {
        base.Awake();

        // Validate that Tilemap is assigned
        if (_itemTilemap == null)
        {
            Debug.LogError("ItemTilemapManager: Item tilemap is not assigned! Please assign a Tilemap component in the Inspector.");
        }

        // Build dictionary from serialized list
        BuildItemTileDictionary();
    }

    /// <summary>
    /// Builds the item tile dictionary from the serialized list.
    /// </summary>
    private void BuildItemTileDictionary()
    {
        _itemTileDictionary.Clear();

        foreach (ItemTileEntry entry in _itemTiles)
        {
            if (entry == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.itemName))
            {
                Debug.LogWarning("ItemTilemapManager: Found item tile entry with null or empty name. Skipping.");
                continue;
            }

            if (entry.tile == null)
            {
                Debug.LogWarning($"ItemTilemapManager: Item tile entry '{entry.itemName}' has no tile assigned. Skipping.");
                continue;
            }

            if (_itemTileDictionary.ContainsKey(entry.itemName))
            {
                Debug.LogWarning($"ItemTilemapManager: Duplicate item name '{entry.itemName}' found. Keeping first occurrence.");
                continue;
            }

            _itemTileDictionary[entry.itemName] = entry.tile;
        }

        Debug.Log($"ItemTilemapManager: Built item tile dictionary with {_itemTileDictionary.Count} entries.");
    }

    /// <summary>
    /// Gets the tile for a given item name. Returns null if not found.
    /// </summary>
    private TileBase GetTileForItemName(string itemName)
    {
        if (_itemTileDictionary.TryGetValue(itemName, out TileBase tile))
        {
            return tile;
        }
        return null;
    }

    /// <summary>
    /// Places an item at the specified grid position.
    /// </summary>
    public void PlaceItem(string itemName, Vector2Int gridPosition)
    {
        if (_itemTilemap == null)
        {
            Debug.LogError("ItemTilemapManager: Item tilemap is null! Cannot place item.");
            return;
        }

        TileBase tile = GetTileForItemName(itemName);
        if (tile == null)
        {
            Debug.LogError($"ItemTilemapManager: Item tile not found for '{itemName}'. Make sure the item name is defined in the Item Tile Types list with an assigned tile.");
            return;
        }

        if (EnvironmentManager.Instance != null && !EnvironmentManager.Instance.IsValidPosition(gridPosition))
        {
            Debug.LogWarning($"ItemTilemapManager: Cannot place item at invalid position ({gridPosition.x}, {gridPosition.y}).");
            return;
        }

        // Check if there's already an item at this position
        if (_itemPositions.ContainsKey(gridPosition))
        {
            Debug.LogWarning($"ItemTilemapManager: Item already exists at position ({gridPosition.x}, {gridPosition.y}). Replacing with new item.");
        }

        Vector3Int tilePosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
        _itemTilemap.SetTile(tilePosition, tile);
        _itemPositions[gridPosition] = itemName;
    }

    /// <summary>
    /// Removes an item from the specified grid position.
    /// </summary>
    public void RemoveItem(Vector2Int gridPosition)
    {
        if (_itemTilemap == null)
        {
            return;
        }

        Vector3Int tilePosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
        _itemTilemap.SetTile(tilePosition, null);
        _itemPositions.Remove(gridPosition);
    }

    /// <summary>
    /// Checks if there is an item at the specified grid position.
    /// </summary>
    public bool HasItemAt(Vector2Int gridPosition)
    {
        return _itemPositions.ContainsKey(gridPosition);
    }

    /// <summary>
    /// Gets the item name at the specified grid position. Returns null if no item exists.
    /// </summary>
    public string GetItemNameAt(Vector2Int gridPosition)
    {
        if (_itemPositions.TryGetValue(gridPosition, out string itemName))
        {
            return itemName;
        }
        return null;
    }

    /// <summary>
    /// Clears all items from the tilemap.
    /// </summary>
    public void ClearAllItems()
    {
        if (_itemTilemap != null)
        {
            _itemTilemap.ClearAllTiles();
        }
        _itemPositions.Clear();
    }

    /// <summary>
    /// Places multiple items from level data.
    /// </summary>
    public void PlaceItemsFromLevelData(List<(string itemName, int x, int y)> items)
    {
        ClearAllItems();

        foreach (var (itemName, x, y) in items)
        {
            Vector2Int gridPos = new Vector2Int(x, y);
            PlaceItem(itemName, gridPos);
        }
    }

    /// <summary>
    /// Gets the tile for a given item name. Public method for external access.
    /// </summary>
    public TileBase GetItemTile(string itemName)
    {
        return GetTileForItemName(itemName);
    }
}

