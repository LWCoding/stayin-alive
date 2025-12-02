using UnityEngine;

/// <summary>
/// ScriptableObject that stores data for an item type.
/// Contains name and prefab reference.
/// </summary>
[CreateAssetMenu(fileName = "New Item Data", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    [Tooltip("Unique name identifier for this item type")]
    public string itemName;

    [Header("Prefab")]
    [Tooltip("Prefab to instantiate when spawning this item")]
    public GameObject prefab;
}

