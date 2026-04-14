using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuButton : MonoBehaviour
{
    private static readonly Dictionary<Vector2Int, BuildMenuButton> buttonLookup = new Dictionary<Vector2Int, BuildMenuButton>();
    [SerializeField] private bool debugRegistration;

    [SerializeField] private int gridX;
    [SerializeField] private int gridY;

    private int pendingGridX;
    private int pendingGridY;
    private bool hasPendingX;
    private bool hasPendingY;
    private Button button;
    private RectTransform rectTransform;
    private Image image;
    private Color defaultColor;
    private bool lastInteractableState = true;
    private bool isOccupied;
    private bool isRegistered;

    private void OnEnable()
    {
        InitializeComponents();
        RegisterButtonIfValid();
        ApplyColorState();
    }

    private void Start()
    {
        InitializeComponents();
        RegisterButtonIfValid();
        ApplyColorState();
    }

    private void OnDisable()
    {
        UnregisterCurrentKey();
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
        UnregisterCurrentKey();

        gridX = x;
        gridY = y;
        Debug.Log("Grid Position set to: (" + gridX + ", " + gridY + ")");

        RegisterButtonIfValid();
        ApplyColorState();

        if (BuildConstruction.instance == null) {
            Debug.LogWarning("BuildConstruction instance is missing.");
            return;
        }

        BuildConstruction.instance.GetGridPosition(gridX, gridY);
    }

    public void SetOccupiedState(bool occupied)
    {
        isOccupied = occupied;
        ApplyColorState();

        if (debugRegistration)
        {
            Debug.Log($"[BuildMenuButton] Occupied state changed at ({gridX}, {gridY}) -> {isOccupied}", this);
        }
    }

    public bool IsOccupied()
    {
        return isOccupied;
    }

    public static bool TryGetButton(int x, int y, out BuildMenuButton buildMenuButton)
    {
        return buttonLookup.TryGetValue(new Vector2Int(x, y), out buildMenuButton);
    }

    private void RegisterButton()
    {
        if (!HasValidGridCoordinate())
        {
            return;
        }

        Vector2Int key = new Vector2Int(gridX, gridY);
        if (buttonLookup.ContainsKey(key))
        {
            buttonLookup[key] = this;
        }
        else
        {
            buttonLookup.Add(key, this);
        }

        isRegistered = true;

        if (debugRegistration)
        {
            Debug.Log($"[BuildMenuButton] Registered button at ({gridX}, {gridY})", this);
        }
    }

    private void ApplyColorState()
    {
        if (image == null)
        {
            return;
        }

        if (isOccupied)
        {
            image.color = Color.red;
        }
        else
        {
            image.color = defaultColor;
        }
         
    }

    private void InitializeComponents()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (image != null && defaultColor == default)
        {
            defaultColor = image.color;
        }
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

    private void RegisterButtonIfValid()
    {
        if (!HasValidGridCoordinate())
        {
            return;
        }

        RegisterButton();
    }

    private void UnregisterCurrentKey()
    {
        if (!isRegistered)
        {
            return;
        }

        Vector2Int key = new Vector2Int(gridX, gridY);
        if (buttonLookup.ContainsKey(key) && buttonLookup[key] == this)
        {
            buttonLookup.Remove(key);
        }

        isRegistered = false;
    }

    private bool HasValidGridCoordinate()
    {
        return gridX > 0 && gridY > 0;
    }
 
}
