using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for individual inventory slots. Manages the display of items in the inventory UI.
/// </summary>
public class InventorySlot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] [Tooltip("Image component that displays the item sprite. Should be a child GameObject named 'Item'.")]
    private Image _itemImage;
    
    [SerializeField] [Tooltip("GameObject that indicates this slot is selected. Should be a child GameObject named 'SelectedIndicator'.")]
    private GameObject _selectedIndicator;
    
    [SerializeField] [Tooltip("Text component that displays the slot number (1-9) for keyboard selection.")]
    private TextMeshProUGUI _slotNumberText;
    
    private string _itemName = null;
    private bool _isEmpty = true;
    private bool _isSelected = false;
    
    /// <summary>
    /// Whether this slot is currently empty.
    /// </summary>
    public bool IsEmpty => _isEmpty;
    
    /// <summary>
    /// The name of the item currently in this slot. Returns null if empty.
    /// </summary>
    public string ItemName => _itemName;
    
    /// <summary>
    /// Whether this slot is currently selected.
    /// </summary>
    public bool IsSelected => _isSelected;
    
    private void Awake()
    {
        if (_itemImage == null)
        {
            Debug.LogError($"InventorySlot '{name}': Item Image not found. Please assign it in the Inspector or ensure there's an Image component in a child GameObject named 'Item'.");
        }
        
        // Try to find SelectedIndicator if not assigned
        if (_selectedIndicator == null)
        {
            Transform indicatorTransform = transform.Find("SelectedIndicator");
            if (indicatorTransform != null)
            {
                _selectedIndicator = indicatorTransform.gameObject;
            }
            else
            {
                Debug.LogWarning($"InventorySlot '{name}': SelectedIndicator not found. Please assign it in the Inspector or ensure there's a child GameObject named 'SelectedIndicator'.");
            }
        }
        
        // Initialize as empty and deselected
        ClearSlot();
        SetSelected(false);
    }
    
    /// <summary>
    /// Sets the slot number to display (1-9). This corresponds to the keyboard key needed to select this slot.
    /// </summary>
    public void SetSlotNumber(int slotNumber)
    {
        if (_slotNumberText != null)
        {
            // slotNumber should be 1-9 (display value), not 0-8 (index)
            _slotNumberText.text = slotNumber.ToString();
        }
        else
        {
            Debug.LogWarning($"InventorySlot '{name}': Slot number text component not assigned. Cannot display slot number.");
        }
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
    
    /// <summary>
    /// Sets the selection state of this slot. Shows/hides the SelectedIndicator accordingly.
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        
        if (_selectedIndicator != null)
        {
            _selectedIndicator.SetActive(selected);
        }
    }

    public Item GetItem() {
      return ItemManager.Instance.GetItemFromName(_itemName);
    }
}

