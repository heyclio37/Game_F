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

        // Фикс: хост не попадает в OnRemoteConnectionState
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

        Debug.Log("[LobbyRoom] OnStartServer");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        InstanceFinder.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            connectedClients.Add(conn);
            playerNames[conn.ClientId] = "Player_" + conn.ClientId;
            playerReady[conn.ClientId] = false;
            Debug.Log($"[LobbyRoom] Player {conn.ClientId} joined. Total: {connectedClients.Count}");
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
        Debug.Log($"[LobbyRoom] ToggleReadyServerRpc called, conn: {conn?.ClientId}");

        if (conn == null)
        {
            Debug.LogError("[LobbyRoom] conn is null!");
            return;
        }

        int id = conn.ClientId;
        if (!playerReady.ContainsKey(id))
        {
            Debug.LogError($"[LobbyRoom] Player {id} not in playerReady dict!");
            return;
        }

        playerReady[id] = !playerReady[id];
        Debug.Log($"[LobbyRoom] Player {id} ready: {playerReady[id]}");
        CheckAllReady();
    }

    private void CheckAllReady()
    {
        Debug.Log($"[LobbyRoom] CheckAllReady: {playerReady.Count} players");


        if (playerReady.Count < 1)
        {
            Debug.Log($"[LobbyRoom] Not enough players: {playerReady.Count}");
            return;
        }

        foreach (var kv in playerReady)
        {
            Debug.Log($"[LobbyRoom] Player {kv.Key} ready: {kv.Value}");
            if (!kv.Value) return;
        }

        Debug.Log("[LobbyRoom] All ready! Loading scene...");
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
        Debug.Log("[LobbyRoom] Loading game scene: " + gameSceneName);


        StartCoroutine(WaitForSceneAndSpawn());
    }

    private System.Collections.IEnumerator WaitForSceneAndSpawn()
    {
        Debug.Log("[LobbyRoom] Waiting for GameScene to load...");


        while (true)
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneByName(gameSceneName);
            if (scene.isLoaded)
            {
                Debug.Log("[LobbyRoom] GameScene loaded! Waiting extra frame...");
                break;
            }

            yield return null;
        }


        yield return new WaitForSeconds(1f);

        Debug.Log("[LobbyRoom] Calling SignalGameStart");
        SignalGameStart();
    }

    private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer) return;
        if (!gameStartTriggered) return;

        loadedPlayers.Add(conn.ClientId);
        Debug.Log($"[LobbyRoom] Client {conn.ClientId} loaded scene. " +
                  $"{loadedPlayers.Count}/{connectedClients.Count} ready");

        if (loadedPlayers.Count >= connectedClients.Count && connectedClients.Count > 0)
        {
            loadedPlayers.Clear();
            Invoke(nameof(SignalGameStart), 0.5f);
        }
    }


    [Server]
    private void SignalGameStart()
    {
        Debug.Log($"[LobbyRoom] SignalGameStart called. Clients: {connectedClients.Count}");

        for (int i = 0; i < connectedClients.Count; i++)
        {
            Debug.Log($"[LobbyRoom] Spawning player for client {connectedClients[i].ClientId}");
            SpawnPlayer(connectedClients[i], i);
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[LobbyRoom] GameManager not found!");
            return;
        }

        GameManager.Instance.StartGame();
    }

    [Server]
    private void SpawnPlayer(NetworkConnection conn, int spawnIndex)
    {
        Debug.Log($"[LobbyRoom] SpawnPlayer called for conn {conn.ClientId}");

        if (playerPrefab == null)
        {
            Debug.LogError("[LobbyRoom] playerPrefab is NULL! Assign it in Inspector on LobbyRoomManagerPrefab!");
            return;
        }

        var points = SpawnPointsHolder.Instance?.SpawnPoints;
        Debug.Log($"[LobbyRoom] SpawnPointsHolder.Instance is null: {SpawnPointsHolder.Instance == null}");

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (points != null && points.Length > 0)
        {
            int idx = spawnIndex % points.Length;
            spawnPos = points[idx].position;
            spawnRot = points[idx].rotation;
        }

        Debug.Log($"[LobbyRoom] Instantiating player at {spawnPos}");
        NetworkObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        ServerManager.Spawn(player, conn);
        Debug.Log($"[LobbyRoom] Player spawned for conn {conn.ClientId}");
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
        if (LobbyUI.Instance == null) return;
        LobbyUI.Instance?.UpdatePlayerList(playerNames, playerReady);
    }

    private void OnReadyStateChanged(SyncDictionaryOperation op, int key, bool value, bool asServer)
    {
        if (LobbyUI.Instance == null) return;
        LobbyUI.Instance?.UpdatePlayerList(playerNames, playerReady);
    }

    [Server]
    private void RegisterHostAsPlayer()
    {
        NetworkConnection hostConn = LocalConnection;
        if (hostConn == null) return;
        if (connectedClients.Contains(hostConn)) return;

        connectedClients.Add(hostConn);
        playerNames[hostConn.ClientId] = "Player_" + hostConn.ClientId;
        playerReady[hostConn.ClientId] = false;

        Debug.Log($"[LobbyRoom] Host registered: {hostConn.ClientId}");
    }

    public IReadOnlyDictionary<int, string> PlayerNames => playerNames;
    public IReadOnlyDictionary<int, bool> PlayerReady => playerReady;
}