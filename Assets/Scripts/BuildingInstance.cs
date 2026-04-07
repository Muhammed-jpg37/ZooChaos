using System.Collections.Generic;
using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    public BuySystemManager.BuildingType BuildingType { get; private set; } = BuySystemManager.BuildingType.None;
    public Vector2Int GridOrigin { get; private set; }
    public int Width { get; private set; } = 1;
    public int Depth { get; private set; } = 1;

    public void Initialize(BuySystemManager.BuildingType buildingType, Vector2Int gridOrigin, int width, int depth)
    {
        BuildingType = buildingType;
        GridOrigin = gridOrigin;
        Width = Mathf.Max(1, width);
        Depth = Mathf.Max(1, depth);
    }

    public List<Vector2Int> GetEntranceRoadCells(GridScript gridScript)
    {
        List<Vector2Int> entranceCells = new List<Vector2Int>();

        if (gridScript == null || BuildingType == BuySystemManager.BuildingType.Road)
        {
            return entranceCells;
        }

        AddSideCells(gridScript, entranceCells, GridOrigin.x, GridOrigin.y + Depth, Width, true);
        AddSideCells(gridScript, entranceCells, GridOrigin.x, GridOrigin.y - 1, Width, true);
        AddSideCells(gridScript, entranceCells, GridOrigin.x - 1, GridOrigin.y, Depth, false);
        AddSideCells(gridScript, entranceCells, GridOrigin.x + Width, GridOrigin.y, Depth, false);

        return entranceCells;
    }

    private void AddSideCells(GridScript gridScript, List<Vector2Int> entranceCells, int startX, int startZ, int length, bool alongX)
    {
        for (int i = 0; i < length; i++)
        {
            int cellX = alongX ? startX + i : startX;
            int cellZ = alongX ? startZ : startZ + i;
            Vector2Int cell = new Vector2Int(cellX, cellZ);

            if (!gridScript.IsWithinGrid(cell) || !gridScript.IsRoadCell(cell))
            {
                continue;
            }

            if (!entranceCells.Contains(cell))
            {
                entranceCells.Add(cell);
            }
        }
    }
}