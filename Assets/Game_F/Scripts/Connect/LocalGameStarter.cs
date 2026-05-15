using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Managing.Scened;

public class LocalGameStarter : MonoBehaviour
{
    public static LocalGameStarter Instance { get; private set; }

    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private NetworkObject playerPrefab;

    private int spawnIndex = 0;

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

    public void StartHostGame()
    {
        SceneLoadData sld = new SceneLoadData(gameSceneName)
        {
            ReplaceScenes = ReplaceOption.All,
            Options = new LoadOptions
            {
                AllowStacking = false,
                LocalPhysics = UnityEngine.SceneManagement.LocalPhysicsMode.None
            }
        };
        InstanceFinder.SceneManager.OnLoadEnd += OnSceneLoaded;
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    private void OnSceneLoaded(SceneLoadEndEventArgs args)
    {
        foreach (var scene in args.LoadedScenes)
        {
            if (scene.name != gameSceneName) continue;

            InstanceFinder.SceneManager.OnLoadEnd -= OnSceneLoaded;

            SpawnPlayer(InstanceFinder.ClientManager.Connection);

            if (GameManager.Instance != null)
                GameManager.Instance.StartGame();
            else
                Debug.LogError("[LocalGameStarter] GameManager not found!");

            return;
        }
    }

    public void SpawnJoiningPlayer(NetworkConnection conn)
    {
        SpawnPlayer(conn);
    }

    private void SpawnPlayer(NetworkConnection conn)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[LocalGameStarter] playerPrefab is null!");
            return;
        }

        var points = SpawnPointsHolder.Instance?.SpawnPoints;

        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        if (points != null && points.Length > 0)
        {
            int idx = spawnIndex % points.Length;
            pos = points[idx].position;
            rot = points[idx].rotation;
        }

        spawnIndex++;

        NetworkObject player = Instantiate(playerPrefab, pos, rot);
        InstanceFinder.ServerManager.Spawn(player, conn);
        Debug.Log($"[LocalGameStarter] Spawned player for conn {conn.ClientId}");
    }
}