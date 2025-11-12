using UnityEngine;
using TMPro;

/// <summary>
/// Manages the player's readiness points (hibernation readiness).
/// Points increase when animals bring food back to dens.
/// </summary>
public class PointsManager : Singleton<PointsManager>
{
    [Header("UI")]
    [SerializeField] [Tooltip("TextMeshProUGUI component to display the readiness points")]
    private TextMeshProUGUI _pointsText;
    
    [SerializeField] [Tooltip("UI GameObject that displays the win screen. Should be inactive by default.")]
    private GameObject _winScreen;
    
    [Header("Settings")]
    [SerializeField] [Tooltip("Format string for displaying points. Use {0} for current points and {1} for maximum food count.")]
    private string _pointsFormat = "{0}/{1}";
    
    private int _readinessPoints = 0;
    private int _maxFoodCount = 0;
    private bool _hasWon = false;
    
    public int ReadinessPoints => _readinessPoints;
    public int MaxFoodCount => _maxFoodCount;
    
    protected override void Awake()
    {
        base.Awake();
        UpdatePointsDisplay();
    }
    
    /// <summary>
    /// Adds points to the readiness score.
    /// </summary>
    /// <param name="points">Number of points to add</param>
    public void AddPoints(int points)
    {
        if (points <= 0)
        {
            return;
        }
        
        _readinessPoints += points;
        UpdatePointsDisplay();
        Debug.Log($"PointsManager: Added {points} points. Total readiness: {_readinessPoints}");
        
        CheckWinCondition();
    }
    
    /// <summary>
    /// Sets the maximum food count for the current level.
    /// This should be called when a level is loaded.
    /// </summary>
    /// <param name="maxFoodCount">Total number of food items in the level</param>
    public void SetMaxFoodCount(int maxFoodCount)
    {
        _maxFoodCount = maxFoodCount;
        UpdatePointsDisplay();
        Debug.Log($"PointsManager: Set maximum food count to {maxFoodCount}");
        
        CheckWinCondition();
    }
    
    /// <summary>
    /// Resets the readiness points to zero.
    /// </summary>
    public void ResetPoints()
    {
        _readinessPoints = 0;
        _hasWon = false;
        UpdatePointsDisplay();
        
        // Hide win screen if it was shown
        if (_winScreen != null)
        {
            _winScreen.SetActive(false);
        }
        
        // Resume time if it was paused
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Resume();
        }
    }
    
    /// <summary>
    /// Updates the UI text to display the current points.
    /// </summary>
    private void UpdatePointsDisplay()
    {
        if (_pointsText != null)
        {
            _pointsText.text = string.Format(_pointsFormat, _readinessPoints, _maxFoodCount);
        }
    }
    
    /// <summary>
    /// Checks if the player has met the win condition (readiness points >= max food count).
    /// If so, shows the win screen and pauses time.
    /// </summary>
    private void CheckWinCondition()
    {
        // Only check if we haven't already won and max food count is set
        if (_hasWon || _maxFoodCount <= 0)
        {
            return;
        }
        
        // Check if quota is met
        if (_readinessPoints >= _maxFoodCount)
        {
            _hasWon = true;
            
            // Show win screen
            if (_winScreen != null)
            {
                _winScreen.SetActive(true);
                Debug.Log("PointsManager: Win condition met! Showing win screen.");
            }
            else
            {
                Debug.LogWarning("PointsManager: Win condition met, but win screen UI element is not assigned!");
            }
            
            // Pause time
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.Pause();
            }
            else
            {
                Debug.LogWarning("PointsManager: Win condition met, but TimeManager instance not found!");
            }
        }
    }
}

