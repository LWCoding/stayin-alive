using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

public class WorkerManager : Singleton<WorkerManager> {
  public enum WorkerOperationResult {
    UNEXPECTED_ERROR,
    ADMIN_DEN_NOT_FOUND,
    
    // Operation Successful
    WORKER_CREATED,
    WORKER_ASSIGNED,
    WORKER_UNASSIGNED,
    WORKER_DESTROYED,
    
    // 
    DEN_FULL,
    UNASSIGNED_FULL,
  }

  public enum WorkerAssignmentStatus {
    ERROR,
    UNASSIGNED,
    ASSIGNED
  }
  
  [Header("Worker Settings")]
  [Tooltip("AnimalData ScriptableObject that defines the worker animal type")]
  public AnimalData workerAnimalData;

  public const int UNASSIGNED_DEN_ID = -1;
  public const int PLAYER_MVP_CONTRIBUTION = 1;
  private const int NO_DEN_ID_SET = -2;
  
  /// Maps from worker animal object to the Den ID of the worker
  private Dictionary<Animal, int> workersToDens = new Dictionary<Animal, int>();
  public Dictionary<Animal, int> WorkersToDens => workersToDens;

  private List<Animal> unassignedWorkers = new List<Animal>();
  private bool unassignedFull => unassignedWorkers.Count >= Globals.MaxWorkersUnassigned;

  private bool adminDenInitialized => DenSystemManager.Instance.CurrentDenAdministrator != null && DenSystemManager.Instance.CurrentAdminDen != null;
  
  private Den currentAdminDen => DenSystemManager.Instance.CurrentAdminDen;
  
  private DenSystemManager denSystemManager => DenSystemManager.Instance;

  public List<Animal> AllWorkers => workersToDens.Keys.ToList();
  
  public int CurrentMvpPopulation => workersToDens.Keys.Count + PLAYER_MVP_CONTRIBUTION;
  public int CurrentUnassignedPopulation => unassignedWorkers.Count;
  
  public int CurrentAssignedPopulation => CurrentMvpPopulation - CurrentUnassignedPopulation;
  
  public int CurrentAdminPopulation => adminDenInitialized ? currentAdminDen.WorkerCount() : 0;
  
  /// <summary>
  /// Calculates the current worker price based on the total number of workers.
  /// </summary>
  public int CurrentWorkerPrice => Mathf.FloorToInt(Mathf.Sqrt(workersToDens.Keys.Count));

  public float CurrentWorkerBonusFoodDropRate => ((float)CurrentAssignedPopulation) / ((float)Globals.MvpWorkerGoal);

  /// <summary>
  /// Get assignment status 
  /// </summary>
  /// <param name="animal"></param>
  /// <returns></returns>
  public WorkerAssignmentStatus GetWorkerAssignmentStatus(Animal animal) {
    if (animal == null || !workersToDens.ContainsKey(animal)) {
      return WorkerAssignmentStatus.ERROR;
    }

    return workersToDens[animal] == UNASSIGNED_DEN_ID
      ? WorkerAssignmentStatus.UNASSIGNED
      : WorkerAssignmentStatus.ASSIGNED;
  }
  
  /// Check if there is room to create a new worker in either the current den or unassigned
  public bool HaveRoomToCreateWorker() {
    if (!adminDenInitialized) {
      return false;
    }

    return !unassignedFull || !currentAdminDen.IsFull();
  }
  
  /// <summary>
  /// Creates new worker animal, adding it to the <see cref="currentAdminDen"/>, if the <see cref="currentAdminDen"/> is full, instead adds the animal to <see cref="unassignedWorkers"/>
  /// </summary>
  /// <param name="spawnPosition">spawnPosition to be passed into <see cref="AnimalManager.SpawnAnimal"/></param>
  /// <returns>
  ///   <see cref="WorkerOperationResult.WORKER_CREATED">WORKER_CREATED</see> <br/>
  ///   <see cref="WorkerOperationResult.UNASSIGNED_FULL">UNASSIGNED_FULL</see> - when <see cref="currentAdminDen"/> and <see cref="unassignedWorkers"/> are full <br/>
  ///   <see cref="WorkerOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see>
  /// </returns>
  public WorkerOperationResult CreateWorker(Vector2Int spawnPosition = default(Vector2Int)) {
    if (workerAnimalData == null) {
      Debug.LogError("DenSystemManager: Worker AnimalData is not assigned! Please assign a worker AnimalData in the Inspector.");
      return WorkerOperationResult.UNEXPECTED_ERROR;
    }
    
    if (AnimalManager.Instance == null) {
      Debug.LogError("DenSystemManager: AnimalManager instance not found!");
      return WorkerOperationResult.UNEXPECTED_ERROR;
    }
    
    Animal newWorkerAnimal = AnimalManager.Instance.SpawnAnimal(
      workerAnimalData.animalName, 
      spawnPosition, 
      1
    );
    
    if (newWorkerAnimal == null) {
      Debug.LogError("DenSystemManager: Failed to spawn worker animal!");
      return WorkerOperationResult.UNEXPECTED_ERROR;
    }
    
    if (workerAnimalData.hungerThreshold > 0) {
      newWorkerAnimal.SetHunger(workerAnimalData.hungerThreshold - 1);
    }

    WorkerOperationResult result = Assign(newWorkerAnimal);

    switch (result) {
      case WorkerOperationResult.UNEXPECTED_ERROR:
        Destroy(newWorkerAnimal.gameObject);
        return WorkerOperationResult.UNEXPECTED_ERROR;
      
      case WorkerOperationResult.ADMIN_DEN_NOT_FOUND:
      case WorkerOperationResult.DEN_FULL:
        result = Unassign(newWorkerAnimal);
        break;
    }

    switch (result) {
      case WorkerOperationResult.UNEXPECTED_ERROR:
      case WorkerOperationResult.UNASSIGNED_FULL:
        Destroy(newWorkerAnimal.gameObject);
        return WorkerOperationResult.UNASSIGNED_FULL;
      
      case WorkerOperationResult.WORKER_ASSIGNED:
      case WorkerOperationResult.WORKER_UNASSIGNED:
        if (CurrentMvpPopulation >= Globals.MvpWorkerGoal && GameManager.Instance != null) {
          GameManager.Instance.TriggerWin();
        }
        denSystemManager.WorkerCreatedInvoke();
        return WorkerOperationResult.WORKER_CREATED;
    }
    
    return WorkerOperationResult.UNEXPECTED_ERROR;
  }

