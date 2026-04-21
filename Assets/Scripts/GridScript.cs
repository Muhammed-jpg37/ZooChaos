using UnityEngine;
using System.Collections.Generic;

public class GridScript : MonoBehaviour
{
    private enum LockedExpansionSide
    {
        None,
        Bottom,
        Top,
        Left,
        Right
    }

[Header("Grid Layout")]
    [SerializeField] private int startingGridSize = 10;
    public int gridSize = 10; 
    public float cellSize = 1f;
    [SerializeField] private Vector2 startingStartCorner = new Vector2(127, 66);
    public Vector2 startCorner = new Vector2(-5f, -5f); // Bottom-Left Anchor

    [Header("Grid Upgrades")]
    [SerializeField] private int gridUpgradeCost = 100;
    [SerializeField] private int gridUpgradeCostIncrease = 50;
    [SerializeField] private LockedExpansionSide lockedExpansionSide = LockedExpansionSide.None;
    [SerializeField] private Transform groundPlane;
    [SerializeField] private float groundPlaneBaseSize = 10f;

    [Header("Perimeter Walls")]
    [SerializeField] private GameObject exteriorWallPrefab;
    [SerializeField] private Transform exteriorWallParent;
    [SerializeField] private float exteriorWallY = 0f;
    [SerializeField] private Vector3 exteriorWallOffset = Vector3.zero;
    [SerializeField] private Vector3 bottomWallOffset = Vector3.zero;
    [SerializeField] private Vector3 topWallOffset = Vector3.zero;
    [SerializeField] private Vector3 leftWallOffset = Vector3.zero;
    [SerializeField] private Vector3 rightWallOffset = Vector3.zero;
    [SerializeField] private Vector3 bottomLeftCornerOffset = Vector3.zero;
    [SerializeField] private Vector3 bottomRightCornerOffset = Vector3.zero;
    
    [SerializeField] private Vector3 topLeftCornerOffset = Vector3.zero;
    [SerializeField] private Vector3 topRightCornerOffset = Vector3.zero;
    [SerializeField, Range(0f, 1f)] private float exteriorWallAnchorOffsetCells = 0f;
    [SerializeField] private Vector3 bottomWallRotation = Vector3.zero;
    [SerializeField] private Vector3 topWallRotation = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 leftWallRotation = new Vector3(0f, 90f, 0f);
    [SerializeField] private Vector3 rightWallRotation = new Vector3(0f, -90f, 0f);
    [SerializeField] private Vector3 bottomLeftCornerRotation = Vector3.zero;
    [SerializeField] private Vector3 bottomRightCornerRotation = new Vector3(0f, 90f, 0f);
    [SerializeField] private Vector3 topLeftCornerRotation = new Vector3(0f, -90f, 0f);
    [SerializeField] private Vector3 topRightCornerRotation = new Vector3(0f, 180f, 0f);

    [Header("Corner Lights")]
    [SerializeField] private GameObject cornerLightPrefab;
    [SerializeField] private Transform cornerLightParent;
    [SerializeField] private float cornerLightY = 0f;
    [SerializeField] private Vector3 cornerLightOffset = Vector3.zero;
    [SerializeField] private Vector3 bottomLeftCornerLightOffset = Vector3.zero;
    [SerializeField] private Vector3 bottomRightCornerLightOffset = Vector3.zero;
    [SerializeField] private Vector3 topLeftCornerLightOffset = Vector3.zero;
    [SerializeField] private Vector3 topRightCornerLightOffset = Vector3.zero;
    [SerializeField] private Vector3 bottomLeftCornerLightRotation = Vector3.zero;
    [SerializeField] private Vector3 bottomRightCornerLightRotation = Vector3.zero;
    [SerializeField] private Vector3 topLeftCornerLightRotation = Vector3.zero;
    [SerializeField] private Vector3 topRightCornerLightRotation = Vector3.zero;

