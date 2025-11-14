using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ScriptableObject that catalogs every item that can appear in the world.
/// Each entry defines the item's name along with the tile used for placement
/// and the sprite that should be shown in UI such as the inventory.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
	[System.Serializable]
	public class ItemDefinition
	{
		[Tooltip("Unique name identifier for the item. Must match level data and pickup logic.")]
		public string itemName;

		[Tooltip("Tile that represents this item on the item tilemap.")]
		public TileBase tile;

		[Tooltip("Sprite used when displaying this item in UI (e.g., inventory).")]
		public Sprite inventorySprite;
	}

	[SerializeField, Tooltip("List of all item definitions available in the game.")]
	private List<ItemDefinition> _items = new List<ItemDefinition>();

	private Dictionary<string, ItemDefinition> _lookup;

	/// <summary>
	/// Returns the list of item definitions.
	/// </summary>
	public IReadOnlyList<ItemDefinition> Items => _items;

	/// <summary>
	/// Attempts to get the definition for the given item name.
	/// </summary>
	public bool TryGetDefinition(string itemName, out ItemDefinition definition)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			definition = null;
			return false;
		}

		EnsureLookup();
		return _lookup.TryGetValue(itemName, out definition);
	}

	private void EnsureLookup()
	{
		if (_lookup != null)
		{
			return;
		}

		_lookup = new Dictionary<string, ItemDefinition>();
		for (int i = 0; i < _items.Count; i++)
		{
			ItemDefinition definition = _items[i];
			if (definition == null || string.IsNullOrEmpty(definition.itemName))
			{
				continue;
			}

			if (_lookup.ContainsKey(definition.itemName))
			{
				continue;
			}

			_lookup[definition.itemName] = definition;
		}
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		_lookup = null;
	}
#endif
}


