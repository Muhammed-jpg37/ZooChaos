using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    // Minigame types - 5 per need
   
     
    public enum MinigameType
    {
        // Water Need
        WaterFill,
        WaterPuzzle,
        WaterBalloon,
        WaterTilt,
        WaterMemory,

        // Urine Need
        UrineTiming,
        UrinePattern,
        UrineSwipe,
        UrineReflexes,
        UrinePuzzle,

        // Food Need
        FoodCatch,
        FoodSort,
        FoodTap,
        FoodMaze,
        FoodMatch
    }

    [System.Serializable]
    public class MinigameConfig
    {
        public MinigameType minigametype;
        public Canvas canvas;
        public AnimalBehaviour.NeedType requiredNeed;
    }

    public MinigameConfig[] minigameConfigs; // Assign all 15 minigame canvases in Inspector

    public  static MinigameManager instance { get;  set; }
    private AnimalBehaviour currentTargetAnimal;
    private MinigameType currentMinigame;
    private Dictionary<AnimalBehaviour.NeedType, List<MinigameType>> minigamePool;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        currentTargetAnimal = null;
        InitializeMinigamePool();
    }

    private void InitializeMinigamePool()
    {
        minigamePool = new Dictionary<AnimalBehaviour.NeedType, List<MinigameType>>();

        // Water minigames
        minigamePool[AnimalBehaviour.NeedType.Water] = new List<MinigameType>
        {
            MinigameType.WaterFill,
            MinigameType.WaterPuzzle,
            MinigameType.WaterBalloon,
            MinigameType.WaterTilt,
            MinigameType.WaterMemory
        };

        // Urine minigames
        minigamePool[AnimalBehaviour.NeedType.Urine] = new List<MinigameType>
        {
            MinigameType.UrineTiming,
            MinigameType.UrinePattern,
            MinigameType.UrineSwipe,
            MinigameType.UrineReflexes,
            MinigameType.UrinePuzzle
        };

        // Food minigames
        minigamePool[AnimalBehaviour.NeedType.Food] = new List<MinigameType>
        {
            MinigameType.FoodCatch,
            MinigameType.FoodSort,
            MinigameType.FoodTap,
            MinigameType.FoodMaze,
            MinigameType.FoodMatch
        };
    }

    public void StartMinigame(AnimalBehaviour.NeedType need, AnimalBehaviour targetAnimal)
    {
        if (need == AnimalBehaviour.NeedType.None || !minigamePool.ContainsKey(need))
        {
            Debug.LogError($"Invalid need type: {need}");
            return;
        }

        List<MinigameType> availableMinigames = minigamePool[need];
        currentMinigame = availableMinigames[Random.Range(0, availableMinigames.Count)];


        foreach (MinigameConfig config in minigameConfigs)
        {
            if (config.minigametype == currentMinigame && config.requiredNeed == need)
            {
                config.canvas.gameObject.SetActive(true);
                GenerateMinigameContext(config.minigametype);
                currentTargetAnimal = targetAnimal;
                return;
            }
        }

        Debug.LogError($"Minigame config not found for {currentMinigame}");
    }

    private void GenerateMinigameContext(MinigameType minigame)
    {
        
        int difficulty = Random.Range(1, 4); // Easy (1), Medium (2), Hard (3)
        float speed = 1f + (difficulty - 1) * 0.5f;
        
        switch (minigame)
        {
            case MinigameType.WaterFill:
                GenerateWaterFillContext(difficulty);
                break;
            case MinigameType.WaterPuzzle:
                GenerateWaterPuzzleContext(difficulty);
                break;
            case MinigameType.FoodCatch:
                GenerateFoodCatchContext(difficulty, speed);
                break;
            default:
                Debug.Log($"Minigame {minigame} started with difficulty {difficulty}");
                break;
        }
    }

    private void GenerateWaterFillContext(int difficulty)
    {

        float targetAmount = 50f + (difficulty * 25f);
        Debug.Log($"Water Fill: Target {targetAmount}% - Difficulty {difficulty}");
    }

    private void GenerateWaterPuzzleContext(int difficulty)
    {
        int numPieces = 4 + (difficulty * 2);
        Debug.Log($"Water Puzzle: {numPieces} pieces - Difficulty {difficulty}");
    }

    private void GenerateFoodCatchContext(int difficulty, float speed)
    {
        Debug.Log($"Food Catch: Speed {speed}x - Difficulty {difficulty}");
    }

    public void EndMinigame()
    {
        if (currentTargetAnimal != null)
        {
            currentTargetAnimal.ResolveNeed();
        }
        currentTargetAnimal = null;
        currentMinigame = MinigameType.WaterFill; // Reset to default

        foreach (MinigameConfig config in minigameConfigs)
        {
            config.canvas.gameObject.SetActive(false);
        }
    }

    public MinigameType GetCurrentMinigame()
    {
        return currentMinigame;
    }
}
