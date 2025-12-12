using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InventoryOperationResult;

public enum InventoryOperationResult {
  UNEXPECTED_ERROR,
  FOOD_ADDED,
  OTHER_ADDED,
  INSUFFICIENT_FOOD,
  FOOD_SPENT,
  INVENTORY_FULL,
}

public class GlobalInventoryManager : Singleton<GlobalInventoryManager> {
  public struct InventoryItem : IEquatable<InventoryItem> {
    public ItemId id;
    public int count;

    public InventoryItem(ItemId id, int count) {
      this.id = id;
      this.count = count;
    }

    public bool Equals(InventoryItem other) {
      return id == other.id &&  count == other.count;
    }

    public override bool Equals(object obj) {
      return obj is InventoryItem && Equals((InventoryItem)obj);
    }

    public override int GetHashCode() {
      return (id, count).GetHashCode();
    }

    public static bool operator ==(InventoryItem lhs, InventoryItem rhs) {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(InventoryItem lhs, InventoryItem rhs) {
      return !lhs.Equals(rhs);
    }
  }
  
  private static int count = 0;
  
  private List<InventoryItem> foodItemsInDen;
  private List<InventoryItem> otherItemsInDen;
  
  public List<InventoryItem> FoodItemsInDen => foodItemsInDen;
  public List<InventoryItem> OtherItemsInDen => otherItemsInDen;
  
  public int FoodInDen => foodItemsInDen.Count;
  public int NumFoodItems => foodItemsInDen.Count;

  private int startingDenFood = 0;
  
  private DenAdminMenuGuiController denAdminMenu => DenSystemManager.Instance.DenAdminMenu;
  
  private static InventoryItem CreateInventoryItem(ItemId id) {
    return new InventoryItem(id, count++);
  }
  
  private static Item GetPrefabItemFromId(ItemId id) {
    ItemData itemData = ItemManager.Instance.GetItemData(id);
    return itemData.prefab.GetComponent<Item>();
  }

  protected override void Awake() {
    base.Awake();
    InitializeState();
  }
  
  private void InitializeState() {
    foodItemsInDen ??= new List<InventoryItem>();
    otherItemsInDen ??= new List<InventoryItem>();
  }
  
  public InventoryOperationResult AddItemIdToDen(ItemId id) {
    Item readOnlyItem = GetPrefabItemFromId(id);
    
    if (readOnlyItem == null) {
      return UNEXPECTED_ERROR;
    }
    
    if (readOnlyItem is FoodItem) {
      foodItemsInDen.Add(CreateInventoryItem(id));
      return FOOD_ADDED;
    }
    
    otherItemsInDen.Add(CreateInventoryItem(id));
    return OTHER_ADDED;
  }

  public InventoryOperationResult SpendFood(int amount) {
    if (amount <= 0) {
      return UNEXPECTED_ERROR;
    }

    if (NumFoodItems - amount < 0) {
      return INSUFFICIENT_FOOD;
    }

    int itemsToRemove = Mathf.Min(amount, NumFoodItems);

    for (int i = 0; i < itemsToRemove; i++) {
      int res = SpendSingleFood();
      if (res < 0) {
        return UNEXPECTED_ERROR;
      }
    }
    
    denAdminMenu.UpdateGui();
    return FOOD_SPENT;
  }

  /// Return value is hunger of food spend, -1 in error case
  public int SpendSingleFood() {
    if (NumFoodItems <= 0) {
      return -1;
    }

    int foodToRemoveIndex = -1;

    // Get index of a food item with the lowest hunger tier availible
    foreach (int hungerTier in ItemManager.Instance.HungerOrder) {
      foodToRemoveIndex = foodItemsInDen.FindIndex( 
        (invItem) => (GetPrefabItemFromId(invItem.id) as FoodItem)?.HungerRestored == hungerTier
      );
      if (foodToRemoveIndex >= 0) {
        break;
      }
    }

    foodToRemoveIndex = Mathf.Max(0, foodToRemoveIndex);
    int? ret = (GetPrefabItemFromId(foodItemsInDen[foodToRemoveIndex].id) as FoodItem)?.HungerRestored;
    foodItemsInDen.RemoveAt(foodToRemoveIndex);
    if (ret == null) {
      return -1;
    }

    return ret.Value;
  }
  
  private InventoryOperationResult TransferListToPlayer(InventoryItem invItem, List<InventoryItem> list) {
    if (!list.Contains(invItem)) {
      return UNEXPECTED_ERROR;
    }

    Item itemToTransfer = ItemManager.Instance.CreateItemForStorage(invItem.id);
    bool itemAdded = InventoryManager.Instance.AddItem(itemToTransfer);

    if (!itemAdded) {
      denAdminMenu.ShakeInventoryPanel();
      return INVENTORY_FULL;
    }
    
    list.Remove(invItem);
    Destroy(itemToTransfer.gameObject);
    denAdminMenu.UpdateGui();
    return FOOD_ADDED;
  }
  
  public InventoryOperationResult TransferFoodToPlayer(InventoryItem invItem) {
    return TransferListToPlayer(invItem, foodItemsInDen);
  }

  public InventoryOperationResult TransferOtherToPlayer(InventoryItem invItem) {
    return TransferListToPlayer(invItem, otherItemsInDen);
  }

  public InventoryOperationResult TransferItemToPlayer(InventoryItem inventoryItem) {
    if (foodItemsInDen.Contains(inventoryItem)) {
      return TransferFoodToPlayer(inventoryItem);
    }
    return TransferOtherToPlayer(inventoryItem);
  }

  public InventoryOperationResult PopListToPlayer(List<InventoryItem> list) {
    if (list.Count <= 0) {
      return UNEXPECTED_ERROR;
    }
    InventoryItem item = list[0];
    return TransferListToPlayer(item, list);
  }
  
  // If override amount is set, will clear out food and add grass foods to the passed amount
  public void ResetDenFood(int? overrideAmount = null) {
    int target = overrideAmount.HasValue ? overrideAmount.Value : startingDenFood;
    foodItemsInDen.Clear();
    if (target > 0) {
      for (int i = 0; i < target; i++) {
        AddItemIdToDen(Globals.GRASS_ITEM_TYPE_FOR_WORKER_HARDCODE);
      }
    }
  }
  
  // Deposits Player Inventory into Den
  // Player must be in a den for this to work
  // public void DepositAllPlayerItemsToDen() {
  //   if (CurrentAdminDen != null) {
  //     CurrentAdminDen.ProcessFoodDelivery(CurrentDenAdministrator.Animal);
  //     OnItemsDeposited?.Invoke();
  //   }
  // }
}