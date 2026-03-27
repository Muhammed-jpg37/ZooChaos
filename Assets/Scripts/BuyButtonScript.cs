using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class BuyButtonScript : MonoBehaviour
{
    public Sprite availableSprite;
    public Sprite unavailableSprite;
    public bool haveEnoughResources;

    public MarketController marketController;
    public int databaseBuildingID;
    void Start()
    {
       
        CheckResourceCountsUI();
       
        
    }


    public void ClickOnSlot()
    {
        if (haveEnoughResources)
        {
            marketController.placementSystem.StartPlacement(databaseBuildingID);
        }
        else
        {
            Debug.Log("Not enough resources to place this building.");
        }
    }

    private void CheckResourceCountsUI()
    {
        if (haveEnoughResources)
        {
            GetComponent<UnityEngine.UI.Image>().sprite = availableSprite;
            GetComponent<Button>().interactable = true;
        }
        else
        {
            GetComponent<UnityEngine.UI.Image>().sprite = unavailableSprite;
            GetComponent<Button>().interactable = false;
        }
    }

    private void HandleResourceChange()
    {
       

        bool requirementsMet = true;


        haveEnoughResources = requirementsMet;
        CheckResourceCountsUI();
    }

    
    
}
