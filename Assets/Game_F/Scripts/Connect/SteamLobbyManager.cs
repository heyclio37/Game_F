using FishNet;
using FishNet.Managing;
using FishNet.Transporting.Multipass;
using Steamworks;
using UnityEngine;


public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    [Header("Lobby Settings")] [SerializeField]
    private int maxPlayers = 4;

    public CSteamID CurrentLobbyId { get; private set; }
    public bool IsHost { get; private set; }


    private const int TUGBOAT_INDEX = 0;
    private const int FISHY_INDEX = 1;


    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<GameLobbyJoinRequested_t> joinRequestedCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    private Callback<LobbyMatchList_t> lobbyListCallback;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!SteamManager.Initialized)
        {
            Debug.LogError("[Steam] Steamworks not initialization! Launch steam and heck steam_appid.txt");
            enabled = false;
            return;
        }

        Debug.Log("[Steam] Ready, user: " + SteamFriends.GetPersonaName());
    }

    private void OnEnable()
    {
        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        joinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        lobbyListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyListReceived);
    }

    private void OnDisable()
    {
        lobbyCreatedCallback?.Dispose();
        joinRequestedCallback?.Dispose();
        lobbyEnteredCallback?.Dispose();
        lobbyChatUpdateCallback?.Dispose();
        lobbyListCallback?.Dispose();
    }


    public void CreateLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers);
    }

    public void FindAndJoinLobby()
    {
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "GameName", "MyGame", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
        Debug.Log("[SteamLobby] Searching for lobbies...");
    }

    public void JoinLobbyById(string lobbyIdStr)
    {
        if (!ulong.TryParse(lobbyIdStr, out ulong id))
        {
            Debug.LogWarning("[SteamLobby] Invalid lobby ID: " + lobbyIdStr);
            return;
        }

        JoinLobby(new CSteamID(id));
    }

    public void JoinLobby(CSteamID lobbyId)
    {
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    public void LeaveLobby()
    {
        if (CurrentLobbyId.IsValid())
            SteamMatchmaking.LeaveLobby(CurrentLobbyId);

        if (InstanceFinder.NetworkManager != null)
        {

            Multipass multipass = InstanceFinder.NetworkManager
                .TransportManager.GetTransport<Multipass>();
            if (multipass != null)
                multipass.SetClientTransport(FISHY_INDEX);

            if (IsHost)
            {
                InstanceFinder.ServerManager.StopConnection(true);
                InstanceFinder.ClientManager.StopConnection();
            }
            else
            {

                if (InstanceFinder.IsClientStarted)
                    InstanceFinder.ClientManager.StopConnection();
            }
        }

        CurrentLobbyId = CSteamID.Nil;
        IsHost = false;
    }


    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("[SteamLobby] Failed to create lobby: " + callback.m_eResult);
            return;
        }

        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        IsHost = true;

        SteamMatchmaking.SetLobbyData(CurrentLobbyId, "HostSteamId",
            SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(CurrentLobbyId, "GameName", "MyGame");


        Multipass multipass = InstanceFinder.NetworkManager
            .TransportManager.GetTransport<Multipass>();

        if (multipass != null)
        {
            multipass.SetClientTransport(TUGBOAT_INDEX);
            Debug.Log("[SteamLobby] Host using Tugboat transport");
        }
        else
        {
            Debug.LogError("[SteamLobby] Multipass transport not found!");
            return;
        }

        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();

        LobbyUI.Instance?.ShowLobbyRoom();
        LobbyUI.Instance?.SetLobbyIdText(CurrentLobbyId.m_SteamID.ToString());

        Debug.Log("[SteamLobby] Lobby created: " + CurrentLobbyId.m_SteamID);
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (IsHost) return;

        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        string hostIdStr = SteamMatchmaking.GetLobbyData(CurrentLobbyId, "HostSteamId");
        if (!ulong.TryParse(hostIdStr, out ulong hostId))
        {
            Debug.LogError("[SteamLobby] Cannot parse HostSteamId from lobby data");
            return;
        }

        NetworkManager nm = InstanceFinder.NetworkManager;


        Multipass multipass = nm.TransportManager.GetTransport<Multipass>();
        if (multipass == null)
        {
            Debug.LogError("[SteamLobby] Multipass not found!");
            return;
        }

        multipass.SetClientTransport(FISHY_INDEX);
        Debug.Log("[SteamLobby] Client using FishySteamworks transport");


        FishySteamworks.FishySteamworks fishyTransport =
            nm.TransportManager.GetTransport<FishySteamworks.FishySteamworks>();

        if (fishyTransport == null)
        {
            Debug.LogError("[SteamLobby] FishySteamworks transport not found!");
            return;
        }

        fishyTransport.SetClientAddress(hostId.ToString());
        Debug.Log("[SteamLobby] Set Steam P2P address: " + hostId);

        InstanceFinder.ClientManager.StartConnection();

        LobbyUI.Instance?.ShowLobbyRoom();
        LobbyUI.Instance?.SetLobbyIdText(CurrentLobbyId.m_SteamID.ToString());

        Debug.Log("[SteamLobby] Joined lobby, connecting to host: " + hostIdStr);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        LobbyRoomManager.Instance?.RefreshPlayerList();
    }

    private void OnLobbyListReceived(LobbyMatchList_t callback)
    {
        if (callback.m_nLobbiesMatching == 0)
        {
            Debug.Log("[SteamLobby] No lobbies found");
            LobbyUI.Instance?.ShowNoLobbiesMessage();
            return;
        }

        CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(0);
        Debug.Log("[SteamLobby] Found lobby: " + lobbyId.m_SteamID + ", joining...");
        JoinLobby(lobbyId);
    }
}