using FishNet;
using System.Collections;
using UnityEngine;

public class GameSceneClientNotifier : MonoBehaviour
{
    [SerializeField] private float startupDelay = 0.5f;
    [SerializeField] private float retryInterval = 0.5f;
    [SerializeField] private float maxWaitTime = 10f;

    private void Start()
    {
        if (GameConnectionManager.Instance == null ||
            GameConnectionManager.Instance.Mode != ConnectionMode.Steam)
        {
            Debug.Log("[GameSceneClientNotifier] Not in Steam mode, skipping");
            return;
        }

        StartCoroutine(NotifyServerRoutine());
    }

    private IEnumerator NotifyServerRoutine()
    {
        yield return new WaitForSeconds(startupDelay);

        float elapsed = 0f;

        while (elapsed < maxWaitTime)
        {
            if (!InstanceFinder.IsClientStarted)
            {
                Debug.LogWarning("[GameSceneClientNotifier] Client not started yet, waiting...");
            }
            else if (LobbyRoomManager.Instance == null)
            {
                Debug.LogWarning("[GameSceneClientNotifier] LobbyRoomManager.Instance is null, waiting...");
            }
            else
            {
                Debug.Log("[GameSceneClientNotifier] Notifying server that scene is loaded");
                LobbyRoomManager.Instance.NotifySceneLoadedServerRpc();
                yield break;
            }

            yield return new WaitForSeconds(retryInterval);
            elapsed += retryInterval;
        }

        Debug.LogError("[GameSceneClientNotifier] Failed to notify server within timeout");
    }
}