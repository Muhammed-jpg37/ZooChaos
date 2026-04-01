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

        gridScript.SetPlacementPreview(internalGridX, internalGridY, width, depth);
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

        if (!gridScript.CanPlaceBuilding(internalGridX, internalGridY, width, depth)) {
            Debug.Log("Cannot build here. The area is occupied or out of bounds.");
            return;
        }

        gridScript.MarkCellsOccupied(internalGridX, internalGridY, width, depth);

        Vector3 spawnPosition = new Vector3(
            gridScript.startCorner.x + (internalGridX + (width * 0.5f)) * gridScript.cellSize,
            0f,
            gridScript.startCorner.y + (internalGridY + (depth * 0.5f)) * gridScript.cellSize
        );

        Instantiate(prefab, spawnPosition, Quaternion.identity);
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
}
