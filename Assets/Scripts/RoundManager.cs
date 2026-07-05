using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    public Spawner spawner;
    public Image progressBar;
    public List<GameObject> plants = new List<GameObject>();

    [Header("UI")]
    public TextMeshProUGUI timerText; // UI text to show danger timer
    public bool inGame = false;

    [Header("Digital Countdown")]
    [Tooltip("Text that shows the mm:ss countdown. Reuses timerText if left empty.")]
    public TextMeshProUGUI digitalTimerText;

    [Tooltip("What gets scaled for the attention pulse. Defaults to digitalTimerText's RectTransform if left empty.")]
    public RectTransform pulseTarget;

    [Tooltip("Whole-second marks (counting down) that should trigger one attention pulse each.")]
    public int[] pulseAtSeconds = { 60, 30 };

    [Tooltip("Once the countdown reaches this many seconds or fewer, pulse every single second.")]
    public int continuousPulseSeconds = 5;

    [Header("Pulse Animation")]
    public float pulseScaleUpTime = 0.15f;
    public float pulseScaleDownTime = 0.35f;
    public float pulseMaxScale = 1.3f;

    public MenuHandler menuHandler;
    public float currentTime = 0;
    public float maxTime = 5;

    public float winVal = 0.4f;

    private int lastWholeSecond = -1;
    private Coroutine pulseRoutine;

    void Start()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }

        if (pulseTarget == null && digitalTimerText != null)
        {
            pulseTarget = digitalTimerText.GetComponent<RectTransform>();
        }

        UpdateDigitalTimer(maxTime);
    }

    public void Reset()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            currentTime = 0;
        }

        lastWholeSecond = -1;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
        if (pulseTarget != null)
        {
            pulseTarget.localScale = Vector3.one;
        }

        UpdateDigitalTimer(maxTime);
    }

    public void GetPlants(List<GameObject> plants2)
    {
        plants = plants2;
    }

    void Update()
    {
        if (!inGame)
            return;

        if (SocketHandler.instance.hasGameStarted == false)
        {
            return;
        }

        float progress = CalculateProgress();
        UpdateProgressBar(progress);

        // WIN CONDITION
        if (progress >= winVal)
        {
            WinGame();
            return;
        }

        // LOSE TIMER LOGIC
        if (currentTime < maxTime)
        {
            currentTime += Time.deltaTime;

            float remaining = Mathf.Max(0f, maxTime - currentTime);
            UpdateDigitalTimer(remaining);
            CheckPulseThresholds(remaining);

            if (currentTime >= maxTime)
            {
                LoseGame();
                return;
            }
        }
        else
        {
            currentTime = 0;
        }
    }

    float CalculateProgress()
    {
        float totalCount = 0;
        float flowerCount = 0;

        foreach (GameObject plant in plants)
        {
            PlantHandler plantHandler = plant.GetComponent<PlantHandler>();
            totalCount++;
            if (plantHandler.currentState == PlantHandler.PlantState.flower)
            {
                // Debug.Log("FLOWER");
                flowerCount++;
            }
        }

        if (totalCount == 0) { return 0; }

        return flowerCount / totalCount;
    }

    void UpdateProgressBar(float progress)
    {
        // Debug.Log(progress);
        if (progressBar != null)
            progressBar.fillAmount = progress;
    }

    void UpdateDigitalTimer(float remainingSeconds)
    {
        TextMeshProUGUI target = digitalTimerText != null ? digitalTimerText : timerText;
        if (target == null)
            return;

        target.text = FormatTime(remainingSeconds);
    }

    string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }

    void CheckPulseThresholds(float remainingSeconds)
    {
        int wholeSecond = Mathf.CeilToInt(remainingSeconds);
        if (wholeSecond == lastWholeSecond)
            return;

        lastWholeSecond = wholeSecond;

        bool isNamedMark = System.Array.IndexOf(pulseAtSeconds, wholeSecond) >= 0;
        bool isFinalStretch = wholeSecond > 0 && wholeSecond <= continuousPulseSeconds;

        if (isNamedMark || isFinalStretch)
        {
            TriggerPulse();
        }
    }

    void TriggerPulse()
    {
        if (pulseTarget == null)
            return;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }
        pulseRoutine = StartCoroutine(PulseOnce());
    }

    IEnumerator PulseOnce()
    {
        Vector3 baseScale = Vector3.one;
        Vector3 peakScale = baseScale * pulseMaxScale;

        float t = 0f;
        while (t < pulseScaleUpTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / pulseScaleUpTime);
            pulseTarget.localScale = Vector3.Lerp(baseScale, peakScale, p);
            yield return null;
        }
        pulseTarget.localScale = peakScale;

        t = 0f;
        while (t < pulseScaleDownTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / pulseScaleDownTime);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            pulseTarget.localScale = Vector3.Lerp(peakScale, baseScale, eased);
            yield return null;
        }
        pulseTarget.localScale = baseScale;

        pulseRoutine = null;
    }

    void WinGame()
    {
        inGame = false;
        Debug.Log("YOU WIN! All blocks are C!");

        if (timerText != null)
            timerText.text = "YOU WIN!";

        menuHandler.ChangeScreen(menuHandler.win);
    }

    void LoseGame()
    {
        inGame = false;
        Debug.Log("YOU LOSE! Stayed below 40% too long.");

        if (timerText != null)
            timerText.text = "YOU LOSE!";

        menuHandler.ChangeScreen(menuHandler.lose);
    }
}