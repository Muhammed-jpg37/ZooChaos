using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class BuildConstruction : MonoBehaviour
{
    private int gridX = -1;
    private int gridY = -1;
    private int databaseIndex = -1;
    [Header("Building Preview")]
    [SerializeField] private Color previewValidColor = new Color(0.2f, 1f, 0.2f, 0.45f);
    [SerializeField] private Color previewInvalidColor = new Color(1f, 0.2f, 0.2f, 0.45f);
    [SerializeField] private bool followMouseCursor = true;
    [SerializeField] private bool buildOnLeftClick = true;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private LayerMask previewSurfaceMask = ~0;
    [SerializeField] private float previewRaycastDistance = 2000f;
    [SerializeField] private TMP_Text previewSizeText;
    [Header("Construction Effects")]
    [SerializeField] private GameObject constructionSmokePrefab;
    [SerializeField] private float constructionSmokeDuration = 1f;
    [SerializeField] private Vector3 constructionSmokeOffset = new Vector3(0f, 0.5f, 0f);
    public bool LastConstructionSucceeded { get; private set; }
    public int LastConstructedGridX { get; private set; } = -1;
    public int LastConstructedGridY { get; private set; } = -1;
    public int LastConstructedWidth { get; private set; }
    public int LastConstructedDepth { get; private set; }
    public BuySystemManager.BuildingType LastConstructedBuildingType { get; private set; } = BuySystemManager.BuildingType.None;
    public static BuildConstruction instance { get; private set; }

    private GameObject previewInstance;
    private Renderer[] previewRenderers;
    private BuySystemManager.BuildingType previewType = BuySystemManager.BuildingType.None;
    private MaterialPropertyBlock previewPropertyBlock;

    private int GridToInternalIndex(int oneBasedIndex)
    {
        return oneBasedIndex - 1;
    }

    private void GetFootprintOriginFromCenter(int centerGridX, int centerGridY, int width, int depth, out int originX, out int originY)
    {
        int centerInternalX = GridToInternalIndex(centerGridX);
        int centerInternalY = GridToInternalIndex(centerGridY);

        originX = centerInternalX - (width / 2);
        originY = centerInternalY - (depth / 2);
    }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
        }

        previewPropertyBlock = new MaterialPropertyBlock();

        if (previewCamera == null)
        {
            previewCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!followMouseCursor)
        {
            return;
        }

        if (databaseIndex <= 0 || !System.Enum.IsDefined(typeof(BuySystemManager.BuildingType), databaseIndex))
        {
            return;
        }

        if (TryGetGridPositionFromMouse(out int mouseGridX, out int mouseGridY))
        {
            if (mouseGridX != gridX || mouseGridY != gridY)
            {
                gridX = mouseGridX;
                gridY = mouseGridY;
                UpdatePlacementPreview();
            }

            if (buildOnLeftClick && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                LastConstructionSucceeded = false;
                LastConstructedGridX = -1;
                LastConstructedGridY = -1;
                LastConstructedWidth = 0;
                LastConstructedDepth = 0;
                LastConstructedBuildingType = BuySystemManager.BuildingType.None;
                LastConstructionSucceeded = TryConstructBuilding();
                UpdatePlacementPreview();
            }
        }
        else
        {
            GridScript gridScript = FindObjectOfType<GridScript>();
            if (gridScript != null)
            {
                gridScript.ClearPlacementPreview();
            }

            ClearBuildingPreviewVisual();
            SetPreviewSizeText(string.Empty);
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    public void GetGridPosition(int x, int y) {
        if (followMouseCursor)
        {
            return;
        }

        gridX = x;
        gridY = y;
        LastConstructionSucceeded = false;
        LastConstructedGridX = -1;
        LastConstructedGridY = -1;
        LastConstructedWidth = 0;
        LastConstructedDepth = 0;
        LastConstructedBuildingType = BuySystemManager.BuildingType.None;
        UpdatePlacementPreview();
        LastConstructionSucceeded = TryConstructBuilding();
    }

    public void GetBuildingType(int buildingIndex) {
        this.databaseIndex = buildingIndex;
        UpdatePlacementPreview();

        if (databaseIndex <= 0 || !System.Enum.IsDefined(typeof(BuySystemManager.BuildingType), databaseIndex) || BuySystemManager.instance == null)
        {
            SetPreviewSizeText(string.Empty);
            return;
        }

        BuySystemManager.BuildingType selectedType = (BuySystemManager.BuildingType)databaseIndex;
        if (BuySystemManager.instance.TryGetBuildingData(selectedType, out GameObject _, out int width, out int depth))
        {
            SetPreviewSizeText($"Size: {width}x{depth}");
        }
        else
        {
            SetPreviewSizeText(string.Empty);
        }
    }

    private void UpdatePlacementPreview()
    {
        GridScript gridScript = FindObjectOfType<GridScript>();
        if (gridScript == null)
        {
            ClearBuildingPreviewVisual();
            return;
        }

        if (gridX < 1 || gridY < 1)
        {
            gridScript.ClearPlacementPreview();
            ClearBuildingPreviewVisual();
            return;
        }

        if (databaseIndex <= 0 || !System.Enum.IsDefined(typeof(BuySystemManager.BuildingType), databaseIndex))
        {
            gridScript.ClearPlacementPreview();
            ClearBuildingPreviewVisual();
            return;
        }

        if (BuySystemManager.instance == null)
        {
            gridScript.ClearPlacementPreview();
            ClearBuildingPreviewVisual();
            return;
        }

        BuySystemManager.BuildingType selectedType = (BuySystemManager.BuildingType)databaseIndex;
        if (!BuySystemManager.instance.TryGetBuildingData(selectedType, out GameObject prefab, out int width, out int depth))
        {
            gridScript.ClearPlacementPreview();
            ClearBuildingPreviewVisual();
            return;
        }

        GetFootprintOriginFromCenter(gridX, gridY, width, depth, out int internalGridX, out int internalGridY);

        bool requiresFullRoadSide = selectedType != BuySystemManager.BuildingType.Road;
        gridScript.SetPlacementPreview(internalGridX, internalGridY, width, depth, requiresFullRoadSide);

        bool canPlace = CanPlaceSelectedBuildingAt(gridScript, selectedType, internalGridX, internalGridY, width, depth);
        ShowOrUpdateBuildingPreview(gridScript, selectedType, prefab, internalGridX, internalGridY, width, depth, canPlace);
    }

    private bool TryConstructBuilding() {
        if (gridX < 1 || gridY < 1) {
            return false;
        }

        if (databaseIndex <= 0 || !System.Enum.IsDefined(typeof(BuySystemManager.BuildingType), databaseIndex)) {
            return false;
        }

        if (BuySystemManager.instance == null) {
            Debug.LogWarning("BuySystemManager instance is missing.");
            return false;
        }

        GridScript gridScript = FindObjectOfType<GridScript>();
        if (gridScript == null) {
            Debug.LogWarning("GridScript instance is missing.");
            return false;
        }

        BuySystemManager.BuildingType selectedType = (BuySystemManager.BuildingType)databaseIndex;
        if (!BuySystemManager.instance.TryGetBuildingPlacementData(selectedType, out GameObject prefab, out int width, out int depth, out Vector3 buildSpawnOffset)) {
            Debug.LogWarning("No prefab configured for selected building type: " + selectedType);
            return false;
        }

        GetFootprintOriginFromCenter(gridX, gridY, width, depth, out int internalGridX, out int internalGridY);

        if (!BuySystemManager.instance.TryGetBuildingCost(selectedType, out int buildingCost))
        {
            Debug.LogWarning("No building cost configured for selected building type: " + selectedType);
            return false;
        }

        if (ResourceManager.instance == null)
        {
            Debug.LogWarning("ResourceManager instance is missing.");
            return false;
        }

        if (!ResourceManager.instance.CanAfford(buildingCost))
        {
            Debug.Log($"Cannot build {selectedType}. Required: {buildingCost}, Current money: {ResourceManager.instance.Money}");
            return false;
        }

        if (!gridScript.CanPlaceBuilding(internalGridX, internalGridY, width, depth)) {
            Debug.Log("Cannot build here. The area is occupied or out of bounds.");
            return false;
        }

        if (selectedType != BuySystemManager.BuildingType.Road &&
            !gridScript.HasAtLeastOneFullRoadSide(internalGridX, internalGridY, width, depth)) {
            Debug.Log("Cannot build here. Non-road buildings need at least one full side touching roads.");
            return false;
        }

        if (selectedType == BuySystemManager.BuildingType.Road &&
            !CanPlaceRoadWithConnection(gridScript, internalGridX, internalGridY, width, depth))
        {
            Debug.Log("Cannot build here. Roads must connect to an existing road (except the first road).");
            return false;
        }

        if (!ResourceManager.instance.SpendMoney(buildingCost))
        {
            Debug.Log($"Cannot build {selectedType}. Not enough money.");
            return false;
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

        GameObject spawnedBuilding = Instantiate(prefab, spawnPosition + buildSpawnOffset, spawnRotation);

        BuildingInstance buildingInstance = spawnedBuilding.GetComponent<BuildingInstance>();
        if (buildingInstance == null)
        {
            buildingInstance = spawnedBuilding.AddComponent<BuildingInstance>();
        }

        buildingInstance.Initialize(selectedType, new Vector2Int(internalGridX, internalGridY), width, depth);

        PlayConstructionSmoke(spawnPosition);

        if (selectedType == BuySystemManager.BuildingType.Road)
        {
            RefreshAllRoadRotations(gridScript);
        }

        LastConstructedGridX = internalGridX + 1;
        LastConstructedGridY = internalGridY + 1;
        LastConstructedWidth = width;
        LastConstructedDepth = depth;
        LastConstructedBuildingType = selectedType;
   
        ResetPendingGridPosition();

        if (gridScript != null)
        {
            gridScript.ClearPlacementPreview();
        }

        ClearBuildingPreviewVisual();

        return true;
    }

    private void PlayConstructionSmoke(Vector3 basePosition)
    {
        if (constructionSmokePrefab == null)
        {
            return;
        }

        Vector3 smokePosition = basePosition + constructionSmokeOffset;
        GameObject smokeInstance = Instantiate(constructionSmokePrefab, smokePosition, Quaternion.identity);

        ParticleSystem[] particleSystems = smokeInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play();
        }

        float lifetime = Mathf.Max(0.1f, constructionSmokeDuration);
        Destroy(smokeInstance, lifetime);
    }

    private void ResetPendingGridPosition()
    {
        if (followMouseCursor)
        {
            return;
        }

        gridX = -1;
        gridY = -1;
    }

    private bool TryGetGridPositionFromMouse(out int oneBasedGridX, out int oneBasedGridY)
    {
        oneBasedGridX = -1;
        oneBasedGridY = -1;

        if (previewCamera == null)
        {
            previewCamera = Camera.main;
            if (previewCamera == null)
            {
                return false;
            }
        }

        GridScript gridScript = FindObjectOfType<GridScript>();
        if (gridScript == null)
        {
            return false;
        }

        Ray ray = previewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, previewRaycastDistance, previewSurfaceMask))
        {
            return false;
        }

        Vector2Int internalCell = gridScript.WorldToCell(hit.point);
        if (!gridScript.IsWithinGrid(internalCell))
        {
            return false;
        }

        oneBasedGridX = internalCell.x + 1;
        oneBasedGridY = internalCell.y + 1;
        return true;
    }

    private void SetPreviewSizeText(string text)
    {
        if (previewSizeText == null)
        {
            return;
        }

        previewSizeText.text = text;
        previewSizeText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    private bool CanPlaceSelectedBuildingAt(
        GridScript gridScript,
        BuySystemManager.BuildingType selectedType,
        int internalGridX,
        int internalGridY,
        int width,
        int depth)
    {
        if (ResourceManager.instance == null)
        {
            return false;
        }

        if (!BuySystemManager.instance.TryGetBuildingCost(selectedType, out int buildingCost))
        {
            return false;
        }

        if (!ResourceManager.instance.CanAfford(buildingCost))
        {
            return false;
        }

        if (!gridScript.CanPlaceBuilding(internalGridX, internalGridY, width, depth))
        {
            return false;
        }

        if (selectedType != BuySystemManager.BuildingType.Road &&
            !gridScript.HasAtLeastOneFullRoadSide(internalGridX, internalGridY, width, depth))
        {
            return false;
        }

        if (selectedType == BuySystemManager.BuildingType.Road &&
            !CanPlaceRoadWithConnection(gridScript, internalGridX, internalGridY, width, depth))
        {
            return false;
        }

        return true;
    }

    private void ShowOrUpdateBuildingPreview(
        GridScript gridScript,
        BuySystemManager.BuildingType selectedType,
        GameObject prefab,
        int internalGridX,
        int internalGridY,
        int width,
        int depth,
        bool canPlace)
    {
        EnsurePreviewInstance(prefab, selectedType);
        if (previewInstance == null)
        {
            return;
        }

        Vector3 previewPosition = new Vector3(
            gridScript.startCorner.x + (internalGridX + (width * 0.5f)) * gridScript.cellSize,
            0f,
            gridScript.startCorner.y + (internalGridY + (depth * 0.5f)) * gridScript.cellSize
        );

        Quaternion previewRotation = Quaternion.identity;
        if (selectedType == BuySystemManager.BuildingType.Road)
        {
            previewRotation = GetRoadRotation(gridScript, internalGridX, internalGridY);
        }

        previewInstance.transform.SetPositionAndRotation(previewPosition, previewRotation);
        SetPreviewColor(canPlace ? previewValidColor : previewInvalidColor);
    }

    private void EnsurePreviewInstance(GameObject prefab, BuySystemManager.BuildingType selectedType)
    {
        if (prefab == null)
        {
            ClearBuildingPreviewVisual();
            return;
        }

        if (previewInstance != null && previewType == selectedType)
        {
            return;
        }

        ClearBuildingPreviewVisual();

        previewInstance = Instantiate(prefab);
        previewInstance.name = prefab.name + "_Preview";
        previewType = selectedType;
        previewRenderers = previewInstance.GetComponentsInChildren<Renderer>(true);

        Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void SetPreviewColor(Color color)
    {
        if (previewRenderers == null)
        {
            return;
        }

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            Renderer rendererComponent = previewRenderers[i];
            if (rendererComponent == null)
            {
                continue;
            }

            rendererComponent.GetPropertyBlock(previewPropertyBlock);
            previewPropertyBlock.SetColor("_Color", color);
            previewPropertyBlock.SetColor("_BaseColor", color);
            rendererComponent.SetPropertyBlock(previewPropertyBlock);
        }
    }

    private void ClearBuildingPreviewVisual()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }

        previewInstance = null;
        previewRenderers = null;
        previewType = BuySystemManager.BuildingType.None;
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

        // Keep single/isolated road tile horizontal as the default start orientation.
        if (!connectedOnX && !connectedOnZ)
        {
            return Quaternion.Euler(-90f, 0f, 90f);
        }

        if (straightZ && !straightX)
        {
            return Quaternion.Euler(-90f, 0f, 0f);
        }

        if (straightX && !straightZ)
        {
            return Quaternion.Euler(-90f, 0f, 90f);
        }

        if (connectedOnZ && !connectedOnX)
        {
            return Quaternion.Euler(-90f, 0f, 0f);
        }

        // Junction/cross keeps base horizontal orientation to avoid flip jitter.
        return Quaternion.Euler(-90f, 0f, 90f);
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

    private void OnDisable()
    {
        ClearBuildingPreviewVisual();
        SetPreviewSizeText(string.Empty);
    }

    private void OnDestroy()
    {
        ClearBuildingPreviewVisual();
    }
}