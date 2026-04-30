using FishNet.Object;
using UnityEngine;

public class PrisonDoor : Door
{
    [SerializeField] private float autoCloseDelay = 5f;
    private float closeTimer;

    protected override void Awake()
    {
        state.Value = DoorState.Closed;
    }

    public override void Interact(PlayerInteract player)
    {
        player.InteractWithPrisonDoorServerRpc(NetworkObject);
    }

    [Server]
    public void TryOpen(PlayerCaptureState playerState)
    {
        if (playerState == null) return;
        if (playerState.IsCaptured) return;
        if (state.Value == DoorState.Open) return;

        state.Value = DoorState.Open;
        closeTimer = autoCloseDelay;

        GameManager.Instance.FreeAllPrisoners();
    }

    protected override void Update()
    {
        base.Update();

        if (IsServerStarted && state.Value == DoorState.Open)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer <= 0f)
                state.Value = DoorState.Closed;
        }
    }
}