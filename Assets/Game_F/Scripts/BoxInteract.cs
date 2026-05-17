using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class BoxInteract : NetworkBehaviour, IInteractable
{
    [Header("Visual")]
    [SerializeField] private GameObject boxInteractVisual;

    [Header("Reward")]
    [Tooltip("Префаб ключа, который заспавнится после взлома")]
    [SerializeField] private NetworkObject keyPrefab;

    [Tooltip("Смещение позиции спавна ключа")]
    [SerializeField] private Vector3 keySpawnOffset = new Vector3(0, 1f, 0);

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;

    private readonly SyncVar<bool> isOpened = new(false);

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (isOpened.Value && boxInteractVisual != null)
            boxInteractVisual.SetActive(false);

        isOpened.OnChange += OnOpenedChanged;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        isOpened.OnChange -= OnOpenedChanged;
    }

    private void OnOpenedChanged(bool oldValue, bool newValue, bool asServer)
    {
        if (newValue && boxInteractVisual != null)
            boxInteractVisual.SetActive(false);
    }

    public void Interact(PlayerInteract player)
    {
        if (isOpened.Value) return;
        if (!IsHoldingScrewdriver(player)) return;

        OpenServerRpc();
    }

    private bool IsHoldingScrewdriver(PlayerInteract player)
    {
        if (player == null) return false;

        PickupItem held = player.HeldPickupItem;
        if (held == null) return false;
        if (held.itemData == null) return false;

        return held.itemData.itemType == ItemType.Screwdriver;
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenServerRpc(NetworkConnection conn = null)
    {
        if (conn == null) return;
        if (isOpened.Value) return;

        PlayerInteract player = FindPlayerByConnection(conn);
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance > interactDistance) return;

        if (!IsHoldingScrewdriver(player)) return;

        isOpened.Value = true;

        if (keyPrefab != null)
        {
            Vector3 spawnPos = transform.position + keySpawnOffset;
            NetworkObject key = Instantiate(keyPrefab, spawnPos, Quaternion.identity);
            ServerManager.Spawn(key);
            Debug.Log($"[BoxInteract] Box opened by client {conn.ClientId}, key spawned");
        }
        else
        {
            Debug.LogError("[BoxInteract] keyPrefab is null!");
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
}