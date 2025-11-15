using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script which allows an animal to purchase dens using "Readiness Points"
/// Deducts the amount set in the Den System Manager when successful
/// Temporarily, purchaseDen is called on 'B' press
/// </summary>
public class DenPurchaser : MonoBehaviour {
  [SerializeField]
  private ControllableAnimal playerAnimal;

  private Dictionary<int, DenSystemManager.DenInformation> validTeleports;

  private bool PanelOpen;

  public enum DenAdminActions {
    DEN_PURCHASE = 0,
    DEN_OPEN_ADMIN_PANEL = 1,
    DEN_CLOSE_ADMIN_PANEL = 2,
    DEN_GET_VALID_TELEPORTS = 3,
    DEN_TELEPORT = 4,
  }

  private List<DenAdminActions> GetViableActions() {
    List<DenAdminActions> actions = new List<DenAdminActions>();
    if (playerAnimal.CurrentDen == null) {
      actions.Add(DenAdminActions.DEN_PURCHASE);
      return actions;
    }
    
    if (!PanelOpen) {
      actions.Add(DenAdminActions.DEN_OPEN_ADMIN_PANEL);
    }
    else {
      actions.Add(DenAdminActions.DEN_CLOSE_ADMIN_PANEL);
    }
    
    actions.Add(DenAdminActions.DEN_GET_VALID_TELEPORTS);
    actions.Add(DenAdminActions.DEN_TELEPORT);

    return actions;
  }

  private void ExecuteAction(DenAdminActions action) {
    switch (action) {
      case DenAdminActions.DEN_PURCHASE:
        PurchaseDen();
        break;
      case DenAdminActions.DEN_OPEN_ADMIN_PANEL:
        break;
      case DenAdminActions.DEN_TELEPORT:
        
        break;
    }
  }

  private void Update() {
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
  }

  private void DenTeleport(int DenId) {
    if (validTeleports.ContainsKey(DenId)) {
      
    }
  }

  private List<DenSystemManager.DenInformation> GetValidDenTeleportDestinations() {
    List<DenSystemManager.DenInformation> validDestinations = new List<DenSystemManager.DenInformation>();

    // If player not in a den, return empty lists
    if (playerAnimal.CurrentDen == null) {
      return validDestinations;
    }

    List<DenSystemManager.DenInformation> destinations = DenSystemManager.Instance.GetDens();

    // Really inefficient way to do this, but remove den the player is in from the list of dens 
    foreach (DenSystemManager.DenInformation destination in destinations) {
      if (destination.denObject != playerAnimal.CurrentDen) {
        validDestinations.Add(destination);
      }
    }

    return validDestinations;
  }
}