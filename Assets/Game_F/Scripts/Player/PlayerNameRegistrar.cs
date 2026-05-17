using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using System;
using System.Collections.Generic;
using UnityEngine;

public struct RegisterNameBroadcast : IBroadcast
{
    public string Name;
    public ulong SteamId;
}

public class PlayerNameRegistrar : MonoBehaviour
{
    public static PlayerNameRegistrar Instance { get; private set; }
    
    public static event Action<int, string, ulong> OnNameReceived;

    private readonly Dictionary<int, RegisterNameBroadcast> pending = new();
    private NetworkManager subscribedNm;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm == subscribedNm) return;

        Unsubscribe();
        Subscribe(nm);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe(NetworkManager nm)
    {
        if (nm == null) return;

        subscribedNm = nm;
        nm.ClientManager.OnClientConnectionState += OnClientConn;
        nm.ServerManager.OnServerConnectionState += OnServerConn;

        Debug.Log("[PlayerNameRegistrar] Subscribed to new NetworkManager");
    }

    private void Unsubscribe()
    {
        if (subscribedNm == null) return;

        if (subscribedNm != null)
        {
            if (subscribedNm.ClientManager != null)
                subscribedNm.ClientManager.OnClientConnectionState -= OnClientConn;
            if (subscribedNm.ServerManager != null)
                subscribedNm.ServerManager.OnServerConnectionState -= OnServerConn;
        }

        subscribedNm = null;
        pending.Clear();
    }

    private void OnClientConn(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState != LocalConnectionState.Started) return;

        var msg = new RegisterNameBroadcast
        {
            Name = PlayerNameProvider.GetLocalName(),
            SteamId = PlayerNameProvider.GetLocalSteamId()
        };
        InstanceFinder.ClientManager.Broadcast(msg);

        Debug.Log($"[PlayerNameRegistrar] Sent name to server: {msg.Name}");
    }

    private void OnServerConn(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            InstanceFinder.ServerManager.RegisterBroadcast<RegisterNameBroadcast>(OnReceived);
            Debug.Log("[PlayerNameRegistrar] Server registered broadcast handler");
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            pending.Clear();
        }
    }

    private void OnReceived(NetworkConnection conn, RegisterNameBroadcast msg, Channel ch)
    {
        pending[conn.ClientId] = msg;
        Debug.Log($"[PlayerNameRegistrar] Received name from client {conn.ClientId}: {msg.Name}");
        
        OnNameReceived?.Invoke(conn.ClientId, msg.Name, msg.SteamId);

        TryApply(conn.ClientId);
    }

    public void TryApply(int clientId)
    {
        if (!pending.TryGetValue(clientId, out var msg)) return;

        PlayerInfo info = PlayerInfo.FindByClientId(clientId);
        if (info == null) return;

        info.SetInfo(msg.Name, msg.SteamId);
        pending.Remove(clientId);
        Debug.Log($"[PlayerNameRegistrar] Applied name '{msg.Name}' to client {clientId}");
    }
    
    public bool TryGetPendingName(int clientId, out string name, out ulong steamId)
    {
        if (pending.TryGetValue(clientId, out var msg))
        {
            name = msg.Name;
            steamId = msg.SteamId;
            return true;
        }
        name = null;
        steamId = 0;
        return false;
    }
}