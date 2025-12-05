using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bee that flies around randomly within a specific radius of a center point.
/// Only moves when other animals are moving (during turns).
/// Does not occupy a grid position and can move freely in world space.
/// </summary>
public class Bee : WanderingAnimal
{
    [Header("Radius Settings")]
    [Tooltip("Maximum radius from center that the bee can fly (in world units)")]
    [SerializeField] private float _maxRadius = 3f;

    /// <summary>
    /// Sets the center position and maximum radius for this bee's movement.
    /// Should be called after instantiation to configure the bee's movement area.
    /// </summary>
    public void SetCenterAndRadius(Vector3 center, float radius)
    {
        _maxRadius = radius;
        SetMovementRadius(center, radius);
        
        // Set initial position close to the center (within 30% of radius to keep them nearby)
        Vector2 randomOffset = Random.insideUnitCircle * radius * 0.3f;
        transform.position = center + new Vector3(randomOffset.x, randomOffset.y, 0);
        _targetPosition = transform.position;
    }

    /// <summary>
    /// Override to ensure movement stays within radius of center.
    /// </summary>
    protected override void ChooseNewTarget()
    {
        // Choose a random direction
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float moveDistance = Random.Range(_minMoveDistance, _maxMoveDistance);
        
        Vector3 newTarget = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * moveDistance;
        
        // Ensure target stays within radius of center (primary constraint)
        Vector3 offsetFromCenter = newTarget - _centerPosition;
        float distanceFromCenter = offsetFromCenter.magnitude;
        
        if (distanceFromCenter > _maxRadius)
        {
            // Clamp to max radius - keep bee within its tree's radius
            offsetFromCenter = offsetFromCenter.normalized * _maxRadius;
            newTarget = _centerPosition + offsetFromCenter;
        }
        
        // Also ensure target is not on the border by checking grid position
        // But prioritize radius constraint over border constraint
        if (EnvironmentManager.Instance != null)
        {
            Vector2Int gridPos = EnvironmentManager.Instance.WorldToGridPosition(newTarget);
            Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
            
            // If on border, push inward
            if (gridPos.x <= 0 || gridPos.x >= gridSize.x - 1 || gridPos.y <= 0 || gridPos.y >= gridSize.y - 1)
            {
                // Clamp grid position to be at least 1 cell from each edge
                gridPos.x = Mathf.Clamp(gridPos.x, 1, gridSize.x - 2);
                gridPos.y = Mathf.Clamp(gridPos.y, 1, gridSize.y - 2);
                
                // Convert back to world position
                Vector3 borderClampedPos = EnvironmentManager.Instance.GridToWorldPosition(gridPos);
                
                // Re-check radius after grid clamping - if border clamping moved us outside radius, prefer radius constraint
                Vector3 borderOffset = borderClampedPos - _centerPosition;
                float borderDistance = borderOffset.magnitude;
                
                if (borderDistance <= _maxRadius)
                {
                    // Border clamping is within radius, use it
                    newTarget = borderClampedPos;
                }
                else
                {
                    // Border clamping would move us outside radius - stay within radius instead
                    // Find a position that's both within radius and not on border
                    // Try to move toward center while staying off border
                    Vector3 towardCenter = (_centerPosition - newTarget).normalized;
                    newTarget = _centerPosition + towardCenter * _maxRadius * 0.9f; // Use 90% of radius to ensure we're off border
                }
            }
        }
        
        _targetPosition = newTarget;
        
        // Update sprite facing direction based on movement
        UpdateFacingDirection(newTarget);
    }
}
