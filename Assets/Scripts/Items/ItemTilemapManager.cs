using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages items by placing them on a tilemap. Items are drawn as special tiles that can be picked up.
/// Similar to GridDrawer, uses serialized fields to define item tile types.
/// </summary>
public class ItemTilemapManager : Singleton<ItemTilemapManager>
{
    [Header("References")]
    [SerializeField] [Tooltip("Tilemap for items. Must be assigned via Inspector. Should be on a separate layer above the environment tiles.")]
    private Tilemap _itemTilemap;

    [Header("Item Database")]
    [SerializeField] [Tooltip("Resource path (relative to a Resources folder) to the ItemDatabase asset.")]
    private string _itemDatabaseResourcePath = "ItemDatabase";

    private ItemDatabase _itemDatabase;

    // Track which positions have items (itemName -> positions)
    private readonly Dictionary<Vector2Int, string> _itemPositions = new Dictionary<Vector2Int, string>();
    
    // Dictionary to map item names to their definitions (built from scriptable object)
    private readonly Dictionary<string, ItemDatabase.ItemDefinition> _itemDefinitionLookup = new Dictionary<string, ItemDatabase.ItemDefinition>();

    protected override void Awake()
    {
        base.Awake();

        // Validate that Tilemap is assigned
        if (_itemTilemap == null)
        {
            Debug.LogError("ItemTilemapManager: Item tilemap is not assigned! Please assign a Tilemap component in the Inspector.");
        }

        // Load definitions from the scriptable object database
        LoadItemDatabase();
    }

    /// <summary>
    /// Loads the item database from Resources and builds a lookup of definitions.
    /// </summary>
    private void LoadItemDatabase()
    {
        _itemDefinitionLookup.Clear();
        _itemDatabase = Resources.Load<ItemDatabase>(_itemDatabaseResourcePath);

        if (_itemDatabase == null)
        {
            Debug.LogError($"ItemTilemapManager: Failed to load ItemDatabase at Resources path '{_itemDatabaseResourcePath}'. Items cannot be placed.");
            return;
        }

        IReadOnlyList<ItemDatabase.ItemDefinition> definitions = _itemDatabase.Items;
        for (int i = 0; i < definitions.Count; i++)
        {
            ItemDatabase.ItemDefinition definition = definitions[i];
            if (definition == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(definition.itemName))
            {
                Debug.LogWarning("ItemTilemapManager: Found item definition with null or empty name. Skipping.");
                continue;
            }

            if (definition.tile == null)
            {
                Debug.LogWarning($"ItemTilemapManager: Item definition '{definition.itemName}' has no tile assigned. Skipping.");
                continue;
            }

            if (_itemDefinitionLookup.ContainsKey(definition.itemName))
            {
                Debug.LogWarning($"ItemTilemapManager: Duplicate item name '{definition.itemName}' found in database. Keeping first occurrence.");
                continue;
            }

            _itemDefinitionLookup[definition.itemName] = definition;
        }

        Debug.Log($"ItemTilemapManager: Loaded {_itemDefinitionLookup.Count} item definitions from '{_itemDatabaseResourcePath}'.");
    }

    /// <summary>
    /// Gets the tile for a given item name. Returns null if not found.
    /// </summary>
    private ItemDatabase.ItemDefinition GetDefinitionForItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return null;
        }

        if (_itemDefinitionLookup.TryGetValue(itemName, out ItemDatabase.ItemDefinition definition))
        {
            return definition;
        }

        Debug.LogWarning($"ItemTilemapManager: Item definition not found for '{itemName}'. Ensure the ItemDatabase contains this item.");
        return null;
    }

    private TileBase GetTileForItemName(string itemName)
    {
        ItemDatabase.ItemDefinition definition = GetDefinitionForItem(itemName);
        return definition?.tile;
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
            Debug.LogError($"ItemTilemapManager: Item tile not found for '{itemName}'. Make sure the ItemDatabase contains this definition with an assigned tile.");
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

    /// <summary>
    /// Gets the tile at the specified grid position. Returns null if no tile exists.
    /// </summary>
    public TileBase GetTileAt(Vector2Int gridPosition)
    {
        if (_itemTilemap == null)
        {
            return null;
        }

        Vector3Int tilePosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
        return _itemTilemap.GetTile(tilePosition);
    }

    /// <summary>
    /// Extracts the sprite from a tile. Handles both Tile and AnimatedTile types.
    /// Returns null if sprite cannot be extracted.
    /// </summary>
    public Sprite GetSpriteFromTile(TileBase tile)
    {
        if (tile == null)
        {
            return null;
        }

        // Try to get sprite from Tile (not TileBase)
        if (tile is Tile tileAsset)
        {
            if (tileAsset.sprite != null)
            {
                return tileAsset.sprite;
            }
        }
        // Try to get first sprite from AnimatedTile
        else if (tile.GetType().Name == "AnimatedTile")
        {
            // Use reflection to access m_AnimatedSprites field
            // Try with different binding flags to search through inheritance hierarchy
            var animatedSpritesField = tile.GetType().GetField("m_AnimatedSprites", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);

            if (animatedSpritesField == null)
            {
                var allFields = tile.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
                
                foreach (var field in allFields)
                {
                    if (field.FieldType == typeof(Sprite[]))
                    {
                        animatedSpritesField = field;
                        break;
                    }
                }
            }
            
            if (animatedSpritesField != null)
            {
                Sprite[] animatedSprites = animatedSpritesField.GetValue(tile) as Sprite[];
                if (animatedSprites != null && animatedSprites.Length > 0 && animatedSprites[0] != null)
                {
                    return animatedSprites[0];
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the sprite from the tile at the specified grid position.
    /// Returns null if no tile exists or sprite cannot be extracted.
    /// </summary>
    public Sprite GetItemSprite(string itemName)
    {
        ItemDatabase.ItemDefinition definition = GetDefinitionForItem(itemName);
        if (definition == null)
        {
            return null;
        }

        if (definition.inventorySprite != null)
        {
            return definition.inventorySprite;
        }

        if (definition.tile != null)
        {
            return GetSpriteFromTile(definition.tile);
        }

        return null;
    }

    /// <summary>
    /// Gets the sprite from the item currently occupying the specified grid position.
    /// </summary>
    public Sprite GetSpriteAt(Vector2Int gridPosition)
    {
        string itemName = GetItemNameAt(gridPosition);
        if (string.IsNullOrEmpty(itemName))
        {
            return null;
        }

        return GetItemSprite(itemName);
    }
}

