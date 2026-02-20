using UnityEngine;
using System.Collections;

public class OverheatCollider : MonoBehaviour
{
    public float minInterval = 20f;
    public float maxInterval = 30f;
    public float overheatDuration = 5f;

    [SerializeField] private bool isPlayerSun = true;

    private bool isOverheating = false;

    void Start()
    {
        StartCoroutine(RandomOverheatRoutine());
    }

    IEnumerator RandomOverheatRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            isOverheating = true;
            Debug.Log("OVERHEAT STARTED");
            Vibrate();
            yield return new WaitForSeconds(overheatDuration);

            isOverheating = false;
            Debug.Log("OVERHEAT ENDED");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isOverheating) return;

        PlantHandler plant = other.GetComponentInParent<PlantHandler>();
        if (plant == null) return;

        // Force monster every frame during overheat
        plant.SetFinalState(isPlayerSun);
        plant.currentAmount = plant.maxAmount;
    }

    public bool IsOverheating()
    {
        return isOverheating;
    }

    public void ForceStopOverheat()
    {
        StopAllCoroutines();
        isOverheating = false;
        Vibrate();
        // Restart timer cycle
        StartCoroutine(RandomOverheatRoutine());
    }
    void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}