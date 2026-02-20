using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    public Spawner spawner;
    public Slider progressSlider;

    [Header("Win/Lose Settings")]
    public string correctTag = "Flower";
    public float loseThreshold = 0.4f;     // 40%
    public float loseTimeLimit = 60f;      // 60 seconds

    [Header("UI")]
    public Text timerText; // UI text to show danger timer

    private float belowThresholdTimer = 0f;
    private bool gameEnded = false;

    void Start()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }
    }

    void Update()
    {
        if (gameEnded)
            return;

        float progress = CalculateProgress();
        UpdateProgressBar(progress);

        // WIN CONDITION
        if (progress >= 1f)
        {
            WinGame();
            return;
        }

        // LOSE TIMER LOGIC
        if (progress < loseThreshold)
        {
            belowThresholdTimer += Time.deltaTime;

            if (belowThresholdTimer >= loseTimeLimit)
            {
                LoseGame();
                return;
            }
        }
        else
        {
            // Reset timer if player recovers above 40%
            belowThresholdTimer = 0f;
        }

        UpdateTimerUI();
    }

    float CalculateProgress()
    {
        if (spawner == null || spawner.spawned == null || spawner.spawned.Count == 0)
            return 0f;

        int totalBlocks = 0;
        int correctBlocks = 0;

        foreach (GameObject block in spawner.spawned)
        {
            if (block == null)
                continue;

            totalBlocks++;

            if (block.CompareTag(correctTag))
                correctBlocks++;
        }

        if (totalBlocks == 0)
            return 0f;

        return (float)correctBlocks / totalBlocks;
    }

    void UpdateProgressBar(float progress)
    {
        if (progressSlider != null)
            progressSlider.value = progress;
    }

    void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        if (progressSlider.value < loseThreshold)
        {
            float remaining = loseTimeLimit - belowThresholdTimer;
            timerText.text = $"Danger: {remaining:F1}s";
        }
        else
        {
            timerText.text = "Safe";
        }
    }

    void WinGame()
    {
        gameEnded = true;
        Debug.Log("YOU WIN! All blocks are C!");

        if (timerText != null)
            timerText.text = "YOU WIN!";
    }

    void LoseGame()
    {
        gameEnded = true;
        Debug.Log("YOU LOSE! Stayed below 40% too long.");

        if (timerText != null)
            timerText.text = "YOU LOSE!";
    }
}
