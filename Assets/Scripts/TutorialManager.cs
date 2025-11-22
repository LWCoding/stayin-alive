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
    
    [Header("Tutorial Triggers")]
    [Tooltip("Player X position threshold for showing hunger explanation")]
    [SerializeField] private float _hungerExplanationXThreshold = 5f;
    [Tooltip("Player X position threshold for showing sticks explanation")]
    [SerializeField] private float _sticksExplanationXThreshold = 10f;
    [Tooltip("Player X position threshold for showing rabbits and dens explanation")]
    [SerializeField] private float _rabbitsAndDensExplanationXThreshold = 15f;

    private TutorialLevelLoader _tutorialLevelLoader;
    private bool _denManagementUnlocked = false;
    private bool _hungerEnabled = true;
    private bool _hungerExplanationShown = false;
    private bool _inventoryExplanationShown = false;
    private bool _sticksExplanationShown = false;
    private bool _rabbitsAndDensExplanationShown = false;
    private bool _hasEatenFood = false;
    
    public bool IsDenManagementUnlocked => _denManagementUnlocked;
    public bool IsHungerEnabled => _hungerEnabled;
    
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
        // Show inventory explanation on first item pickup
        if (!_inventoryExplanationShown && _inventoryExplanationUI != null)
        {
            _inventoryExplanationUI.SetActive(true);
            _inventoryExplanationShown = true;
            TimeManager.Instance?.Pause(); // Pause game while showing explanation
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
    }
    
    /// <summary>
    /// Unlocks den management and refreshes all den visuals to show E to Manage prompts.
    /// </summary>
    public void UnlockDenManagement()
    {
        _denManagementUnlocked = true;
        
        // Refresh all dens to show the E to Manage objects
        if (InteractableManager.Instance != null)
        {
            List<Den> allDens = InteractableManager.Instance.Dens;
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
