using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameScreen : Screens
{
    public Button settings;
    RoundManager roundManager;

    internal override void AddListeners()
    {
        settings.onClick.AddListener(OnSettingsPress);
        roundManager.inGame = true;
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