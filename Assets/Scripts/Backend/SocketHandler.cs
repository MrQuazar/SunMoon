using UnityEngine;
using Best.HTTP;
using Best.WebSockets;
using Best.WebSockets.Implementations;
using System;
using System.Collections;
using DigitsNFCToolkit.Samples;
// using Newtonsoft.Json;

public class SocketHandler : MonoBehaviour
{
    public static SocketHandler instance;

    private WebSocket ws;

    [SerializeField] private bool useLocalhost = false; // Toggle this in the Inspector to switch between local and remote server
    private string baseURLLocal = "localhost:8000";
    private string baseURLRemote = "10.219.193.252:8000";

    // URL to your WebSocket server
    private string serverUrl = "ws://172.24.144.152:8000/ws"; // Change this to your server URL
    private string httpUrl = "http://";

    public Transform cube1;
    public Transform cube2;

    public string[] players;

    public string myID;
    internal bool hasGameStarted = false;
    private bool isPlayerSun = true;

    // TEMP: set true when the game was started via the single-player test button.
    // Skips all networking (no HTTP/WS calls) so the main game loop can be
    // play-tested on one device. Remove this whole block (and its call sites)
    // once real multiplayer testing takes over.
    internal bool isSinglePlayerMode = false;
    private Vector3 previousLocation = Vector3.zero;
    public Vector3 recievedLocation = Vector3.zero;

    internal bool isEclipseActive = false;

    // Track the last time an eclipse request was sent
    private DateTime timeSpan = DateTime.MinValue;

    private LobbyManager lobbyManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        // else
        // {
        //     Destroy(gameObject);
        // }