  /// <summary>
  /// Adds an animal to <see cref="unassignedWorkers"/>, and removes its current assignment if it is assigned
  /// </summary>
  /// <param name="animal">The animal to be unassigned</param>
  /// <returns>
  ///   <see cref="WorkerOperationResult.WORKER_UNASSIGNED">WORKER_UNASSIGNED</see> on success <br/>
  ///   <see cref="WorkerOperationResult.UNASSIGNED_FULL">UNASSIGNED_FULL</see> failure, <see cref="unassignedWorkers"/> full <br/>
  ///   <see cref="WorkerOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see> ¯\_(ツ)_/¯
  /// </returns>
  public WorkerOperationResult Unassign(Animal animal) {
    if (animal == null) {
      return WorkerOperationResult.UNEXPECTED_ERROR;
    }
    
    if (unassignedFull) {
      return WorkerOperationResult.UNASSIGNED_FULL;
    }

    switch (GetWorkerAssignmentStatus(animal)) {
      case WorkerAssignmentStatus.UNASSIGNED:
        unassignedWorkers.Remove(animal);  // avoid adding animal to unassignedWorkers twice
        break;
      case WorkerAssignmentStatus.ASSIGNED:
        denSystemManager.DenInfos[workersToDens[animal]].denObject.RemoveWorker(animal);
        animal.ClearHome();
        break;
    }
    
    animal.SetVisualVisibility(false);
    
    unassignedWorkers.Add(animal);
    
    workersToDens[animal] = UNASSIGNED_DEN_ID;
    denSystemManager.InvokeOnWorkerAssigned();
    denSystemManager.NotifyPlayerUpdateFollowerCount();
    return WorkerOperationResult.WORKER_UNASSIGNED;
    
  }

  /// <summary>
  /// Assign adds an animal to a den as a worker, and removes its current unassigned status if it is unassigned
  /// </summary>
  /// <param name="animal">The animal to be assigned</param>
  /// <param name="denId">The den <paramref name="animal"/> should be assigned to - default value of <see cref="NO_DEN_ID_SET"/> will be set to <see cref="currentAdminDen"/>. If <see cref="UNASSIGNED_DEN_ID"/> is passed, it will call <see cref="Unassign"/></param>
  /// <returns>
  /// <see cref="WorkerOperationResult.WORKER_ASSIGNED">WORKER_ASSIGNED</see> success <br/>
  /// <see cref="WorkerOperationResult.DEN_FULL">DEN_FULL</see> failure, <paramref name="denId"/> full <br/>
  /// <see cref="WorkerOperationResult.ADMIN_DEN_NOT_FOUND">ADMIN_DEN_NOT_FOUND</see> when <see cref="NO_DEN_ID_SET"/> and CurrentAdminDen is null <br/>
  /// <see cref="Unassign">Unassign(animal)</see> when <see cref="UNASSIGNED_DEN_ID"/> <br/>
  /// <see cref="WorkerOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see>
  /// </returns>
  public WorkerOperationResult Assign(Animal animal, int denId = NO_DEN_ID_SET) {
    if (animal == null) {
      return WorkerOperationResult.UNEXPECTED_ERROR;
    }

    if (denId == UNASSIGNED_DEN_ID) {
      return Unassign(animal);
    }
    
    if (denId == NO_DEN_ID_SET) {
      if (!adminDenInitialized) {
        return WorkerOperationResult.ADMIN_DEN_NOT_FOUND;
      }
      denId = currentAdminDen.DenId;
    }
    
    Den targetDen = denSystemManager.DenInfos[denId].denObject;

    if (targetDen.IsFull()) {
      return WorkerOperationResult.DEN_FULL;
    }

    switch (GetWorkerAssignmentStatus(animal)) {
      case WorkerAssignmentStatus.UNASSIGNED:
        unassignedWorkers.Remove(animal);
        break;
      case WorkerAssignmentStatus.ASSIGNED:
        denSystemManager.DenInfos[workersToDens[animal]].denObject.RemoveWorker(animal);
        animal.ClearHome();
        break;
    }
    
    // Setup animal and relevant den tracking information
    animal.SetHome(targetDen);
    animal.TeleportToGridPosition(targetDen.GridPosition);
    animal.SetVisualVisibility(true);
    targetDen.AddWorker(animal);
    
    workersToDens[animal] = denId;
    
    denSystemManager.InvokeOnWorkerAssigned();
    denSystemManager.NotifyPlayerUpdateFollowerCount();
    
    return WorkerOperationResult.WORKER_ASSIGNED;
  }
  
