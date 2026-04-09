using FishNet.Managing;
using UnityEngine;

public class NetworkStarter : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = FindAnyObjectByType<NetworkManager>();
    }

    public void OnHostClick()
    {
        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
    }

    public void OnClientClick()
    {
        networkManager.ClientManager.StartConnection();
    }
}