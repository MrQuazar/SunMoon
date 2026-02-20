using UnityEngine;
using UnityEngine.UI;

public class WinScreen : Screens
{
    public Button playAgain;
    public Button mainMenu;

    internal override void AddListeners()
    {
        playAgain.onClick.AddListener(OnPlayAgain);
        mainMenu.onClick.AddListener(OnMainMenu);
    }

    internal override void RemoveListeners()
    {
        playAgain.onClick.RemoveListener(OnPlayAgain);
        mainMenu.onClick.RemoveListener(OnMainMenu);
    }

    private void OnPlayAgain()
    {
        // play again
    }
    private void OnMainMenu()
    {
        menuHandler.ChangeScreen(menuHandler.mainMenu);
    }
}