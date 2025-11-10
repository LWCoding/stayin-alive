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

    [SerializeField] [Tooltip("Whether this animal can be controlled by the player")]
    private bool _isControllable = true;

    private bool _isDragging = false;
    private Camera _mainCamera;
    private LineRenderer _lineRenderer;

    public AnimalData AnimalData => _animalData;
    public Vector2Int GridPosition => _gridPosition;
    public int CurrentHunger => _currentHunger;
    public int CurrentThirst => _currentThirst;
    public bool IsControllable
    {
        get => _isControllable;
        set => _isControllable = value;
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        // Setup line renderer if not assigned
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                // Create a child object for the line renderer
                GameObject lineObj = new GameObject("DestinationLine");
                lineObj.transform.SetParent(transform);
                lineObj.transform.localPosition = Vector3.zero;
                _lineRenderer = lineObj.AddComponent<LineRenderer>();
            }
        }
    }

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

    /// <summary>
    /// Called when the mouse button is pressed down on this animal.
    /// Requires a Collider2D component on the GameObject for this to work.
    /// </summary>
    private void OnMouseDown()
    {
        if (!_isControllable)
        {
            return;
        }

        StartDragging();
    }

    /// <summary>
    /// Called when the mouse button is released.
    /// </summary>
    private void OnMouseUp()
    {
        if (_isDragging)
        {
            StopDragging();
        }
    }

    private void Update()
    {
        // Update the line while dragging (even if mouse is outside the collider)
        if (_isDragging)
        {
            UpdateDestinationLine();
            
            // Check for mouse button release (fallback in case OnMouseUp doesn't fire)
            if (Input.GetMouseButtonUp(0))
            {
                StopDragging();
            }
        }
    }

    /// <summary>
    /// Starts the dragging operation.
    /// </summary>
    private void StartDragging()
    {
        _isDragging = true;
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = true;
        }
    }

    /// <summary>
    /// Stops the dragging operation and clears the line.
    /// </summary>
    private void StopDragging()
    {
        _isDragging = false;
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Updates the destination line to show from animal position to mouse position.
    /// </summary>
    private void UpdateDestinationLine()
    {
        if (_lineRenderer == null || _mainCamera == null)
        {
            return;
        }

        // Get animal's world position
        Vector3 animalPos = transform.position;
        
        // Account for bottom pivot by adding vertical offset of 0.5 units
        Vector3 lineStartPos = animalPos + Vector3.up * 0.5f;
        
        // Convert mouse position to world position
        // For orthographic cameras, Z distance doesn't affect the result, but we set it for consistency
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = _mainCamera.WorldToScreenPoint(animalPos).z;
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = animalPos.z; // Use the same Z as the animal

        // Update line renderer positions
        _lineRenderer.SetPosition(0, lineStartPos);
        _lineRenderer.SetPosition(1, mouseWorldPos);
    }
}

