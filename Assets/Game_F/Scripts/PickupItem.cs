using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PickupItem : NetworkBehaviour, IInteractable
{
    public ItemData itemData;
    public Collider ItemCollider { get; private set; }
    public Rigidbody ItemRigidbody { get; private set; }
    public bool IsHeld { get; set; }

    private void Awake()
    {
        ItemCollider = GetComponent<Collider>();
        ItemRigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsServerStarted)
            ItemRigidbody.isKinematic = true;
    }

    public void Interact(PlayerInteract player)
    {
        player.PickupServerRpc(NetworkObject);
    }

    public void AttachTo(Transform parent)
    {
        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        ItemCollider.enabled = false;
        ItemRigidbody.isKinematic = true;
    }

    public void Detach(Vector3 position, Vector3 velocity, bool isServer)
    {
        transform.SetParent(null);
        transform.position = position;
        ItemCollider.enabled = true;
        ItemRigidbody.isKinematic = !isServer;
        if (isServer)
            ItemRigidbody.linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServerStarted) return;
        if (IsHeld) return;

        NoiseSystem.MakeNoise(transform.position);
    }

    [Server]
    public void Consume()
    {
        ServerManager.Despawn(gameObject);
    }
}