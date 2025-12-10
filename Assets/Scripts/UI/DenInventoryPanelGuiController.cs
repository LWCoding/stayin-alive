using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ItemId;

public class DenInventoryPanelGuiController : MonoBehaviour {
  [Header("UI Elements")]
  public RectTransform inventoryBar;
  public RectTransform foodPanel;
  public RectTransform itemPanel;
  public Button depositButton;
  
  [Header("Prefabs")]
  public GameObject inventorySlotPrefab;
  
  private List<DenMenuInventorySlotGui> inventorySlots = new List<DenMenuInventorySlotGui>();

  private void Start() {
    inventorySlots = new List<DenMenuInventorySlotGui>();
    
    depositButton.onClick.AddListener(DepositPlayerInventory);
  }

  public void DepositPlayerInventory() {
    DenSystemManager.Instance.DepositAllPlayerItemsToDen();
    DenSystemManager.Instance.DenAdminMenu.UpdateGui();
  }

  public void RefreshGui() {
    foreach (DenMenuInventorySlotGui inventorySlot in inventorySlots) {
      Destroy(inventorySlot.gameObject);
    } 
    inventorySlots.Clear();

    // Get item names from inventory slots without destroying the items
    // Items should only be destroyed when actually removed from inventory
    List<ItemId> itemIds = new List<ItemId>();
    foreach (InventorySlot slot in InventoryManager.Instance.GetInventorySlots()) {
      Item item = slot.GetItem();
      if (item != null && item.ItemName != null) {
        itemIds.Add(item.ItemType);
      }
    }
    itemIds.Reverse();

    foreach (ItemId itemId in itemIds) {
      DenMenuInventorySlotGui newSlot = Instantiate(inventorySlotPrefab, inventoryBar).GetComponent<DenMenuInventorySlotGui>();
      newSlot.Setup(itemId);
      inventorySlots.Add(newSlot);
    }
  }

  /// <summary>
  /// Shakes each inventory slot to provide visual feedback.
  /// </summary>
  public void Shake() {
    // Shake each slot independently
    foreach (DenMenuInventorySlotGui slot in inventorySlots) {
      if (slot != null && slot.gameObject != null) {
        slot.Shake();
      }
    }
  }
}