using UnityEngine;

/// <summary>
/// Butterfly that flies around randomly. Only moves when other animals are moving (during turns).
/// Does not occupy a grid position and can move freely in world space.
/// Spawning is handled by ButterflySpawner.
/// </summary>
public class Butterfly : WanderingAnimal
{
    // Butterfly behavior is handled by the WanderingAnimal base class.
    // Spawning is handled by ButterflySpawner utility class.
}
