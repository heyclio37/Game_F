using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public enum DoorState
{
    Locked,
    Closed,
    Open
}

public class Door : NetworkBehaviour, IInteractable
{
    [SerializeField] private Transform doorWing;
    [SerializeField] private float openAngle;
    [SerializeField] private float speed;
    [SerializeField] protected ItemData requiredItem;

    protected readonly SyncVar<DoorState> state = new();
    private float currentAngle;

    public bool IsOpen => state.Value == DoorState.Open;

    protected virtual void Awake()
    {
        state.Value = requiredItem != null ? DoorState.Locked : DoorState.Closed;
    }

    public virtual void Interact(PlayerInteract player)
    {
        player.InteractWithDoorServerRpc(NetworkObject);
    }

    [Server]
    public virtual bool TryInteract(PickupItem heldItem)
    {
        switch (state.Value)
        {
            case DoorState.Locked:
                if (heldItem != null && heldItem.itemData == requiredItem)
                {
                    state.Value = DoorState.Closed;
                    heldItem.Consume();
                    return true;
                }
                return false;

            case DoorState.Closed:
                state.Value = DoorState.Open;
                return false;

            case DoorState.Open:
                state.Value = DoorState.Closed;
                return false;
        }
        return false;
    }

    protected virtual void Update()
    {
        float targetAngle = state.Value == DoorState.Open ? openAngle : 0f;
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * speed);
        doorWing.localRotation = Quaternion.Euler(0, currentAngle, 0);
    }
}