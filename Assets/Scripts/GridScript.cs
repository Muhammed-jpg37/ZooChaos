using UnityEngine;
using System.Collections.Generic;

public class GridScript : MonoBehaviour
{
[Header("Grid Layout")]
    public int gridSize = 40; 
    public float cellSize = 1f;
    public Vector2 startCorner = new Vector2(-12.5f, -12.5f); // Bottom-Left Anchor
    
    [Header("Visuals")]
    public Color gridColor = Color.green;
    public Color occupiedCellColor = new Color(0.2f, 0.45f, 1f, 0.35f);
    public Color validPreviewColor = new Color(0.2f, 0.9f, 0.3f, 0.35f);
    public Color invalidPreviewColor = new Color(0.95f, 0.2f, 0.2f, 0.35f);

    // Data storage for occupied cells
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> roadCells = new HashSet<Vector2Int>();
    private bool hasPreview;
    private int previewX;
    private int previewZ;
    private int previewWidth = 1;
    private int previewDepth = 1;
    private bool previewRequiresFullRoadSide;

    public void SetPlacementPreview(int gridX, int gridZ, int width, int depth, bool requiresFullRoadSide = false)
    {
        hasPreview = true;
        previewX = gridX;
        previewZ = gridZ;
        previewWidth = Mathf.Max(1, width);
        previewDepth = Mathf.Max(1, depth);
        previewRequiresFullRoadSide = requiresFullRoadSide;
    }

    public void ClearPlacementPreview()
    {
        hasPreview = false;
        previewRequiresFullRoadSide = false;
    }
    /// <summary>
    /// Checks if a building footprint is clear.
    /// gridX/gridZ are the local grid indices (0 to gridSize-1)
    /// </summary>
    public bool CanPlaceBuilding(int gridX, int gridZ, int width, int depth)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int checkX = gridX + x;
                int checkZ = gridZ + z;

                // 1. Boundary Check
                if (checkX < 0 || checkX >= gridSize || checkZ < 0 || checkZ >= gridSize)
                    return false;

                // 2. Occupancy Check
                if (occupiedCells.Contains(new Vector2Int(checkX, checkZ)))
                    return false;
            }
        }
        return true;
    }

    public void MarkCellsOccupied(int gridX, int gridZ, int width, int depth, bool markAsRoad = false)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int cell = new Vector2Int(gridX + x, gridZ + z);
                occupiedCells.Add(cell);

                if (markAsRoad)
                {
                    roadCells.Add(cell);
                }
                else
                {
                    roadCells.Remove(cell);
                }
            }
        }
    }

    public bool HasAtLeastOneFullRoadSide(int gridX, int gridZ, int width, int depth)
    {
        // Top side (positive Z)
        if (IsFullRoadEdge(gridX, gridZ + depth, width, true))
            return true;

        // Bottom side (negative Z)
        if (IsFullRoadEdge(gridX, gridZ - 1, width, true))
            return true;

        // Left side (negative X)
        if (IsFullRoadEdge(gridX - 1, gridZ, depth, false))
            return true;

        // Right side (positive X)
        if (IsFullRoadEdge(gridX + width, gridZ, depth, false))
            return true;

        return false;
    }

    private bool IsFullRoadEdge(int startX, int startZ, int length, bool alongX)
    {
        for (int i = 0; i < length; i++)
        {
            int cellX = alongX ? startX + i : startX;
            int cellZ = alongX ? startZ : startZ + i;

            if (cellX < 0 || cellX >= gridSize || cellZ < 0 || cellZ >= gridSize)
                return false;

            if (!roadCells.Contains(new Vector2Int(cellX, cellZ)))
                return false;
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gridColor;
        
        // Calculate the far edges based on current size
        float endX = startCorner.x + (gridSize * cellSize);
        float endZ = startCorner.y + (gridSize * cellSize);

        for (int i = 0; i <= gridSize; i++)
        {
            float offset = i * cellSize;

            // Vertical Lines (Parallel to Z)
            Gizmos.DrawLine(
                new Vector3(startCorner.x + offset, 0, startCorner.y),
                new Vector3(startCorner.x + offset, 0, endZ)
            );

            // Horizontal Lines (Parallel to X)
            Gizmos.DrawLine(
                new Vector3(startCorner.x, 0, startCorner.y + offset),
                new Vector3(endX, 0, startCorner.y + offset)
            );
        }

        if (occupiedCells != null && occupiedCells.Count > 0)
        {
            Gizmos.color = occupiedCellColor;
            foreach (Vector2Int cell in occupiedCells)
            {
                if (cell.x < 0 || cell.x >= gridSize || cell.y < 0 || cell.y >= gridSize)
                {
                    continue;
                }

                Vector3 cellCenter = new Vector3(
                    startCorner.x + (cell.x + 0.5f) * cellSize,
                    0.01f,
                    startCorner.y + (cell.y + 0.5f) * cellSize
                );

                Vector3 size = new Vector3(cellSize * 0.95f, 0.015f, cellSize * 0.95f);
                Gizmos.DrawCube(cellCenter, size);
            }
        }

        if (!hasPreview)
        {
            return;
        }

        bool canPlacePreview = CanPlaceBuilding(previewX, previewZ, previewWidth, previewDepth);
        if (canPlacePreview && previewRequiresFullRoadSide)
        {
            canPlacePreview = HasAtLeastOneFullRoadSide(previewX, previewZ, previewWidth, previewDepth);
        }
        Gizmos.color = canPlacePreview ? validPreviewColor : invalidPreviewColor;

        for (int x = 0; x < previewWidth; x++)
        {
            for (int z = 0; z < previewDepth; z++)
            {
                int cellX = previewX + x;
                int cellZ = previewZ + z;

                if (cellX < 0 || cellX >= gridSize || cellZ < 0 || cellZ >= gridSize)
                {
                    continue;
                }

                Vector3 cellCenter = new Vector3(
                    startCorner.x + (cellX + 0.5f) * cellSize,
                    0.02f,
                    startCorner.y + (cellZ + 0.5f) * cellSize
                );

                Vector3 size = new Vector3(cellSize * 0.95f, 0.02f, cellSize * 0.95f);
                Gizmos.DrawCube(cellCenter, size);
            }
        }
    }
}
