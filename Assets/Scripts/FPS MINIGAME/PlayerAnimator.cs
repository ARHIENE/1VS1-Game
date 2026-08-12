using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

namespace FPSMinigame
{
public class PlayerAnimator : MonoBehaviourPun
{
    private Animator animator;
    private FPSController fpsController;
    private CharacterController cc;

    private float lastRotationY;
    private float animSpeed = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        fpsController = GetComponent<FPSController>();
        cc = GetComponent<CharacterController>();
        lastRotationY = transform.eulerAngles.y;
    }

    void Update()
    {
        if (animator == null) return;

        // Only the local player controls their own animations.
        // PhotonAnimatorView will automatically sync the Animator parameters to other clients.
        if (photonView.IsMine)
        {
            bool isMoving = false;
            bool isRunning = false;

            if (Keyboard.current != null)
            {
                isMoving = Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
                           Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed;
                isRunning = Keyboard.current.leftShiftKey.isPressed && isMoving;
            }

            bool isCrouching = fpsController != null && fpsController.IsCrouching();
            bool isGrounded = cc != null ? cc.isGrounded : true;

            float targetSpeed = 0f;
            if (isMoving)
            {
                targetSpeed = isCrouching ? 0.3f : (isRunning ? 1.0f : 0.5f);
            }
            animSpeed = Mathf.MoveTowards(animSpeed, targetSpeed, Time.deltaTime * 5f);

            // Calculate Turn
            float currentRotationY = transform.eulerAngles.y;
            float deltaRotation = Mathf.DeltaAngle(lastRotationY, currentRotationY);
            float targetTurn = Mathf.Clamp(deltaRotation / (15f * 5f), -1f, 1f);
            lastRotationY = currentRotationY;

            // Apply locally
            animator.SetFloat("Speed", animSpeed);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsCrouching", isCrouching);
            animator.SetFloat("Turn", targetTurn, 0.1f, Time.deltaTime);

            // Sync Jump Trigger via RPC for instant response
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !isCrouching)
            {
                animator.SetTrigger("Jump");
                photonView.RPC("RPC_TriggerJump", RpcTarget.Others);
            }
        }
    }

    [PunRPC]
    void RPC_TriggerJump()
    {
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }
}
}