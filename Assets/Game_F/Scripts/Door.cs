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
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float speed = 3f;
    [SerializeField] private ItemData requiredItem;

    private readonly SyncVar<DoorState> state = new();
    private float currentAngle;

    private void Awake()
    {
        state.Value = requiredItem != null ? DoorState.Locked : DoorState.Closed;
    }

    public void Interact(PlayerInteract player)
    {
        player.InteractWithDoorServerRpc(NetworkObject);
    }

    [Server]
    public bool TryInteract(PickupItem heldItem)
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

    private void Update()
    {
        float targetAngle = state.Value == DoorState.Open ? openAngle : 0f;
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * speed);
        doorWing.localRotation = Quaternion.Euler(0, currentAngle, 0);
    }
}