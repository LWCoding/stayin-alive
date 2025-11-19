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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
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
        Time.timeScale = 0f;
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
        Time.timeScale = 1f;
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

        Time.timeScale = 1f;
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
        if (_isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}

