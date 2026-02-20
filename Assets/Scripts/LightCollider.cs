using UnityEngine;

public class LightCollider : MonoBehaviour
{
    public bool isSun = true;
    public float increaseRate = 0.05f;

    private void OnTriggerStay(Collider other)
    {
        PlantHandler plant = other.GetComponent<PlantHandler>();
        if (plant == null) return;

        if (isSun)
        {
            plant.IncreaseProgress(increaseRate, true);
        }
        else
        {
            plant.DecreaseProgress(increaseRate, true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlantHandler plant = other.GetComponent<PlantHandler>();
        if (plant == null) return;

        plant.isTriggered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        PlantHandler plant = other.GetComponent<PlantHandler>();
        if (plant == null) return;

        plant.isTriggered = false;
    }
}