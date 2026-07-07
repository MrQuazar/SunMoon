using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuitPanel : Screens
{
    private enum PanelMode
    {
        ConfirmQuit,   // "Are you sure you want to quit?" -> Yes / No
        OpponentQuit   // "Opponent has quit" -> Main Menu only
    }

    [Header("Info")]
    [SerializeField] private Image infoImage;
    [SerializeField] private Sprite confirmQuitSprite;   // "Are you sure you want to quit?"
    [SerializeField] private Sprite opponentQuitSprite;  // "Opponent has quit"

    [Header("Buttons")]
    [SerializeField] private Button yesBtn;
    [SerializeField] private Button noBtn;
    [SerializeField] private Button mainMenuBtn;

    private PanelMode mode = PanelMode.ConfirmQuit;

    // Where "No" sends the player back to. Set by whoever opened this panel via ShowConfirmQuit.
    private Screens returnScreen;

    private void Awake()
    {
        // Subscribed here (not in AddListeners) so we hear about an opponent
        // quitting even while this panel isn't the active screen.
        if (SocketHandler.instance != null)
            SocketHandler.instance.OnOpponentQuit += HandleOpponentQuit;
    }

    private void OnDestroy()
    {
        if (SocketHandler.instance != null)
            SocketHandler.instance.OnOpponentQuit -= HandleOpponentQuit;
    }

    internal override void AddListeners()
    {
        yesBtn.onClick.AddListener(OnYesPress);
        noBtn.onClick.AddListener(OnNoPress);
        mainMenuBtn.onClick.AddListener(OnMainMenuPress);
    }

    internal override void RemoveListeners()
    {
        yesBtn.onClick.RemoveListener(OnYesPress);
        noBtn.onClick.RemoveListener(OnNoPress);
        mainMenuBtn.onClick.RemoveListener(OnMainMenuPress);
    }

    internal override void Enable()
    {
        ApplyMode();
        base.Enable();
    }

    // Call this from wherever your gameplay "Quit" button lives, e.g.:
    //   quitPanel.ShowConfirmQuit(menuHandler.gameScreen);
    public void ShowConfirmQuit(Screens callingScreen)
    {
        mode = PanelMode.ConfirmQuit;
        returnScreen = callingScreen;
        menuHandler.ChangeScreen(menuHandler.quitPanel);
    }

    private void HandleOpponentQuit(string opponentClientId)
    {
        mode = PanelMode.OpponentQuit;
        menuHandler.ChangeScreen(menuHandler.quitPanel);
    }

    private void ApplyMode()
    {
        bool isConfirm = mode == PanelMode.ConfirmQuit;

        if (infoImage != null)
            infoImage.sprite = isConfirm ? confirmQuitSprite : opponentQuitSprite;

        if (yesBtn != null)
            yesBtn.gameObject.SetActive(isConfirm);

        if (noBtn != null)
            noBtn.gameObject.SetActive(isConfirm);

        if (mainMenuBtn != null)
            mainMenuBtn.gameObject.SetActive(!isConfirm);
    }

    private void OnYesPress()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click1);

        // Notify the opponent, then restart the game (single-level game,
        // so "main menu" and "restart" are the same scene load).
        if (SocketHandler.instance != null)
            SocketHandler.instance.QuitGame();

        SceneManager.LoadScene("MainGame");
    }

    private void OnNoPress()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click2);

        if (returnScreen != null)
            menuHandler.ChangeScreen(returnScreen);
        else
            menuHandler.ChangeScreen(menuHandler.gameScreen); // fallback
    }

    private void OnMainMenuPress()
    {
        AudioManager.Instance.PlaySFX(menuHandler.click1);
        SceneManager.LoadScene("MainGame");
    }
}
