using UnityEngine;
using UnityEngine.UI;

public class JoinMenu : Screens
{
    public static JoinMenu instance;

    [Header("UI References")]
    public Button back;
    public Button joinGameButton;
    public InputField roomCodeInput;

    [Header("Screens")]
    public HostMenu lobbyWaitingScreen;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        AddListeners();
        joinGameButton.interactable = false;
        LanHostDiscovery.Instance.StartListening(ip =>
        {
        SocketHandler.instance.SetServerAddress(ip, 8000);
        joinGameButton.interactable = roomCodeInput.text.Length == 5;
        });
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    internal override void AddListeners()
    {
        back.onClick.AddListener(OnBackPress);
        joinGameButton.onClick.AddListener(OnJoinPressed);
        roomCodeInput.onValueChanged.AddListener(OnRoomCodeChanged);
    }

    internal override void RemoveListeners()
    {
        back.onClick.RemoveListener(OnBackPress);
        joinGameButton.onClick.RemoveListener(OnJoinPressed);
        roomCodeInput.onValueChanged.RemoveListener(OnRoomCodeChanged);
    }

    private void OnBackPress()
    {
        menuHandler.ChangeScreen(menuHandler.mainMenu);
        AudioManager.Instance.PlaySFX(menuHandler.click1);
    }

    private void OnJoinPressed()
    {
        string roomCode = roomCodeInput.text.Trim().ToUpper();

        if (roomCode.Length != 5)
        {
            Debug.Log("Room code must be 5 characters.");
            return;
        }

        Debug.Log("Trying to join room: " + roomCode);
        SocketHandler.instance.JoinRoomRequest(roomCode);
    }

    private void OnRoomCodeChanged(string text)
    {
        // Auto uppercase while typing
        roomCodeInput.text = text.ToUpper();

        // Enable join button only if exactly 5 characters
        joinGameButton.interactable = roomCodeInput.text.Length == 5;
    }

    // ========================
    // SERVER RESPONSES
    // ========================

    internal void OnJoinRoomSuccess(string roomId, int playerCount)
    {
        Debug.Log("Successfully joined room: " + roomId);
        gameObject.SetActive(false);
    }

    internal void OnCreateRoomSuccess(string roomId)
    {
        Debug.Log("Successfully created room: " + roomId);
    }
}