using System;
using UnityEngine;

public class GameInput : MonoBehaviour
{
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
       playerInputActions.Dispose();
   }
}
