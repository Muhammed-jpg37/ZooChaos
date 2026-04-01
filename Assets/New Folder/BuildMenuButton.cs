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
