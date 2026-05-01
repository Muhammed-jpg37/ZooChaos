using System.Collections;
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

    [Header("Feedback")]
    [SerializeField] private ParticleSystem splashPrefab;
    [SerializeField] private ParticleSystem overlapPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip reelLoopClip;
    [SerializeField] private AudioClip overlapClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private float progressPulseScale = 1.12f;
    [SerializeField] private float progressPulseDuration = 0.12f;
    [SerializeField] private float progressPulseMaxScale = 1.5f;
    [SerializeField] private Image redSegmentImage;
    [SerializeField] private Image yellowSegmentImage;
    [SerializeField] private Image greenSegmentImage;
    [SerializeField] private float barBounceScale = 1.08f;
    [SerializeField] private float barBounceDuration = 0.14f;
    [SerializeField] private Color overlapGlowColor = new Color(1f, 0.9f, 0.45f, 1f);

    private float barVelocity;
    private float targetVelocity;
    private float directionChangeTimer;
    private float progress;
    private float elapsedTime;
    private bool isRunning;
    // feedback runtime
    private float previousProgress;
    private bool wasOverlapping;
    private Image controlBarImage;
    private Image targetFoodImage;
    private Coroutine progressPulseCoroutine;
    private Coroutine barBounceCoroutine;
    private Vector3 progressSliderBaseScale;

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

        // feedback init
        previousProgress = progress;
        wasOverlapping = false;
        if (controlBar != null)
            controlBarImage = controlBar.GetComponent<Image>();
        if (targetFood != null)
            targetFoodImage = targetFood.GetComponent<Image>();

        if (progressSlider != null)
        {
            progressSliderBaseScale = progressSlider.transform.localScale;
        }

        if (audioSource != null && reelLoopClip != null)
        {
            audioSource.loop = false;
        }

        // ensure segment images are configured for filled vertical behavior
        ConfigureSegmentImage(redSegmentImage);
        ConfigureSegmentImage(yellowSegmentImage);
        ConfigureSegmentImage(greenSegmentImage);

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
                StartBarBounce();
            }
        }
        else if (nextY >= limit)
        {
            nextY = limit;
            if (barVelocity > 0f)
            {
                barVelocity = 0f;
                StartBarBounce();
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

            // ripple on direction change
            if (splashPrefab != null && targetFood != null)
            {
                ParticleSystem ps = Instantiate(splashPrefab, targetFood);
                ps.transform.localPosition = Vector3.zero;
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
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
            if (!wasOverlapping)
            {
                // enter overlap: play SFX and particles
                if (audioSource != null && overlapClip != null)
                {
                    audioSource.PlayOneShot(overlapClip);
                }

                if (overlapPrefab != null && targetFood != null)
                {
                    ParticleSystem ps = Instantiate(overlapPrefab, targetFood);
                    ps.transform.localPosition = Vector3.zero;
                    ps.Play();
                    Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                }
            }
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
            if (audioSource != null && successClip != null) audioSource.PlayOneShot(successClip);
            FinishGame(true);
        }
        else if (progress <= loseProgress)
        {
            if (audioSource != null && failClip != null) audioSource.PlayOneShot(failClip);
            FinishGame(false);
        }

        // pulse on progress increase
        if (progress > previousProgress)
        {
            StartProgressPulse();
        }
        previousProgress = progress;
        wasOverlapping = IsOverlapping(controlBar, targetFood);

        // update the 3-color segments to reflect current progress
        UpdateProgressColorSegments();
    }

    private void UpdateProgressColorSegments()
    {
        if (progressSlider == null) return;
        float max = progressSlider.maxValue > 0f ? progressSlider.maxValue : 100f;
        float p = Mathf.Clamp(progress, 0f, max);

        // ranges defined in same units as slider max (0..max). We'll treat them as absolute 0-100 if slider max is 100.
        // Use standard ranges: 0-40 red, 40-80 yellow, 80-100 green
        float redMax = 40f * (max / 100f);
        float yellowMax = 80f * (max / 100f);
        float greenMax = max; // 100%

        float redFill = redMax > 0f ? Mathf.Clamp01(p / redMax) : 0f;
        float yellowFill = 0f;
        if (p > redMax)
            yellowFill = Mathf.Clamp01((p - redMax) / (yellowMax - redMax));
        float greenFill = 0f;
        if (p > yellowMax)
            greenFill = Mathf.Clamp01((p - yellowMax) / (greenMax - yellowMax));

        if (redSegmentImage != null) redSegmentImage.fillAmount = redFill;
        if (yellowSegmentImage != null) yellowSegmentImage.fillAmount = yellowFill;
        if (greenSegmentImage != null) greenSegmentImage.fillAmount = greenFill;
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

    private void StartProgressPulse()
    {
        if (progressSlider == null) return;
        if (progressPulseCoroutine != null) StopCoroutine(progressPulseCoroutine);
        float scaleFactor = progressPulseScale;
        if (progressPulseMaxScale > 0f)
        {
            scaleFactor = Mathf.Min(progressPulseScale, progressPulseMaxScale);
        }
        progressPulseCoroutine = StartCoroutine(ProgressPulseRoutine(progressSlider.transform, scaleFactor));
    }

    private IEnumerator ProgressPulseRoutine(Transform t, float scaleFactor)
    {
        Vector3 original = progressSliderBaseScale == Vector3.zero ? t.localScale : progressSliderBaseScale;
        Vector3 target = original * scaleFactor;
        float half = progressPulseDuration * 0.5f;
        float timer = 0f;
        while (timer < half)
        {
            timer += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(original, target, timer / half);
            yield return null;
        }
        timer = 0f;
        while (timer < half)
        {
            timer += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(target, original, timer / half);
            yield return null;
        }
        t.localScale = original;
        progressPulseCoroutine = null;
    }

    private void StartBarBounce()
    {
        if (controlBar == null) return;
        if (barBounceCoroutine != null) StopCoroutine(barBounceCoroutine);
        barBounceCoroutine = StartCoroutine(BarBounceRoutine(controlBar));
    }

    private IEnumerator BarBounceRoutine(RectTransform rect)
    {
        Vector3 orig = rect.localScale;
        Vector3 peak = orig * barBounceScale;
        float half = barBounceDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(orig, peak, t / half);
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(peak, orig, t / half);
            yield return null;
        }
        rect.localScale = orig;
        barBounceCoroutine = null;
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

    private void ConfigureSegmentImage(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Vertical;
        img.fillOrigin = 0; // bottom
        img.fillClockwise = false;
        img.fillAmount = 0f;
    }
}
