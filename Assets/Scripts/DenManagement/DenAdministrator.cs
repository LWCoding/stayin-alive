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

  // private PointsManager pointsManager;

  public enum DenAdminActions {
    DEN_PURCHASE,
    DEN_TELEPORT,
  }
  
  private void Update() {
    if (Input.GetKeyDown(KeyCode.B)) {
      PurchaseDen();
    }
  }

  private void PurchaseDen() {
    if (PointsManager.Instance.ReadinessPoints < DenSystemManager.Instance.denPrice) {
      Debug.Log("Not Enough Food To Make Den");
    }

    Den newDen = InteractableManager.Instance.SpawnDen(playerAnimal.GridPosition);

    if (newDen == null) {
      Debug.Log("Cannot make den at specified location");
    }

    // Worst case if something is out of sync, error will be in player's favor
    PointsManager.Instance.AddPoints(-1 * DenSystemManager.Instance.denPrice, true);
  }

  private void DenTeleport() {
    
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