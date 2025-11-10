using UnityEngine;

/// <summary>
/// Component attached to animal prefabs. Represents an animal in the game world.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Animal : MonoBehaviour
{
    [Header("Animal Info")]
    [HideInInspector] [SerializeField] private int _animalId;
    [HideInInspector] [SerializeField] private Vector2Int _gridPosition;

    private SpriteRenderer _spriteRenderer;

    public int AnimalId => _animalId;
    public Vector2Int GridPosition => _gridPosition;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Initializes the animal with an ID and grid position.
    /// </summary>
    public void Initialize(int animalId, Vector2Int gridPosition)
    {
        _animalId = animalId;
        _gridPosition = gridPosition;
        UpdateWorldPosition();
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
    /// Sets the animal's sprite.
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = sprite;
        }
    }
}

