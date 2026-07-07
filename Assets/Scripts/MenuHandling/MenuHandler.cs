using UnityEngine;

public class MenuHandler : MonoBehaviour
{
    public Screens previousScreen;
    public Screens currentScreen;

    [Header("All Screens")]
    public Screens settings;
    public Screens countdown;
    public Screens gameScreen;
    public Screens hostGame;
    public Screens joinGame;
    public Screens mainMenu;
    public Screens tutorial;
    public Screens lose;
    public Screens win;
    public Screens gameSettingsMenu;

    [Header("Screen Handling")]
    public Screens startScreen;

    [Header("Sounds")]
    public AudioClip click1;
    public AudioClip click2;
    public static MenuHandler instance;

    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        // Set menu handler for all
        settings.menuHandler = this;
        // countdown.menuHandler = this;
        // gameScreen.menuHandler = this;
        hostGame.menuHandler = this;
        joinGame.menuHandler = this;
        mainMenu.menuHandler = this;
        tutorial.menuHandler = this;
        // lose.menuHandler = this;
        // win.menuHandler = this;

        if (startScreen == null)
        {
            mainMenu.Enable();
            currentScreen = mainMenu;
        }
        else
        {
            startScreen.Enable();
            currentScreen = startScreen;
        }
    }

    public void ChangeScreen(Screens nextScreen)
    {
        previousScreen = currentScreen;
        currentScreen = nextScreen;

        previousScreen.Disable();
        currentScreen.Enable();
    }
}