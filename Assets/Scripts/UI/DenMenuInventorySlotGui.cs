using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DenMenuInventorySlotGui : MonoBehaviour {
  [SerializeField]
  private Image itemImage;
  
  // private int inventoryIndex

  public void Setup(string itemName) {
    Sprite itemSprite = ItemManager.Instance.GetItemSprite(itemName);
    itemImage.sprite = itemSprite;
  }
}