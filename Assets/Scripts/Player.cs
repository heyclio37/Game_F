using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Cinemachine;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform itemHolder;
    
    [SerializeField] private float moveSpeed;
    [SerializeField] private float mouseSensitivity;
    [SerializeField] private float interactDistance;
    
    
    private float cameraPitch;
    private Rigidbody rb;
    private readonly SyncVar<NetworkObject> heldItem = new();
    
    private const string PLAYER_CAMERA_TAG = "PlayerCamera";
    
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner) return;
        GameInput.Instance.OnInteractAction += Interact;
        GameInput.Instance.OnDropAction += Drop;
        SetupCamera();
    }

    private void Update()
    {
        if (!IsOwner) return;
        TurnControl();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        MovementControl();
    }

    private void SetupCamera()
    {
        GameObject obj = GameObject.FindWithTag(PLAYER_CAMERA_TAG);
        if (obj == null)
        {
            Debug.LogError(PLAYER_CAMERA_TAG + " not found");
        }
        CinemachineCamera cinemachineCamera = obj.GetComponent<CinemachineCamera>();
       // if (cinemachineCamera != null) return;
        cinemachineCamera.Target.TrackingTarget = cameraTarget;
    }


    private void MovementControl()
    {
        Vector2 input = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
    }

    private void TurnControl()
    {
        Vector2 lookDelta = GameInput.Instance.GetLookDelta();

        transform.Rotate(0, lookDelta.x * mouseSensitivity, 0);

        cameraPitch -= lookDelta.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        head.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }
    
    private void Interact()
    {
        if (!Physics.Raycast(cameraTarget.position, cameraTarget.forward, out RaycastHit hit, interactDistance)) return;
        if (!hit.collider.TryGetComponent(out PickupItem pickupItem)) return;

        PickupServerRpc(pickupItem.NetworkObject);
    }

    private void Drop()
    {
        if (heldItem.Value == null) return;
        DropServerRpc();
    }

    [ServerRpc]
    private void PickupServerRpc(NetworkObject item)
    {
        if (item == null) return;
        if (heldItem.Value != null) return;
        float distance = Vector3.Distance(transform.position, item.transform.position);
        if (distance > interactDistance) return;

        var pickupItem = item.GetComponent<PickupItem>();
        if (pickupItem == null) return;

        heldItem.Value = item;
        AttachItemObserversRpc(item);
    }

    [ServerRpc]
    private void DropServerRpc()
    {
        if (heldItem.Value == null) return;
        NetworkObject item = heldItem.Value;
        heldItem.Value = null;
        DetachItemObserversRpc(item);
    }

    [ObserversRpc]
    private void AttachItemObserversRpc(NetworkObject item)
    {
        item.transform.SetParent(itemHolder);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        SetItemPhysics(item.gameObject, false);
    }

    [ObserversRpc]
    private void DetachItemObserversRpc(NetworkObject item)
    {
        if (item == null) return;

        item.transform.SetParent(null);
        item.transform.position = itemHolder.position;

        SetItemPhysics(item.gameObject, true);
    }

    private void SetItemPhysics(GameObject item, bool enabled)
    {
        var collider = item.GetComponent<Collider>();
        if (collider != null) collider.enabled = enabled;

        var rigidbody = item.GetComponent<Rigidbody>();
        if (rigidbody != null) rigidbody.isKinematic = !enabled;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (!IsOwner) return;
        GameInput.Instance.OnInteractAction -= Interact;
        GameInput.Instance.OnDropAction -= Drop;
    }
}