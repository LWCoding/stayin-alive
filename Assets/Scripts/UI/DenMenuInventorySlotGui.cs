using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DenMenuInventorySlotGui : MonoBehaviour {
  [SerializeField]
  private Image itemImage;
  
  [Header("Shake Settings")]
  [SerializeField] [Tooltip("Intensity of the shake")]
  private float _shakeIntensity = 10f;
  
  [SerializeField] [Tooltip("Duration of the shake animation in seconds")]
  private float _shakeDuration = 0.5f;
  
  // Track shake coroutine
  private Coroutine _shakeCoroutine;
  private Vector3 _originalLocalPosition;
  
  // private int inventoryIndex

  private void Awake() {
    // Store original position for shake animation
    _originalLocalPosition = transform.localPosition;
  }

  public void Setup(string itemName) {
    // Convert string to ItemType enum
    if (System.Enum.TryParse<ItemType>(itemName, out ItemType itemType))
    {
      Sprite itemSprite = ItemManager.Instance.GetItemSprite(itemType);
      itemImage.sprite = itemSprite;
    }
    else
    {
      Debug.LogWarning($"DenMenuInventorySlotGui: Could not parse item name '{itemName}' as ItemType.");
    }
  }
  
  /// <summary>
  /// Shakes this slot to provide visual feedback.
  /// </summary>
  public void Shake() {
    // Stop any existing shake coroutine
    if (_shakeCoroutine != null) {
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
  private IEnumerator ShakeCoroutine() {
    float elapsed = 0f;
    
    while (elapsed < _shakeDuration) {
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