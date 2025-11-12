using UnityEngine;

/// <summary>
/// Data structure representing a particle effect configuration.
/// Contains the sprite, direction, and fade delay for a particle effect.
/// </summary>
[System.Serializable]
public struct ParticleEffectData
{
    [Tooltip("Sprite to use for this particle effect")]
    public Sprite sprite;
    
    [Tooltip("Base direction vector for particle spray (will be randomized around this)")]
    public Vector2 direction;
    
    [Tooltip("Time in seconds before the particle starts fading out")]
    [Min(0f)]
    public float fadeDelay;
}

