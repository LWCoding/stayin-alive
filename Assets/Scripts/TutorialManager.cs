using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the tutorial scene state and initialization.
/// Handles loading the tutorial level when the tutorial begins.
/// This should only be placed in the tutorial scene.
/// </summary>
public class TutorialManager : Singleton<TutorialManager>
{
    [Header("Game End UI")]
    [Tooltip("UI GameObject that displays the win screen. Should be inactive by default.")]
    [SerializeField] private GameObject _winScreen;
    
    [Tooltip("UI GameObject that displays the lose screen. Should be inactive by default.")]
    [SerializeField] private GameObject _loseScreen;
    
    private TutorialLevelLoader _tutorialLevelLoader;

    private bool _hasWon = false;
    private bool _hasLost = false;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Start()
    {
        _tutorialLevelLoader = FindObjectOfType<TutorialLevelLoader>();
        // Load level if tutorial level loader exists
        if (_tutorialLevelLoader != null)
        {
            Debug.Log("TutorialManager: Loading tutorial level");
            _tutorialLevelLoader.LoadAndApplyLevel();
        }
        else
        {
            Debug.LogWarning("TutorialManager: TutorialLevelLoader not found in scene! Tutorial level will not be loaded.");
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
            Debug.Log("TutorialManager: Lose condition triggered! Showing lose screen.");
        }
        else
        {
            Debug.LogWarning("TutorialManager: Lose condition triggered, but lose screen UI element is not assigned!");
        }
        
        // Pause time
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Pause();
        }
        else
        {
            Debug.LogWarning("TutorialManager: Lose condition triggered, but TimeManager instance not found!");
        }
    }
    
    /// <summary>
    /// Resets the tutorial state. Hides win/lose screens and resumes time.
    /// </summary>
    public void ResetTutorialState()
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
    
    private void Update()
    {
        // Check for Escape key press to return to title screen
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("TutorialManager: Escape key pressed. Returning to title screen.");
            SceneManager.LoadScene("01_Title");
        }
    }
}

