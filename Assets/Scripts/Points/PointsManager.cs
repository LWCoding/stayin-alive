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
    
    [Header("Settings")]
    [SerializeField] [Tooltip("Format string for displaying points. Use {0} for current points and {1} for maximum food count.")]
    private string _pointsFormat = "{0}/{1}";
    
    private int _readinessPoints = 0;
    private int _maxFoodCount = 0;
    
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
        
        // Notify GameManager to check win condition
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckWinCondition(_readinessPoints, _maxFoodCount);
        }
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
        
        // Notify GameManager to check win condition
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckWinCondition(_readinessPoints, _maxFoodCount);
        }
    }
    
    /// <summary>
    /// Resets the readiness points to zero.
    /// </summary>
    public void ResetPoints()
    {
        _readinessPoints = 0;
        UpdatePointsDisplay();
        
        // Reset game state through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
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
}

