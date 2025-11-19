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

  [Header("Den Resources")]
  [Tooltip("Food stored in dens at the start of a run.")]
  [SerializeField] private int startingDenFood = 0;

  private int storedDenFood;
  public int FoodInDen => storedDenFood;
  
  private Dictionary<int, DenInformation> validTeleports;
  
  private Dictionary<int, DenInformation> denInformations;

  private Dictionary<Animal, int> workersToDens;
  public Dictionary<Animal, int> WorkersToDens => workersToDens;
  
  private List<Animal> unassignedWorkers;

  private int _densBuiltWithSticks;
  public int DensBuiltWithSticks => _densBuiltWithSticks;

  [HideInInspector]
  public int UNASSIGNED_DEN_ID = -1;
  
  private int _currentMvpPopulation = 0;
  private bool _hasInitializedMvpPopulation = false;
  private bool _hasTriggeredWin = false;
  
  /// <summary>
  /// Gets the current MVP population (controllable animal + living workers).
  /// </summary>
  public int CurrentMvpPopulation => _currentMvpPopulation;
  
  /// <summary>
  /// Initializes the MVP population using the player's follower count (followers + controllable animal).
  /// This should run once after the controllable animal has spawned.
  /// </summary>
  /// <param name="followerCountIncludingPlayer">Follower count including the controllable animal.</param>
  public void InitializeMvpPopulationFromFollowerCount(int followerCountIncludingPlayer)
  {
    if (_hasInitializedMvpPopulation)
    {
      return;
    }
    
    _hasInitializedMvpPopulation = true;
    SetMvpPopulation(followerCountIncludingPlayer);
  }
  
  private void IncrementMvpPopulation(int delta = 1)
  {
    if (delta <= 0)
    {
      return;
    }
    
    SetMvpPopulation(_currentMvpPopulation + delta);
  }
  
  private void DecrementMvpPopulation(int delta = 1)
  {
    if (delta <= 0)
    {
      return;
    }
    
    SetMvpPopulation(Mathf.Max(0, _currentMvpPopulation - delta));
  }
  
  private void SetMvpPopulation(int newValue)
  {
    _currentMvpPopulation = Mathf.Max(0, newValue);
    
    if (!_hasTriggeredWin && _currentMvpPopulation >= Globals.MvpWorkerGoal && GameManager.Instance != null)
    {
      _hasTriggeredWin = true;
      GameManager.Instance.TriggerWin();
    }
  }

  public bool AddFoodToDen(int amount)
  {
    if (amount <= 0)
    {
      return false;
    }

    storedDenFood += amount;
    return true;
  }

  public bool SpendFoodFromDen(int amount)
  {
    if (amount <= 0)
    {
      return false;
    }

    if (storedDenFood - amount < 0)
    {
      return false;
    }

    storedDenFood -= amount;
    return true;
  }

  public void ResetDenFood(int? overrideAmount = null)
  {
    int target = overrideAmount.HasValue ? overrideAmount.Value : startingDenFood;
    storedDenFood = Mathf.Max(0, target);
  }

  public bool CreateWorker() {
    Vector2Int spawnPosition = Vector2Int.zero;
    if (currentDenAdministrator != null && CurrentAdminDen != null) {
      spawnPosition = CurrentAdminDen.GridPosition;
    }
    return CreateWorkerAtPosition(spawnPosition);
  }
  
  public bool CreateWorkerAtPosition(Vector2Int spawnPosition) {
    if (workerAnimalData == null) {
      Debug.LogError("DenSystemManager: Worker AnimalData is not assigned! Please assign a worker AnimalData in the Inspector.");
      return false;
    }
    
    if (AnimalManager.Instance == null) {
      Debug.LogError("DenSystemManager: AnimalManager instance not found!");
      return false;
    }
    
    Animal newWorkerAnimal = AnimalManager.Instance.SpawnAnimal(
      workerAnimalData.animalName, 
      spawnPosition, 
      1
    );
    
    if (newWorkerAnimal == null) {
      Debug.LogError("DenSystemManager: Failed to spawn worker animal!");
      return false;
    }


    
    if (workerAnimalData.hungerThreshold > 0) {
      newWorkerAnimal.SetHunger(workerAnimalData.hungerThreshold - 1);
    }
    
    newWorkerAnimal.SetVisualVisibility(false);
    
    AddUnassignedWorker(newWorkerAnimal);
    IncrementMvpPopulation();
    
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
    
    // Get the den object
    Den targetDen = denInformations[denId].denObject;
    
    // Check if den is at maximum capacity
    if (targetDen.IsFull()) {
      Debug.LogWarning($"Cannot assign worker to den at ({targetDen.GridPosition.x}, {targetDen.GridPosition.y}) - den is at maximum capacity ({Globals.MaxWorkersPerDen} workers).");
      return false;
    }
    
    RemoveUnassignedWorker(animal);
    
    animal.SetHome(targetDen);
    
    // Use TeleportToGridPosition instead of SetGridPosition to immediately snap the worker
    // to the den location without animation. This prevents position sync issues when
    // reassigning workers that may have stale position data from before being unassigned.
    animal.TeleportToGridPosition(targetDen.GridPosition);
  
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
    
    // Add it to the unassigned list (this will also update mapping and notify player)
    AddUnassignedWorker(animal);
    
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
      RemoveUnassignedWorker(animal);
      Debug.Log($"Unassigned worker '{animal.name}' died and was removed from unassigned list.");
    }
    
    // Remove from the worker tracking dictionary
    workersToDens.Remove(animal);
    DecrementMvpPopulation();
  }
  
  /// <summary>
  /// Checks if the given animal is an unassigned worker (a worker that hasn't been assigned to a den yet).
  /// Unassigned workers should not execute turn logic or be visible.
  /// </summary>
  /// <param name="animal">The animal to check</param>
  /// <returns>True if the animal is an unassigned worker, false otherwise</returns>
  public bool IsUnassignedWorker(Animal animal) {
    if (animal == null || !workersToDens.ContainsKey(animal)) {
      return false;
    }
    
    return workersToDens[animal] == UNASSIGNED_DEN_ID;
  }
  
  /// <summary>
  /// Gets the current number of unassigned workers.
  /// </summary>
  /// <returns>The count of unassigned workers</returns>
  public int GetUnassignedWorkerCount() {
    if (unassignedWorkers == null) {
      return 0;
    }
    return unassignedWorkers.Count;
  }
  
  /// <summary>
  /// Consumes one unassigned worker to represent the controllable animal taking damage.
  /// This removes the worker from tracking, destroys the worker GameObject, and decreases MVP.
  /// </summary>
  /// <returns>True if a worker was consumed, false if no unassigned workers were available.</returns>
  public bool TryConsumeUnassignedWorkerForPlayerDamage()
  {
    if (unassignedWorkers == null || unassignedWorkers.Count == 0)
    {
      return false;
    }
    
    Animal workerToRemove = null;
    for (int i = unassignedWorkers.Count - 1; i >= 0; i--)
    {
      Animal candidate = unassignedWorkers[i];
      if (candidate == null)
      {
        unassignedWorkers.RemoveAt(i);
        continue;
      }
      
      workerToRemove = candidate;
      break;
    }
    
    if (workerToRemove == null)
    {
      return false;
    }
    
    workerToRemove.Die();
    return true;
  }
  
  /// <summary>
  /// Adds a worker to the unassigned workers list and notifies the player to update follower count.
  /// </summary>
  /// <param name="animal">The worker animal to add as unassigned</param>
  private void AddUnassignedWorker(Animal animal) {
    if (animal == null) {
      return;
    }
    
    unassignedWorkers.Add(animal);
    workersToDens[animal] = UNASSIGNED_DEN_ID;
    NotifyPlayerUpdateFollowerCount();
  }
  
  /// <summary>
  /// Removes a worker from the unassigned workers list and notifies the player to update follower count.
  /// </summary>
  /// <param name="animal">The worker animal to remove from unassigned</param>
  private void RemoveUnassignedWorker(Animal animal) {
    if (animal == null) {
      return;
    }
    
    unassignedWorkers.Remove(animal);
    NotifyPlayerUpdateFollowerCount();
  }
  
  /// <summary>
  /// Notifies the player (ControllableAnimal) to update its follower count based on unassigned workers.
  /// </summary>
  private void NotifyPlayerUpdateFollowerCount() {
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
  
  [SerializeField]
  private DenAdminMenuGuiController denAdminMenu;
  public DenAdminMenuGuiController DenAdminMenu => denAdminMenu;
  
  public void RegisterDenAdministrator(DenAdministrator administrator) {
    currentDenAdministrator = administrator;
  }

  protected override void Awake() {
    base.Awake();
    InitializeState();
  }

  private void Start()
  {
    ResetDensBuiltWithSticks();
  }

  private void InitializeState() {
    validTeleports ??= new Dictionary<int, DenInformation>();
    denInformations ??= new Dictionary<int, DenInformation>();
    workersToDens ??= new Dictionary<Animal, int>();
    unassignedWorkers ??= new List<Animal>();
    _currentMvpPopulation = 0;
    _hasInitializedMvpPopulation = false;
    _hasTriggeredWin = false;
    ResetDenFood();
  }

  public void ResetDensBuiltWithSticks()
  {
    _densBuiltWithSticks = 0;
  }

  public void IncrementDensBuiltWithSticks()
  {
    _densBuiltWithSticks++;
  }
  
  public void OpenPanel() {
    panelOpen = true;
    DenAdminMenu.Show();
    Debug.LogWarning("Panel Opened");
    ConstructValidDenTeleportInfos();
    DenAdminMenu.CreateDenMapIcons(ConstructDenInfos().Values.ToList());
    DenAdminMenu.SetupCurrentDenRenderTexture();
    Debug.LogWarning(validTeleports);
    TimeManager.Instance.Pause();
  }

  public void ClosePanel() {
    panelOpen = false;
    DenAdminMenu.Hide();
    Debug.LogWarning("Panel Closed");
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