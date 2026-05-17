using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Lever : NetworkBehaviour, IHoldInteractable
{
    [Header("Door")]
    [Tooltip("Гаражная дверь, на которую влияет этот рычаг")]
    [SerializeField] private GarageDoor connectedDoor;

    [Header("Visual")]
    [Tooltip("Часть рычага, которая будет поворачиваться")]
    [SerializeField] private Transform leverHandle;

    [Tooltip("Поворот рукоятки в положении 'выключено'")]
    [SerializeField] private Vector3 inactiveRotation = new Vector3(0, 0, 0);

    [Tooltip("Поворот рукоятки в положении 'включено'")]
    [SerializeField] private Vector3 activeRotation = new Vector3(45, 0, 0);

    [SerializeField] private float rotationSpeed = 5f;

    private readonly SyncVar<bool> isActive = new(false);

    public bool IsActive => isActive.Value;

    public override void OnStartClient()
    {
        base.OnStartClient();
        isActive.OnChange += OnActiveChanged;

        if (leverHandle != null)
            leverHandle.localRotation = Quaternion.Euler(isActive.Value ? activeRotation : inactiveRotation);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        isActive.OnChange -= OnActiveChanged;
    }

    private void OnActiveChanged(bool oldValue, bool newValue, bool asServer)
    {
        // Визуально интерполируется в Update
    }

    private void Update()
    {
        if (leverHandle == null) return;

        Quaternion target = Quaternion.Euler(isActive.Value ? activeRotation : inactiveRotation);
        leverHandle.localRotation = Quaternion.Slerp(leverHandle.localRotation, target, Time.deltaTime * rotationSpeed);
    }

  
    public void OnHoldStart(PlayerInteract player)
    {
        SetActiveServerRpc(true);
    }

    public void OnHoldEnd(PlayerInteract player)
    {
        SetActiveServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetActiveServerRpc(bool active, NetworkConnection conn = null)
    {
        if (conn == null) return;
        
        if (active)
        {
            PlayerInteract player = FindPlayerByConnection(conn);
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > 5f) return; 
        }

        if (isActive.Value == active) return;
        isActive.Value = active;
        
        if (connectedDoor != null)
        {
            if (active)
                connectedDoor.RegisterLeverActive(this);
            else
                connectedDoor.RegisterLeverInactive(this);
        }
    }

    [Server]
    private PlayerInteract FindPlayerByConnection(NetworkConnection conn)
    {
        PlayerInteract[] all = FindObjectsByType<PlayerInteract>(FindObjectsSortMode.None);
        foreach (var p in all)
        {
            if (p.OwnerId == conn.ClientId)
                return p;
        }
        return null;
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();
        if (isActive.Value && connectedDoor != null)
            connectedDoor.RegisterLeverInactive(this);
    }
}