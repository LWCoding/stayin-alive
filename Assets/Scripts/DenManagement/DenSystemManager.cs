using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class DenSystemManager : Singleton<DenSystemManager> {
  public struct DenInformation {
    public int denId;
    public int denWorkerPop;
    public Den denObject;
  }

  public static DenInformation ConstructDenInformation(Den den) {
    int denId = den.GridPosition.x * 10000 + den.GridPosition.y;
    int denPopulation = den.WorkerCount();
    return new DenInformation { denId = denId, denWorkerPop = denPopulation, denObject = den };
  }
  
  public int denPrice;
  public int workerPrice;
  
  public GameObject FollowerAnimalPrefab;
  
  private Dictionary<int, DenInformation> validTeleports;
  
  private Dictionary<int, DenInformation> denInformations;

  private Dictionary<Animal, int> workersToDens;
  public Dictionary<Animal, int> WorkersToDens => workersToDens;
  
  private List<Animal> unassignedWorkers;

  [HideInInspector]
  public int UNASSIGNED_DEN_ID = -1;

  public bool CreateWorker() {
    
    GameObject newWorker = Instantiate(FollowerAnimalPrefab);
    Animal newWorkerAnimal = newWorker.GetComponent<Animal>();
    unassignedWorkers.Add(newWorkerAnimal);
    workersToDens[newWorkerAnimal] = -1;
    return true;
  }
  
  
  public bool AssignWorker(Animal animal, int denId) {
    ConstructDenInfos();
    // Make sure animal and den properly exist
    if (!workersToDens.ContainsKey(animal) || !denInformations.ContainsKey(denId)) {
      return false;
    }

    // If animal is currently assigned, fail
    if (workersToDens[animal] != UNASSIGNED_DEN_ID) {
      return false;
    }
    
    // First, remove from the unassigned worker list
    unassignedWorkers.Remove(animal);
    
    // Then, add it to the list for the other den
    denInformations[denId].denObject.AddWorker(animal);
    
    // Only then, update the map
    workersToDens[animal] = denId;

    return true;
  }

  public bool UnassignWorker(Animal animal) {
    ConstructDenInfos();
    if (!workersToDens.ContainsKey(animal) || !workersToDens.ContainsKey(animal)) {
      return false;
    }

    if (workersToDens[animal] == UNASSIGNED_DEN_ID) {
      return false;
    }
    
    // Then, remove it from list for the other den
    denInformations[workersToDens[animal]].denObject.RemoveWorker(animal);
    
    // Add it to the unassigned list
    unassignedWorkers.Add(animal);
    
    // Only then, unassign it in the map
    workersToDens[animal] = UNASSIGNED_DEN_ID;
    
    return true;
  }
  
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
    workersToDens = new Dictionary<Animal, int>();
    unassignedWorkers = new List<Animal>();
  }
  
  public void OpenPanel() {
    panelOpen = true;
    DenAdminMenu.Show();
    Debug.LogError("Panel Opened");
    ConstructValidDenTeleportInfos();
    DenAdminMenu.CreateDenMapIcons(ConstructDenInfos().Values.ToList());
    DenAdminMenu.SetupCurrentDenRenderTexture();
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

  public List<Den> GetDenList() {
    return InteractableManager.Instance.Dens;
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