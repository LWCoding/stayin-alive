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

    /// <summary>
    /// Maximum number of follower GameObjects to spawn per animal for visuals.
    /// </summary>
    public static int MaxFollowerVisuals = 5;

    /// <summary>
    /// Seconds between automatic NextTurn() calls when animals are in a den.
    /// </summary>
    public static float DenTimeProgressionDelay = 0.5f;

    /// <summary>
    /// Radius in grid cells for fog of war visibility around controllable animals.
    /// </summary>
    public static int FogOfWarRadius = 5;

    /// <summary>
    /// Minimum number of rabbits to spawn initially at a rabbit spawner.
    /// </summary>
    public static int RabbitSpawnMinCount = 3;

    /// <summary>
    /// Maximum number of rabbits to spawn initially at a rabbit spawner.
    /// </summary>
    public static int RabbitSpawnMaxCount = 6;

    /// <summary>
    /// Radius in grid cells for rabbits to detect predators.
    /// Used for both fleeing behavior and determining when to hide/emerge from spawners.
    /// </summary>
    public static int RabbitPredatorDetectionRadius = 7;

    /// <summary>
    /// Goal number of assigned workers required to achieve the MVP milestone.
    /// </summary>
    public static int MvpWorkerGoal = 50;

    /// <summary>
    /// Maximum number of workers that can be assigned to a single den.
    /// </summary>
    public static int MaxWorkersPerDen = 5;

    /// <summary>
    /// Maximum number of inventory slots.
    /// </summary>
    public static int MaxInventorySize = 5;

    /// <summary>
    /// Maximum number of sticks required to place a den.
    /// Scales up from 1 until it reaches this value.
    /// </summary>
    public static int MaxDenStickCost = 3;

	/// <summary>
	/// Hunger restored when a worker eats stored food from the den.
	/// </summary>
	public static int DenFoodHungerRestoration = 50;

  public static string GRASS_ITEM_NAME_FOR_WORKER_HARDCODE = "Grass";
}

