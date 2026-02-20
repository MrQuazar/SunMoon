using UnityEngine;
using UnityEngine.UI;

public class MainMenu : Screens
{
    public Button settings;
    public Button host;
    public Button join;

    internal override void AddListeners()
    {
        settings.onClick.AddListener(OnSettingsPress);
        host.onClick.AddListener(OnHostPress);
        join.onClick.AddListener(OnJoinPress);
    }

    private void OnSettingsPress()
    {
        menuHandler.ChangeScreen(menuHandler.settings);
    }
    private void OnHostPress()
    {
        menuHandler.ChangeScreen(menuHandler.hostGame);
    }
    private void OnJoinPress()
    {
        menuHandler.ChangeScreen(menuHandler.joinGame);
    }
}