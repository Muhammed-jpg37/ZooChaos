using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;

public class Fishing : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform controlBar;
    [SerializeField] private RectTransform targetFood;
    [SerializeField] private Slider progressSlider;

    [Header("Bar Physics")]
    [SerializeField] private float holdAcceleration = 3200f;
    [SerializeField] private float gravity = 2600f;
    [SerializeField] private float maxUpSpeed = 900f;
    [SerializeField] private float maxDownSpeed = 1200f;
    [SerializeField] private float bottomBounceFactor = 0.35f;

    [Header("Target Motion")]
    [SerializeField] private float minTargetSpeed = 160f;
    [SerializeField] private float maxTargetSpeed = 360f;
    [SerializeField] private float minDirectionChangeTime = 0.25f;
    [SerializeField] private float maxDirectionChangeTime = 1.1f;

    [Header("Score")]
    [SerializeField] private float progressGainPerSecond = 35f;
    [SerializeField] private float progressLossPerSecond = 26f;
    [SerializeField] [Range(0f, 100f)] private float startProgress = 30f;
    [SerializeField] private float winProgress = 100f;
    [SerializeField] private float loseProgress = 0f;
    [SerializeField] private float startGraceDuration = 1.2f;

    [Header("Reward")]
    [SerializeField] private int foodRewardAmount = 1;
    [SerializeField] private UnityEvent<int> onFoodCaught;
    [SerializeField] private UnityEvent onFoodEscaped;
    [SerializeField] private bool disableOnFinish = true;

    private float barVelocity;
    private float targetVelocity;
    private float directionChangeTimer;
    private float progress;
    private float elapsedTime;
    private bool isRunning;

    private void OnEnable()
    {
        StartGame();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        float dt = Time.deltaTime;
        UpdateBarPhysics(dt);
        UpdateTargetMotion(dt);
        UpdateProgress(dt);
    }

    public void StartGame()
    {
        if (playArea == null || controlBar == null || targetFood == null)
        {
            Debug.LogWarning("Fishing minigame is missing UI references.");
            isRunning = false;
            return;
        }

        progress = Mathf.Clamp(startProgress, loseProgress, winProgress);
        if (progressSlider != null)
        {
            progressSlider.minValue = loseProgress;
            progressSlider.maxValue = winProgress;
            progressSlider.value = progress;
        }

        barVelocity = 0f;
        targetVelocity = Random.Range(minTargetSpeed, maxTargetSpeed) * (Random.value < 0.5f ? -1f : 1f);
        directionChangeTimer = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
        elapsedTime = 0f;

        SetAnchoredY(controlBar, 0f);
        float targetLimit = GetMovementLimit(targetFood);
        float startSpread = Mathf.Min(targetLimit, controlBar.rect.height * 0.75f);
        SetAnchoredY(targetFood, Random.Range(-startSpread, startSpread));

        isRunning = true;
    }

    private void UpdateBarPhysics(float dt)
    {
        bool isHolding = Input.GetMouseButton(0) || Input.GetKey(KeyCode.X);
        if (isHolding)
        {
            barVelocity += holdAcceleration * dt;
        }
        else
        {
            barVelocity -= gravity * dt;
        }

        barVelocity = Mathf.Clamp(barVelocity, -maxDownSpeed, maxUpSpeed);

        float limit = GetMovementLimit(controlBar);
        float nextY = controlBar.anchoredPosition.y + (barVelocity * dt);

        if (nextY <= -limit)
        {
            nextY = -limit;
            if (barVelocity < 0f)
            {
                barVelocity = -barVelocity * bottomBounceFactor;
            }
        }
        else if (nextY >= limit)
        {
            nextY = limit;
            if (barVelocity > 0f)
            {
                barVelocity = 0f;
            }
        }

        SetAnchoredY(controlBar, nextY);
    }

    private void UpdateTargetMotion(float dt)
    {
        directionChangeTimer -= dt;
        if (directionChangeTimer <= 0f)
        {
            float randomDirection = Random.value < 0.5f ? -1f : 1f;
            float randomSpeed = Random.Range(minTargetSpeed, maxTargetSpeed);
            targetVelocity = randomDirection * randomSpeed;
            directionChangeTimer = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
        }

        float limit = GetMovementLimit(targetFood);
        float nextY = targetFood.anchoredPosition.y + (targetVelocity * dt);

        if (nextY <= -limit)
        {
            nextY = -limit;
            targetVelocity = Mathf.Abs(targetVelocity);
        }
        else if (nextY >= limit)
        {
            nextY = limit;
            targetVelocity = -Mathf.Abs(targetVelocity);
        }

        SetAnchoredY(targetFood, nextY);
    }

    private void UpdateProgress(float dt)
    {
        elapsedTime += dt;

        if (IsOverlapping(controlBar, targetFood))
        {
            progress += progressGainPerSecond * dt;
        }
        else if (elapsedTime >= startGraceDuration)
        {
            progress -= progressLossPerSecond * dt;
        }

        progress = Mathf.Clamp(progress, loseProgress, winProgress);

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progress >= winProgress)
        {
            FinishGame(true);
        }
        else if (progress <= loseProgress)
        {
            FinishGame(false);
        }
    }

    private void FinishGame(bool success)
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        bool endedByManager = false;

        if (success)
        {
            onFoodCaught?.Invoke(foodRewardAmount);
            Debug.Log($"Food caught! +{foodRewardAmount} food");

            if (MinigameManager.instance != null)
            {
                MinigameManager.instance.EndMinigame();
                endedByManager = true;
            }
        }
        else
        {
            onFoodEscaped?.Invoke();
            Debug.Log("Food escaped.");
        }

        if (disableOnFinish && !endedByManager)
        {
            gameObject.SetActive(false);
        }
    }

    private float GetMovementLimit(RectTransform element)
    {
        float areaHeight = playArea.rect.height;
        float elementHeight = element.rect.height;
        return Mathf.Max(0f, (areaHeight - elementHeight) * 0.5f);
    }

    private void SetAnchoredY(RectTransform rect, float y)
    {
        Vector2 p = rect.anchoredPosition;
        p.y = y;
        rect.anchoredPosition = p;
    }

    private bool IsOverlapping(RectTransform a, RectTransform b)
    {
        float aHalf = a.rect.height * 0.5f;
        float bHalf = b.rect.height * 0.5f;
        float aMin = a.anchoredPosition.y - aHalf;
        float aMax = a.anchoredPosition.y + aHalf;
        float bMin = b.anchoredPosition.y - bHalf;
        float bMax = b.anchoredPosition.y + bHalf;

        return aMax >= bMin && bMax >= aMin;
    }
}
