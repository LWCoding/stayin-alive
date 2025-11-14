using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Base class for all animals. Handles animations, animal count tracking, and death.
/// Controllable-specific functionality is handled by ControllableAnimal subclass.
/// </summary>
public class Animal : MonoBehaviour
{
    [Header("Animal Info")]
    [HideInInspector] [SerializeField] private AnimalData _animalData;
    [HideInInspector] [SerializeField] private Vector2Int _gridPosition;
    [HideInInspector] [SerializeField] private Vector2Int _previousGridPosition;
    [HideInInspector] [SerializeField] private bool _encounteredAnimalDuringMove;

    [HideInInspector] [SerializeField] protected SpriteRenderer _spriteRenderer;
    private TwoFrameAnimator _twoFrameAnimator;
    private Coroutine _positionLerpCoroutine;
    private bool _areVisualsVisible = true;

    [Header("Inventory")]
    // Dictionary to track items: itemName -> count
    private Dictionary<string, int> _inventory = new Dictionary<string, int>();

    [Header("Animal Grouping")]
    [Tooltip("Number of animals in this group (acts as hitpoints). When reduced to 0, this animal is destroyed.")]
    [SerializeField] private int _animalCount = 1;
    [Tooltip("Text component that displays the animal count as 'x{count}'")]
    [SerializeField] private TMP_Text _countText;

    [Header("Follower System")]
    [Tooltip("List of follower GameObjects that visually represent additional animals in the group")]
    private List<GameObject> _followers = new List<GameObject>();
    private Coroutine _followerUpdateCoroutine;

    public AnimalData AnimalData => _animalData;
    public Vector2Int GridPosition => _gridPosition;
    public Vector2Int PreviousGridPosition => _previousGridPosition;
    public bool EncounteredAnimalDuringMove => _encounteredAnimalDuringMove;
    public void ClearEncounteredAnimalDuringMoveFlag()
    {
        _encounteredAnimalDuringMove = false;
    }

    /// <summary>
    /// Whether this animal can be controlled by the player. Override in subclasses to specify controllability.
    /// </summary>
    public virtual bool IsControllable => false;

    public int AnimalCount => _animalCount;

    /// <summary>
    /// Gets a copy of the inventory dictionary. Returns a new dictionary to prevent external modification.
    /// </summary>
    public Dictionary<string, int> GetInventory()
    {
        return new Dictionary<string, int>(_inventory);
    }

    /// <summary>
    /// Sets the visibility of this animal's primary visuals (sprite renderer and count text),
    /// along with any follower sprites.
    /// </summary>
    public void SetVisualVisibility(bool isVisible)
    {
        _areVisualsVisible = isVisible;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = isVisible;
        }

        if (_countText != null)
        {
            _countText.enabled = isVisible;
        }

