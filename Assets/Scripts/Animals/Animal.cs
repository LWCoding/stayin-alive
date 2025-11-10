using System.Collections.Generic;
using System.Collections;
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
    private bool _isSelected;

    [Header("Drag Destination")]
    [Tooltip("Color of the line when pathfinding is possible")]
    [SerializeField] private Color _pathPossibleColor = Color.yellow;
    [Tooltip("Color of the line when pathfinding is impossible")]
    [SerializeField] private Color _pathImpossibleColor = Color.red;
    private LineRenderer _lineRenderer;

    [Header("Path Line")]
    [Tooltip("LineRenderer used to show the planned A* path")]
    [SerializeField] private LineRenderer _pathLineRenderer;
    [Tooltip("Color of the path line")]
    [SerializeField] private Color _pathLineColor = Color.yellow;
    [Tooltip("Opacity of the path line when this animal is selected (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float _selectedPathOpacity = 1f;
    [Tooltip("Opacity of the path line when this animal is not selected (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultPathOpacity = 0.3f;
    private Seeker _seeker;

    [Header("Input")]
    [Tooltip("Minimum world-space distance the cursor must move while held down before a drag begins")]
    [SerializeField] private float _dragStartThreshold = 0.5f;

    [Header("Movement")]
    [Tooltip("Seconds to interpolate when moving one grid cell")]
    [SerializeField] private float _moveDurationSeconds = 0.5f;
    private Coroutine _positionLerpCoroutine;

    private bool _isDragging = false;
    private bool _isPointerDown = false;
    private Vector3 _pointerDownWorldPos;
    private Camera _mainCamera;
    private Vector2Int _lastDragEndGridPosition;
    private bool _hasLastDragEndGridPosition;
    private bool _lastPathfindingSuccessful;
    private bool _hasLastPathfindingResult;
    private bool _isBeingDestroyed = false;

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

    /// <summary>
    /// Applies per-turn needs decay, handles death, and restores from resource tiles.
    /// Called by TimeManager after movement each turn.
    /// Checks the tile type first - animals on food/water tiles don't lose hunger/thirst.
    /// </summary>
    public virtual void ApplyTurnNeedsAndTileRestoration()
    {
        // Check what tile the animal is on AFTER moving
        TileType currentTile = TileType.Empty; // Default
        if (EnvironmentManager.Instance != null)
        {
            currentTile = EnvironmentManager.Instance.GetTileType(_gridPosition);
        }

        // Only decay hunger if NOT on a Grass tile (animals can eat on Grass)
        if (currentTile != TileType.Grass)
        {
            AddHunger(-1);
        }
        else
        {
            // On Grass tile - restore hunger to max instead of decaying
            int maxHunger = _animalData != null ? _animalData.maxHunger : 100;
            SetHunger(maxHunger);
        }

        // Only decay thirst if NOT on a Water tile (animals can drink on Water)
        if (currentTile != TileType.Water)
        {
            AddThirst(-1);
        }
        else
        {
            // On Water tile - restore thirst to max instead of decaying
            int maxHydration = _animalData != null ? _animalData.maxHydration : 100;
            SetThirst(maxHydration);
        }

        // Death if either is zero or below
        if (_currentHunger <= 0 || _currentThirst <= 0)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Executes this animal's turn: moves one step along planned path (if applicable) and applies needs/restoration.
    /// </summary>
    public virtual void TakeTurn()
    {
        if (_isControllable)
        {
            AdvanceOneStepAlongPlannedPath();
        }

        ApplyTurnNeedsAndTileRestoration();
    }

    /// <summary>
    /// If a destination is set and the path was previously validated, move exactly one grid step along the path.
    /// </summary>
    protected void AdvanceOneStepAlongPlannedPath()
    {
        if (!HasDestination || LastPathfindingSuccessful != true)
        {
            return;
        }
        MoveOneStepTowards(_lastDragEndGridPosition);
    }

    /// <summary>
    /// Computes an A* path from current grid to the destination and moves one grid step along it if possible.
    /// </summary>
    /// <param name="destinationGrid">Target grid position to move towards.</param>
    protected void MoveOneStepTowards(Vector2Int destinationGrid)
    {
        if (EnvironmentManager.Instance == null || AstarPath.active == null)
        {
            return;
        }

        Vector2Int startGrid = _gridPosition;
        Vector2Int destGrid = destinationGrid;

        Vector3 startWorld = ConvertGridToWorldPosition(startGrid);
        Vector3 destWorld = ConvertGridToWorldPosition(destGrid);

        var path = ABPath.Construct(startWorld, destWorld, null);
        AstarPath.StartPath(path, true);
        path.BlockUntilCalculated();

        if (path.error || path.vectorPath == null || path.vectorPath.Count == 0)
        {
            return;
        }

        List<Vector3> axisAligned = BuildAxisAlignedPath(path.vectorPath);
        if (axisAligned.Count < 2)
        {
            return;
        }

        Vector2Int nextGrid = ConvertWorldToGridPosition(axisAligned[1]);
        if (EnvironmentManager.Instance.IsValidPosition(nextGrid) &&
            EnvironmentManager.Instance.IsWalkable(nextGrid))
        {
            SetGridPosition(nextGrid);
        }
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
        }

        // Ensure Seeker exists for A* requests
        _seeker = GetComponent<Seeker>();
        if (_seeker == null)
        {
            _seeker = gameObject.AddComponent<Seeker>();
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

        // Setup path line (separate renderer, hidden by default)
        if (_pathLineRenderer == null)
        {
            // Avoid reusing the destination line; create a new child
            GameObject pathObj = new GameObject("PathLine");
            pathObj.transform.SetParent(transform);
            pathObj.transform.localPosition = Vector3.zero;
            _pathLineRenderer = pathObj.AddComponent<LineRenderer>();
        }
        if (_pathLineRenderer != null)
        {
            _pathLineRenderer.positionCount = 0;
            _pathLineRenderer.enabled = false;
            _pathLineRenderer.useWorldSpace = true;
            // Reasonable defaults if none set in inspector
            if (_pathLineRenderer.sharedMaterial == null)
            {
                _pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            if (_pathLineRenderer.startWidth <= 0f)
            {
                _pathLineRenderer.startWidth = 0.08f;
                _pathLineRenderer.endWidth = 0.08f;
            }
            // Set initial color
            _pathLineRenderer.startColor = _pathLineColor;
            _pathLineRenderer.endColor = _pathLineColor;
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
        
        // Refresh path line if we have a valid destination (path start has changed)
        TryRequestAndDrawPathFromState();
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

        _isPointerDown = true;
        _pointerDownWorldPos = GetDestinationPosition();
    }

    private void OnMouseUp()
    {
        _isPointerDown = false;
        HandleMouseRelease();
    }

    private void Update()
    {
        if (_isPointerDown && !_isDragging)
        {
            Vector3 currentWorldPos = GetDestinationPosition();
            float planarDistance = Vector2.Distance(new Vector2(_pointerDownWorldPos.x, _pointerDownWorldPos.y),
                                                    new Vector2(currentWorldPos.x, currentWorldPos.y));
            if (planarDistance >= _dragStartThreshold)
            {
                StartDragging();
            }
        }

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

        // If path is possible, request and draw the A* path; otherwise clear
        if (pathPossible)
        {
            RequestAndDrawPath(_gridPosition, destinationGridPos);
        }
        else
        {
            ClearPathLine();
        }
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

        // While dragging we clear any previously planned path
        ClearPathLine();
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
        
        // Update path line opacity based on selection state
        UpdatePathLineOpacity();
        
        // Request/refresh path if we have a valid destination
        TryRequestAndDrawPathFromState();
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

    /// <summary>
    /// If we have a valid destination and feasibility, request and draw the path.
    /// </summary>
    private void TryRequestAndDrawPathFromState()
    {
        if (_hasLastDragEndGridPosition && _hasLastPathfindingResult && _lastPathfindingSuccessful)
        {
            RequestAndDrawPath(_gridPosition, _lastDragEndGridPosition);
        }
        else if (!_hasLastDragEndGridPosition || !_hasLastPathfindingResult || !_lastPathfindingSuccessful)
        {
            ClearPathLine();
        }
    }

    /// <summary>
    /// Requests an A* path and draws it when complete.
    /// </summary>
    private void RequestAndDrawPath(Vector2Int startGrid, Vector2Int destGrid)
    {
        if (_seeker == null || AstarPath.active == null)
        {
            ClearPathLine();
            return;
        }

        Vector3 startWorld = ConvertGridToWorldPosition(startGrid);
        Vector3 endWorld = ConvertGridToWorldPosition(destGrid);

        _seeker.CancelCurrentPathRequest();
        _seeker.StartPath(startWorld, endWorld, OnPathComplete);
    }

    /// <summary>
    /// Callback for Seeker path completion. Draws the points on the path line.
    /// </summary>
    private void OnPathComplete(Path path)
    {
        // Safety check: if this animal is being destroyed, ignore the callback
        if (_isBeingDestroyed || _pathLineRenderer == null)
        {
            return;
        }

        if (path == null || path.error)
        {
            ClearPathLine();
            return;
        }

        var pts = path.vectorPath;
        if (pts == null || pts.Count == 0)
        {
            ClearPathLine();
            return;
        }

        List<Vector3> axisAlignedPoints = BuildAxisAlignedPath(pts);
        if (axisAlignedPoints.Count == 0)
        {
            ClearPathLine();
            return;
        }

        // Additional safety check before accessing renderer
        if (_pathLineRenderer == null || _pathLineRenderer.gameObject == null)
        {
            return;
        }

        _pathLineRenderer.positionCount = axisAlignedPoints.Count;
        for (int i = 0; i < axisAlignedPoints.Count; i++)
        {
            _pathLineRenderer.SetPosition(i, axisAlignedPoints[i]);
        }
        
        // Always enable the path line and update opacity based on selection
        _pathLineRenderer.enabled = true;
        UpdatePathLineOpacity();
    }

    /// <summary>
    /// Updates the path line opacity based on selection state.
    /// </summary>
    private void UpdatePathLineOpacity()
    {
        if (_pathLineRenderer == null)
        {
            return;
        }

        float opacity = _isSelected ? _selectedPathOpacity : _defaultPathOpacity;
        Color pathColor = _pathLineColor;
        pathColor.a = opacity;
        
        _pathLineRenderer.startColor = pathColor;
        _pathLineRenderer.endColor = pathColor;
    }

    /// <summary>
    /// Hides and clears the path line.
    /// </summary>
    private void ClearPathLine()
    {
        if (_pathLineRenderer != null)
        {
            _pathLineRenderer.enabled = false;
            _pathLineRenderer.positionCount = 0;
        }
    }

    /// <summary>
    /// Converts the raw path points into a list of axis-aligned world positions (no diagonals).
    /// </summary>
    private List<Vector3> BuildAxisAlignedPath(IReadOnlyList<Vector3> rawPoints)
    {
        List<Vector3> axisPoints = new List<Vector3>();
        if (rawPoints == null || rawPoints.Count == 0)
        {
            return axisPoints;
        }

        Vector2Int currentGrid = ConvertWorldToGridPosition(rawPoints[0]);
        axisPoints.Add(ConvertGridToWorldPosition(currentGrid));

        for (int i = 1; i < rawPoints.Count; i++)
        {
            Vector2Int targetGrid = ConvertWorldToGridPosition(rawPoints[i]);
            if (targetGrid == currentGrid)
            {
                continue;
            }

            while (currentGrid.x != targetGrid.x)
            {
                currentGrid.x += targetGrid.x > currentGrid.x ? 1 : -1;
                axisPoints.Add(ConvertGridToWorldPosition(currentGrid));
            }

            while (currentGrid.y != targetGrid.y)
            {
                currentGrid.y += targetGrid.y > currentGrid.y ? 1 : -1;
                axisPoints.Add(ConvertGridToWorldPosition(currentGrid));
            }
        }

        return axisPoints;
    }

    private void OnDestroy()
    {
        // Mark as being destroyed to prevent callbacks from executing
        _isBeingDestroyed = true;

        // Cancel any pending path requests
        if (_seeker != null)
        {
            _seeker.CancelCurrentPathRequest();
        }

        // Stop any running coroutines
        if (_positionLerpCoroutine != null)
        {
            StopCoroutine(_positionLerpCoroutine);
            _positionLerpCoroutine = null;
        }

        // Clear and disable path line renderers
        ClearPathLine();
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }

        // Clean up AnimalManager references
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.ClearSelectedAnimal(this);
            AnimalManager.Instance.RemoveAnimal(this);
        }
    }
}

