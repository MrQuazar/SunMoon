using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Drop-in replacement for server.js, running in-process on the host
/// player's device instead of on a PC. Speaks the exact same wire protocol
/// (same "type" strings, same JSON field names as WebSocketMessage in
/// SocketHandler.cs) so SocketHandler does not need to change - it just
/// gets pointed at 127.0.0.1 (the host itself) or the host's hotspot IP
/// (the joiner) instead of a remote server.
///
/// Usage:
///   Host:   EmbeddedGameServer.Instance.StartServer(8000);
///           SocketHandler.instance.SetServerAddress("127.0.0.1", 8000);
///           // then call CreateNewRoomRequest() exactly as before
///   Joiner: SocketHandler.instance.SetServerAddress(hostIp, 8000);
///           // then call JoinRoomRequest(code) exactly as before
/// </summary>
public class EmbeddedGameServer : MonoBehaviour
{
    public static EmbeddedGameServer Instance { get; private set; }

    public bool IsRunning { get; private set; }
    public int Port { get; private set; } = 8000;

    private TcpListener _listener;
    private Thread _acceptThread;
    private volatile bool _stop;

    // ---- state, mirrors the Maps in server.js ----
    private readonly object _lock = new object();
    private readonly Dictionary<string, List<ClientConn>> _rooms = new Dictionary<string, List<ClientConn>>();
    private readonly Dictionary<string, ClientConn> _clients = new Dictionary<string, ClientConn>();
    private readonly Dictionary<string, int> _roomSeeds = new Dictionary<string, int>();
    private readonly Dictionary<string, List<string>> _eclipseEvents = new Dictionary<string, List<string>>();
    private readonly Dictionary<string, HashSet<string>> _replayRequests = new Dictionary<string, HashSet<string>>();
    private readonly System.Random _rng = new System.Random();

    private class ClientConn
    {
        public string Id;
        public string RoomId;
        public bool HasQuit;
        public TcpClient Tcp;
        public NetworkStream Stream;
        public readonly object WriteLock = new object();
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else if (Instance != this) { Destroy(gameObject); }
    }

    private void OnApplicationQuit() => StopServer();
    private void OnDestroy() => StopServer();

    // =====================================================================
    // Lifecycle
    // =====================================================================

    public void StartServer(int port = 8000)
    {
        if (IsRunning) return;
        Port = port;
        _stop = false;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        IsRunning = true;
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
        _acceptThread.Start();
        Debug.Log($"[EmbeddedGameServer] Listening on 0.0.0.0:{port}");
    }

    public void StopServer()
    {
        if (!IsRunning) return;
        _stop = true;
        try { _listener?.Stop(); } catch { }

        lock (_lock)
        {
            foreach (var c in _clients.Values)
                CloseClient(c);
            _clients.Clear();
            _rooms.Clear();
            _roomSeeds.Clear();
            _eclipseEvents.Clear();
            _replayRequests.Clear();
        }
        IsRunning = false;
        Debug.Log("[EmbeddedGameServer] Stopped.");
    }

    private void AcceptLoop()
    {
        while (!_stop)
        {
            TcpClient tcp;
            try { tcp = _listener.AcceptTcpClient(); }
            catch { break; } // listener was stopped
            var t = new Thread(() => HandleConnection(tcp)) { IsBackground = true };
            t.Start();
        }
    }

    // =====================================================================
    // HTTP / WebSocket handshake
    // =====================================================================

    private void HandleConnection(TcpClient tcp)
    {
        try
        {
            tcp.NoDelay = true;
            var stream = tcp.GetStream();
            var headers = ReadHttpHeaders(stream, out string requestLine);
            if (requestLine == null) { tcp.Close(); return; }

            var parts = requestLine.Split(' ');
            string path = parts.Length > 1 ? parts[1] : "";

            if (path.StartsWith("/room/create"))
            {
                HandleCreateRoom(stream);
                tcp.Close();
                return;
            }

            bool isUpgrade = headers.TryGetValue("upgrade", out var upgradeVal)
                              && upgradeVal.ToLowerInvariant().Contains("websocket");

            if (path.StartsWith("/ws") && isUpgrade && headers.TryGetValue("sec-websocket-key", out var key))
            {
                CompleteWebSocketHandshake(stream, key);

                var client = new ClientConn
                {
                    Id = Guid.NewGuid().ToString(),
                    Tcp = tcp,
                    Stream = stream
                };
                lock (_lock) _clients[client.Id] = client;

                Send(client, new WebSocketMessage { type = "connected", clientId = client.Id });
                RunClientReadLoop(client);
                return;
            }

            WriteSimpleHttp(stream, 404, "not found");
            tcp.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[EmbeddedGameServer] connection error: " + e.Message);
            try { tcp.Close(); } catch { }
        }
    }

