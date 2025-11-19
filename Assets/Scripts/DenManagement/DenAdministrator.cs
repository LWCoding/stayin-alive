using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Script which allows an animal to purchase dens using "Readiness Points"
/// Deducts the amount set in the Den System Manager when successful
/// </summary>
public class DenAdministrator : MonoBehaviour {
  [SerializeField]
  private ControllableAnimal playerAnimal;

  public ControllableAnimal Animal => playerAnimal;

  public static bool isInstantiated = false;

  public enum DenAdminActions {
    DEN_PURCHASE = 0,
    DEN_OPEN_ADMIN_PANEL = 1,
    DEN_CLOSE_ADMIN_PANEL = 2,
    DEN_GET_VALID_TELEPORTS = 3,
    DEN_TELEPORT = 4,
  }

  // private List<DenAdminActions> GetViableActions() {
  //   List<DenAdminActions> actions = new List<DenAdminActions>();
  //   if (playerAnimal.CurrentDen == null) {
  //     
  //     actions.Add(DenAdminActions.DEN_PURCHASE);
  //     return actions;
  //   }
  //   
  //   if (!PanelOpen) {
  //     actions.Add(DenAdminActions.DEN_OPEN_ADMIN_PANEL);
  //     return actions;
  //   }
  //   
  //   actions.Add(DenAdminActions.DEN_CLOSE_ADMIN_PANEL);
  //   
  //   if (validTeleports.Count <= 0) {
  //     actions.Add(DenAdminActions.DEN_GET_VALID_TELEPORTS);
  //   }
  //
  //   if (validTeleports.Count > 0) {
  //     actions.Add(DenAdminActions.DEN_TELEPORT);
  //   }
  //   
  //   
  //   return actions;
  // }

  // private void ExecuteAction(DenAdminActions action) {
  //   switch (action) {
  //     case DenAdminActions.DEN_PURCHASE:
  //       PurchaseDen();
  //       break;
  //     case DenAdminActions.DEN_OPEN_ADMIN_PANEL:
  //       break;
  //     case DenAdminActions.DEN_TELEPORT:
  //       break;
  //   }
  // }

  
  
  private void Awake() {
    if (isInstantiated) Component.Destroy(this);
    isInstantiated = true;
  }

  private void Start() {
    DenSystemManager.Instance.RegisterDenAdministrator(this);
  }

  private void OnDestroy() {
    isInstantiated = false;
  }

  private void Update() {
    if (Input.GetKeyDown(KeyCode.E)) {
      // Only allow opening panel if player is inside a den
      if (!DenSystemManager.Instance.PanelOpen) {
        if (playerAnimal.CurrentDen != null) {
          DenSystemManager.Instance.OpenPanel();
        }
      }
      else {
        DenSystemManager.Instance.ClosePanel();
      }
    }

    if (Input.GetKeyDown(KeyCode.B)) {
      PurchaseDen();
    }
  }

  private void PurchaseDen() {
    if (PointsManager.Instance.ReadinessPoints < DenSystemManager.Instance.denPrice) {
      Debug.Log("Not Enough Food To Make Den");
      return;
    }

    Den newDen = InteractableManager.Instance.SpawnDen(playerAnimal.GridPosition);

    if (newDen == null) {
      Debug.Log("Cannot make den at specified location");
      return;
    }

    // Worst case if something is out of sync, error will be in player's favor
    PointsManager.Instance.AddPoints(-1 * DenSystemManager.Instance.denPrice, true);
    
    // Automatically enter the den after building it
    newDen.OnAnimalEnter(playerAnimal);
  }
  
  public void PurchaseWorker() {
    if (PointsManager.Instance.ReadinessPoints < DenSystemManager.Instance.workerPrice) {
      Debug.Log("Not Enough Food To Make Worker");
      return;
    }

    DenSystemManager.Instance.CreateWorker();
    
    // Worst case if something is out of sync, error will be in player's favor
    PointsManager.Instance.AddPoints(-1 * DenSystemManager.Instance.workerPrice, true);
  }

  public void DenTeleport(int DenId) {
    Debug.LogWarning(DenId);
    if (DenSystemManager.Instance.GetValidTeleports.ContainsKey(DenId)) {
      // playerAnimal.CurrentDen.OnAnimalLeave(playerAnimal);
      
      playerAnimal.SetGridPosition(DenSystemManager.Instance.GetValidTeleports[DenId].denObject.GridPosition);
      Debug.LogWarning("Teleported");

      if (DenSystemManager.Instance.PanelOpen) {
        DenSystemManager.Instance.ConstructDenInfos();
        DenSystemManager.Instance.ConstructValidDenTeleportInfos();
        DenSystemManager.Instance.DenAdminMenu.CreateDenMapIcons(DenSystemManager.Instance.DenInfos.Values.ToList());
        DenSystemManager.Instance.DenAdminMenu.SetupCurrentDenRenderTexture();
        DenSystemManager.Instance.DenAdminMenu.SetupCurrentDenWorkers();
        DenSystemManager.Instance.DenAdminMenu.UpdatePurchaseWorkerButton();
      }
    }

    return;
  }
}