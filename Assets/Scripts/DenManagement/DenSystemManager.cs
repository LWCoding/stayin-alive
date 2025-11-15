using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class DenSystemManager : Singleton<DenSystemManager> {
  public struct DenInformation {
    public int denId;
    public int denPopulation;
    public Den denObject;
  }

  public static DenInformation ConstructDenInformation(Den den) {
    int denId = den.GridPosition.x * 10000 + den.GridPosition.y;
    int denPopulation = den.NumberAnimalsInDen();
    return new DenInformation { denId = denId, denPopulation = denPopulation, denObject = den };
  }


  private Dictionary<int, DenInformation> validTeleports;
  
  public Dictionary<int, DenInformation> GetValidTeleports => validTeleports;
  

  private bool panelOpen;
  
  public bool PanelOpen => panelOpen;

  private void Start() {
    validTeleports = new Dictionary<int, DenInformation>();
  }
  
  public void OpenPanel(DenAdministrator denAdmin) {
    panelOpen = true;
    Debug.LogError("Panel Opened");
    ConstructValidDenTeleportInfos(denAdmin.Animal);
    Debug.LogError(validTeleports);
  }

  public void ClosePanel(DenAdministrator denAdmin) {
    panelOpen = false;
    Debug.LogError("Panel Closed");
    validTeleports.Clear();
  }


  public int denPrice;

  public List<Den> GetDenList() {
    return InteractableManager.Instance.GetAllDens();
  }

  public List<DenInformation> GetDens() {
    List<Den> denList = GetDenList();
    List<DenInformation> denInfos = new List<DenInformation>();
    foreach (Den den in denList) {
      denInfos.Add(ConstructDenInformation(den));
      Debug.LogError(ConstructDenInformation(den).denId);
    }

    return denInfos;
  }

  public void ConstructValidDenTeleportInfos(ControllableAnimal playerAnimal) {
    validTeleports.Clear();
    List<DenInformation> denInfos = GetValidDenTeleportDestinations(playerAnimal);
    foreach (DenInformation denInfo in denInfos) {
      validTeleports.Add(denInfo.denId, denInfo);
    }
  }

  public List<DenInformation> GetValidDenTeleportDestinations(ControllableAnimal playerAnimal) {
    List<DenInformation> validDestinations = new List<DenInformation>();

    // If player not in a den, return empty lists
    if (playerAnimal.CurrentDen == null) {
      return validDestinations;
    }

    List<DenInformation> destinations = GetDens();
    // Really inefficient way to do this, but remove den the player is in from the list of dens 
    foreach (DenInformation destination in destinations) {
      if (destination.denObject != playerAnimal.CurrentDen) {
        validDestinations.Add(destination);
        Debug.LogError(destination.denId.ToString());
      }
    }

    return validDestinations;
  }
}