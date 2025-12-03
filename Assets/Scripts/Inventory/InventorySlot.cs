using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for individual inventory slots. Manages the display of items in the inventory UI.
/// </summary>
public class InventorySlot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] [Tooltip("Image component that displays the item sprite.")]
    private Image _itemImage;
    
    [SerializeField] [Tooltip("GameObject that indicates this slot is selected.")]
    private GameObject _selectedIndicator;
    
    [SerializeField] [Tooltip("Text component that displays the slot number (1-9) for keyboard selection.")]
    private TextMeshProUGUI _slotNumberText;
    
    [Header("Shake Settings")]
    [SerializeField] [Tooltip("Intensity of the shake")]
    private float _shakeIntensity = 10f;
    
    [SerializeField] [Tooltip("Duration of the shake animation in seconds")]
    private float _shakeDuration = 0.5f;
    
    private Item _item = null;
    private bool _isEmpty = true;
    private bool _isSelected = false;
    
    // Track shake coroutine
    private Coroutine _shakeCoroutine;
    private Vector3 _originalLocalPosition;

    public Item GetItem() => _item;
    public bool IsEmpty => _isEmpty;
    public bool IsSelected => _isSelected;
    
    private void Awake()
    {
        if (_itemImage == null)
        {
            Debug.LogError($"InventorySlot '{name}': Item Image not found. Please assign it in the Inspector or ensure there's an Image component in a child GameObject named 'Item'.");
        }
        
        // Store original position for shake animation
        _originalLocalPosition = transform.localPosition;
        
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
    public bool SetItem(Item item)
    {
        if (!_isEmpty)
        {
            Debug.LogWarning($"InventorySlot '{name}': Cannot set item '{item?.ItemName}' - slot is already occupied by '{_item?.ItemName}'.");
            return false;
        }
        
        if (item == null)
        {
            Debug.LogWarning($"InventorySlot '{name}': Cannot set null item.");
            return false;
        }
        
        _item = item;
        _isEmpty = false;
        
        // Update the image
        _itemImage.sprite = item.InventorySprite;
        _itemImage.enabled = true;

        return true;
    }
    
    /// <summary>
    /// Clears the slot, destroying the item and hiding the image.
    /// </summary>
    public void ClearSlot()
    {
        // Destroy the item GameObject if it exists
        if (_item != null && _item.gameObject != null)
        {
            Destroy(_item.gameObject);
        }
        
        _item = null;
        _isEmpty = true;
        
        if (_itemImage != null)
        {
            _itemImage.sprite = null;
            _itemImage.enabled = false;
        }
    }
    
    /// <summary>
    /// Extracts the item from the slot without destroying it. Used when transferring items to another system (e.g., den inventory).
    /// </summary>
    /// <returns>The Item that was extracted, or null if slot was empty</returns>
    public Item ExtractItem()
    {
        Item item = _item;
        
        _item = null;
        _isEmpty = true;
        
        if (_itemImage != null)
        {
            _itemImage.sprite = null;
            _itemImage.enabled = false;
        }
        
        return item;
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
    
    /// <summary>
    /// Shakes this slot to provide visual feedback.
    /// </summary>
    public void Shake()
    {
        // Stop any existing shake coroutine
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            // Reset to original position before starting new shake
            transform.localPosition = _originalLocalPosition;
        }
        
        // Always capture the current position right before shaking to ensure we use the correct base position
        _originalLocalPosition = transform.localPosition;
        
        // Start shake coroutine
        _shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }
    
    /// <summary>
    /// Coroutine that shakes this slot.
    /// </summary>
    private IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < _shakeDuration)
        {
            // Calculate random offset for shake
            float offsetX = UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity);
            float offsetY = UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity);
            
            // Apply shake offset
            transform.localPosition = _originalLocalPosition + new Vector3(offsetX, offsetY, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset to original position
        transform.localPosition = _originalLocalPosition;
        _shakeCoroutine = null;
    }
}

