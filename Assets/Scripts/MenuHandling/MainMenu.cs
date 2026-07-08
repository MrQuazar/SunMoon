using UnityEngine;
using UnityEngine.UI;

public class MainMenu : Screens
{
    public static MainMenu instance;
    public Button settings;
    public Button tutorial;
    public Button host;
    public Button join;

    // TEMP: play-test entry point. Remove this button + OnSinglePlayerPress
    // once solo testing of the main game loop is no longer needed.
    public Button singlePlayer;

    private void Awake()
    {
        instance = this;
    }
    internal override void AddListeners()
    {
        settings.onClick.AddListener(OnSettingsPress);
        tutorial.onClick.AddListener(OnTutorialPress);
        host.onClick.AddListener(OnHostPress);
        join.onClick.AddListener(OnJoinPress);

        if (singlePlayer != null)
            singlePlayer.onClick.AddListener(OnSinglePlayerPress);
    }

    internal override void RemoveListeners()
    {
        settings.onClick.RemoveListener(OnSettingsPress);
        tutorial.onClick.RemoveListener(OnTutorialPress);
        host.onClick.RemoveListener(OnHostPress);
        join.onClick.RemoveListener(OnJoinPress);

        if (singlePlayer != null)
            singlePlayer.onClick.RemoveListener(OnSinglePlayerPress);
    }

    private void OnSettingsPress()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click1);
        menuHandler.ChangeScreen(menuHandler.settings);
    }
    private void OnTutorialPress()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click1);
        menuHandler.ChangeScreen(menuHandler.tutorial);
    }
    private void OnHostPress()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click1);
        SocketHandler.instance.CreateNewRoomRequest();
    }

    // TEMP: play-test only. Skips room create/join over HTTP/WS entirely and
    // drops straight into the game as Sun, with Moon idling in place, so the
    // main loop can be tested solo. MenuHandler.ChangeScreen(gameScreen) is
    // called from inside StartSinglePlayerMode(), same as the real
    // HandleStartGame() flow does after two players join.
    private void OnSinglePlayerPress()
    {
        SocketHandler.instance.StartSinglePlayerMode();
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
        AudioManager.Instance.PlaySFX(menuHandler.click1);
        menuHandler.ChangeScreen(menuHandler.joinGame);
    }
}