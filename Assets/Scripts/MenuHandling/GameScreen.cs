using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameScreen : Screens
{
    public Button settings;
    public Button quitBtn;
    public RoundManager roundManager;
    public Spawner spawner;

    [Header("Replay Reset")]
    [Tooltip("Both players' OverheatCollider components. Their internal coroutine runs independently of this screen's visibility, so it needs an explicit reset on every entry.")]
    public OverheatCollider[] overheatColliders;
    public LobbyManager lobbyManager;

    internal override void AddListeners()
    {
        // settings.onClick.AddListener(OnSettingsPress);
        quitBtn.onClick.AddListener(OnQuitPress);

        // Runs on every entry into this screen — both the very first game
        // start and every replay — so gameplay always begins from a clean
        // state instead of carrying over timers/overheat/eclipse leftovers.
        spawner.ResetPlants();
        roundManager.Reset();
        roundManager.inGame = true;

        if (overheatColliders != null)
        {
            foreach (OverheatCollider oc in overheatColliders)
            {
                if (oc != null)
                    oc.ResetForReplay();
            }
        }

        if (lobbyManager != null)
            lobbyManager.ResetForReplay();
    }

    internal override void RemoveListeners()
    {
        //settings.onClick.RemoveListener(OnSettingsPress);
        quitBtn.onClick.RemoveListener(OnQuitPress);

        // This screen is being hidden - whether because we won, lost, or
        // quit. Whatever the reason, gameplay should stop right here:
        // otherwise OverheatCollider's interval coroutine, plant hover
        // conversions, and position sync all keep running (and making
        // noise/network traffic) behind the Win/Lose screen, since none of
        // that is otherwise tied to this screen's visibility.
        StopGameplay();
    }

    private void StopGameplay()
    {
        if (roundManager != null)
            roundManager.inGame = false;

        if (overheatColliders != null)
        {
            foreach (OverheatCollider oc in overheatColliders)
            {
                if (oc != null)
                    oc.StopForRoundEnd();
            }
        }

        // Stops SocketHandler.FixedUpdate's per-frame SendData loop, and
        // (paired with the SocketHandler-side guard) tells this client to
        // ignore any late in-round messages the opponent's client sends
        // before it also finishes freezing.
        if (SocketHandler.instance != null)
            SocketHandler.instance.hasGameStarted = false;
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