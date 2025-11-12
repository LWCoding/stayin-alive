using UnityEngine;

/// <summary>
/// Manages the overall game state and initialization.
/// Handles loading the starting level when the game begins.
/// Manages win/lose conditions and displays appropriate screens.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("Level Settings")]
    [Tooltip("Starting level file name (without .txt extension) from Levels/ folder")]
    [SerializeField] private string _startingLevel = "level1";
    
    [Header("References")]
    [Tooltip("LevelLoader component. If not assigned, will try to find it in the scene.")]
    [SerializeField] private LevelLoader _levelLoader;
    
    [Header("Game End UI")]
    [Tooltip("UI GameObject that displays the win screen. Should be inactive by default.")]
    [SerializeField] private GameObject _winScreen;
    
    [Tooltip("UI GameObject that displays the lose screen. Should be inactive by default.")]
    [SerializeField] private GameObject _loseScreen;
    
    private bool _hasWon = false;
    private bool _hasLost = false;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
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
    
    /// <summary>
    /// Checks if the player has met the win condition (readiness points >= max food count).
    /// Called by PointsManager when points change.
    /// </summary>
    /// <param name="readinessPoints">Current readiness points</param>
    /// <param name="maxFoodCount">Maximum food count required to win</param>
    public void CheckWinCondition(int readinessPoints, int maxFoodCount)
    {
        // Only check if we haven't already won or lost and max food count is set
        if (_hasWon || _hasLost || maxFoodCount <= 0)
        {
            return;
        }
        
        // Check if quota is met
        if (readinessPoints >= maxFoodCount)
        {
            TriggerWin();
        }
    }
    
    /// <summary>
    /// Triggers the win condition. Shows win screen and pauses time.
    /// </summary>
    public void TriggerWin()
    {
        if (_hasWon || _hasLost)
        {
            return;
        }
        
        _hasWon = true;
        
        // Show win screen
        if (_winScreen != null)
        {
            _winScreen.SetActive(true);
            Debug.Log("GameManager: Win condition met! Showing win screen.");
        }
        else
        {
            Debug.LogWarning("GameManager: Win condition met, but win screen UI element is not assigned!");
        }
        
        // Pause time
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Pause();
        }
        else
        {
            Debug.LogWarning("GameManager: Win condition met, but TimeManager instance not found!");
        }
    }
    
    /// <summary>
    /// Triggers the lose condition when a controllable animal dies.
    /// Shows lose screen and pauses time.
    /// </summary>
    public void TriggerLose()
    {
        if (_hasWon || _hasLost)
        {
            return;
        }
        
        _hasLost = true;
        
        // Show lose screen
        if (_loseScreen != null)
        {
            _loseScreen.SetActive(true);
            Debug.Log("GameManager: Lose condition triggered! Showing lose screen.");
        }
        else
        {
            Debug.LogWarning("GameManager: Lose condition triggered, but lose screen UI element is not assigned!");
        }
        
        // Pause time
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Pause();
        }
        else
        {
            Debug.LogWarning("GameManager: Lose condition triggered, but TimeManager instance not found!");
        }
    }
    
    /// <summary>
    /// Resets the game state. Hides win/lose screens and resumes time.
    /// </summary>
    public void ResetGameState()
    {
        _hasWon = false;
        _hasLost = false;
        
        // Hide win screen if it was shown
        if (_winScreen != null)
        {
            _winScreen.SetActive(false);
        }
        
        // Hide lose screen if it was shown
        if (_loseScreen != null)
        {
            _loseScreen.SetActive(false);
        }
        
        // Resume time if it was paused
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.ResetTimerAndPauseForFirstMove();
        }
    }
}

