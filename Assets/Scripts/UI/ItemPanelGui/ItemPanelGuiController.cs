using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ItemPanelGuiController : MonoBehaviour {
  
  public enum ItemPanelType {
    FOOD_ITEMS,
    OTHER_ITEMS,
  }

  [Header("Per Instance Config")]
  [SerializeField]
  private string itemPanelNameString;
  [SerializeField]
  private ItemPanelType itemPanelType;
  
  [Header("UI Elements")]
  [SerializeField]
  private RectTransform rectTransform;
  [SerializeField]
  private TextMeshProUGUI itemPanelNameTMP;
  [SerializeField]
  private TextMeshProUGUI itemPanelCountTMP;

  [Header("WorldSpace Simulation Elements")]
  [SerializeField]
  private Transform itemSpawnTransform;
  
  [Header("Prefabs")]
  [SerializeField]
  private GameObject itemPrefab;
  
  // Internal Backing Structures
  private List<Item> items;
  private Dictionary<Item, ItemPanelItemController> itemPanelUiItems = new Dictionary<Item, ItemPanelItemController>();
  
  private void Start() {
    itemPanelNameTMP.text = itemPanelNameString;
    items = itemPanelType switch {
      ItemPanelType.FOOD_ITEMS => DenSystemManager.Instance.FoodItemsInDen,
      ItemPanelType.OTHER_ITEMS => DenSystemManager.Instance.OtherItemsInDen,
      _ => new List<Item>()
    };
    
    // Subscribe to inventory change events instead of checking every frame
    if (DenSystemManager.Instance != null)
    {
      DenSystemManager.Instance.OnDenInventoryChanged += UpdateItemUI;
    }
    
    // Initial update
    UpdateItemUI();
  }
  
  private void OnDestroy()
  {
    // Unsubscribe from events
    if (DenSystemManager.Instance != null)
    {
      DenSystemManager.Instance.OnDenInventoryChanged -= UpdateItemUI;
    }
  }

  /// <summary>
  /// Updates the item panel UI when items change in the den inventory.
  /// Called via event subscription instead of every frame.
  /// </summary>
  private void UpdateItemUI() {
    if (items == null)
    {
      return;
    }
    
    itemPanelCountTMP.text = items.Count.ToString();
    
    foreach (Item item in items) {
      if (!itemPanelUiItems.ContainsKey(item)) {
        ItemPanelItemController newItem = Instantiate(itemPrefab, itemSpawnTransform).GetComponent<ItemPanelItemController>();
        newItem.Setup(item);
        itemPanelUiItems.Add(item, newItem);
      }
    }

    foreach (Item item in itemPanelUiItems.Keys.ToList()) {
      if (!items.Contains(item)) {
        Destroy(itemPanelUiItems[item].gameObject);
        itemPanelUiItems.Remove(item);
      }
    }
  }
  
  
}