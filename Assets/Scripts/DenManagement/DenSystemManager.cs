using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
  private Dictionary<int, DenInformation> denInformations;
  
  
  public Dictionary<int, DenInformation> GetValidTeleports => validTeleports;
  public Dictionary<int, DenInformation> DenInfos => denInformations;
  private bool panelOpen;
  
  public bool PanelOpen => panelOpen;
  
  private DenAdministrator currentDenAdministrator;
  
  public DenAdministrator CurrentDenAdministrator => currentDenAdministrator;
  
  public Den CurrentAdminDen => currentDenAdministrator.Animal.CurrentDen;
  
  [SerializeField]
  private DenAdminMenuGuiController denAdminMenu;
  public DenAdminMenuGuiController DenAdminMenu => denAdminMenu;
  
  public void RegisterDenAdministrator(DenAdministrator administrator) {
    currentDenAdministrator = administrator;
  }

  private void Start() {
    validTeleports = new Dictionary<int, DenInformation>();
    denInformations = new Dictionary<int, DenInformation>();
  }
  
  public void OpenPanel() {
    panelOpen = true;
    DenAdminMenu.Show();
    Debug.LogError("Panel Opened");
    ConstructValidDenTeleportInfos();
    DenAdminMenu.CreateDenMapIcons(ConstructDenInfos().Values.ToList());
    Debug.LogError(validTeleports);
    TimeManager.Instance.Pause();
  }

  public void ClosePanel() {
    panelOpen = false;
    DenAdminMenu.Hide();
    Debug.LogError("Panel Closed");
    validTeleports.Clear();
    TimeManager.Instance.Resume();
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
        Debug.LogError(destination.denId.ToString());
      }
    }

    return validDestinations;
  }
}