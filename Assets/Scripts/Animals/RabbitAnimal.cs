using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized prey animal that seeks out and eats grass when hungry.
/// Wanders randomly when not hungry, but will prioritize finding fully grown grass when hunger is below threshold.
/// </summary>
public class RabbitAnimal : PreyAnimal
{
	[Header("Rabbit Settings")]
	[Tooltip("Hunger threshold below which the rabbit will seek food. When hunger is below this value, the rabbit will look for grass.")]
	[SerializeField] private int _hungerThreshold = 50;
	[Tooltip("Detection radius for finding grass. Only grass within this distance will be considered.")]
	[SerializeField] private int _grassDetectionRadius = 10;
	[Tooltip("Target food type name. This is used for identification purposes.")]
	[SerializeField] private string _targetFood = "Grass";

	private Vector2Int? _foodDestination = null;
	private int _rabbitTurnCounter = 0;

	/// <summary>
	/// Override TakeTurn to add food-seeking behavior when hungry.
	/// Integrates with base PreyAnimal logic for fleeing and movement timing.
	/// </summary>
	public override void TakeTurn()
	{
		// Decrease hunger each turn (same as base Animal.TakeTurn)
		DecreaseHunger(1);
		
		_rabbitTurnCounter++;
		
		// Only move every other turn (on even turns: 2, 4, 6, etc.) - same as base PreyAnimal
		bool shouldMove = (_rabbitTurnCounter % 2 == 0);
		
		// First priority: Check for predators (fleeing takes precedence over food)
		PredatorAnimal nearestPredator = FindNearestPredator();
		
		if (nearestPredator != null)
		{
			// If we detect a predator, cancel food seeking and flee (only if it's a move turn)
			if (shouldMove)
			{
				_foodDestination = null;
				FleeFromPredator(nearestPredator);
			}
			return;
		}
		
		// Second priority: If hungry, seek food
		if (CurrentHunger < _hungerThreshold)
		{
			// Try to find and move towards fully grown grass
			Vector2Int? nearestGrass = FindNearestFullyGrownGrass();
			
			if (nearestGrass.HasValue)
			{
				_foodDestination = nearestGrass.Value;
				
				// Check if we're already at the grass position
				if (GridPosition == nearestGrass.Value)
				{
					// We're on the grass, try to eat it
					TryEatGrassAtCurrentPosition();
				}
				else if (shouldMove)
				{
					// Move towards the grass
					if (!MoveOneStepTowards(nearestGrass.Value))
					{
						// If move failed, try to find a new grass destination
						Vector2Int? newGrass = FindNearestFullyGrownGrass();
						if (newGrass.HasValue)
						{
							_foodDestination = newGrass.Value;
							MoveOneStepTowards(newGrass.Value);
						}
						else
						{
							_foodDestination = null;
						}
					}
				}
			}
			else
			{
				// No grass found, fall back to wandering
				_foodDestination = null;
				WanderIfShouldMove(shouldMove);
			}
		}
		else
		{
			// Not hungry, wander normally
			_foodDestination = null;
			WanderIfShouldMove(shouldMove);
		}
	}
	
	/// <summary>
	/// Handles wandering behavior when not seeking food or fleeing.
	/// </summary>
	private void WanderIfShouldMove(bool shouldMove)
	{
		if (!shouldMove)
		{
			return;
		}
		
		if (!_wanderingDestination.HasValue || GridPosition == _wanderingDestination.Value)
		{
			// Need a new wandering destination
			_wanderingDestination = ChooseWanderingDestination();
		}
		
		if (_wanderingDestination.HasValue)
		{
			// Check if we detect predators while wandering - if so, cancel wandering
			PredatorAnimal detectedPredator = FindNearestPredator();
			if (detectedPredator != null)
			{
				_wanderingDestination = null;
				FleeFromPredator(detectedPredator);
			}
			else
			{
				// Continue wandering
				if (!MoveOneStepTowards(_wanderingDestination.Value))
				{
					// If move failed, try to find a new destination
					_wanderingDestination = ChooseWanderingDestination();
					if (_wanderingDestination.HasValue)
					{
						MoveOneStepTowards(_wanderingDestination.Value);
					}
				}
			}
		}
	}
	
