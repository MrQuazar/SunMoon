using UnityEngine;
using Best.HTTP;
using Best.WebSockets;
using Best.WebSockets.Implementations;
using System;
using System.Collections;
// using Newtonsoft.Json;

public class SocketHandler : MonoBehaviour
{
    private WebSocket ws;

    // URL to your WebSocket server
    private string serverUrl = "ws://localhost:3000/ws"; // Change this to your server URL

    [SerializeField] private Transform cube1;
    [SerializeField] private Transform cube2;

    // Define a simple class for JSON serialization
    [System.Serializable]
    public class WebSocketMessage
    {
        public string type;
        public string roomId;
        public string data;
        public string did;
        public Vector3 pos; // Example additional field for position data
    }

    private void Start()
    {
        // Create WebSocket connection
        ws = new WebSocket(new System.Uri(serverUrl));

        // Single set of event handlers (match Best.WebSockets delegate signatures)
        ws.OnOpen += (WebSocket w) =>
        {
            Debug.Log("Connected to server");
            // After connection, join a room
            JoinRoom("ABCD");
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
            did = SystemInfo.deviceUniqueIdentifier,
            data = null // Optional field, can be used if needed
        };

        string json = JsonUtility.ToJson(joinMessage);
        ws.Send(json);
    }

    [ContextMenu("Send Dummy Data")]
    public void DummyData()
    {
        StartCoroutine(ContinouslySendData());
    }

    private IEnumerator ContinouslySendData()
    {
        int counter = 0;
        while (true)
        {
            string dummyData = "Hello, this is a test message!" + counter;

            // Send dummy data every 5 seconds
            SendData(dummyData);
            counter++;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Send data to the other player in the room
    public void SendData(string data)
    {
        WebSocketMessage dataMessage = new WebSocketMessage
        {
            type = "send_data",
            roomId = null, // Not needed in this case
            data = data,
            pos = cube1.position
        };

        string json = JsonUtility.ToJson(dataMessage);
        ws.Send(json);
        Debug.Log("Sent data: " + data);
    }
    string roomID;
    // Handle received messages based on type
    private void HandleReceivedMessage(string message)
    {
        WebSocketMessage receivedMessage = JsonUtility.FromJson<WebSocketMessage>(message);
        string type = receivedMessage.type;

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
    // [Header("Server Configuration")]
    // [SerializeField] private string serverUrl = "ws://YOUR_SERVER_IP:3000/ws";  // WebSocket URL (your Node.js server)
    // [SerializeField] private string roomId = "ABCD";  // Room ID to join

    // private WebSocket websocket;
    // private string myClientId;

    // // Start is called before the first frame update
    // void Start()
    // {
    //     ConnectToServer();
    // }

    // // Connect to the WebSocket signaling server
    // private void ConnectToServer()
    // {
    //     websocket = new WebSocket(new Uri(serverUrl));

    //     // When the connection is opened
    //     websocket.OnOpen += OnOpen;
    //     websocket.OnMessage += OnMessage;
    //     // websocket.OnError += OnError;
    //     // websocket.OnClosed += OnClosed;

    //     // Connect
    //     websocket.Open();
    // }

    // // Called when WebSocket connection is open
    // private void OnOpen(object sender)
    // {
    //     Debug.Log("Connected to WebSocket server.");

    //     // Join the room
    //     JoinRoom(roomId);
    // }

    // // Send message to the server (WebSocket)
    // private void SendMessageToServer(string messageType, string jsonData)
    // {
    //     var message = new
    //     {
    //         type = messageType,
    //         data = jsonData
    //     };
    //     websocket.Send(JsonUtility.ToJson(message));
    // }

    // // Join room method
    // private void JoinRoom(string roomId)
    // {
    //     var message = new { type = "join_room", roomId };
    //     SendMessageToServer("join_room", JsonUtility.ToJson(message));
    // }

    // // Handle incoming messages
    // private void OnMessage(object sender, string e)
    // {
    //     var message = e;
    //     Debug.Log("Received: " + message);

    //     // Parse the message
    //     // var response = JsonUtility.FromJson<MessageResponse>(message);

    //     // switch (response.type)
    //     // {
    //     //     case "room_created":
    //     //         Debug.Log("Room created, my Client ID: " + response.data.clientId);
    //     //         myClientId = response.data.clientId;
    //     //         break;

    //     //     case "room_joined":
    //     //         Debug.Log("Joined room, my Client ID: " + response.data.clientId);
    //     //         myClientId = response.data.clientId;
    //     //         break;

    //     //     case "room_full":
    //     //         Debug.Log("Room is full, cannot join.");
    //     //         break;

    //     //     case "peer_joined":
    //     //         Debug.Log("Peer joined: " + response.data.clientId);
    //     //         break;

    //     //     case "ready":
    //     //         Debug.Log("Both peers are ready, start WebRTC negotiation.");
    //     //         break;

    //     //     case "signal":
    //     //         HandleSignaling(response.data);
    //     //         break;

    //     //     case "peer_left":
    //     //         Debug.Log("Peer left: " + response.data.clientId);
    //     //         break;

    //     //     default:
    //     //         Debug.Log("Unhandled message type: " + response.type);
    //     //         break;
    //     // }
    // }

    // // Handle signaling (offer/answer/ice candidates)
    // private void HandleSignaling(dynamic signalData)
    // {
    //     // Debug.Log("Received signal from " + signalData.from);

    //     // switch (signalData.data.type)
    //     // {
    //     //     case "offer":
    //     //         Debug.Log("Received offer, handle it");
    //     //         // Handle offer (you can trigger the WebRTC logic here)
    //     //         break;

    //     //     case "answer":
    //     //         Debug.Log("Received answer, handle it");
    //     //         // Handle answer (you can trigger the WebRTC logic here)
    //     //         break;

    //     //     case "ice-candidate":
    //     //         Debug.Log("Received ICE candidate, handle it");
    //     //         // Handle ICE candidate (you can trigger the WebRTC logic here)
    //     //         break;

    //     //     default:
    //     //         Debug.Log("Unknown signal type");
    //     //         break;
    //     // }
    // }

    // // Handle errors
    // // private void OnError(object sender, BestHTTP.WebSocket.Events.ErrorEventArgs e)
    // // {
    // //     Debug.LogError("WebSocket Error: " + e.Exception);
    // // }

    // // // Handle WebSocket closure
    // // private void OnClosed(object sender, BestHTTP.WebSocket.Events.ClosedEventArgs e)
    // // {
    // //     Debug.Log("WebSocket connection closed.");
    // // }

    // // // Close the connection
    // // private void CloseConnection()
    // // {
    // //     if (websocket != null && websocket.State == WebSocketState.Open)
    // //     {
    // //         websocket.Close();
    // //     }
    // // }

    // // Struct for response data
    // [Serializable]
    // private class MessageResponse
    // {
    //     public string type;
    //     public dynamic data;
    // }

    // // For cleanup on closing
    // // private void OnApplicationQuit()
    // // {
    // //     CloseConnection();
    // // }
}
