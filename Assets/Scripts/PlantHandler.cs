using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class PlantHandler : MonoBehaviour
{
    public CanvasGroup barGroup;
    public Image progressBar;
    public VisualEffect dustCloud;

    [HideInInspector]
    public float currentAmount = 0f;
    public float maxAmount = 3f;

    [HideInInspector]
    public float currentTimeTriggered = 0;
    public float timeTriggered = 2;

    public bool isTriggered = false;
    public enum PlantState
    {
        dead,
        grass,
        flower,
        baby,
        monster
    }

    public Color[] stateColors = new Color[5];
    public GameObject[] statePrefabs = new GameObject[5];

    public PlantState currentState = PlantState.dead;
    public GameObject currentStatePrefab;

    void Awake()
    {
        if (currentState == PlantState.monster)
            currentAmount = maxAmount;
        else currentAmount = 0;

        progressBar.color = stateColors[(int)currentState];
        currentStatePrefab = Instantiate(statePrefabs[(int)currentState], transform);
        dustCloud.SendEvent("Stop");
    }

    void Update()
    {
        progressBar.fillAmount = Mathf.Clamp01(currentAmount / maxAmount);

        if (!isTriggered)
        {
            //DecreaseProgress(false);

            currentTimeTriggered -= 0.05f;
            if (currentTimeTriggered <= 0)
            {
                currentTimeTriggered = 0;
                ShowProgressBar(false);
            }
        }
        else
        {
            currentTimeTriggered = timeTriggered;
            ShowProgressBar(true);
        }
    }

    public void ShowProgressBar(bool show)
    {
        if (show)
        {
            barGroup.alpha = 1;
        }
        else
        {
            barGroup.alpha = 0;
        }
    }

    IEnumerator DustPuff()
    {
        dustCloud.SendEvent("OnPlay");
        yield return new WaitForSeconds(1.0f);
        dustCloud.SendEvent("Stop");
    }

    public void ChangeState(int value = 1)
    {
        if (currentState == PlantState.dead && value < 0) return;
        if (currentState == PlantState.monster && value > 0) return;

        if (value > 0)
        {
            currentAmount = 0;
        }
        else if (value < 0)
        {
            currentAmount = maxAmount;
        }
        currentState = (PlantState)((int)currentState + value);
        progressBar.color = stateColors[(int)currentState];


        Destroy(currentStatePrefab);
        currentStatePrefab = Instantiate(statePrefabs[(int)currentState], transform);

        StartCoroutine(DustPuff());
    }

    public void SetFinalState(bool isPlayerSun)
    {
        currentState = isPlayerSun ? PlantState.monster : PlantState.dead;
        progressBar.color = stateColors[(int)currentState];

        Destroy(currentStatePrefab);
        currentStatePrefab = Instantiate(statePrefabs[(int)currentState], transform);
    }

    public void IncreaseProgress(bool CanChangeState)
    {
        currentAmount += Time.deltaTime;

        if (currentAmount > maxAmount)
            currentAmount = maxAmount;

        if (currentAmount >= maxAmount && CanChangeState)
        {
            ChangeState(1);
        }
    }

    public void DecreaseProgress(bool CanChangeState)
    {
        currentAmount -= Time.deltaTime;

        if (currentAmount < 0)
            currentAmount = 0;

        if (currentAmount <= 0 && CanChangeState)
        {
            ChangeState(-1);
        }
    }

    public void SetToMonster()
    {
        currentState = PlantState.baby;
        ChangeState(1);
    }

    public void SetToDeadr()
    {

        currentState = PlantState.grass;
        ChangeState(-1);
    }

    public void GoToPerfect()
    {

        currentState = PlantState.grass;
        ChangeState(1);

        // else
        // {
        //     IncreaseProgress(false);
        // }
    }
}