using System.Collections.Generic;
using UnityEngine;

public class CustomerBehaviour : MonoBehaviour
{
	private static readonly Dictionary<int, List<CustomerBehaviour>> buildingVisitQueues = new Dictionary<int, List<CustomerBehaviour>>();

	private enum CustomerState
	{
		Travelling,
		Visiting,
		Exiting,
		Finished
	}

	[Header("Movement")]
	[SerializeField] private float moveSpeed = 2.75f;
	[SerializeField] private float arriveDistance = 0.05f;
	[SerializeField] private float minVisitPauseDuration = 4f;
	[SerializeField] private float maxVisitPauseDuration = 9f;
	[SerializeField] private float queueSlotSpacingCells = 0.9f;
	[SerializeField] private float nearBuildingOffsetCells = 0.35f;
	[SerializeField] private float walkTurnSpeed = 10f;
	[SerializeField] private float minMoveSpeedMultiplier = 0.9f;
	[SerializeField] private float maxMoveSpeedMultiplier = 1.15f;

	[Header("Trip Plan")]
	[SerializeField] private int minBuildingsToVisit = 1;
	[SerializeField] private int maxBuildingsToVisit = 4;

	[Header("Rewards")]
	[SerializeField] private int cageBaseReward = 18;
	[SerializeField] private int foodStallBaseReward = 8;
	[SerializeField] private int waterFountainBaseReward = 6;
	[SerializeField] private int roadBaseReward = 1;
	[SerializeField] private float lowAnimalHappinessMultiplier = 0.5f;
	[SerializeField] private float highAnimalHappinessMultiplier = 2.0f;
	[SerializeField] private float lowCustomerHappinessMultiplier = 0.75f;
	[SerializeField] private float highCustomerHappinessMultiplier = 1.35f;

	private ResourceManager resourceManager;
	private GridScript gridScript;
	private readonly List<BuildingInstance> visitTargets = new List<BuildingInstance>();
	private readonly List<Vector3> travelPoints = new List<Vector3>();
	private CustomerState state = CustomerState.Finished;
	private BuildingInstance activeTarget;
	private int travelIndex;
	private float visitTimer;
	private Vector2Int currentRoadCell;
	private Vector2Int entryRoadCell;
	private Vector2Int targetEntranceRoadCell;
	private Vector3 entryWorldPosition;
	private bool hasEntryRoadCell;
	private bool hasTargetEntranceRoadCell;
	private bool isInitialized;
	private float runtimeMoveSpeed;

	private void OnEnable()
	{
		if (resourceManager != null && !isInitialized)
		{
			Initialize(resourceManager);
		}
	}

	private void Update()
	{
		if (!isInitialized || state == CustomerState.Finished)
		{
			return;
		}

		switch (state)
		{
			case CustomerState.Travelling:
				UpdateTravel();
				break;
			case CustomerState.Visiting:
				UpdateVisit();
				break;
			case CustomerState.Exiting:
				UpdateExiting();
				break;
		}
	}

	public void Initialize(ResourceManager manager)
	{
		resourceManager = manager;
		gridScript = FindGridScript();
		float minSpeed = Mathf.Max(0.2f, minMoveSpeedMultiplier);
		float maxSpeed = Mathf.Max(minSpeed, maxMoveSpeedMultiplier);
		runtimeMoveSpeed = moveSpeed * Random.Range(minSpeed, maxSpeed);

		if (resourceManager == null || gridScript == null)
		{
			//Debug.LogWarning("CustomerBehaviour could not initialize because ResourceManager or GridScript is missing.");
			Destroy(gameObject);
			return;
		}

		entryWorldPosition = transform.position;
		hasEntryRoadCell = gridScript.TryGetClosestRoadCell(entryWorldPosition, out entryRoadCell);

		isInitialized = true;
		PrepareTrip();
	}

