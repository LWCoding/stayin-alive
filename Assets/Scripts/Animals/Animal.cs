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

    private SpriteRenderer _spriteRenderer;
    private TwoFrameAnimator _twoFrameAnimator;

    [Header("Movement")]
    [Tooltip("Seconds to interpolate when moving one grid cell")]
    [SerializeField] private float _moveDurationSeconds = 0.5f;
    private Coroutine _positionLerpCoroutine;

    [Header("Inventory")]
    // Dictionary to track items: itemName -> count
    private Dictionary<string, int> _inventory = new Dictionary<string, int>();

    [Header("Animal Grouping")]
    [Tooltip("Number of animals in this group (acts as hitpoints). When reduced to 0, this animal is destroyed.")]
    [SerializeField] private int _animalCount = 1;
    [Tooltip("Text component that displays the animal count as 'x{count}'")]
    [SerializeField] private TMP_Text _countText;

    public AnimalData AnimalData => _animalData;
    public Vector2Int GridPosition => _gridPosition;

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

        _animalCount--;
        UpdateCountText();

        if (_animalCount <= 0)
        {
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
            Die();
        }
        else
        {
            _animalCount = count;
            UpdateCountText();
        }
    }

    /// <summary>
    /// Increases the animal count by the specified amount.
    /// </summary>
    public void IncreaseAnimalCount(int amount)
    {
        if (amount > 0)
        {
            _animalCount += amount;
            UpdateCountText();
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

        // Setup two-frame animation if data is available
        SetupTwoFrameAnimation();

        UpdateWorldPosition();

        // Update count text display
        UpdateCountText();
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
        _gridPosition = gridPosition;
        Vector3 targetWorld;
        if (EnvironmentManager.Instance != null)
        {
            targetWorld = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
        }
        else
        {
            targetWorld = new Vector3(_gridPosition.x, _gridPosition.y, transform.position.z);
        }
        StartMoveToWorldPosition(targetWorld, _moveDurationSeconds);
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
    /// </summary>
    private void UpdateCountText()
    {
        if (_countText != null)
        {
            _countText.text = $"x{_animalCount}";
        }
    }

    private void OnDestroy()
    {
        // Stop any running coroutines
        if (_positionLerpCoroutine != null)
        {
            StopCoroutine(_positionLerpCoroutine);
            _positionLerpCoroutine = null;
        }

        // Clean up AnimalManager references
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.RemoveAnimal(this);
        }
    }
}
