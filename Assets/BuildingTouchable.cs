using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class BuildingTouchable : MonoBehaviour
{
    public string buildingName;
    public int id;
    void OnMouseDown()
    {
        Debug.Log("Mouse is over GameObject.");
        BuildingSidePanel.instance.SetActive(true, id);
    }
    void OnMouseExit()
    {
        // BuildingSidePanel.instance.SetActive(false,id);
    }
}
