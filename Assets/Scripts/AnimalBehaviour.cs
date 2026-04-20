using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;

public class AnimalBehaviour : MonoBehaviour
{
    [Header("Need Settings")]
    [SerializeField] private float minNeedInterval = 12f;
    [SerializeField] private float maxNeedInterval = 30f;
    [SerializeField] private float activeNeedDuration = 30f;

    [SerializeField, Range(0f, 100f)] private float animalHappiness = 100f;

    public enum NeedType { None, Water, Urine, Food }
    public NeedType currentNeed = NeedType.None;
    private bool isNeedActive = false;
    private DayEndScript dayEndScript;
    private float needTimer;
    private float activeNeedTimer;

    [Header("UI References")]
    public GameObject[] needIndicators; // 0: Water, 1: Urine, 2: Food
    private bool playerInRange = false;

    public float Happiness => animalHappiness;
    public float HappinessNormalized => Mathf.Clamp01(animalHappiness / 100f);

    void Start()
    {
        dayEndScript = FindObjectOfType<DayEndScript>();
        needTimer = GetNextNeedInterval();
    }

    void CheckForNeeds()
    {
        if (dayEndScript == null)
        {
            dayEndScript = FindObjectOfType<DayEndScript>();
        }

        if (dayEndScript != null && !dayEndScript.IsDayRunning)
        {
            return;
        }

        if (currentNeed == NeedType.None)
        {
            int randomNeed = Random.Range(1, 4); // 1 to 3
            currentNeed = (NeedType)randomNeed;
            isNeedActive = true;
            activeNeedTimer = activeNeedDuration;
            UpdateIcon();
        }
    }

    void UpdateIcon()
    {
        if (currentNeed != NeedType.None)
        {
            needIndicators[(int)currentNeed - 1].SetActive(true);
        }
    }

    void Update()
    {
        if (dayEndScript == null)
        {
            dayEndScript = FindObjectOfType<DayEndScript>();
        }

        if (dayEndScript != null && !dayEndScript.IsDayRunning)
        {
            return;
        }

        if (currentNeed == NeedType.None)
        {
            needTimer -= Time.deltaTime;
            if (needTimer <= 0f)
            {
                CheckForNeeds();
                if (currentNeed == NeedType.None)
                {
                    needTimer = GetNextNeedInterval();
                }
            }
        }

        if (playerInRange && currentNeed != NeedType.None && Input.GetKeyDown(KeyCode.E))
        {
            MinigameManager manager = FindObjectOfType<MinigameManager>();
            if (manager != null)
            {
                manager.StartMinigame(currentNeed, this);
            }
        }

        if (isNeedActive)
        {
            animalHappiness -= Time.deltaTime * 5f;
            if (animalHappiness <= 0f)
            {
                animalHappiness = 0f;
                isNeedActive = false;
            }

            if (currentNeed != NeedType.None)
            {
                activeNeedTimer -= Time.deltaTime;
                if (activeNeedTimer <= 0f)
                {
                    ExpireCurrentNeed();
                }
            }
        }
    }

    public void ResolveNeed()
    {
        ClearNeedIndicators();
        currentNeed = NeedType.None;

        animalHappiness = Mathf.Clamp(animalHappiness + 20f, 0f, 100f);
        needTimer = GetNextNeedInterval();
        isNeedActive = false;
        activeNeedTimer = activeNeedDuration;
    }

    private void ExpireCurrentNeed()
    {
        ClearNeedIndicators();

        currentNeed = NeedType.None;
        isNeedActive = false;
        activeNeedTimer = activeNeedDuration;

        animalHappiness = Mathf.Clamp(animalHappiness - 10f, 0f, 100f);
        needTimer = GetNextNeedInterval();
    }

    private float GetNextNeedInterval()
    {
        float happinessT = Mathf.Clamp01(animalHappiness / 100f);
        return Mathf.Lerp(minNeedInterval, maxNeedInterval, happinessT);
    }

    private void ClearNeedIndicators()
    {
        if (needIndicators == null)
        {
            return;
        }

        foreach (GameObject indicator in needIndicators)
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}