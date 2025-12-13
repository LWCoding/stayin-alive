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
  
  private Dictionary<int, DenInformation> validTeleports;
  
  private Dictionary<int, DenInformation> denInformations;
  
  public int DensBuiltWithSticks => ConstructDenInfos().Count - 1;
  
  // This should be fine since it doesn't reference any of the new inv stuff
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
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
    Debug.unityLogger.logEnabled = false;
#endif
    InitializeState();
  }

  private void InitializeState() {
    validTeleports ??= new Dictionary<int, DenInformation>();
    denInformations ??= new Dictionary<int, DenInformation>();
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
    
    // Resume TimeManager, but only if it's not waiting for first move
    // (TimeManager handles its own first move pause state)
    if (TimeManager.Instance != null && !TimeManager.Instance.IsWaitingForFirstMove)
    {
      TimeManager.Instance.Resume();
    }
    
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