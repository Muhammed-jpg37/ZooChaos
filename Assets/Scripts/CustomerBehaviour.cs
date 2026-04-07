using System.Collections.Generic;
using UnityEngine;

public class CustomerBehaviour : MonoBehaviour
{
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
	[SerializeField] private float visitPauseDuration = 0.75f;

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
	private Vector3 entryWorldPosition;
	private bool hasEntryRoadCell;
	private bool isInitialized;

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
		gridScript = FindObjectOfType<GridScript>();

		if (resourceManager == null || gridScript == null)
		{
			Debug.LogWarning("CustomerBehaviour could not initialize because ResourceManager or GridScript is missing.");
			Destroy(gameObject);
			return;
		}

		entryWorldPosition = transform.position;
		hasEntryRoadCell = gridScript.TryGetClosestRoadCell(entryWorldPosition, out entryRoadCell);

		isInitialized = true;
		PrepareTrip();
	}

	private void PrepareTrip()
	{
		visitTargets.Clear();
		travelPoints.Clear();
		activeTarget = null;
		travelIndex = 0;
		visitTimer = 0f;

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
		transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

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
			transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

			if (Vector3.Distance(transform.position, targetPoint) <= arriveDistance)
			{
				travelIndex++;
			}

			return;
		}

		transform.position = Vector3.MoveTowards(transform.position, entryWorldPosition, moveSpeed * Time.deltaTime);
		if (Vector3.Distance(transform.position, entryWorldPosition) <= arriveDistance)
		{
			CompleteExit();
		}
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
			state = CustomerState.Travelling;
			return true;
		}

		return false;
	}

	private void BeginVisit()
	{
		state = CustomerState.Visiting;
		visitTimer = visitPauseDuration;

		if (activeTarget != null)
		{
			transform.position = activeTarget.transform.position;
		}
	}

	private void CompleteVisit()
	{
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
		activeTarget = null;
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
			case BuySystemManager.BuildingType.Cage1:
			case BuySystemManager.BuildingType.Cage2:
			case BuySystemManager.BuildingType.Cage3:
				return cageBaseReward;
			case BuySystemManager.BuildingType.FoodStall:
				return foodStallBaseReward;
			case BuySystemManager.BuildingType.WaterFountain:
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
		if (resourceManager != null)
		{
			resourceManager.UnregisterCustomer(this);
		}
	}
}
