using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class UrineCleaning : MonoBehaviour
{
    [System.Serializable]
    private class DirtPatch
    {
        public RectTransform rectTransform;
        public Image image;
        public float cleanProgress;
        public float totalRequired;
    }

    [Header("UI References")]
    [SerializeField] private RectTransform playArea;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private GameObject dirtPrefab;

    [Header("Hidden Obstacle Settings")]
    [SerializeField] private List<GameObject> obstaclePrefabs = new List<GameObject>();
    [SerializeField] private int obstacleCount = 3;
    [SerializeField, Range(0f, 1f)] private float urineUnderObstacleChance = 0.6f;
    [SerializeField] private Vector2 minObstacleSize = new Vector2(70f, 70f);
    [SerializeField] private Vector2 maxObstacleSize = new Vector2(130f, 130f);
    [SerializeField] private Color fallbackObstacleColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Spawn Settings")]
    [SerializeField] private Vector2 minDirtySize = new Vector2(50f, 35f);
    [SerializeField] private Vector2 maxDirtySize = new Vector2(170f, 120f);
    [SerializeField] private float minSpacingBetweenPatches = 100f;
    [SerializeField] private int maxSpawnAttempts = 25;

    [Header("Cleaning")]
    [SerializeField] private float brushRadius = 70f;
    [SerializeField] private float cleanSpeed = 1.75f;
    [SerializeField] private float minMouseMovement = 1f;
 
    [SerializeField] private Color cleanedColor = new Color(0.35f, 0.23f, 0.12f, 0f);

    [Header("Completion")]
    [SerializeField] private UnityEvent onCompleted;

    private readonly List<DirtPatch> dirtPatches = new List<DirtPatch>();
    private readonly List<UrineMinigameObstacle> activeObstacles = new List<UrineMinigameObstacle>();
    private bool isRunning;
    private Vector2 previousMousePosition;

    private void OnEnable()
    {
        StartGame();
    }

    private void OnDisable()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (!isRunning || playArea == null)
        {
            return;
        }

        UpdateCleaning();
    }

    public void StartGame()
    {
        if (playArea == null)
        {
            Debug.LogWarning("UrineCleaning is missing the playArea reference.");
            isRunning = false;
            return;
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        previousMousePosition = Input.mousePosition;
        ClearExistingMinigameObjects();
        SpawnHiddenUrineObstacles();
        isRunning = true;
    }

    public void HandleObstacleDraggedOutside(UrineMinigameObstacle obstacle)
    {
        if (obstacle == null)
        {
            return;
        }

        Vector2 spawnPosition = obstacle.StartAnchoredPosition;
        Vector2 spawnSize = obstacle.Size;

        activeObstacles.Remove(obstacle);
        Destroy(obstacle.gameObject);

        if (obstacle.HasHiddenUrine)
        {
            SpawnUrinePatch(spawnPosition, spawnSize);
        }

        TryFinishGame();
    }

    private void UpdateCleaning()
    {
        if (!TryGetCursorLocalPoint(out Vector2 cursorLocalPoint))
        {
            return;
        }

        bool isHoldingMouse = Input.GetMouseButton(0);
        float mouseMovement = Vector2.Distance(Input.mousePosition, previousMousePosition);
        bool isMouseMoving = mouseMovement > minMouseMovement;
        previousMousePosition = Input.mousePosition;

        if (!isHoldingMouse || !isMouseMoving)
        {
            return;
        }

        float dt = Time.deltaTime;

        for (int i = dirtPatches.Count - 1; i >= 0; i--)
        {
            DirtPatch patch = dirtPatches[i];
            if (patch == null || patch.rectTransform == null)
            {
                dirtPatches.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(cursorLocalPoint, patch.rectTransform.anchoredPosition);
            float halfWidth = patch.rectTransform.rect.width * 0.5f;
            float halfHeight = patch.rectTransform.rect.height * 0.5f;
            float effectiveRange = brushRadius + Mathf.Max(halfWidth, halfHeight);

            if (distance <= effectiveRange)
            {
                float proximity = 1f - Mathf.Clamp01(distance / effectiveRange);
                patch.cleanProgress += cleanSpeed * proximity * dt;
                UpdatePatchVisual(patch);
            }

            if (patch.cleanProgress >= patch.totalRequired)
            {
                Destroy(patch.rectTransform.gameObject);
                dirtPatches.RemoveAt(i);
            }
        }

        TryFinishGame();
    }

    private void SpawnHiddenUrineObstacles()
    {
        int clampedCount = Mathf.Max(0, obstacleCount);

        for (int i = 0; i < clampedCount; i++)
        {
            GameObject obstacleObject = CreateObstacleObject();
            RectTransform obstacleRect = obstacleObject.GetComponent<RectTransform>();
            Vector2 obstacleSize = GetObstacleSize(obstacleRect);

            if (!TrySetRandomPosition(obstacleSize, out Vector2 obstaclePosition))
            {
                Destroy(obstacleObject);
                continue;
            }

            obstacleRect.sizeDelta = obstacleSize;
            obstacleRect.anchoredPosition = obstaclePosition;

            UrineMinigameObstacle obstacle = obstacleObject.GetComponent<UrineMinigameObstacle>();
            if (obstacle == null)
            {
                obstacle = obstacleObject.AddComponent<UrineMinigameObstacle>();
            }

            bool hasHiddenUrine = Random.value <= Mathf.Clamp01(urineUnderObstacleChance);
            obstacle.Initialize(this, playArea, rootCanvas, obstaclePosition, obstacleSize, hasHiddenUrine);
            activeObstacles.Add(obstacle);
        }
    }

    private GameObject CreateObstacleObject()
    {
        GameObject obstacleObject;

        if (obstaclePrefabs != null && obstaclePrefabs.Count > 0)
        {
            int randomIndex = Random.Range(0, obstaclePrefabs.Count);
            obstacleObject = Instantiate(obstaclePrefabs[randomIndex], playArea);
        }
        else
        {
            obstacleObject = new GameObject("ObstacleItem", typeof(RectTransform), typeof(Image));
            obstacleObject.transform.SetParent(playArea, false);

            Image fallbackImage = obstacleObject.GetComponent<Image>();
            fallbackImage.color = fallbackObstacleColor;
            fallbackImage.raycastTarget = true;
        }

        RectTransform rectTransform = obstacleObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;

        Image image = obstacleObject.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        return obstacleObject;
    }

    private Vector2 GetObstacleSize(RectTransform obstacleRect)
    {
        if (obstacleRect != null)
        {
            Vector2 currentSize = obstacleRect.sizeDelta;
            if (currentSize.x > 1f && currentSize.y > 1f)
            {
                return currentSize;
            }
        }

        return new Vector2(
            Random.Range(minObstacleSize.x, maxObstacleSize.x),
            Random.Range(minObstacleSize.y, maxObstacleSize.y)
        );
    }

    private void SpawnUrinePatch(Vector2 anchoredPosition, Vector2 size)
    {
        GameObject dirtObject = CreateDirtObject();
        RectTransform dirtRect = dirtObject.GetComponent<RectTransform>();
        Image dirtImage = dirtObject.GetComponent<Image>();

        Vector2 clampedSize = new Vector2(
            Mathf.Clamp(size.x, minDirtySize.x, maxDirtySize.x),
            Mathf.Clamp(size.y, minDirtySize.y, maxDirtySize.y)
        );

        dirtRect.sizeDelta = clampedSize;
        dirtRect.anchoredPosition = anchoredPosition;
       

        dirtPatches.Add(new DirtPatch
        {
            rectTransform = dirtRect,
            image = dirtImage,
            cleanProgress = 0f,
            totalRequired = 1f
        });
    }

    private GameObject CreateDirtObject()
    {
        GameObject dirtObject;
        if (dirtPrefab != null)
        {
            dirtObject = Instantiate(dirtPrefab, playArea);
        }
        else
        {
            dirtObject = new GameObject("DirtPatch", typeof(RectTransform), typeof(Image));
            dirtObject.transform.SetParent(playArea, false);
        }

        RectTransform rectTransform = dirtObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;

        Image image = dirtObject.GetComponent<Image>();
        if (image == null)
        {
            image = dirtObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        return dirtObject;
    }

    private bool TrySetRandomPosition(Vector2 size, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        Rect playRect = playArea.rect;
        float halfWidthLimit = Mathf.Max(0f, playRect.width * 0.5f - size.x * 0.5f);
        float halfHeightLimit = Mathf.Max(0f, playRect.height * 0.5f - size.y * 0.5f);

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(-halfWidthLimit, halfWidthLimit),
                Random.Range(-halfHeightLimit, halfHeightLimit)
            );

            if (IsPositionValid(candidate, size))
            {
                anchoredPosition = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsPositionValid(Vector2 candidatePosition, Vector2 size)
    {
        for (int i = 0; i < activeObstacles.Count; i++)
        {
            UrineMinigameObstacle obstacle = activeObstacles[i];
            if (obstacle == null)
            {
                continue;
            }

            if (RectsOverlap(candidatePosition, size, obstacle.StartAnchoredPosition, obstacle.Size, minSpacingBetweenPatches))
            {
                return false;
            }
        }

        for (int i = 0; i < dirtPatches.Count; i++)
        {
            DirtPatch patch = dirtPatches[i];
            if (patch == null || patch.rectTransform == null)
            {
                continue;
            }

            Vector2 patchSize = patch.rectTransform.rect.size;
            if (RectsOverlap(candidatePosition, size, patch.rectTransform.anchoredPosition, patchSize, minSpacingBetweenPatches))
            {
                return false;
            }
        }

        return true;
    }

    private bool RectsOverlap(Vector2 posA, Vector2 sizeA, Vector2 posB, Vector2 sizeB, float padding)
    {
        float aHalfW = sizeA.x * 0.5f;
        float aHalfH = sizeA.y * 0.5f;
        float bHalfW = sizeB.x * 0.5f;
        float bHalfH = sizeB.y * 0.5f;

        float aMinX = posA.x - aHalfW;
        float aMaxX = posA.x + aHalfW;
        float aMinY = posA.y - aHalfH;
        float aMaxY = posA.y + aHalfH;

        float bMinX = posB.x - bHalfW - padding;
        float bMaxX = posB.x + bHalfW + padding;
        float bMinY = posB.y - bHalfH - padding;
        float bMaxY = posB.y + bHalfH + padding;

        return aMaxX >= bMinX && aMinX <= bMaxX && aMaxY >= bMinY && aMinY <= bMaxY;
    }

    private void UpdatePatchVisual(DirtPatch patch)
    {
        float t = Mathf.Clamp01(patch.cleanProgress / patch.totalRequired);
        
    

        float scale = Mathf.Lerp(1f, 0.7f, t);
        patch.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private bool TryGetCursorLocalPoint(out Vector2 localPoint)
    {
        Camera eventCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = rootCanvas.worldCamera;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea,
            Input.mousePosition,
            eventCamera,
            out localPoint
        );
    }

    private void TryFinishGame()
    {
        if (!isRunning)
        {
            return;
        }

        if (activeObstacles.Count > 0)
        {
            return;
        }

        if (dirtPatches.Count > 0)
        {
            return;
        }

        FinishGame();
    }

    private void FinishGame()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        onCompleted?.Invoke();

        if (MinigameManager.instance != null)
        {
            MinigameManager.instance.EndMinigame();
        }
    }

    private void ClearExistingMinigameObjects()
    {
        dirtPatches.Clear();
        activeObstacles.Clear();

        if (playArea == null)
        {
            return;
        }

        for (int i = playArea.childCount - 1; i >= 0; i--)
        {
            Destroy(playArea.GetChild(i).gameObject);
        }
    }
}

public class UrineMinigameObstacle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Vector2 StartAnchoredPosition { get; private set; }
    public Vector2 Size { get; private set; }
    public bool HasHiddenUrine { get; private set; }

    private UrineCleaning owner;
    private RectTransform playArea;
    private Canvas rootCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool isInitialized;

    public void Initialize(UrineCleaning ownerRef, RectTransform playAreaRef, Canvas canvasRef, Vector2 startPosition, Vector2 size, bool hasHiddenUrine)
    {
        owner = ownerRef;
        playArea = playAreaRef;
        rootCanvas = canvasRef;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        StartAnchoredPosition = startPosition;
        Size = size;
        HasHiddenUrine = hasHiddenUrine;
        isInitialized = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isInitialized)
        {
            return;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.9f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isInitialized || rectTransform == null || playArea == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, eventData.position, GetEventCamera(), out Vector2 localPoint))
        {
            return;
        }

        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isInitialized)
        {
            return;
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        bool draggedOutsidePlayArea = !RectTransformUtility.RectangleContainsScreenPoint(playArea, eventData.position, GetEventCamera());
        if (draggedOutsidePlayArea)
        {
            owner.HandleObstacleDraggedOutside(this);
            return;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = StartAnchoredPosition;
        }
    }

    private Camera GetEventCamera()
    {
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return rootCanvas.worldCamera;
        }

        return null;
    }
}