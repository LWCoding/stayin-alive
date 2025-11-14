using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component for individual inventory slots. Manages the display of items in the inventory UI.
/// </summary>
public class InventorySlot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] [Tooltip("Image component that displays the item sprite. Should be a child GameObject named 'Item'.")]
    private Image _itemImage;
    
    private string _itemName = null;
    private bool _isEmpty = true;
    
    /// <summary>
    /// Whether this slot is currently empty.
    /// </summary>
    public bool IsEmpty => _isEmpty;
    
    /// <summary>
    /// The name of the item currently in this slot. Returns null if empty.
    /// </summary>
    public string ItemName => _itemName;
    
    private void Awake()
    {
        if (_itemImage == null)
        {
            Debug.LogError($"InventorySlot '{name}': Item Image not found. Please assign it in the Inspector or ensure there's an Image component in a child GameObject named 'Item'.");
        }
        
        // Initialize as empty
        ClearSlot();
    }
    
    /// <summary>
    /// Sets the item in this slot. Returns true if successful, false if slot is already occupied.
    /// </summary>
    public bool SetItem(string itemName, Sprite itemSprite)
    {
        if (!_isEmpty)
        {
            Debug.LogWarning($"InventorySlot '{name}': Cannot set item '{itemName}' - slot is already occupied by '{_itemName}'.");
            return false;
        }
        
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning($"InventorySlot '{name}': Cannot set item with null or empty name.");
            return false;
        }
        
        _itemName = itemName;
        _isEmpty = false;
        
        // Update the image
        _itemImage.sprite = itemSprite;
        _itemImage.enabled = true;

        return true;
    }
    
    /// <summary>
    /// Clears the slot, removing the item and hiding the image.
    /// </summary>
    public void ClearSlot()
    {
        _itemName = null;
        _isEmpty = true;
        
        if (_itemImage != null)
        {
            _itemImage.sprite = null;
            _itemImage.enabled = false;
        }
    }
}

