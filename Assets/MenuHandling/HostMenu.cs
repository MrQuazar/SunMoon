using UnityEngine;
using UnityEngine.UI;

public class HostMenu : Screens
{
    public Button back;
    public Button startGame;
    public Text status;

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
}