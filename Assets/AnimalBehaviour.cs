using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class AnimalBehaviour : MonoBehaviour {
    public enum NeedType { None, Water, Urine, Food }
    public NeedType currentNeed = NeedType.None;

    [Header("UI References")]
    
    public GameObject[] needIndicators; // 0: Water, 1: Urine, 2: Food


    private bool playerInRange = false;

    void Start() {
   

        InvokeRepeating("CheckForNeeds", 5f, 10f); // Check every 10s
    }

    void CheckForNeeds() {
        if (currentNeed == NeedType.None) {
            currentNeed = (NeedType)Random.Range(1, 4);
            UpdateIcon();
        }
    }

    void UpdateIcon() {
            if (currentNeed != NeedType.None) {
                needIndicators[(int)currentNeed - 1].SetActive(true);
         
        }
    }

    void Update() {
        if (playerInRange && currentNeed != NeedType.None && Input.GetKeyDown(KeyCode.E)) {
            // Tell the Manager to start the game and pass THIS animal as the target
           MinigameManager.Instance.StartMinigame(currentNeed, this);
        }
        Debug.Log("Player in range: " + playerInRange + ", Current Need: " + currentNeed);
    }

    public void ResolveNeed() {
        currentNeed = NeedType.None;
        foreach (GameObject indicator in needIndicators) {
            indicator.SetActive(false);
        }
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