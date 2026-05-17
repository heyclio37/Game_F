using System;
using Unity.Cinemachine;
using UnityEngine;

public class SpectatorView : MonoBehaviour
{
    [SerializeField] private float retargetInterval = 1f;

    private const string PLAYER_CAMERA_TAG = "PlayerCamera";

    private CinemachineCamera cinemaCamera;
    private Transform currentTarget;
    private PlayerInfo currentTargetInfo;
    private float retargetTimer;
    private bool isActive;
    
    public event Action<string> OnTargetChanged;

    public bool IsActive => isActive;
    public string CurrentTargetName => currentTargetInfo != null ? currentTargetInfo.PlayerName : null;

    public void Activate()
    {
        if (isActive) return;
        isActive = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameObject obj = GameObject.FindWithTag(PLAYER_CAMERA_TAG);
        if (obj == null)
        {
            Debug.LogError("[SpectatorView] PlayerCamera tag not found");
            return;
        }

        cinemaCamera = obj.GetComponent<CinemachineCamera>();
        if (cinemaCamera == null)
        {
            Debug.LogError("[SpectatorView] CinemachineCamera component not found");
            return;
        }

        FindNewTarget();
    }

    public void Deactivate()
    {
        isActive = false;
        cinemaCamera = null;
        currentTarget = null;
        currentTargetInfo = null;
        OnTargetChanged?.Invoke(null);
    }

    private void Update()
    {
        if (!isActive) return;

        retargetTimer -= Time.deltaTime;

        bool needRetarget = currentTarget == null || retargetTimer <= 0f;
        if (needRetarget)
        {
            FindNewTarget();
            retargetTimer = retargetInterval;
        }
    }

    private void FindNewTarget()
    {
        PlayerCaptureState[] all = FindObjectsByType<PlayerCaptureState>(FindObjectsSortMode.None);

        Transform best = null;
        PlayerInfo bestInfo = null;

        foreach (var p in all)
        {
            if (p.gameObject == gameObject) continue;
            if (p.IsEscaped) continue;

            best = GetCameraTarget(p);
            bestInfo = p.GetComponent<PlayerInfo>();
            if (best != null)
                break;
        }

        if (best == currentTarget) return;

        currentTarget = best;
        currentTargetInfo = bestInfo;

        if (cinemaCamera != null)
            cinemaCamera.Target.TrackingTarget = currentTarget;

        string name = bestInfo != null ? bestInfo.PlayerName : (best != null ? best.root.name : null);
        OnTargetChanged?.Invoke(name);

        if (currentTarget != null)
            Debug.Log($"[SpectatorView] Now spectating {name}");
    }

    private Transform GetCameraTarget(PlayerCaptureState player)
    {
        PlayerRefs refs = player.GetComponent<PlayerRefs>();
        if (refs != null && refs.CameraTarget != null)
            return refs.CameraTarget;

        return player.transform;
    }
}