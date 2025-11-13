using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages turn-based time progression. Time advances when the player makes a move.
/// Controllable animals will move only one step along their stored pathing (if any).
/// </summary>
public class TimeManager : Singleton<TimeManager>
{
	[Header("Season Settings")]
	[Tooltip("Number of player turns before changing to the next season")]
	[SerializeField] private int _turnsPerSeason = 50;

	[Header("UI")]
	[Tooltip("Progress bar Image that shows progress through the current season. Scales from 0 (start of season) to 1 (end of season).")]
	[SerializeField] private Image _progressBarImage;

	private int _playerTurnCount = 0;
	private Season _currentSeason = Season.Spring;
	private bool _isPaused = false;
	private bool _waitingForFirstPlayerMove = false;
	private bool _pauseLockedForFirstMove = false;

	public enum Season
	{
		Spring,
		Summer,
		Fall,
		Winter
	}

	private void Start()
	{
		ResetTimerAndPauseForFirstMove();
	}

	/// <summary>
	/// Resets the turn count and season, pauses time progression, and waits for the player's first move.
	/// Call this when starting a new level or restarting the game.
	/// </summary>
	public void ResetTimerAndPauseForFirstMove()
	{
		_playerTurnCount = 0;
		_currentSeason = Season.Spring;
		_waitingForFirstPlayerMove = true;
		_pauseLockedForFirstMove = true;
		_isPaused = true;
		UpdateProgressBar();
	}

	/// <summary>
	/// Notifies the time manager that the player has made a move.
	/// Advances time by one turn.
	/// </summary>
	public void NotifyPlayerMoved()
	{
		// Handle first move unlock
		if (_pauseLockedForFirstMove)
		{
			_pauseLockedForFirstMove = false;
			_waitingForFirstPlayerMove = false;

			if (_isPaused)
			{
				Resume();
			}
		}

		// Advance time for this move
		NextTurn();
	}

	/// <summary>
	/// Advances time by one turn.
	/// - Increments player turn count
	/// - Updates season if needed (every 50 turns)
	/// - All animals take their turn (AI animals move, controllable animals don't move here as they already moved)
	/// </summary>
	public void NextTurn()
	{
		// Increment player turn count
		_playerTurnCount++;

		// Update season if needed (every 50 turns)
		int newSeasonIndex = _playerTurnCount / _turnsPerSeason;
		Season newSeason = (Season)(newSeasonIndex % 4);
		if (newSeason != _currentSeason)
		{
			_currentSeason = newSeason;
		}

		// Update progress bar to show progress through current season
		UpdateProgressBar();

		// Advance time for all animals (non-controllable animals take their turn)
		if (AnimalManager.Instance == null)
		{
			Debug.LogWarning("TimeManager: AnimalManager instance not found. Cannot advance time.");
			return;
		}

		List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
		for (int i = 0; i < animals.Count; i++)
		{
			Animal animal = animals[i];
			if (animal == null)
			{
				continue;
			}

			// Skip controllable animals - they already moved when the player moved
			if (animal.IsControllable)
			{
				continue;
			}

			animal.TakeTurn();
		}

		// After all animals have taken their turn, clear selection so no animal keeps brighter pathing
		AnimalManager.Instance.ClearSelection();

		// Update fog of war after animals have moved
		if (FogOfWarManager.Instance != null)
		{
			FogOfWarManager.Instance.UpdateFogOfWar();
		}
	}

	/// <summary>
	/// Updates the progress bar to show progress through the current season.
	/// The bar scales from 0 (start of season) to 1 (end of season).
	/// </summary>
	private void UpdateProgressBar()
	{
		if (_progressBarImage == null)
		{
			return;
		}

		// Calculate progress through current season (0 to 1)
		// Turns into current season = remainder when dividing by turns per season
		int turnsIntoCurrentSeason = _playerTurnCount % _turnsPerSeason;
		float progress = (float)turnsIntoCurrentSeason / _turnsPerSeason;

		// Clamp to ensure it's between 0 and 1
		float scale = Mathf.Clamp01(progress);

		// Update the Image's scale (using localScale on the RectTransform)
		// Scale only the X axis (width) from 0 to 1, preserving Y and Z
		RectTransform rectTransform = _progressBarImage.rectTransform;
		if (rectTransform != null)
		{
			rectTransform.localScale = new Vector3(scale, 1f, 1f);
		}
	}

	/// <summary>
	/// Advances time by one turn. Kept for backwards compatibility.
	/// </summary>
	public void AdvanceTime()
	{
		NextTurn();
	}

	/// <summary>
	/// Pauses time progression. When paused, turns will not occur.
	/// </summary>
	public void Pause()
	{
		_isPaused = true;
	}

	/// <summary>
	/// Resumes time progression.
	/// </summary>
	public void Resume()
	{
		_isPaused = false;
	}

	/// <summary>
	/// Returns whether time is currently paused.
	/// </summary>
	public bool IsPaused => _isPaused;

	/// <summary>
	/// Returns whether the timer is waiting for the player's first move before starting.
	/// </summary>
	public bool IsWaitingForFirstMove => _waitingForFirstPlayerMove;

	/// <summary>
	/// Returns the current number of turns the player has made.
	/// </summary>
	public int PlayerTurnCount => _playerTurnCount;

	/// <summary>
	/// Returns the current season.
	/// </summary>
	public Season CurrentSeason => _currentSeason;
}


