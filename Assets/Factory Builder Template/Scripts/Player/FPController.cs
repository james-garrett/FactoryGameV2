using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// Simple first person controller
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPController : MonoBehaviour
    {
        [Header("Camera attached to player")]
        public Camera ConnectedCamera;

        [Header("Controller variables")]

        [Header("WSAD to move, space to jump, hold shift to sprint, F to toggle fly mode (space - up, Q - down), ESC to toggle cursor")]
        public bool FlyMode = false;
        public float MouseLookAroundSpeed = 1.7f;
        public float WalkingSpeed = 10f;
        public float RunningSpeed = 14f;
        public float JumpForce = 10f;
        public float GravityForce = 30f;

        private CharacterController characterController;
        private Vector3 moveDirection;
        private float rotationX; 
        private bool cursorVisible;

        void Start()
        {
            characterController = GetComponent<CharacterController>();

            cursorVisible = true;
            moveDirection = Vector3.zero;
            rotationX = 0;

            ChangeCursor();
        }

        public void ShowCursor()
        {
            cursorVisible = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        public void HideCursor()
        {
            cursorVisible = false;
            Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
        }

        public void ChangeCursor()
        {
            cursorVisible = !cursorVisible;
            if(cursorVisible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } else
            {
                Cursor.lockState = CursorLockMode.Locked;
                // Idk why cursor disappear here.
                // Cursor.visible = false;
            }
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F))
            {
                FlyMode = !FlyMode;
            }

            if(Input.GetKeyDown(KeyCode.Escape))
            {
                ChangeCursor();
            }
             
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right); 

            float speed = Input.GetKey(KeyCode.LeftShift) ? RunningSpeed : WalkingSpeed; 
            float dirX = speed * Input.GetAxis("Vertical");
            float dirZ = speed * Input.GetAxis("Horizontal");
            float movementDirectionY = moveDirection.y;

            moveDirection = (forward * dirX) + (right * dirZ);

            if(Input.GetButton("Jump") && (characterController.isGrounded || FlyMode))
            {
                moveDirection.y = JumpForce;
            } 
            else if(!FlyMode)
            {
                moveDirection.y = movementDirectionY;
            }

            //lower player when in fly mode
            if(Input.GetKey(KeyCode.Q) && FlyMode)
            {
                moveDirection.y = -JumpForce;
            }

            //apply gravity force
            if(!characterController.isGrounded && !FlyMode)
            {
                moveDirection.y -= GravityForce * Time.deltaTime; 
            }

            //move controller
            characterController.Move(moveDirection * Time.deltaTime);

            //rotation
            rotationX += -Input.GetAxis("Mouse Y") * MouseLookAroundSpeed;
            rotationX = Mathf.Clamp(rotationX, -90, 90);

            //rotate camera only when cursor is locked
            if(Cursor.lockState == CursorLockMode.Locked)
            {
                ConnectedCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * MouseLookAroundSpeed, 0);
            }
        }
    }
}
