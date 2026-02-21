using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    public Spawner spawner;
    public Image progressBar;
    public List<GameObject> plants = new List<GameObject>();
    public Image clock;

    [Header("UI")]
    public Text timerText; // UI text to show danger timer
    public bool inGame = false;

    public MenuHandler menuHandler;
    public float currentTime = 0;
    public float maxTime = 5;

    public float winVal = 0.4f;

    void Start()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }
        if (clock != null)
        {
            clock.fillAmount = 0f;
        }
    }

    public void Reset()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            currentTime = 0;
        }
        if (clock != null)
        {
            clock.fillAmount = 0f;
        }
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
            clock.fillAmount = currentTime / maxTime;

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
                Debug.Log("FLOWER");
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
