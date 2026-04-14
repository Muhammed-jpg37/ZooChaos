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
    public Color largeOccupiedCellColor = new Color(1f, 0.2f, 0.2f, 0.45f);
    public Color validPreviewColor = new Color(0.2f, 0.9f, 0.3f, 0.35f);
    public Color invalidPreviewColor = new Color(0.95f, 0.2f, 0.2f, 0.35f);

    // Data storage for occupied cells
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> largeOccupiedCells = new HashSet<Vector2Int>();
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

    /// Checks if a building footprint is clear.
    /// gridX/gridZ are the local grid indices (0 to gridSize-1)
  
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
        bool isLargeFootprint = width > 1 || depth > 1;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int cell = new Vector2Int(gridX + x, gridZ + z);
                occupiedCells.Add(cell);

                if (isLargeFootprint)
                {
                    largeOccupiedCells.Add(cell);
                }
                else
                {
                    largeOccupiedCells.Remove(cell);
                }

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

    public List<Vector2Int> GetRoadCells()
    {
        return new List<Vector2Int>(roadCells);
    }

    public bool IsRoadCell(Vector2Int cell)
    {
        return roadCells.Contains(cell);
    }

    public bool IsWithinGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridSize && cell.y >= 0 && cell.y < gridSize;
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        int cellX = Mathf.FloorToInt((worldPosition.x - startCorner.x) / cellSize);
        int cellY = Mathf.FloorToInt((worldPosition.z - startCorner.y) / cellSize);
        return new Vector2Int(cellX, cellY);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        return new Vector3(
            startCorner.x + (cell.x + 0.5f) * cellSize,
            0f,
            startCorner.y + (cell.y + 0.5f) * cellSize
        );
    }
    public bool TryGetClosestRoadCell(Vector3 worldPosition, out Vector2Int closestCell)
    {
        closestCell = default;

        if (roadCells == null || roadCells.Count == 0)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        foreach (Vector2Int roadCell in roadCells)
        {
            Vector3 roadWorld = CellToWorldCenter(roadCell);
            float distance = Vector3.Distance(worldPosition, roadWorld);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closestCell = roadCell;
            }
        }

        return true;
    }

    public Vector2Int GetClosestRoadCell(Vector3 worldPosition)
    {
        if (TryGetClosestRoadCell(worldPosition, out Vector2Int closestCell))
        {
            return closestCell;
        }

        return Vector2Int.zero;
    }

    public List<Vector2Int> GetRoadPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> emptyPath = new List<Vector2Int>();

        if (!IsWithinGrid(start) || !IsWithinGrid(end) || !IsRoadCell(start) || !IsRoadCell(end))
        {
            return emptyPath;
        }

        Queue<Vector2Int> openSet = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        openSet.Enqueue(start);
        visited.Add(start);

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.Dequeue();
            if (current == end)
            {
                break;
            }

            foreach (Vector2Int neighbor in GetRoadNeighbors(current))
            {
                if (visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                openSet.Enqueue(neighbor);
            }
        }

        if (!visited.Contains(end))
        {
            return emptyPath;
        }

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentCell = end;
        path.Add(currentCell);

        while (currentCell != start)
        {
            currentCell = cameFrom[currentCell];
            path.Add(currentCell);
        }

        path.Reverse();
        return path;
    }

    private List<Vector2Int> GetRoadNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighbor = cell + direction;
            if (IsWithinGrid(neighbor) && IsRoadCell(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
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
            foreach (Vector2Int cell in occupiedCells)
            {
                if (cell.x < 0 || cell.x >= gridSize || cell.y < 0 || cell.y >= gridSize)
                {
                    continue;
                }

                Gizmos.color = largeOccupiedCells.Contains(cell) ? largeOccupiedCellColor : occupiedCellColor;

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
