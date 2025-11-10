using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages turn-based time progression. Each call to AdvanceTime() makes animals take a turn.
/// Controllable animals will move only one step along their stored pathing (if any).
/// </summary>
public class TimeManager : Singleton<TimeManager>
{
	[Header("UI")]
	[SerializeField] private Button _advanceTurnButton;

	private void OnEnable()
	{
		if (_advanceTurnButton != null)
		{
			_advanceTurnButton.onClick.AddListener(OnAdvanceButtonClicked);
		}
	}

	private void OnDisable()
	{
		if (_advanceTurnButton != null)
		{
			_advanceTurnButton.onClick.RemoveListener(OnAdvanceButtonClicked);
		}
	}

	private void OnAdvanceButtonClicked()
	{
		if (_advanceTurnButton != null)
		{
			_advanceTurnButton.interactable = false;
		}

		AdvanceTime();
		StartCoroutine(ReenableButtonAfterDelay(0.75f));
	}

	private IEnumerator ReenableButtonAfterDelay(float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		if (_advanceTurnButton != null)
		{
			_advanceTurnButton.interactable = true;
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
	}
}


