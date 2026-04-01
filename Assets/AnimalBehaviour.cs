using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class AnimalBehaviour : MonoBehaviour {
   
    [Header("Need Settings")]
    public float needCheckInterval; 
    private float animalHappiness = 100f;
    public enum NeedType { None, Water, Urine, Food }
    public NeedType currentNeed = NeedType.None;
    private bool isNeedActive = false;

    [Header("UI References")]
    
    public GameObject[] needIndicators; // 0: Water, 1: Urine, 2: Food
    private bool playerInRange = false;
    void Start() {
        needCheckInterval = 10f; 
        InvokeRepeating("CheckForNeeds", 5f, needCheckInterval); 
    }

    void CheckForNeeds() {
        if (currentNeed == NeedType.None) {
            currentNeed = (NeedType)Random.Range(1, 4);
            isNeedActive = true;
            UpdateIcon();
        }
    }

    void UpdateIcon() {
            if (currentNeed != NeedType.None) {
                needIndicators[(int)currentNeed - 1].SetActive(true);

        }
    }

    void Update() {
        if (playerInRange && currentNeed != NeedType.None && Input.GetKeyDown(KeyCode.E)) 
        {
           MinigameManager.Instance.StartMinigame(currentNeed, this);
        }
        Debug.Log("Player in range: " + playerInRange + ", Current Need: " + currentNeed);
        for(int i = 0; i < needIndicators.Length; i++) {
            Debug.Log("Need Indicator " + i + ": " + needIndicators[i].activeSelf);
        }
    }

    public void ResolveNeed() {
        //Reset Need
        currentNeed = NeedType.None;
        foreach (GameObject indicator in needIndicators) {
            indicator.SetActive(false);
        }
        //Reset Need Clock
        float randomTime = Random.Range(5f, 30f);
        needCheckInterval = randomTime;
        isNeedActive = false;
    }

    private void OnTriggerStay(Collider other) {
        if(other.CompareTag("Player")) {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}