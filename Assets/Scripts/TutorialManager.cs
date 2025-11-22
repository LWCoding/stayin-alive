using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Manages the tutorial scene state and initialization.
/// Handles loading the tutorial level when the tutorial begins.
/// This should only be placed in the tutorial scene.
/// </summary>
public class TutorialManager : Singleton<TutorialManager>
{
    [Header("UI References")]
    [Tooltip("The MVP container transform that shows the goal MVP text")]
    [SerializeField] private Transform _mvpContainerTransform;
    [Tooltip("UI object that explains hunger drains after every turn")]
    [SerializeField] private GameObject _hungerExplanationUI;
    [Tooltip("UI object that explains the inventory system")]
    [SerializeField] private GameObject _inventoryExplanationUI;
    [Tooltip("UI object that explains the sticks item")]
    [SerializeField] private GameObject _sticksExplanationUI;
    [Tooltip("UI object that explains rabbits and coyote dens")]
    [SerializeField] private GameObject _rabbitsAndDensExplanationUI;
    [Tooltip("UI object that explains the den UI interface")]
    [SerializeField] private GameObject _denUIExplanationUI;
    [Tooltip("UI object that explains the den teleport feature")]
    [SerializeField] private GameObject _denUITeleportExplanationUI;
    [Tooltip("UI object prompting player to teleport back to original den")]
    [SerializeField] private GameObject _teleportBackExplanationUI;
    [Tooltip("UI object that explains breeding mechanics")]
    [SerializeField] private GameObject _breedExplanationUI;
    [Tooltip("UI object that explains den inventory management")]
    [SerializeField] private GameObject _denInventoryExplanationUI;
    [Tooltip("UI object that explains assigned and unassigned workers")]
    [SerializeField] private GameObject _workersExplanationUI;
    [Tooltip("UI object that explains the game goal and win condition")]
    [SerializeField] private GameObject _goalExplanationUI;
    [Tooltip("Final tutorial blocker shown after goal explanation")]
    [SerializeField] private GameObject _endBlockerUI;
    
    [Header("Tutorial Triggers")]
    [Tooltip("Player cannot move past this X value until they pick up grass")]
    [SerializeField] private float _grassPickupBlockerXThreshold;
    [Tooltip("Player cannot move past this X value until they build and enter a den")]
    [SerializeField] private float _denEntryBlockerXThreshold;
    [Tooltip("Player X position threshold for showing hunger explanation")]
    [SerializeField] private float _hungerExplanationXThreshold;
    [Tooltip("Player X position threshold for showing sticks explanation")]
    [SerializeField] private float _sticksExplanationXThreshold;
    [Tooltip("Player X position threshold for showing rabbits and dens explanation")]
    [SerializeField] private float _rabbitsAndDensExplanationXThreshold;
    [Tooltip("Player Y position threshold for showing goal explanation")]
    [SerializeField] private float _goalExplanationYThreshold;

    private TutorialLevelLoader _tutorialLevelLoader;
    private bool _denManagementUnlocked = false;
    private bool _hungerEnabled = true;
    private bool _hungerExplanationShown = false;
    private bool _inventoryExplanationShown = false;
    private bool _sticksExplanationShown = false;
    private bool _rabbitsAndDensExplanationShown = false;
    private bool _denUIExplanationShown = false;
    private bool _denUITeleportExplanationShown = false;
    private bool _teleportBackExplanationShown = false;
    private int _playerTeleportCount = 0;
    private bool _breedExplanationShown = false;
    private bool _denInventoryExplanationShown = false;
    private bool _workersExplanationShown = false;
    private bool _goalExplanationShown = false;
    private bool _endBlockerShown = false;
    private bool _hasEatenFood = false;
    private bool _hasPickedUpGrass = false;
    private bool _hasEnteredDen = false;
    
    public bool IsDenManagementUnlocked => _denManagementUnlocked;
    public bool IsHungerEnabled => _hungerEnabled;
    
