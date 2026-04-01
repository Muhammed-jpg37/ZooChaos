using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuButton : MonoBehaviour
{
    private int gridX;
    private int gridY;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsMouseOverUIElement()) {
               this.gameObject.GetComponent<Button>().interactable = false;
        } else {
               this.gameObject.GetComponent<Button>().interactable = true;
            }
    }
    private bool IsMouseOverUIElement() {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void GetGridPositionX(int x) {
        WriteGridPosition(x, gridY);
    }
    public void GetGridPositionY(int y) {
        WriteGridPosition(gridX, y);
    }

    private void WriteGridPosition(int x, int y) {
        gridX = x;
        gridY = y;
        Debug.Log("Grid Position set to: (" + gridX + ", " + gridY + ")");

        GridScript gridScript = FindObjectOfType<GridScript>();
        if (gridScript != null) {
            if (gridScript.IsCellEmpty(gridX, gridY)) {
                Debug.Log("Cell is empty. You can build here.");
            } else {
                Debug.Log("Cell is occupied. Choose another location.");
            }
        } else {
            Debug.LogError("GridScript not found in the scene.");
        }
    }
 
}
