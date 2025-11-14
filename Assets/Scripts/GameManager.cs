using UnityEngine;

/// <summary>
/// Manages the overall game state and initialization.
/// Handles loading the starting level when the game begins.
/// Manages lose conditions and displays appropriate screens.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("References")]
    [Tooltip("ProceduralLevelLoader component. If not assigned, will try to find it in the scene.")]
    [SerializeField] private ProceduralLevelLoader _proceduralLevelLoader;
    
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
        // Find ProceduralLevelLoader if not assigned
        if (_proceduralLevelLoader == null)
        {
            _proceduralLevelLoader = FindObjectOfType<ProceduralLevelLoader>();
        }
        
        // Check if ProceduralLevelLoader exists
        if (_proceduralLevelLoader == null)
        {
            Debug.LogError("GameManager: ProceduralLevelLoader not found! Please ensure a ProceduralLevelLoader component exists in the scene.");
            return;
        }
        
        // Generate and load a procedurally generated level
        Debug.Log("GameManager: Generating procedurally generated level");
        _proceduralLevelLoader.LoadAndApplyLevel();
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

