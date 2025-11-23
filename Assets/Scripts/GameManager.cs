using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the overall game state and initialization.
/// Handles loading the starting level when the game begins.
/// Manages lose conditions and displays appropriate screens.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("Game End UI")]
    [Tooltip("UI GameObject that displays the win screen. Should be inactive by default.")]
    [SerializeField] private GameObject _winScreen;
    
    [Tooltip("UI GameObject that displays the lose screen. Should be inactive by default.")]
    [SerializeField] private GameObject _loseScreen;
    
    [Header("Scene Transitions")]
    [Tooltip("Time in seconds to wait after showing win/lose screen before switching scenes.")]
    [SerializeField] private float _sceneTransitionDelay = 1.5f;
    
    [Tooltip("Scene name to load after winning the game.")]
    [SerializeField] private string _winSceneName = "05_Cutscene_2";
    
    [Tooltip("Scene name to load after losing the game.")]
    [SerializeField] private string _loseSceneName = "06_Cutscene_3";
    
    private ProceduralLevelLoader _proceduralLevelLoader;

    private bool _hasWon = false;
    private bool _hasLost = false;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Start()
    {
        _proceduralLevelLoader = FindObjectOfType<ProceduralLevelLoader>();
        // Load level if procedural level loader exists
        if (_proceduralLevelLoader != null)
        {
            _proceduralLevelLoader.LoadAndApplyLevel();
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
        
        // Wait for delay then switch to lose cutscene
        StartCoroutine(WaitAndSwitchToScene(_loseSceneName, _sceneTransitionDelay));
    }
    
    /// <summary>
    /// Triggers the win condition when the MVP goal is reached.
    /// Shows win screen and pauses time.
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
            Debug.Log("GameManager: Win condition triggered! Showing win screen.");
        }
        else
        {
            Debug.LogWarning("GameManager: Win condition triggered, but win screen UI element is not assigned!");
        }
        
        // Pause time
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Pause();
        }
        else
        {
            Debug.LogWarning("GameManager: Win condition triggered, but TimeManager instance not found!");
        }
        
        // Wait for delay then switch to win cutscene
        StartCoroutine(WaitAndSwitchToScene(_winSceneName, _sceneTransitionDelay));
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
    
    /// <summary>
    /// Waits for the specified duration then switches to the given scene using ScreenWipe.
    /// Uses the same pattern as SwitchSceneTo.
    /// </summary>
    private IEnumerator WaitAndSwitchToScene(string sceneName, float waitTime)
    {
        // Wait for the specified duration (using unscaled time since game is paused)
        yield return new WaitForSecondsRealtime(waitTime);
        
        // Switch scene using the same pattern as SwitchSceneTo
        if (ScreenWipe.Instance != null)
        {
            ScreenWipe.Instance.WipeToScene(sceneName);
        }
        else
        {
            Debug.LogError("GameManager: ScreenWipe instance not found!");
            SceneManager.LoadScene(sceneName);
        }
    }
}

