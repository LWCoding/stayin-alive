using UnityEngine;

/// <summary>
/// Camera controller that allows panning by moving the mouse to screen edges.
/// The camera is constrained to stay within the grid bounds plus a 2-tile margin.
/// </summary>
[RequireComponent(typeof(Camera))]
public class EdgeScrollCamera : MonoBehaviour
{
    [Header("Edge Scrolling")]
    [Tooltip("Width of the edge detection zone in pixels")]
    [SerializeField] private float _edgeScrollZone = 10f;
    
    [Tooltip("Speed at which the camera moves when mouse is at edge")]
    [SerializeField] private float _scrollSpeed = 5f;
    
    [Header("Camera Bounds")]
    [Tooltip("Margin in tiles beyond the grid edges (default: 2)")]
    [SerializeField] private int _gridMargin = 2;
    
    private Camera _camera;
    private EnvironmentManager _environmentManager;
    private Grid _gridComponent;
    
    // Camera bounds in world space
    private float _minX;
    private float _maxX;
    private float _minY;
    private float _maxY;
    
    // Whether bounds have been calculated
    private bool _boundsCalculated = false;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        
        if (_camera == null)
        {
            Debug.LogError("EdgeScrollCamera: Camera component not found!");
        }
    }

    private void Start()
    {
        _environmentManager = EnvironmentManager.Instance;
        
        if (_environmentManager == null)
        {
            Debug.LogWarning("EdgeScrollCamera: EnvironmentManager instance not found! Camera bounds will not be calculated.");
            return;
        }
        
        // Subscribe to grid initialization event
        _environmentManager.OnGridInitialized += OnGridInitialized;
        
        // Try to calculate bounds and center camera if grid is already initialized
        CalculateCameraBounds();
        if (_boundsCalculated)
        {
            CenterCameraOnGrid();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event
        if (_environmentManager != null)
        {
            _environmentManager.OnGridInitialized -= OnGridInitialized;
        }
    }

    /// <summary>
    /// Called when the grid is initialized or reinitialized.
    /// </summary>
    private void OnGridInitialized(int width, int height)
    {
        CalculateCameraBounds();
        CenterCameraOnGrid();
    }

    private void LateUpdate()
    {
        // Handle edge scrolling
        HandleEdgeScrolling();
        
        // Clamp camera position to bounds
        ClampCameraPosition();
    }

    /// <summary>
    /// Calculates the camera bounds based on the grid size.
    /// </summary>
    private void CalculateCameraBounds()
    {
        if (_environmentManager == null)
        {
            return;
        }
        
        Vector2Int gridSize = _environmentManager.GetGridSize();
        
        // If grid is not initialized yet (size is 0), skip calculation
        if (gridSize.x == 0 || gridSize.y == 0)
        {
            return;
        }
        
        // Get Grid component - we need to access it to get cell size
        // Since EnvironmentManager doesn't expose it, we'll find it in the scene
        if (_gridComponent == null)
        {
            _gridComponent = FindObjectOfType<Grid>();
        }
        
        if (_gridComponent == null)
        {
            Debug.LogWarning("EdgeScrollCamera: Grid component not found! Using default cell size of 1.");
            // Fallback: assume cell size of 1
            CalculateBoundsWithCellSize(gridSize, Vector3.one);
            return;
        }
        
        // Get cell size from grid
        Vector3 cellSize = _gridComponent.cellSize;
        CalculateBoundsWithCellSize(gridSize, cellSize);
    }
    
    /// <summary>
    /// Calculates camera bounds using the grid size and cell size.
    /// </summary>
    private void CalculateBoundsWithCellSize(Vector2Int gridSize, Vector3 cellSize)
    {
        // Get world positions for grid corners
        // Grid coordinates go from (0, 0) to (gridSize.x - 1, gridSize.y - 1)
        
        // Bottom-left corner (grid position 0, 0)
        Vector3 bottomLeftWorld = _environmentManager.GridToWorldPosition(0, 0);
        
        // Top-right corner (grid position gridSize.x - 1, gridSize.y - 1)
        Vector3 topRightWorld = _environmentManager.GridToWorldPosition(gridSize.x - 1, gridSize.y - 1);
        
        // Calculate the actual world bounds of the grid
        // Account for cell size - the world position is at the center of the cell
        float halfCellWidth = cellSize.x * 0.5f;
        float halfCellHeight = cellSize.y * 0.5f;
        
        float gridMinX = bottomLeftWorld.x - halfCellWidth;
        float gridMaxX = topRightWorld.x + halfCellWidth;
        float gridMinY = bottomLeftWorld.y - halfCellHeight;
        float gridMaxY = topRightWorld.y + halfCellHeight;
        
        // Add margin (2 tiles) to the grid bounds
        float marginX = cellSize.x * _gridMargin;
        float marginY = cellSize.y * _gridMargin;
        
        // Calculate the world bounds with margin (where camera edges can go)
        float gridBoundsMinX = gridMinX - marginX;
        float gridBoundsMaxX = gridMaxX + marginX;
        float gridBoundsMinY = gridMinY - marginY;
        float gridBoundsMaxY = gridMaxY + marginY;
        
        // Calculate camera view dimensions
        // Orthographic size is half the height of the camera view
        float cameraHeight = _camera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * _camera.aspect;
        
        float halfCameraWidth = cameraWidth * 0.5f;
        float halfCameraHeight = cameraHeight * 0.5f;
        
        // Calculate camera center bounds
        // The camera center must stay within bounds such that the camera edges don't exceed the grid bounds + margin
        _minX = gridBoundsMinX + halfCameraWidth;
        _maxX = gridBoundsMaxX - halfCameraWidth;
        _minY = gridBoundsMinY + halfCameraHeight;
        _maxY = gridBoundsMaxY - halfCameraHeight;
        
        // If grid (with margin) is smaller than camera view, allow camera to center on grid
        // In this case, we allow the camera to move slightly beyond the margin to center the view
        if (_maxX < _minX)
        {
            float centerX = (gridMinX + gridMaxX) * 0.5f;
            _minX = centerX - halfCameraWidth;
            _maxX = centerX + halfCameraWidth;
        }
        
        if (_maxY < _minY)
        {
            float centerY = (gridMinY + gridMaxY) * 0.5f;
            _minY = centerY - halfCameraHeight;
            _maxY = centerY + halfCameraHeight;
        }
        
        _boundsCalculated = true;
    }

    /// <summary>
    /// Centers the camera on the grid's effective area.
    /// </summary>
    private void CenterCameraOnGrid()
    {
        if (!_boundsCalculated || _environmentManager == null)
        {
            return;
        }
        
        Vector2Int gridSize = _environmentManager.GetGridSize();
        
        if (gridSize.x == 0 || gridSize.y == 0)
        {
            return;
        }
        
        // Calculate the center of the grid (effective area)
        // Bottom-left corner (grid position 0, 0)
        Vector3 bottomLeftWorld = _environmentManager.GridToWorldPosition(0, 0);
        
        // Top-right corner (grid position gridSize.x - 1, gridSize.y - 1)
        Vector3 topRightWorld = _environmentManager.GridToWorldPosition(gridSize.x - 1, gridSize.y - 1);
        
        // Calculate the center point of the grid
        float centerX = (bottomLeftWorld.x + topRightWorld.x) * 0.5f;
        float centerY = (bottomLeftWorld.y + topRightWorld.y) * 0.5f;
        
        // Get current camera position to preserve Z
        Vector3 currentPosition = transform.position;
        
        // Set camera position to grid center, keeping Z unchanged
        Vector3 newPosition = new Vector3(centerX, centerY, currentPosition.z);
        
        // Clamp to bounds to ensure it's within valid camera movement area
        newPosition.x = Mathf.Clamp(newPosition.x, _minX, _maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, _minY, _maxY);
        
        transform.position = newPosition;
    }

    /// <summary>
    /// Handles edge scrolling based on mouse position.
    /// </summary>
    private void HandleEdgeScrolling()
    {
        if (_camera == null)
        {
            return;
        }
        
        Vector3 mousePosition = Input.mousePosition;
        
        // Check if mouse is within the game screen bounds
        // If mouse is outside the screen, don't scroll
        if (mousePosition.x < 0 || mousePosition.x > Screen.width ||
            mousePosition.y < 0 || mousePosition.y > Screen.height)
        {
            return;
        }
        
        Vector3 moveDirection = Vector3.zero;
        
        // Check if mouse is at screen edges
        // Left edge
        if (mousePosition.x <= _edgeScrollZone)
        {
            moveDirection.x -= 1f;
        }
        // Right edge
        else if (mousePosition.x >= Screen.width - _edgeScrollZone)
        {
            moveDirection.x += 1f;
        }
        
        // Bottom edge
        if (mousePosition.y <= _edgeScrollZone)
        {
            moveDirection.y -= 1f;
        }
        // Top edge
        else if (mousePosition.y >= Screen.height - _edgeScrollZone)
        {
            moveDirection.y += 1f;
        }
        
        // Normalize diagonal movement
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }
        
        // Move camera
        if (moveDirection.magnitude > 0f)
        {
            Vector3 movement = moveDirection * _scrollSpeed * Time.deltaTime;
            transform.position += movement;
        }
    }

    /// <summary>
    /// Clamps the camera position to stay within the calculated bounds.
    /// </summary>
    private void ClampCameraPosition()
    {
        if (!_boundsCalculated)
        {
            return;
        }
        
        Vector3 position = transform.position;
        
        // Clamp X position
        position.x = Mathf.Clamp(position.x, _minX, _maxX);
        
        // Clamp Y position
        position.y = Mathf.Clamp(position.y, _minY, _maxY);
        
        // Keep Z position unchanged (camera depth)
        // position.z remains the same
        
        transform.position = position;
    }
    
    /// <summary>
    /// Manually recalculates camera bounds. Useful if the grid changes after initialization.
    /// Note: This is normally called automatically via the OnGridInitialized event.
    /// </summary>
    public void RecalculateBounds()
    {
        _boundsCalculated = false;
        CalculateCameraBounds();
    }
}

