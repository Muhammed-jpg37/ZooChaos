using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarketController : MonoBehaviour
{
    public GameObject buildingsSlot;
    public GameObject tradeSlot;
    public Button buildingsButton;
    public Button tradeButton;
    public PlacementSystem  placementSystem;

    void Start()
    {
        buildingsButton.onClick.AddListener(BuildCategorySelector);
        tradeButton.onClick.AddListener(TradeCategorySelector);
         tradeSlot.SetActive(false);
        buildingsSlot.SetActive(true);
    }

    private void TradeCategorySelector()
    {
        buildingsSlot.SetActive(false);
        tradeSlot.SetActive(true);  
    }

    private void BuildCategorySelector()
    {
        tradeSlot.SetActive(false);
        buildingsSlot.SetActive(true);
    }
}
