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
        HostMenu hostMenu = menuHandler.hostGame.GetComponent<HostMenu>();
        if (hostMenu != null)
        {
            Debug.Log("Host menu found, setting room code.");
            hostMenu.SetRoomCode(SocketHandler.instance.roomID, SocketHandler.instance.players.Length);
        }
        else
        {
            Debug.LogError("Host menu component not found!");
        }
    }
    private void OnJoinPress()
    {
        menuHandler.ChangeScreen(menuHandler.joinGame);
    }
}