using UnityEngine;
using Best.HTTP;
using Best.WebSockets;
using System;

public class SocketHandler : MonoBehaviour
{
   [Header("Server Configuration")]
    [SerializeField] private string serverUrl = "ws://YOUR_SERVER_IP:3000/ws";  // WebSocket URL (your Node.js server)
    [SerializeField] private string roomId = "ABCD";  // Room ID to join

    private WebSocket websocket;
    private string myClientId;

    // Start is called before the first frame update
    void Start()
    {
        ConnectToServer();
    }

    // Connect to the WebSocket signaling server
    private void ConnectToServer()
    {
        websocket = new WebSocket(new Uri(serverUrl));

        // When the connection is opened
        websocket.OnOpen += OnOpen;
        websocket.OnMessage += OnMessage;
        // websocket.OnError += OnError;
        // websocket.OnClosed += OnClosed;

        // Connect
        websocket.Open();
    }

    // Called when WebSocket connection is open
    private void OnOpen(object sender)
    {
        Debug.Log("Connected to WebSocket server.");

        // Join the room
        JoinRoom(roomId);
    }

    // Send message to the server (WebSocket)
    private void SendMessageToServer(string messageType, string jsonData)
    {
        var message = new
        {
            type = messageType,
            data = jsonData
        };
        websocket.Send(JsonUtility.ToJson(message));
    }

    // Join room method
    private void JoinRoom(string roomId)
    {
        var message = new { type = "join_room", roomId };
        SendMessageToServer("join_room", JsonUtility.ToJson(message));
    }

    // Handle incoming messages
    private void OnMessage(object sender,string e)
    {
        var message = e;
        Debug.Log("Received: " + message);

        // Parse the message
        var response = JsonUtility.FromJson<MessageResponse>(message);

        switch (response.type)
        {
            case "room_created":
                Debug.Log("Room created, my Client ID: " + response.data.clientId);
                myClientId = response.data.clientId;
                break;

            case "room_joined":
                Debug.Log("Joined room, my Client ID: " + response.data.clientId);
                myClientId = response.data.clientId;
                break;

            case "room_full":
                Debug.Log("Room is full, cannot join.");
                break;

            case "peer_joined":
                Debug.Log("Peer joined: " + response.data.clientId);
                break;

            case "ready":
                Debug.Log("Both peers are ready, start WebRTC negotiation.");
                break;

            case "signal":
                HandleSignaling(response.data);
                break;

            case "peer_left":
                Debug.Log("Peer left: " + response.data.clientId);
                break;

            default:
                Debug.Log("Unhandled message type: " + response.type);
                break;
        }
    }

    // Handle signaling (offer/answer/ice candidates)
    private void HandleSignaling(dynamic signalData)
    {
        Debug.Log("Received signal from " + signalData.from);

        switch (signalData.data.type)
        {
            case "offer":
                Debug.Log("Received offer, handle it");
                // Handle offer (you can trigger the WebRTC logic here)
                break;

            case "answer":
                Debug.Log("Received answer, handle it");
                // Handle answer (you can trigger the WebRTC logic here)
                break;

            case "ice-candidate":
                Debug.Log("Received ICE candidate, handle it");
                // Handle ICE candidate (you can trigger the WebRTC logic here)
                break;

            default:
                Debug.Log("Unknown signal type");
                break;
        }
    }

    // Handle errors
    // private void OnError(object sender, BestHTTP.WebSocket.Events.ErrorEventArgs e)
    // {
    //     Debug.LogError("WebSocket Error: " + e.Exception);
    // }

    // // Handle WebSocket closure
    // private void OnClosed(object sender, BestHTTP.WebSocket.Events.ClosedEventArgs e)
    // {
    //     Debug.Log("WebSocket connection closed.");
    // }

    // // Close the connection
    // private void CloseConnection()
    // {
    //     if (websocket != null && websocket.State == WebSocketState.Open)
    //     {
    //         websocket.Close();
    //     }
    // }

    // Struct for response data
    [Serializable]
    private class MessageResponse
    {
        public string type;
        public dynamic data;
    }

    // For cleanup on closing
    // private void OnApplicationQuit()
    // {
    //     CloseConnection();
    // }
}
