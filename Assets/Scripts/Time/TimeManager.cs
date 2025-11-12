using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages turn-based time progression. Automatically advances time every X seconds.
/// Controllable animals will move only one step along their stored pathing (if any).
/// </summary>
public class TimeManager : Singleton<TimeManager>
{
	[Header("Turn Settings")]
	[Tooltip("Time in seconds between automatic turn advances")]
	[SerializeField] private float _turnInterval = 3f;

	[Header("UI")]
	[Tooltip("Progress bar Image that shows time remaining. Should scale from 1 (at turnInterval) to 0 (at 0 seconds).")]
	[SerializeField] private Image _progressBarImage;

	private float _timeUntilNextTurn;
	private bool _isPaused = false;

	private void Start()
	{
		_timeUntilNextTurn = _turnInterval;
	}

	private void Update()
	{
		if (_isPaused)
		{
			return;
		}

		// Update timer
		_timeUntilNextTurn -= Time.deltaTime;

		// Update progress bar visual
		UpdateProgressBar();

		// Advance turn when timer reaches zero
		if (_timeUntilNextTurn <= 0f)
		{
			AdvanceTime();
			_timeUntilNextTurn = _turnInterval;
		}
	}

	private void UpdateProgressBar()
	{
		if (_progressBarImage == null)
		{
			return;
		}

		// Calculate scale: 1 when _turnInterval seconds remaining, 0 when 0 seconds remaining
		// Show progress bar for the full turn interval
		float timeRemaining = _timeUntilNextTurn;
		float scale = 0f;

		if (timeRemaining <= _turnInterval)
		{
			// Scale from 1 (at _turnInterval seconds) to 0 (at 0 seconds)
			scale = Mathf.Clamp01(timeRemaining / _turnInterval);
		}

		// Update the Image's scale (using localScale on the RectTransform)
		// Scale only the X axis (width) from 1 to 0, preserving Y and Z
		RectTransform rectTransform = _progressBarImage.rectTransform;
		if (rectTransform != null)
		{
			rectTransform.localScale = new Vector3(scale, 1f, 1f);
		}
	}

	/// <summary>
	/// Advances time by one turn.
	/// - Controllable animals move a single grid step along their planned path (if set and valid).
	/// - Non-controllable animals currently do nothing (hook for AI turns).
	/// </summary>
	public void AdvanceTime()
	{
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
}


