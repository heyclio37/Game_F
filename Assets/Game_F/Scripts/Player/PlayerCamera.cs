using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private PlayerRefs playerRefs;
    [SerializeField] private float mouseSensitivity = 0.5f;

    private float cameraPitch;
    private const string PLAYER_CAMERA_TAG = "PlayerCamera";

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner) return;
        SetupCamera();
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleRotation();
    }

    private void SetupCamera()
    {
        GameObject obj = GameObject.FindWithTag(PLAYER_CAMERA_TAG);
        if (obj == null)
        {
            Debug.LogError(PLAYER_CAMERA_TAG + " not found");
            return;
        }

        CinemachineCamera cinemachineCamera = obj.GetComponent<CinemachineCamera>();
        if (cinemachineCamera != null)
            cinemachineCamera.Target.TrackingTarget = playerRefs.CameraTarget;
    }

    private void HandleRotation()
    {
        Vector2 lookDelta = GameInput.Instance.GetLookDelta();

        transform.Rotate(0, lookDelta.x * mouseSensitivity, 0);

        cameraPitch -= lookDelta.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        playerRefs.Head.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }
}