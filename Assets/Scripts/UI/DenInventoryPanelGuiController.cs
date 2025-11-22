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

    List<Item> invItems = InventoryManager.Instance.GetInventoryItems().ToList();
    invItems.Reverse();

    foreach (Item invItem in invItems) {
      DenMenuInventorySlotGui newSlot = Instantiate(inventorySlotPrefab, inventoryBar).GetComponent<DenMenuInventorySlotGui>();
      newSlot.Setup(invItem.ItemName);
      inventorySlots.Add(newSlot);
      Destroy(invItem.gameObject);
    }
  }
}