    [Header("Entry System")]
    [SerializeField] private GameObject entryDoorPrefab;
    [SerializeField] private GameObject entryAccessRoadPrefab;
    [SerializeField] private Transform entryParent;
    [SerializeField] private float entryY = 0f;
    [SerializeField] private Vector3 entryDoorOffset = Vector3.zero;
    [SerializeField] private Vector3 entryAccessOffset = Vector3.zero;
    [SerializeField] private Vector3 entryDoorRotation = Vector3.zero;
    [SerializeField] private Vector3 entryAccessRotation = Vector3.zero;
    [SerializeField] private bool hasEntryPoint;
    [SerializeField] private int entryRightSideRow;
    
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
    private readonly List<GameObject> spawnedExteriorWalls = new List<GameObject>();
    private readonly List<GameObject> spawnedCornerLights = new List<GameObject>();
    private readonly List<GameObject> spawnedEntryObjects = new List<GameObject>();
    private Transform runtimeWallsParent;
    private Transform runtimeCornerLightsParent;
    private Transform runtimeEntryParent;
    private bool hasMarkedEntryRoadCell;
    private Vector2Int markedEntryRoadCell;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapPerimeterVisualsAfterSceneLoad()
    {
        GridScript[] allGrids = Resources.FindObjectsOfTypeAll<GridScript>();
        for (int i = 0; i < allGrids.Length; i++)
        {
            GridScript grid = allGrids[i];
            if (grid == null || !grid.gameObject.scene.IsValid())
            {
                continue;
            }

            grid.EnsurePerimeterVisuals();
        }
    }

    private void Awake()
    {
        ResetToStartingGrid();
        EnsurePerimeterVisuals();
    }

    private void Start()
    {
        EnsurePerimeterVisuals();
    }

    private void OnEnable()
    {
        EnsurePerimeterVisuals();
    }

    private void OnValidate()
    {
        if (groundPlane != null)
        {
            UpdateGroundPlaneScale();
        }

        if (Application.isPlaying)
        {
            EnsurePerimeterVisuals();
        }
    }

    public void EnsurePerimeterVisuals()
    {
        UpdateGroundPlaneScale();
        RebuildExteriorWalls();
        RebuildCornerLights();
        RebuildEntryObjects();
    }

    public bool SetEntryOnRightSide(int oneBasedRow)
    {
        if (gridSize <= 0)
        {
            return false;
        }

        entryRightSideRow = Mathf.Clamp(oneBasedRow - 1, 0, gridSize - 1);
        hasEntryPoint = true;
        EnsurePerimeterVisuals();
        return true;
    }

