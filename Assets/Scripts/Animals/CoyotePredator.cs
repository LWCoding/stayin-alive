using UnityEngine;

/// <summary>
/// Specialized predator that takes a break after chasing prey for too long without eating.
/// </summary>
public class CoyotePredator : PredatorAnimal
{
    [Header("Coyote Settings")]
    [Tooltip("Number of turns the coyote will chase before taking a break if it hasn't eaten.")]
    [SerializeField] private int _chaseTurnsBeforeBreak = 8;
    [Tooltip("Number of turns the coyote will rest when taking a break.")]
    [SerializeField] private int _breakDuration = 2;

    private int _chaseTurnsWithoutEating = 0;
    private bool _justAteThisTurn = false;

    /// <summary>
    /// Determines whether this predator should hunt based on its current hunger level.
    /// Reads the hunger threshold from AnimalData.
    /// At critical hunger, always hunts. Below normal hunger threshold, hunts normally.
    /// </summary>
    protected override bool ShouldHuntBasedOnHunger()
    {
        if (AnimalData == null)
        {
            return true; // Default to always hunt if no data
        }
        // Hunt if critically hungry OR below normal hunger threshold
        return CurrentHunger < AnimalData.hungerThreshold || IsCriticallyHungry();
    }

    /// <summary>
    /// Override to track successful hunts and reset chase counter.
    /// </summary>
    protected override void TryHuntAtCurrentPosition()
    {
        bool hadHuntingDestination = _huntingDestination.HasValue;
        
        // Call base implementation
        base.TryHuntAtCurrentPosition();
        
        // If we successfully hunted (stall was activated), mark that we just ate
        if (_isEatingStallActive && hadHuntingDestination)
        {
            _justAteThisTurn = true;
        }
    }

    /// <summary>
    /// After the standard predator logic runs, track chase duration and take breaks if needed.
    /// </summary>
    protected override void OnStandardTurnComplete()
    {
        base.OnStandardTurnComplete();

        // If we just ate this turn, reset the chase counter
        if (_justAteThisTurn)
        {
            _chaseTurnsWithoutEating = 0;
            _justAteThisTurn = false;
            return;
        }

        // If we're currently chasing (have a hunting destination) and not stalled
        if (_huntingDestination.HasValue && _stallTurnsRemaining == 0)
        {
            _chaseTurnsWithoutEating++;
            
            // If we've been chasing too long without eating, take a break
            if (_chaseTurnsWithoutEating > _chaseTurnsBeforeBreak)
            {
                _stallTurnsRemaining = _breakDuration;
                _chaseTurnsWithoutEating = 0; // Reset counter after taking break
            }
        }
        else if (!_huntingDestination.HasValue)
        {
            // No longer chasing, reset counter
            _chaseTurnsWithoutEating = 0;
        }
    }
}

