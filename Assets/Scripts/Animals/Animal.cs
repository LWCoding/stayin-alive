using UnityEngine;
using TMPro;

/// <summary>
/// Component attached to animal prefabs. Represents an animal in the game world.
/// </summary>
public class Animal : MonoBehaviour
{
    [Header("Animal Info")]
    [HideInInspector] [SerializeField] private AnimalData _animalData;
    [HideInInspector] [SerializeField] private Vector2Int _gridPosition;

    [Header("UI")]
    [Tooltip("TextMeshPro UGUI component to display current hunger value")]
    [SerializeField] private TextMeshProUGUI _hungerText;
    [Tooltip("TextMeshPro UGUI component to display current thirst/hydration value")]
    [SerializeField] private TextMeshProUGUI _thirstText;

    [Header("Stats")]
    private int _currentHunger;
    private int _currentThirst;

    public AnimalData AnimalData => _animalData;
    public Vector2Int GridPosition => _gridPosition;
    public int CurrentHunger => _currentHunger;
    public int CurrentThirst => _currentThirst;

    /// <summary>
    /// Initializes the animal with AnimalData and grid position.
    /// </summary>
    public void Initialize(AnimalData animalData, Vector2Int gridPosition)
    {
        _animalData = animalData;
        _gridPosition = gridPosition;
        
        // Initialize hunger and thirst to max values
        if (_animalData != null)
        {
            _currentHunger = _animalData.maxHunger;
            _currentThirst = _animalData.maxHydration;
        }
        else
        {
            _currentHunger = 100;
            _currentThirst = 100;
        }
        
        // Apply sprite if available
        ApplySprite();
        
        // Update UI to show initial values
        UpdateHungerThirstUI();
        
        UpdateWorldPosition();
    }

    /// <summary>
    /// Applies the idle sprite from AnimalData to the animal's SpriteRenderer if available.
    /// </summary>
    private void ApplySprite()
    {
        if (_animalData == null || _animalData.idleSprite == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = _animalData.idleSprite;
        }
    }

    /// <summary>
    /// Sets the animal's grid position and updates its world position.
    /// </summary>
    public void SetGridPosition(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
        UpdateWorldPosition();
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
    /// Sets the animal's hunger value and updates the UI.
    /// </summary>
    /// <param name="hunger">New hunger value (will be clamped between 0 and maxHunger)</param>
    public void SetHunger(int hunger)
    {
        if (_animalData != null)
        {
            _currentHunger = Mathf.Clamp(hunger, 0, _animalData.maxHunger);
        }
        else
        {
            _currentHunger = Mathf.Clamp(hunger, 0, 100);
        }
        
        UpdateHungerThirstUI();
    }

    /// <summary>
    /// Sets the animal's thirst/hydration value and updates the UI.
    /// </summary>
    /// <param name="thirst">New thirst value (will be clamped between 0 and maxHydration)</param>
    public void SetThirst(int thirst)
    {
        if (_animalData != null)
        {
            _currentThirst = Mathf.Clamp(thirst, 0, _animalData.maxHydration);
        }
        else
        {
            _currentThirst = Mathf.Clamp(thirst, 0, 100);
        }
        
        UpdateHungerThirstUI();
    }

    /// <summary>
    /// Adds to the animal's hunger value and updates the UI.
    /// </summary>
    /// <param name="amount">Amount to add to hunger (can be negative)</param>
    public void AddHunger(int amount)
    {
        SetHunger(_currentHunger + amount);
    }

    /// <summary>
    /// Adds to the animal's thirst/hydration value and updates the UI.
    /// </summary>
    /// <param name="amount">Amount to add to thirst (can be negative)</param>
    public void AddThirst(int amount)
    {
        SetThirst(_currentThirst + amount);
    }

    /// <summary>
    /// Updates the hunger and thirst UI text displays.
    /// </summary>
    private void UpdateHungerThirstUI()
    {
        if (_hungerText != null)
        {
            _hungerText.text = _currentHunger.ToString();
        }

        if (_thirstText != null)
        {
            _thirstText.text = _currentThirst.ToString();
        }
    }
}

