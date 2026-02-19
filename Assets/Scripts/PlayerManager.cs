using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Players")]
    public GameObject player1;
    public GameObject player2;

    [Header("Game Mode")]
    public bool isPlayerSun = true;

    void Start()
    {
        SetupPlayers();
    }

    void SetupPlayers()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("PlayerManager: Player1 or Player2 is not assigned!");
            return;
        }

        PlayerController p1Controller = player1.GetComponent<PlayerController>();
        PlayerController p2Controller = player2.GetComponent<PlayerController>();

        if (p1Controller == null || p2Controller == null)
        {
            Debug.LogError("PlayerManager: One of the players does not have PlayerController!");
            return;
        }

        Camera p1Cam = player1.GetComponentInChildren<Camera>(true);
        Camera p2Cam = player2.GetComponentInChildren<Camera>(true);

        if (p1Cam == null || p2Cam == null)
        {
            Debug.LogError("PlayerManager: One of the players does not have a Camera child!");
            return;
        }

        if (isPlayerSun)
        {
            // Player 2 is controlled by Arrow Keys
            p2Controller.useArrowKeys = true;
            p1Controller.useArrowKeys = false;

            // Player 2 camera OFF, Player 1 camera ON
            p2Cam.gameObject.SetActive(false);
            p1Cam.gameObject.SetActive(true);
        }
        else
        {
            // Player 1 is controlled by Arrow Keys
            p1Controller.useArrowKeys = true;
            p2Controller.useArrowKeys = false;

            // Player 1 camera OFF, Player 2 camera ON
            p1Cam.gameObject.SetActive(false);
            p2Cam.gameObject.SetActive(true);
        }
    }
}
