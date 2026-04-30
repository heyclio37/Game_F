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

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartGame();
        currentState.OnChange += OnStateChanged;
    }

    private void OnStateChanged(GameState oldValue, GameState newValue, bool asServer)
    {
        if (asServer)
            return; // клиенты уже получат значение, но мы дополнительно сообщим через RPC для явного уведомления
        if (newValue == GameState.GameWin)
            OnGameWinObserversRpc();
        else if (newValue == GameState.GameOver)
            OnGameOverObserversRpc();
    }

    [ObserversRpc]
    private void OnGameWinObserversRpc()
    {
        Debug.Log("[GameManager] Game Win!");
    }

    [ObserversRpc]
    private void OnGameOverObserversRpc()
    {
        Debug.Log("[GameManager] Game Over!");
    }

    [Server]
    public void StartGame()
    {
        if (currentState.Value != GameState.WaitingForPlayers) return;
        currentState.Value = GameState.Playing;
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

        PlayerCaptureState[] allPlayers = FindObjectsByType<PlayerCaptureState>(FindObjectsSortMode.None);
        bool anyPlaying = false;
        bool anyEscaped = false;
        bool allCaptured = true;

        foreach (var p in allPlayers)
        {
            if (!p.IsServerStarted) continue;
            if (!p.IsCaptured && !p.IsEscaped)
                anyPlaying = true;
            if (p.IsEscaped)
                anyEscaped = true;
            if (!p.IsCaptured)
                allCaptured = false;
        }

        if (!anyPlaying)
        {
            // Определяем исход для каждого игрока и отправляем RPC
            foreach (var p in allPlayers)
            {
                if (!p.IsServerStarted) continue;
                bool win = false;
                if (anyEscaped)
                {
                    win = p.IsEscaped;
                }
                else if (allCaptured)
                {
                    win = false;
                }

                p.ShowGameResult(win);
            }

            if (anyEscaped)
                currentState.Value = GameState.GameWin;
            else if (allCaptured)
                currentState.Value = GameState.GameOver;
        }
    }
}