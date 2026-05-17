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
    [SerializeField] private TMP_InputField nameInputField;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OnJoin);
        backButton.onClick.AddListener(OnBack);

        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("LocalPlayerName", "");
            nameInputField.onEndEdit.AddListener(OnNameChanged);
        }
    }

    private void OnNameChanged(string newName)
    {
        PlayerNameProvider.SaveLocalName(newName.Trim());
    }

    private void OnHost()
    {
        SaveNameIfNeeded();
        LocalLobbyManager.Instance.HostGame();
    }

    private void OnJoin()
    {
        SaveNameIfNeeded();
        string ip = ipInputField != null ? ipInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
        LocalLobbyManager.Instance.JoinGame(ip);
    }

    private void SaveNameIfNeeded()
    {
        if (nameInputField == null) return;
        string n = nameInputField.text.Trim();
        if (!string.IsNullOrEmpty(n))
            PlayerNameProvider.SaveLocalName(n);
    }

    private void OnBack()
    {
        LocalLobbyManager.Instance.Leave();
        Destroy(InstanceFinder.NetworkManager.gameObject);
        SceneManager.LoadScene("ModeSelectScene");
    }
}