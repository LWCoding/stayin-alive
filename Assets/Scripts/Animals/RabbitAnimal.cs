using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized prey animal that seeks out and eats grass when hungry.
/// Wanders randomly when not hungry, but will prioritize finding fully grown grass when hunger is below threshold.
/// Below the standard hunger threshold, rabbits will only seek food if no predators are nearby.
/// Below the critical hunger threshold, rabbits will seek food regardless of predators to avoid starvation.
/// </summary>
public class RabbitAnimal : PreyAnimal
{
	[Header("Rabbit Settings")]
	[Tooltip("Hunger threshold below which the rabbit will seek food. When hunger is below this value, the rabbit will look for grass (only if no predators are nearby).")]
	[SerializeField] private int _hungerThreshold = 70;
	[Tooltip("Critical hunger threshold below which the rabbit will seek food regardless of predators. Below this value, survival takes priority over safety.")]
	[SerializeField] private int _criticalHungerThreshold = 20;
	[Tooltip("Detection radius for finding grass. Only grass within this distance will be considered.")]
	[SerializeField] private int _grassDetectionRadius = 10;
	[Tooltip("Target food type name. This is used for identification purposes.")]
	[SerializeField] private string _targetFood = "Grass";

	private Vector2Int? _foodDestination = null;
	private int _rabbitTurnCounter = 0;
	private RabbitSpawner _rabbitSpawner = null;

	/// <summary>
	/// Gets the rabbit spawner this rabbit belongs to.
	/// </summary>
	public RabbitSpawner RabbitSpawner => _rabbitSpawner;

	/// <summary>
	/// Gets whether this rabbit is hungry (hunger below threshold).
	/// </summary>
	public bool IsHungry => CurrentHunger < _hungerThreshold;

	/// <summary>
	/// Gets whether this rabbit is at critical hunger (hunger below critical threshold).
	/// At critical hunger, the rabbit will seek food regardless of predators.
	/// </summary>
	public bool IsCriticallyHungry => CurrentHunger < _criticalHungerThreshold;

	/// <summary>
	/// Gets the hunger threshold below which the rabbit will seek food.
	/// </summary>
	public int HungerThreshold => _hungerThreshold;

	/// <summary>
	/// Gets the critical hunger threshold below which the rabbit will seek food regardless of predators.
	/// </summary>
	public int CriticalHungerThreshold => _criticalHungerThreshold;

	/// <summary>
	/// Sets the rabbit spawner this rabbit belongs to.
	/// Used when spawning rabbits near specific spawners.
	/// </summary>
	public void SetRabbitSpawner(RabbitSpawner spawner)
	{
		_rabbitSpawner = spawner;
	}

	private void Start()
	{
		// Find the nearest rabbit spawner to this rabbit's spawn position
		FindNearestRabbitSpawner();
	}

	/// <summary>
	/// Finds the nearest rabbit spawner to this rabbit and associates with it.
	/// </summary>
	private void FindNearestRabbitSpawner()
	{
		if (InteractableManager.Instance == null)
		{
			return;
		}

		// Skip if already linked to a spawner
		if (_rabbitSpawner != null)
		{
			return;
		}

		List<RabbitSpawner> allSpawners = InteractableManager.Instance.RabbitSpawners;
		if (allSpawners == null || allSpawners.Count == 0)
		{
			return;
		}

		Vector2Int myPos = GridPosition;
		RabbitSpawner nearest = null;
		int bestDistance = int.MaxValue;

		foreach (RabbitSpawner spawner in allSpawners)
		{
			if (spawner == null)
			{
				continue;
			}

			Vector2Int spawnerPos = spawner.GridPosition;
			int distance = Mathf.Abs(spawnerPos.x - myPos.x) + Mathf.Abs(spawnerPos.y - myPos.y);

			if (distance < bestDistance)
			{
				bestDistance = distance;
				nearest = spawner;
			}
		}

		if (nearest != null)
		{
			_rabbitSpawner = nearest;
			Debug.Log($"RabbitAnimal '{name}' associated with rabbit spawner at ({nearest.GridPosition.x}, {nearest.GridPosition.y})");
		}
	}

	/// <summary>
	/// Override TakeTurn to add food-seeking behavior when hungry.
	/// Integrates with base PreyAnimal logic for fleeing and movement timing.
	/// </summary>
	public override void TakeTurn()
	{
		// Decrease hunger each turn (same as base Animal.TakeTurn)
		DecreaseHunger(1);
		
		// Check hunger levels
		bool isCriticallyHungry = CurrentHunger < _criticalHungerThreshold;
		bool isHungry = CurrentHunger < _hungerThreshold;
		
		// If we're hiding in our spawner and critically hungry, force exit immediately
		if (CurrentHideable is RabbitSpawner)
		{
			if (isCriticallyHungry)
			{
				// Force exit from spawner - critical hunger takes priority over safety
				ForceExitFromSpawner();
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
					ForceExitFromSpawner();
				}
				else
				{
					// Predators nearby, stay hidden even if hungry (but not critical)
					return;
				}
			}
		}
		
