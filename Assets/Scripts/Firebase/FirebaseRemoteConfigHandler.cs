using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.RemoteConfig;
using UnityEngine;

/// <summary>
/// Drop-in Firebase Remote Config handler.
/// - Initializes Firebase (dependencies) if needed
/// - Sets defaults (optional)
/// - Fetches + Activates with configurable intervals/timeouts
/// - Exposes typed getters + change event
///
/// Usage:
///   await remoteConfig.InitializeAndFetchAsync(new Dictionary<string, object> { { "welcome_text", "Hi" } });
///   var txt = remoteConfig.GetString("welcome_text");
/// </summary>
public class FirebaseRemoteConfigHandler : MonoBehaviour
{
    public static FirebaseRemoteConfigHandler Instance { get; private set; }
    [Header("Fetch Settings")]
    [Tooltip("Minimum time between fetches (seconds). Use 0 for dev/testing.")]
    [SerializeField] private long minimumFetchIntervalSeconds = 3600;

    [Tooltip("Timeout for fetch calls (seconds).")]
    [SerializeField] private long fetchTimeoutSeconds = 10;

    public bool IsReady { get; private set; }
    public bool LastFetchSucceeded { get; private set; }
    public DateTime LastFetchTimeUtc { get; private set; } = DateTime.MinValue;

    /// <summary> Fires after Fetch+Activate completes (success or fail). </summary>
    public event Action OnFetchCompleted;

    /// <summary> Fires when Remote Config values are updated by Firebase. </summary>
    public event Action OnConfigUpdated;

    private FirebaseRemoteConfig _rc;

    #region Firebase Essentials
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of FirebaseRemoteConfigHandler detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _rc = FirebaseRemoteConfig.DefaultInstance;
        _rc.OnConfigUpdateListener += OnConfigUpdateListener;
    }

    private void Start()
    {
        InitAndFetch();
    }

    private void OnDestroy()
    {
        if (_rc != null)
            _rc.OnConfigUpdateListener -= OnConfigUpdateListener;
    }

    /// <summary>
    /// Initializes Firebase dependencies (if needed), applies config settings, sets defaults, then Fetch+Activate.
    /// </summary>
    public async Task InitializeAndFetchAsync(Dictionary<string, object> defaults = null)
    {
        await EnsureFirebaseInitializedAsync();
        // ApplySettings();

        if (defaults != null && defaults.Count > 0)
            await _rc.SetDefaultsAsync(defaults);

        await FetchAndActivateAsync();
    }

    /// <summary>
    /// Fetches and activates Remote Config values.
    /// </summary>
    public async Task FetchAndActivateAsync()
    {
        try
        {
            var ok = await _rc.FetchAndActivateAsync();
            LastFetchSucceeded = ok;
            LastFetchTimeUtc = DateTime.UtcNow;
            IsReady = true;

            Debug.Log($"✅ Remote Config Fetch+Activate: {ok} | Source: {_rc.Info.LastFetchStatus} | Time: {LastFetchTimeUtc:u}");
        }
        catch (Exception e)
        {
            LastFetchSucceeded = false;
            IsReady = false;
            Debug.LogError($"❌ Remote Config Fetch+Activate failed: {e}");
        }
        finally
        {
            SetValues();
            OnFetchCompleted?.Invoke();
        }
    }

    /// <summary>
    /// Optional: Force a fetch by temporarily setting min interval to 0 (useful for dev builds).
    /// </summary>
    public async Task ForceFetchNowAsync()
    {
        var old = minimumFetchIntervalSeconds;
        minimumFetchIntervalSeconds = 0;
        // ApplySettings();
        await FetchAndActivateAsync();
        minimumFetchIntervalSeconds = old;
        // ApplySettings();
    }

    // ---------------- Typed Getters ----------------

    public string GetString(string key, string fallback = "")
    {
        if (string.IsNullOrEmpty(key)) return fallback;
        try
        {
            var v = _rc.GetValue(key);
            var s = v.StringValue;
            return string.IsNullOrEmpty(s) ? fallback : s;
        }
        catch { return fallback; }
    }

    public bool GetBool(string key, bool fallback = false)
    {
        if (string.IsNullOrEmpty(key)) return fallback;
        try { return _rc.GetValue(key).BooleanValue; }
        catch { return fallback; }
    }

    public long GetLong(string key, long fallback = 0)
    {
        if (string.IsNullOrEmpty(key)) return fallback;
        try { return _rc.GetValue(key).LongValue; }
        catch { return fallback; }
    }

    public int GetInt(string key, int fallback = 0)
    {
        var v = GetLong(key, fallback);
        if (v > int.MaxValue) return int.MaxValue;
        if (v < int.MinValue) return int.MinValue;
        return (int)v;
    }

    public double GetDouble(string key, double fallback = 0)
    {
        if (string.IsNullOrEmpty(key)) return fallback;
        try { return _rc.GetValue(key).DoubleValue; }
        catch { return fallback; }
    }

    // ---------------- Internals ----------------

    private async Task EnsureFirebaseInitializedAsync()
    {
        // Safe to call multiple times
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
            throw new Exception($"Firebase dependencies not available: {status}");
    }

    // private void ApplySettings()
    // {
    //     _rc.ConfigSettings = new ConfigSettings
    //     {
    //         MinimumFetchIntervalInMilliseconds = minimumFetchIntervalSeconds * 1000,
    //         FetchTimeoutInMilliseconds = fetchTimeoutSeconds * 1000
    //     };
    // }

    private void OnConfigUpdateListener(object sender, ConfigUpdateEventArgs args)
    {
        if (args.Error != RemoteConfigError.None)
        {
            Debug.LogWarning($"Remote Config update event error: {args.Error}");
            return;
        }

        // Values are updated on Firebase's side; you still need to Activate to apply locally.
        _ = ActivateOnUpdateAsync();
    }

    private async Task ActivateOnUpdateAsync()
    {
        try
        {
            await _rc.ActivateAsync();
            Debug.Log("✅ Remote Config activated after update event.");
            OnConfigUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Remote Config ActivateAsync failed after update event: {e}");
        }
    }

    #endregion Firebase Essentials

    #region Project Vars

    private async void InitAndFetch()
    {
        // Implementation for fetching and activating remote config
        var defaults = new Dictionary<string, object>
        {
            { "BASE_URL", FirebaseRemoteConfigConstants.BASE_URL }
        };

        await InitializeAndFetchAsync(defaults);
    }

    private void SetValues()
    {
        Debug.Log("Setting Remote Config values to FirebaseRemoteConfigConstants...");

        FirebaseRemoteConfigConstants.BASE_URL = GetString("BASE_URL", FirebaseRemoteConfigConstants.BASE_URL);

#if UNITY_EDITOR
        FirebaseRemoteConfigConstants.BASE_URL = "10.26.128.152:8000";
#endif

        #endregion Project Vars

    }
}
