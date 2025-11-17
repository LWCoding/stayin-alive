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

    [Header("Hunger")]
    [Tooltip("Maximum hunger value. When hunger reaches 0, the animal dies. Hunger decreases by 1 each turn.")]
    [Min(1)]
    public int maxHunger = 100;
    
    [Tooltip("Hunger threshold below which the animal will seek food or hunt. When hunger is below this value, the animal will actively look for food.")]
    [Range(0, 100)]
    public int hungerThreshold = 70;
    
    [Tooltip("Critical hunger threshold (for prey animals). Below this value, the animal will seek food even when it's dangerous. Set to 0 if not applicable.")]
    [Range(0, 100)]
    public int criticalHungerThreshold = 20;
    
    [Header("Audio")]
    [Tooltip("Sound to play when this animal dies (AudioManager SFX type).")]
    public AudioManager.SFXType dyingSFX = AudioManager.SFXType.None;
    [Tooltip("Sound to play when this animal starts chasing a controllable target.")]
    public AudioManager.SFXType chasingSFX = AudioManager.SFXType.None;
}

