using FishNet;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;


public class LobbyRoomSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject lobbyRoomManagerPrefab;

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
        Debug.Log($"[LobbyRoomSpawner] Server state: {args.ConnectionState}");

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            if (lobbyRoomManagerPrefab == null)
            {
                Debug.LogError("[LobbyRoomSpawner] prefab is NULL! Assign it in Inspector!");
                return;
            }

            Debug.Log("[LobbyRoomSpawner] Spawning LobbyRoomManager...");
            NetworkObject obj = Instantiate(lobbyRoomManagerPrefab);
            InstanceFinder.ServerManager.Spawn(obj);
            Debug.Log("[LobbyRoomSpawner] Done!");
        }
    }
}