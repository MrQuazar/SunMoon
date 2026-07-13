using UnityEngine;

public class LightCollider : MonoBehaviour
{
    public bool isSun = true;
    public float increaseRate = 0.05f;
    public bool isEclipse = false;

    // The PlayerController this collider belongs to. Every client has BOTH
    // a Sun and a Moon object in its scene (one is the locally-controlled
    // player, the other is just a visual driven by the last position the
    // network sent). Both have a LightCollider, so without a guard here
    // BOTH clients independently run hover logic for BOTH players -
    // including for the remote one, based on laggy/late position data -
    // and that's what caused plant conversions to desync between clients.
    // Only the collider on the object this client actually controls is
    // allowed to touch plant state locally; conversions caused by the
    // OTHER player only ever arrive via the server (plant_convert),
    // never from local collision here.
    [SerializeField] private PlayerController playerController;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();
    }

    private bool IsAuthoritative =>
        playerController != null &&
        (RoundManager.Instance == null || RoundManager.Instance.inGame) &&
        (playerController.isPlayerController || (SocketHandler.instance != null && SocketHandler.instance.isSinglePlayerMode));

    private void OnTriggerStay(Collider other)
    {
        if (!IsAuthoritative) return;

        PlantHandler plant = other.GetComponent<PlantHandler>();
        if (plant == null) return;

        if (isEclipse)
            plant.GoToPerfect();
        else if (isSun)
            plant.IncreaseProgress(true);
        else
            plant.DecreaseProgress(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAuthoritative) return;

        PlantHandler plant = other.GetComponent<PlantHandler>();
        if (plant == null) return;

        plant.isTriggered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAuthoritative) return;

        PlantHandler plant = other.GetComponent<PlantHandler>();
        if (plant == null) return;

        plant.isTriggered = false;
    }
}