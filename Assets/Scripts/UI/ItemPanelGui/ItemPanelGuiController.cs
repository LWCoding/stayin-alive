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
  private List<GlobalInventoryManager.InventoryItem> items;
  private List<GlobalInventoryManager.InventoryItem> nonDisplayedItems;
  private Dictionary<GlobalInventoryManager.InventoryItem, ItemPanelItemController> displayedItemPanelUiItems = new Dictionary<GlobalInventoryManager.InventoryItem, ItemPanelItemController>();

  private int renderCap = 60;
  
  private void Start() {
    itemPanelNameTMP.text = itemPanelNameString;
    items = itemPanelType switch {
      ItemPanelType.FOOD_ITEMS => GlobalInventoryManager.Instance.FoodItemsInDen,
      ItemPanelType.OTHER_ITEMS => GlobalInventoryManager.Instance.OtherItemsInDen,
      _ => new List<GlobalInventoryManager.InventoryItem>()
    };
    nonDisplayedItems = new List<GlobalInventoryManager.InventoryItem>();
  }

  private void Update() {
    itemPanelCountTMP.text = items.Count.ToString();
    
    foreach (GlobalInventoryManager.InventoryItem inventoryItem in items) {
      if (!displayedItemPanelUiItems.ContainsKey(inventoryItem)) {
        // Not already rendered, and we are over the cap
        if (displayedItemPanelUiItems.Count >= renderCap) {
          if (!nonDisplayedItems.Contains(inventoryItem)) {
            nonDisplayedItems.Add(inventoryItem);
          }
        }
        // Not already rendered and we are under the cap
        else {
          if (nonDisplayedItems.Contains(inventoryItem)) {
            nonDisplayedItems.Remove(inventoryItem);
          }
          ItemPanelItemController newItem =
            Instantiate(itemPrefab, itemSpawnTransform).GetComponent<ItemPanelItemController>();
          newItem.Setup(inventoryItem);
          displayedItemPanelUiItems.Add(inventoryItem, newItem);
        }
      }
    }

    foreach (GlobalInventoryManager.InventoryItem inventoryItem in displayedItemPanelUiItems.Keys.ToList()) {
      if (!items.Contains(inventoryItem)) {
        Destroy(displayedItemPanelUiItems[inventoryItem].gameObject);
        displayedItemPanelUiItems.Remove(inventoryItem);
      }
    }
  }
  
  
}