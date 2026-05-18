using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;

    private bool isOpen;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;
    
    private PlayerCamera disabledCamera;
    private PlayerMoveController disabledMove;
    private PlayerInteract disabledInteract;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Close);
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToMenu);
    }

    private void Update()
    {
        if (GameResultUI.Instance != null && GameResultUI.Instance.IsShown)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (panel != null) panel.SetActive(true);

        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisableLocalPlayerControls();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panel != null) panel.SetActive(false);

        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;

        RestoreLocalPlayerControls();
    }

    private void DisableLocalPlayerControls()
    {
        PlayerInteract local = FindLocalPlayerInteract();
        if (local == null) return;

        PlayerCamera cam = local.GetComponent<PlayerCamera>();
        PlayerMoveController move = local.GetComponent<PlayerMoveController>();
        
        if (cam != null && cam.enabled)
        {
            cam.enabled = false;
            disabledCamera = cam;
        }

        if (move != null && move.enabled)
        {
            move.enabled = false;
            disabledMove = move;
        }

        if (local.enabled)
        {
            local.enabled = false;
            disabledInteract = local;
        }
    }

    private void RestoreLocalPlayerControls()
    {
        if (disabledCamera != null)
        {
            disabledCamera.enabled = true;
            disabledCamera = null;
        }

        if (disabledMove != null)
        {
            disabledMove.enabled = true;
            disabledMove = null;
        }

        if (disabledInteract != null)
        {
            disabledInteract.enabled = true;
            disabledInteract = null;
        }
    }

    private PlayerInteract FindLocalPlayerInteract()
    {
        PlayerInteract[] all = FindObjectsByType<PlayerInteract>(FindObjectsSortMode.None);
        foreach (var p in all)
        {
            if (p.IsOwner) return p;
        }
        return null;
    }

    private void ExitToMenu()
    {
        exitButton.interactable = false;

        LobbyRoomManager.Cleanup();

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("ModeSelectScene");
    }
}