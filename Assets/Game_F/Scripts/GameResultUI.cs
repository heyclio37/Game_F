using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultUI : MonoBehaviour
{
    public static GameResultUI Instance { get; private set; }

    [Header("Panel")] [SerializeField] private GameObject panel;

    [Header("Header")] [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Player List")] [SerializeField]
    private Transform listContainer;

    [SerializeField] private GameObject rowPrefab;

    [Header("Buttons")] [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button quitButton;

    public bool IsShown => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(OnReturnToMenu);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }

    public void Show(GameResultEntry[] results, int localClientId)
    {
        if (panel == null) return;
        panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        DeactivateLocalSpectator();

        bool localWon = false;
        foreach (var r in results)
        {
            if (r.ClientId == localClientId)
            {
                localWon = r.Escaped;
                break;
            }
        }

        if (titleText != null)
            titleText.text = localWon ? "WIN" : "LOSE";

        if (subtitleText != null)
            subtitleText.text = localWon
                ? "You run away"
                : "You've been caught";

        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        foreach (var r in results)
        {
            GameObject row = Instantiate(rowPrefab, listContainer);
            GameResultRow rowComp = row.GetComponent<GameResultRow>();
            if (rowComp != null)
                rowComp.Setup(r, r.ClientId == localClientId);
        }
    }
    
    private void DeactivateLocalSpectator()
    {
        SpectatorView[] all = FindObjectsByType<SpectatorView>(FindObjectsSortMode.None);
        foreach (var s in all)
        {
            if (s.IsActive)
            {
                s.Deactivate();
            }
        }
    }

    private void OnReturnToMenu()
    {
        returnToMenuButton.interactable = false;

        if (LobbyRoomManager.Instance != null)
        {
            if (LobbyRoomManager.Instance.IsServerStarted)
                LobbyRoomManager.Instance.ServerManager.Despawn(LobbyRoomManager.Instance.NetworkObject);
            LobbyRoomManager.Cleanup();
        }

        if (GameConnectionManager.Instance != null &&
            GameConnectionManager.Instance.Mode == ConnectionMode.Steam)
        {
            if (SteamLobbyManager.Instance != null)
                SteamLobbyManager.Instance.LeaveLobby();
        }
        else
        {
            if (LocalLobbyManager.Instance != null)
                LocalLobbyManager.Instance.Leave();
        }

        if (InstanceFinder.NetworkManager != null)
            Destroy(InstanceFinder.NetworkManager.gameObject);

        SceneManager.LoadScene("ModeSelectScene");
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}