    public bool TryGetEntrySpawnPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!hasEntryPoint || gridSize <= 0)
        {
            return false;
        }

        worldPosition = GetEntryAccessWorldPosition();
        return true;
    }

    public bool HasEntryPointConfigured => hasEntryPoint;

    public int CurrentGridUpgradeCost => Mathf.Max(0, gridUpgradeCost);

    public bool TryPurchaseGridUpgrade()
    {
        if (ResourceManager.instance == null)
        {
            return false;
        }

        int cost = Mathf.Max(0, gridUpgradeCost);
        if (!ResourceManager.instance.SpendMoney(cost))
        {
            return false;
        }

        float expansion = cellSize * 0.5f;

        float deltaX = -expansion;
        float deltaZ = -expansion;

        switch (lockedExpansionSide)
        {
            case LockedExpansionSide.Bottom:
                deltaZ = -cellSize;
                break;
            case LockedExpansionSide.Top:
                deltaZ = 0f;
                break;
            case LockedExpansionSide.Left:
                deltaX = -cellSize;
                break;
            case LockedExpansionSide.Right:
                deltaX = 0f;
                break;
        }

        startCorner += new Vector2(deltaX, deltaZ);
        ShiftPlacedStructuresByDelta(new Vector3(deltaX, 0f, deltaZ));
        gridSize = Mathf.Max(1, gridSize + 1);
        gridUpgradeCost += Mathf.Max(0, gridUpgradeCostIncrease);
        UpdateGroundPlaneScale();
        RebuildExteriorWalls();
        RebuildCornerLights();
        return true;
    }

    private void ShiftPlacedStructuresByDelta(Vector3 worldDelta)
    {
        BuildingInstance[] allBuildings = FindObjectsOfType<BuildingInstance>();
        for (int i = 0; i < allBuildings.Length; i++)
        {
            BuildingInstance building = allBuildings[i];
            if (building == null)
            {
                continue;
            }

            building.transform.position += worldDelta;
        }
    }

    private void ResetToStartingGrid()
    {
        gridSize = Mathf.Max(1, startingGridSize);
        startCorner = startingStartCorner;
        hasEntryPoint = false;
        hasMarkedEntryRoadCell = false;
        markedEntryRoadCell = default;
        occupiedCells.Clear();
        roadCells.Clear();
        UpdateGroundPlaneScale();
        RebuildExteriorWalls();
        RebuildCornerLights();
        RebuildEntryObjects();
    }

    private void UpdateGroundPlaneScale()
    {
        if (groundPlane == null)
        {
            return;
        }

        float planeWorldSize = Mathf.Max(0.01f, groundPlaneBaseSize);
        float worldSize = gridSize * cellSize;
        float scale = worldSize / planeWorldSize;

        Vector3 currentScale = groundPlane.localScale;
        groundPlane.localScale = new Vector3(scale, currentScale.y, scale);

        Vector3 anchoredPosition = new Vector3(
            startCorner.x + (worldSize * 0.5f),
            groundPlane.position.y,
            startCorner.y + (worldSize * 0.5f)
        );
        groundPlane.position = anchoredPosition;
    }

    private void RebuildExteriorWalls()
    {
        ClearExteriorWalls();

        if (exteriorWallPrefab == null)
        {
            return;
        }

        float worldSize = gridSize * cellSize;
        float endX = startCorner.x + worldSize;
        float endZ = startCorner.y + worldSize;
        float anchorOffset = Mathf.Clamp01(exteriorWallAnchorOffsetCells);

        for (int x = 0; x < gridSize; x++)
        {
            float wallX = startCorner.x + ((x + anchorOffset) * cellSize);

            Vector3 bottomPos = new Vector3(wallX, exteriorWallY, startCorner.y) + exteriorWallOffset + bottomWallOffset;
            SpawnExteriorWall(bottomPos, Quaternion.Euler(bottomWallRotation));

            Vector3 topPos = new Vector3(wallX, exteriorWallY, endZ) + exteriorWallOffset + topWallOffset;
            SpawnExteriorWall(topPos, Quaternion.Euler(topWallRotation));
        }

        for (int z = 0; z < gridSize; z++)
        {
            if (hasEntryPoint && z == Mathf.Clamp(entryRightSideRow, 0, gridSize - 1))
            {
                continue;
            }

            float wallZ = startCorner.y + ((z + anchorOffset) * cellSize);

            Vector3 leftPos = new Vector3(startCorner.x, exteriorWallY, wallZ) + exteriorWallOffset + leftWallOffset;
            SpawnExteriorWall(leftPos, Quaternion.Euler(leftWallRotation));

            Vector3 rightPos = new Vector3(endX, exteriorWallY, wallZ) + exteriorWallOffset + rightWallOffset;
            SpawnExteriorWall(rightPos, Quaternion.Euler(rightWallRotation));
        }
    }

    private void RebuildEntryObjects()
    {
        ClearEntryObjects();
        UpdateEntryRoadCell();

        if (!hasEntryPoint || gridSize <= 0)
        {
            return;
        }

        Transform parent = ResolveSpawnParent(entryParent, ref runtimeEntryParent, "Entry_Runtime");

        if (entryDoorPrefab != null)
        {
            Vector3 doorPos = GetEntryDoorWorldPosition() + entryDoorOffset;
            GameObject door = Instantiate(entryDoorPrefab, doorPos, Quaternion.Euler(entryDoorRotation), parent);
            spawnedEntryObjects.Add(door);
        }

        if (entryAccessRoadPrefab != null)
        {
            Vector3 accessPos = GetEntryAccessWorldPosition() + entryAccessOffset;
            GameObject access = Instantiate(entryAccessRoadPrefab, accessPos, Quaternion.Euler(entryAccessRotation), parent);
            spawnedEntryObjects.Add(access);
        }
    }

    private Vector3 GetEntryDoorWorldPosition()
    {
        float worldSize = gridSize * cellSize;
        float endX = startCorner.x + worldSize;
        int row = Mathf.Clamp(entryRightSideRow, 0, gridSize - 1);
        float rowCenterZ = startCorner.y + ((row + 0.5f) * cellSize);
        return new Vector3(endX, entryY, rowCenterZ);
    }

    private Vector3 GetEntryAccessWorldPosition()
    {
        int row = Mathf.Clamp(entryRightSideRow, 0, gridSize - 1);
        int x = Mathf.Max(0, gridSize - 1);
        return CellToWorldCenter(new Vector2Int(x, row)) + new Vector3(0f, entryY, 0f);
    }

    private void UpdateEntryRoadCell()
    {
        if (hasMarkedEntryRoadCell)
        {
            roadCells.Remove(markedEntryRoadCell);
            occupiedCells.Remove(markedEntryRoadCell);
            hasMarkedEntryRoadCell = false;
        }

        if (!hasEntryPoint || gridSize <= 0)
        {
            return;
        }

        Vector2Int entryCell = new Vector2Int(Mathf.Max(0, gridSize - 1), Mathf.Clamp(entryRightSideRow, 0, gridSize - 1));
        roadCells.Add(entryCell);
        occupiedCells.Add(entryCell);
        markedEntryRoadCell = entryCell;
        hasMarkedEntryRoadCell = true;
    }

    private void ClearEntryObjects()
    {
        for (int i = spawnedEntryObjects.Count - 1; i >= 0; i--)
        {
            GameObject entryObject = spawnedEntryObjects[i];
            if (entryObject == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(entryObject);
            }
            else
            {
                DestroyImmediate(entryObject);
            }
        }

        spawnedEntryObjects.Clear();
    }

    private void RebuildCornerLights()
    {
        ClearCornerLights();

        if (cornerLightPrefab == null)
        {
            return;
        }

        float worldSize = gridSize * cellSize;
        float endX = startCorner.x + worldSize;
        float endZ = startCorner.y + worldSize;

        Vector3 bottomLeftPos = new Vector3(startCorner.x, cornerLightY, startCorner.y)
            + cornerLightOffset
            + bottomLeftCornerLightOffset;
        SpawnCornerLight(bottomLeftPos, Quaternion.Euler(bottomLeftCornerLightRotation));

        Vector3 bottomRightPos = new Vector3(endX, cornerLightY, startCorner.y)
            + cornerLightOffset
            + bottomRightCornerLightOffset;
        SpawnCornerLight(bottomRightPos, Quaternion.Euler(bottomRightCornerLightRotation));

        Vector3 topLeftPos = new Vector3(startCorner.x, cornerLightY, endZ)
            + cornerLightOffset
            + topLeftCornerLightOffset;
        SpawnCornerLight(topLeftPos, Quaternion.Euler(topLeftCornerLightRotation));

        Vector3 topRightPos = new Vector3(endX, cornerLightY, endZ)
            + cornerLightOffset
            + topRightCornerLightOffset;
        SpawnCornerLight(topRightPos, Quaternion.Euler(topRightCornerLightRotation));
    }

    private void SpawnCornerLight(Vector3 position, Quaternion rotation)
    {
        Transform parent = ResolveSpawnParent(cornerLightParent, ref runtimeCornerLightsParent, "CornerLights_Runtime");
        GameObject lightObject = Instantiate(cornerLightPrefab, position, rotation, parent);
        spawnedCornerLights.Add(lightObject);
    }

    private void ClearCornerLights()
    {
        for (int i = spawnedCornerLights.Count - 1; i >= 0; i--)
        {
            GameObject lightObject = spawnedCornerLights[i];
            if (lightObject == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(lightObject);
            }
            else
            {
                DestroyImmediate(lightObject);
            }
        }

        spawnedCornerLights.Clear();
    }

    private void SpawnExteriorWall(Vector3 position, Quaternion rotation)
    {
        Transform parent = ResolveSpawnParent(exteriorWallParent, ref runtimeWallsParent, "PerimeterWalls_Runtime");
        GameObject wall = Instantiate(exteriorWallPrefab, position, rotation, parent);
        spawnedExteriorWalls.Add(wall);
    }

    private Transform ResolveSpawnParent(Transform configuredParent, ref Transform runtimeParentCache, string runtimeRootName)
    {
        if (configuredParent != null && configuredParent.gameObject.activeInHierarchy)
        {
            return configuredParent;
        }

        if (runtimeParentCache == null)
        {
            GameObject runtimeRoot = GameObject.Find(runtimeRootName);
            if (runtimeRoot == null)
            {
                runtimeRoot = new GameObject(runtimeRootName);
            }

            runtimeParentCache = runtimeRoot.transform;
        }

        return runtimeParentCache;
    }

    private void ClearExteriorWalls()
    {
        for (int i = spawnedExteriorWalls.Count - 1; i >= 0; i--)
        {
            GameObject wall = spawnedExteriorWalls[i];
            if (wall == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(wall);
            }
            else
            {
                DestroyImmediate(wall);
            }
        }

        spawnedExteriorWalls.Clear();
    }

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
