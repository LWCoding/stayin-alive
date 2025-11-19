using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized worker animal (Kangaroo Rat) that seeks out and eats grass when hungry.
/// Wanders randomly when not hungry, but will prioritize finding fully grown grass when hunger is below threshold.
/// Below the standard hunger threshold, will only seek food if no predators are nearby.
/// Below the critical hunger threshold, will seek food regardless of predators to avoid starvation.
/// Workers can carry food and deposit it at their home den to increase the player's food-in-den amount.
/// </summary>
public class KangRatWorker : WorkerAnimal
{
	[Header("Kangaroo Rat Settings")]

	private Vector2Int? _foodDestination = null;
	private int _kangRatTurnCounter = 0;

	/// <summary>
	/// Gets whether this kangaroo rat is hungry (hunger below threshold).
	/// Reads the hunger threshold from AnimalData.
	/// </summary>
	public bool IsHungry => AnimalData != null && CurrentHunger < AnimalData.hungerThreshold;


	/// <summary>
	/// Gets the hunger threshold below which the kangaroo rat will seek food.
	/// Reads from AnimalData.
	/// </summary>
	public int HungerThreshold => AnimalData != null ? AnimalData.hungerThreshold : 70;

	/// <summary>
	/// Gets the critical hunger threshold below which the kangaroo rat will seek food regardless of predators.
	/// Reads from AnimalData.
	/// </summary>
	public int CriticalHungerThreshold => AnimalData != null ? AnimalData.criticalHungerThreshold : 20;


