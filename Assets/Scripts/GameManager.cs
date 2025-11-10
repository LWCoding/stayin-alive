using UnityEngine;

/// <summary>
/// Manages the overall game state and initialization.
/// Handles loading the starting level when the game begins.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("Starting level file name (without .txt extension) from Levels/ folder")]
    [SerializeField] private string _startingLevel = "level1";
    
    [Header("References")]
    [Tooltip("LevelLoader component. If not assigned, will try to find it in the scene.")]
    [SerializeField] private LevelLoader _levelLoader;
    
    private void Start()
    {
        // Find LevelLoader if not assigned
        if (_levelLoader == null)
        {
            _levelLoader = FindObjectOfType<LevelLoader>();
        }
        
        // Check if LevelLoader exists
        if (_levelLoader == null)
        {
            Debug.LogError("GameManager: LevelLoader not found! Please ensure a LevelLoader component exists in the scene.");
            return;
        }
        
        // Load the starting level
        if (string.IsNullOrEmpty(_startingLevel))
        {
            Debug.LogWarning("GameManager: Starting level is not set! Please assign a level name in the inspector.");
            return;
        }
        
        Debug.Log($"GameManager: Loading starting level: {_startingLevel}");
        _levelLoader.LoadAndApplyLevel(_startingLevel);
    }
}

