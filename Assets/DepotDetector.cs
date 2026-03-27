using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DepotDetector : MonoBehaviour
{
    public GameObject R;
    public void OnMouseDown()
    {
        Instantiate(R,transform.position,Quaternion.identity);
    }
     
}
