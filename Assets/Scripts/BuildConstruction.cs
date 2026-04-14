using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildConstruction : MonoBehaviour
{
    private int gridX = -1;
    private int gridY = -1;
    private int databaseIndex = -1;
    public static BuildConstruction instance { get; private set; }

    private int GridToInternalIndex(int oneBasedIndex)
    {
        return oneBasedIndex - 1;
    }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }
    }

    public void GetGridPosition(int x, int y) {
        gridX = x;
        gridY = y;
        UpdatePlacementPreview();
        TryConstructBuilding();
    }

    public void GetBuildingType(int buildingIndex) {
        this.databaseIndex = buildingIndex;
        UpdatePlacementPreview();
    }

    private void UpdatePlacementPreview()
    {
        GridScript gridScript = FindObjectOfType<GridScript>();
        if (gridScript == null)
        {
            return;
        }

        if (gridX < 1 || gridY < 1)
        {
            gridScript.ClearPlacementPreview();
            return;
        }

        int internalGridX = GridToInternalIndex(gridX);
        int internalGridY = GridToInternalIndex(gridY);

        if (databaseIndex <= 0 || !System.Enum.IsDefined(typeof(BuySystemManager.BuildingType), databaseIndex))
        {
            gridScript.ClearPlacementPreview();
            return;
        }

        if (BuySystemManager.instance == null)
        {
            gridScript.ClearPlacementPreview();
            return;
        }

        BuySystemManager.BuildingType selectedType = (BuySystemManager.BuildingType)databaseIndex;
        if (!BuySystemManager.instance.TryGetBuildingData(selectedType, out GameObject _, out int width, out int depth))
        {
            gridScript.ClearPlacementPreview();
            return;
        }

        bool requiresFullRoadSide = selectedType != BuySystemManager.BuildingType.Road;
        gridScript.SetPlacementPreview(internalGridX, internalGridY, width, depth, requiresFullRoadSide);
    }

    private void TryConstructBuilding() {
        if (gridX < 1 || gridY < 1) {
            return;
        }

        int internalGridX = GridToInternalIndex(gridX);
        int internalGridY = GridToInternalIndex(gridY);

        if (databaseIndex <= 0 || !System.Enum.IsDefined(typeof(BuySystemManager.BuildingType), databaseIndex)) {
            return;
        }

        if (BuySystemManager.instance == null) {
            Debug.LogWarning("BuySystemManager instance is missing.");
            return;
        }

        GridScript gridScript = FindObjectOfType<GridScript>();
        if (gridScript == null) {
            Debug.LogWarning("GridScript instance is missing.");
            return;
        }

        BuySystemManager.BuildingType selectedType = (BuySystemManager.BuildingType)databaseIndex;
        if (!BuySystemManager.instance.TryGetBuildingData(selectedType, out GameObject prefab, out int width, out int depth)) {
            Debug.LogWarning("No prefab configured for selected building type: " + selectedType);
            return;
        }

        if (!BuySystemManager.instance.TryGetBuildingCost(selectedType, out int buildingCost))
        {
            Debug.LogWarning("No building cost configured for selected building type: " + selectedType);
            return;
        }

        if (ResourceManager.instance == null)
        {
            Debug.LogWarning("ResourceManager instance is missing.");
            return;
        }

        if (!ResourceManager.instance.CanAfford(buildingCost))
        {
            Debug.Log($"Cannot build {selectedType}. Required: {buildingCost}, Current money: {ResourceManager.instance.Money}");
            return;
        }

        if (!gridScript.CanPlaceBuilding(internalGridX, internalGridY, width, depth)) {
            Debug.Log("Cannot build here. The area is occupied or out of bounds.");
            return;
        }

        if (selectedType != BuySystemManager.BuildingType.Road &&
            !gridScript.HasAtLeastOneFullRoadSide(internalGridX, internalGridY, width, depth)) {
            Debug.Log("Cannot build here. Non-road buildings need at least one full side touching roads.");
            return;
        }

        if (selectedType == BuySystemManager.BuildingType.Road &&
            !CanPlaceRoadWithConnection(gridScript, internalGridX, internalGridY, width, depth))
        {
            Debug.Log("Cannot build here. Roads must connect to an existing road (except the first road).");
            return;
        }

        if (!ResourceManager.instance.SpendMoney(buildingCost))
        {
            Debug.Log($"Cannot build {selectedType}. Not enough money.");
            return;
        }

        bool markAsRoad = selectedType == BuySystemManager.BuildingType.Road;
        gridScript.MarkCellsOccupied(internalGridX, internalGridY, width, depth, markAsRoad);

        Vector3 spawnPosition = new Vector3(
            gridScript.startCorner.x + (internalGridX + (width * 0.5f)) * gridScript.cellSize,
            0f,
            gridScript.startCorner.y + (internalGridY + (depth * 0.5f)) * gridScript.cellSize
        );

        Quaternion spawnRotation = Quaternion.identity;
        if (selectedType == BuySystemManager.BuildingType.Road)
        {
            spawnRotation = GetRoadRotation(gridScript, internalGridX, internalGridY);
        }

        GameObject spawnedBuilding = Instantiate(prefab, spawnPosition, spawnRotation);

        BuildingInstance buildingInstance = spawnedBuilding.GetComponent<BuildingInstance>();
        if (buildingInstance == null)
        {
            buildingInstance = spawnedBuilding.AddComponent<BuildingInstance>();
        }

        buildingInstance.Initialize(selectedType, new Vector2Int(internalGridX, internalGridY), width, depth);

        if (selectedType == BuySystemManager.BuildingType.Road)
        {
            RefreshAllRoadRotations(gridScript);
        }

        ResetPendingGridPosition();

        if (gridScript != null)
        {
            gridScript.ClearPlacementPreview();
        }
    }

    private void ResetPendingGridPosition()
    {
        gridX = -1;
        gridY = -1;
    }

    private Quaternion GetRoadRotation(GridScript gridScript, int gridX, int gridZ)
    {
        bool hasLeftRoad = gridScript.IsRoadCell(new Vector2Int(gridX - 1, gridZ));
        bool hasRightRoad = gridScript.IsRoadCell(new Vector2Int(gridX + 1, gridZ));
        bool hasForwardRoad = gridScript.IsRoadCell(new Vector2Int(gridX, gridZ + 1));
        bool hasBackRoad = gridScript.IsRoadCell(new Vector2Int(gridX, gridZ - 1));

        bool connectedOnX = hasLeftRoad || hasRightRoad;
        bool connectedOnZ = hasForwardRoad || hasBackRoad;

        bool straightX = hasLeftRoad && hasRightRoad;
        bool straightZ = hasForwardRoad && hasBackRoad;

        if (straightZ && !straightX)
        {
            return Quaternion.Euler(0f, 90f, 0f);
        }

        if (straightX && !straightZ)
        {
            return Quaternion.identity;
        }

        if (connectedOnZ && !connectedOnX)
        {
            return Quaternion.Euler(0f, 90f, 0f);
        }

        return Quaternion.identity;
    }

    private bool CanPlaceRoadWithConnection(GridScript gridScript, int gridX, int gridZ, int width, int depth)
    {
        List<Vector2Int> roadCells = gridScript.GetRoadCells();
        if (roadCells == null || roadCells.Count == 0)
        {
            return true;
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int cell = new Vector2Int(gridX + x, gridZ + z);
                if (HasAdjacentRoad(gridScript, cell))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasAdjacentRoad(GridScript gridScript, Vector2Int cell)
    {
        Vector2Int[] offsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (gridScript.IsRoadCell(cell + offsets[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshAllRoadRotations(GridScript gridScript)
    {
        BuildingInstance[] allBuildings = FindObjectsOfType<BuildingInstance>();
        for (int i = 0; i < allBuildings.Length; i++)
        {
            BuildingInstance building = allBuildings[i];
            if (building == null || building.BuildingType != BuySystemManager.BuildingType.Road)
            {
                continue;
            }

            Vector2Int roadCell = building.GridOrigin;
            building.transform.rotation = GetRoadRotation(gridScript, roadCell.x, roadCell.y);
        }
    }
}