        // Update follower sprites as well
        for (int i = 0; i < _followers.Count; i++)
        {
            GameObject follower = _followers[i];
            if (follower == null)
            {
                continue;
            }

            ApplyVisibilityToFollower(follower, isVisible);
        }
    }

    private void ApplyVisibilityToFollower(GameObject follower, bool isVisible)
    {
        if (follower == null)
        {
            return;
        }

        SpriteRenderer followerSpriteRenderer = follower.GetComponent<SpriteRenderer>();
        if (followerSpriteRenderer == null)
        {
            followerSpriteRenderer = follower.GetComponentInChildren<SpriteRenderer>();
        }

        if (followerSpriteRenderer != null)
        {
            followerSpriteRenderer.enabled = isVisible;
        }
    }

    /// <summary>
    /// Gets the count of a specific item in the inventory. Returns 0 if the item is not in inventory.
    /// </summary>
    public int GetItemCount(string itemName)
    {
        if (_inventory.TryGetValue(itemName, out int count))
        {
            return count;
        }
        return 0;
    }

    /// <summary>
    /// Checks if the animal has at least one of the specified item.
    /// </summary>
    public bool HasItem(string itemName)
    {
        return GetItemCount(itemName) > 0;
    }

    /// <summary>
    /// Adds an item to the inventory. If the item already exists, increments the count.
    /// </summary>
    public void AddItemToInventory(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning($"Animal '{name}': Cannot add item with null or empty name to inventory.");
            return;
        }

        if (_inventory.ContainsKey(itemName))
        {
            _inventory[itemName]++;
        }
        else
        {
            _inventory[itemName] = 1;
        }

        Debug.Log($"Animal '{name}' picked up item '{itemName}'. Inventory now has {_inventory[itemName]} of this item.");
    }

    /// <summary>
    /// Removes one instance of an item from the inventory. Returns true if the item was removed, false if the item was not in inventory.
    /// </summary>
    public bool RemoveItemFromInventory(string itemName)
    {
        if (!_inventory.ContainsKey(itemName))
        {
            return false;
        }

        _inventory[itemName]--;
        if (_inventory[itemName] <= 0)
        {
            _inventory.Remove(itemName);
        }

        return true;
    }

    /// <summary>
    /// Removes all instances of an item from the inventory. Returns the number of items removed.
    /// </summary>
    public int RemoveAllItemsFromInventory(string itemName)
    {
        if (!_inventory.ContainsKey(itemName))
        {
            return 0;
        }

        int count = _inventory[itemName];
        _inventory.Remove(itemName);
        return count;
    }

    /// <summary>
    /// Clears all items from the inventory.
    /// </summary>
    public void ClearInventory()
    {
        _inventory.Clear();
    }

    /// <summary>
    /// Reduces the animal count by one. If the count reaches zero, destroys this animal.
    /// </summary>
    /// <returns>True if the animal was destroyed, false otherwise.</returns>
    public bool ReduceAnimalCount()
    {
        if (_animalCount <= 0)
        {
            return false;
        }

        int oldCount = _animalCount;
        _animalCount--;
        UpdateCountText();
        UpdateFollowers(oldCount, _animalCount);

        // Spawn blood particle effects when taking damage
        if (ParticleEffectManager.Instance != null)
        {
            Vector3 effectPosition = transform.position;
            if (EnvironmentManager.Instance != null)
            {
                effectPosition = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
                effectPosition.z = transform.position.z;
            }
            ParticleEffectManager.Instance.SpawnParticleEffect("Blood", effectPosition, 2);
        }

        if (_animalCount <= 0)
        {
            ClearAllFollowers();
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the animal count. If set to zero or below, destroys this animal.
    /// </summary>
    public void SetAnimalCount(int count)
    {
        if (count <= 0)
        {
            _animalCount = 0;
            UpdateCountText();
            ClearAllFollowers();
            Die();
        }
        else
        {
            int oldCount = _animalCount;
            _animalCount = count;
            UpdateCountText();
            UpdateFollowers(oldCount, count);
        }
    }

    /// <summary>
    /// Increases the animal count by the specified amount.
    /// </summary>
    public void IncreaseAnimalCount(int amount)
    {
        if (amount > 0)
        {
            int oldCount = _animalCount;
            _animalCount += amount;
            UpdateCountText();
            UpdateFollowers(oldCount, _animalCount);
        }
    }

    /// <summary>
    /// Executes this animal's turn. Override in subclasses to implement turn behavior.
    /// </summary>
    public virtual void TakeTurn()
    {
        // Base implementation does nothing
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
    public virtual void Initialize(AnimalData animalData, Vector2Int gridPosition)
    {
        _animalData = animalData;
        _gridPosition = gridPosition;
        _previousGridPosition = gridPosition;
        _encounteredAnimalDuringMove = false;

        // Setup two-frame animation if data is available
        SetupTwoFrameAnimation();

        UpdateWorldPosition();

        // Update count text display
        UpdateCountText();

        // Initialize followers based on current count
        UpdateFollowers(0, _animalCount);
    }

    /// <summary>
    /// Sets up the two-frame animator with data from AnimalData if available.
    /// Initializes the sprite to the first frame if available.
    /// </summary>
    private void SetupTwoFrameAnimation()
    {
        if (_animalData == null)
        {
            return;
        }

        // Initialize sprite to first frame if available
        if (_animalData.frame1Sprite != null && _spriteRenderer != null)
        {
            _spriteRenderer.sprite = _animalData.frame1Sprite;
        }

        // Check if we have full animation data (both frames and valid interval)
        if (_animalData.frame1Sprite != null && _animalData.frame2Sprite != null && _animalData.animationInterval > 0)
        {
            // Get or add TwoFrameAnimator component
            if (_twoFrameAnimator == null)
            {
                _twoFrameAnimator = GetComponent<TwoFrameAnimator>();
                if (_twoFrameAnimator == null)
                {
                    _twoFrameAnimator = gameObject.AddComponent<TwoFrameAnimator>();
                }
            }

            // Assign the SpriteRenderer if not already assigned
            if (_spriteRenderer != null)
            {
                _twoFrameAnimator.SetSpriteRenderer(_spriteRenderer);
            }

            // Initialize the animator with the data
            _twoFrameAnimator.Initialize(_animalData.frame1Sprite, _animalData.frame2Sprite, _animalData.animationInterval);
        }
    }

    /// <summary>
    /// Sets the animal's grid position and updates its world position.
    /// </summary>
    public virtual void SetGridPosition(Vector2Int gridPosition)
    {
        if (_gridPosition != gridPosition)
        {
            _previousGridPosition = _gridPosition;
            _gridPosition = gridPosition;
            _encounteredAnimalDuringMove = false;

            if (AnimalManager.Instance != null &&
                AnimalManager.Instance.HasOtherAnimalAtPosition(this, _gridPosition))
            {
                _encounteredAnimalDuringMove = true;
            }

            // Update sprite facing direction based on horizontal movement
            UpdateFacingDirection();
        }
        Vector3 targetWorld;
        if (EnvironmentManager.Instance != null)
        {
            targetWorld = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
        }
        else
        {
            targetWorld = new Vector3(_gridPosition.x, _gridPosition.y, transform.position.z);
        }
        StartMoveToWorldPosition(targetWorld, Globals.MoveDurationSeconds);
        
        // Update follower positions will be handled by the follower update coroutine
    }

    /// <summary>
    /// Updates the sprite facing direction based on horizontal movement.
    /// Flips the sprite to face left when moving left, right when moving right.
    /// Only flips the sprite, not the entire GameObject (preserves counter text orientation).
    /// </summary>
    private void UpdateFacingDirection()
    {
        // Calculate horizontal movement direction
        int horizontalMovement = _gridPosition.x - _previousGridPosition.x;
        
        // Only update if there's horizontal movement
        if (horizontalMovement == 0)
        {
            return;
        }

        // Flip only the sprite using SpriteRenderer.flipX
        // When flipX is true, sprite faces left; when false, sprite faces right
        // Assuming sprites face right by default (flipX = false means facing right)
        if (_spriteRenderer != null)
        {
            if (horizontalMovement > 0)
            {
                // Moving right - sprite should face right (flipX = false)
                _spriteRenderer.flipX = false;
            }
            else // horizontalMovement < 0
            {
                // Moving left - sprite should face left (flipX = true)
                _spriteRenderer.flipX = true;
            }
        }
        
        // Update followers' facing direction as well
        UpdateFollowersFacingDirection(horizontalMovement > 0);
    }

    /// <summary>
    /// Updates the facing direction of all followers to match the main animal.
    /// Only flips the sprite, not the entire GameObject.
    /// </summary>
    private void UpdateFollowersFacingDirection(bool facingRight)
    {
        foreach (GameObject follower in _followers)
        {
            if (follower == null)
            {
                continue;
            }

            // Get the follower's sprite renderer
            SpriteRenderer followerSpriteRenderer = follower.GetComponent<SpriteRenderer>();
            if (followerSpriteRenderer == null)
            {
                followerSpriteRenderer = follower.GetComponentInChildren<SpriteRenderer>();
            }

            if (followerSpriteRenderer != null)
            {
                // Flip only the sprite
                followerSpriteRenderer.flipX = !facingRight;
            }
        }
    }

    /// <summary>
    /// Forces the animal back onto its previous grid position without preventing the logic
    /// of the tile it attempted to enter from executing.
    /// </summary>
    public void ResetToPreviousGridPosition()
    {
        if (_gridPosition == _previousGridPosition)
        {
            return;
        }

        if (_positionLerpCoroutine != null)
        {
            StopCoroutine(_positionLerpCoroutine);
            _positionLerpCoroutine = null;
        }

        _gridPosition = _previousGridPosition;
        _encounteredAnimalDuringMove = false;

        Vector3 targetWorld;
        if (EnvironmentManager.Instance != null)
        {
            targetWorld = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
        }
        else
        {
            targetWorld = new Vector3(_gridPosition.x, _gridPosition.y, transform.position.z);
        }

        // Preserve current z value to avoid unintended layering changes
        targetWorld.z = transform.position.z;
        transform.position = targetWorld;
    }

    /// <summary>
    /// Updates the world position based on the grid position.
    /// </summary>
    private void UpdateWorldPosition()
    {
        if (EnvironmentManager.Instance != null)
        {
            // Get the world position from the grid
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
            transform.position = worldPos;
        }
        else
        {
            // Fallback: use grid position directly
            transform.position = new Vector3(_gridPosition.x, _gridPosition.y, 0);
        }
    }

    /// <summary>
    /// Smoothly interpolates the transform to target world position over the given duration.
    /// Stops any previous interpolation in progress.
    /// </summary>
    private void StartMoveToWorldPosition(Vector3 targetWorldPosition, float durationSeconds)
    {
        // Preserve current z in case target is computed at different z
        targetWorldPosition.z = transform.position.z;
        if (_positionLerpCoroutine != null)
        {
            StopCoroutine(_positionLerpCoroutine);
        }
        _positionLerpCoroutine = StartCoroutine(LerpPositionCoroutine(targetWorldPosition, durationSeconds));
    }

    private IEnumerator LerpPositionCoroutine(Vector3 targetWorldPosition, float durationSeconds)
    {
        Vector3 start = transform.position;
        if (durationSeconds <= 0f)
        {
            transform.position = targetWorldPosition;
            _positionLerpCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / durationSeconds);
            transform.position = Vector3.Lerp(start, targetWorldPosition, t);
            yield return null;
        }

        transform.position = targetWorldPosition;
        _positionLerpCoroutine = null;
    }

    /// <summary>
    /// Kills this animal by destroying its GameObject.
    /// Override in subclasses to add additional behavior (e.g., trigger lose condition).
    /// </summary>
    public virtual void Die()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Updates the count text display to show "x{count}" format.
    /// Only displays text when there is more than one animal (hides for single animal).
    /// </summary>
    private void UpdateCountText()
    {
        if (_countText != null)
        {
            if (_animalCount > 1)
            {
                _countText.text = $"x{_animalCount}";
            }
            else
            {
                _countText.text = "";
            }
        }
    }

    /// <summary>
    /// Updates the follower count to match the animal count.
    /// Spawns new followers if count increased, removes followers if count decreased.
    /// </summary>
    private void UpdateFollowers(int oldCount, int newCount)
    {
        int oldFollowerCount = oldCount > 1 ? oldCount - 1 : 0;
        int newFollowerCount = newCount > 1 ? newCount - 1 : 0;

        // Remove excess followers if count decreased
        while (_followers.Count > newFollowerCount)
        {
            RemoveLastFollower();
        }

        // Add new followers if count increased
        while (_followers.Count < newFollowerCount)
        {
            SpawnFollower();
        }

        // Start/restart the follower update coroutine if we have followers
        if (_followers.Count > 0)
        {
            if (_followerUpdateCoroutine != null)
            {
                StopCoroutine(_followerUpdateCoroutine);
            }
            _followerUpdateCoroutine = StartCoroutine(UpdateFollowersCoroutine());
        }
        else if (_followerUpdateCoroutine != null)
        {
            StopCoroutine(_followerUpdateCoroutine);
            _followerUpdateCoroutine = null;
        }
    }

    /// <summary>
    /// Spawns a new follower GameObject that follows this animal with a delay.
    /// </summary>
    private void SpawnFollower()
    {
        if (_animalData == null || _animalData.prefab == null)
        {
            Debug.LogWarning($"Animal '{name}': Cannot spawn follower - AnimalData or prefab is null.");
            return;
        }

        // Instantiate a copy of this animal
        GameObject followerObj = Instantiate(_animalData.prefab, transform.parent);
        followerObj.name = $"{name}_Follower_{_followers.Count + 1}";

        // Get the follower's Animal component and sprite renderer before disabling
        Animal followerAnimal = followerObj.GetComponent<Animal>();
        SpriteRenderer followerSpriteRenderer = null;
        if (followerAnimal != null)
        {
            // Get the sprite renderer from the Animal component before disabling it
            followerSpriteRenderer = followerAnimal._spriteRenderer;
            followerAnimal.enabled = false;
        }
        
        // Fallback: try to get sprite renderer directly if not found
        if (followerSpriteRenderer == null)
        {
            followerSpriteRenderer = followerObj.GetComponent<SpriteRenderer>();
            if (followerSpriteRenderer == null)
            {
                followerSpriteRenderer = followerObj.GetComponentInChildren<SpriteRenderer>();
            }
        }

        // Also disable ControllableAnimal if it exists
        ControllableAnimal followerControllable = followerObj.GetComponent<ControllableAnimal>();
        if (followerControllable != null)
        {
            followerControllable.enabled = false;
        }

        // Disable Rigidbody2D physics simulation if it exists
        Rigidbody2D followerRigidbody = followerObj.GetComponent<Rigidbody2D>();
        if (followerRigidbody != null)
        {
            followerRigidbody.simulated = false;
        }

        // Disable count text on followers
        TMP_Text followerCountText = followerObj.GetComponentInChildren<TMP_Text>();
        if (followerCountText != null)
        {
            followerCountText.enabled = false;
        }

        // Set initial position to match the original
        followerObj.transform.position = transform.position;

        // Scale down the follower to the specified percentage of the original size
        followerObj.transform.localScale = transform.localScale * Globals.FollowerScale;

        // Set follower sprite sorting order to be one less than the main sprite
        if (followerSpriteRenderer != null && _spriteRenderer != null)
        {
            followerSpriteRenderer.sortingOrder = _spriteRenderer.sortingOrder + 1;
            // Match the flipX state of the main sprite
            followerSpriteRenderer.flipX = _spriteRenderer.flipX;
        }

        ApplyVisibilityToFollower(followerObj, _areVisualsVisible);

        _followers.Add(followerObj);
    }

    /// <summary>
    /// Removes the last follower from the list and destroys it.
    /// </summary>
    private void RemoveLastFollower()
    {
        if (_followers.Count == 0)
        {
            return;
        }

        int lastIndex = _followers.Count - 1;
        GameObject lastFollower = _followers[lastIndex];
        _followers.RemoveAt(lastIndex);

        // Safely destroy the follower (check if it still exists)
        if (lastFollower != null)
        {
            Destroy(lastFollower);
        }
    }

    /// <summary>
    /// Clears all followers and destroys them.
    /// </summary>
    private void ClearAllFollowers()
    {
        // Stop the update coroutine
        if (_followerUpdateCoroutine != null)
        {
            StopCoroutine(_followerUpdateCoroutine);
            _followerUpdateCoroutine = null;
        }

        // Destroy all followers
        foreach (GameObject follower in _followers)
        {
            if (follower != null)
            {
                Destroy(follower);
            }
        }
        _followers.Clear();
    }

    /// <summary>
    /// Coroutine that updates follower positions with their respective delays.
    /// Each follower follows the original animal's position with a delay based on its index.
    /// Uses a position history queue to track where the animal was at different times.
    /// </summary>
    private IEnumerator UpdateFollowersCoroutine()
    {
        // Store position history with timestamps for each follower
        List<Queue<PositionTime>> positionHistories = new List<Queue<PositionTime>>();
        float[] delays = new float[_followers.Count];

        // Initialize delays and position histories: first follower uses base delay, subsequent followers add increment
        for (int i = 0; i < _followers.Count; i++)
        {
            delays[i] = Globals.BaseFollowerDelay + (i * Globals.FollowerDelayIncrement);
            positionHistories.Add(new Queue<PositionTime>());
            // Initialize with current position
            positionHistories[i].Enqueue(new PositionTime { position = transform.position, time = Time.time });
        }

        while (_followers.Count > 0 && this != null)
        {
            Vector3 currentPosition = transform.position;
            float currentTime = Time.time;

            // Record current position for all followers
            for (int i = 0; i < _followers.Count; i++)
            {
                if (_followers[i] == null)
                {
                    continue;
                }

                // Add current position to history
                positionHistories[i].Enqueue(new PositionTime { position = currentPosition, time = currentTime });

                // Remove old positions that are beyond the delay window
                float targetTime = currentTime - delays[i];
                while (positionHistories[i].Count > 0 && positionHistories[i].Peek().time < targetTime)
                {
                    positionHistories[i].Dequeue();
                }

                // Get the target position (where the animal was at the delay time)
                if (positionHistories[i].Count > 0)
                {
                    PositionTime target = positionHistories[i].Peek();
                    _followers[i].transform.position = target.position;
                }
                else
                {
                    // Fallback: use current position if no history
                    _followers[i].transform.position = currentPosition;
                }
            }

            // Wait a frame before checking again
            yield return null;
        }
    }

    /// <summary>
    /// Helper struct to store position with timestamp for follower following.
    /// </summary>
    private struct PositionTime
    {
        public Vector3 position;
        public float time;
    }

    private void OnDestroy()
    {
        // Stop any running coroutines
        if (_positionLerpCoroutine != null)
        {
            StopCoroutine(_positionLerpCoroutine);
            _positionLerpCoroutine = null;
        }

        if (_followerUpdateCoroutine != null)
        {
            StopCoroutine(_followerUpdateCoroutine);
            _followerUpdateCoroutine = null;
        }

        // Clear all followers
        ClearAllFollowers();

        // Clean up AnimalManager references
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.RemoveAnimal(this);
        }
    }
}
