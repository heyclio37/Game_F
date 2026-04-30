using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public event Action OnInteractAction;
    public event Action OnDropAction;
    public event Action OnAttackAction;
   public static GameInput Instance { get; private set; }

   private PlayerInputActions playerInputActions;

   private void Awake()
   {
       if (Instance != null)
       {
           Destroy(gameObject);
           return;
       }

       Instance = this;

       playerInputActions = new PlayerInputActions();
       playerInputActions.Player.Enable();

       playerInputActions.Player.Interact.performed += Interact_Performed;
       playerInputActions.Player.Drop.performed += Drop_Performed;
       playerInputActions.Player.Attack.performed += Attack_Performed;
   }

   private void Drop_Performed(InputAction.CallbackContext obj)
   {
       OnDropAction?.Invoke();
   }

   private void Interact_Performed(InputAction.CallbackContext obj)
   {
       OnInteractAction?.Invoke();
   }
   private void Attack_Performed(InputAction.CallbackContext obj)
   {
       OnAttackAction?.Invoke();
   }

   public Vector2 GetMovementVectorNormalized()
   {
       return playerInputActions.Player.Move.ReadValue<Vector2>().normalized;
   }

   public Vector2 GetLookDelta()
   {
       return playerInputActions.Player.Look.ReadValue<Vector2>();
   }

   private void OnDestroy()
   {
       playerInputActions.Player.Interact.performed -= Interact_Performed;
       playerInputActions.Player.Drop.performed -= Drop_Performed;
       playerInputActions.Dispose();
   }
}
