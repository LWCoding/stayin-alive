using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the tutorial scene state and initialization.
/// Handles loading the tutorial level when the tutorial begins.
/// This should only be placed in the tutorial scene.
/// </summary>
public class TutorialManager : Singleton<TutorialManager>
{

    private TutorialLevelLoader _tutorialLevelLoader;
    
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
    /// Resets the tutorial state. Hides win/lose screens and resumes time.
    /// </summary>
    public void ResetTutorialState()
    {
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