  /// <summary>
  /// Destroys a currently unassigned worker
  /// </summary>
  /// <param name="animal">animal to be destroyed</param>
  /// <returns>
  /// <see cref="WorkerOperationResult.WORKER_DESTROYED">WORKER_DESTROYED</see> success <br/>
  /// <see cref="WorkerOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see>
  /// </returns>
  public WorkerOperationResult DestroyUnassigned(Animal animal) {
    if (GetWorkerAssignmentStatus(animal) != WorkerAssignmentStatus.UNASSIGNED) {
      return WorkerOperationResult.UNEXPECTED_ERROR;
    }
    workersToDens.Remove(animal);
    unassignedWorkers.Remove(animal);
    Destroy(animal.gameObject);
    denSystemManager.NotifyPlayerUpdateFollowerCount();
    return WorkerOperationResult.WORKER_DESTROYED;
  }

  /// <summary>
  /// Destroys a currently assigned worker
  /// </summary>
  /// <param name="animal">animal to be destroyed</param>
  /// <returns>
  /// <see cref="WorkerOperationResult.WORKER_DESTROYED">WORKER_DESTROYED</see> success <br/>
  /// <see cref="WorkerOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see>
  /// </returns>
  public WorkerOperationResult DestroyAssigned(Animal animal) {
    if (GetWorkerAssignmentStatus(animal) != WorkerAssignmentStatus.ASSIGNED) {
      return WorkerOperationResult.UNEXPECTED_ERROR;
    }
    denSystemManager.DenInfos[workersToDens[animal]].denObject.RemoveWorker(animal);
    workersToDens.Remove(animal);
    animal.ClearHome();
    Destroy(animal.gameObject);
    denSystemManager.InvokeOnWorkerAssigned();
    return WorkerOperationResult.WORKER_DESTROYED;
  }

  /// <summary>
  /// Destroys a worker being managed by <see cref="WorkerManager"/>
  /// </summary>
  /// <param name="animal">animal to be destroyed</param>
  /// <returns>
  /// <see cref="WorkerOperationResult.WORKER_DESTROYED">WORKER_DESTROYED</see> success <br/>
  /// <see cref="WorkerOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see>
  /// </returns>
  public WorkerOperationResult DestroyWorker(Animal animal) {
    WorkerAssignmentStatus assignmentStatus = GetWorkerAssignmentStatus(animal);
    switch (assignmentStatus) {
      case WorkerAssignmentStatus.UNASSIGNED:
        return DestroyUnassigned(animal);
      case WorkerAssignmentStatus.ASSIGNED:
        return DestroyAssigned(animal);
      case WorkerAssignmentStatus.ERROR:
      default:
        return WorkerOperationResult.UNEXPECTED_ERROR;
    }
  }
  
  /// <summary>
  /// Handles cleanup when a worker dies. Removes the worker from their assigned den (if any)
  /// and from the worker tracking system. Should be called from worker animal itself
  /// </summary>
  public void OnWorkerDeath(Animal animal) {
    
    
    WorkerOperationResult result = DestroyWorker(animal);
    if (result != WorkerOperationResult.WORKER_DESTROYED) {
      return;
    }
    
    if (animal.IsDyingFromStarvation) {
      denSystemManager.LogHolder.SpawnLog(LogEntryGuiController.DenLogType.WORKER_STARVE);
    }
    else {
      denSystemManager.LogHolder.SpawnLog(LogEntryGuiController.DenLogType.WORKER_EATEN);
    }
  }
  
  /// <summary>
  /// Consumes one unassigned worker to represent the controllable animal taking damage.
  /// This removes the worker from tracking, destroys the worker GameObject, and decreases MVP.
  /// </summary>
  /// <returns>True if a worker was consumed, false if no unassigned workers were available.</returns>
  public bool TryConsumeUnassignedWorkerForPlayerDamage()
  {
    if (CurrentUnassignedPopulation <= 0) {
      return false;
    }
    
    Animal workerToRemove = unassignedWorkers[0];
    
    if (workerToRemove == null)
    {
      return false;
    }
    
    workerToRemove.Die();
    return true;
  }
}