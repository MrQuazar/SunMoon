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

        if (!isPlayerSun)
        {
            p2Controller.isPlayerController = true;
            p1Controller.isPlayerController = false;
        }
        else
        {
            p1Controller.isPlayerController = true;
            p2Controller.isPlayerController = false;
        }
    }
}