    private Dictionary<string, string> ReadHttpHeaders(NetworkStream stream, out string requestLine)
    {
        var headers = new Dictionary<string, string>();
        requestLine = ReadLine(stream);
        if (requestLine == null) return headers;

        string line;
        while (!string.IsNullOrEmpty(line = ReadLine(stream)))
        {
            int idx = line.IndexOf(':');
            if (idx <= 0) continue;
            string k = line.Substring(0, idx).Trim().ToLowerInvariant();
            string v = line.Substring(idx + 1).Trim();
            headers[k] = v;
        }
        return headers;
    }

    private string ReadLine(NetworkStream stream)
    {
        var sb = new StringBuilder();
        int prev = -1, cur;
        while ((cur = stream.ReadByte()) != -1)
        {
            if (prev == '\r' && cur == '\n')
            {
                sb.Length -= 1; // drop the \r we already appended
                return sb.ToString();
            }
            sb.Append((char)cur);
            prev = cur;
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    private void HandleCreateRoom(NetworkStream stream)
    {
        string roomId;
        lock (_lock)
        {
            roomId = _rng.Next(10000, 100000).ToString();
            _rooms[roomId] = new List<ClientConn>();
            // One seed per room, generated up front, same as /room/create in server.js
            _roomSeeds[roomId] = _rng.Next(0, int.MaxValue);
        }
        string json = $"{{\"roomId\":\"{roomId}\"}}";
        WriteSimpleHttp(stream, 200, json, "application/json");
    }

    private void WriteSimpleHttp(NetworkStream stream, int code, string body, string contentType = "text/plain")
    {
        string status = code == 200 ? "200 OK" : code == 404 ? "404 Not Found" : code.ToString();
        var bytes = Encoding.UTF8.GetBytes(body);
        string head = $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
        var headBytes = Encoding.UTF8.GetBytes(head);
        stream.Write(headBytes, 0, headBytes.Length);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private void CompleteWebSocketHandshake(NetworkStream stream, string clientKey)
    {
        const string magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        string accept;
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(clientKey + magic));
            accept = Convert.ToBase64String(hash);
        }
        string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                           "Upgrade: websocket\r\n" +
                           "Connection: Upgrade\r\n" +
                           $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
        var bytes = Encoding.UTF8.GetBytes(response);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    // =====================================================================
    // WebSocket framing (RFC 6455) - minimal single-frame text support,
    // which is all Best.WebSockets and our own SendData() ever produce.
    // =====================================================================

    private string ReadTextFrame(NetworkStream stream, out bool isClose)
    {
        isClose = false;
        int b0 = stream.ReadByte();
        int b1 = stream.ReadByte();
        if (b0 == -1 || b1 == -1) return null;

        int opcode = b0 & 0x0F;
        bool masked = (b1 & 0x80) != 0;
        long len = b1 & 0x7F;

        if (len == 126)
        {
            var ext = ReadExact(stream, 2);
            if (ext == null) return null;
            len = (ext[0] << 8) | ext[1];
        }
        else if (len == 127)
        {
            var ext = ReadExact(stream, 8);
            if (ext == null) return null;
            len = 0;
            for (int i = 0; i < 8; i++) len = (len << 8) | ext[i];
        }

        byte[] mask = null;
        if (masked)
        {
            mask = ReadExact(stream, 4);
            if (mask == null) return null;
        }

        var payload = ReadExact(stream, (int)len);
        if (payload == null) return null;

        if (masked)
            for (int i = 0; i < payload.Length; i++)
                payload[i] ^= mask[i % 4];

        if (opcode == 0x8) { isClose = true; return null; }   // close
        if (opcode == 0x9 || opcode == 0xA) return "";        // ping/pong, ignore content
        if (opcode != 0x1) return "";                         // ignore binary/continuation frames

        return Encoding.UTF8.GetString(payload);
    }

    private byte[] ReadExact(NetworkStream stream, int count)
    {
        if (count == 0) return new byte[0];
        var buf = new byte[count];
        int read = 0;
        while (read < count)
        {
            int n = stream.Read(buf, read, count - read);
            if (n <= 0) return null;
            read += n;
        }
        return buf;
    }

    private void WriteTextFrame(NetworkStream stream, string text)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        var header = new List<byte> { 0x81 }; // FIN + text opcode, unmasked (server->client)

        if (payload.Length < 126)
        {
            header.Add((byte)payload.Length);
        }
        else if (payload.Length <= ushort.MaxValue)
        {
            header.Add(126);
            header.Add((byte)(payload.Length >> 8));
            header.Add((byte)(payload.Length & 0xFF));
        }
        else
        {
            header.Add(127);
            long len = payload.Length;
            for (int i = 7; i >= 0; i--)
                header.Add((byte)((len >> (8 * i)) & 0xFF));
        }

        stream.Write(header.ToArray(), 0, header.Count);
        stream.Write(payload, 0, payload.Length);
        stream.Flush();
    }

    private void Send(ClientConn c, WebSocketMessage msg)
    {
        if (c == null || c.Tcp == null || !c.Tcp.Connected) return;
        string json = JsonUtility.ToJson(msg);
        lock (c.WriteLock)
        {
            try { WriteTextFrame(c.Stream, json); }
            catch { /* client disconnected mid-write; close loop will clean up */ }
        }
    }

    private void BroadcastToRoom(string roomId, WebSocketMessage msg, string exceptClientId = null)
    {
        List<ClientConn> room;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out room)) return;
            room = new List<ClientConn>(room); // snapshot, send outside the lock
        }
        foreach (var c in room)
        {
            if (c.Id == exceptClientId) continue;
            Send(c, msg);
        }
    }

    private void CloseClient(ClientConn c)
    {
        try { c.Stream?.Close(); } catch { }
        try { c.Tcp?.Close(); } catch { }
    }

    // =====================================================================
    // Message dispatch - mirrors the wss.on("connection") switch in server.js
    // =====================================================================

    private void RunClientReadLoop(ClientConn ws)
    {
        try
        {
            while (!_stop)
            {
                string json = ReadTextFrame(ws.Stream, out bool isClose);
                if (isClose || json == null) break;
                if (json.Length == 0) continue; // ping/pong/ignored frame

                WebSocketMessage msg;
                try { msg = JsonUtility.FromJson<WebSocketMessage>(json); }
                catch { continue; }

                HandleMessage(ws, msg);
            }
        }
        catch (Exception e)
        {
            Debug.Log("[EmbeddedGameServer] client loop ended: " + e.Message);
        }
        finally
        {
            HandleClientClosed(ws);
        }
    }

    private void HandleMessage(ClientConn ws, WebSocketMessage msg)
    {
        switch (msg.type)
        {
            case "join_room": HandleJoinRoom(ws, msg.roomId); break;
            case "leave_room": QuitRoom(ws, "quit"); Send(ws, new WebSocketMessage { type = "left_room" }); break;
            case "quit_game": QuitRoom(ws, "quit"); break;
            case "replay_request": HandleReplayRequest(ws); break;
            case "replay_cancel": HandleReplayCancel(ws); break;
            case "eclipse": HandleEclipse(ws, msg.roomId); break;
            case "plant_convert": HandlePlantConvert(ws, msg); break;
            case "send_data": HandleSendData(ws, msg); break;
        }
    }

    private void HandleJoinRoom(ClientConn ws, string roomId)
    {
        List<ClientConn> roomSnapshot = null;
        int seed = -1;
        bool invalid = false, full = false;

        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) { invalid = true; }
            else if (room.Count >= 2) { full = true; }
            else
            {
                room.Add(ws);
                ws.RoomId = roomId;
                roomSnapshot = new List<ClientConn>(room);
                _roomSeeds.TryGetValue(roomId, out seed);
            }
        }

        if (invalid) { Send(ws, new WebSocketMessage { type = "invalid_room", roomId = roomId }); return; }
        if (full) { Send(ws, new WebSocketMessage { type = "room_full", roomId = roomId }); return; }

        var players = roomSnapshot.ConvertAll(c => c.Id).ToArray();
        var payload = new WebSocketMessage { type = "room_joined", roomId = roomId, players = players, seed = seed };
        foreach (var c in roomSnapshot) Send(c, payload);

        Debug.Log($"[EmbeddedGameServer] {ws.Id} joined room {roomId} ({roomSnapshot.Count}/2)");
    }

    // Explicit quit - mirrors quitRoom() in server.js
    private void QuitRoom(ClientConn ws, string reason)
    {
        string roomId = ws.RoomId;
        if (string.IsNullOrEmpty(roomId)) return;

        List<ClientConn> remaining = null;
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                foreach (var c in room)
                    if (c != ws) Send(c, new WebSocketMessage { type = "opponent_quit", roomId = roomId, clientId = ws.Id });

                remaining = room.FindAll(c => c != ws);
                _rooms[roomId] = remaining;
            }
            _replayRequests.Remove(roomId);
        }

        ws.HasQuit = true;
        ws.RoomId = null;
        Debug.Log($"[EmbeddedGameServer] {ws.Id} quit room {roomId}");
    }

    private void HandleReplayRequest(ClientConn ws)
    {
        if (string.IsNullOrEmpty(ws.RoomId)) return;
        string roomId = ws.RoomId;

        List<ClientConn> room;
        HashSet<string> requests;
        bool allIn;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out room)) return;
            if (!_replayRequests.TryGetValue(roomId, out requests))
            {
                requests = new HashSet<string>();
                _replayRequests[roomId] = requests;
            }
            requests.Add(ws.Id);
            allIn = requests.Count >= room.Count && room.Count > 0;
            room = new List<ClientConn>(room);
        }

        foreach (var c in room)
            if (c != ws) Send(c, new WebSocketMessage { type = "replay_requested", roomId = roomId, clientId = ws.Id });

        if (allIn)
        {
            var players = room.ConvertAll(c => c.Id).ToArray();
            foreach (var c in room) Send(c, new WebSocketMessage { type = "replay_start", roomId = roomId, players = players });
            lock (_lock) _replayRequests.Remove(roomId);
        }
    }

    private void HandleReplayCancel(ClientConn ws)
    {
        if (string.IsNullOrEmpty(ws.RoomId)) return;
        lock (_lock)
        {
            if (_replayRequests.TryGetValue(ws.RoomId, out var requests))
                requests.Remove(ws.Id);
        }
        BroadcastToRoom(ws.RoomId, new WebSocketMessage { type = "replay_cancelled", roomId = ws.RoomId, clientId = ws.Id }, ws.Id);
    }

    // Mirrors the (slightly quirky) eclipse handling in server.js: each
    // trigger re-broadcasts eclipse_start with the current participant
    // list and schedules its own eclipse_end ~7s later.
    private void HandleEclipse(ClientConn ws, string roomId)
    {
        List<ClientConn> room;
        List<string> participants;

        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out room)) { Send(ws, new WebSocketMessage { type = "invalid_room", roomId = roomId }); return; }

            if (!_eclipseEvents.TryGetValue(roomId, out participants))
            {
                participants = new List<string>();
                _eclipseEvents[roomId] = participants;
                var capturedRoomId = roomId;
                new Timer(_ => { lock (_lock) _eclipseEvents.Remove(capturedRoomId); }, null, 3000, Timeout.Infinite);
            }

            if (participants.Contains(ws.Id)) return;
            participants.Add(ws.Id);
            room = new List<ClientConn>(room);
        }

        var startPayload = new WebSocketMessage { type = "eclipse_start", roomId = roomId, players = participants.ToArray() };
        foreach (var c in room) Send(c, startPayload);

        var capturedRoomId2 = roomId;
        var capturedParticipants = new List<string>(participants);
        new Timer(_ =>
        {
            var endPayload = new WebSocketMessage { type = "eclipse_end", roomId = capturedRoomId2, players = capturedParticipants.ToArray() };
            foreach (var c in room) Send(c, endPayload);
            lock (_lock) _eclipseEvents.Remove(capturedRoomId2);
        }, null, 7000, Timeout.Infinite);
    }

    private void HandlePlantConvert(ClientConn ws, WebSocketMessage msg)
    {
        if (string.IsNullOrEmpty(ws.RoomId)) return;
        var payload = new WebSocketMessage
        {
            type = "plant_convert",
            roomId = ws.RoomId,
            plantId = msg.plantId,
            state = msg.state,
            clientId = ws.Id,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        BroadcastToRoom(ws.RoomId, payload, ws.Id);
    }

    private void HandleSendData(ClientConn ws, WebSocketMessage msg)
    {
        if (string.IsNullOrEmpty(ws.RoomId)) return;
        var payload = new WebSocketMessage
        {
            type = "receive_data",
            clientId = ws.Id,
            data = msg.data,
            pos = msg.pos,
            direction = msg.direction
        };
        BroadcastToRoom(ws.RoomId, payload, ws.Id);
    }

    private void HandleClientClosed(ClientConn ws)
    {
        string roomId = ws.RoomId;
        if (!string.IsNullOrEmpty(roomId))
        {
            List<ClientConn> remaining = null;
            bool roomNowEmpty = false;
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room))
                {
                    remaining = room.FindAll(c => c != ws);
                    _rooms[roomId] = remaining;
                    roomNowEmpty = remaining.Count == 0;
                }
                _replayRequests.Remove(roomId);
                if (roomNowEmpty) _roomSeeds.Remove(roomId);
            }

            if (!ws.HasQuit && remaining != null)
                foreach (var c in remaining)
                    Send(c, new WebSocketMessage { type = "peer_left", roomId = roomId });

            Debug.Log($"[EmbeddedGameServer] {ws.Id} left room {roomId}");
        }

        lock (_lock) _clients.Remove(ws.Id);
        CloseClient(ws);
    }
}
