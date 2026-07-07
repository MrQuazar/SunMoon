using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinScreen : Screens
{
    public Button playAgain;
    public Button mainMenu;

    internal override void AddListeners()
    {
        playAgain.onClick.AddListener(OnPlayAgain);
        mainMenu.onClick.AddListener(OnMainMenu);

        if (SocketHandler.instance != null)
            SocketHandler.instance.OnReplayStart += HandleReplayStart;

        // Reset button state each time this screen is shown
        playAgain.interactable = true;
    }

    internal override void RemoveListeners()
    {
        playAgain.onClick.RemoveListener(OnPlayAgain);
        mainMenu.onClick.RemoveListener(OnMainMenu);

        if (SocketHandler.instance != null)
            SocketHandler.instance.OnReplayStart -= HandleReplayStart;
    }

    private void OnPlayAgain()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click1);

        // Wait for the other player to also request a replay before
        // actually restarting (server confirms via OnReplayStart).
        playAgain.interactable = false;
        SocketHandler.instance.RequestReplay();
    }

    private void HandleReplayStart()
    {
        // Both players agreed — same room, jump back into gameplay.
        menuHandler.ChangeScreen(menuHandler.gameScreen);
    }

    private void OnMainMenu()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click1);

        // Let the other player know we left before we actually go.
        if (SocketHandler.instance != null)
            SocketHandler.instance.QuitGame();

        SceneManager.LoadScene("MainGame");
    }
}