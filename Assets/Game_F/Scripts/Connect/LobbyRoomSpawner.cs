using FishNet;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class LobbyRoomSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject lobbyRoomManagerPrefab;

    private bool hasSpawned = false;

    private void Start()
    {
        InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState != LocalConnectionState.Started) return;

        if (hasSpawned)
        {
            Debug.LogWarning("[LobbyRoomSpawner] Already spawned, skipping");
            return;
        }

        if (LobbyRoomManager.Instance != null)
        {
            Debug.LogWarning("[LobbyRoomSpawner] LobbyRoomManager.Instance already exists, skipping spawn");
            hasSpawned = true;
            return;
        }

        NetworkObject obj = Instantiate(lobbyRoomManagerPrefab);
        InstanceFinder.ServerManager.Spawn(obj);
        hasSpawned = true;

        Debug.Log("[LobbyRoomSpawner] LobbyRoomManager spawned");
    }
}