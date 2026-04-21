using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaterWayPuzzle : MonoBehaviour
{
    [System.Serializable]
    public class GridNode
    {
        public bool isPath;
        public bool isStart;
        public bool isEnd;
        public Image uiImage;
        public Image clickImage;
        public Color originalColor;
        public Color clickOriginalColor;
    }

    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private int difficulty = 3; // 1-3, affects path complexity

    private GridNode[,] grid;
    private List<Vector2Int> correctPath;
    private List<Vector2Int> playerPath;
    private Vector2Int startPos;
    private Vector2Int endPos;

    private Color pathColor = Color.blue;
    private Color startColor = new Color(0, 1, 0, 1); // Green
    private Color endColor = new Color(1, 0, 0, 1); // Red
    private Color playerTraceColor = Color.blue;
    private Color correctTraceColor = new Color(0.2f, 0.8f, 0.2f, 1); // Light green

    private bool isActive = false;
    private bool isPuzzleComplete = false;

    private void OnEnable()
    {
        EnsureGridLayout();
        GeneratePuzzle();
        isActive = true;
        isPuzzleComplete = false;
    }

    private void OnDisable()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive || isPuzzleComplete)
            return;

        HandlePlayerInput();
    }

    public void SetDifficulty(int diff)
    {
        difficulty = Mathf.Clamp(diff, 1, 3);
        if (isActiveAndEnabled)
        {
            EnsureGridLayout();
            GeneratePuzzle();
        }
    }

    private void EnsureGridLayout()
    {
        if (gridLayout == null)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
        }

        if (gridLayout == null)
        {
            gridLayout = gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridWidth;

        RectTransform rectTransform = gridLayout.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float cellWidth = rectTransform.rect.width / gridWidth;
            float cellHeight = rectTransform.rect.height / gridHeight;
            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        }

        gridLayout.spacing = Vector2.zero;
    }

    private void GeneratePuzzle()
    {
        if (grid != null)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        grid = new GridNode[gridWidth, gridHeight];
        correctPath = new List<Vector2Int>();
        playerPath = new List<Vector2Int>();

    
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = new GridNode();
            }
        }

        startPos = new Vector2Int(Random.Range(0, gridWidth), 0);
        endPos = new Vector2Int(Random.Range(0, gridWidth), gridHeight - 1);


        GenerateRandomPath();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject nodeObj = Instantiate(nodePrefab, transform);
                Image nodeImage = nodeObj.GetComponent<Image>();

                if (nodeImage == null)
                    nodeImage = nodeObj.AddComponent<Image>();

                nodeImage.raycastTarget = true;

                grid[x, y].uiImage = nodeImage;
                grid[x, y].originalColor = Color.white;
                grid[x, y].clickImage = GetChildImage(nodeObj, nodeImage);
                if (grid[x, y].clickImage == null)
                {
                    grid[x, y].clickImage = nodeImage;
                }

                grid[x, y].clickOriginalColor = grid[x, y].clickImage.color;

                if (new Vector2Int(x, y) == startPos)
                {
                    grid[x, y].isStart = true;
                    nodeImage.color = startColor;
                }
                else if (new Vector2Int(x, y) == endPos)
                {
                    grid[x, y].isEnd = true;
                    nodeImage.color = endColor;
                }
                else if (grid[x, y].isPath)
                {
                    nodeImage.color = Color.white;
                }
                else
                {
                    nodeImage.color = new Color(0.7f, 0.7f, 0.7f, 1);
                }

                Button btn = nodeObj.GetComponent<Button>();
                if (btn == null)
                    btn = nodeObj.AddComponent<Button>();

                btn.targetGraphic = nodeImage;
                btn.interactable = true;
                btn.transition = Selectable.Transition.None;

                RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
                if (nodeRect != null)
                {
                    nodeRect.localScale = Vector3.one;
                }

                Vector2Int pos = new Vector2Int(x, y);
                btn.onClick.AddListener(() => OnNodeClicked(pos));
            }
        }
    }

    private Image GetChildImage(GameObject nodeObj, Image rootImage)
    {
        Image[] images = nodeObj.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image != null && image != rootImage)
            {
                return image;
            }
        }

        return null;
    }

    private void GenerateRandomPath()
    {
        Vector2Int currentPos = startPos;
        correctPath.Add(currentPos);
        grid[currentPos.x, currentPos.y].isPath = true;

        int safetyCounter = gridWidth * gridHeight * 6;
        int targetPassableCount = Mathf.CeilToInt((gridWidth * gridHeight) * 0.6f);

        while (currentPos != endPos && safetyCounter > 0)
        {
            safetyCounter--;

            List<Vector2Int> nextSteps = new List<Vector2Int>();

            if (currentPos.x < endPos.x)
                nextSteps.Add(new Vector2Int(currentPos.x + 1, currentPos.y));
            if (currentPos.x > endPos.x)
                nextSteps.Add(new Vector2Int(currentPos.x - 1, currentPos.y));
            if (currentPos.y < endPos.y)
                nextSteps.Add(new Vector2Int(currentPos.x, currentPos.y + 1));
            if (currentPos.y > endPos.y)
                nextSteps.Add(new Vector2Int(currentPos.x, currentPos.y - 1));

            List<Vector2Int> validSteps = new List<Vector2Int>();
            foreach (Vector2Int step in nextSteps)
            {
                if (IsWithinBounds(step) && !correctPath.Contains(step))
                {
                    validSteps.Add(step);
                }
            }

            if (validSteps.Count == 0)
                break;

            Vector2Int chosenStep;
            if (Random.value < 0.7f)
            {
                List<Vector2Int> preferredSteps = new List<Vector2Int>();
                foreach (Vector2Int step in validSteps)
                {
                    if (step.y > currentPos.y)
                        preferredSteps.Add(step);
                }

                chosenStep = preferredSteps.Count > 0
                    ? preferredSteps[Random.Range(0, preferredSteps.Count)]
                    : validSteps[Random.Range(0, validSteps.Count)];
            }
            else
            {
                chosenStep = validSteps[Random.Range(0, validSteps.Count)];
            }

            currentPos = chosenStep;
            correctPath.Add(currentPos);
            grid[currentPos.x, currentPos.y].isPath = true;
        }

        if (currentPos != endPos)
        {
            currentPos = endPos;
            if (!correctPath.Contains(currentPos))
            {
                correctPath.Add(currentPos);
                grid[currentPos.x, currentPos.y].isPath = true;
            }
        }

        AddExtraPathBranches(targetPassableCount);

        Debug.Log($"Water Path Generated: {correctPath.Count} nodes - Difficulty {difficulty}");
    }

    private void AddExtraPathBranches(int targetPassableCount)
    {
        int currentPassableCount = CountPassableNodes();
        int branchBudget = gridWidth + gridHeight + (difficulty * 4);
        int attempts = 0;

        while (currentPassableCount < targetPassableCount && branchBudget > 0 && attempts < 100)
        {
            attempts++;
            branchBudget--;

            Vector2Int branchStart = correctPath[Random.Range(0, correctPath.Count - 1)];
            Vector2Int branchPos = branchStart;
            int branchLength = Random.Range(2, 5 + difficulty);

            for (int i = 0; i < branchLength && currentPassableCount < targetPassableCount; i++)
            {
                List<Vector2Int> branchOptions = GetBranchOptions(branchPos);
                if (branchOptions.Count == 0)
                    break;

                branchPos = branchOptions[Random.Range(0, branchOptions.Count)];
                if (!grid[branchPos.x, branchPos.y].isPath)
                {
                    grid[branchPos.x, branchPos.y].isPath = true;
                    currentPassableCount++;
                }
            }
        }
    }

    private int CountPassableNodes()
    {
        int count = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y].isPath || new Vector2Int(x, y) == startPos || new Vector2Int(x, y) == endPos)
                    count++;
            }
        }

        return count;
    }

    private List<Vector2Int> GetBranchOptions(Vector2Int pos)
    {
        List<Vector2Int> options = new List<Vector2Int>();
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int candidate = pos + direction;
            if (!IsWithinBounds(candidate))
                continue;

            if (candidate == startPos || candidate == endPos)
                continue;

            if (!grid[candidate.x, candidate.y].isPath)
                options.Add(candidate);
        }

        return options;
    }

    private List<Vector2Int> GetValidPathNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (IsWithinBounds(newPos) && IsValidNewPathNode(newPos))
            {
                neighbors.Add(newPos);
            }
        }

        return neighbors;
    }

    private bool IsValidNewPathNode(Vector2Int pos)
    {
        if (!IsWithinBounds(pos))
            return false;

        if (pos.y < startPos.y)
            return false;

        return true;
    }

    private bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    private void OnNodeClicked(Vector2Int pos)
    {
        if (!isActive || isPuzzleComplete)
            return;

        if (!grid[pos.x, pos.y].isPath && pos != startPos)
            return;

        if (pos == startPos)
        {
            playerPath.Clear();
            playerPath.Add(startPos);
            UpdatePlayerPathVisuals();
            return;
        }

        if (playerPath.Count == 0)
            return;

        Vector2Int lastPos = playerPath[playerPath.Count - 1];
        int distance = Mathf.Abs(pos.x - lastPos.x) + Mathf.Abs(pos.y - lastPos.y);

        if (distance != 1) 
            return;

        if (playerPath.Contains(pos))
        {
            while (playerPath.Count > 0 && playerPath[playerPath.Count - 1] != pos)
            {
                playerPath.RemoveAt(playerPath.Count - 1);
            }

            UpdatePlayerPathVisuals();
            return;
        }

        if (!IsConnectedToPlayerPath(pos))
            return;

        playerPath.Add(pos);

        UpdatePlayerPathVisuals();

        if (pos == endPos)
        {
            CompletePuzzle();
        }
    }

    private bool IsConnectedToPlayerPath(Vector2Int pos)
    {
        for (int i = 0; i < playerPath.Count; i++)
        {
            Vector2Int pathPos = playerPath[i];
            int distance = Mathf.Abs(pos.x - pathPos.x) + Mathf.Abs(pos.y - pathPos.y);
            if (distance == 1)
                return true;
        }

        return false;
    }

    private void UpdatePlayerPathVisuals()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int nodePos = new Vector2Int(x, y);
                if (nodePos == startPos)
                    grid[x, y].uiImage.color = startColor;
                else if (nodePos == endPos)
                    grid[x, y].uiImage.color = endColor;
                else if (grid[x, y].isPath)
                    grid[x, y].uiImage.color = Color.white;
                else
                    grid[x, y].uiImage.color = new Color(0.7f, 0.7f, 0.7f, 1);

                if (grid[x, y].clickImage != null)
                {
                    grid[x, y].clickImage.color = grid[x, y].clickOriginalColor;
                }
            }
        }


        for (int i = 0; i < playerPath.Count; i++)
        {
            Vector2Int pos = playerPath[i];
            if (pos != startPos && pos != endPos)
            {
                if (grid[pos.x, pos.y].clickImage != null)
                {
                    grid[pos.x, pos.y].clickImage.color = playerTraceColor;
                }
            }
        }
    }

    private void HandlePlayerInput()
    {
        if (playerPath.Count == 0)
        {
    
            playerPath.Add(startPos);
        }
    }

    private void CompletePuzzle()
    {
        isPuzzleComplete = true;

        foreach (Vector2Int pos in playerPath)
        {
            if (pos != startPos && pos != endPos)
            {
                if (grid[pos.x, pos.y].clickImage != null)
                {
                    grid[pos.x, pos.y].clickImage.color = correctTraceColor;
                }
            }
        }

        Debug.Log("Water Path Puzzle Complete!");
        StartCoroutine(WaitAndEndMinigame());
    }

    private IEnumerator WaitAndEndMinigame()
    {
        yield return new WaitForSeconds(2f);
        MinigameManager.instance.EndMinigame();
    }
}
