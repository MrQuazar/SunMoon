using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameScreen : Screens
{
    public Button settings;
    public Button quitBtn;
    public RoundManager roundManager;
    public Spawner spawner;

    internal override void AddListeners()
    {
        // settings.onClick.AddListener(OnSettingsPress);
        quitBtn.onClick.AddListener(OnQuitPress);
        spawner.ResetPlants();
        roundManager.inGame = true;
    }

    internal override void RemoveListeners()
    {
        //settings.onClick.RemoveListener(OnSettingsPress);
        quitBtn.onClick.RemoveListener(OnQuitPress);
    }

    private void OnSettingsPress()
    {
        menuHandler.ChangeScreen(menuHandler.gameSettingsMenu);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }

    private void OnQuitPress()
    {
        menuHandler.ChangeScreen(menuHandler.quitPanel);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }
}