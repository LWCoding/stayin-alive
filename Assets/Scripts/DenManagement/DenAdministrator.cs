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
      // Check if knowledge menu is visible - if so, don't handle E key here
      // (the knowledge menu will handle closing itself)
      if (UIManager.Instance != null && UIManager.Instance.IsKnowledgePanelVisible()) {
        return;
      }
      
      // If panel is open, close it
      if (DenSystemManager.Instance.PanelOpen) {
        // Don't allow closing panel if tutorial UI is active
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsDenUITutorialActive) {
          return;
        }
        DenSystemManager.Instance.ClosePanel();
      }
      // Otherwise, try to open panel if player is inside a den
      else if (playerAnimal.CurrentDen != null) {
        // In tutorial, require 1 den built (not counting starting den) before allowing panel to open
        if (TutorialManager.Instance != null && DenSystemManager.Instance != null) {
          if (DenSystemManager.Instance.DensBuiltWithSticks < 1) {
            // In tutorial and haven't built den yet, transfer items instead
            DenSystemManager.Instance.TransferOtherItemToPlayerByIndex();
            return;
          }
        }
        
        // Don't allow opening panel if worker log explanation is showing
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsWorkerLogExplanationActive) {
          return;
        }
        
        DenSystemManager.Instance.OpenPanel();
      }
      // If panel is closed and player is not in den, transfer items
      else {
        DenSystemManager.Instance.TransferOtherItemToPlayerByIndex();
      }
    }

    if (Input.GetKeyDown(KeyCode.Z)) {
      DenSystemManager.Instance.TransferFoodItemToPlayerByIndex();
    }

    if (Input.GetKeyDown(KeyCode.C)) {
      DenSystemManager.Instance.DepositAllPlayerItemsToDen();
    }
  }
  
  public void PurchaseWorker() {
    int currentWorkerPrice = WorkerManager.Instance.CurrentWorkerPrice;
    if (DenSystemManager.Instance.FoodInDen < currentWorkerPrice) {
      Debug.Log("Not Enough Food To Make Worker");
      return;
    }
    
    WorkerManager.WorkerOperationResult result = WorkerManager.Instance.CreateWorker();
    
    if (result != WorkerManager.WorkerOperationResult.WORKER_CREATED) {
      return;
    }
    
    DenSystemManager.Instance.SpendFoodFromDen(currentWorkerPrice);
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
        DenSystemManager.Instance.DenAdminMenu.UpdateGui();
      }
      
      // Notify that player has teleported
      DenSystemManager.Instance.NotifyPlayerTeleported();
    }

    return;
  }
}