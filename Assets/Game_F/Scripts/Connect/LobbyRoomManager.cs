using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

public class LobbyRoomManager : NetworkBehaviour
{
    public static LobbyRoomManager Instance { get; private set; }

    [Header("Scene to load")] [SerializeField]
    private string gameSceneName = "GameScene";

    [Header("Player Spawning")] [SerializeField]
    private NetworkObject playerPrefab;

    private readonly SyncDictionary<int, string> playerNames = new();
    private readonly SyncDictionary<int, bool> playerReady = new();
    private readonly HashSet<int> loadedPlayers = new();
    private readonly List<NetworkConnection> connectedClients = new();

    private readonly SyncVar<bool> gameInProgress = new(false);
    public bool IsGameInProgress => gameInProgress.Value;

    private bool gameStartTriggered = false;
    private bool gameStarted = false;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        playerNames.OnChange += OnPlayerListChanged;
        playerReady.OnChange += OnReadyStateChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        playerNames.OnChange -= OnPlayerListChanged;
        playerReady.OnChange -= OnReadyStateChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Защита от дубликата
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LobbyRoom] Duplicate LobbyRoomManager detected, destroying this");
            if (IsServerStarted)
                ServerManager.Despawn(NetworkObject);
            return;
        }

        Instance = this;

        playerNames.OnChange += OnPlayerListChanged;
        playerReady.OnChange += OnReadyStateChanged;

        LobbyUI.Instance?.EnableReadyButton();

        if (IsServerStarted)
            RegisterHostAsPlayer();

        Debug.Log("[LobbyRoom] OnStartClient");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Защита от дубликата на сервере
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LobbyRoom] Duplicate LobbyRoomManager on server, destroying this");
            ServerManager.Despawn(NetworkObject);
            return;
        }

        Instance = this;

        ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        PlayerNameRegistrar.OnNameReceived += OnNameReceivedFromClient;

        Debug.Log("[LobbyRoom] OnStartServer");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        PlayerNameRegistrar.OnNameReceived -= OnNameReceivedFromClient;
    }

    [Server]
    private void OnNameReceivedFromClient(int clientId, string name, ulong steamId)
    {
        if (!playerNames.ContainsKey(clientId)) return;
        if (string.IsNullOrEmpty(name)) return;

        playerNames[clientId] = name;
        Debug.Log($"[LobbyRoom] Updated name for client {clientId}: {name}");
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            if (gameStartTriggered || gameInProgress.Value)
            {
                Debug.LogWarning($"[LobbyRoom] Kicking client {conn.ClientId}: game already in progress");
                conn.Kick(KickReason.UnexpectedProblem);
                return;
            }

            connectedClients.Add(conn);

            string initialName = "Player_" + conn.ClientId;
            if (PlayerNameRegistrar.Instance != null &&
                PlayerNameRegistrar.Instance.TryGetPendingName(conn.ClientId, out string pn, out _) &&
                !string.IsNullOrEmpty(pn))
            {
                initialName = pn;
            }

            playerNames[conn.ClientId] = initialName;
            playerReady[conn.ClientId] = false;

            Debug.Log($"[LobbyRoom] Player {conn.ClientId} joined as '{initialName}'. Total: {connectedClients.Count}");
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            connectedClients.Remove(conn);
            playerNames.Remove(conn.ClientId);
            playerReady.Remove(conn.ClientId);
            loadedPlayers.Remove(conn.ClientId);
            Debug.Log($"[LobbyRoom] Player {conn.ClientId} left. Total: {connectedClients.Count}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(NetworkConnection conn = null)
    {
        if (conn == null) return;
        if (gameStartTriggered) return;

        int id = conn.ClientId;
        if (!playerReady.ContainsKey(id)) return;

        playerReady[id] = !playerReady[id];
        CheckAllReady();
    }

    private void CheckAllReady()
    {
        if (playerReady.Count < 1) return;

        foreach (var kv in playerReady)
            if (!kv.Value) return;

        LoadGameScene();
    }
    
    public static void Cleanup()
    {
        if (Instance == null) return;

        if (Instance.IsServerStarted)
            Instance.ServerManager.Despawn(Instance.NetworkObject);
        else
            Destroy(Instance.gameObject);

        Instance = null;
    }

    [Server]
    private void LoadGameScene()
    {
        if (gameStartTriggered) return;
        gameStartTriggered = true;

        NotifyLoadingObserversRpc();

        SceneLoadData sld = new SceneLoadData(gameSceneName)
        {
            ReplaceScenes = ReplaceOption.All,
            Options = new LoadOptions
            {
                AllowStacking = false,
                LocalPhysics = UnityEngine.SceneManagement.LocalPhysicsMode.None
            }
        };

        loadedPlayers.Clear();
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);

        Debug.Log($"[LobbyRoom] Loading game scene for {connectedClients.Count} players");
    }


    [ServerRpc(RequireOwnership = false)]
    public void NotifySceneLoadedServerRpc(NetworkConnection conn = null)
    {
        if (conn == null) return;
        if (!gameStartTriggered) return;
        if (gameStarted) return;
        if (loadedPlayers.Contains(conn.ClientId)) return;

        loadedPlayers.Add(conn.ClientId);
        Debug.Log($"[LobbyRoom] Client {conn.ClientId} reported scene loaded. Total: {loadedPlayers.Count}/{connectedClients.Count}");

        TryStartGame();
    }

    [Server]
    private void TryStartGame()
    {
        if (gameStarted) return;
        if (loadedPlayers.Count < connectedClients.Count) return;
        if (connectedClients.Count == 0) return;

        gameStarted = true;
        Invoke(nameof(SignalGameStart), 0.5f);
    }

    [Server]
    private void SignalGameStart()
    {
        Debug.Log($"[LobbyRoom] SignalGameStart called. connectedClients.Count={connectedClients.Count}");

        gameInProgress.Value = true;

        for (int i = 0; i < connectedClients.Count; i++)
            SpawnPlayer(connectedClients[i], i);

        if (GameManager.Instance == null)
        {
            Debug.LogError("[LobbyRoom] GameManager not found!");
            return;
        }

        GameManager.Instance.StartGame();
        Debug.Log("[LobbyRoom] Game started!");
    }

    [Server]
    private void SpawnPlayer(NetworkConnection conn, int spawnIndex)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[LobbyRoom] playerPrefab is NULL!");
            return;
        }

        var points = SpawnPointsHolder.Instance?.SpawnPoints;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (points != null && points.Length > 0)
        {
            int idx = spawnIndex % points.Length;
            spawnPos = points[idx].position;
            spawnRot = points[idx].rotation;
        }

        NetworkObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        ServerManager.Spawn(player, conn);

        PlayerNameRegistrar.Instance?.TryApply(conn.ClientId);

        Debug.Log($"[LobbyRoom] Spawned player for client {conn.ClientId}");
    }

    [ObserversRpc]
    private void NotifyLoadingObserversRpc()
    {
        LobbyUI.Instance?.ShowLoadingScreen();
    }

    public void RefreshPlayerList()
    {
        LobbyUI.Instance?.UpdatePlayerList(playerNames, playerReady);
    }

    private void OnPlayerListChanged(SyncDictionaryOperation op, int key, string value, bool asServer)
    {
        LobbyUI.Instance?.UpdatePlayerList(playerNames, playerReady);
    }

    private void OnReadyStateChanged(SyncDictionaryOperation op, int key, bool value, bool asServer)
    {
        LobbyUI.Instance?.UpdatePlayerList(playerNames, playerReady);
    }

    [Server]
    private void RegisterHostAsPlayer()
    {
        NetworkConnection hostConn = LocalConnection;
        if (hostConn == null) return;
        if (connectedClients.Contains(hostConn)) return;

        connectedClients.Add(hostConn);

        string hostName = PlayerNameProvider.GetLocalName();
        if (string.IsNullOrEmpty(hostName))
            hostName = "Player_" + hostConn.ClientId;

        playerNames[hostConn.ClientId] = hostName;
        playerReady[hostConn.ClientId] = false;

        Debug.Log($"[LobbyRoom] Host registered as '{hostName}'");
    }

    public IReadOnlyDictionary<int, string> PlayerNames => playerNames;
    public IReadOnlyDictionary<int, bool> PlayerReady => playerReady;
}