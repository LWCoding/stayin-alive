using UnityEngine;

/// <summary>
/// Specialized predator that can prepare a straight-line dash when it spots prey directly ahead.
/// </summary>
public class HawkPredator : PredatorAnimal
{
    [Header("Hawk Settings")]
    [Tooltip("Number of tiles to dash forward once prey is spotted in a straight line ahead.")]
    [SerializeField] private int _dashDistance = 3;

    private Vector2Int? _pendingDashDirection = null;
    private Vector2Int _lastFacingDirection = Vector2Int.up;

    /// <summary>
    /// Determines whether this predator should hunt based on its current hunger level.
    /// Reads the hunger threshold from AnimalData.
    /// </summary>
    protected override bool ShouldHuntBasedOnHunger()
    {
        if (AnimalData == null)
        {
            return true; // Default to always hunt if no data
        }
        return CurrentHunger < AnimalData.hungerThreshold;
    }

    /// <summary>
    /// Executes the hawk's dash if one is queued up from a previous turn.
    /// Only dashes if actively hunting (hungry enough).
    /// </summary>
    protected override bool TryPerformSpecialTurnAction()
    {
        if (!_pendingDashDirection.HasValue)
        {
            return false;
        }

        // If not hunting due to hunger, cancel the pending dash and return false
        if (!ShouldHuntBasedOnHunger())
        {
            _pendingDashDirection = null;
            return false;
        }

        Vector2Int dashDirection = _pendingDashDirection.Value;
        _pendingDashDirection = null;

        ExecuteDash(dashDirection);
        return true;
    }

    /// <summary>
    /// After the standard predator logic runs, check if we can queue up a dash for next turn.
    /// Only prepares dash if actively hunting (hungry enough).
    /// </summary>
    protected override void OnStandardTurnComplete()
    {
        base.OnStandardTurnComplete();

        EnsureMinimumStallTurns(1);

        if (_pendingDashDirection.HasValue)
        {
            return;
        }

        // Only prepare dash if actively hunting
        if (ShouldHuntBasedOnHunger())
        {
            TryPrepareDash();
        }
    }

    private void ExecuteDash(Vector2Int direction)
    {
        if (direction == Vector2Int.zero || EnvironmentManager.Instance == null)
        {
            return;
        }

        // Cancel any wandering/hunting destinations - dash takes priority.
        _wanderingDestination = null;
        _huntingDestination = null;

        for (int step = 0; step < _dashDistance; step++)
        {
            Vector2Int nextGrid = GridPosition + direction;

            if (!EnvironmentManager.Instance.IsValidPosition(nextGrid))
            {
                break;
            }

            if (!EnvironmentManager.Instance.IsWalkable(nextGrid))
            {
                break;
            }

            Animal blockingAnimal = GetAnimalAtPosition(nextGrid);
            if (blockingAnimal != null && !IsValidDashTarget(blockingAnimal))
            {
                break;
            }

            SetGridPosition(nextGrid);

            if (blockingAnimal != null && IsValidDashTarget(blockingAnimal))
            {
                TryHuntAtCurrentPosition();
                EnsureMinimumStallTurns(1);
                return;
            }
        }

        // If no prey was encountered, still attempt to hunt in case something moved into our tile.
        TryHuntAtCurrentPosition();
        EnsureMinimumStallTurns(1);
    }

    private bool TryPrepareDash()
    {
        if (EnvironmentManager.Instance == null)
        {
            return false;
        }

        Vector2Int facingDirection = GetFacingDirection();
        if (facingDirection == Vector2Int.zero)
        {
            return false;
        }

        if (!HasDashTargetAhead(facingDirection))
        {
            return false;
        }

        _pendingDashDirection = facingDirection;
        return true;
    }

    private bool HasDashTargetAhead(Vector2Int direction)
    {
        if (AnimalManager.Instance == null)
        {
            return false;
        }

        for (int distance = 1; distance <= DetectionRadius; distance++)
        {
            Vector2Int checkPos = GridPosition + direction * distance;

            if (!EnvironmentManager.Instance.IsValidPosition(checkPos) ||
                !EnvironmentManager.Instance.IsWalkable(checkPos))
            {
                break;
            }

            Animal animal = GetAnimalAtPosition(checkPos);
            if (animal == null)
            {
                continue;
            }

            if (!IsValidDashTarget(animal))
            {
                // Another animal is blocking the line - no dash.
                break;
            }

            return true;
        }

        return false;
    }

    private bool IsValidDashTarget(Animal target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.IsControllable && (Den.IsControllableAnimalInDen(target) || Bush.IsControllableAnimalInBush(target)))
        {
            return false;
        }

        return IsValidTarget(target);
    }

    private Vector2Int GetFacingDirection()
    {
        Vector2Int delta = GridPosition - PreviousGridPosition;
        if (delta != Vector2Int.zero)
        {
            delta.x = Mathf.Clamp(delta.x, -1, 1);
            delta.y = Mathf.Clamp(delta.y, -1, 1);
            if (delta != Vector2Int.zero)
            {
                _lastFacingDirection = delta;
            }
        }

        return _lastFacingDirection;
    }

    protected override bool ShouldShowTrackingIndicator()
    {
        if (IsEatingStallActive)
        {
            return false;
        }

        // Only show indicator when actively hunting (hungry enough)
        if (!ShouldHuntBasedOnHunger())
        {
            return false;
        }

        Vector2Int? detected = FindNearestPreyGrid();
        return detected.HasValue;
    }

    private void EnsureMinimumStallTurns(int minTurns)
    {
        if (_stallTurnsRemaining < minTurns)
        {
            _stallTurnsRemaining = minTurns;
        }
    }
}

