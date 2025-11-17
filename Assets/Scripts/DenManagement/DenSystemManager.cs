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
  
  [Header("Worker Settings")]
  [Tooltip("AnimalData ScriptableObject that defines the worker animal type")]
  public AnimalData workerAnimalData;
  
  private Dictionary<int, DenInformation> validTeleports;
  
  private Dictionary<int, DenInformation> denInformations;

  private Dictionary<Animal, int> workersToDens;
  public Dictionary<Animal, int> WorkersToDens => workersToDens;
  
  private List<Animal> unassignedWorkers;

  [HideInInspector]
  public int UNASSIGNED_DEN_ID = -1;

  public bool CreateWorker() {
    if (workerAnimalData == null) {
      Debug.LogError("DenSystemManager: Worker AnimalData is not assigned! Please assign a worker AnimalData in the Inspector.");
      return false;
    }
    
    if (AnimalManager.Instance == null) {
      Debug.LogError("DenSystemManager: AnimalManager instance not found!");
      return false;
    }
    
    // Determine spawn position - use current den position if player is in a den
    Vector2Int spawnPosition = Vector2Int.zero;
    if (currentDenAdministrator != null && CurrentAdminDen != null) {
      spawnPosition = CurrentAdminDen.GridPosition;
    }
    
    // Spawn worker at the determined position
    // Workers start hidden (unassigned state)
    Animal newWorkerAnimal = AnimalManager.Instance.SpawnAnimal(
      workerAnimalData.animalName, 
      spawnPosition, 
      1
    );
    
    if (newWorkerAnimal == null) {
      Debug.LogError("DenSystemManager: Failed to spawn worker animal!");
      return false;
    }
    
    // Hide the worker since they're unassigned
    // Workers will only show up at dens when explicitly assigned
    newWorkerAnimal.SetVisualVisibility(false);
    
    unassignedWorkers.Add(newWorkerAnimal);
    workersToDens[newWorkerAnimal] = UNASSIGNED_DEN_ID;
    
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
    
    // Get the den object
    Den targetDen = denInformations[denId].denObject;
    
    animal.SetHome(targetDen);
    animal.SetGridPosition(targetDen.GridPosition);
  
    // Make the worker visible
    animal.SetVisualVisibility(true);
    
    // Add it to the den's worker list
    targetDen.AddWorker(animal);
    
    // Update the mapping
    workersToDens[animal] = denId;
    
    Debug.Log($"Worker '{animal.name}' assigned to den at ({targetDen.GridPosition.x}, {targetDen.GridPosition.y})");

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
    
    // Remove it from the den's worker list
    denInformations[workersToDens[animal]].denObject.RemoveWorker(animal);
    
    // Clear the worker's home reference
    animal.ClearHome();
    Debug.Log($"Worker '{animal.name}' unassigned and home cleared.");
    
    // Hide the worker since they're now unassigned
    animal.SetVisualVisibility(false);
    
    // Add it to the unassigned list
    unassignedWorkers.Add(animal);
    
    // Update the mapping to unassigned
    workersToDens[animal] = UNASSIGNED_DEN_ID;
    
    return true;
  }
  
  /// <summary>
  /// Handles cleanup when a worker dies. Removes the worker from their assigned den (if any)
  /// and from the worker tracking system.
  /// </summary>
  public void OnWorkerDeath(Animal animal) {
    // Check if this animal is a tracked worker
    if (!workersToDens.ContainsKey(animal)) {
      return;
    }
    
    int assignedDenId = workersToDens[animal];
    
    // If the worker was assigned to a den, remove them from that den's worker list
    if (assignedDenId != UNASSIGNED_DEN_ID) {
      ConstructDenInfos();
      if (denInformations.ContainsKey(assignedDenId)) {
        denInformations[assignedDenId].denObject.RemoveWorker(animal);
        Debug.Log($"Worker '{animal.name}' died and was removed from den at ID {assignedDenId}.");
      }
    } else {
      // Worker was unassigned, remove from unassigned list
      unassignedWorkers.Remove(animal);
      Debug.Log($"Unassigned worker '{animal.name}' died and was removed from unassigned list.");
    }
    
    // Remove from the worker tracking dictionary
    workersToDens.Remove(animal);
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