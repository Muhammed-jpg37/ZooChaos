using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapOpener : MonoBehaviour
{
    public GameObject map;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OpenMap() {
        map.SetActive(true);
    }
    public void CloseMap() {
        map.SetActive(false);
    }
}
