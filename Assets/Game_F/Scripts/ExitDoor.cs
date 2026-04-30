using FishNet.Object;
using UnityEngine;

public class ExitDoor : Door
{
    [SerializeField] private Collider exitTrigger;

    public override void Interact(PlayerInteract player)
    {
        player.InteractWithExitDoorServerRpc(NetworkObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ExitDoor] Trigger entered by {other.name}, IsServerStarted={IsServerStarted}, IsOpen={IsOpen}");
        if (!IsServerStarted) return;
        if (!IsOpen) return;

        PlayerCaptureState player = other.GetComponentInParent<PlayerCaptureState>();
        if (player == null)
        {
            Debug.Log("[ExitDoor] player is null");
            return;
        }
        Debug.Log($"[ExitDoor] Player IsCaptured={player.IsCaptured}, IsEscaped={player.IsEscaped}");
        if (player.IsCaptured || player.IsEscaped) return;

        player.SetEscaped();
        Debug.Log("[ExitDoor] Player marked as escaped, calling CheckGameEnd");
        GameManager.Instance?.CheckGameEnd();
    }
}