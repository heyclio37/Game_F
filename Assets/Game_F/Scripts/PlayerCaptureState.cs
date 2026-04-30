using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerCaptureState : NetworkBehaviour
{
    [SerializeField] private PlayerRefs playerRefs;

    private readonly SyncVar<bool> isCaptured = new(false);
    private readonly SyncVar<bool> isEscaped = new(false);

    public bool IsCaptured => isCaptured.Value;
    public bool IsEscaped => isEscaped.Value;

    [Server]
    public void Capture(Vector3 prisonPosition)
    {
        isCaptured.Value = true;

        PlayerInteract interact = GetComponent<PlayerInteract>();
        if (interact != null)
            interact.ForceDropItem();

        TeleportObserversRpc(prisonPosition);
    }

    [Server]
    public void Release()
    {
        isCaptured.Value = false;
    }

    [Server]
    public void SetEscaped()
    {
        isEscaped.Value = true;
    }

    [ObserversRpc]
    private void TeleportObserversRpc(Vector3 position)
    {
        CharacterController cc = playerRefs.CharacterController;
        cc.enabled = false;
        transform.position = position;
        cc.enabled = true;
    }

    [ObserversRpc]
    public void ShowGameResult(bool isWin)
    {
        if (!IsOwner) return; 

        if (isWin)
            Debug.Log("[Player] You Win!");
        else
            Debug.Log("[Player] You Lose!");
    }
}