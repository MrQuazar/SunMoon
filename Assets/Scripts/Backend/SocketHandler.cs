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
    private string baseURLRemote = "10.26.128.152:8000";

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

    internal int timeout = 10000; // Default timeout value in milliseconds

    // Track the last time an eclipse request was sent
    private DateTime timeSpan = DateTime.MinValue;

    private LobbyManager lobbyManager;

    // ---------- Replay / Quit events ----------
    // Hook these up from your menu/UI scripts instead of editing this file.
    public event Action OnReplayRequestedByOpponent; // opponent asked for a rematch, we haven't yet
    public event Action OnReplayStart;               // both players agreed, restart the session now
    public event Action<string> OnOpponentQuit;      // opponent quit (arg = their clientId); show "player quit" UI then go to main menu

    // ---------- World generation sync ----------
    // Both clients receive the same seed from the server (assigned once per
    // room in room_joined). Subscribe to this to trigger Planet + Spawner
    // generation only once the shared seed is known, instead of generating
    // independently in each script's own Start().
    public int worldSeed = -1;
    public event Action<int> OnWorldSeedReady;

    private bool hasQuit = false;

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

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f); // Wait a frame to ensure PlayerPrefs are loaded
        if (useLocalhost)
        {
            serverUrl = "ws://" + baseURLLocal + "/ws";
            httpUrl = "http://" + baseURLLocal;

        }
        else
        {
            serverUrl = "ws://" + FirebaseRemoteConfigConstants.BASE_URL + "/ws";
            httpUrl = "http://" + FirebaseRemoteConfigConstants.BASE_URL;
        }
        yield return null;
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

        // Once our round has ended (win/lose/quit) or hasn't started yet,
        // ignore anything that's only meaningful mid-round. Without this, a
        // message the opponent's client sent just before IT froze too
        // (their overheat/eclipse/plant conversion, one more position tick)
        // can still land here and swap cameras, move the remote cube, or
        // force-convert a plant behind the Win/Lose screen.
        bool isGameplayOnlyMessage = type == "receive_data" || type == "plant_convert"
            || type == "eclipse_start" || type == "eclipse_end";
        if (isGameplayOnlyMessage && !hasGameStarted)
            return;

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

            // Shared world seed from the server, generated once per room.
            // Both players receive the same value here, before HandleStartGame
            // fires, so Planet + Spawner generation is identical on both ends.
            if (receivedMessage.seed != -1)
            {
                worldSeed = receivedMessage.seed;
            }

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
            timeout = receivedMessage.timeout; // Set the timeout value from the received message
            // LobbyManager lobbyManager = gameObject.GetComponent<LobbyManager>();
            lobbyManager.eclipse.transform.position = lobbyManager.player1.transform.position;
            lobbyManager.eclipse.transform.localRotation = lobbyManager.player1.transform.localRotation;
            if (isPlayerSun)
            {
                cube1 = lobbyManager.eclipse.transform;
                lobbyManager.eclipse.GetComponent<PlayerController>().isPlayerController = true;
                // lobbyManager.eclipse.GetComponent<PlayerController>().StartEclipse();

            }
            else
            {
                cube2 = lobbyManager.eclipse.transform;
                lobbyManager.eclipse.GetComponent<PlayerController>().isPlayerController = false;
            }
            lobbyManager.eclipse.GetComponent<PlayerController>().StartEclipse();
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
                lobbyManager.eclipse.GetComponent<PlayerController>().EndEclipse();
            }
            else
            {
                cube2 = lobbyManager.player2.transform;
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
            // Unexpected disconnect (crash, network drop, tab close) rather
            // than an explicit quit_game — treat it the same way on the UI
            // side: show the "player left" screen, then go to main menu.
            Debug.Log("A peer left the room.");
            hasGameStarted = false;
            OnOpponentQuit?.Invoke(receivedMessage.clientId);
        }
        else if (type == "room_full")
        {
            Debug.Log("Room is full, cannot join.");
        }
        else if (type == "replay_requested")
        {
            // Opponent tapped "Replay" first — show "opponent wants a
            // rematch" UI. Fires only for the player who hasn't requested yet.
            Debug.Log("Opponent requested a replay.");
            OnReplayRequestedByOpponent?.Invoke();
        }
        else if (type == "replay_start")
        {
            // Both players have opted in — reset local game state and
            // jump back into the game screen using the same room.
            Debug.Log("Replay starting.");
            players = receivedMessage.players;

            // Clear anything left over from the previous match. Previously
            // hasGameStarted was reset to false here and never set back to
            // true, which silently froze RoundManager's timer (it gates on
            // hasGameStarted) and stopped position sync in FixedUpdate.
            isEclipseActive = false;
            isSameDirection = false;
            eclipseDirection = -1;
            timeSpan = DateTime.MinValue;
            previousLocation = Vector3.zero;

            // Subscribers (GameScreen, RoundManager, OverheatCollider,
            // LobbyManager) do their local resets in response to this event.
            OnReplayStart?.Invoke();

            hasGameStarted = true;
        }
        else if (type == "opponent_quit")
        {
            // The other player explicitly quit via the UI. Show the
            // "opponent quit" screen, then send both players to main menu.
            Debug.Log("Opponent quit the game.");
            hasGameStarted = false;
            OnOpponentQuit?.Invoke(receivedMessage.clientId);
        }
        else if (type == "plant_convert")
        {
            // The OTHER player's client caused this conversion (their Sun/Moon
            // hover, eclipse, or overheat). Force our copy of the same plant
            // to match - this is authoritative, not another vote toward a
            // locally-simulated hover.
            ApplyRemotePlantConversion(receivedMessage.plantId, receivedMessage.state, receivedMessage.timestamp);
        }
        else
        {
            Debug.Log("Unknown message type: " + type);
        }
    }

    private void ApplyRemotePlantConversion(int plantId, int state, long serverTimestamp)
    {
        if (plantId < 0) return;
        if (RoundManager.Instance == null || RoundManager.Instance.plants == null) return;
        if (plantId >= RoundManager.Instance.plants.Count) return;

        GameObject plantObj = RoundManager.Instance.plants[plantId];
        if (plantObj == null) return;

        PlantHandler plant = plantObj.GetComponent<PlantHandler>();
        if (plant == null) return;

        plant.ApplyNetworkState((PlantHandler.PlantState)state, serverTimestamp);
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

        // Both clients now have the same worldSeed (received in room_joined).
        // Fire the event so Planet + Spawner generate identical terrain and
        // spawn placements before the game screen is shown.
        OnWorldSeedReady?.Invoke(worldSeed);

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

        // No server involved in single-player, so there's no room_joined
        // message to supply a seed — pick a local one so world generation
        // still fires the same way it does in the networked flow.
        worldSeed = UnityEngine.Random.Range(0, int.MaxValue);
        OnWorldSeedReady?.Invoke(worldSeed);

        hasGameStarted = true;

        MenuHandler.instance.ChangeScreen(MenuHandler.instance.gameScreen);
    }

    // ---------- Plant conversion sync ----------
    // Called by PlantHandler whenever a LOCAL (this device's own, authoritative)
    // collision caused a state change - never for a change that itself arrived
    // from the network, so this can't ping-pong.
    public void SendPlantConversion(int plantId, int state)
    {
        if (ws == null || isSinglePlayerMode || string.IsNullOrEmpty(roomID)) return;

        WebSocketMessage msg = new WebSocketMessage
        {
            type = "plant_convert",
            roomId = roomID,
            plantId = plantId,
            state = state
        };
        ws.Send(JsonUtility.ToJson(msg));
    }

    [ContextMenu("Send Eclipse Request")]
    public void SendEclipseRequest()
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

    // ---------- Replay / Quit: call these from UI buttons ----------

    // Wire this to your "Replay" button. Both players need to call it
    // before the session actually restarts (server waits for both).
    [ContextMenu("Request Replay")]
    public void RequestReplay()
    {
        if (ws == null || string.IsNullOrEmpty(roomID))
        {
            Debug.LogWarning("Cannot request replay: no active room/socket.");
            return;
        }

        WebSocketMessage msg = new WebSocketMessage
        {
            type = "replay_request",
            roomId = roomID
        };
        ws.Send(JsonUtility.ToJson(msg));
        Debug.Log("Requested replay for room " + roomID);
    }

    // Wire this to your "Quit" button. Notifies the other player, then
    // closes this client's own connection.
    [ContextMenu("Quit Game")]
    public void QuitGame()
    {
        if (ws != null && !string.IsNullOrEmpty(roomID))
        {
            WebSocketMessage msg = new WebSocketMessage
            {
                type = "quit_game",
                roomId = roomID
            };
            ws.Send(JsonUtility.ToJson(msg));
        }

        hasGameStarted = false;
        hasQuit = true;

        if (ws != null)
        {
            ws.Close(WebSocketStatusCodes.NormalClosure, "Player quit");
            ws = null;
        }

        Debug.Log("Quit game, room " + roomID);
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
    public int timeout = 10000;
    public int seed = -1;        // Shared world seed for Planet + Spawner generation
    public int plantId = -1;     // Index into Spawner/RoundManager's plant list
    public int state = -1;       // PlantHandler.PlantState as int, for plant_convert
    public long timestamp = 0;   // Server-assigned time a plant_convert was relayed at
}

[Serializable]
public class CreateRoomResponse
{
    public string roomId;
    public string message;
}