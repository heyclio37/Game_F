using System.Collections.Generic;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }

    [Header("Main Menu Panel")] [SerializeField]
    private GameObject mainMenuPanel;

    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button findLobbyButton;
    [SerializeField] private TMP_InputField joinIdInputField;
    [SerializeField] private Button joinByIdButton;
    [SerializeField] private TMP_Text noLobbiesText;

    [Header("Lobby Room Panel")] [SerializeField]
    private GameObject lobbyRoomPanel;

    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private TMP_Text lobbyIdText;

    [Header("Loading Panel")] [SerializeField]
    private GameObject loadingPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateLobby);
        findLobbyButton.onClick.AddListener(OnFindLobby);
        joinByIdButton.onClick.AddListener(OnJoinById);
        readyButton.onClick.AddListener(OnToggleReady);
        leaveLobbyButton.onClick.AddListener(OnLeaveLobby);

        if (noLobbiesText != null)
            noLobbiesText.gameObject.SetActive(false);

        ShowMainMenu();
    }


    private void OnCreateLobby()
    {
        SteamLobbyManager.Instance.CreateLobby();
    }

    private void OnFindLobby()
    {
        if (noLobbiesText != null)
            noLobbiesText.gameObject.SetActive(false);

        SteamLobbyManager.Instance.FindAndJoinLobby();
    }

    private void OnJoinById()
    {
        string input = joinIdInputField != null ? joinIdInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(input))
        {
            Debug.LogWarning("[LobbyUI] Lobby ID field is empty");
            return;
        }

        SteamLobbyManager.Instance.JoinLobbyById(input);
    }

    private void OnToggleReady()
    {

        if (!InstanceFinder.IsClientStarted)
        {
            Debug.LogWarning("[LobbyUI] Client not started yet");
            return;
        }


        if (LobbyRoomManager.Instance == null)
        {
            Debug.LogWarning("[LobbyUI] LobbyRoomManager.Instance is null");
            return;
        }

        if (!LobbyRoomManager.Instance.IsClientInitialized)
        {
            Debug.LogWarning("[LobbyUI] LobbyRoomManager not initialized on client yet");
            return;
        }

        LobbyRoomManager.Instance.ToggleReadyServerRpc();
    }

    private void OnLeaveLobby()
    {
        SteamLobbyManager.Instance.LeaveLobby();
        ShowMainMenu();
    }


    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyRoomPanel != null) lobbyRoomPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void ShowLobbyRoom()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyRoomPanel != null) lobbyRoomPanel.SetActive(true);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void ShowLoadingScreen()
    {

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyRoomPanel != null) lobbyRoomPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }


    public void EnableReadyButton()
    {
        Debug.Log("[LobbyUI] EnableReadyButton called!");
        Debug.Log($"[LobbyUI] readyButton is null: {readyButton == null}");
        if (readyButton != null)
            readyButton.interactable = true;
    }


    public void SetLobbyIdText(string id)
    {
        if (lobbyIdText != null)
            lobbyIdText.text = "Lobby ID: " + id;
    }


    public void ShowNoLobbiesMessage()
    {
        if (noLobbiesText != null)
        {
            noLobbiesText.gameObject.SetActive(true);
            noLobbiesText.text = "Лобби не найдено. Создай своё!";
        }
    }


    public void ShowError(string message)
    {
        if (noLobbiesText != null)
        {
            noLobbiesText.gameObject.SetActive(true);
            noLobbiesText.text = message;
        }
    }

    public void UpdatePlayerList(
        IReadOnlyDictionary<int, string> names,
        IReadOnlyDictionary<int, bool> ready)
    {
        if (playerListContainer == null) return;

        foreach (Transform child in playerListContainer)
        {
            if (child != null)
                Destroy(child.gameObject);
        }

        foreach (var kv in names)
        {
            if (playerListContainer == null) return;

            int id = kv.Key;
            string playerName = kv.Value;
            bool isReady = ready.TryGetValue(id, out bool r) && r;

            GameObject row = Instantiate(playerRowPrefab, playerListContainer);

            TMP_Text nameText = row.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text readyText = row.transform.Find("ReadyText")?.GetComponent<TMP_Text>();

            if (nameText) nameText.text = playerName;
            if (readyText) readyText.text = isReady ? "Ready" : "...";
        }
    }
}