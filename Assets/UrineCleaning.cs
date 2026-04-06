using UnityEngine;
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

    [Header("Spawn Settings")]
    [SerializeField] private int minDirtyAreas = 2;
    [SerializeField] private int maxDirtyAreas = 6;
    [SerializeField] private Vector2 minDirtySize = new Vector2(50f, 35f);
    [SerializeField] private Vector2 maxDirtySize = new Vector2(170f, 120f);
    [SerializeField] private float minSpacingBetweenPatches = 100f;
    [SerializeField] private int maxSpawnAttempts = 20;

    [Header("Cleaning")]
    [SerializeField] private float brushRadius = 70f;
    [SerializeField] private float cleanSpeed = 1.75f;
    [SerializeField] private Color dirtColor = new Color(0.35f, 0.23f, 0.12f, 0.9f);
    [SerializeField] private Color cleanedColor = new Color(0.35f, 0.23f, 0.12f, 0f);

    [Header("Completion")]
    [SerializeField] private UnityEvent onCompleted;

    private readonly System.Collections.Generic.List<DirtPatch> dirtPatches = new System.Collections.Generic.List<DirtPatch>();
    private bool isRunning;
    private Vector2 previousMousePosition;
    [SerializeField] private float minMouseMovement = 1f;

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
        ClearExistingDirt();
        SpawnRandomDirt();
        isRunning = true;
    }

    private void UpdateCleaning()
    {
        if (!TryGetCursorLocalPoint(out Vector2 cursorLocalPoint))
        {
            return;
        }

        float mouseMovement = Vector2.Distance(Input.mousePosition, previousMousePosition);
        bool isMouseMoving = mouseMovement > minMouseMovement;
        previousMousePosition = Input.mousePosition;

        if (!isMouseMoving)
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

        if (dirtPatches.Count == 0)
        {
            FinishGame();
        }
    }

    private void SpawnRandomDirt()
    {
        int dirtCount = Random.Range(minDirtyAreas, maxDirtyAreas + 1);

        for (int i = 0; i < dirtCount; i++)
        {
            GameObject dirtObject = CreateDirtObject();
            RectTransform dirtRect = dirtObject.GetComponent<RectTransform>();
            Image dirtImage = dirtObject.GetComponent<Image>();

            Vector2 size = new Vector2(
                Random.Range(minDirtySize.x, maxDirtySize.x),
                Random.Range(minDirtySize.y, maxDirtySize.y)
            );

            if (!TrySetRandomDirtyPosition(dirtRect, size))
            {
                Destroy(dirtObject);
                continue;
            }

            dirtImage.color = dirtColor;

            dirtPatches.Add(new DirtPatch
            {
                rectTransform = dirtRect,
                image = dirtImage,
                cleanProgress = 0f,
                totalRequired = 1f
            });
        }
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

    private bool TrySetRandomDirtyPosition(RectTransform dirtRect, Vector2 size)
    {
        Rect playRect = playArea.rect;
        float halfWidthLimit = Mathf.Max(0f, playRect.width * 0.5f - size.x * 0.5f);
        float halfHeightLimit = Mathf.Max(0f, playRect.height * 0.5f - size.y * 0.5f);

        dirtRect.sizeDelta = size;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 candidatePosition = new Vector2(
                Random.Range(-halfWidthLimit, halfWidthLimit),
                Random.Range(-halfHeightLimit, halfHeightLimit)
            );

            if (IsPositionValid(candidatePosition, size))
            {
                dirtRect.anchoredPosition = candidatePosition;
                return true;
            }
        }

        return false;
    }

    private bool IsPositionValid(Vector2 candidatePosition, Vector2 size)
    {
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        float minDist = minSpacingBetweenPatches;

        foreach (DirtPatch existingPatch in dirtPatches)
        {
            if (existingPatch?.rectTransform == null)
            {
                continue;
            }

            Vector2 existingPos = existingPatch.rectTransform.anchoredPosition;
            float existingHalfWidth = existingPatch.rectTransform.rect.width * 0.5f;
            float existingHalfHeight = existingPatch.rectTransform.rect.height * 0.5f;

            float minX = candidatePosition.x - halfWidth;
            float maxX = candidatePosition.x + halfWidth;
            float minY = candidatePosition.y - halfHeight;
            float maxY = candidatePosition.y + halfHeight;

            float existingMinX = existingPos.x - existingHalfWidth - minDist;
            float existingMaxX = existingPos.x + existingHalfWidth + minDist;
            float existingMinY = existingPos.y - existingHalfHeight - minDist;
            float existingMaxY = existingPos.y + existingHalfHeight + minDist;

            if (maxX >= existingMinX && minX <= existingMaxX &&
                maxY >= existingMinY && minY <= existingMaxY)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePatchVisual(DirtPatch patch)
    {
        float t = Mathf.Clamp01(patch.cleanProgress / patch.totalRequired);
        Color currentColor = Color.Lerp(dirtColor, cleanedColor, t);
        patch.image.color = currentColor;

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

    private void ClearExistingDirt()
    {
        for (int i = playArea.childCount - 1; i >= 0; i--)
        {
            Transform child = playArea.GetChild(i);
            if (dirtPrefab != null && child.gameObject == dirtPrefab)
            {
                continue;
            }

            Destroy(child.gameObject);
        {
        
    }
}
    }}