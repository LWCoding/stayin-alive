using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A controllable animal that can be controlled by the player using WASD/arrow key input.
/// </summary>
public class ControllableAnimal : Animal
{
    [Header("Input")]
    [Tooltip("Whether this animal can be controlled by keyboard input")]
    [SerializeField] private bool _canReceiveInput = true;

    // Track the last time the animal moved (for rate limiting)
    private float _lastMoveTime = 0f;

    // Track the den this animal is currently in (if any)
    private Den _currentDen = null;

    // Track the bush this animal is currently in (if any)
    private Bush _currentBush = null;

    // Track if we're subscribed to TimeManager events
    private bool _isSubscribedToTimeManager = false;

    private void OnEnable()
    {
        // Subscribe to turn advancement to decrease hunger
        SubscribeToTimeManager();

        // Update UI when enabled
        UpdateHungerUI();
    }

    private void OnDisable()
    {
        // Unsubscribe from turn advancement
        UnsubscribeFromTimeManager();
    }

    private void Start()
    {
        // Ensure we're subscribed if TimeManager wasn't available in OnEnable
        SubscribeToTimeManager();
    }

    private void SubscribeToTimeManager()
    {
        if (!_isSubscribedToTimeManager && TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced += OnTurnAdvanced;
            _isSubscribedToTimeManager = true;
        }
    }

    private void UnsubscribeFromTimeManager()
    {
        if (_isSubscribedToTimeManager && TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= OnTurnAdvanced;
            _isSubscribedToTimeManager = false;
        }
    }

    /// <summary>
    /// Called when a turn advances. Decreases hunger for controllable animals.
    /// </summary>
    private void OnTurnAdvanced(int turnCount)
    {
        // Decrease hunger each turn (controllable animals don't go through TakeTurn)
        // UI will be updated automatically via DecreaseHunger override
        DecreaseHunger(1);
    }

