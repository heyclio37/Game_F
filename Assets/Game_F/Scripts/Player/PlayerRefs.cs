using FishNet.Object;
using UnityEngine;

public class PlayerRefs : NetworkBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform itemHolder;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float interactDistance;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float gravity;

    private PlayerCaptureState captureState;

    public Transform Head => head;
    public Transform CameraTarget => cameraTarget;
    public Transform ItemHolder => itemHolder;
    public float InteractDistance => interactDistance;
    public CharacterController CharacterController => characterController;
    public float Gravity => gravity;
    public float MoveSpeed => moveSpeed;
    public bool IsCaptured => captureState != null && captureState.IsCaptured;

    private void Awake()
    {
        captureState = GetComponent<PlayerCaptureState>();
        if (!characterController) characterController = GetComponent<CharacterController>();
    }
}