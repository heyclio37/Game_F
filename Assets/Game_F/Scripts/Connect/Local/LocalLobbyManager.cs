using System.Collections;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using UnityEngine;

public class LocalLobbyManager : MonoBehaviour
{
    public static LocalLobbyManager Instance { get; private set; }
    public bool IsHost { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void HostGame()
    {
        IsHost = true;
        StartCoroutine(HostRoutine());
    }

    private IEnumerator HostRoutine()
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm == null) { Debug.LogError("[LocalLobby] NetworkManager not found!"); yield break; }

        Tugboat tugboat = nm.TransportManager.GetTransport<Tugboat>();
        if (tugboat == null) { Debug.LogError("[LocalLobby] Tugboat not found!"); yield break; }

        tugboat.SetClientAddress("127.0.0.1");
        yield return null;

        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();

        // Ждём пока сервер реально станет активным
        float timeout = 5f;
        while (!InstanceFinder.IsServerStarted && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (!InstanceFinder.IsServerStarted)
        {
            Debug.LogError("[LocalLobby] Server failed to start within 5 seconds!");
            yield break;
        }

        InstanceFinder.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;

        LocalGameStarter starter = FindAnyObjectByType<LocalGameStarter>();
        if (starter != null)
            starter.StartHostGame();
        else
            Debug.LogError("[LocalLobby] LocalGameStarter not found!");
    }

    public void JoinGame(string ip)
    {
        IsHost = false;
        StartCoroutine(JoinRoutine(ip));
    }

    private IEnumerator JoinRoutine(string ip)
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm == null) { Debug.LogError("[LocalLobby] NetworkManager not found!"); yield break; }

        Tugboat tugboat = nm.TransportManager.GetTransport<Tugboat>();
        if (tugboat == null) { Debug.LogError("[LocalLobby] Tugboat not found!"); yield break; }

        tugboat.SetClientAddress(ip);
        yield return null;

        InstanceFinder.ClientManager.StartConnection();

        Debug.Log("[LocalLobby] Joining: " + ip);
    }

    private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer) return;
        if (conn == InstanceFinder.ClientManager.Connection) return;

        LocalGameStarter starter = FindAnyObjectByType<LocalGameStarter>();
        if (starter != null)
            starter.SpawnJoiningPlayer(conn);
        else
            Debug.LogError("[LocalLobby] LocalGameStarter not found!");
    }

    public void Leave()
    {
        if (InstanceFinder.SceneManager != null)
            InstanceFinder.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;

        if (InstanceFinder.NetworkManager == null)
        {
            IsHost = false;
            return;
        }

        if (IsHost)
        {
            InstanceFinder.ServerManager.StopConnection(true);
            InstanceFinder.ClientManager.StopConnection();
        }
        else
        {
            InstanceFinder.ClientManager.StopConnection();
        }

        IsHost = false;
    }
}