	/// <summary>
	/// Override TakeTurn to add food-seeking behavior when hungry.
	/// Integrates with base PreyAnimal logic for fleeing and movement timing.
	/// </summary>
	public override void TakeTurn()
	{
		// Decrease hunger each turn (same as base Animal.TakeTurn)
		DecreaseHunger(1);
		
		// Check hunger levels (read from AnimalData)
		int hungerThreshold = AnimalData != null ? AnimalData.hungerThreshold : 70;
		int criticalHungerThreshold = AnimalData != null ? AnimalData.criticalHungerThreshold : 20;
		bool isCriticallyHungry = CurrentHunger < criticalHungerThreshold;
		bool isHungry = CurrentHunger < hungerThreshold;
		
		// If we're hiding in our den and critically hungry, force exit immediately
		if (IsHidingInHome)
		{
			if (isCriticallyHungry)
			{
				// Force exit from den - critical hunger takes priority over safety
				ForceExitFromHome();
				Debug.Log($"KangRatWorker '{name}' forced to exit den due to critical hunger (hunger: {CurrentHunger} < threshold: {criticalHungerThreshold})");
			}
			else if (!isHungry)
			{
				// Not hungry, stay hidden in den
				return;
			}
			// If standard hungry (but not critical), check for predators before exiting
			else
			{
				PredatorAnimal nearbyPredator = FindNearestPredator();
				if (nearbyPredator == null)
				{
					// No predators nearby, safe to exit and look for food
					ForceExitFromHome();
					Debug.Log($"KangRatWorker '{name}' exiting den to seek food (hunger: {CurrentHunger} < threshold: {hungerThreshold})");
				}
				else
				{
					// Predators nearby, stay hidden even if hungry (but not critical)
					return;
				}
			}
		}
		
		_kangRatTurnCounter++;
		
		// If not hungry, always try to return to and stay in the den
		if (!isHungry)
		{
			if (HasHomeHideable)
			{
				// If we're at the den, hide in it
				if (IsAtHomeHideable)
				{
					TryHideInHome();
					return;
				}
				// If we're not at the den, move towards it
				else 
                {
					// Move one step towards the den
					Vector2Int homePos = HomeHideable.GridPosition;
					if (MoveOneStepTowards(homePos))
					{
						// Check if we reached the den
						if (IsAtHomeHideable)
						{
							TryHideInHome();
						}
					}
					return;
				}
			}
			// If no den assigned, just stay put (shouldn't happen, but safety check)
			return;
		}
		
		// If hungry, check if we should seek food
		if (isHungry)
		{
			// Check for nearby predators
			PredatorAnimal nearbyPredator = FindNearestPredator();
			
			// Only seek food if critically hungry OR if no predators are nearby
			if (isCriticallyHungry || nearbyPredator == null)
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
					else 
                    {
						// Move towards the grass (even if predators are nearby when critically hungry)
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
					// No grass found, fall back to wandering (still prioritize food search)
					_foodDestination = null;
					Wander();
				}
			}
			else
			{
				// Standard hungry but predators nearby - prioritize fleeing to den over food
				_foodDestination = null;
				TryFleeToHome(nearbyPredator);
			}
		}
	}
	
	/// <summary>
	/// Handles wandering behavior when not seeking food or fleeing.
	/// </summary>
	private void Wander()
	{
		if (!_wanderingDestination.HasValue || GridPosition == _wanderingDestination.Value)
		{
			// Need a new wandering destination
			_wanderingDestination = ChooseWanderingDestination();
		}
		
		if (_wanderingDestination.HasValue)
		{
			// Check if we detect predators while wandering - if so, cancel wandering and flee to den
			PredatorAnimal detectedPredator = FindNearestPredator();
			if (detectedPredator != null)
			{
				_wanderingDestination = null;
				TryFleeToHome(detectedPredator);
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
	/// Override to choose wandering destinations around the home den.
	/// Similar to how predators wander around their den.
	/// </summary>
	protected override Vector2Int? ChooseWanderingDestination()
	{
		if (HasHomeHideable && EnvironmentManager.Instance != null)
		{
			return ChooseWanderingDestinationAroundHome();
		}
		
		// Fallback: use base class wandering behavior if no home is assigned
		return base.ChooseWanderingDestination();
	}

	/// <summary>
	/// Chooses a wandering destination around the home location (for den workers).
	/// </summary>
	private Vector2Int? ChooseWanderingDestinationAroundHome()
	{
		if (!HasHomeHideable || EnvironmentManager.Instance == null)
		{
			return null;
		}

		Vector2Int homePos = HomeHideable.GridPosition;
		int maxWanderDistance = 8; // Maximum distance to wander from home
		int minWanderDistance = 2; // Minimum distance to wander from home

		// Try to find a random position around the home
		for (int attempts = 0; attempts < 30; attempts++)
		{
			// Pick a random direction and distance
			int distance = Random.Range(minWanderDistance, maxWanderDistance + 1);
			int angle = Random.Range(0, 360);

			// Convert angle to direction
			float radians = angle * Mathf.Deg2Rad;
			int dx = Mathf.RoundToInt(Mathf.Cos(radians) * distance);
			int dy = Mathf.RoundToInt(Mathf.Sin(radians) * distance);

			Vector2Int targetPos = homePos + new Vector2Int(dx, dy);

			// Clamp to grid bounds
			Vector2Int gridSize = EnvironmentManager.Instance.GetGridSize();
			targetPos.x = Mathf.Clamp(targetPos.x, 0, gridSize.x - 1);
			targetPos.y = Mathf.Clamp(targetPos.y, 0, gridSize.y - 1);

			// Check if this position is valid and walkable
			if (IsTileTraversable(targetPos))
			{
				return targetPos;
			}
		}

		return null;
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
			Debug.Log($"KangRatWorker '{name}' ate grass at ({GridPosition.x}, {GridPosition.y}). Hunger restored by {hungerRestored} to {CurrentHunger}.");
		}
		else
		{
			Debug.LogWarning($"KangRatWorker '{name}' ate grass at ({GridPosition.x}, {GridPosition.y}), but no hunger was restored (food item prefab not set or invalid).");
		}

		// Clear food destination since we've eaten
		_foodDestination = null;
	}


	/// <summary>
	/// Harvests the grass (changes it from Full to Growing state).
	/// This simulates the kangaroo rat eating the grass.
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
	/// Gets the kangaroo rat's intended destination (food or wandering).
	/// Returns null if no destination is set.
	/// </summary>
	public Vector2Int? GetIntendedDestination()
	{
		// Prioritize food destination over wandering destination
		if (_foodDestination.HasValue)
		{
			return _foodDestination.Value;
		}
		
		if (_wanderingDestination.HasValue)
		{
			return _wanderingDestination.Value;
		}
		
		return null;
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

		// Draw line to home den if we have one
		if (HasHomeHideable)
		{
			Vector3 homeWorldPos = EnvironmentManager.Instance.GridToWorldPosition(HomeHideable.GridPosition);
			Gizmos.color = Color.magenta; // Magenta for den home
			Gizmos.DrawLine(currentWorldPos, homeWorldPos);
			Gizmos.DrawWireSphere(homeWorldPos, 0.2f);
		}
	}
}


