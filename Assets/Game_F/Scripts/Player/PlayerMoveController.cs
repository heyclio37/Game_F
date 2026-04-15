using FishNet.Object;
using UnityEngine;

public class PlayerMoveController : NetworkBehaviour
{
    [SerializeField] private PlayerRefs playerRefs;

    private float verticalVelocity;
    private readonly float groundSnap = -0.5f;

    


    private void Update()
    {
        if (!IsOwner) return;
        HandleMove();
    }

    private void HandleMove()
    {
        Vector2 input = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
        Vector3 moveVelocity = moveDirection * playerRefs.MoveSpeed;

        if (playerRefs.CharacterController.isGrounded && verticalVelocity < 0f) 
            verticalVelocity = groundSnap;
        else 
            verticalVelocity -= playerRefs.Gravity * Time.deltaTime;

        moveVelocity.y = verticalVelocity;
        playerRefs.CharacterController.Move(moveVelocity * Time.deltaTime);
    }
}