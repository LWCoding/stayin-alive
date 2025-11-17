using UnityEngine;

/// <summary>
/// Interface for objects that can hide animals (e.g., bushes, dens).
/// When an animal enters a hideable, it becomes hidden from view.
/// </summary>
public interface IHideable
{
    /// <summary>
    /// The grid position of this hideable object.
    /// </summary>
    Vector2Int GridPosition { get; }

    /// <summary>
    /// Called when an animal enters this hideable location.
    /// </summary>
    void OnAnimalEnter(Animal animal);

    /// <summary>
    /// Called when an animal leaves this hideable location.
    /// </summary>
    void OnAnimalLeave(Animal animal);

    /// <summary>
    /// Checks if the specified animal is currently in this hideable location.
    /// </summary>
    bool IsAnimalInHideable(Animal animal);
}

