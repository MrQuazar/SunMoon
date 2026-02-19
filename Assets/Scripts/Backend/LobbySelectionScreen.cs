using UnityEngine;
using UnityEngine.UI;

public class LobbySelectionScreen : MonoBehaviour
{
    public static LobbySelectionScreen instance;

    [Header("UI Elements")]
    [SerializeField] private Button btnCreateRoom;
    [SerializeField] private Button btnJoinRoom;

    [SerializeField] private InputField inputRoomID;

    [Header("Screen")]
    [SerializeField] internal LobbyWaitingScreen lobbyWaitingScreen;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        AddListener();
    }

    private void OnDisable()
    {
        RemoveListener();
    }

    private void AddListener()
    {
        btnCreateRoom.onClick.AddListener(OnClickCreateRoom);
        btnJoinRoom.onClick.AddListener(OnClickJoinRoom);
        inputRoomID.onValueChanged.AddListener(OnInputRoomIDChanged);
    }

    private void RemoveListener()
    {
        btnCreateRoom.onClick.RemoveListener(OnClickCreateRoom);
        btnJoinRoom.onClick.RemoveListener(OnClickJoinRoom);
        inputRoomID.onValueChanged.RemoveListener(OnInputRoomIDChanged);
    }

    private void OnClickCreateRoom()
    {
        SocketHandler.instance.CreateNewRoomRequest();
    }

    private void OnClickJoinRoom()
    {
        string roomId = inputRoomID.text;
        SocketHandler.instance.JoinRoomRequest(roomId);
    }

    private void OnInputRoomIDChanged(string text)
    {
        btnJoinRoom.interactable = !string.IsNullOrEmpty(text) && text.Length == 5;
    }

    //Responses
    internal void OnJoinRoomSuccess(string roomId, int playerCount)
    {
        Debug.Log("Successfully joined room: " + roomId);
        lobbyWaitingScreen.gameObject.SetActive(true);
        lobbyWaitingScreen.SetRoomCode(roomId, playerCount);
        gameObject.SetActive(false);
    }

    internal void OnCreateRoomSuccess(string roomId)
    {
        Debug.Log("Successfully created room: " + roomId);
    }
}
