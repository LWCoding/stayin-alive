using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private string titleSceneName = "01_Title";

    private bool _isPaused;

    private void Awake()
    {
        ResumeGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("PauseManager: Escape key pressed");
            if (_isPaused)
            {
                Debug.Log("PauseManager: Resuming game");
                ResumeGame();
            }
            else
            {
                Debug.Log("PauseManager: Pausing game");
                PauseGame();
            }
        }

        if (_isPaused && Input.GetKeyDown(KeyCode.R))
        {
            ReturnToTitle();
        }
    }

    private void PauseGame()
    {
        _isPaused = true;
        
        // Pause TimeManager to prevent turn progression
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Pause();
        }
        
        if (pauseScreen != null)
        {
            pauseScreen.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PauseManager: Pause screen GameObject not assigned.");
        }
    }

    private void ResumeGame()
    {
        _isPaused = false;
        
        // Resume TimeManager, but only if it's not waiting for first move
        // (TimeManager handles its own first move pause state)
        if (TimeManager.Instance != null && !TimeManager.Instance.IsWaitingForFirstMove)
        {
            TimeManager.Instance.Resume();
        }
        
        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }
    }

    private void ReturnToTitle()
    {
        if (string.IsNullOrWhiteSpace(titleSceneName))
        {
            Debug.LogWarning("PauseManager: Title scene name not assigned.");
            return;
        }

        if (ScreenWipe.Instance != null)
        {
            ScreenWipe.Instance.WipeToScene(titleSceneName);
        }
        else
        {
            Debug.LogError("ScreenWipe instance not found!");
            SceneManager.LoadScene(titleSceneName);
        }
    }

    private void OnDisable()
    {
        // Ensure TimeManager is resumed if we're disabled while paused
        if (_isPaused && TimeManager.Instance != null && !TimeManager.Instance.IsWaitingForFirstMove)
        {
            TimeManager.Instance.Resume();
        }
    }
}

