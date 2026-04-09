using System;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Transform cameraTarget;
    private float cameraPitch;
    
    [SerializeField] private float moveSpeed;
    [SerializeField] private float mouseSensivity;
    private Rigidbody rb;

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("OnStartClient, IsOwner: " + IsOwner);
        if (IsOwner)
        {
            var cinemachineCamera = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
            Debug.Log("Cinemachine found: " + (cinemachineCamera != null));
            if (cinemachineCamera != null)
            {
                cinemachineCamera.Target.TrackingTarget = cameraTarget;
            }
            // Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleRotation();
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        HandleMovement();
        
    }

    private void HandleMovement()
    {
        Vector2 input = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);

    }

    private void HandleRotation()
    {
        Vector2 lookDelta = GameInput.Instance.GetLookDelta();
        
        float rotationY = lookDelta.x * mouseSensivity; 
        transform.Rotate(0, rotationY, 0);
        
        cameraPitch -= lookDelta.y * mouseSensivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        head.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }
}