	private GridScript FindGridScript()
	{
		GridScript activeGrid = FindObjectOfType<GridScript>();
		if (activeGrid != null)
		{
			return activeGrid;
		}

		GridScript[] allGrids = Resources.FindObjectsOfTypeAll<GridScript>();
		for (int i = 0; i < allGrids.Length; i++)
		{
			GridScript grid = allGrids[i];
			if (grid == null)
			{
				continue;
			}

			if (!grid.gameObject.scene.IsValid())
			{
				continue;
			}

			return grid;
		}

		return null;
	}

	private void PrepareTrip()
	{
		LeaveCurrentQueue();
		visitTargets.Clear();
		travelPoints.Clear();
		activeTarget = null;
		travelIndex = 0;
		visitTimer = 0f;
		hasTargetEntranceRoadCell = false;

		BuildingInstance[] allBuildings = FindObjectsOfType<BuildingInstance>();
		List<BuildingInstance> candidates = new List<BuildingInstance>();
		for (int i = 0; i < allBuildings.Length; i++)
		{
			if (allBuildings[i] != null && allBuildings[i].BuildingType != BuySystemManager.BuildingType.Road)
			{
				candidates.Add(allBuildings[i]);
			}
		}

		if (candidates.Count == 0)
		{
			BeginExitZoo();
			return;
		}

		Shuffle(candidates);

		int customerHappinessBonus = Mathf.FloorToInt(resourceManager.CustomerHappiness / 35f);
		int desiredVisitCount = Random.Range(minBuildingsToVisit, maxBuildingsToVisit + 1) + customerHappinessBonus;
		desiredVisitCount = Mathf.Clamp(desiredVisitCount, 1, candidates.Count);

		for (int i = 0; i < candidates.Count && visitTargets.Count < desiredVisitCount; i++)
		{
			visitTargets.Add(candidates[i]);
		}

		if (hasEntryRoadCell)
		{
			currentRoadCell = entryRoadCell;
		}
		else if (!gridScript.TryGetClosestRoadCell(transform.position, out currentRoadCell))
		{
			BeginExitZoo();
			return;
		}

		if (!TryPrepareNextTravelPath())
		{
			BeginExitZoo();
			return;
		}

		state = CustomerState.Travelling;
	}

	private void UpdateTravel()
	{
		if (travelPoints.Count == 0)
		{
			if (!TryPrepareNextTravelPath())
			{
				BeginExitZoo();
			}

			return;
		}

		if (travelIndex >= travelPoints.Count)
		{
			BeginVisit();
			return;
		}

		Vector3 targetPoint = travelPoints[travelIndex];
		UpdateWalkingFacing(targetPoint);
		transform.position = Vector3.MoveTowards(transform.position, targetPoint, runtimeMoveSpeed * Time.deltaTime);

		if (Vector3.Distance(transform.position, targetPoint) <= arriveDistance)
		{
			travelIndex++;
			if (travelIndex >= travelPoints.Count)
			{
				BeginVisit();
			}
		}
	}

	private void UpdateVisit()
	{
		if (activeTarget == null)
		{
			CompleteVisit();
			return;
		}

		if (TryGetCurrentViewingPosition(out Vector3 viewingPosition))
		{
			transform.position = Vector3.MoveTowards(transform.position, viewingPosition, runtimeMoveSpeed * Time.deltaTime);

			Vector3 lookDirection = activeTarget.transform.position - transform.position;
			lookDirection.y = 0f;
			if (lookDirection.sqrMagnitude > 0.0001f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
			}

			if (Vector3.Distance(transform.position, viewingPosition) > arriveDistance)
			{
				return;
			}
		}

		visitTimer -= Time.deltaTime;
		if (visitTimer > 0f)
		{
			return;
		}

		CompleteVisit();
	}

