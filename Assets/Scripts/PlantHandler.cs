using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class PlantHandler : MonoBehaviour
{
    public CanvasGroup barGroup;
    public Image progressBar;
    public VisualEffect dustCloud;

    // Timer for the CURRENT, unbroken hover session only. Resets to 0 the
    // instant contact is lost or a state change fires - a state change
    // always requires a fresh, continuous maxAmount-second hover.
    [HideInInspector]
    public float currentAmount = 0f;
    public float maxAmount = 3f;

    [HideInInspector]
    public float currentTimeTriggered = 0;
    public float timeTriggered = 2;

    public bool isTriggered = false;
    private bool wasTriggered = false;

    public enum PlantState
    {
        dead,
        flower,
        monster
    }

    public Color[] stateColors = new Color[3];
    public GameObject[] statePrefabs = new GameObject[3];

    public PlantState currentState = PlantState.dead;
    public GameObject currentStatePrefab;

    // Stable network ID for this plant. Assigned by Spawner as it walks the
    // baked spawn data in a fixed order, so it lines up on both clients
    // (same seed/bake -> same spawn order -> same index).
    [HideInInspector]
    public int plantId = -1;

    // Guards against applying a stale/out-of-order network conversion for
    // this plant (e.g. two conversions land close together).
    private long lastNetworkTimestamp = 0;

    void Awake()
    {
        currentAmount = 0f;

        progressBar.color = stateColors[(int)currentState];
        currentStatePrefab = Instantiate(statePrefabs[(int)currentState], transform);
        dustCloud.SendEvent("Stop");
    }

    void Update()
    {
        progressBar.fillAmount = Mathf.Clamp01(currentAmount / maxAmount);

        if (!isTriggered)
        {
            if (wasTriggered)
            {
                // Contact just broke before a full hover was completed:
                // discard the partial progress so the next contact must
                // start the 3 seconds over from scratch.
                currentAmount = 0f;
            }

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

        wasTriggered = isTriggered;
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

    // Single source of truth for actually swapping state: updates the enum,
    // resets the hover timer, swaps colour/prefab, and puffs dust. Both the
    // hover-based ChangeState() and the instant force-setters below funnel
    // through here.
    //
    // `broadcast` distinguishes a LOCALLY-caused change (hover, eclipse,
    // overheat - this device's own colliders decided this) from a change
    // that's being force-applied because it arrived from the server. Only
    // locally-caused changes get sent back out, otherwise every conversion
    // would ping-pong between clients forever.
    private void SetState(PlantState newState, bool broadcast = true)
    {
        currentState = newState;
        currentAmount = 0f;
        progressBar.color = stateColors[(int)currentState];

        Destroy(currentStatePrefab);
        currentStatePrefab = Instantiate(statePrefabs[(int)currentState], transform);

        StartCoroutine(DustPuff());

        if (broadcast && plantId >= 0 && SocketHandler.instance != null && !SocketHandler.instance.isSinglePlayerMode)
        {
            SocketHandler.instance.SendPlantConversion(plantId, (int)newState);
        }
    }

    // Force-applies a state that the server relayed from the OTHER player's
    // client. Bypasses hover/timers entirely and never re-broadcasts (that
    // would just bounce the same conversion back and forth).
    // `serverTimestamp` lets us drop a message that arrived out of order
    // relative to one we've already applied for this same plant.
    public void ApplyNetworkState(PlantState newState, long serverTimestamp = 0)
    {
        if (serverTimestamp != 0 && serverTimestamp < lastNetworkTimestamp)
            return;

        if (serverTimestamp != 0)
            lastNetworkTimestamp = serverTimestamp;

        if (newState == currentState)
            return;

        SetState(newState, broadcast: false);
    }

    // Steps one state in `value`'s direction, respecting bounds (dead can't
    // go lower, monster can't go higher). Used for the hover-based
    // Sun/Moon conversion - NOT for the forced Set/Go methods below.
    public void ChangeState(int value = 1)
    {
        int stateCount = System.Enum.GetValues(typeof(PlantState)).Length;
        int newIndex = Mathf.Clamp((int)currentState + value, 0, stateCount - 1);

        if (newIndex == (int)currentState)
            return;

        SetState((PlantState)newIndex);
    }

    // Forced, instant state setters - bypass the hover requirement entirely.
    public void SetFinalState(bool isPlayerSun)
    {
        SetState(isPlayerSun ? PlantState.monster : PlantState.dead);
    }

    public void SetToMonster()
    {
        SetState(PlantState.monster);
    }

    public void SetToDead()
    {
        SetState(PlantState.dead);
    }

    public void GoToPerfect()
    {
        SetState(PlantState.flower);
    }

    // For resets that both clients compute independently from the same
    // deterministic baked data (e.g. Spawner.ResetPlants at the start of a
    // replay) - both sides arrive at the same answer on their own, so there's
    // no need to also broadcast it over the network.
    public void SetStateLocalOnly(PlantState newState)
    {
        SetState(newState, broadcast: false);
    }

    // Hover-based conversion: only steps the state once currentAmount has
    // built up to a full, unbroken maxAmount seconds of contact.
    public void IncreaseProgress(bool CanChangeState)
    {
        ApplyHover(1, CanChangeState);
    }

    public void DecreaseProgress(bool CanChangeState)
    {
        ApplyHover(-1, CanChangeState);
    }

    private void ApplyHover(int direction, bool canChangeState)
    {
        // Already at the extreme for this direction - nothing to build
        // toward, so don't let progress silently accumulate.
        if (direction > 0 && currentState == PlantState.monster) return;
        if (direction < 0 && currentState == PlantState.dead) return;

        currentAmount += Time.deltaTime;

        if (currentAmount >= maxAmount)
        {
            currentAmount = maxAmount;

            if (canChangeState)
            {
                // ChangeState -> SetState resets currentAmount back to 0,
                // so the next step requires another full 3s hover.
                ChangeState(direction);
            }
        }
    }
}