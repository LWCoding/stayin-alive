using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class DenSystemManager : Singleton<DenSystemManager> {
  /// <summary>
  /// Event fired when the den management panel is opened.
  /// </summary>
  public event Action OnPanelOpened;
  
  /// <summary>
  /// Event fired when the den management panel is closed.
  /// </summary>
  public event Action OnPanelClosed;
  
  /// <summary>
  /// Event fired when the player teleports between dens.
  /// </summary>
  public event Action OnPlayerTeleported;
  
  /// <summary>
  /// Event fired when the player deposits items to a den.
  /// </summary>
  public event Action OnItemsDeposited;
  
  /// <summary>
  /// Event fired when a worker is created (purchased/bred).
  /// </summary>
  public event Action OnWorkerCreated;
  
  /// <summary>
  /// Event fired when a worker is assigned to a den.
  /// </summary>
  public event Action OnWorkerAssigned;
  
  /// <summary>
  /// Notifies subscribers that the player has teleported between dens.
  /// Called from DenAdministrator when teleport occurs.
  /// </summary>
  public void NotifyPlayerTeleported()
  {
    OnPlayerTeleported?.Invoke();
  }

  public void WorkerCreatedInvoke() {
    OnWorkerCreated?.Invoke();
  }
  
  public struct DenInformation {
    public int denId;
    public Den denObject;
  }

  public static DenInformation ConstructDenInformation(Den den) {
    int denId = den.DenId;
    return new DenInformation { denId = denId, denObject = den };
  }
  
  [Header("Worker Settings")]
  [Tooltip("AnimalData ScriptableObject that defines the worker animal type")]
  public AnimalData workerAnimalData;

  [Header("Den Resources")]
  [Tooltip("Food stored in dens at the start of a run.")]
  [SerializeField] private int startingDenFood = 0;

  private int storedDenFood;
  
  public int FoodInDen => foodItemsInDen.Count;
  
  private Dictionary<int, DenInformation> validTeleports;
  
  private Dictionary<int, DenInformation> denInformations;
  
  public int DensBuiltWithSticks => ConstructDenInfos().Count - 1;
  
  // Both lists treated as FiFo
  private List<Item> foodItemsInDen;
  private List<Item> otherItemsInDen;
  
  public List<Item> FoodItemsInDen => foodItemsInDen;
  public List<Item> OtherItemsInDen => otherItemsInDen;
  
  public int NumFoodItems => foodItemsInDen.Count;
  public int NumOtherItems => otherItemsInDen.Count;
  
  public void AddItemToDenInventory(Item item) {
    if (item is FoodItem) {
      foodItemsInDen.Add(item);
    }
    else {
      Debug.LogWarning(item);
      Debug.LogWarning($"{item.ItemName} added to other items inventory");
      otherItemsInDen.Add(item);
    }
  }
  
  public bool SpendFoodFromDen(int amount)
  {
    if (amount <= 0)
    {
      return false;
    }

    if (NumFoodItems - amount < 0)
    {
      return false;
    }
    
    // Remove and destroy only the requested amount of food items
    int itemsToRemove = Mathf.Min(amount, foodItemsInDen.Count);
    for (int i = 0; i < itemsToRemove; i++) {
      int res = SpendFoodFromDen();
      if (res < 0) {
        return false;
      }
    }
    
    DenAdminMenu.UpdateGui();
    return true;
  }

  /// Return value is hunger of food spent
  public int SpendFoodFromDen() {
    if (NumFoodItems <= 0) {
      return -1;
    }

    int foodToRemoveIndex = -1;
    
    foreach (int hungerTier in ItemManager.Instance.HungerOrder) {
      foodToRemoveIndex = foodItemsInDen.FindIndex((item) => (item as FoodItem)?.HungerRestored == hungerTier);
      if (foodToRemoveIndex >= 0) {
        break;
      }
    }
    
    foodToRemoveIndex = Mathf.Max(0, foodToRemoveIndex);
    Item itemToDestroy = foodItemsInDen[foodToRemoveIndex];
    if (!foodItemsInDen.Remove(itemToDestroy)) {
      return -1;
    }
    int ret = (int)((itemToDestroy as FoodItem).HungerRestored);
    
    Destroy(itemToDestroy.gameObject);
    return ret;
  }

  // Transfer indexed food item
  public bool TransferFoodItemToPlayerByIndex(int index = 0) {
    if (foodItemsInDen.Count <= index) {
      return false;
    }
    
    // Get first item in food item list
    Item itemToTransfer = foodItemsInDen[index];
    
    // Attempt to add item to the inventory (pass the Item object directly)
    bool itemAdded = InventoryManager.Instance.AddItem(itemToTransfer);
    
    // If the item is not added successfully, shake the den inventory panel
    if (!itemAdded) {
      DenAdminMenu.ShakeInventoryPanel();
      return false;
    }
    
    // If it is, remove the ItemObject from the Den System food item list
    foodItemsInDen.Remove(itemToTransfer);
    Destroy(itemToTransfer.gameObject);
    DenAdminMenu.UpdateGui();
    return true;
  }
  
  // Transfer indexed food item
  public bool TransferOtherItemToPlayerByIndex(int index = 0) {
    if (otherItemsInDen.Count <= index) {
      return false;
    }
    
    // Get first item in food item list
    Item itemToTransfer = otherItemsInDen[index];
    
    // Attempt to add item to the inventory (pass the Item object directly)
    bool itemAdded = InventoryManager.Instance.AddItem(itemToTransfer);
    
    // If the item is not added successfully, shake the den inventory panel
    if (!itemAdded) {
      DenAdminMenu.ShakeInventoryPanel();
      return false;
    }
    
    // If it is, remove the ItemObject from the Den System food item list
    otherItemsInDen.Remove(itemToTransfer);
    Destroy(itemToTransfer.gameObject);
    DenAdminMenu.UpdateGui();
    return true;
  }

  // Find a copy of the desired item in the backing lists, transfer it to the player
  public bool TransferItemToPlayer(Item itemToTransfer) {
    bool ret;
    if (itemToTransfer is FoodItem) {
      int foodIndex = foodItemsInDen.FindIndex(item => item == itemToTransfer);
      ret = TransferFoodItemToPlayerByIndex(foodIndex);
      DenAdminMenu.UpdateGui();
      return ret;
    } 
    int otherIndex = otherItemsInDen.FindIndex(item => item == itemToTransfer);
    ret = TransferOtherItemToPlayerByIndex(otherIndex);
    DenAdminMenu.UpdateGui();
    return ret;
  }
  
  // If override amount is set, will clear out food and add grass foods to the passed amount
  public void ResetDenFood(int? overrideAmount = null)
  {
    int target = overrideAmount.HasValue ? overrideAmount.Value : startingDenFood;
    foodItemsInDen.Clear();
    if (target > 0) {
      for (int i = 0; i < target; i++) {
        AddItemToDenInventory(ItemManager.Instance.CreateItemForStorage(Globals.GRASS_ITEM_TYPE_FOR_WORKER_HARDCODE));
      }
    }
    
  }
  
  // Deposits Player Inventory into Den
  // Player must be in a den for this to work
  public void DepositAllPlayerItemsToDen() {
    if (CurrentAdminDen != null) {
      CurrentAdminDen.ProcessFoodDelivery(CurrentDenAdministrator.Animal);
      OnItemsDeposited?.Invoke();
    }
  }
  
  public void InvokeOnWorkerAssigned() {
    OnWorkerAssigned?.Invoke();
  }
  
  /// <summary>
  /// Notifies the player (ControllableAnimal) to update its follower count based on unassigned workers.
  /// </summary>
  public void NotifyPlayerUpdateFollowerCount() {
    if (AnimalManager.Instance != null) {
      ControllableAnimal player = AnimalManager.Instance.GetPlayer();
      if (player != null) {
        player.UpdateFollowerCount();
      }
    }
  }
  
  public Dictionary<int, DenInformation> GetValidTeleports => validTeleports;
  public Dictionary<int, DenInformation> DenInfos => denInformations;
  private bool panelOpen;
  
  public bool PanelOpen => panelOpen;
  
  private DenAdministrator currentDenAdministrator;
  
  public DenAdministrator CurrentDenAdministrator => currentDenAdministrator;
  
  public Den CurrentAdminDen => currentDenAdministrator.Animal.CurrentDen;
  public int CurrentAdminDenID => CurrentAdminDen.GetDenInfo().denId;
  
  [SerializeField]
  private DenAdminMenuGuiController denAdminMenu;
  public DenAdminMenuGuiController DenAdminMenu => denAdminMenu;
  
  [SerializeField]
  private LogHolderGuiController logHolder;
  
  public LogHolderGuiController LogHolder => logHolder;
  
  public void RegisterDenAdministrator(DenAdministrator administrator) {
    currentDenAdministrator = administrator;
  }

  protected override void Awake() {
    base.Awake();
    InitializeState();
  }

  private void InitializeState() {
    validTeleports ??= new Dictionary<int, DenInformation>();
    denInformations ??= new Dictionary<int, DenInformation>();
    foodItemsInDen ??= new List<Item>();
    otherItemsInDen ??= new List<Item>();
    ResetDenFood();
  }
  
  public void OpenPanel() {
    // Ensure knowledge menu is closed so the menus remain mutually exclusive
    if (UIManager.Instance != null) {
      UIManager.Instance.HideKnowledgePanelIfVisible();
    }
    
    panelOpen = true;
    DenAdminMenu.Show();
    Debug.LogWarning("Panel Opened");
    ConstructValidDenTeleportInfos();
    Debug.LogWarning(validTeleports);
    TimeManager.Instance.Pause();
    
    // Fire event for panel opened
    OnPanelOpened?.Invoke();
  }

  public void ClosePanel() {
    panelOpen = false;
    DenAdminMenu.Hide();
    Debug.LogWarning("Panel Closed");
    validTeleports.Clear();
    TimeManager.Instance.Resume();
    
    // Fire event for panel closed
    OnPanelClosed?.Invoke();
  }

  public List<Den> GetDenList() {
    return InteractableManager.Instance.Dens;
  }

  public List<DenInformation> GetDens() {
    List<Den> denList = GetDenList();
    List<DenInformation> denInfos = new List<DenInformation>();
    foreach (Den den in denList) {
      denInfos.Add(ConstructDenInformation(den));
      Debug.LogWarning(ConstructDenInformation(den).denId);
    }

    return denInfos;
  }

  public void ConstructValidDenTeleportInfos() {
    validTeleports.Clear();
    List<DenInformation> denInfos = GetValidDenTeleportDestinations();
    foreach (DenInformation denInfo in denInfos) {
      validTeleports.Add(denInfo.denId, denInfo);
    }
  }
  
  public Dictionary<int, DenInformation> ConstructDenInfos() {
    denInformations.Clear();
    List<DenInformation> denInfos = GetDens();
    foreach (DenInformation denInfo in denInfos) {
      denInformations.Add(denInfo.denId, denInfo);
    }
    return denInformations;
  }

  private List<DenInformation> GetValidDenTeleportDestinations() {
    List<DenInformation> validDestinations = new List<DenInformation>();

    // If player not in a den, return empty lists
    if (currentDenAdministrator.Animal.CurrentDen == null) {
      return validDestinations;
    }

    List<DenInformation> destinations = GetDens();
    // Really inefficient way to do this, but remove den the player is in from the list of dens 
    foreach (DenInformation destination in destinations) {
      if (destination.denObject != currentDenAdministrator.Animal.CurrentDen) {
        validDestinations.Add(destination);
        Debug.LogWarning(destination.denId.ToString());
      }
    }

    return validDestinations;
  }
}