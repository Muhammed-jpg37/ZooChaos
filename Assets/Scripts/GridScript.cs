using UnityEngine;
using System.Collections.Generic;

public class GridScript : MonoBehaviour
{
    public enum EntrySide
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
    [SerializeField] private int chunkSize = 10;
    [SerializeField] private int chunkPurchaseCost = 100;
    [SerializeField] private Transform groundPlane;
    [SerializeField] private float groundPlaneBaseSize = 10f;
    [SerializeField] private GameObject shaderPlanePrefab;
    [SerializeField] private Transform shaderPlaneParent;

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
    [SerializeField] private bool hasEntryPoint;
    [SerializeField] public EntrySide entrySide = EntrySide.None;
    [SerializeField] private int entryIndex;
    
    [Header("Visuals")]
    public Color gridColor = Color.green;
    public Color occupiedCellColor = new Color(0.2f, 0.45f, 1f, 0.35f);
    public Color validPreviewColor = new Color(0.2f, 0.9f, 0.3f, 0.35f);
    public Color invalidPreviewColor = new Color(0.95f, 0.2f, 0.2f, 0.35f);

    // Data storage for occupied cells
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> roadCells = new HashSet<Vector2Int>();
    private HashSet<EntrySide> purchasedChunks = new HashSet<EntrySide>();
    private bool hasPreview;
    private int previewX;
    private int previewZ;
    private int previewWidth = 1;
    private int previewDepth = 1;
    private bool previewRequiresFullRoadSide;
    private readonly List<GameObject> spawnedExteriorWalls = new List<GameObject>();
    private readonly List<GameObject> spawnedCornerLights = new List<GameObject>();
    private Transform runtimeWallsParent;
    private Transform runtimeCornerLightsParent;

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
    }

    public bool HasEntryPointConfigured => hasEntryPoint;

    public int CurrentChunkPurchaseCost => Mathf.Max(0, chunkPurchaseCost);

    public bool SetEntryOnCell(int gridX, int gridZ)
    {
        if (gridSize <= 0 || gridX < 0 || gridX >= gridSize || gridZ < 0 || gridZ >= gridSize)
        {
            return false;
        }

        bool isLeftEdge = (gridX == 0);
        bool isRightEdge = (gridX == gridSize - 1);
        bool isBottomEdge = (gridZ == 0);
        bool isTopEdge = (gridZ == gridSize - 1);

        if (isRightEdge)
        {
            entrySide = EntrySide.Right;
            entryIndex = gridZ;
        }
        else if (isLeftEdge)
        {
            entrySide = EntrySide.Left;
            entryIndex = gridZ;
        }
        else if (isTopEdge)
        {
            entrySide = EntrySide.Top;
            entryIndex = gridX;
        }
        else if (isBottomEdge)
        {
            entrySide = EntrySide.Bottom;
            entryIndex = gridX;
        }
        else
        {
            return false;
        }

        hasEntryPoint = true;
        EnsurePerimeterVisuals();
        return true;
    }

    public bool SetEntryOnRightSide(int oneBasedRow)
    {
        if (gridSize <= 0)
        {
            return false;
        }

        entrySide = EntrySide.Right;
        entryIndex = Mathf.Clamp(oneBasedRow - 1, 0, gridSize - 1);
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

        int clampedIndex = Mathf.Clamp(entryIndex, 0, gridSize - 1);
        Vector2Int cell = new Vector2Int(Mathf.Max(0, gridSize - 1), clampedIndex);

        if (entrySide == EntrySide.Left)
        {
            cell = new Vector2Int(0, clampedIndex);
        }
        else if (entrySide == EntrySide.Top)
        {
            cell = new Vector2Int(clampedIndex, Mathf.Max(0, gridSize - 1));
        }
        else if (entrySide == EntrySide.Bottom)
        {
            cell = new Vector2Int(clampedIndex, 0);
        }

        worldPosition = CellToWorldCenter(cell);
        return true;
    }

    public bool TryGetEntryBuildCell(out Vector2Int cell)
    {
        cell = default;

        if (!hasEntryPoint || gridSize <= 0)
        {
            return false;
        }

        int clampedIndex = Mathf.Clamp(entryIndex, 0, gridSize - 1);
        cell = new Vector2Int(Mathf.Max(0, gridSize - 1), clampedIndex);

        if (entrySide == EntrySide.Left)
        {
            cell = new Vector2Int(0, clampedIndex);
        }
        else if (entrySide == EntrySide.Top)
        {
            cell = new Vector2Int(clampedIndex, Mathf.Max(0, gridSize - 1));
        }
        else if (entrySide == EntrySide.Bottom)
        {
            cell = new Vector2Int(clampedIndex, 0);
        }

        return true;
    }

    private bool IsChunkAvailable(EntrySide side)
    {
        // Right side always locked
        if (side == EntrySide.Right)
        {
            return false;
        }

        // Can't purchase on entry side
        if (hasEntryPoint && side == entrySide)
        {
            return false;
        }

        // Can't repurchase
        if (purchasedChunks.Contains(side))
        {
            return false;
        }

        // At start, only purchase from the opposite direction of entry
        // After that, only adjacent chunks (those that touch the expanded grid)
        return true;
    }

    public bool TryPurchaseChunkAtWorldPosition(Vector3 worldPosition)
    {
        float worldSize = gridSize * cellSize;
        float endX = startCorner.x + worldSize;
        float endZ = startCorner.y + worldSize;

        EntrySide side = EntrySide.None;
        if (worldPosition.x < startCorner.x)
        {
            side = EntrySide.Left;
        }
        else if (worldPosition.x >= endX)
        {
            side = EntrySide.Right;
        }
        else if (worldPosition.z < startCorner.y)
        {
            side = EntrySide.Bottom;
        }
        else if (worldPosition.z >= endZ)
        {
            side = EntrySide.Top;
        }

        if (side == EntrySide.None || !IsChunkAvailable(side))
        {
            return false;
        }

        if (ResourceManager.instance == null)
        {
            return false;
        }

        if (!ResourceManager.instance.SpendMoney(CurrentChunkPurchaseCost))
        {
            return false;
        }

        return TryPurchaseChunk(side);
    }

    public bool TryPurchaseChunk(EntrySide side)
    {
        if (chunkSize <= 0 || !IsChunkAvailable(side))
        {
            return false;
        }

        float deltaX = 0f;
        float deltaZ = 0f;
        int addedSize = Mathf.Max(1, chunkSize);

        switch (side)
        {
            case EntrySide.Left:
                deltaX = -addedSize * cellSize;
                break;
            case EntrySide.Bottom:
                deltaZ = -addedSize * cellSize;
                break;
            case EntrySide.Top:
                break;
            case EntrySide.Right:
                return false;
            default:
                return false;
        }

        purchasedChunks.Add(side);
        startCorner += new Vector2(deltaX, deltaZ);
        gridSize = Mathf.Max(1, gridSize + addedSize);
        UpdateGroundPlaneScale();
        RebuildExteriorWalls();
        RebuildCornerLights();
        RebuildEntryObjects();
        SpawnShaderPlaneForChunk(side);
        return true;
    }

    private void SpawnShaderPlaneForChunk(EntrySide side)
    {
        if (shaderPlanePrefab == null)
        {
            return;
        }

        float chunkWorldSize = Mathf.Max(1, chunkSize) * cellSize;
        float currentWorldSize = (gridSize - chunkSize) * cellSize; // Size before this chunk was added
        Vector3 spawnPosition = Vector3.zero;

        switch (side)
        {
            case EntrySide.Left:
                spawnPosition = new Vector3(startCorner.x + (chunkWorldSize * 0.5f), 0f, startCorner.y + (currentWorldSize * 0.5f));
                break;
            case EntrySide.Bottom:
                spawnPosition = new Vector3(startCorner.x + (currentWorldSize * 0.5f), 0f, startCorner.y + (chunkWorldSize * 0.5f));
                break;
            case EntrySide.Top:
                spawnPosition = new Vector3(startCorner.x + (currentWorldSize * 0.5f), 0f, startCorner.y + currentWorldSize + (chunkWorldSize * 0.5f));
                break;
            default:
                return;
        }

        GameObject shaderPlane = Instantiate(shaderPlanePrefab, spawnPosition, Quaternion.identity, shaderPlaneParent);
        shaderPlane.transform.localScale = new Vector3(chunkWorldSize / 10f, 1f, currentWorldSize / 10f);
    }

    private void ResetToStartingGrid()
    {
        gridSize = Mathf.Max(1, startingGridSize);
        startCorner = startingStartCorner;
        hasEntryPoint = false;
        entrySide = EntrySide.None;
        entryIndex = 0;
        occupiedCells.Clear();
        roadCells.Clear();
        purchasedChunks.Clear();
        UpdateGroundPlaneScale();
        RebuildExteriorWalls();
        RebuildCornerLights();
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
            if (hasEntryPoint && entrySide == EntrySide.Left && z == entryIndex)
                continue;
            if (hasEntryPoint && entrySide == EntrySide.Right && z == entryIndex)
                continue;

            float wallZ = startCorner.y + ((z + anchorOffset) * cellSize);

            Vector3 leftPos = new Vector3(startCorner.x, exteriorWallY, wallZ) + exteriorWallOffset + leftWallOffset;
            SpawnExteriorWall(leftPos, Quaternion.Euler(leftWallRotation));

            Vector3 rightPos = new Vector3(endX, exteriorWallY, wallZ) + exteriorWallOffset + rightWallOffset;
            SpawnExteriorWall(rightPos, Quaternion.Euler(rightWallRotation));
        }

        for (int x = 0; x < gridSize; x++)
        {
            if (hasEntryPoint && entrySide == EntrySide.Bottom && x == entryIndex)
                continue;
            if (hasEntryPoint && entrySide == EntrySide.Top && x == entryIndex)
                continue;

            float wallX = startCorner.x + ((x + anchorOffset) * cellSize);

            Vector3 bottomPos = new Vector3(wallX, exteriorWallY, startCorner.y) + exteriorWallOffset + bottomWallOffset;
            SpawnExteriorWall(bottomPos, Quaternion.Euler(bottomWallRotation));

            Vector3 topPos = new Vector3(wallX, exteriorWallY, endZ) + exteriorWallOffset + topWallOffset;
            SpawnExteriorWall(topPos, Quaternion.Euler(topWallRotation));
        }
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
        float worldSize = gridSize * cellSize;
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

        if (hasEntryPoint)
        {
            float chunkWorldSize = Mathf.Max(1, chunkSize) * cellSize;

            // Draw Left chunk
            if (entrySide != EntrySide.Left && EntrySide.Left != EntrySide.Right)
            {
                bool available = IsChunkAvailable(EntrySide.Left);
                Gizmos.color = available ? new Color(1f, 0.5f, 0f, 0.22f) : new Color(0.2f, 0.4f, 1f, 0.22f); // Orange available, Blue locked
                Gizmos.DrawCube(new Vector3(startCorner.x - (chunkWorldSize * 0.5f), 0.01f, startCorner.y + (worldSize * 0.5f)), new Vector3(chunkWorldSize * 0.95f, 0.02f, worldSize * 0.95f));
            }

            // Draw Right chunk (always blocked)
            if (entrySide != EntrySide.Right)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.22f); // Red for blocked
                Gizmos.DrawCube(new Vector3(endX + (chunkWorldSize * 0.5f), 0.01f, startCorner.y + (worldSize * 0.5f)), new Vector3(chunkWorldSize * 0.95f, 0.02f, worldSize * 0.95f));
            }

            // Draw Bottom chunk
            if (entrySide != EntrySide.Bottom && EntrySide.Bottom != EntrySide.Right)
            {
                bool available = IsChunkAvailable(EntrySide.Bottom);
                Gizmos.color = available ? new Color(1f, 0.5f, 0f, 0.22f) : new Color(0.2f, 0.4f, 1f, 0.22f); // Orange available, Blue locked
                Gizmos.DrawCube(new Vector3(startCorner.x + (worldSize * 0.5f), 0.01f, startCorner.y - (chunkWorldSize * 0.5f)), new Vector3(worldSize * 0.95f, 0.02f, chunkWorldSize * 0.95f));
            }

            // Draw Top chunk
            if (entrySide != EntrySide.Top && EntrySide.Top != EntrySide.Right)
            {
                bool available = IsChunkAvailable(EntrySide.Top);
                Gizmos.color = available ? new Color(1f, 0.5f, 0f, 0.22f) : new Color(0.2f, 0.4f, 1f, 0.22f); // Orange available, Blue locked
                Gizmos.DrawCube(new Vector3(startCorner.x + (worldSize * 0.5f), 0.01f, endZ + (chunkWorldSize * 0.5f)), new Vector3(worldSize * 0.95f, 0.02f, chunkWorldSize * 0.95f));
            }
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

    private void RebuildEntryObjects()
    {
        // Entry prefabs are no longer spawned here; the door is handled by the scene setup/UI.
    }
}
