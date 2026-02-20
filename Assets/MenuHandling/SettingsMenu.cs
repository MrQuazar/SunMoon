using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : Screens
{
    public Button back;

    internal override void AddListeners()
    {
        back.onClick.AddListener(OnBackPress);
    }

    private void OnBackPress()
    {
        menuHandler.ChangeScreen(menuHandler.mainMenu);
    }
}