	private void UpdateExiting()
	{
		if (travelIndex < travelPoints.Count)
		{
			Vector3 targetPoint = travelPoints[travelIndex];
			UpdateWalkingFacing(targetPoint);
			transform.position = Vector3.MoveTowards(transform.position, targetPoint, runtimeMoveSpeed * Time.deltaTime);

			if (Vector3.Distance(transform.position, targetPoint) <= arriveDistance)
			{
				travelIndex++;
			}

			return;
		}

		UpdateWalkingFacing(entryWorldPosition);
		transform.position = Vector3.MoveTowards(transform.position, entryWorldPosition, runtimeMoveSpeed * Time.deltaTime);
		if (Vector3.Distance(transform.position, entryWorldPosition) <= arriveDistance)
		{
			CompleteExit();
		}
	}

	private void UpdateWalkingFacing(Vector3 targetPoint)
	{
		Vector3 direction = targetPoint - transform.position;
		direction.y = 0f;
		if (direction.sqrMagnitude < 0.0001f)
		{
			return;
		}

		Quaternion targetRotation = GetCardinalRotation(direction);
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * Mathf.Max(1f, walkTurnSpeed));
	}

	private Quaternion GetCardinalRotation(Vector3 direction)
	{
		if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z))
		{
			return direction.x >= 0f
				? Quaternion.Euler(0f, 90f, 0f)
				: Quaternion.Euler(0f, 270f, 0f);
		}

		return direction.z >= 0f
			? Quaternion.Euler(0f, 0f, 0f)
			: Quaternion.Euler(0f, 180f, 0f);
	}

	private bool TryPrepareNextTravelPath()
	{
		travelPoints.Clear();
		travelIndex = 0;

		while (visitTargets.Count > 0)
		{
			BuildingInstance target = visitTargets[0];
			List<Vector2Int> entranceCells = target.GetEntranceRoadCells(gridScript);
			if (entranceCells.Count == 0)
			{
				visitTargets.RemoveAt(0);
				continue;
			}

			List<Vector2Int> bestPath = null;
			Vector2Int bestEntrance = default;

			for (int i = 0; i < entranceCells.Count; i++)
			{
				List<Vector2Int> roadPath = gridScript.GetRoadPath(currentRoadCell, entranceCells[i]);
				if (roadPath.Count == 0)
				{
					continue;
				}

				if (bestPath == null || roadPath.Count < bestPath.Count)
				{
					bestPath = roadPath;
					bestEntrance = entranceCells[i];
				}
			}

			if (bestPath == null)
			{
				visitTargets.RemoveAt(0);
				continue;
			}

			for (int i = 0; i < bestPath.Count; i++)
			{
				travelPoints.Add(gridScript.CellToWorldCenter(bestPath[i]));
			}

			activeTarget = target;
			currentRoadCell = bestEntrance;
			targetEntranceRoadCell = bestEntrance;
			hasTargetEntranceRoadCell = true;
			state = CustomerState.Travelling;
			return true;
		}

		return false;
	}

	private void BeginVisit()
	{
		state = CustomerState.Visiting;
		float minDuration = Mathf.Max(0.1f, minVisitPauseDuration);
		float maxDuration = Mathf.Max(minDuration, maxVisitPauseDuration);
		visitTimer = Random.Range(minDuration, maxDuration);
		JoinCurrentQueue();
	}

	private bool TryGetCurrentViewingPosition(out Vector3 viewingPosition)
	{
		viewingPosition = transform.position;

		if (activeTarget == null || gridScript == null)
		{
			return false;
		}

		if (!hasTargetEntranceRoadCell)
		{
			if (!gridScript.TryGetClosestRoadCell(transform.position, out targetEntranceRoadCell))
			{
				return false;
			}

			hasTargetEntranceRoadCell = true;
		}

		int slotIndex = GetQueueSlotIndex(activeTarget, this);
		if (slotIndex < 0)
		{
			slotIndex = 0;
		}

		Vector2Int queueDirection = GetQueueDirectionAwayFromBuilding(activeTarget, targetEntranceRoadCell);
		Vector2Int towardBuildingDirection = new Vector2Int(-queueDirection.x, -queueDirection.y);
		Vector3 queueAnchor = gridScript.CellToWorldCenter(targetEntranceRoadCell);
		float spacingWorld = Mathf.Max(0.2f, queueSlotSpacingCells) * gridScript.cellSize;
		float nearOffsetWorld = Mathf.Clamp(nearBuildingOffsetCells, 0f, 0.49f) * gridScript.cellSize;
		Vector3 nearBuildingOffset = new Vector3(towardBuildingDirection.x, 0f, towardBuildingDirection.y) * nearOffsetWorld;
		Vector3 lineOffset = new Vector3(queueDirection.x, 0f, queueDirection.y) * (slotIndex * spacingWorld);
		viewingPosition = queueAnchor + nearBuildingOffset + lineOffset;
		return true;
	}

	private Vector2Int GetQueueDirectionAwayFromBuilding(BuildingInstance building, Vector2Int entranceCell)
	{
		int minX = building.GridOrigin.x;
		int maxX = building.GridOrigin.x + building.Width - 1;
		int minZ = building.GridOrigin.y;
		int maxZ = building.GridOrigin.y + building.Depth - 1;

		if (entranceCell.x < minX)
		{
			return Vector2Int.left;
		}

		if (entranceCell.x > maxX)
		{
			return Vector2Int.right;
		}

		if (entranceCell.y < minZ)
		{
			return Vector2Int.down;
		}

		if (entranceCell.y > maxZ)
		{
			return Vector2Int.up;
		}

		Vector2 buildingCenter = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
		Vector2 away = new Vector2(entranceCell.x, entranceCell.y) - buildingCenter;
		if (Mathf.Abs(away.x) >= Mathf.Abs(away.y))
		{
			return away.x >= 0f ? Vector2Int.right : Vector2Int.left;
		}

		return away.y >= 0f ? Vector2Int.up : Vector2Int.down;
	}

	private void JoinCurrentQueue()
	{
		if (activeTarget == null)
		{
			return;
		}

		int key = activeTarget.GetInstanceID();
		if (!buildingVisitQueues.TryGetValue(key, out List<CustomerBehaviour> queue))
		{
			queue = new List<CustomerBehaviour>();
			buildingVisitQueues[key] = queue;
		}

		if (!queue.Contains(this))
		{
			queue.Add(this);
		}
	}

	private void LeaveCurrentQueue()
	{
		if (activeTarget == null)
		{
			return;
		}

		int key = activeTarget.GetInstanceID();
		if (!buildingVisitQueues.TryGetValue(key, out List<CustomerBehaviour> queue))
		{
			return;
		}

		queue.Remove(this);
		if (queue.Count == 0)
		{
			buildingVisitQueues.Remove(key);
		}
	}

	private int GetQueueSlotIndex(BuildingInstance building, CustomerBehaviour customer)
	{
		if (building == null)
		{
			return -1;
		}

		int key = building.GetInstanceID();
		if (!buildingVisitQueues.TryGetValue(key, out List<CustomerBehaviour> queue))
		{
			return -1;
		}

		return queue.IndexOf(customer);

	}

	private void CompleteVisit()
	{
		LeaveCurrentQueue();

		if (activeTarget != null && resourceManager != null)
		{
			int reward = CalculateReward(activeTarget);
			resourceManager.AddMoney(reward);
			Debug.Log($"Customer visited {activeTarget.BuildingType} and paid {reward}.");
		}

		if (visitTargets.Count > 0)
		{
			visitTargets.RemoveAt(0);
		}

		activeTarget = null;
		hasTargetEntranceRoadCell = false;
		travelPoints.Clear();
		travelIndex = 0;

		if (visitTargets.Count == 0)
		{
			BeginExitZoo();
			return;
		}

		if (!gridScript.TryGetClosestRoadCell(transform.position, out currentRoadCell))
		{
			BeginExitZoo();
			return;
		}

		if (!TryPrepareNextTravelPath())
		{
			BeginExitZoo();
		}
		else
		{
			state = CustomerState.Travelling;
		}
	}

	private void BeginExitZoo()
	{
		if (state == CustomerState.Exiting || state == CustomerState.Finished)
		{
			return;
		}

		visitTargets.Clear();
		LeaveCurrentQueue();
		activeTarget = null;
		hasTargetEntranceRoadCell = false;
		travelPoints.Clear();
		travelIndex = 0;
		state = CustomerState.Exiting;

		if (!hasEntryRoadCell)
		{
			return;
		}

		if (!gridScript.TryGetClosestRoadCell(transform.position, out currentRoadCell))
		{
			return;
		}

		List<Vector2Int> roadPath = gridScript.GetRoadPath(currentRoadCell, entryRoadCell);
		for (int i = 0; i < roadPath.Count; i++)
		{
			travelPoints.Add(gridScript.CellToWorldCenter(roadPath[i]));
		}
	}

	private int CalculateReward(BuildingInstance building)
	{
		float baseReward = GetBaseReward(building.BuildingType);
		float customerHappinessNormalized = Mathf.Clamp01(resourceManager.CustomerHappiness / 100f);
		float customerMultiplier = Mathf.Lerp(lowCustomerHappinessMultiplier, highCustomerHappinessMultiplier, customerHappinessNormalized);

		float animalMultiplier = 1f;
		AnimalBehaviour nearbyAnimal = FindNearestAnimal(building.transform.position, 12f);
		if (nearbyAnimal != null)
		{
			animalMultiplier = Mathf.Lerp(lowAnimalHappinessMultiplier, highAnimalHappinessMultiplier, nearbyAnimal.HappinessNormalized);
		}

		return Mathf.Max(1, Mathf.RoundToInt(baseReward * customerMultiplier * animalMultiplier));
	}

	private float GetBaseReward(BuySystemManager.BuildingType type)
	{
		switch (type)
		{
			case BuySystemManager.BuildingType.LionCage:
			case BuySystemManager.BuildingType.MonkeyCage:
				return cageBaseReward;
			case BuySystemManager.BuildingType.SodaVendor:
			case BuySystemManager.BuildingType.PopcornVendor:
			case BuySystemManager.BuildingType.VendingMachine:
				return foodStallBaseReward;
			case BuySystemManager.BuildingType.Decorations:
				return waterFountainBaseReward;
			case BuySystemManager.BuildingType.Road:
				return roadBaseReward;
			default:
				return foodStallBaseReward;
		}
	}

	private AnimalBehaviour FindNearestAnimal(Vector3 worldPosition, float maxDistance)
	{
		AnimalBehaviour[] animals = FindObjectsOfType<AnimalBehaviour>();
		AnimalBehaviour bestAnimal = null;
		float bestDistance = maxDistance;

		for (int i = 0; i < animals.Length; i++)
		{
			AnimalBehaviour animal = animals[i];
			if (animal == null)
			{
				continue;
			}

			float distance = Vector3.Distance(worldPosition, animal.transform.position);
			if (distance <= bestDistance)
			{
				bestDistance = distance;
				bestAnimal = animal;
			}
		}

		return bestAnimal;
	}

	private void Shuffle(List<BuildingInstance> buildings)
	{
		for (int i = 0; i < buildings.Count; i++)
		{
			int swapIndex = Random.Range(i, buildings.Count);
			BuildingInstance temp = buildings[i];
			buildings[i] = buildings[swapIndex];
			buildings[swapIndex] = temp;
		}
	}

	private void CompleteExit()
	{
		state = CustomerState.Finished;
		if (resourceManager != null)
		{
			resourceManager.UnregisterCustomer(this);
		}

		Destroy(gameObject);
	}

	private void OnDestroy()
	{
		LeaveCurrentQueue();

		if (resourceManager != null)
		{
			resourceManager.UnregisterCustomer(this);
		}
	}
}
