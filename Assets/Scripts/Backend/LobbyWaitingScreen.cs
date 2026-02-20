using UnityEngine;
using UnityEngine.UI;

public class LobbyWaitingScreen : MonoBehaviour
{
    [SerializeField] private Text txtRoomCode;
    [SerializeField] private Text txtWaitingMessage;
    [SerializeField] private Text txtPlayer1Name;
    [SerializeField] private Text txtPlayer2Name;

    internal void SetRoomCode(string roomCode, int playerCount)
    {
        txtRoomCode.text = $"Room Code: {roomCode}";
        if (playerCount == 1)
        {
            txtWaitingMessage.text = "Waiting for another player to join...";
            txtPlayer1Name.text = "Player 1: You";
            txtPlayer2Name.text = "Player 2: Waiting...";
        }
        else if (playerCount == 2)
        {
            txtWaitingMessage.text = "Both players have joined! Starting game...";
            txtPlayer1Name.text = "Player 1: You";
            txtPlayer2Name.text = "Player 2: Opponent";
        }
    }

}
