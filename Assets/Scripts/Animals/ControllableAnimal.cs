using UnityEngine;

/// <summary>
/// A controllable animal that advances one step along its planned path each turn.
/// </summary>
public class ControllableAnimal : Animal
{
    public override void TakeTurn()
    {
        // For controllable animals, follow their assigned pathing one step per turn.
        AdvanceOneStepAlongPlannedPath();
        ApplyTurnNeedsAndTileRestoration();
    }
}


