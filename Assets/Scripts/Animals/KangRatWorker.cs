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
		bool hasStoredDenFood = HasHomeHideable && HasStoredDenFoodAvailable();
		bool shouldUseDenStoredFood = isCriticallyHungry && hasStoredDenFood;
		bool hasCarriedItems = WorkerItemsCopy.Count > 0;
		
		// If we're hiding in our den and critically hungry, consume stored food if available.
        // Otherwise, force exit from den.
		if (IsHidingInHome)
		{
			if (shouldUseDenStoredFood && TryConsumeStoredDenFood())
			{
				return;
			}

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
			// If carrying anything, prioritize returning to den to deposit first
			if (hasCarriedItems && HasHomeHideable)
			{
				_foodDestination = null;
				_wanderingDestination = null;

				Vector2Int homePos = HomeHideable.GridPosition;
				
				if (IsAtHomeHideable)
				{
					if (!IsHidingInHome)
					{
						TryHideInHome();
					}
					return;
				}

				MoveOneStepTowards(homePos);
				return;
			}

			if (shouldUseDenStoredFood)
			{
				ReturnToDenForStoredFood();
				return;
			}

			// Check for nearby predators
			PredatorAnimal nearbyPredator = FindNearestPredator();
			
			// Only seek food/items if critically hungry OR if no predators are nearby
			if (isCriticallyHungry || nearbyPredator == null)
			{
				Vector2Int? nearestTarget = FindNearestFoodOrItemTarget();
				
				if (nearestTarget.HasValue)
				{
					_foodDestination = nearestTarget.Value;
					
					// Check if we're already at the item position
					if (GridPosition == nearestTarget.Value)
					{
						// TODO: Leyth, make this work for all item types (for now, we just delete the item and restore hunger cuz why not)
						TryCollectAtCurrentPosition(); 
						// IncreaseHunger(100);
						// if (ItemManager.Instance != null)  // Delete this section once TryCollectAtCurrentPosition() works for all item types
						// {
						// 	Item item = ItemManager.Instance.GetItemAtPosition(GridPosition);
						// 	if (item != null)
						// 	{
						// 		item.DestroyItem();
						// 		_foodDestination = null;
						// 	}
						// }
						// END TODO
					}
					else
					{
						// Move towards the target
						if (!MoveOneStepTowards(nearestTarget.Value))
						{
							// If move failed, try to find a new food/item destination
							Vector2Int? newTarget = FindNearestFoodOrItemTarget();
							if (newTarget.HasValue)
							{
								_foodDestination = newTarget.Value;
								MoveOneStepTowards(newTarget.Value);
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
					// No grass or items found, fall back to wandering
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
	/// Attempts to eat grass at the current position.
	/// If fully grown grass is found, changes it to growing state and restores hunger.
	/// </summary>
	private void TryCollectAtCurrentPosition()
	{
		if (InteractableManager.Instance == null || ItemManager.Instance == null)
		{
			return;
		} 
		
		Grass grass = InteractableManager.Instance.GetGrassAtPosition(GridPosition);
		Item item = null;
		
		if (grass != null) {
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
			
			// Use the same grass item type that the den system and inventory expect
			item = ItemManager.Instance.CreateItemForStorage(Globals.GRASS_ITEM_TYPE_FOR_WORKER_HARDCODE);
			
			// Eat the grass (change it to growing state)
			HarvestGrass(grass);

			if (item != null)
			{
				AddItem(item);  // Pick up the grass for carrying to the den
			}
			else
			{
				Debug.LogWarning(
					$"KangRatWorker '{name}' failed to create storage item for grass using type '{Globals.GRASS_ITEM_TYPE_FOR_WORKER_HARDCODE}'. " +
					"Grass will still restore hunger but won't be carried to the den.");
			}

			// Optionally create and carry a grass seed item
			if (Random.Range(0f, 1f) <= Grass.SEED_DROP_RATE_FULL)
			{
				Item seedItem = ItemManager.Instance.CreateItemForStorage(Grass.SEED_ITEM_TYPE);
				if (seedItem != null)
				{
					AddItem(seedItem);
				}
				else
				{
					Debug.LogWarning(
						$"KangRatWorker '{name}' failed to create storage item for grass seed using type '{Grass.SEED_ITEM_TYPE}'.");
				}
			}
			
			// Clear food destination since we've eaten
			_foodDestination = null;
		}
		else 
		{
			Item originalItem = ItemManager.Instance.GetItemAtPosition(GridPosition);
			
			if (originalItem == null) {
				return;
			}
			
			item = ItemManager.Instance.CreateItemForStorage(originalItem.ItemId);
			
			if (originalItem != null) {
				originalItem.DestroyItem();
				_foodDestination = null;
			}
	  	
			
			if (item == null) {
				Debug.LogWarning(
					$"KangRatWorker '{name}' failed to create storage item for ground item '{originalItem.ItemName}'.");
				return;
			}

			AddItem(item);
		}
		
		// Debug: log carried inventory after pickup (ensure items are retained for den deposit)
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		string pickedUpName = item != null ? item.ItemName : (grass != null ? Globals.GRASS_ITEM_TYPE_FOR_WORKER_HARDCODE.ToString() : "UNKNOWN");
		sb.Append($"KangRatWorker '{name}' picked up '{pickedUpName}'. Carried items now: ");
		List<Item> carriedItems = WorkerItemsCopy;
		for (int i = 0; i < carriedItems.Count; i++)
		{
			string itemName = carriedItems[i] != null ? carriedItems[i].ItemName : "null";
			sb.Append(itemName);
			if (i < carriedItems.Count - 1)
			{
				sb.Append(", ");
			}
		}
		Debug.Log(sb.ToString());
		
		// Restore hunger from what was collected WITHOUT consuming carried inventory
		int hungerRestored = 0;
		
		if (grass != null)
		{
			// Use the grass's hunger value (worker gets 1.5x) but keep the carried grass item for the den
			hungerRestored = Mathf.RoundToInt(grass.HungerRestored * 1.5f);
		}
		else if (item is FoodItem)
		{
			// For picked-up food items, use the item's hunger value but keep it in the inventory
			int baseHungerRestored = (item as FoodItem).HungerRestored;
			hungerRestored = Mathf.RoundToInt(baseHungerRestored * 1.5f);
		}
		else if (item != null)
		{
			// Non-food items still provide a hunger boost on pickup
			hungerRestored = Globals.WorkerItemPickupHungerRestoration;
		}
		
		if (hungerRestored > 0)
		{
			IncreaseHunger(hungerRestored);
		}
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
	/// Chooses what to target between food (fully grown matching grass) and items.
	/// When below the hunger threshold (IsHungry), prioritizes grass OR food items.
	/// When not hungry, prioritizes items over grass.
	/// Uses the existing search logic in PreyAnimal (including critical-hunger radius rules).
	/// </summary>
	private Vector2Int? FindNearestFoodOrItemTarget()
	{
		// Get candidates from base-class search helpers
		Vector2Int? nearestGrass = FindNearestFullyGrownGrass();
		Vector2Int? nearestPickupItem = FindNearestPickupItem();
		Vector2Int? nearestItem = FindNearestItem();

		// If we're hungry, prioritize grass OR food items (choose the nearest one)
		if (IsHungry)
		{
			// Find the nearest between grass and items
			if (nearestGrass.HasValue && nearestPickupItem.HasValue)
			{
				// Both available - choose the closer one
				Vector2Int myPos = GridPosition;
				int grassDistance = Mathf.Abs(nearestGrass.Value.x - myPos.x) + Mathf.Abs(nearestGrass.Value.y - myPos.y);
				int pickupItemDistance = Mathf.Abs(nearestPickupItem.Value.x - myPos.x) + Mathf.Abs(nearestPickupItem.Value.y - myPos.y);
				
				return grassDistance <= pickupItemDistance ? nearestGrass : nearestPickupItem;
			}
			
			// Only one available - use that one
			if (nearestGrass.HasValue)
			{
				return nearestGrass;
			}
			
			if (nearestPickupItem.HasValue)
			{
				return nearestPickupItem;
			}
			
			// No grass or food items - return null (don't seek non-food items when hungry)
			return null;
		}

		// If we're not hungry, prioritize items over grass
		if (nearestItem.HasValue)
		{
			return nearestItem;
		}

		return nearestGrass;
	}
	
	/// <summary>
	/// Finds the nearest item (any type) within detection radius.
	/// If critically hungry, searches with infinite range (ignores detection radius).
	/// </summary>
	private Vector2Int? FindNearestPickupItem()
	{
		if (ItemManager.Instance == null)
		{
			return null;
		}

		List<Item> allItems = ItemManager.Instance.Items;
		if (allItems == null || allItems.Count == 0)
		{
			return null;
		}

		Vector2Int myPos = GridPosition;
		Item nearest = null;
		int bestDistance = int.MaxValue;
		
		// Check if at critical hunger - if so, ignore detection radius (infinite range)
		bool isCriticallyHungry = IsCriticallyHungry;

		for (int i = 0; i < allItems.Count; i++)
		{
			Item item = allItems[i];
			if (item == null)
			{
				continue;
			}

			Vector2Int itemPos = item.GridPosition;
			int distance = Mathf.Abs(itemPos.x - myPos.x) + Mathf.Abs(itemPos.y - myPos.y); // Manhattan distance
			
			// If critically hungry, ignore detection radius. Otherwise, only consider items within detection radius
			if (isCriticallyHungry)
			{
				// Infinite range when critically hungry - just find the nearest
				if (distance < bestDistance)
				{
					bestDistance = distance;
					nearest = item;
				}
			}
			else
			{
				// Normal behavior - only consider items within detection radius
				// Use _grassDetectionRadius from base class (now protected)
				if (distance <= _grassDetectionRadius && distance < bestDistance)
				{
					bestDistance = distance;
					nearest = item;
				}
			}
		}

		return nearest != null ? nearest.GridPosition : (Vector2Int?)null;
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
	/// Allow stacking with other animals when moving onto the home den tile
	/// so this worker can enter or exit even if another animal occupies it.
	/// </summary>
	public override bool CanShareTileWithOtherAnimal(Animal other, Vector2Int position)
	{
		if (HasHomeHideable && HomeHideable != null && HomeHideable.GridPosition == position)
		{
			return true;
		}

		return base.CanShareTileWithOtherAnimal(other, position);
	}

	/// <summary>
	/// Returns true if there is stored food available in the den system.
	/// </summary>
	private bool HasStoredDenFoodAvailable()
	{
		return GlobalInventoryManager.Instance != null && GlobalInventoryManager.Instance.FoodInDen > 0;
	}

	/// <summary>
	/// Moves the worker back to its home den to consume stored food.
	/// </summary>
	private void ReturnToDenForStoredFood()
	{
		if (!HasHomeHideable)
		{
			return;
		}

		_foodDestination = null;
		_wanderingDestination = null;

		Vector2Int homePos = HomeHideable.GridPosition;

		if (IsAtHomeHideable)
		{
			if (!IsHidingInHome)
			{
				TryHideInHome();
			}

			TryConsumeStoredDenFood();
			return;
		}

		if (MoveOneStepTowards(homePos) && IsAtHomeHideable)
		{
			if (!IsHidingInHome)
			{
				TryHideInHome();
			}

			TryConsumeStoredDenFood();
		}
	}

	/// <summary>
	/// Attempts to consume stored den food, restoring hunger by the configured amount.
	/// </summary>
	private bool TryConsumeStoredDenFood()
	{
		if (DenSystemManager.Instance == null)
		{
			return false;
		}
    
    // int hungerRestoredResult = DenSystemManager.Instance.SpendFoodFromDen();
    int hungerRestoredResult = GlobalInventoryManager.Instance.SpendSingleFood();
    

		int hungerRestored = Mathf.Max(0, hungerRestoredResult);
		if (hungerRestored > 0)
		{
			IncreaseHunger(hungerRestored);
		}
    DenSystemManager.Instance.LogHolder.SpawnLog(LogEntryGuiController.DenLogType.TAKE_FOOD);
		Debug.Log($"KangRatWorker '{name}' consumed stored den food. Hunger restored by {hungerRestored}. Current hunger: {CurrentHunger}. Remaining den food: {GlobalInventoryManager.Instance.FoodInDen}");
		return true;
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


