using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
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

    private bool gameStartTriggered = false;

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
        Instance = this;

        ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        InstanceFinder.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;

        PlayerNameRegistrar.OnNameReceived += OnNameReceivedFromClient;

        Debug.Log("[LobbyRoom] OnStartServer");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;

        if (InstanceFinder.SceneManager != null)
            InstanceFinder.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;

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

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
        StartCoroutine(WaitForSceneAndSpawn());
    }

    private System.Collections.IEnumerator WaitForSceneAndSpawn()
    {
        while (true)
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneByName(gameSceneName);
            if (scene.isLoaded) break;
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        SignalGameStart();
    }

    private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer) return;
        if (!gameStartTriggered) return;

        loadedPlayers.Add(conn.ClientId);

        if (loadedPlayers.Count >= connectedClients.Count && connectedClients.Count > 0)
        {
            loadedPlayers.Clear();
            Invoke(nameof(SignalGameStart), 0.5f);
        }
    }

    [Server]
    private void SignalGameStart()
    {
        for (int i = 0; i < connectedClients.Count; i++)
            SpawnPlayer(connectedClients[i], i);

        if (GameManager.Instance == null) return;
        GameManager.Instance.StartGame();
    }

    [Server]
    private void SpawnPlayer(NetworkConnection conn, int spawnIndex)
    {
        if (playerPrefab == null) return;

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