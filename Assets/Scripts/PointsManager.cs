using UnityEngine;

/// <summary>
/// Manages the player's readiness points (hibernation readiness).
/// Points increase when animals bring food back to dens.
/// </summary>
public class PointsManager : Singleton<PointsManager>
{
    private int _readinessPoints = 0;
    
    public int ReadinessPoints => _readinessPoints;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    /// <summary>
    /// Adds points to the readiness score.
    /// </summary>
    /// <param name="points">Number of points to add</param>
    /// <param name="allowNegative"> if true, allows negative values to be passed in order to deduct points </param>
    /// <returns>boolean representing if addition was successful</returns>
    public bool AddPoints(int points, bool allowNegative = false)
    {
        // I overloaded this horrifically so we didn't need multiple functions for it...
        // I put a default value though so it wouldn't break any other functionality that
        // depended on the positive nature of it
        if ((points <= 0 && !allowNegative) || _readinessPoints + points < 0)
        {
            return false;
        }
        
        _readinessPoints += points;
        Debug.Log($"PointsManager: Added {points} points. Total readiness: {_readinessPoints}");
        return true;
    }
    
    /// <summary>
    /// Resets the readiness points to zero.
    /// </summary>
    public void ResetPoints()
    {
        _readinessPoints = 0;
        
        // Reset game state through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameState();
        }
    }
}

