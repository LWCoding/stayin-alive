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
    private TwoFrameAnimator _twoFrameAnimator;

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

    [Header("Inventory")]
    // Dictionary to track items: itemName -> count
    private Dictionary<string, int> _inventory = new Dictionary<string, int>();

    [Header("Animal Grouping")]
    [Tooltip("Number of animals in this group (acts as hitpoints). When reduced to 0, this animal is destroyed.")]
    [SerializeField] private int _animalCount = 1;
    [Tooltip("Text component that displays the animal count as 'x{count}'")]
    [SerializeField] private TMP_Text _countText;

    // Track the den this animal is currently in (if any)
    private Den _currentDen = null;

    /// <summary>
    /// Sets the current den this animal is in. Used internally and by InteractableManager.
    /// </summary>
    internal void SetCurrentDen(Den den)
    {
        _currentDen = den;
    }

    public AnimalData AnimalData => _animalData;
    public Vector2Int GridPosition => _gridPosition;
    public bool HasDestination => _hasLastDragEndGridPosition;
    public Vector2Int LastDestinationGridPosition => _lastDragEndGridPosition;
    public bool? LastPathfindingSuccessful => _hasLastPathfindingResult ? _lastPathfindingSuccessful : (bool?)null;
    public bool IsControllable
    {
        get => _isControllable;
        set => _isControllable = value;
    }
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
    /// Executes this animal's turn: moves one step along planned path (if applicable).
    /// </summary>
    public virtual void TakeTurn()
    {
        if (_isControllable)
        {
            AdvanceOneStepAlongPlannedPath();
        }
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
        
        // Setup two-frame animation if data is available
        SetupTwoFrameAnimation();

        // Ensure selection visuals are reset
        SetSelectionState(false);
        
        UpdateWorldPosition();
        
        // Update count text display
        UpdateCountText();
        
        // Handle den entry if animal spawns on a den (for controllable animals)
        if (_isControllable && InteractableManager.Instance != null)
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
    public void SetGridPosition(Vector2Int gridPosition)
    {
        Vector2Int previousPosition = _gridPosition;
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
        
        // Handle den entry/exit for controllable animals
        if (_isControllable && InteractableManager.Instance != null)
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
            
            // If we're still in the same den but position changed (shouldn't happen, but handle it)
            if (previousDen == newDen && newDen != null && previousPosition != gridPosition)
            {
                // Position changed but still in same den - this shouldn't happen normally
                // but handle it gracefully
            }
        }
        
        // Check for items at this position and pick them up if this is a controllable animal
        if (_isControllable && ItemTilemapManager.Instance != null)
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
    /// Kills this animal by destroying its GameObject.
    /// </summary>
    public void Die()
    {
        Destroy(gameObject);
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

        // Clean up den references (leave den if in one)
        if (_currentDen != null)
        {
            _currentDen.OnAnimalLeave(this);
            _currentDen = null;
        }

        // Clean up AnimalManager references
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.ClearSelectedAnimal(this);
            AnimalManager.Instance.RemoveAnimal(this);
        }
    }
}

