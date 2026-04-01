using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildMenuButton : MonoBehaviour
{
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
        Debug.Log("Button clicked at: " + x );
    }
    public void GetGridPositionY(int y) {
        Debug.Log("Button clicked at: " + y);
    }
 
}
