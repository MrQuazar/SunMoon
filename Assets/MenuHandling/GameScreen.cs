using UnityEngine;
using UnityEngine.UI;

public class GameScreen : Screens
{
    public Button settings;

    internal override void AddListeners()
    {
        settings.onClick.AddListener(OnSettingsPress);
    }

    internal override void RemoveListeners()
    {
        settings.onClick.RemoveListener(OnSettingsPress);
    }

    private void OnSettingsPress()
    {
        menuHandler.ChangeScreen(menuHandler.gameSettingsMenu);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }
}