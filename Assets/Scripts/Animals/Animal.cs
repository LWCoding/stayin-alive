using UnityEngine;
using TMPro;
using Pathfinding;

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

    [Header("Selection Visuals")]
    [Tooltip("Color blended with the base sprite color when this animal is selected")]
    [SerializeField] private Color _selectionTintColor = new Color(1f, 0.92f, 0.7f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float _selectionTintStrength = 0.35f;
    private SpriteRenderer _spriteRenderer;
    private Color _originalSpriteColor;
    private bool _hasOriginalColor;
    private bool _isSelected;

    [Header("Drag Destination")]
    [Tooltip("Color of the line when pathfinding is possible")]
    [SerializeField] private Color _pathPossibleColor = Color.yellow;
    [Tooltip("Color of the line when pathfinding is impossible")]
    [SerializeField] private Color _pathImpossibleColor = Color.red;
    private LineRenderer _lineRenderer;

    private bool _isDragging = false;
    private Camera _mainCamera;
    private Vector2Int _lastDragEndGridPosition;
    private bool _hasLastDragEndGridPosition;
    private bool _lastPathfindingSuccessful;
    private bool _hasLastPathfindingResult;

    public AnimalData AnimalData => _animalData;
    public Vector2Int GridPosition => _gridPosition;
    public int CurrentHunger => _currentHunger;
    public int CurrentThirst => _currentThirst;
    public bool HasDestination => _hasLastDragEndGridPosition;
    public Vector2Int LastDestinationGridPosition => _lastDragEndGridPosition;
    public bool? LastPathfindingSuccessful => _hasLastPathfindingResult ? _lastPathfindingSuccessful : (bool?)null;
    public bool IsControllable
    {
        get => _isControllable;
        set => _isControllable = value;
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            _originalSpriteColor = _spriteRenderer.color;
            _hasOriginalColor = true;
        }

        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
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
        
        // Apply sprite if available
        ApplySprite();

        // Ensure selection visuals are reset
        SetSelectionState(false);
        
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

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = _animalData.idleSprite;
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

    private void OnMouseDown()
    {
        SelectThisAnimal();

        if (!_isControllable)
        {
            return;
        }

        StartDragging();
    }

    private void OnMouseUp()
    {
        HandleMouseRelease();
    }

    private void Update()
    {
        if (_isDragging)
        {
            UpdateDestinationLine();

            if (Input.GetMouseButtonUp(0))
            {
                HandleMouseRelease();
            }
        }
    }

    /// <summary>
    /// Handles the end of a drag to record the destination and evaluate pathfinding.
    /// </summary>
    private void HandleMouseRelease()
    {
        if (!_isDragging)
        {
            return;
        }

        Vector3 destinationWorldPos = GetDestinationPosition();
        Vector2Int destinationGridPos = ConvertWorldToGridPosition(destinationWorldPos);

        _lastDragEndGridPosition = destinationGridPos;
        _hasLastDragEndGridPosition = true;

        bool pathPossible = false;
        if (EnvironmentManager.Instance == null || EnvironmentManager.Instance.IsValidPosition(destinationGridPos))
        {
            pathPossible = CheckPathfindingPossible(_gridPosition, destinationGridPos);
        }

        _lastPathfindingSuccessful = pathPossible;
        _hasLastPathfindingResult = true;

        if (_lineRenderer != null)
        {
            _lineRenderer.startColor = pathPossible ? _pathPossibleColor : _pathImpossibleColor;
            _lineRenderer.endColor = _lineRenderer.startColor;

            Vector3 startPos = transform.position + Vector3.up * 0.5f;
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, destinationWorldPos);
        }

        Debug.Log($"Animal '{name}' path to {destinationGridPos} is {(pathPossible ? "possible" : "not possible")}.");

        StopDragging();
    }

    /// <summary>
    /// Starts the dragging operation.
    /// </summary>
    private void StartDragging()
    {
        _isDragging = true;
        _hasLastDragEndGridPosition = false;
        _hasLastPathfindingResult = false;

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.startColor = _pathPossibleColor;
            _lineRenderer.endColor = _pathPossibleColor;
            UpdateDestinationLine();
        }
    }

    /// <summary>
    /// Stops the dragging operation and clears drag state.
    /// </summary>
    private void StopDragging()
    {
        _isDragging = false;

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }

        if (!_hasLastDragEndGridPosition)
        {
            _hasLastPathfindingResult = false;
        }
    }

    private void SelectThisAnimal()
    {
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.SetSelectedAnimal(this);
        }
        else
        {
            SetSelectionState(true);
        }
    }

    /// <summary>
    /// Applies or removes the selection tint on this animal.
    /// </summary>
    public void SetSelectionState(bool isSelected)
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        _isSelected = isSelected;

        if (_isSelected)
        {
            Color tinted = Color.Lerp(_originalSpriteColor, _selectionTintColor, _selectionTintStrength);
            tinted.a = _originalSpriteColor.a;
            _spriteRenderer.color = tinted;
        }
        else
        {
            _spriteRenderer.color = _originalSpriteColor;
        }
    }
    
    /// <summary>
    /// Gets the current destination position (mouse world position).
    /// </summary>
    private Vector3 GetDestinationPosition()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                return transform.position;
            }
        }

        Vector3 animalPos = transform.position;
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = _mainCamera.WorldToScreenPoint(animalPos).z;
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = animalPos.z;

        return mouseWorldPos;
    }

    /// <summary>
    /// Converts a world position to a grid position.
    /// </summary>
    private Vector2Int ConvertWorldToGridPosition(Vector3 worldPosition)
    {
        if (EnvironmentManager.Instance != null)
        {
            return EnvironmentManager.Instance.WorldToGridPosition(worldPosition);
        }

        return new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.y));
    }

    /// <summary>
    /// Converts a grid position to a world position.
    /// </summary>
    private Vector3 ConvertGridToWorldPosition(Vector2Int gridPosition)
    {
        if (EnvironmentManager.Instance != null)
        {
            return EnvironmentManager.Instance.GridToWorldPosition(gridPosition);
        }

        return new Vector3(gridPosition.x, gridPosition.y, transform.position.z);
    }

    /// <summary>
    /// Updates the drag line to follow the current mouse position.
    /// </summary>
    private void UpdateDestinationLine()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        Vector3 animalPos = transform.position;
        Vector3 lineStartPos = animalPos + Vector3.up * 0.5f;
        Vector3 mouseWorldPos = GetDestinationPosition();

        _lineRenderer.SetPosition(0, lineStartPos);
        _lineRenderer.SetPosition(1, mouseWorldPos);
    }

    /// <summary>
    /// Determines if pathfinding is possible between two grid positions.
    /// </summary>
    private bool CheckPathfindingPossible(Vector2Int startGridPosition, Vector2Int destinationGridPosition)
    {
        // If we have environment data, both start and destination must be walkable
        if (EnvironmentManager.Instance != null)
        {
            if (!EnvironmentManager.Instance.IsValidPosition(startGridPosition) ||
                !EnvironmentManager.Instance.IsValidPosition(destinationGridPosition))
            {
                return false;
            }
            if (!EnvironmentManager.Instance.IsWalkable(startGridPosition) ||
                !EnvironmentManager.Instance.IsWalkable(destinationGridPosition))
            {
                return false;
            }
        }

        if (AstarPath.active == null)
        {
            return false;
        }

        Vector3 startWorld = ConvertGridToWorldPosition(startGridPosition);
        Vector3 destinationWorld = ConvertGridToWorldPosition(destinationGridPosition);

        NNInfo startNNInfo = AstarPath.active.GetNearest(startWorld, NNConstraint.Default);
        NNInfo destNNInfo = AstarPath.active.GetNearest(destinationWorld, NNConstraint.Default);

        if (startNNInfo.node == null || destNNInfo.node == null)
        {
            return false;
        }

        return PathUtilities.IsPathPossible(startNNInfo.node, destNNInfo.node);
    }

    private void OnDestroy()
    {
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.ClearSelectedAnimal(this);
        }
    }
}

