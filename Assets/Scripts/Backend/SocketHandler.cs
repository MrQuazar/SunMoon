using UnityEngine;
using Best.HTTP;
using Best.WebSockets;
using Best.WebSockets.Implementations;
using System;
using System.Collections;
// using Newtonsoft.Json;

public class SocketHandler : MonoBehaviour
{
    public static SocketHandler instance;

    private WebSocket ws;

    // URL to your WebSocket server
    private string serverUrl = "ws://172.24.144.152:3000/ws"; // Change this to your server URL

    [SerializeField] private Transform cube1;
    [SerializeField] private Transform cube2;

    private string[] players;

    private string myID;
    private bool hasGameStarted = false;
    private bool isPlayerSun = true;
    private Vector3 previousLocation = Vector3.zero;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        Debug.Log(hasGameStarted + " " + Vector3.Distance(cube1.position, previousLocation));
        if (hasGameStarted && Vector3.Distance(cube1.position, previousLocation) > 0.01f)
        {
            Debug.Log("Position changed, sending data...");
            SendData();
        }
    }

    [ContextMenu("Create New Room")]
    internal void CreateNewRoomRequest()
    {
        // 1. Create request with a callback
        var request = HTTPRequest.CreateGet("http://172.24.144.152:3000/room/create",
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
                Debug.LogError($"Request finished with error! Request state: {req.State}");
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

    // Send data to the other player in the room
    public void SendData()
    {
        WebSocketMessage dataMessage = new WebSocketMessage
        {
            type = "send_data",
            roomId = null, // Not needed in this case
            pos = cube1.position
        };

        string json = JsonUtility.ToJson(dataMessage);
        ws.Send(json);
        previousLocation = cube1.position;
        Debug.Log("Sent data: " + json);
    }
    string roomID;
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
        }

        if (type == "receive_data")
        {
            // Handle received data from the other player
            string receivedData = receivedMessage.data;
            Debug.Log("Received data from peer: " + receivedData);
            Vector3 receivedPos = receivedMessage.pos;
            cube2.position = new Vector3(receivedPos.x, receivedPos.y, receivedPos.z - 10);
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
            if (LobbySelectionScreen.instance != null)
            {
                if (LobbySelectionScreen.instance.gameObject.activeInHierarchy)
                    LobbySelectionScreen.instance.OnJoinRoomSuccess(roomID, players.Length);
                else if (LobbySelectionScreen.instance.lobbyWaitingScreen.gameObject.activeInHierarchy)
                    LobbySelectionScreen.instance.lobbyWaitingScreen.SetRoomCode(roomID, players.Length);
            }
            if (players.Length == 2)
            {
                Invoke(nameof(HandleStartGame), 2f);
            }
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
        }
        LobbyManager lobbyManager = gameObject.GetComponent<LobbyManager>();
        lobbyManager.roomCreated = true;
        lobbyManager.isPlayerSun = isPlayerSun;
        hasGameStarted = true;
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
}

[Serializable]
public class CreateRoomResponse
{
    public string roomId;
    public string message;
}