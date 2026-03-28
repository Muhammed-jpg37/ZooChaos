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
    public GameObject MiniGamePanel;

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
            if (activeIcon != null)                Destroy(activeIcon);
            if (activeMiniGame != null)                Destroy(activeMiniGame);
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

        // Instantiate corresponding mini game
        if (needConfigs[(int)need].miniGamePrefab != null)
        {
            activeMiniGame = Instantiate(needConfigs[(int)need].miniGamePrefab, MiniGamePanel.transform);
        }

        Debug.Log($"Animal {id} needs: {need}");
    }

    public void FulfillNeed()
    {
        if (activeIcon != null)
            Destroy(activeIcon);
        if (activeMiniGame != null)
            Destroy(activeMiniGame);
        
        currentNeed = NeedType.None;
    }

    public NeedType GetCurrentNeed() => currentNeed;
}
