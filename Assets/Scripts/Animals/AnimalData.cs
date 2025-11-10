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

    [Header("Visual")]
    [Tooltip("Sprite to display when the animal is idle")]
    public Sprite idleSprite;

    [Header("Stats")]
    [Tooltip("Maximum hunger value for this animal")]
    public int maxHunger;

    [Tooltip("Maximum hydration value for this animal")]
    public int maxHydration;

    [Header("Prefab")]
    [Tooltip("Prefab to instantiate when spawning this animal")]
    public GameObject prefab;
}

