using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{
    private readonly SyncVar<string> playerName = new("Player");
    private readonly SyncVar<ulong> steamId = new(0);

    public string PlayerName => playerName.Value;
    public ulong SteamId => steamId.Value;

    [Server]
    public void SetInfo(string name, ulong steam)
    {
        playerName.Value = string.IsNullOrEmpty(name) ? "Player_" + OwnerId : name;
        steamId.Value = steam;
    }

    public static PlayerInfo FindByClientId(int clientId)
    {
        PlayerInfo[] all = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None);
        foreach (var info in all)
        {
            if (info.OwnerId == clientId)
                return info;
        }
        return null;
    }
}