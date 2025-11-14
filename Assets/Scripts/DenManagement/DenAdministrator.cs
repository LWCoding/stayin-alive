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
  private Animal playerAnimal;
  
  // private PointsManager pointsManager;

  private void Update() {
    if (Input.GetKeyDown(KeyCode.B)) {
      purchaseDen();
    }
  }
  
  private void purchaseDen() {

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
}
