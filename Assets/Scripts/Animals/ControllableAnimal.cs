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

    /// <summary>
    /// Sets the current den this animal is in. Used internally and by InteractableManager.
    /// </summary>
    internal void SetCurrentDen(Den den)
    {
        _currentDen = den;
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
            if (den != null)
            {
                den.OnAnimalEnter(this);
                _currentDen = den;
            }
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

            // If we entered a new den, notify it
            if (newDen != null && newDen != previousDen)
            {
                newDen.OnAnimalEnter(this);
                _currentDen = newDen;
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
                    // Add item to inventory
                    AddItemToInventory(itemName);
                }
                // Remove item from tilemap
                ItemTilemapManager.Instance.RemoveItem(gridPosition);
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

    private void OnDestroy()
    {
        // Clean up den references (leave den if in one)
        if (_currentDen != null)
        {
            _currentDen.OnAnimalLeave(this);
            _currentDen = null;
        }
    }
}
