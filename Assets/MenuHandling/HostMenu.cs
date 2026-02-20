using UnityEngine;
using UnityEngine.UI;

public class HostMenu : Screens
{
    public Button back;
    public Button startGame;

    [SerializeField] private Text txtRoomCode;

    internal override void AddListeners()
    {
        back.onClick.AddListener(OnBackPress);
    }

    internal override void RemoveListeners()
    {
        back.onClick.RemoveListener(OnBackPress);
    }

    private void OnBackPress()
    {
        menuHandler.ChangeScreen(menuHandler.mainMenu);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }

    private void OnStartPress()
    {
        //
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }
    internal void SetRoomCode(string roomCode, int playerCount)
    {
        Debug.Log($"Setting room code: {roomCode} with player count: {playerCount}");
        txtRoomCode.text = $"{roomCode}";
    }
}