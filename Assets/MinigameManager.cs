using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
   public Canvas[] minigameCanvases; // Assign in Inspector: 0-Water, 1-Urine, 2-Food
    public static MinigameManager Instance { get; private set; }
    private AnimalBehaviour currentTargetAnimal;
    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        currentTargetAnimal = null;
    }

    public void StartMinigame(AnimalBehaviour.NeedType need, AnimalBehaviour targetAnimal) {
        int index = (int)need - 1; // Convert enum to index
        if (index >= 0 && index < minigameCanvases.Length) {
            minigameCanvases[index].gameObject.SetActive(true);
            // You can also pass the targetAnimal reference to the minigame canvas if needed
            currentTargetAnimal = targetAnimal;
        }
    }

    public void EndMinigame() {
        if (currentTargetAnimal != null) 
        {
            currentTargetAnimal.ResolveNeed(); // Tell the animal to reset its need
        }
        currentTargetAnimal = null;
        foreach (Canvas canvas in minigameCanvases) {
            canvas.gameObject.SetActive(false);
        }
    }
}
