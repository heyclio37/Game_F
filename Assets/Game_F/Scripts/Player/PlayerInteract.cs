using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerInteract : NetworkBehaviour
{
    [SerializeField] private PlayerRefs playerRefs;

    [Header("Item Clipping")]
    [SerializeField] private float itemBaseDistance = 0.4f;
    [SerializeField] private float itemMinDistance = 0.08f;
    [SerializeField] private float itemRadius = 0.12f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Shooting")]
    [SerializeField] private float minShootDistance = 1.0f;

    private readonly SyncVar<NetworkObject> heldItem = new();
    private PickupItem heldPickupItem;
    private TaserGun heldTaserGun;

    public TaserGun HeldTaserGun => heldTaserGun;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner) return;
        GameInput.Instance.OnInteractAction += Interact;
        GameInput.Instance.OnDropAction += Drop;
        GameInput.Instance.OnAttackAction += Attack;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (!IsOwner) return;
        GameInput.Instance.OnInteractAction -= Interact;
        GameInput.Instance.OnDropAction -= Drop;
        GameInput.Instance.OnAttackAction -= Attack;
    }

    private void Update()
    {
        if (!IsOwner) return;
        AdjustItemHolderDistance();
    }

    private void Interact()
    {
        if (!Physics.Raycast(playerRefs.CameraTarget.position, playerRefs.CameraTarget.forward, out RaycastHit hit,
                playerRefs.InteractDistance)) return;

        if (hit.collider.TryGetComponent(out Bullet bullet))
        {
            if (heldTaserGun != null)
                RetrieveBulletServerRpc(bullet.NetworkObject);
            return;
        }

        if (!hit.collider.TryGetComponent(out IInteractable interactable))
            hit.collider.transform.root.TryGetComponent(out interactable);

        if (interactable == null) return;
        interactable.Interact(this);
    }

    private void Drop()
    {
        if (heldItem.Value == null) return;
        Vector3 velocity = playerRefs.CharacterController.velocity;
        DropServerRpc(velocity);
    }

    private void Attack()
    {
        if (heldTaserGun == null) return;

        if (Physics.Raycast(playerRefs.CameraTarget.position, playerRefs.CameraTarget.forward, out _, minShootDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            return;

        ShootServerRpc(playerRefs.CameraTarget.forward);
    }

    [ServerRpc]
    public void PickupServerRpc(NetworkObject item)
    {
        if (item == null) return;
        if (heldItem.Value != null) return;

        float distance = Vector3.Distance(transform.position, item.transform.position);
        if (distance > playerRefs.InteractDistance) return;

        PickupItem pickupItem = item.GetComponent<PickupItem>();
        if (pickupItem == null) return;

        heldItem.Value = item;
        AttachItemObserversRpc(item);
    }

    [ObserversRpc]
    private void AttachItemObserversRpc(NetworkObject item)
    {
        if (item == null) return;

        Vector3 resetPos = playerRefs.ItemHolder.localPosition;
        resetPos.z = itemBaseDistance;
        playerRefs.ItemHolder.localPosition = resetPos;

        heldPickupItem = item.GetComponent<PickupItem>();
        heldPickupItem.AttachTo(playerRefs.ItemHolder);
        heldTaserGun = heldPickupItem.GetComponent<TaserGun>();
    }

    [ServerRpc]
    private void DropServerRpc(Vector3 playerVelocity)
    {
        if (heldItem.Value == null) return;
        NetworkObject item = heldItem.Value;
        heldItem.Value = null;
        DetachItemObserversRpc(item, playerVelocity);
    }

    [ObserversRpc]
    private void DetachItemObserversRpc(NetworkObject item, Vector3 playerVelocity)
    {
        if (item == null) return;
        heldPickupItem.Detach(playerRefs.ItemHolder.position, playerVelocity, IsServerStarted);
        heldPickupItem = null;
        heldTaserGun = null;
    }

    [Server]
    public void ForceDropItem()
    {
        if (heldItem.Value == null) return;
        NetworkObject item = heldItem.Value;
        heldItem.Value = null;
        DetachItemObserversRpc(item, Vector3.zero);
    }

    [ServerRpc]
    public void InteractWithDoorServerRpc(NetworkObject doorObject)
    {
        if (doorObject == null) return;
        Door door = doorObject.GetComponent<Door>();
        if (door == null) return;

        bool consumed = door.TryInteract(heldPickupItem);
        if (consumed)
        {
            heldItem.Value = null;
            ClearHeldRefsObserversRpc();
        }
    }

    [ServerRpc]
    public void InteractWithExitDoorServerRpc(NetworkObject doorObject)
    {
        if (doorObject == null) return;
        ExitDoor exitDoor = doorObject.GetComponent<ExitDoor>();
        if (exitDoor == null) return;

        bool consumed = exitDoor.TryInteract(heldPickupItem);
        if (consumed)
        {
            heldItem.Value = null;
            ClearHeldRefsObserversRpc();
        }
    }

    [ServerRpc]
    public void InteractWithPrisonDoorServerRpc(NetworkObject doorObject)
    {
        if (doorObject == null) return;
        PrisonDoor prisonDoor = doorObject.GetComponent<PrisonDoor>();
        if (prisonDoor == null) return;

        PlayerCaptureState captureState = GetComponent<PlayerCaptureState>();
        if (captureState != null && captureState.IsCaptured) return;

        prisonDoor.TryOpen(captureState);
    }

    [ObserversRpc]
    private void ClearHeldRefsObserversRpc()
    {
        heldPickupItem = null;
        heldTaserGun = null;
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 aimDirection)
    {
        heldTaserGun?.TryShoot(aimDirection);
    }

    [ServerRpc]
    private void RetrieveBulletServerRpc(NetworkObject bulletObject)
    {
        if (bulletObject == null) return;

        float distance = Vector3.Distance(playerRefs.CameraTarget.position, bulletObject.transform.position);
        if (distance > playerRefs.InteractDistance) return;

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null || heldTaserGun == null) return;

        TaserGun gun = bullet.GetOwnerGun();
        if (gun != null && gun == heldTaserGun)
        {
            gun.RetrieveBullet();
            ServerManager.Despawn(bulletObject);
        }
    }

    private void AdjustItemHolderDistance()
    {
        if (heldPickupItem == null) return;

        Vector3 origin = playerRefs.CameraTarget.position;
        Vector3 direction = playerRefs.CameraTarget.forward;

        if (Physics.SphereCast(
                origin,
                itemRadius,
                direction,
                out RaycastHit hit,
                itemBaseDistance + itemRadius,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            float targetZ = Mathf.Max(itemMinDistance, hit.distance - itemRadius);

            Vector3 localPos = playerRefs.ItemHolder.localPosition;
            localPos.z = Mathf.Lerp(localPos.z, targetZ, Time.deltaTime * 20f);
            playerRefs.ItemHolder.localPosition = localPos;
        }
        else
        {
            Vector3 localPos = playerRefs.ItemHolder.localPosition;
            localPos.z = Mathf.Lerp(localPos.z, itemBaseDistance, Time.deltaTime * 20f);
            playerRefs.ItemHolder.localPosition = localPos;
        }
    }
}