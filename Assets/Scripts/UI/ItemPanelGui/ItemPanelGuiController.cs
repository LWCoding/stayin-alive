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
  private Dictionary<GlobalInventoryManager.InventoryItem, ItemPanelItemController> itemPanelUiItems = new Dictionary<GlobalInventoryManager.InventoryItem, ItemPanelItemController>();
  
  private void Start() {
    itemPanelNameTMP.text = itemPanelNameString;
    items = itemPanelType switch {
      ItemPanelType.FOOD_ITEMS => GlobalInventoryManager.Instance.FoodItemsInDen,
      ItemPanelType.OTHER_ITEMS => GlobalInventoryManager.Instance.OtherItemsInDen,
      _ => new List<GlobalInventoryManager.InventoryItem>()
    };
  }

  private void Update() {
    itemPanelCountTMP.text = items.Count.ToString();
    
    foreach (GlobalInventoryManager.InventoryItem inventoryItem in items) {
      if (!itemPanelUiItems.ContainsKey(inventoryItem)) {
        ItemPanelItemController newItem = Instantiate(itemPrefab, itemSpawnTransform).GetComponent<ItemPanelItemController>();
        newItem.Setup(inventoryItem);
        itemPanelUiItems.Add(inventoryItem, newItem);
      }
    }

    foreach (GlobalInventoryManager.InventoryItem inventoryItem in itemPanelUiItems.Keys.ToList()) {
      if (!items.Contains(inventoryItem)) {
        Destroy(itemPanelUiItems[inventoryItem].gameObject);
        itemPanelUiItems.Remove(inventoryItem);
      }
    }
  }
  
  
}