using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LocalMenuUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_InputField ipInputField;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OnJoin);
        backButton.onClick.AddListener(OnBack);
    }

    private void OnHost() => LocalLobbyManager.Instance.HostGame();

    private void OnJoin()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
        LocalLobbyManager.Instance.JoinGame(ip);
    }

    private void OnBack()
    {
        LocalLobbyManager.Instance.Leave();
        Destroy(InstanceFinder.NetworkManager.gameObject);
        SceneManager.LoadScene("ModeSelectScene");
    }
}