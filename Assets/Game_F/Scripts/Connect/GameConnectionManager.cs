using UnityEngine;

public class GameConnectionManager : MonoBehaviour
{
    public static GameConnectionManager Instance { get; private set; }
    public ConnectionMode Mode { get; private set; } = ConnectionMode.Steam;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetMode(ConnectionMode mode)
    {
        Mode = mode;
    }
}