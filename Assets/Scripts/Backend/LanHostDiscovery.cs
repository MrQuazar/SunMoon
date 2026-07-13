using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Lets the joining phone find the host's IP on the hotspot network
/// automatically, so nobody has to type an IP address in - only the
/// 5-digit room code, same as today.
///
/// Host:   call StartBroadcasting() right after EmbeddedGameServer starts.
/// Joiner: call StartListening(ip => ...) when the Join screen opens;
///         the callback fires on the main thread with the host's IP.
/// </summary>
public class LanHostDiscovery : MonoBehaviour
{
    public static LanHostDiscovery Instance { get; private set; }

    private const int DiscoveryPort = 8001;
    private const string BeaconTag = "SUNMOON_HOST_V1";

    private UdpClient _broadcastSocket;
    private UdpClient _listenSocket;
    private Thread _thread;
    private volatile bool _stop;

    private Action<string> _pendingCallback;
    private volatile string _foundIp;
    private volatile bool _hasResult;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else if (Instance != this) { Destroy(gameObject); }
    }

    private void OnDestroy() => StopAll();
    private void OnApplicationQuit() => StopAll();

    // ---------------- HOST ----------------

    public void StartBroadcasting()
    {
        StopBroadcasting();
        _stop = false;
        _thread = new Thread(BroadcastLoop) { IsBackground = true };
        _thread.Start();
    }

    public void StopBroadcasting()
    {
        _stop = true;
        try { _broadcastSocket?.Close(); } catch { }
        _broadcastSocket = null;
    }

    private void BroadcastLoop()
    {
        try
        {
            _broadcastSocket = new UdpClient();
            _broadcastSocket.EnableBroadcast = true;
            var payload = Encoding.UTF8.GetBytes(BeaconTag);
            int tick = 0;
            while (!_stop)
            {
                var targets = GetBroadcastTargets();
                foreach (var ep in targets)
                {
                    try { _broadcastSocket.Send(payload, payload.Length, ep); } catch { }
                }
                if (tick++ % 5 == 0)
                    Debug.Log($"[LanHostDiscovery] broadcasting to {targets.Count} target(s): {string.Join(", ", targets.ConvertAll(e => e.Address.ToString()))}");
                Thread.Sleep(1000);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LanHostDiscovery] broadcast stopped: " + e.Message);
        }
    }

    // Global 255.255.255.255 broadcasts can leave on whatever interface the
    // OS treats as the default route - on a phone running a hotspot that's
    // often still the cellular/data interface, not the WiFi AP interface the
    // other player is actually on. Instead, compute the directed broadcast
    // address (e.g. 192.168.43.255) for every active IPv4 interface and send
    // to all of them, so we hit the hotspot subnet regardless of routing.
    private List<IPEndPoint> GetBroadcastTargets()
    {
        var targets = new List<IPEndPoint>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (ua.IPv4Mask == null) continue;

                    byte[] ipBytes = ua.Address.GetAddressBytes();
                    byte[] maskBytes = ua.IPv4Mask.GetAddressBytes();
                    byte[] broadcastBytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                        broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

                    targets.Add(new IPEndPoint(new IPAddress(broadcastBytes), DiscoveryPort));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LanHostDiscovery] interface enumeration failed: " + e.Message);
        }

        // Always include the global broadcast too, as a fallback.
        targets.Add(new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
        return targets;
    }

    // ---------------- JOINER ----------------

    public void StartListening(Action<string> onFound)
    {
        StopListening();
        _stop = false;
        _hasResult = false;
        _pendingCallback = onFound;
        _thread = new Thread(ListenLoop) { IsBackground = true };
        _thread.Start();
    }

    public void StopListening()
    {
        _stop = true;
        try { _listenSocket?.Close(); } catch { }
        _listenSocket = null;
        _pendingCallback = null;
    }

    private void ListenLoop()
    {
        try
        {
            _listenSocket = new UdpClient(DiscoveryPort);
            Debug.Log($"[LanHostDiscovery] listening for host beacon on UDP {DiscoveryPort}");
            var remote = new IPEndPoint(IPAddress.Any, DiscoveryPort);
            while (!_stop)
            {
                byte[] data;
                try { data = _listenSocket.Receive(ref remote); }
                catch { break; } // socket closed via StopListening()

                string text = Encoding.UTF8.GetString(data);
                Debug.Log($"[LanHostDiscovery] packet from {remote.Address}: \"{text}\"");
                if (text == BeaconTag)
                {
                    _foundIp = remote.Address.ToString();
                    _hasResult = true;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LanHostDiscovery] listen stopped: " + e.Message);
        }
    }

    // Dispatch the result back on the main thread, like the rest of Unity expects.
    private void Update()
    {
        if (_hasResult)
        {
            _hasResult = false;
            var cb = _pendingCallback;
            var ip = _foundIp;
            StopListening();
            cb?.Invoke(ip);
        }
    }

    private void StopAll()
    {
        StopBroadcasting();
        StopListening();
    }
}