    /// <summary>
    /// Returns true if any den UI tutorial blocker is currently active.
    /// </summary>
    public bool IsDenUITutorialActive
    {
        get
        {
            return (_denUIExplanationUI != null && _denUIExplanationUI.activeSelf) ||
                   (_denUITeleportExplanationUI != null && _denUITeleportExplanationUI.activeSelf) ||
                   (_teleportBackExplanationUI != null && _teleportBackExplanationUI.activeSelf) ||
                   (_denInventoryExplanationUI != null && _denInventoryExplanationUI.activeSelf) ||
                   (_breedExplanationUI != null && _breedExplanationUI.activeSelf) ||
                   (_workersExplanationUI != null && _workersExplanationUI.activeSelf);
        }
    }
    
    /// <summary>
    /// Checks if the player can move to a specific position based on tutorial constraints.
    /// Returns false if the position would exceed the thresholds without meeting the conditions.
    /// </summary>
    public bool CanPlayerMoveTo(Vector2Int targetPosition)
    {
        // Block movement past X threshold if player hasn't picked up grass yet
        if (!_hasPickedUpGrass && targetPosition.x > _grassPickupBlockerXThreshold)
        {
            return false;
        }
        
        // Block movement past X threshold if player hasn't entered a den yet
        if (!_hasEnteredDen && targetPosition.x > _denEntryBlockerXThreshold)
        {
            return false;
        }
        
        return true;
    }
    
    protected override void Awake()
    {
        base.Awake();
        
        // Hide MVP container at start of tutorial
        if (_mvpContainerTransform != null)
        {
            _mvpContainerTransform.gameObject.SetActive(false);
        }
        
        // Hide hunger explanation UI at start
        if (_hungerExplanationUI != null)
        {
            _hungerExplanationUI.SetActive(false);
        }
        
        // Hide inventory explanation UI at start
        if (_inventoryExplanationUI != null)
        {
            _inventoryExplanationUI.SetActive(false);
        }
        
        // Hide sticks explanation UI at start
        if (_sticksExplanationUI != null)
        {
            _sticksExplanationUI.SetActive(false);
        }
        
        // Hide rabbits and dens explanation UI at start
        if (_rabbitsAndDensExplanationUI != null)
        {
            _rabbitsAndDensExplanationUI.SetActive(false);
        }
        
        // Hide den UI explanation at start
        if (_denUIExplanationUI != null)
        {
            _denUIExplanationUI.SetActive(false);
        }
        
        // Hide den UI teleport explanation at start
        if (_denUITeleportExplanationUI != null)
        {
            _denUITeleportExplanationUI.SetActive(false);
        }
        
        // Hide teleport back explanation at start
        if (_teleportBackExplanationUI != null)
        {
            _teleportBackExplanationUI.SetActive(false);
        }
        
        // Hide breed explanation at start
        if (_breedExplanationUI != null)
        {
            _breedExplanationUI.SetActive(false);
        }
        
        // Hide den inventory explanation at start
        if (_denInventoryExplanationUI != null)
        {
            _denInventoryExplanationUI.SetActive(false);
        }
        
        // Hide workers explanation at start
        if (_workersExplanationUI != null)
        {
            _workersExplanationUI.SetActive(false);
        }
        
        // Hide goal explanation at start
        if (_goalExplanationUI != null)
        {
            _goalExplanationUI.SetActive(false);
        }
        
        // Hide end blocker at start
        if (_endBlockerUI != null)
        {
            _endBlockerUI.SetActive(false);
        }
    }
    
    private void Start()
    {
        // Load tutorial level
        _tutorialLevelLoader = FindObjectOfType<TutorialLevelLoader>();
        if (_tutorialLevelLoader != null)
        {
            _tutorialLevelLoader.LoadAndApplyLevel();
        }
        
        // Subscribe to turn events to track turns
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced += OnTurnAdvanced;
        }
        
        // Subscribe to player's food consumed event
        if (AnimalManager.Instance != null)
        {
            ControllableAnimal player = AnimalManager.Instance.GetPlayer();
            if (player != null)
            {
                player.OnFoodConsumed += OnPlayerFoodConsumed;
            }
        }
        