    /// <summary>
    /// Updates the hunger UI if UIManager is available.
    /// </summary>
    private void UpdateHungerUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHungerBar();
        }
    }

    /// <summary>
    /// Override to update UI when hunger decreases.
    /// </summary>
    public override void DecreaseHunger(int amount)
    {
        base.DecreaseHunger(amount);
        UpdateHungerUI();
    }

    /// <summary>
    /// Override to update UI when hunger increases.
    /// </summary>
    public override void IncreaseHunger(int amount)
    {
        base.IncreaseHunger(amount);
        UpdateHungerUI();
    }

    /// <summary>
    /// Sets the current den this animal is in. Used internally and by InteractableManager.
    /// </summary>
    internal void SetCurrentDen(Den den)
    {
        _currentDen = den;
    }

    /// <summary>
    /// Sets the current bush this animal is in. Used internally.
    /// </summary>
    internal void SetCurrentBush(Bush bush)
    {
        _currentBush = bush;
    }


    /// <summary>
    /// Override to indicate this animal is controllable.
    /// </summary>
    public override bool IsControllable => true;

    public override void TakeTurn()
    {
        // Controllable animals don't move automatically - they move when the player presses keys
        // This method is kept for compatibility but does nothing
    }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Initializes the animal with AnimalData and grid position.
    /// </summary>
    public override void Initialize(AnimalData animalData, Vector2Int gridPosition)
    {
        base.Initialize(animalData, gridPosition);

        // Handle den entry if animal spawns on a den
        if (InteractableManager.Instance != null)
        {
            Den den = InteractableManager.Instance.GetDenAtPosition(gridPosition);
            if (den != null && !HasPredatorAtPosition(gridPosition))
            {
                den.OnAnimalEnter(this);
                _currentDen = den;
            }

            // Handle bush entry if animal spawns on a bush
            Bush bush = InteractableManager.Instance.GetBushAtPosition(gridPosition);
            if (bush != null && !HasPredatorAtPosition(gridPosition))
            {
                bush.OnAnimalEnter(this);
                _currentBush = bush;
            }
        }

        // Register with UIManager for hunger display
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetTrackedAnimal(this);
        }
    }

    /// <summary>
    /// Sets the animal's grid position and updates its world position.
    /// </summary>
    public override void SetGridPosition(Vector2Int gridPosition)
    {
        Vector2Int previousPosition = GridPosition;
        base.SetGridPosition(gridPosition);

        // Handle den entry/exit
        if (InteractableManager.Instance != null)
        {
            Den previousDen = _currentDen;
            Den newDen = InteractableManager.Instance.GetDenAtPosition(gridPosition);

            // If we left a den, notify it
            if (previousDen != null && previousDen != newDen)
            {
                previousDen.OnAnimalLeave(this);
                _currentDen = null;
            }

            // If we entered a new den, notify it (only if no predator is blocking the den)
            if (newDen != null && newDen != previousDen)
            {
                // Check if there's a predator at this position - if so, prevent den entry
                if (!HasPredatorAtPosition(gridPosition))
                {
                    newDen.OnAnimalEnter(this);
                    _currentDen = newDen;
                }
            }

            // Handle bush entry/exit
            Bush previousBush = _currentBush;
            Bush newBush = InteractableManager.Instance.GetBushAtPosition(gridPosition);

            // If we left a bush, notify it
            if (previousBush != null && previousBush != newBush)
            {
                previousBush.OnAnimalLeave(this);
                _currentBush = null;
            }

            // If we entered a new bush, notify it (only if no predator is blocking the bush)
            if (newBush != null && newBush != previousBush)
            {
                // Check if there's a predator at this position - if so, prevent bush entry
                if (!HasPredatorAtPosition(gridPosition))
                {
                    newBush.OnAnimalEnter(this);
                    _currentBush = newBush;
                }
            }
        }

        // Check for items at this position and pick them up
        if (ItemTilemapManager.Instance != null)
        {
            if (ItemTilemapManager.Instance.HasItemAt(gridPosition))
            {
                // Get the item name before removing it
                string itemName = ItemTilemapManager.Instance.GetItemNameAt(gridPosition);
                
                if (!string.IsNullOrEmpty(itemName))
                {
                    // Try to add item to inventory using InventoryManager
                    if (InventoryManager.Instance != null)
                    {
                    if (InventoryManager.Instance.AddItem(itemName))
                        {
                            // Remove item from tilemap only if successfully added to inventory
                            ItemTilemapManager.Instance.RemoveItem(gridPosition);
                            // Restore hunger to full when picking up an item
                            IncreaseHunger(MaxHunger);
                        }
                        else
                        {
                            Debug.Log($"Cannot pick up '{itemName}' - inventory is full!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("ControllableAnimal: InventoryManager instance not found! Cannot add item to inventory.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Kills this animal by destroying its GameObject.
    /// Triggers the lose condition for controllable animals.
    /// </summary>
    public override void Die()
    {
        // Trigger lose condition before destroying
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerLose();
        }

        base.Die();
    }

    private void Update()
    {
        // Only process input if this animal can receive input
        if (!_canReceiveInput || TimeManager.Instance == null)
        {
            return;
        }

        // Allow input if paused only if we're waiting for the first move
        // Otherwise, block input when paused
        if (TimeManager.Instance.IsPaused && !TimeManager.Instance.IsWaitingForFirstMove)
        {
            return;
        }


        // Check if enough time has passed since last movement (rate limiting)
        float timeSinceLastMove = Time.time - _lastMoveTime;
        if (timeSinceLastMove < Globals.MoveDurationSeconds)
        {
            return;
        }

        // Handle WASD or Arrow key input (supports held keys)
        Vector2Int moveDirection = Vector2Int.zero;

        // Check for movement input (WASD or Arrow keys) - using GetKey to support held keys
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            moveDirection = Vector2Int.up;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            moveDirection = Vector2Int.down;
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveDirection = Vector2Int.left;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            moveDirection = Vector2Int.right;
        }

        // If a movement key is held, try to move
        if (moveDirection != Vector2Int.zero)
        {
            TryMove(moveDirection);
        }
    }


    /// <summary>
    /// Attempts to move the animal one step in the specified direction.
    /// </summary>
    private void TryMove(Vector2Int direction)
    {
        if (EnvironmentManager.Instance == null)
        {
            return;
        }

        Vector2Int currentPos = GridPosition;
        Vector2Int targetPos = currentPos + direction;

        // Check if the target position is valid and walkable
        if (!EnvironmentManager.Instance.IsValidPosition(targetPos))
        {
            return;
        }

        if (!EnvironmentManager.Instance.IsWalkable(targetPos))
        {
            return;
        }

        // Check if the target position is water and if this animal can go on water
        TileType tileType = EnvironmentManager.Instance.GetTileType(targetPos);
        if (tileType == TileType.Water && !CanGoOnWater)
        {
            return; // Cannot move on water
        }

        // Move to the new position
        SetGridPosition(targetPos);

        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.ResolveTileConflictsForAnimal(this);
        }

        // Update last move time for rate limiting
        _lastMoveTime = Time.time;

        // Notify TimeManager that the player has moved
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.NotifyPlayerMoved();
        }
    }

    /// <summary>
    /// Checks if there is a predator animal at the specified grid position.
    /// </summary>
    private bool HasPredatorAtPosition(Vector2Int position)
    {
        if (AnimalManager.Instance == null)
        {
            return false;
        }

        List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
        for (int i = 0; i < animals.Count; i++)
        {
            Animal other = animals[i];
            if (other == null || other == this)
            {
                continue;
            }

            if (other.GridPosition == position && other is PredatorAnimal)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        UnsubscribeFromTimeManager();

        // Clean up den references (leave den if in one)
        if (_currentDen != null)
        {
            _currentDen.OnAnimalLeave(this);
            _currentDen = null;
        }

        // Clean up bush references (leave bush if in one)
        if (_currentBush != null)
        {
            _currentBush.OnAnimalLeave(this);
            _currentBush = null;
        }

        // Clear UI tracking if this animal is currently tracked
        if (UIManager.Instance != null && UIManager.Instance.IsTrackingAnimal(this))
        {
            UIManager.Instance.SetTrackedAnimal(null);
        }
    }
}
