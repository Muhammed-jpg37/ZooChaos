using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
	public static ResourceManager instance { get; private set; }

	[Header("Money")]
	[SerializeField] private int startingMoney = 0;
	[SerializeField] private int money;
	[SerializeField] private TMP_Text moneyText;

	private int dayStartBalance;
	private int dayIncome;
	private int dayExpenses;

	[Header("Customer Happiness")]
	[SerializeField, Range(0f, 100f)] private float customerHappiness = 50f;
	[SerializeField] private float minCustomerSpawnInterval = 4f;
	[SerializeField] private float maxCustomerSpawnInterval = 16f;
	[SerializeField] private int minCustomersPerSpawn = 1;
	[SerializeField] private int maxCustomersPerSpawn = 3;
	[SerializeField] private int maxActiveCustomers = 12;

	[Header("Customer Spawn")]
	[SerializeField] private GameObject customerPrefab;
	[SerializeField] private Transform customerSpawnPoint;
	[SerializeField] private bool customerSpawningEnabled = true;

	private readonly List<CustomerBehaviour> activeCustomers = new List<CustomerBehaviour>();
	private float spawnTimer;
	private GridScript cachedGridScript;

	public int Money => money;
	public float CustomerHappiness => customerHappiness;
	public int DayStartBalance => dayStartBalance;
	public int DayIncome => dayIncome;
	public int DayExpenses => dayExpenses;
	public bool IsCustomerSpawningEnabled => customerSpawningEnabled;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;
		money = startingMoney;
		BeginDayFinancials();
		RefreshMoneyUI();
	}

	private void Start()
	{
		spawnTimer = GetNextSpawnDelay();
	}

	private void Update()
	{
		CleanupCustomers();

		if (customerPrefab == null)
		{
			return;
		}

		if (!customerSpawningEnabled)
		{
			return;
		}

		if (!IsEntryReadyForGameplay())
		{
			return;
		}

		spawnTimer -= Time.deltaTime;
		if (spawnTimer <= 0f)
		{
			SpawnCustomers();
			spawnTimer = GetNextSpawnDelay();
		}
	}

	public void AddMoney(int amount)
	{
		int validAmount = Mathf.Max(0, amount);
		money += validAmount;
		dayIncome += validAmount;
		RefreshMoneyUI();
	}

	public bool CanAfford(int amount)
	{
		return money >= Mathf.Max(0, amount);
	}

	public bool SpendMoney(int amount)
	{
		int validAmount = Mathf.Max(0, amount);
		if (money < validAmount)
		{
			return false;
		}

		money -= validAmount;
		dayExpenses += validAmount;
		RefreshMoneyUI();
		return true;
	}

	public void BeginDayFinancials()
	{
		dayStartBalance = money;
		dayIncome = 0;
		dayExpenses = 0;
	}

	public void SetCustomerSpawningEnabled(bool isEnabled)
	{
		customerSpawningEnabled = isEnabled;
		if (isEnabled)
		{
			spawnTimer = 0f;
		}
	}

	public void DespawnAllCustomers()
	{
		for (int i = activeCustomers.Count - 1; i >= 0; i--)
		{
			CustomerBehaviour customer = activeCustomers[i];
			if (customer != null)
			{
				Destroy(customer.gameObject);
			}
		}

		activeCustomers.Clear();
	}

	private void RefreshMoneyUI()
	{
		if (moneyText == null)
		{
			return;
		}

		moneyText.text = money.ToString();
		
	}

	public void AddCustomerHappiness(float amount)
	{
		customerHappiness = Mathf.Clamp(customerHappiness + amount, 0f, 100f);
	}

	public void SetCustomerHappiness(float value)
	{
		customerHappiness = Mathf.Clamp(value, 0f, 100f);
	}

	public void RegisterCustomer(CustomerBehaviour customer)
	{
		if (customer != null && !activeCustomers.Contains(customer))
		{
			activeCustomers.Add(customer);
		}
	}

	public void UnregisterCustomer(CustomerBehaviour customer)
	{
		if (customer != null)
		{
			activeCustomers.Remove(customer);
		}
	}

	private void SpawnCustomers()
	{
		int spawnCount = GetCustomersPerSpawn();

		for (int i = 0; i < spawnCount; i++)
		{
			if (activeCustomers.Count >= maxActiveCustomers)
			{
				return;
			}

			Vector3 spawnPosition = GetSpawnPosition();
			GameObject customerObject = Instantiate(customerPrefab, spawnPosition, Quaternion.identity);
			CustomerBehaviour customer = customerObject.GetComponent<CustomerBehaviour>();
			if (customer == null)
			{
				customer = customerObject.AddComponent<CustomerBehaviour>();
			}

			customer.Initialize(this);
			RegisterCustomer(customer);
		}
	}

	private int GetCustomersPerSpawn()
	{
		float happinessT = Mathf.Clamp01(customerHappiness / 100f);
		int scaledCount = Mathf.RoundToInt(Mathf.Lerp(minCustomersPerSpawn, maxCustomersPerSpawn, happinessT));
		return Mathf.Clamp(scaledCount, minCustomersPerSpawn, maxCustomersPerSpawn);
	}

	private float GetNextSpawnDelay()
	{
		float happinessT = Mathf.Clamp01(customerHappiness / 100f);
		float baseDelay = Mathf.Lerp(maxCustomerSpawnInterval, minCustomerSpawnInterval, happinessT);
		return baseDelay * Random.Range(0.8f, 1.2f);
	}

	private Vector3 GetSpawnPosition()
	{
		if (customerSpawnPoint != null)
		{
			return customerSpawnPoint.position;
		}

		GridScript gridScript = FindGridScript();
		if (gridScript != null)
		{
			if (gridScript.TryGetEntrySpawnPosition(out Vector3 entrySpawnPosition))
			{
				return entrySpawnPosition;
			}

			List<Vector2Int> roadCells = gridScript.GetRoadCells();
			if (roadCells.Count > 0)
			{
				Vector2Int roadCell = roadCells[Random.Range(0, roadCells.Count)];
				return gridScript.CellToWorldCenter(roadCell);
			}
		}

		return transform.position;
	}

	private bool IsEntryReadyForGameplay()
	{
		GridScript gridScript = FindGridScript();
		if (gridScript == null)
		{
			return true;
		}

		return gridScript.HasEntryPointConfigured;
	}

	private GridScript FindGridScript()
	{
		if (cachedGridScript != null)
		{
			return cachedGridScript;
		}

		GridScript activeGrid = FindObjectOfType<GridScript>();
		if (activeGrid != null)
		{
			cachedGridScript = activeGrid;
			return cachedGridScript;
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

			cachedGridScript = grid;
			return cachedGridScript;
		}

		return null;
	}

	private void CleanupCustomers()
	{
		for (int i = activeCustomers.Count - 1; i >= 0; i--)
		{
			if (activeCustomers[i] == null)
			{
				activeCustomers.RemoveAt(i);
			}
		}
	}
}
