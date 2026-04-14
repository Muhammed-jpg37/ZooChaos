using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuButton : MonoBehaviour
{
    [SerializeField] private int gridX;
    [SerializeField] private int gridY;

    private int pendingGridX;
    private int pendingGridY;
    private bool hasPendingX;
    private bool hasPendingY;
    private Button button;
    private RectTransform rectTransform;
    private bool lastInteractableState = true;
    private bool hasBeenConstructed;
    private static readonly List<BuildMenuButton> allButtons = new List<BuildMenuButton>();

    private void OnEnable()
    {
        if (!allButtons.Contains(this))
        {
            allButtons.Add(this);
        }
    }

    private void OnDisable()
    {
        allButtons.Remove(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void FixedUpdate()
    {
        if (button == null)
        {
            return;
        }

        bool shouldBeInteractable = IsPointerOverThisButton();
        if (shouldBeInteractable == lastInteractableState)
        {
            return;
        }

        button.interactable = shouldBeInteractable;
        lastInteractableState = shouldBeInteractable;
    }

    public void GetGridPositionX(int x) {
        pendingGridX = x;
        hasPendingX = true;
        TryWritePendingGridPosition();
    }

    public void GetGridPositionY(int y) {
        pendingGridY = y;
        hasPendingY = true;
        TryWritePendingGridPosition();
    }

    public void SubmitConfiguredGridPosition()
    {
        WriteGridPosition(gridX, gridY);
    }

    private void TryWritePendingGridPosition()
    {
        if (!hasPendingX || !hasPendingY)
        {
            return;
        }

        WriteGridPosition(pendingGridX, pendingGridY);
        hasPendingX = false;
        hasPendingY = false;
    }

    private void WriteGridPosition(int x, int y) {
        gridX = x;
        gridY = y;
        Debug.Log("Grid Position set to: (" + gridX + ", " + gridY + ")");

        if (BuildConstruction.instance == null) {
            Debug.LogWarning("BuildConstruction instance is missing.");
            return;
        }

        BuildConstruction.instance.GetGridPosition(gridX, gridY);

        if (BuildConstruction.instance.LastConstructionSucceeded)
        {
            SetConstructedButtonsForLastBuild(BuildConstruction.instance);
        }
    }

    private static void SetConstructedButtonsForLastBuild(BuildConstruction buildConstruction)
    {
        int startX = buildConstruction.LastConstructedGridX;
        int startY = buildConstruction.LastConstructedGridY;
        int width = buildConstruction.LastConstructedWidth;
        int depth = buildConstruction.LastConstructedDepth;
        Color targetColor = buildConstruction.LastConstructedBuildingType == BuySystemManager.BuildingType.Road
            ? Color.blue
            : Color.red;

        if (startX < 1 || startY < 1 || width < 1 || depth < 1)
        {
            return;
        }

        int endX = startX + width - 1;
        int endY = startY + depth - 1;

        for (int i = 0; i < allButtons.Count; i++)
        {
            BuildMenuButton buildButton = allButtons[i];
            if (buildButton == null)
            {
                continue;
            }

            bool insideFootprint =
                buildButton.gridX >= startX && buildButton.gridX <= endX &&
                buildButton.gridY >= startY && buildButton.gridY <= endY;

            if (insideFootprint)
            {
                buildButton.SetConstructedButtonColor(targetColor);
            }
        }
    }

    private void SetConstructedButtonColor(Color targetColor)
    {
        if (button == null || hasBeenConstructed)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = targetColor;
        colors.highlightedColor = targetColor;
        colors.pressedColor = targetColor;
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor;
        button.colors = colors;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = targetColor;
        }

        hasBeenConstructed = true;
    }

    private bool IsPointerOverThisButton()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        bool pointerOverAnyUI = EventSystem.current.IsPointerOverGameObject();
        if (!pointerOverAnyUI)
        {
            return false;
        }

        if (rectTransform == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null);
    }
 
}