		_rabbitTurnCounter++;
		
		// Only move every other turn (on even turns: 2, 4, 6, etc.) - same as base PreyAnimal
		bool shouldMove = (_rabbitTurnCounter % 2 == 0);
		
		// If not hungry, always try to return to and stay in the spawner
		if (!isHungry)
		{
			if (_rabbitSpawner != null)
			{
				// If we're at the spawner, hide in it
				if (shouldMove && GridPosition == _rabbitSpawner.GridPosition)
				{
					TryHideInSpawner();
					return;
				}
				// If we're not at the spawner, move towards it
				else if (shouldMove)
				{
					// Move one step towards the spawner
					if (MoveOneStepTowards(_rabbitSpawner.GridPosition))
					{
						// Check if we reached the spawner
						if (GridPosition == _rabbitSpawner.GridPosition)
						{
							TryHideInSpawner();
						}
					}
					return;
				}
			}
			// If no spawner assigned, just stay put (shouldn't happen, but safety check)
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
					else if (shouldMove)
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
					WanderIfShouldMove(shouldMove);
				}
			}
			else
			{
				// Standard hungry but predators nearby - prioritize fleeing over food
				_foodDestination = null;
				if (shouldMove)
				{
					FleeFromPredator(nearbyPredator);
				}
			}
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
	/// Override to choose wandering destinations within the spawner's territory.
	/// Similar to how predators wander around their den.
	/// </summary>
	protected override Vector2Int? ChooseWanderingDestination()
	{
		// If we have a spawner, wander within its territory
		if (_rabbitSpawner != null && EnvironmentManager.Instance != null)
		{
			// Try multiple times to get a valid territory position that accounts for water walkability
			for (int attempts = 0; attempts < 10; attempts++)
			{
				Vector2Int? territoryPos = _rabbitSpawner.GetRandomPositionInTerritory();
				if (territoryPos.HasValue && IsTileTraversable(territoryPos.Value))
				{
					return territoryPos.Value;
				}
			}
		}
		
		// Fallback: use base class wandering behavior if no spawner is assigned or no valid territory position found
		return base.ChooseWanderingDestination();
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
	/// Attempts to hide in the rabbit spawner when at its position.
	/// </summary>
	private void TryHideInSpawner()
	{
		if (_rabbitSpawner == null)
		{
			return;
		}

		if (GridPosition == _rabbitSpawner.GridPosition)
		{
			_rabbitSpawner.OnAnimalEnter(this);
		}
	}

	/// <summary>
	/// Forces the rabbit to exit from the spawner immediately.
	/// Used when the rabbit is hungry and needs to leave the den to find food.
	/// </summary>
	private void ForceExitFromSpawner()
	{
		if (_rabbitSpawner == null || CurrentHideable != _rabbitSpawner)
		{
			return;
		}

		// Call OnAnimalLeave to properly remove from hiding list and make visible
		_rabbitSpawner.OnAnimalLeave(this);
		
		// Ensure we're visible and no longer hiding
		SetVisualVisibility(true);
		SetCurrentHideable(null);
		
		bool isCriticallyHungry = CurrentHunger < _criticalHungerThreshold;
		string reason = isCriticallyHungry ? "critical hunger" : "hunger";
		Debug.Log($"Rabbit '{name}' forced to exit spawner due to {reason} (hunger: {CurrentHunger} < threshold: {(isCriticallyHungry ? _criticalHungerThreshold : _hungerThreshold)})");
	}

	/// <summary>
	/// Tries to flee to the rabbit spawner when being chased by a predator.
	/// If we can't reach the spawner, falls back to normal fleeing behavior.
	/// </summary>
	private void TryFleeToSpawner(PredatorAnimal predator)
	{
		if (_rabbitSpawner == null)
		{
			FleeFromPredator(predator);
			return;
		}

		Vector2Int spawnerPos = _rabbitSpawner.GridPosition;
		Vector2Int myPos = GridPosition;

		// If we're already at the spawner, hide in it
		if (myPos == spawnerPos)
		{
			TryHideInSpawner();
			return;
		}

		// Try to move one step towards the spawner
		if (MoveOneStepTowards(spawnerPos))
		{
			// Check if we reached the spawner
			if (GridPosition == spawnerPos)
			{
				TryHideInSpawner();
			}
			return;
		}

		// If we can't move towards the spawner, fall back to normal fleeing
		FleeFromPredator(predator);
	}

	/// <summary>
	/// Gets the rabbit's intended destination (food or wandering).
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

		// Draw line to rabbit spawner if we have one
		if (_rabbitSpawner != null)
		{
			Vector3 spawnerWorldPos = EnvironmentManager.Instance.GridToWorldPosition(_rabbitSpawner.GridPosition);
			Gizmos.color = Color.yellow; // Yellow for spawner
			Gizmos.DrawLine(currentWorldPos, spawnerWorldPos);
			Gizmos.DrawWireSphere(spawnerWorldPos, 0.2f);
		}
	}
}
