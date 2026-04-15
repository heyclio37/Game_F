using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerRefs : NetworkBehaviour
{
     [SerializeField] private Transform head;
     [SerializeField] private Transform cameraTarget;
     [SerializeField] private Transform itemHolder;

     [SerializeField] private float moveSpeed = 10f;
     [SerializeField] private float interactDistance = 3f;
     [SerializeField] private CharacterController characterController;
     [SerializeField] private float gravity = 9.81f;

     public Transform Head => head;
     public Transform CameraTarget => cameraTarget;
     public Transform ItemHolder => itemHolder;
     public float InteractDistance => interactDistance;
     public CharacterController CharacterController => characterController;
     public float Gravity => gravity;
     public float MoveSpeed => moveSpeed;

     private void Awake()
     {
          if(!CharacterController) characterController = GetComponent<CharacterController>();
     }
}
