using UnityEngine;

/// <summary>
/// Base class for all interactable objects in the game.
/// Provides common functionality like grid position tracking.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    [Header("Interactable Settings")]
    [HideInInspector]
    protected Vector2Int _gridPosition;
    
    /// <summary>
    /// Grid position of this interactable.
    /// </summary>
    public Vector2Int GridPosition => _gridPosition;
    
    /// <summary>
    /// Initializes the interactable at the specified grid position.
    /// Must be implemented by derived classes.
    /// </summary>
    public abstract void Initialize(Vector2Int gridPosition);
}

