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
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private int coreGridSize = 10;
    [SerializeField] private int coreOriginX;
    [SerializeField] private int coreOriginZ;
    public float cellSize = 1f;
    [SerializeField] private Vector2 startingStartCorner = new Vector2(127, 66);
    public Vector2 startCorner = new Vector2(-5f, -5f); // Bottom-Left Anchor
    [SerializeField] private int chunkSize = 10;
    [SerializeField] private int chunkPurchaseCost = 100;
    [SerializeField] private Transform groundPlane;
    [SerializeField] private GameObject gridVisualPlanePrefab;
    [SerializeField] private Transform gridVisualPlaneParent;
    [SerializeField] private float groundPlaneBaseSize = 10f;
    [SerializeField] private GameObject gridCellPlanePrefab;
    [SerializeField] private Transform gridCellPlaneParent;
    [HideInInspector, SerializeField] private GameObject shaderPlanePrefab;
    [HideInInspector, SerializeField] private Transform shaderPlaneParent;
    [HideInInspector, SerializeField] private GameObject expansionShaderPlanePrefab;
    [HideInInspector, SerializeField] private Transform expansionShaderPlaneParent;
    [SerializeField] private GameObject gridExpansionVisualPrefab;
    [SerializeField] private Transform gridExpansionVisualParent;

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

    [Header("Entry Door")]
    [SerializeField] private GameObject entryDoorPrefab;
    [SerializeField] private Vector3 entryDoorOffset = new Vector3(2.5f,3.5f,2.5f);
    
    [SerializeField] private Vector3 rightEntryDoorRotation = new Vector3(0f, 90f, 0f);
    

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
    private HashSet<Vector2Int> unlockedCells = new HashSet<Vector2Int>();
    private HashSet<EntrySide> purchasedChunks = new HashSet<EntrySide>();
    private HashSet<Vector2Int> purchasedChunkCoords = new HashSet<Vector2Int>();
    private int minChunkX;
    private int minChunkZ;
    private bool hasPreview;
    private int previewX;
    private int previewZ;
    private int previewWidth = 1;
    private int previewDepth = 1;
    private bool previewRequiresFullRoadSide;
    private bool previewHasExternalValidation;
    private bool previewExternalCanPlace;
    private readonly List<GameObject> spawnedExteriorWalls = new List<GameObject>();
    private readonly List<GameObject> spawnedCornerLights = new List<GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> spawnedCellVisuals = new Dictionary<Vector2Int, GameObject>();
    private readonly List<GameObject> spawnedExpansionFrontierVisuals = new List<GameObject>();
    private GameObject spawnedEntryDoor;
    private Transform runtimeWallsParent;
    
    private Transform runtimeCornerLightsParent;
    private Transform runtimeCellVisualsParent;
    private bool expansionFrontierVisible = true;

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

    private void OnDisable()
    {
        ClearExpansionFrontierVisuals();
    }

    private void OnValidate()
    {
        startingGridSize = Mathf.Max(1, startingGridSize);
        coreGridSize = Mathf.Max(1, coreGridSize);

        // Keep newly added dimensions populated for existing serialized scenes/prefabs.
        if (gridWidth <= 0)
        {
            gridWidth = Mathf.Max(1, gridSize);
        }

        if (gridHeight <= 0)
        {
            gridHeight = Mathf.Max(1, gridSize);
        }

        coreOriginX = Mathf.Clamp(coreOriginX, 0, Mathf.Max(0, gridWidth - 1));
        coreOriginZ = Mathf.Clamp(coreOriginZ, 0, Mathf.Max(0, gridHeight - 1));

        if (unlockedCells == null)
        {
            unlockedCells = new HashSet<Vector2Int>();
        }

        if (unlockedCells.Count == 0)
        {
            UnlockRect(0, 0, gridWidth, gridHeight);
        }

        SyncLegacyGridSize();
        MigrateLegacyVisualReferences();

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
        EnsureGroundPlaneInstance();
        UpdateGroundPlaneScale();
        RebuildCellVisuals();
        RebuildExteriorWalls();
        RebuildCornerLights();
        RebuildEntryObjects();
        RebuildExpansionFrontierVisuals();
    }

    public void SetExpansionFrontierVisible(bool isVisible)
    {
        expansionFrontierVisible = isVisible;

        if (!isVisible)
        {
            ClearExpansionFrontierVisuals();
            return;
        }

        RebuildExpansionFrontierVisuals();
    }

    private void EnsureGroundPlaneInstance()
    {
        GameObject prefab = GetOwnedChunkVisualPrefab();
        if (groundPlane != null || prefab == null)
        {
            return;
        }

        Transform parent = GetOwnedChunkVisualParent();
        GameObject plane = Instantiate(prefab);
        plane.transform.SetParent(parent, true);
        groundPlane = plane.transform;
    }

    private void MigrateLegacyVisualReferences()
    {
        if (gridVisualPlanePrefab == null && shaderPlanePrefab != null)
        {
            gridVisualPlanePrefab = shaderPlanePrefab;
        }

        if (gridVisualPlaneParent == null && shaderPlaneParent != null)
        {
            gridVisualPlaneParent = shaderPlaneParent;
        }

        if (gridExpansionVisualPrefab == null && expansionShaderPlanePrefab != null)
        {
            gridExpansionVisualPrefab = expansionShaderPlanePrefab;
        }

        if (gridExpansionVisualParent == null && expansionShaderPlaneParent != null)
        {
            gridExpansionVisualParent = expansionShaderPlaneParent;
        }
    }

    private GameObject GetOwnedChunkVisualPrefab()
    {
        return gridVisualPlanePrefab != null ? gridVisualPlanePrefab : shaderPlanePrefab;
    }

    private Transform GetOwnedChunkVisualParent()
    {
        if (gridVisualPlaneParent != null)
        {
            return gridVisualPlaneParent;
        }

        return shaderPlaneParent != null ? shaderPlaneParent : transform;
    }

    private GameObject GetExpansionFrontierVisualPrefab()
    {
        return gridExpansionVisualPrefab != null ? gridExpansionVisualPrefab : expansionShaderPlanePrefab;
    }

    private Transform GetExpansionFrontierVisualParent()
    {
        if (gridExpansionVisualParent != null)
        {
            return gridExpansionVisualParent;
        }

        return expansionShaderPlaneParent != null ? expansionShaderPlaneParent : transform;
    }

    private GameObject GetCellVisualPrefab()
    {
        if (gridCellPlanePrefab != null)
        {
            return gridCellPlanePrefab;
        }

        if (gridVisualPlanePrefab != null)
        {
            return gridVisualPlanePrefab;
        }

        return shaderPlanePrefab;
    }

    private Transform GetCellVisualParent()
    {
        return ResolveSpawnParent(null, ref runtimeCellVisualsParent, "CellVisuals_Runtime");
    }

    public bool HasEntryPointConfigured => hasEntryPoint;

    public int CurrentChunkPurchaseCost => Mathf.Max(0, chunkPurchaseCost);

    public int CurrentGridWidth => Mathf.Max(1, gridWidth);

    public int CurrentGridHeight => Mathf.Max(1, gridHeight);

    private void SyncLegacyGridSize()
    {
        gridSize = Mathf.Max(CurrentGridWidth, CurrentGridHeight);
    }

    private void UnlockRect(int startX, int startZ, int width, int height)
    {
        for (int x = 0; x < Mathf.Max(1, width); x++)
        {
            for (int z = 0; z < Mathf.Max(1, height); z++)
            {
                unlockedCells.Add(new Vector2Int(startX + x, startZ + z));
            }
        }
    }

    private void ShiftCellSet(HashSet<Vector2Int> cells, int deltaX, int deltaZ)
    {
        if (cells == null || cells.Count == 0)
        {
            return;
        }

        HashSet<Vector2Int> shifted = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            shifted.Add(new Vector2Int(cell.x + deltaX, cell.y + deltaZ));
        }

        cells.Clear();
        foreach (Vector2Int cell in shifted)
        {
            cells.Add(cell);
        }
    }

    private void ShiftGridCellData(int deltaX, int deltaZ)
    {
        ShiftCellSet(unlockedCells, deltaX, deltaZ);
        ShiftCellSet(occupiedCells, deltaX, deltaZ);
        ShiftCellSet(roadCells, deltaX, deltaZ);

        if (hasPreview)
        {
            previewX += deltaX;
            previewZ += deltaZ;
        }
    }

    private void ShiftEntryIndexWithGrid(int deltaX, int deltaZ)
    {
        if (!hasEntryPoint || entrySide == EntrySide.None)
        {
            return;
        }

        switch (entrySide)
        {
            case EntrySide.Left:
            case EntrySide.Right:
                entryIndex += deltaZ;
                break;
            case EntrySide.Top:
            case EntrySide.Bottom:
                entryIndex += deltaX;
                break;
        }
    }

    private bool IsCellUnlocked(int gridX, int gridZ)
    {
        return unlockedCells.Contains(new Vector2Int(gridX, gridZ));
    }

    private static Vector2Int SideToChunkOffset(EntrySide side)
    {
        switch (side)
        {
            case EntrySide.Left:
                return new Vector2Int(-1, 0);
            case EntrySide.Right:
                return new Vector2Int(1, 0);
            case EntrySide.Bottom:
                return new Vector2Int(0, -1);
            case EntrySide.Top:
                return new Vector2Int(0, 1);
            default:
                return Vector2Int.zero;
        }
    }

    private bool IsChunkAdjacentToPurchased(Vector2Int chunkCoord)
    {
        Vector2Int[] neighbors =
        {
            chunkCoord + Vector2Int.left,
            chunkCoord + Vector2Int.right,
            chunkCoord + Vector2Int.up,
            chunkCoord + Vector2Int.down
        };

        for (int i = 0; i < neighbors.Length; i++)
        {
            if (purchasedChunkCoords.Contains(neighbors[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void GetPurchasedChunkBounds(out int minX, out int maxX, out int minZ, out int maxZ)
    {
        minX = int.MaxValue;
        maxX = int.MinValue;
        minZ = int.MaxValue;
        maxZ = int.MinValue;

        foreach (Vector2Int chunk in purchasedChunkCoords)
        {
            minX = Mathf.Min(minX, chunk.x);
            maxX = Mathf.Max(maxX, chunk.x);
            minZ = Mathf.Min(minZ, chunk.y);
            maxZ = Mathf.Max(maxZ, chunk.y);
        }

        if (minX == int.MaxValue)
        {
            minX = 0;
            maxX = 0;
            minZ = 0;
            maxZ = 0;
            Debug.Log("I am so sorry you had to see this. Purchased chunks are empty.");
        }
    }

    private bool IsBlockedByEntrySide(Vector2Int chunkCoord)
    {
        if (!hasEntryPoint || entrySide == EntrySide.None)
        {
            return false;
        }

        GetPurchasedChunkBounds(out int minX, out int maxX, out int minZ, out int maxZ);

        switch (entrySide)
        {
            case EntrySide.Right:
                return chunkCoord.x > maxX;
            case EntrySide.Left:
                return chunkCoord.x < minX;
            case EntrySide.Top:
                return chunkCoord.y > maxZ;
            case EntrySide.Bottom:
                return chunkCoord.y < minZ;
            default:
                return false;
        }
    }

    private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
    {
        float chunkWorld = Mathf.Max(1, chunkSize) * cellSize;
        int chunkX = Mathf.FloorToInt((worldPosition.x - startingStartCorner.x) / chunkWorld);
        int chunkZ = Mathf.FloorToInt((worldPosition.z - startingStartCorner.y) / chunkWorld);
        return new Vector2Int(chunkX, chunkZ);
    }

    private void RebuildGridFromPurchasedChunks()
    {
        if (purchasedChunkCoords.Count == 0)
        {
            purchasedChunkCoords.Add(Vector2Int.zero);
        }

        int oldMinChunkX = minChunkX;
        int oldMinChunkZ = minChunkZ;

        int maxChunkX = int.MinValue;
        int maxChunkZ = int.MinValue;
        minChunkX = int.MaxValue;
        minChunkZ = int.MaxValue;

        foreach (Vector2Int chunk in purchasedChunkCoords)
        {
            minChunkX = Mathf.Min(minChunkX, chunk.x);
            minChunkZ = Mathf.Min(minChunkZ, chunk.y);
            maxChunkX = Mathf.Max(maxChunkX, chunk.x);
            maxChunkZ = Mathf.Max(maxChunkZ, chunk.y);
        }

        int unit = Mathf.Max(1, chunkSize);
        int deltaCellsX = (oldMinChunkX - minChunkX) * unit;
        int deltaCellsZ = (oldMinChunkZ - minChunkZ) * unit;
        ShiftGridCellData(deltaCellsX, deltaCellsZ);
        ShiftEntryIndexWithGrid(deltaCellsX, deltaCellsZ);

        gridWidth = (maxChunkX - minChunkX + 1) * unit;
        gridHeight = (maxChunkZ - minChunkZ + 1) * unit;
        coreGridSize = unit;
        coreOriginX = -minChunkX * unit;
        coreOriginZ = -minChunkZ * unit;

        if (hasEntryPoint)
        {
            int sideLength = (entrySide == EntrySide.Left || entrySide == EntrySide.Right) ? gridHeight : gridWidth;
            entryIndex = Mathf.Clamp(entryIndex, 0, Mathf.Max(0, sideLength - 1));
        }

        unlockedCells.Clear();
        foreach (Vector2Int chunk in purchasedChunkCoords)
        {
            int originX = (chunk.x - minChunkX) * unit;
            int originZ = (chunk.y - minChunkZ) * unit;
            UnlockRect(originX, originZ, unit, unit);
        }

        startCorner = new Vector2(
            startingStartCorner.x + (minChunkX * unit * cellSize),
            startingStartCorner.y + (minChunkZ * unit * cellSize)
        );

        SyncLegacyGridSize();
    }

    private bool TryPurchaseChunkAtCoord(Vector2Int chunkCoord)
    {
        if (purchasedChunkCoords.Contains(chunkCoord))
        {
            return false;
        }

        if (IsBlockedByEntrySide(chunkCoord))
        {
            return false;
        }

        if (!IsChunkAdjacentToPurchased(chunkCoord))
        {
            return false;
        }

        purchasedChunkCoords.Add(chunkCoord);
        RebuildGridFromPurchasedChunks();
        RebuildCellVisuals();
        UpdateGroundPlaneScale();
        RebuildExteriorWalls();
        RebuildCornerLights();
        RebuildExpansionFrontierVisuals();
        RebuildEntryObjects();
        return true;
    }

    private HashSet<Vector2Int> GetFrontierChunkCoords()
    {
        HashSet<Vector2Int> frontier = new HashSet<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

        foreach (Vector2Int owned in purchasedChunkCoords)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int candidate = owned + directions[i];
                if (purchasedChunkCoords.Contains(candidate) || IsBlockedByEntrySide(candidate))
                {
                    continue;
                }

                frontier.Add(candidate);
            }
        }

        return frontier;
    }

    private void RebuildExpansionFrontierVisuals()
    {
        ClearExpansionFrontierVisuals();

        if (!hasEntryPoint || !expansionFrontierVisible)
        {
            return;
        }

        GameObject prefab = GetExpansionFrontierVisualPrefab();

        if (prefab == null)
        {
            return;
        }

        float chunkWorld = Mathf.Max(1, chunkSize) * cellSize;
        Transform parent = GetExpansionFrontierVisualParent();

        HashSet<Vector2Int> frontier = GetFrontierChunkCoords();
        foreach (Vector2Int candidate in frontier)
        {
            Vector3 center = new Vector3(
                startCorner.x + ((candidate.x - minChunkX + 0.5f) * chunkWorld),
                0f,
                startCorner.y + ((candidate.y - minChunkZ + 0.5f) * chunkWorld)
            );

            GameObject visual = Instantiate(prefab, center, Quaternion.identity, parent);
            visual.transform.localScale = new Vector3(chunkWorld / 10f, 1f, chunkWorld / 10f);
            spawnedExpansionFrontierVisuals.Add(visual);
        }
    }

    private void ClearExpansionFrontierVisuals()
    {
        for (int i = spawnedExpansionFrontierVisuals.Count - 1; i >= 0; i--)
        {
            GameObject visual = spawnedExpansionFrontierVisuals[i];
            if (visual == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(visual);
            }
            else
            {
                DestroyImmediate(visual);
            }
        }

        spawnedExpansionFrontierVisuals.Clear();
    }

    public bool SetEntryOnCell(int gridX, int gridZ)
    {
        if (CurrentGridWidth <= 0 || CurrentGridHeight <= 0 || gridX < 0 || gridX >= CurrentGridWidth || gridZ < 0 || gridZ >= CurrentGridHeight)
        {
            return false;
        }

        if (!IsCellUnlocked(gridX, gridZ))
        {
            return false;
        }

        bool isLeftEdge = (gridX == 0);
        bool isRightEdge = (gridX == CurrentGridWidth - 1);
        bool isBottomEdge = (gridZ == 0);
        bool isTopEdge = (gridZ == CurrentGridHeight - 1);

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
        LogSelectedEntryTileTransformDetails();
        EnsurePerimeterVisuals();
        return true;
    }

    public bool SetEntryOnRightSide(int oneBasedRow)
    {
        if (CurrentGridHeight <= 0)
        {
            return false;
        }

        entrySide = EntrySide.Right;
        entryIndex = Mathf.Clamp(oneBasedRow - 1, 0, CurrentGridHeight - 1);
        hasEntryPoint = true;
        LogSelectedEntryTileTransformDetails();
        EnsurePerimeterVisuals();
        return true;
    }

    private void LogSelectedEntryTileTransformDetails()
    {
        if (!TryGetEntryBuildCell(out Vector2Int entryCell))
        {
            return;
        }

        Vector3 tileCenter = CellToWorldCenter(entryCell);
        Vector3 doorSpawnPosition = GetEntryDoorWorldPosition(tileCenter);

        Debug.Log(
            $"[GridScript] Selected entry tile details | Side: {entrySide} | Index: {entryIndex} | GridCell: {entryCell} | " +
            $"TileCenter(world): {tileCenter} | DoorSpawn(world): {doorSpawnPosition} | GridObject(world): {transform.position}");
    }

    public bool TryGetEntrySpawnPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!hasEntryPoint || CurrentGridWidth <= 0 || CurrentGridHeight <= 0)
        {
            return false;
        }

        if (spawnedEntryDoor != null)
        {
            Transform liveDoorSpawnPoint = FindEntryDoorSpawnPoint(spawnedEntryDoor.transform);
            if (liveDoorSpawnPoint != null)
            {
                worldPosition = liveDoorSpawnPoint.position;
                return true;
            }
        }

        int sideLength = (entrySide == EntrySide.Left || entrySide == EntrySide.Right) ? CurrentGridHeight : CurrentGridWidth;
        int clampedIndex = Mathf.Clamp(entryIndex, 0, sideLength - 1);
        Vector2Int cell = GetEntryBuildCellInternal(clampedIndex);
        Vector3 cellCenter = CellToWorldCenter(cell);
        worldPosition = GetEntryDoorWorldPosition(cellCenter) + entryDoorOffset;
        return true;
    }

    public bool TryGetEntryBuildCell(out Vector2Int cell)
    {
        cell = default;

        if (!hasEntryPoint || CurrentGridWidth <= 0 || CurrentGridHeight <= 0)
        {
            return false;
        }

        int sideLength = (entrySide == EntrySide.Left || entrySide == EntrySide.Right) ? CurrentGridHeight : CurrentGridWidth;
        int clampedIndex = Mathf.Clamp(entryIndex, 0, sideLength - 1);
        cell = GetEntryBuildCellInternal(clampedIndex);

        return true;
    }

    private Vector2Int GetEntryBuildCellInternal(int clampedIndex)
    {
        Vector2Int cell = new Vector2Int(Mathf.Max(0, CurrentGridWidth - 1), clampedIndex);

        if (entrySide == EntrySide.Left)
        {
            cell = new Vector2Int(0, clampedIndex);
        }
        else if (entrySide == EntrySide.Top)
        {
            cell = new Vector2Int(clampedIndex, Mathf.Max(0, CurrentGridHeight - 1));
        }
        else if (entrySide == EntrySide.Bottom)
        {
            cell = new Vector2Int(clampedIndex, 0);
        }

        return cell;
    }

    private Vector3 GetEntryDoorWorldPosition(Vector3 cellCenter)
    {
        

        float localX = ((cellCenter.x - startCorner.x) / cellSize) - 0.5f;
        float localZ = ((cellCenter.z - startCorner.y) / cellSize) - 0.5f;

        int snappedCellX = Mathf.RoundToInt(localX);
        int snappedCellZ = Mathf.RoundToInt(localZ);

        cellCenter.x = startCorner.x + ((snappedCellX + 0.5f) * cellSize);
        cellCenter.z = startCorner.y + ((snappedCellZ + 0.5f) * cellSize);
        return cellCenter;
    }

    private bool IsChunkAvailable(EntrySide side)
    {
        if (side == EntrySide.None)
        {
            return false;
        }

        // Can't purchase on entry side
        if (hasEntryPoint && side == entrySide)
        {
            return false;
        }

        Vector2Int chunkOffset = SideToChunkOffset(side);
        if (purchasedChunkCoords.Contains(chunkOffset))
        {
            return false;
        }

        if (IsBlockedByEntrySide(chunkOffset))
        {
            return false;
        }

        return IsChunkAdjacentToPurchased(chunkOffset);
    }

    public bool TryPurchaseChunkAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int clickedChunk = WorldToChunkCoord(worldPosition);
        if (purchasedChunkCoords.Contains(clickedChunk) || IsBlockedByEntrySide(clickedChunk) || !IsChunkAdjacentToPurchased(clickedChunk))
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

        return TryPurchaseChunkAtCoord(clickedChunk);
    }

    public bool TryPurchaseChunk(EntrySide side)
    {
        if (chunkSize <= 0 || side == EntrySide.None)
        {
            return false;
        }

        Vector2Int target = SideToChunkOffset(side);
        return TryPurchaseChunkAtCoord(target);
    }

    private void ResetToStartingGrid()
    {
        int initialSize = Mathf.Max(1, chunkSize);
        coreGridSize = initialSize;
        purchasedChunkCoords.Clear();
        purchasedChunkCoords.Add(Vector2Int.zero);
        minChunkX = 0;
        minChunkZ = 0;
        hasEntryPoint = false;
        entrySide = EntrySide.None;
        entryIndex = 0;
        occupiedCells.Clear();
        roadCells.Clear();
        unlockedCells.Clear();
        purchasedChunks.Clear();
        ClearExpansionFrontierVisuals();
        RebuildGridFromPurchasedChunks();
        purchasedChunks.Clear();
        UpdateGroundPlaneScale();
        RebuildExteriorWalls();
        RebuildCornerLights();
        RebuildEntryObjects();
        RebuildExpansionFrontierVisuals();
    }

    private void UpdateGroundPlaneScale()
    {
        EnsureGroundPlaneInstance();

        if (groundPlane == null)
        {
            return;
        }

        float planeWorldSize = Mathf.Max(0.01f, groundPlaneBaseSize);
        float worldWidth = CurrentGridWidth * cellSize;
        float worldHeight = CurrentGridHeight * cellSize;
        float scaleX = worldWidth / planeWorldSize;
        float scaleZ = worldHeight / planeWorldSize;

        Vector3 currentScale = groundPlane.localScale;
        groundPlane.localScale = new Vector3(scaleX, currentScale.y, scaleZ);

        Vector3 anchoredPosition = new Vector3(
            startCorner.x + (worldWidth * 0.5f),
            groundPlane.position.y,
            startCorner.y + (worldHeight * 0.5f)
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

        float anchorOffset = Mathf.Clamp01(exteriorWallAnchorOffsetCells);

        foreach (Vector2Int cell in unlockedCells)
        {
            if (cell.x < 0 || cell.x >= CurrentGridWidth || cell.y < 0 || cell.y >= CurrentGridHeight)
            {
                continue;
            }

            bool hasBottomNeighbor = IsCellUnlocked(cell.x, cell.y - 1);
            bool hasTopNeighbor = IsCellUnlocked(cell.x, cell.y + 1);
            bool hasLeftNeighbor = IsCellUnlocked(cell.x - 1, cell.y);
            bool hasRightNeighbor = IsCellUnlocked(cell.x + 1, cell.y);

            float wallX = startCorner.x + ((cell.x + 0.5f + anchorOffset) * cellSize);
            float wallZ = startCorner.y + ((cell.y + 0.5f + anchorOffset) * cellSize);

            if (!hasBottomNeighbor && !ShouldSkipEntryGap(cell, EntrySide.Bottom))
            {
                Vector3 bottomPos = new Vector3(wallX, exteriorWallY, startCorner.y + (cell.y * cellSize)) + exteriorWallOffset + bottomWallOffset;
                SpawnExteriorWall(bottomPos, Quaternion.Euler(bottomWallRotation));
            }

            if (!hasTopNeighbor && !ShouldSkipEntryGap(cell, EntrySide.Top))
            {
                Vector3 topPos = new Vector3(wallX, exteriorWallY, startCorner.y + ((cell.y + 1) * cellSize)) + exteriorWallOffset + topWallOffset;
                SpawnExteriorWall(topPos, Quaternion.Euler(topWallRotation));
            }

            if (!hasLeftNeighbor && !ShouldSkipEntryGap(cell, EntrySide.Left))
            {
                Vector3 leftPos = new Vector3(startCorner.x + (cell.x * cellSize), exteriorWallY, wallZ) + exteriorWallOffset + leftWallOffset;
                SpawnExteriorWall(leftPos, Quaternion.Euler(leftWallRotation));
            }

            if (!hasRightNeighbor && !ShouldSkipEntryGap(cell, EntrySide.Right))
            {
                Vector3 rightPos = new Vector3(startCorner.x + ((cell.x + 1) * cellSize), exteriorWallY, wallZ) + exteriorWallOffset + rightWallOffset;
                SpawnExteriorWall(rightPos, Quaternion.Euler(rightWallRotation));
            }
        }
    }

    private bool ShouldSkipEntryGap(Vector2Int cell, EntrySide edge)
    {
        if (!hasEntryPoint || entrySide != edge)
        {
            return false;
        }

        switch (edge)
        {
            case EntrySide.Left:
                return cell.x == 0 && cell.y == entryIndex;
            case EntrySide.Right:
                return cell.x == CurrentGridWidth - 1 && cell.y == entryIndex;
            case EntrySide.Bottom:
                return cell.y == 0 && cell.x == entryIndex;
            case EntrySide.Top:
                return cell.y == CurrentGridHeight - 1 && cell.x == entryIndex;
            default:
                return false;
        }
    }

    private void RebuildCornerLights()
    {
        ClearCornerLights();

        if (cornerLightPrefab == null)
        {
            return;
        }

        float worldWidth = CurrentGridWidth * cellSize;
        float worldHeight = CurrentGridHeight * cellSize;
        float endX = startCorner.x + worldWidth;
        float endZ = startCorner.y + worldHeight;

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
        previewHasExternalValidation = false;
    }

    public void SetPlacementPreviewValidation(bool canPlace)
    {
        previewHasExternalValidation = true;
        previewExternalCanPlace = canPlace;
    }

    public void ClearPlacementPreview()
    {
        hasPreview = false;
        previewRequiresFullRoadSide = false;
        previewHasExternalValidation = false;
    }
    /// <summary>
    /// Checks if a building footprint is clear.
    /// gridX/gridZ are the local grid indices.
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
                if (checkX < 0 || checkX >= CurrentGridWidth || checkZ < 0 || checkZ >= CurrentGridHeight)
                    return false;

                if (!IsCellUnlocked(checkX, checkZ))
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

                RemoveCellVisualAt(cell);
            }
        }
    }

    private void RebuildCellVisuals()
    {
        ClearCellVisuals();

        GameObject prefab = GetCellVisualPrefab();
        if (prefab == null)
        {
            return;
        }

        Transform parent = GetCellVisualParent();
        float planeScale = Mathf.Max(0.01f, cellSize) / 10f;

        for (int x = 0; x < CurrentGridWidth; x++)
        {
            for (int z = 0; z < CurrentGridHeight; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!IsCellUnlocked(x, z) || occupiedCells.Contains(cell))
                {
                    continue;
                }

                SpawnCellVisualAt(cell, prefab, parent, planeScale);
            }
        }
    }

    private void SpawnCellVisualAt(Vector2Int cell, GameObject prefab, Transform parent, float planeScale)
    {
        if (spawnedCellVisuals.ContainsKey(cell))
        {
            RemoveCellVisualAt(cell);
        }

        Vector3 center = CellToWorldCenter(cell);
        GameObject cellVisual = Instantiate(prefab, center, Quaternion.identity, parent);
        Vector3 currentScale = cellVisual.transform.localScale;
        cellVisual.transform.localScale = new Vector3(planeScale, currentScale.y, planeScale);
        spawnedCellVisuals[cell] = cellVisual;
    }

    private void RemoveCellVisualAt(Vector2Int cell)
    {
        if (!spawnedCellVisuals.TryGetValue(cell, out GameObject cellVisual) || cellVisual == null)
        {
            spawnedCellVisuals.Remove(cell);
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(cellVisual);
        }
        else
        {
            DestroyImmediate(cellVisual);
        }

        spawnedCellVisuals.Remove(cell);
    }

    private void ClearCellVisuals()
    {
        foreach (GameObject cellVisual in spawnedCellVisuals.Values)
        {
            if (cellVisual == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(cellVisual);
            }
            else
            {
                DestroyImmediate(cellVisual);
            }
        }

        spawnedCellVisuals.Clear();
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
        return cell.x >= 0 && cell.x < CurrentGridWidth && cell.y >= 0 && cell.y < CurrentGridHeight && IsCellUnlocked(cell.x, cell.y);
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

            if (cellX < 0 || cellX >= CurrentGridWidth || cellZ < 0 || cellZ >= CurrentGridHeight)
                return false;

            if (!IsCellUnlocked(cellX, cellZ))
                return false;

            if (!roadCells.Contains(new Vector2Int(cellX, cellZ)))
                return false;
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gridColor;
        
        float worldWidth = CurrentGridWidth * cellSize;
        float worldHeight = CurrentGridHeight * cellSize;
        float endX = startCorner.x + worldWidth;
        float endZ = startCorner.y + worldHeight;

        if (unlockedCells != null)
        {
            foreach (Vector2Int unlockedCell in unlockedCells)
            {
                if (unlockedCell.x < 0 || unlockedCell.x >= CurrentGridWidth || unlockedCell.y < 0 || unlockedCell.y >= CurrentGridHeight)
                {
                    continue;
                }

                Vector3 center = new Vector3(
                    startCorner.x + (unlockedCell.x + 0.5f) * cellSize,
                    0f,
                    startCorner.y + (unlockedCell.y + 0.5f) * cellSize
                );

                Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.001f, cellSize));
            }
        }

        if (hasEntryPoint)
        {
            // Expansion frontier visuals are spawned with prefab instances instead of Gizmos.
        }

        if (occupiedCells != null && occupiedCells.Count > 0)
        {
            Gizmos.color = occupiedCellColor;
            foreach (Vector2Int cell in occupiedCells)
            {
                if (cell.x < 0 || cell.x >= CurrentGridWidth || cell.y < 0 || cell.y >= CurrentGridHeight)
                {
                    continue;
                }

                if (!IsCellUnlocked(cell.x, cell.y))
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

        bool canPlacePreview;
        if (previewHasExternalValidation)
        {
            canPlacePreview = previewExternalCanPlace;
        }
        else
        {
         
            canPlacePreview = CanPlaceBuilding(previewX, previewZ, previewWidth, previewDepth);
        }

        if (!previewHasExternalValidation && canPlacePreview && previewRequiresFullRoadSide)
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

                if (cellX < 0 || cellX >= CurrentGridWidth || cellZ < 0 || cellZ >= CurrentGridHeight)
                {
                    continue;
                }

                if (!IsCellUnlocked(cellX, cellZ))
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
        ClearEntryDoor();

        if (!hasEntryPoint || entryDoorPrefab == null)
        {
            return;
        }

        if (!TryGetEntryBuildCell(out Vector2Int entryCell))
        {
            return;
        }

        Quaternion rotation = Quaternion.identity;
        switch (entrySide)
        {
        
            case EntrySide.Right:
                rotation = Quaternion.Euler(rightEntryDoorRotation);
                break;
           

        }

        Vector3 spawnPosition = GetEntryDoorWorldPosition(CellToWorldCenter(entryCell)) + entryDoorOffset;
        spawnedEntryDoor = Instantiate(entryDoorPrefab, spawnPosition, rotation);

        Transform doorSpawnPoint = FindEntryDoorSpawnPoint(spawnedEntryDoor.transform);
        Vector3 resolvedSpawnPosition = doorSpawnPoint != null ? doorSpawnPoint.position : spawnPosition;

        PlayerMovementController playerMovementController = FindObjectOfType<PlayerMovementController>();
        if (playerMovementController != null)
        {
            playerMovementController.SetStartPoint(resolvedSpawnPosition);
            playerMovementController.ResetToStartPoint();
        }

        if (ResourceManager.instance != null)
        {
            ResourceManager.instance.SetCustomerSpawnPoint(doorSpawnPoint != null ? doorSpawnPoint : spawnedEntryDoor.transform);
        }
    }

    private void ClearEntryDoor()
    {
        if (spawnedEntryDoor == null)
        {
            Debug.Log("sa");
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(spawnedEntryDoor);
        }
        else
        {
            DestroyImmediate(spawnedEntryDoor);
        }

        spawnedEntryDoor = null;

        PlayerMovementController playerMovementController = FindObjectOfType<PlayerMovementController>();
        if (playerMovementController != null)
        {
            playerMovementController.ClearStartPoint();
        }

        if (ResourceManager.instance != null)
        {
            ResourceManager.instance.SetCustomerSpawnPoint(null);
        }
    }

    private Transform FindEntryDoorSpawnPoint(Transform doorRoot)
    {
        if (doorRoot == null)
        {
            return null;
        }

        Transform taggedSpawnPoint = FindChildWithTagRecursive(doorRoot, "entryPoint");
        if (taggedSpawnPoint != null)
        {
            return taggedSpawnPoint;
        }

        Transform namedSpawnPoint = FindChildTransformRecursive(doorRoot, "EmptyObject", true);
        if (namedSpawnPoint != null)
        {
            return namedSpawnPoint;
        }

        Transform containsSpawnInName = FindChildByNameContainsRecursive(doorRoot, "spawn");
        if (containsSpawnInName != null)
        {
            return containsSpawnInName;
        }

        return doorRoot;
    }

    private Transform FindChildTransformRecursive(Transform root, string targetName, bool ignoreCase)
    {
        if (root == null)
        {
            return null;
        }

        bool isMatch = ignoreCase
            ? string.Equals(root.name, targetName, System.StringComparison.OrdinalIgnoreCase)
            : root.name == targetName;

        if (isMatch)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildTransformRecursive(child, targetName, ignoreCase);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private Transform FindChildByNameContainsRecursive(Transform root, string nameFragment)
    {
        if (root == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(root.name) &&
            root.name.IndexOf(nameFragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildByNameContainsRecursive(child, nameFragment);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private Transform FindChildWithTagRecursive(Transform root, string tagName)
    {
        if (root == null)
        {
            return null;
        }

        try
        {
            if (root.CompareTag(tagName))
            {
                return root;
            }
        }
        catch (UnityException)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildWithTagRecursive(child, tagName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
