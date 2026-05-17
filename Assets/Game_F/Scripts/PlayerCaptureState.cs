using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerCaptureState : NetworkBehaviour
{
    [SerializeField] private PlayerRefs playerRefs;

    [Header("Visual hiding on escape")]
    [Tooltip("Объекты которые отключатся при побеге (модель, оружие в руках)")]
    [SerializeField] private GameObject[] visualsToHide;

    [Tooltip("Коллайдеры которые отключатся (чтобы игрок не блокировал других)")]
    [SerializeField] private Collider[] collidersToDisable;

    private readonly SyncVar<bool> isCaptured = new(false);
    private readonly SyncVar<bool> isEscaped = new(false);

    public bool IsCaptured => isCaptured.Value;
    public bool IsEscaped => isEscaped.Value;

    public override void OnStartClient()
    {
        base.OnStartClient();
        isEscaped.OnChange += OnEscapedChanged;

        if (isEscaped.Value)
            ApplyEscapeVisuals();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        isEscaped.OnChange -= OnEscapedChanged;
    }

    private void OnEscapedChanged(bool oldValue, bool newValue, bool asServer)
    {
        if (newValue)
            ApplyEscapeVisuals();
    }

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

    private void ApplyEscapeVisuals()
    {
        Debug.Log($"[PlayerCaptureState] ApplyEscapeVisuals called. IsOwner={IsOwner}");

        if (visualsToHide != null)
        {
            foreach (var go in visualsToHide)
                if (go != null) go.SetActive(false);
        }

        if (collidersToDisable != null)
        {
            foreach (var c in collidersToDisable)
                if (c != null) c.enabled = false;
        }

        if (IsOwner)
        {
            Debug.Log("[PlayerCaptureState] IsOwner, disabling controls and activating spectator");

            PlayerCamera cam = GetComponent<PlayerCamera>();
            if (cam != null) cam.enabled = false;

            PlayerMoveController move = GetComponent<PlayerMoveController>();
            if (move != null) move.enabled = false;

            PlayerInteract interact = GetComponent<PlayerInteract>();
            if (interact != null) interact.enabled = false;

            SpectatorView spectator = GetComponent<SpectatorView>();
            if (spectator == null)
                Debug.LogError("[PlayerCaptureState] SpectatorView component NOT FOUND on player!");
            else
                spectator.Activate();
        }
    }

    [ObserversRpc]
    private void TeleportObserversRpc(Vector3 position)
    {
        CharacterController cc = playerRefs.CharacterController;
        cc.enabled = false;
        transform.position = position;
        cc.enabled = true;
    }
}