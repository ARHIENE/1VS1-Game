using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.5f;
    public float crouchSpeed = 1.8f;
    public float jumpHeight = 1.8f;
    public float gravity = -30f;
    public float rotationSpeed = 15f;

    [Header("Crouch Settings")]
    public float normalHeight = 1.8f;
    public float crouchHeight = 1.0f;
    public Vector3 normalCenter = new Vector3(0, 0.9f, 0);
    public Vector3 crouchCenter = new Vector3(0, 0.5f, 0);

    private CharacterController controller;
    private Animator animator;

    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    
    private float lastRotationY;
    private float animSpeed;

    private void Awake()
    {
controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        controller.height = normalHeight;
        controller.center = normalCenter;
        lastRotationY = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return;

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (Keyboard.current == null) return;

        // 1. Input
        float moveX = 0;
        float moveZ = 0;
        if (Keyboard.current.wKey.isPressed) moveZ += 1;
        if (Keyboard.current.sKey.isPressed) moveZ -= 1;
        if (Keyboard.current.aKey.isPressed) moveX -= 1;
        if (Keyboard.current.dKey.isPressed) moveX += 1;

        isCrouching = Keyboard.current.cKey.isPressed;
        bool isRunning = Keyboard.current.leftShiftKey.isPressed && !isCrouching;

        Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized;
        float animTurn = 0f;

        // 2. Crouch Height
        controller.height = isCrouching ? crouchHeight : normalHeight;
        controller.center = isCrouching ? crouchCenter : normalCenter;

        // 3. Move & Rotate
        if (moveDirection.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, targetAngle, 0), rotationSpeed * Time.deltaTime);

            float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
            controller.Move(moveDirection * speed * Time.deltaTime);
            
            float targetAnimSpeed = isRunning ? 1.0f : 0.5f;
            animSpeed = Mathf.MoveTowards(animSpeed, targetAnimSpeed, Time.deltaTime * 5f);
        }
        else
        {
            animSpeed = Mathf.MoveTowards(animSpeed, 0f, Time.deltaTime * 5f);
        }

        // 4. Calculate Turn Value (Angular Delta)
        float currentRotationY = transform.eulerAngles.y;
        float deltaRotation = Mathf.DeltaAngle(lastRotationY, currentRotationY);
        animTurn = Mathf.Clamp(deltaRotation / (rotationSpeed * 5f), -1f, 1f);
        lastRotationY = currentRotationY;

        // 5. Jump
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        // 6. Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 7. Update Animator
        animator.SetFloat("Speed", animSpeed);
        animator.SetFloat("Turn", animTurn, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
    }
}
