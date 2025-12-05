using UnityEngine;

/// <summary>
/// ScriptableObject that stores data for an item type.
/// Contains item type enum and prefab reference.
/// </summary>
[CreateAssetMenu(fileName = "New Item Data", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    [Tooltip("The ItemType enum value this ItemData represents")]
    public ItemType itemType;

    [Tooltip("Name of the item for this item type")]
    public string itemName;

    [Header("Prefab")]
    [Tooltip("Prefab to instantiate when spawning this item")]
    public GameObject prefab;
}