        // Subscribe to inventory item added event
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += OnItemAddedToInventory;
        }
        
        // Subscribe to den built event
        if (InteractableManager.Instance != null)
        {
            InteractableManager.Instance.OnDenBuiltByPlayer += OnDenBuiltByPlayer;
        }
        
        // Subscribe to den panel opened event
        if (DenSystemManager.Instance != null)
        {
            DenSystemManager.Instance.OnPanelOpened += OnDenPanelOpened;
            DenSystemManager.Instance.OnPlayerTeleported += OnPlayerTeleported;
            DenSystemManager.Instance.OnItemsDeposited += OnItemsDeposited;
            DenSystemManager.Instance.OnWorkerCreated += OnWorkerCreated;
            DenSystemManager.Instance.OnWorkerAssigned += OnWorkerAssigned;
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Unsubscribe from turn events
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= OnTurnAdvanced;
        }
        
        // Unsubscribe from player's food consumed event
        if (AnimalManager.Instance != null)
        {
            ControllableAnimal player = AnimalManager.Instance.GetPlayer();
            if (player != null)
            {
                player.OnFoodConsumed -= OnPlayerFoodConsumed;
            }
        }
        
        // Unsubscribe from inventory events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded -= OnItemAddedToInventory;
        }
        
        // Unsubscribe from den built event
        if (InteractableManager.Instance != null)
        {
            InteractableManager.Instance.OnDenBuiltByPlayer -= OnDenBuiltByPlayer;
        }
        
        // Unsubscribe from den panel opened event
        if (DenSystemManager.Instance != null)
        {
            DenSystemManager.Instance.OnPanelOpened -= OnDenPanelOpened;
            DenSystemManager.Instance.OnPlayerTeleported -= OnPlayerTeleported;
            DenSystemManager.Instance.OnItemsDeposited -= OnItemsDeposited;
            DenSystemManager.Instance.OnWorkerCreated -= OnWorkerCreated;
            DenSystemManager.Instance.OnWorkerAssigned -= OnWorkerAssigned;
        }
    }
    
    private void OnTurnAdvanced(int currentTurn)
    {
        if (AnimalManager.Instance == null) return;
        
        ControllableAnimal player = AnimalManager.Instance.GetPlayer();
        if (player == null) return;
        
        // Show hunger explanation when player moves beyond X threshold
        if (!_hungerExplanationShown && _hungerExplanationUI != null && player.GridPosition.x >= _hungerExplanationXThreshold)
        {
            _hungerExplanationUI.SetActive(true);
            _hungerExplanationShown = true;
            TimeManager.Instance?.Pause();
        }
        
        // Show sticks explanation when player moves beyond X threshold
        if (!_sticksExplanationShown && _sticksExplanationUI != null && player.GridPosition.x >= _sticksExplanationXThreshold)
        {
            _sticksExplanationUI.SetActive(true);
            _sticksExplanationShown = true;
            TimeManager.Instance?.Pause();
        }
        
        // Show rabbits and dens explanation when player moves beyond X threshold
        if (!_rabbitsAndDensExplanationShown && _rabbitsAndDensExplanationUI != null && player.GridPosition.x >= _rabbitsAndDensExplanationXThreshold)
        {
            _rabbitsAndDensExplanationUI.SetActive(true);
            _rabbitsAndDensExplanationShown = true;
            TimeManager.Instance?.Pause();
        }
        
        // Show goal explanation when player moves beyond Y threshold
        if (!_goalExplanationShown && _goalExplanationUI != null && player.GridPosition.y >= _goalExplanationYThreshold)
        {
            // Show MVP container
            ShowMVPContainer();
            
            _goalExplanationUI.SetActive(true);
            _goalExplanationShown = true;
            TimeManager.Instance?.Pause();
        }
    }
    
    private void OnPlayerFoodConsumed(int amount)
    {
        _hasEatenFood = true;
        
        // If inventory explanation is showing, check if we can close it now
        if (_inventoryExplanationShown && _inventoryExplanationUI != null && _inventoryExplanationUI.activeSelf)
        {
            _inventoryExplanationUI.SetActive(false);
            TimeManager.Instance?.Resume(); // Resume game after eating food
        }
    }
    
    private void OnItemAddedToInventory(string itemName)
    {
        // Track if player picked up grass
        if (itemName == "Grass")
        {
            _hasPickedUpGrass = true;
        }
        
        // Show inventory explanation on first item pickup
        if (!_inventoryExplanationShown && _inventoryExplanationUI != null)
        {
            _inventoryExplanationUI.SetActive(true);
            _inventoryExplanationShown = true;
            TimeManager.Instance?.Pause();
        }
    }
    
    private void OnDenBuiltByPlayer(Den den)
    {
        // Unlock den management when player builds their first den
        // This will show the "E to Manage" prompts
        if (DenSystemManager.Instance != null)
        {
            int densBuilt = DenSystemManager.Instance.DensBuiltWithSticks;
            
            if (!_denManagementUnlocked && densBuilt >= 1)
            {
                UnlockDenManagement();
            }
        }
    }
    
    private void OnDenPanelOpened()
    {
        // Mark that player has entered a den (for movement blocker)
        if (!_hasEnteredDen)
        {
            _hasEnteredDen = true;
        }
        
        // Show den UI explanation the first time player opens the den management panel
        if (!_denUIExplanationShown && _denUIExplanationUI != null)
        {
            _denUIExplanationUI.SetActive(true);
            _denUIExplanationShown = true;
            // Note: Time is already paused by DenSystemManager.OpenPanel()
        }
    }
    
    private void OnPlayerTeleported()
    {
        // Increment teleport counter
        _playerTeleportCount++;
        
        // First teleport: Close initial teleport explanation and show teleport back blocker
        if (_playerTeleportCount == 1 && _denUITeleportExplanationShown && _denUITeleportExplanationUI != null && _denUITeleportExplanationUI.activeSelf)
        {
            _denUITeleportExplanationUI.SetActive(false);
            
            // Show teleport back explanation
            if (!_teleportBackExplanationShown && _teleportBackExplanationUI != null)
            {
                _teleportBackExplanationShown = true;
                _teleportBackExplanationUI.SetActive(true);
                // Time stays paused - den panel is still open
            }
        }
        // Second teleport: Close teleport back blocker and show den inventory tutorial
        else if (_playerTeleportCount == 2 && _teleportBackExplanationShown && _teleportBackExplanationUI != null && _teleportBackExplanationUI.activeSelf)
        {
            _teleportBackExplanationUI.SetActive(false);
            
            // Show den inventory tutorial
            if (!_denInventoryExplanationShown && _denInventoryExplanationUI != null)
            {
                _denInventoryExplanationShown = true;
                _denInventoryExplanationUI.SetActive(true);
                // Time stays paused - den panel is still open
                
                // Give player starting items for den management
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem("Sticks");
                    InventoryManager.Instance.AddItem("Grass");
                }
                
                // Refresh den UI to show new items by closing and reopening the panel
                if (DenSystemManager.Instance != null)
                {
                    DenSystemManager.Instance.ClosePanel();
                    DenSystemManager.Instance.OpenPanel();
                }
            }
        }
    }
    
    /// <summary>
    /// Called when the player deposits items to a den.
    /// </summary>
    private void OnItemsDeposited()
    {
        // Close den inventory explanation when player deposits items
        if (_denInventoryExplanationShown && _denInventoryExplanationUI != null && _denInventoryExplanationUI.activeSelf)
        {
            _denInventoryExplanationUI.SetActive(false);
            
            // Show breed explanation tutorial
            if (!_breedExplanationShown && _breedExplanationUI != null)
            {
                _breedExplanationShown = true;
                _breedExplanationUI.SetActive(true);
                // Time stays paused - den panel is still open
            }
        }
    }
    
    /// <summary>
    /// Called when a worker is created/purchased (breeding).
    /// </summary>
    private void OnWorkerCreated()
    {
        // Close breed explanation when player creates a worker
        if (_breedExplanationShown && _breedExplanationUI != null && _breedExplanationUI.activeSelf)
        {
            _breedExplanationUI.SetActive(false);
            
            // Show workers explanation next
            if (!_workersExplanationShown && _workersExplanationUI != null)
            {
                _workersExplanationShown = true;
                _workersExplanationUI.SetActive(true);
                // Time stays paused - den panel is still open
            }
        }
    }
    
    /// <summary>
    /// Called when a worker is assigned to a den.
    /// </summary>
    private void OnWorkerAssigned()
    {
        // Close workers explanation when player assigns a worker
        if (_workersExplanationShown && _workersExplanationUI != null && _workersExplanationUI.activeSelf)
        {
            _workersExplanationUI.SetActive(false);
            // Time stays paused - den panel is still open, tutorial complete
        }
    }
    
    /// <summary>
    /// Advances the tutorial by closing the current popup. Call from UI buttons.
    /// </summary>
    public void AdvanceTutorial()
    {
        // Close hunger explanation if shown
        if (_hungerExplanationShown && _hungerExplanationUI != null && _hungerExplanationUI.activeSelf)
        {
            _hungerExplanationUI.SetActive(false);
            TimeManager.Instance?.Resume();
            return;
        }
        
        // Close inventory explanation only if player has eaten food
        if (_inventoryExplanationShown && _inventoryExplanationUI != null && _inventoryExplanationUI.activeSelf)
        {
            if (_hasEatenFood)
            {
                _inventoryExplanationUI.SetActive(false);
                TimeManager.Instance?.Resume();
            }
            else
            {
                Debug.Log("TutorialManager: Cannot advance - player must eat food first!");
            }
            return;
        }
        
        // Close sticks explanation if shown
        if (_sticksExplanationShown && _sticksExplanationUI != null && _sticksExplanationUI.activeSelf)
        {
            _sticksExplanationUI.SetActive(false);
            TimeManager.Instance?.Resume();
            return;
        }
        
        // Close rabbits and dens explanation if shown
        if (_rabbitsAndDensExplanationShown && _rabbitsAndDensExplanationUI != null && _rabbitsAndDensExplanationUI.activeSelf)
        {
            _rabbitsAndDensExplanationUI.SetActive(false);
            TimeManager.Instance?.Resume();
            return;
        }
        
        // Transition from goal explanation to end blocker
        if (_goalExplanationShown && _goalExplanationUI != null && _goalExplanationUI.activeSelf)
        {
            _goalExplanationUI.SetActive(false);
            
            // Show end blocker next
            if (!_endBlockerShown && _endBlockerUI != null)
            {
                _endBlockerUI.SetActive(true);
                _endBlockerShown = true;
                // Time stays paused
            }
            return;
        }
        
        // Close end blocker - final tutorial blocker
        if (_endBlockerShown && _endBlockerUI != null && _endBlockerUI.activeSelf)
        {
            _endBlockerUI.SetActive(false);
            TimeManager.Instance?.Resume();
            return;
        }
        
        // Transition from den UI explanation to teleport explanation
        if (_denUIExplanationShown && _denUIExplanationUI != null && _denUIExplanationUI.activeSelf)
        {
            _denUIExplanationUI.SetActive(false);
            
            // Show teleport explanation next
            if (!_denUITeleportExplanationShown && _denUITeleportExplanationUI != null)
            {
                _denUITeleportExplanationUI.SetActive(true);
                _denUITeleportExplanationShown = true;
            }
            // Time stays paused - den panel is still open
            return;
        }
        
        // Note: Den UI teleport explanation closes automatically when player teleports (see OnPlayerTeleported)
        // Note: Teleport back explanation closes automatically when player teleports a second time (see OnPlayerTeleported)
        // Note: Den inventory explanation closes automatically when player deposits items (see OnItemsDeposited)
        // Note: Breed explanation transitions automatically when player creates a worker (see OnWorkerCreated)
        // Note: Workers explanation closes automatically when player assigns a worker (see OnWorkerAssigned)
        // So we don't need manual close handlers for these here
    }
    
    /// <summary>
    /// Unlocks den management and refreshes all den visuals to show E to Manage prompts.
    /// </summary>
    public void UnlockDenManagement()
    {
        _denManagementUnlocked = true;
        Debug.Log("TutorialManager: Den management unlocked! Refreshing all den visuals.");
        
        // Refresh all dens to show the E to Manage objects
        if (InteractableManager.Instance != null)
        {
            List<Den> allDens = InteractableManager.Instance.Dens;
            Debug.Log($"TutorialManager: Found {allDens.Count} dens to refresh.");
            foreach (Den den in allDens)
            {
                den?.UpdateDenVisualState();
            }
        }
    }
    
    public void LockDenManagement()
    {
        _denManagementUnlocked = false;
    }
    
    public void ShowMVPContainer()
    {
        if (_mvpContainerTransform != null)
        {
            _mvpContainerTransform.gameObject.SetActive(true);
        }
    }
    
    public void EnableHunger()
    {
        _hungerEnabled = true;
    }
    
    public void DisableHunger()
    {
        _hungerEnabled = false;
    }
    
    public void ResetTutorialState()
    {
        TimeManager.Instance?.ResetTimerAndPauseForFirstMove();
    }
}
