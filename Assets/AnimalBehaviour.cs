using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalBehaviour : MonoBehaviour
{
    public enum NeedType { Water = 0, Food = 1, Waste = 2, None = -1 }

    [System.Serializable]
    public class NeedConfig
    {
        public float minCooldown;
        public float maxCooldown;
        public GameObject needIcon;
        public GameObject miniGamePrefab; // Different mini game for each need
    }

    public int id;
    public NeedConfig[] needConfigs = new NeedConfig[3]; // Water, Food, Waste
    public bool isPlayerFixing;


    private NeedType currentNeed = NeedType.None;
    private GameObject activeIcon;
    private GameObject activeMiniGame;

    void Start()
    {
        StartCoroutine(NeedCheckCoroutine());
    }

    void Update()
    {
        if (isPlayerFixing)
        {
            currentNeed = NeedType.None;
            if (activeIcon != null) Destroy(activeIcon);
            
       
        }
    }

    private IEnumerator NeedCheckCoroutine()
    {
        while (true)
        {
            if (currentNeed == NeedType.None)
            {
                int randomNeed = Random.Range(0, 3);
                float cooldown = Random.Range(needConfigs[randomNeed].minCooldown, 
                                             needConfigs[randomNeed].maxCooldown);
                yield return new WaitForSeconds(cooldown);
                TriggerNeed((NeedType)randomNeed);
            }
            yield return null;
        }
    }

    private void TriggerNeed(NeedType need)
    {
        currentNeed = need;
        
        // Show corresponding icon
        if (needConfigs[(int)need].needIcon != null)
        {
            activeIcon = Instantiate(needConfigs[(int)need].needIcon, transform);
            activeIcon.SetActive(true);
        }

        

        Debug.Log($"Animal {id} needs: {need}");
    }
    private void MiniGameInstatiate(NeedType need)
    {
     // Instantiate corresponding mini game when player starts fixing
        if (needConfigs[(int)need].miniGamePrefab != null)
        {
            activeMiniGame = Instantiate(needConfigs[(int)need].miniGamePrefab, this.transform);
        }
    }
    

    public void FulfillNeed()
    {
        if (activeIcon != null)
            Destroy(activeIcon);
        MiniGameInstatiate(currentNeed);
        
   
    }

    public NeedType GetCurrentNeed() => currentNeed;
}
