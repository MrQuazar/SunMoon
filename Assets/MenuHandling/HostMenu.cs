using UnityEngine;
using UnityEngine.UI;

public class HostMenu : Screens
{
    public Button back;
    public Button copyCode;

    [SerializeField] private Text txtRoomCode;

    internal override void AddListeners()
    {
        back.onClick.AddListener(OnBackPress);
        copyCode.onClick.AddListener(OnCopyCodePress);
    }

    internal override void RemoveListeners()
    {
        back.onClick.RemoveListener(OnBackPress);

        copyCode.onClick.RemoveListener(OnCopyCodePress);
    }

    private void OnBackPress()
    {
        menuHandler.ChangeScreen(menuHandler.mainMenu);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }

    private void OnCopyCodePress()
    {
        GUIUtility.systemCopyBuffer = txtRoomCode.text;
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }
    internal void SetRoomCode(string roomCode, int playerCount)
    {
        Debug.Log($"Setting room code: {roomCode} with player count: {playerCount}");
        txtRoomCode.text = $"{roomCode}";
    }
}