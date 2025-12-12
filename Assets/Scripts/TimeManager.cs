using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

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
	[Tooltip("Image that displays the current season icon.")]
	[SerializeField] private Image _seasonImage;
	[Tooltip("Text that displays the current season name.")]
	[SerializeField] private TextMeshProUGUI _seasonText;
	
	[Header("Post-Processing")]
	[Tooltip("Global post-processing volume to modify based on season.")]
	[SerializeField] private Volume _postProcessingVolume;
	
	[Header("Season Sprites")]
	[SerializeField] private Sprite _springSprite;
	[SerializeField] private Sprite _summerSprite;
	[SerializeField] private Sprite _fallSprite;
	[SerializeField] private Sprite _winterSprite;

	private int _playerTurnCount = 0;
	private Season _currentSeason = Season.Spring;
	private bool _isPaused = false;
	private bool _waitingForFirstPlayerMove = false;
	private bool _pauseLockedForFirstMove = false;
	private ColorAdjustments _colorAdjustments;

	/// <summary>
	/// Invoked after each turn advance (including resets via <see cref="ResetTimerAndPauseForFirstMove"/>).
	/// Provides the current player turn count.
	/// </summary>
	public event Action<int> OnTurnAdvanced;

	public enum Season
	{
		Spring,
		Summer,
		Fall,
		Winter
	}

	private void Start()
	{
		InitializePostProcessing();
		ResetTimerAndPauseForFirstMove();
	}

	/// <summary>
	/// Initializes the post-processing volume and gets the ColorAdjustments override.
	/// </summary>
	private void InitializePostProcessing()
	{
		if (_postProcessingVolume == null)
		{
			// Try to find a global volume if not assigned
			Volume[] volumes = FindObjectsOfType<Volume>();
			foreach (Volume volume in volumes)
			{
				if (volume.isGlobal)
				{
					_postProcessingVolume = volume;
					break;
				}
			}

			if (_postProcessingVolume == null)
			{
				Debug.LogWarning("TimeManager: No global post-processing volume found. Season color changes will not be applied.");
				return;
			}
		}

		// Get or add ColorAdjustments override
		if (_postProcessingVolume.profile != null)
		{
			if (!_postProcessingVolume.profile.TryGet<ColorAdjustments>(out _colorAdjustments))
			{
				// If ColorAdjustments doesn't exist, add it
				_colorAdjustments = _postProcessingVolume.profile.Add<ColorAdjustments>();
			}

			if (_colorAdjustments != null)
			{
				_colorAdjustments.active = true;
			}
		}
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
		UpdateSeasonImage();
		UpdateSeasonText();
		UpdatePostProcessingColors();
		
		// Play initial Spring season SFX
		PlaySeasonChangeSFX(Season.Spring);

		OnTurnAdvanced?.Invoke(_playerTurnCount);
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
      // Get world coordinate for brain spawn - it'll appear behind the season I think but that's okay
      if (Camera.main != null)
      {
        // Want to gain season knowledge after the season finishes
        KnowledgeManager.Instance.LearnKnowledgeData(_currentSeason.ToString(), Camera.main.ScreenToWorldPoint(_seasonImage.transform.position));
      }
      
			// Handle Winter grass reduction when switching to Winter
			if (newSeason == Season.Winter && InteractableManager.Instance != null)
			{
				InteractableManager.Instance.HandleWinterGrassReduction();
			}

			_currentSeason = newSeason;
			UpdateSeasonImage();
			UpdateSeasonText();
			UpdatePostProcessingColors();
			
			// Play season change SFX
			PlaySeasonChangeSFX(newSeason);
		}

		// Update progress bar to show progress through current season
		UpdateProgressBar();

		// Advance time for all animals (non-controllable animals take their turn)
		if (AnimalManager.Instance == null)
		{
			Debug.LogWarning("TimeManager: AnimalManager instance not found. Cannot advance time.");
			OnTurnAdvanced?.Invoke(_playerTurnCount);
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

			// Skip unassigned workers - they should not execute any logic until assigned to a den
			if (WorkerManager.Instance != null && WorkerManager.Instance.GetWorkerAssignmentStatus(animal) == WorkerManager.WorkerAssignmentStatus.UNASSIGNED)
			{
				continue;
			}

			animal.TakeTurn();

			if (animal != null)
			{
				AnimalManager.Instance.ResolveTileConflictsForAnimal(animal);
			}
		}

		// After all animals have taken their turn, clear selection so no animal keeps brighter pathing
		AnimalManager.Instance.ClearSelection();

		// Update fog of war after animals have moved
		if (FogOfWarManager.Instance != null)
		{
			FogOfWarManager.Instance.UpdateFogOfWar();
		}

		OnTurnAdvanced?.Invoke(_playerTurnCount);
	}

	private void UpdateSeasonImage()
	{
		if (_seasonImage == null)
		{
			return;
		}

		Sprite seasonSprite = null;
		switch (_currentSeason)
		{
			case Season.Spring:
				seasonSprite = _springSprite;
				break;
			case Season.Summer:
				seasonSprite = _summerSprite;
				break;
			case Season.Fall:
				seasonSprite = _fallSprite;
				break;
			case Season.Winter:
				seasonSprite = _winterSprite;
				break;
		}

		_seasonImage.sprite = seasonSprite;
		_seasonImage.enabled = seasonSprite != null;
	}

	/// <summary>
	/// Updates the season text to display the current season's name.
	/// </summary>
	private void UpdateSeasonText()
	{
		if (_seasonText == null)
		{
			return;
		}

		_seasonText.text = _currentSeason.ToString();
	}

	/// <summary>
	/// Updates the post-processing volume colors based on the current season.
	/// </summary>
	private void UpdatePostProcessingColors()
	{
		if (_colorAdjustments == null || _postProcessingVolume == null)
		{
			return;
		}

		Color seasonColorFilter = Color.white;
		float seasonSaturation = 0f;
		
		switch (_currentSeason)
		{
			case Season.Spring:
				// Fresh, bright greens and light colors
				seasonColorFilter = new Color(0.95f, 1.0f, 0.9f, 1.0f); // Slightly green tint
				seasonSaturation = 0f; // Normal saturation
				break;
			case Season.Summer:
				// Warm, vibrant yellows and oranges
				seasonColorFilter = new Color(1.0f, 0.98f, 0.92f, 1.0f); // Warm yellow tint
				seasonSaturation = 20f; // Slightly increased saturation
				break;
			case Season.Fall:
				// Warm oranges and browns
				seasonColorFilter = new Color(1.0f, 0.92f, 0.85f, 1.0f); // Orange/brown tint
				seasonSaturation = 0f; // Normal saturation
				break;
			case Season.Winter:
				// Cool blues and grays - desaturated for a muted, cold look
				seasonColorFilter = new Color(0.9f, 0.95f, 1.0f, 1.0f); // Cool blue tint
				seasonSaturation = -20f; // Reduced saturation for muted look
				break;
		}

		// Apply color filter with moderate intensity
		_colorAdjustments.colorFilter.overrideState = true;
		_colorAdjustments.colorFilter.value = seasonColorFilter;
		
		// Apply saturation adjustment
		_colorAdjustments.saturation.overrideState = true;
		_colorAdjustments.saturation.value = seasonSaturation;
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

	/// <summary>
	/// Plays the appropriate season SFX when a season changes.
	/// The previous season SFX will fade out before the new one plays.
	/// </summary>
	private void PlaySeasonChangeSFX(Season season)
	{
		if (AudioManager.Instance == null)
		{
			return;
		}

		AudioManager.SFXType seasonSFX = GetSeasonSFXType(season);
		if (seasonSFX != AudioManager.SFXType.None)
		{
			AudioManager.Instance.PlaySeasonSFX(seasonSFX);
		}
	}

	/// <summary>
	/// Converts a TimeManager Season to the corresponding AudioManager SFXType.
	/// </summary>
	private AudioManager.SFXType GetSeasonSFXType(Season season)
	{
		switch (season)
		{
			case Season.Spring:
				return AudioManager.SFXType.Spring;
			case Season.Summer:
				return AudioManager.SFXType.Summer;
			case Season.Fall:
				return AudioManager.SFXType.Fall;
			case Season.Winter:
				return AudioManager.SFXType.Winter;
			default:
				return AudioManager.SFXType.None;
		}
	}
}


