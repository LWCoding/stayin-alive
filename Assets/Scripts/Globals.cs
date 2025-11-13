/// <summary>
/// Global constants and settings used throughout the game.
/// </summary>
public static class Globals
{
    /// <summary>
    /// Default seconds to interpolate when moving one grid cell.
    /// </summary>
    public static float MoveDurationSeconds = 0.2f;

    /// <summary>
    /// Base delay in seconds for the first follower, increases for each subsequent follower.
    /// </summary>
    public static float BaseFollowerDelay = 0.1f;

    /// <summary>
    /// Delay increment per follower.
    /// </summary>
    public static float FollowerDelayIncrement = 0.1f;

    /// <summary>
    /// Scale multiplier for followers (0.7 = 70% of original size).
    /// </summary>
    public static float FollowerScale = 0.7f;
}

