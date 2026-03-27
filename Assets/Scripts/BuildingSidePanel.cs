using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuildingSidePanel : MonoBehaviour
{
    public static BuildingSidePanel instance { get; set; }
  
    public Image ForestSidePanel;
    public Image FoodPlaceSidePanel;
    public Image WellSidePanel;
    public Image IronMineSidePanel;

    private bool PanelAlreadyActive = false;
public TMP_Text forestManpowerText;
    

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        else { instance = this; }
        this.gameObject.SetActive(false);
    }
    public void SetActive(bool isActive, int id)
    {
        if (PanelAlreadyActive)
        {
            return;
        }
        else

            switch (id)
            {
                case 1: // Forest
                    ForestSidePanel.gameObject.SetActive(isActive);
                    this.gameObject.SetActive(isActive);
                    PanelAlreadyActive = isActive;
                   
                    break;
                case 2: // Iron Mine
                     IronMineSidePanel.gameObject.SetActive(isActive);
                    this.gameObject.SetActive(isActive);
                    PanelAlreadyActive = isActive;
                  
                    break;
                 case 3: // Well
                    WellSidePanel.gameObject.SetActive(isActive);
                    this.gameObject.SetActive(isActive);
                    PanelAlreadyActive = isActive;
                    break;
                case 4: // Food Place
                    FoodPlaceSidePanel.gameObject.SetActive(isActive);
                  PanelAlreadyActive = isActive;
                    this.gameObject.SetActive(isActive);
                    
                    break;
            }
    }

    public void CloseSidePanel(int CloseSidePanelD)
    {
        switch (CloseSidePanelD)
        {
            case 1:
                ForestSidePanel.gameObject.SetActive(false);
                PanelAlreadyActive = false;
                break;
            case 2:
                IronMineSidePanel.gameObject.SetActive(false);
                PanelAlreadyActive = false;
                break;
            case 3:
                WellSidePanel.gameObject.SetActive(false);
                PanelAlreadyActive = false;
                break;
            case 4:
                FoodPlaceSidePanel.gameObject.SetActive(false);
                PanelAlreadyActive = false;
                break;
        }
        PanelAlreadyActive = false;
    }

   

}
