using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Constructable : MonoBehaviour
{
    NavMeshObstacle obstacle;
    private bool _thisBuildingIsDestroyed;
    public bool inPreviewMode;

    public void ConstructableWasPlaced()
    {
        ActivateObstacle();
    }

    private void ActivateObstacle()
    {
        obstacle = GetComponentInChildren<NavMeshObstacle>();
        obstacle.enabled = true;
    }

  
}
