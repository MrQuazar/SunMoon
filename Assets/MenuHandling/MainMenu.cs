using UnityEngine;
using UnityEngine.UI;

public class MainMenu : Screens
{
    public static MainMenu instance;
    public Button settings;
    public Button host;
    public Button join;

    private void Awake()
    {
        instance = this;
    }
    internal override void AddListeners()
    {
        settings.onClick.AddListener(OnSettingsPress);
        host.onClick.AddListener(OnHostPress);
        join.onClick.AddListener(OnJoinPress);
    }

    internal override void RemoveListeners()
    {
        settings.onClick.RemoveListener(OnSettingsPress);
        host.onClick.RemoveListener(OnHostPress);
        join.onClick.RemoveListener(OnJoinPress);
    }

    private void OnSettingsPress()
    {
        menuHandler.ChangeScreen(menuHandler.settings);
    }
    private void OnHostPress()
    {
        SocketHandler.instance.CreateNewRoomRequest();
    }

    public void ChangeScreenToHost()
    {
        Debug.Log("Changing screen to host menu.");
        menuHandler.ChangeScreen(menuHandler.hostGame);
    }
    private void OnJoinPress()
    {
        menuHandler.ChangeScreen(menuHandler.joinGame);
    }
}