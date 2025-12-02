using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    List<string> itemNames = new List<string>();
    foreach (InventorySlot slot in InventoryManager.Instance.GetInventorySlots()) {
      Item item = slot.GetItem();
      if (item != null && item.ItemName != null) {
        itemNames.Add(item.ItemName);
      }
    }
    itemNames.Reverse();

    foreach (string itemName in itemNames) {
      DenMenuInventorySlotGui newSlot = Instantiate(inventorySlotPrefab, inventoryBar).GetComponent<DenMenuInventorySlotGui>();
      newSlot.Setup(itemName);
      inventorySlots.Add(newSlot);
    }
  }
}