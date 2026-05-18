using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ModeSelectUI : MonoBehaviour
{
    [SerializeField] private Button steamButton;
    [SerializeField] private Button localButton;
    [SerializeField] private Button quitButton;

    private AsyncOperation pendingLoad;
    private bool isLoading = false;

    private void Start()
    {
        steamButton.onClick.AddListener(OnSteamClick);
        localButton.onClick.AddListener(OnLocalClick);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClick);
    }

    private void OnSteamClick()
    {
        if (isLoading) return;
        GameConnectionManager.Instance.SetMode(ConnectionMode.Steam);
        StartCoroutine(LoadScene("MainMenuScene"));
    }

    private void OnLocalClick()
    {
        if (isLoading) return;
        GameConnectionManager.Instance.SetMode(ConnectionMode.Local);
        StartCoroutine(LoadScene("LocalMenuScene"));
    }

    private void OnQuitClick()
    {
        if (isLoading) return;

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator LoadScene(string sceneName)
    {
        isLoading = true;
        steamButton.interactable = false;
        localButton.interactable = false;
        if (quitButton != null)
            quitButton.interactable = false;

        pendingLoad = SceneManager.LoadSceneAsync(sceneName);
        pendingLoad.allowSceneActivation = false;

        while (pendingLoad.progress < 0.9f)
            yield return null;

        pendingLoad.allowSceneActivation = true;
        isLoading = false;
    }
}