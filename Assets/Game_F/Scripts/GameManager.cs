using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public enum GameState
    {
        WaitingForPlayers,
        Playing,
        GameWin,
        GameOver
    }

    public static GameManager Instance { get; private set; }

    [SerializeField] private Transform prisonSpawnPoint;

    private readonly SyncVar<GameState> currentState = new(GameState.WaitingForPlayers);
    private bool resultsSent;

    public GameState CurrentState => currentState.Value;
    public Transform PrisonSpawnPoint => prisonSpawnPoint;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [Server]
    public void StartGame()
    {
        if (currentState.Value != GameState.WaitingForPlayers) return;
        currentState.Value = GameState.Playing;
        resultsSent = false;
        Debug.Log("[GameManager] Game started.");
    }

    [Server]
    public void OnPlayerCaught(PlayerCaptureState player)
    {
        if (currentState.Value != GameState.Playing) return;
        if (player.IsCaptured) return;

        player.Capture(prisonSpawnPoint.position);
        CheckGameEnd();
    }

    [Server]
    public void FreeAllPrisoners()
    {
        PlayerCaptureState[] allPlayers = FindObjectsByType<PlayerCaptureState>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p.IsCaptured)
                p.Release();
        }

        CheckGameEnd();
    }

    [Server]
    public void CheckGameEnd()
    {
        if (currentState.Value != GameState.Playing) return;
        if (resultsSent) return;

        PlayerCaptureState[] allPlayers = FindObjectsByType<PlayerCaptureState>(FindObjectsSortMode.None);

        bool anyPlaying = false;
        bool anyEscaped = false;
        bool allCaptured = true;

        foreach (var p in allPlayers)
        {
            if (!p.IsServerStarted) continue;
            if (!p.IsCaptured && !p.IsEscaped) anyPlaying = true;
            if (p.IsEscaped) anyEscaped = true;
            if (!p.IsCaptured) allCaptured = false;
        }

        if (anyPlaying) return;

        resultsSent = true;

        List<GameResultEntry> results = new();
        foreach (var p in allPlayers)
        {
            if (!p.IsServerStarted) continue;

            PlayerInfo info = p.GetComponent<PlayerInfo>();
            results.Add(new GameResultEntry
            {
                PlayerName = info != null && !string.IsNullOrEmpty(info.PlayerName)
                    ? info.PlayerName
                    : "Player_" + p.OwnerId,
                ClientId = p.OwnerId,
                Escaped = p.IsEscaped,
                Captured = p.IsCaptured
            });
        }

        currentState.Value = anyEscaped ? GameState.GameWin : GameState.GameOver;

        ShowResultsObserversRpc(results.ToArray());
    }

    [ObserversRpc(BufferLast = true)]
    private void ShowResultsObserversRpc(GameResultEntry[] results)
    {
        int localClientId = InstanceFinder.ClientManager.Connection.ClientId;
        GameResultUI.Instance?.Show(results, localClientId);
    }
}