	/// <summary>
	/// Finds the nearest fully grown grass within detection radius.
	/// </summary>
	private Vector2Int? FindNearestFullyGrownGrass()
	{
		if (InteractableManager.Instance == null)
		{
			return null;
		}

		List<Grass> allGrasses = InteractableManager.Instance.Grasses;
		if (allGrasses == null || allGrasses.Count == 0)
		{
			return null;
		}

		Vector2Int myPos = GridPosition;
		Grass nearest = null;
		int bestDistance = int.MaxValue;

		for (int i = 0; i < allGrasses.Count; i++)
		{
			Grass grass = allGrasses[i];
			if (grass == null)
			{
				continue;
			}

			// Only consider grass that matches the target food type
			if (!DoesGrassMatchTargetFood(grass))
			{
				continue;
			}

			// Only consider fully grown grass
			if (!grass.IsFullyGrown())
			{
				continue;
			}

			Vector2Int grassPos = grass.GridPosition;
			int distance = Mathf.Abs(grassPos.x - myPos.x) + Mathf.Abs(grassPos.y - myPos.y); // Manhattan distance

			// Only consider grass within detection radius
			if (distance <= _grassDetectionRadius && distance < bestDistance)
			{
				bestDistance = distance;
				nearest = grass;
			}
		}

		return nearest != null ? nearest.GridPosition : (Vector2Int?)null;
	}

	/// <summary>
	/// Attempts to eat grass at the current position.
	/// If fully grown grass is found, changes it to growing state and restores hunger.
	/// </summary>
	private void TryEatGrassAtCurrentPosition()
	{
		if (InteractableManager.Instance == null)
		{
			return;
		}

		Grass grass = InteractableManager.Instance.GetGrassAtPosition(GridPosition);
		if (grass == null)
		{
			return;
		}

		// Only eat grass that matches the target food type
		if (!DoesGrassMatchTargetFood(grass))
		{
			return;
		}

		// Only eat fully grown grass
		if (!grass.IsFullyGrown())
		{
			return;
		}

		// Eat the grass (change it to growing state)
		HarvestGrass(grass);

		// Restore hunger based on the grass's food item prefab hunger restoration value
		int hungerRestored = grass.HungerRestored;
		if (hungerRestored > 0)
		{
			IncreaseHunger(hungerRestored);
			Debug.Log($"Rabbit '{name}' ate grass at ({GridPosition.x}, {GridPosition.y}). Hunger restored by {hungerRestored} to {CurrentHunger}.");
		}
		else
		{
			Debug.LogWarning($"Rabbit '{name}' ate grass at ({GridPosition.x}, {GridPosition.y}), but no hunger was restored (food item prefab not set or invalid).");
		}

		// Clear food destination since we've eaten
		_foodDestination = null;
	}

	/// <summary>
	/// Checks if a grass interactable matches the target food type.
	/// </summary>
	private bool DoesGrassMatchTargetFood(Grass grass)
	{
		if (grass == null || string.IsNullOrEmpty(_targetFood))
		{
			return false;
		}

		// Check if the grass type name matches the target food
		string grassTypeName = grass.GetType().Name;
		if (grassTypeName.Equals(_targetFood, System.StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Also check if the GameObject name contains the target food name
		if (grass.gameObject.name.Contains(_targetFood))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Harvests the grass (changes it from Full to Growing state).
	/// This simulates the rabbit eating the grass.
	/// </summary>
	private void HarvestGrass(Grass grass)
	{
		if (grass == null)
		{
			return;
		}

		grass.HarvestForAnimal();
	}

	/// <summary>
	/// Draws gizmos when the object is selected in the editor.
	/// Shows food destination and wandering destination.
	/// </summary>
	private void OnDrawGizmosSelected()
	{
		if (EnvironmentManager.Instance == null)
		{
			return;
		}

		Vector3 currentWorldPos = EnvironmentManager.Instance.GridToWorldPosition(GridPosition);

		// Draw wandering destination (from base PreyAnimal)
		if (_wanderingDestination.HasValue)
		{
			Vector3 wanderWorldPos = EnvironmentManager.Instance.GridToWorldPosition(_wanderingDestination.Value);
			Gizmos.color = Color.cyan; // Cyan for wandering
			Gizmos.DrawLine(currentWorldPos, wanderWorldPos);
			Gizmos.DrawWireSphere(wanderWorldPos, 0.3f);
		}

		// Draw food destination if we have one
		if (_foodDestination.HasValue)
		{
			Vector3 foodWorldPos = EnvironmentManager.Instance.GridToWorldPosition(_foodDestination.Value);

			// Draw line from current position to food destination
			Gizmos.color = Color.green;
			Gizmos.DrawLine(currentWorldPos, foodWorldPos);

			// Draw food destination point
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(foodWorldPos, 0.25f);
		}
	}
}
