using UnityEngine;
using UnityEngine.UI;

public class JoinMenu : Screens
{
    public Button back;

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
    }
}