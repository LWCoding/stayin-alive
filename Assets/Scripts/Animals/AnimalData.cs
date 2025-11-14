using UnityEngine;

/// <summary>
/// ScriptableObject that stores data for an animal type.
/// Contains name, visual assets, stats, and prefab reference.
/// </summary>
[CreateAssetMenu(fileName = "New Animal Data", menuName = "Animals/Animal Data")]
public class AnimalData : ScriptableObject
{
    [Header("Animal Info")]
    [Tooltip("Unique name identifier for this animal type")]
    public string animalName;

    [Header("Animation")]
    [Tooltip("First frame sprite for two-frame animation")]
    public Sprite frame1Sprite;
    
    [Tooltip("Second frame sprite for two-frame animation")]
    public Sprite frame2Sprite;
    
    [Tooltip("Time interval in seconds between frame switches for animation")]
    [Min(0.01f)]
    public float animationInterval = 0.5f;

    [Header("Prefab")]
    [Tooltip("Prefab to instantiate when spawning this animal")]
    public GameObject prefab;
}