        lobbyManager = GetComponent<LobbyManager>();
    }

    private void Start()
    {
        if (useLocalhost)
        {
            serverUrl = "ws://" + baseURLLocal + "/ws";
            httpUrl = "http://" + baseURLLocal;

        }
        else
        {
            serverUrl = "ws://" + baseURLRemote + "/ws";
            httpUrl = "http://" + baseURLRemote;
        }
    }

    private void FixedUpdate()
    {
        // Debug.Log(hasGameStarted + " " + Vector3.Distance(cube1.position, previousLocation));
        // Single-player test mode has no socket, so never try to send over it.
        if (hasGameStarted && !isSinglePlayerMode /* && Vector3.Distance(cube1.position, previousLocation) > 0.01f */)
        {
            SendData();
        }
    }

    [ContextMenu("Create New Room")]
    internal void CreateNewRoomRequest()
    {
        // 1. Create request with a callback
        var request = HTTPRequest.CreateGet(httpUrl + "/room/create",
                                             CreateNewRoomResponse);

        // 3. Send request
        request.Send();
    }

    // 4. This callback is called when the request is finished. It might finished because of an error!
    private void CreateNewRoomResponse(HTTPRequest req, HTTPResponse resp)
    {
        switch (req.State)
        {
            case HTTPRequestStates.Finished:
                if (resp.IsSuccess)
                {
                    // 5. Here we can process the server's response
                    Debug.Log("Data received from server:" + resp.DataAsText);
                    CreateRoomResponse createRoomResponse = new CreateRoomResponse();
                    JsonUtility.FromJsonOverwrite(resp.DataAsText, createRoomResponse);
                    if (!string.IsNullOrEmpty(createRoomResponse.roomId))
                    {
                        Debug.Log($"Created room with ID: {createRoomResponse.roomId}");
                        roomID = createRoomResponse.roomId;
                        JoinRoomWS();
                    }
                    else
                    {
                        Debug.LogWarning("Room ID is missing in the response.");
                    }
                }
                else
                {
                    // 6. Error handling
                    Debug.Log($"Server sent an error: {resp.StatusCode}-{resp.Message}");
                }
                break;

            default:
                // 6. Error handling
                Debug.Log($"Request finished with error! Request state: {req.State}");
                break;
        }
    }

    internal void JoinRoomRequest(string roomId)
    {
        roomID = roomId;
        JoinRoomWS();
    }

    private void JoinRoomWS()
    {
        // Create WebSocket connection
        ws = new WebSocket(new System.Uri(serverUrl));

        // Single set of event handlers (match Best.WebSockets delegate signatures)
        ws.OnOpen += (WebSocket w) =>
        {
            Debug.Log("Connected to server");
            // After connection, join a room
        };

        ws.OnMessage += (WebSocket w, string message) =>
        {
            Debug.Log("Received message: " + message);
            // Handle received messages
            HandleReceivedMessage(message);
        };

        ws.OnClosed += (WebSocket w, WebSocketStatusCodes code, string reason) =>
        {
            Debug.Log("Disconnected from server. Code: " + code + " Reason: " + reason);
            hasGameStarted = false;
            // Clear reference
            ws = null;
        };

        // Note: Best.WebSockets doesn't expose an OnError delegate like this package sample, errors can surface via OnClosed or exceptions.

        // Open the WebSocket connection
        ws.Open();
    }

    // Join a room by sending a join message
    private void JoinRoom(string roomId)
    {
        WebSocketMessage joinMessage = new WebSocketMessage
        {
            type = "join_room",
            roomId = roomId,
            // did = SystemInfo.deviceUniqueIdentifier,
            data = null // Optional field, can be used if needed
        };

        string json = JsonUtility.ToJson(joinMessage);
        ws.Send(json);
    }

    private int eclipseDirection = -1;
    // Send data to the other player in the room
    public void SendData()
    {
        WebSocketMessage dataMessage = new WebSocketMessage
        {
            type = "send_data",
            roomId = null, // Not needed in this case
            pos = cube1.position
        };

        if (isEclipseActive)
        {
            float x = lobbyManager.eclipse.GetComponent<PlayerController>().moveDirGlobal.x;
            float y = lobbyManager.eclipse.GetComponent<PlayerController>().moveDirGlobal.y;
            float z = lobbyManager.eclipse.GetComponent<PlayerController>().moveDirGlobal.z;
            // Debug.LogError(x + " " + y + " " + z);
            // Debug.Log(x > 0f && z > 0f);
            if (x > 0f && y < 0f && z > 0f)
            {
                Debug.Log("Direction: Left");
                eclipseDirection = 0;
                dataMessage.direction = 0;
            }
            else if (x < 0f && y > 0f && z < 0f)
            {
                Debug.Log("Direction: Right");
                eclipseDirection = 1;
                dataMessage.direction = 1;
            }
            else if (x > 0f && y > 0f && z > 0f)
            {
                Debug.Log("Direction: Up");
                eclipseDirection = 2;
                dataMessage.direction = 2;
            }
            else if (x < 0f && y < 0f && z < 0f)
            {
                Debug.Log("Direction: Down");
                eclipseDirection = 3;
                dataMessage.direction = 3;
            }
        }

        string json = JsonUtility.ToJson(dataMessage);
        ws.Send(json);
        previousLocation = cube1.position;
        Debug.Log("Sent data: " + json);
    }
    public string roomID;
    internal bool isSameDirection = false;
    // Handle received messages based on type
    private void HandleReceivedMessage(string message)
    {
        WebSocketMessage receivedMessage = JsonUtility.FromJson<WebSocketMessage>(message);
        string type = receivedMessage.type;

        if (type == "connected")
        {
            Debug.Log("Received connected message. My ID: " + receivedMessage.clientId);
            myID = receivedMessage.clientId;
            JoinRoom(roomID);
            // SendEclipseRequest();

        }

        if (type == "receive_data")
        {
            // Handle received data from the other player
            string receivedData = receivedMessage.data;
            Debug.Log("Received data from peer: " + receivedData);
            // if (isEclipseActive && (receivedMessage.direction > 0 || eclipseDirection > 0))
            //     Debug.LogError("Other: " + receivedMessage.direction + " MY: " + eclipseDirection);
            isSameDirection = isEclipseActive && receivedMessage.direction == eclipseDirection;
            if (/* isEclipseActive && isSameDirection && */ myID != receivedMessage.clientId)
            {
                if (!isEclipseActive)
                {
                    recievedLocation = receivedMessage.pos;
                }
                else if (isEclipseActive && !isPlayerSun)
                {
                    recievedLocation = receivedMessage.pos;
                }
            }
        }
        else if (type == "room_created")
        {
            Debug.Log("Room created successfully.");
        }
        else if (type == "room_joined")
        {
            Debug.Log("Joined room successfully: " + receivedMessage.roomId);
            roomID = receivedMessage.roomId;
            players = receivedMessage.players;
            if (MainMenu.instance != null)
            {
                if (players[0] == myID)
                    MainMenu.instance.ChangeScreenToHost();
                else
                {
                    Debug.Log("Updating lobby waiting screen with room code and player count.");
                    JoinMenu.instance.lobbyWaitingScreen.SetRoomCode(roomID, players.Length);
                    JoinMenu.instance.OnJoinRoomSuccess(roomID, players.Length);
                }
            }
            if (players.Length == 2)
            {
                Invoke(nameof(HandleStartGame), 2f);
            }
        }
        else if (type == "eclipse_start")
        {
            Debug.Log("Eclipse started!");
            isEclipseActive = true;
            isPlayerSun = players.Length > 0 && players[0] == myID; // First player is Sun, second is Moon

            // LobbyManager lobbyManager = gameObject.GetComponent<LobbyManager>();
            lobbyManager.eclipse.transform.position = lobbyManager.player1.transform.position;
            // lobbyManager.eclipse.transform.localRotation = lobbyManager.player1.transform.localRotation;
            if (isPlayerSun)
            {
                cube1 = lobbyManager.eclipse.transform;
                lobbyManager.eclipse.GetComponent<PlayerController>().isPlayerController = true;
            }
            lobbyManager.eclipse.gameObject.SetActive(true);
            lobbyManager.HandleEclipseCameraTransition();
            lobbyManager.player1.gameObject.SetActive(false);
            lobbyManager.player2.gameObject.SetActive(false);

            timeSpan = DateTime.Now;
        }
        else if (type == "eclipse_end")
        {
            Debug.Log("Eclipse ended!");
            isEclipseActive = false;
            isPlayerSun = players.Length > 0 && players[0] == myID; // First player is Sun, second is Moon

            // LobbyManager lobbyManager = gameObject.GetComponent<LobbyManager>();
            lobbyManager.player1.gameObject.SetActive(true);
            lobbyManager.player2.gameObject.SetActive(true);
            if (isPlayerSun)
            {
                cube1 = lobbyManager.player1.transform;
                lobbyManager.eclipse.GetComponent<PlayerController>().isPlayerController = false;
            }
            lobbyManager.HandleCameraTransition();
            lobbyManager.eclipse.gameObject.SetActive(false);
        }
        else if (type == "peer_joined")
        {
            Debug.Log("A peer joined the room.");
        }
        else if (type == "peer_left")
        {
            Debug.Log("A peer left the room.");
        }
        else if (type == "room_full")
        {
            Debug.Log("Room is full, cannot join.");
        }
        else
        {
            Debug.Log("Unknown message type: " + type);
        }
    }

    // Ensure the WebSocket is closed properly when the object is destroyed
    private void OnDestroy()
    {
        if (ws != null)
        {
            try
            {
                if (ws.State == WebSocketStates.Open)
                {
                    // Close with normal closure code and a reason
                    ws.Close(WebSocketStatusCodes.NormalClosure, "OnDestroy");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Exception while closing WebSocket: " + ex.Message);
            }

            ws = null;
        }
    }

    private void HandleStartGame()
    {
        isPlayerSun = players.Length > 0 && players[0] == myID; // First player is Sun, second is Moon
        if (!isPlayerSun)
        {
            Transform temp = cube1;
            cube1 = cube2;
            cube2 = temp;
            NFCGameHandler.instance.OnMakeReadonlyClick();
        }
        else
        {
            NFCGameHandler.instance.OnPushMessageClick("Hello from Sun!");
        }
        // LobbyManager lobbyManager = gameObject.GetComponent<LobbyManager>();
        lobbyManager.roomCreated = true;
        lobbyManager.isPlayerSun = isPlayerSun;
        MenuHandler.instance.ChangeScreen(MenuHandler.instance.gameScreen);
        hasGameStarted = true;
    }

    // ------------------------------------------------------------------
    // TEMP TEST-ONLY: single-player mode.
    // Bypasses room-create/join over HTTP/WS entirely and drops the local
    // player straight into the game as Sun so the main loop can be
    // play-tested solo. Player2 (Moon) stays put in its spawn position
    // (see PlayerController.HandleMovement's isSinglePlayerMode check).
    // Delete this method and its call site in LobbySelectionScreen once
    // you no longer need solo play-testing.
    // ------------------------------------------------------------------
    [ContextMenu("Start Single Player (Test)")]
    internal void StartSinglePlayerMode()
    {
        isSinglePlayerMode = true;

        myID = "local_player";
        roomID = "LOCAL";
        players = new string[] { myID };
        isPlayerSun = true;

        lobbyManager.isPlayerSun = true;
        lobbyManager.roomCreated = true;

        hasGameStarted = true;

        MenuHandler.instance.ChangeScreen(MenuHandler.instance.gameScreen);
    }

    [ContextMenu("Send Eclipse Request")]
    internal void SendEclipseRequest()
    {
        if (DateTime.Now - timeSpan < TimeSpan.FromSeconds(60))
        {
            Debug.Log("Eclipse request sent too soon. Please wait before sending another request.");
            return;
        }
        WebSocketMessage dataMessage = new WebSocketMessage
        {
            type = "eclipse",
            roomId = roomID
        };
        string json = JsonUtility.ToJson(dataMessage);
        ws.Send(json);
        Debug.Log("Sending eclipse request: " + JsonUtility.ToJson(dataMessage));
    }
}

// Define a simple class for JSON serialization
[System.Serializable]
public class WebSocketMessage
{
    public string type;
    public string roomId;
    public string data;
    public string clientId;
    public Vector3 pos; // Example additional field for position data
    public string[] players; // Example additional field for player list
    public int direction = -1;   // 0 = left, 1 = right, 2 = up, 3 = down
}

[Serializable]
public class CreateRoomResponse
{
    public string roomId;
    public string message;
}