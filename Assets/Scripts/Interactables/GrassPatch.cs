using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An interactable that occupies a 3x3 area and contains 9 grass interactables as child objects.
/// Clears any existing interactables in its area before placing.
/// </summary>
public class GrassPatch : Interactable
{
    [Header("Grass Patch Settings")]
    [Tooltip("Size of the patch (width and height in grid tiles)")]
    [SerializeField] private int _patchSize = 3;
    
    private List<Grass> _childGrass = new List<Grass>();
    
    /// <summary>
    /// Gets the size of this grass patch (width and height in grid tiles).
    /// </summary>
    public int PatchSize => _patchSize;
    
    /// <summary>
    /// Gets all grid positions occupied by this grass patch.
    /// </summary>
    public List<Vector2Int> GetOccupiedPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        int halfSize = _patchSize / 2;
        
        // Center the patch around the grid position
        for (int x = _gridPosition.x - halfSize; x <= _gridPosition.x + halfSize; x++)
        {
            for (int y = _gridPosition.y - halfSize; y <= _gridPosition.y + halfSize; y++)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Initializes the grass patch at the specified grid position.
    /// This is the center position of the 3x3 patch.
    /// </summary>
    public override void Initialize(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
        
        UpdateWorldPosition();
        
        // Clear any existing interactables in the patch area
        ClearInteractablesInArea();
        
        // Initialize child grass objects that are already in the prefab
        InitializeChildGrass();
    }
    
    private void UpdateWorldPosition()
    {
        if (EnvironmentManager.Instance != null)
        {
            Vector3 worldPos = EnvironmentManager.Instance.GridToWorldPosition(_gridPosition);
            transform.position = worldPos;
        }
        else
        {
            transform.position = new Vector3(_gridPosition.x, _gridPosition.y, transform.position.z);
        }
    }
    
    /// <summary>
    /// Clears all existing interactables within the patch area.
    /// </summary>
    private void ClearInteractablesInArea()
    {
        if (InteractableManager.Instance == null)
        {
            Debug.LogWarning("GrassPatch: InteractableManager instance not found! Cannot clear interactables in area.");
            return;
        }
        
        List<Vector2Int> occupiedPositions = GetOccupiedPositions();
        
        foreach (Vector2Int pos in occupiedPositions)
        {
            // Check if position is valid
            if (EnvironmentManager.Instance != null && !EnvironmentManager.Instance.IsValidPosition(pos))
            {
                continue;
            }
            
            // Get and remove any interactable at this position
            Interactable existingInteractable = InteractableManager.Instance.GetInteractableAtPosition(pos);
            if (existingInteractable != null && existingInteractable != this)
            {
                // Remove from manager and destroy
                InteractableManager.Instance.RemoveInteractable(existingInteractable);
                Destroy(existingInteractable.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Initializes child grass objects that are already in the prefab.
    /// Positions them at the correct grid positions and registers them with InteractableManager.
    /// </summary>
    private void InitializeChildGrass()
    {
        if (InteractableManager.Instance == null)
        {
            Debug.LogWarning("GrassPatch: InteractableManager instance not found! Cannot initialize child grass.");
            return;
        }
        
        if (EnvironmentManager.Instance == null)
        {
            Debug.LogWarning("GrassPatch: EnvironmentManager instance not found! Cannot initialize child grass.");
            return;
        }
        
        // Find all Grass components in child objects (exclude self)
        Grass[] childGrassArray = GetComponentsInChildren<Grass>();
        _childGrass = new List<Grass>();
        
        // Filter out the GrassPatch's own Grass component if it has one (it shouldn't, but be safe)
        foreach (Grass grass in childGrassArray)
        {
            if (grass != null && grass.transform != transform && grass.transform.IsChildOf(transform))
            {
                _childGrass.Add(grass);
            }
        }
        
        List<Vector2Int> occupiedPositions = GetOccupiedPositions();
        
        // Initialize each grass child at its corresponding grid position
        for (int i = 0; i < _childGrass.Count && i < occupiedPositions.Count; i++)
        {
            Vector2Int pos = occupiedPositions[i];
            Grass grass = _childGrass[i];
            
            if (grass == null)
            {
                continue;
            }
            
            // Check if position is valid
            if (!EnvironmentManager.Instance.IsValidPosition(pos))
            {
                continue;
            }
            
            // Check if tile is water or obstacle
            TileType tileType = EnvironmentManager.Instance.GetTileType(pos);
            if (tileType == TileType.Water || tileType == TileType.Obstacle)
            {
                continue;
            }
            
            // Initialize the grass at this grid position
            grass.Initialize(pos);
            
            // Register with InteractableManager (manually register since these are prefab children)
            InteractableManager.Instance.RegisterGrass(grass);
        }
        
        Debug.Log($"GrassPatch: Initialized {_childGrass.Count} child grass interactables in patch at ({_gridPosition.x}, {_gridPosition.y})");
    }
    
    private void OnDestroy()
    {
        // Clean up child grass when patch is destroyed
        // The grass children will be destroyed automatically as part of the parent GameObject
        // But we should remove them from InteractableManager's tracking
        foreach (Grass grass in _childGrass)
        {
            if (grass != null && InteractableManager.Instance != null)
            {
                InteractableManager.Instance.RemoveGrass(grass);
            }
        }
        _childGrass.Clear();
        
        if (InteractableManager.Instance != null)
        {
            InteractableManager.Instance.RemoveGrassPatch(this);
        }
    }
}
