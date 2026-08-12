using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

namespace FPSMinigame
{
public class FPSController : MonoBehaviourPun, IPunObservable
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float runSpeed = 9f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 1.5f;
    public float gravity = -30f;

    [Header("마우스 설정")]
    public float mouseSensitivity = 2f;
    public Transform cameraHolder;

    [Header("앉기 설정")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float standCamY = 1.7f;
    public float crouchCamY = 0.9f;
    public float crouchSmoothSpeed = 10f;

    [Header("정확도 설정")]
    public float maxSpread = 3f;
    public float spreadRecoverySpeed = 5f;
    public float counterStrafeThreshold = 0.1f;

    [Header("발소리 설정")]
    public AudioClip[] runFootstepClips;
    public AudioClip landClip;
    public float runFootstepInterval = 0.3f;

    public bool isKillCamPlaying = false;

    private CharacterController cc;
    private Vector3 velocity;
    private float xRotation = 0f;
    private float currentSpread = 0f;
    private bool isCrouching = false;
    private bool isRunning = false;
    private bool isGrounded = false;
    private bool wasGrounded = false;
    private float footstepTimer = 0f;
    private AudioSource audioSource;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 50f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        if (photonView.IsMine)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MiniGame1")
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (cameraHolder != null)
            {
                Camera cam = cameraHolder.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.enabled = true;
                    
                    int layer = LayerMask.NameToLayer("ThirdPersonOnly");
                    if (layer == -1) layer = 8; // Fallback to index 8
                    
                    cam.cullingMask &= ~(1 << layer);
                    
                    Debug.Log($"[FPSController] Configured Camera cullingMask to exclude layer {layer}. Current cullingMask: {cam.cullingMask}");
                }
            }
            audioSource.spatialBlend = 0f;
        }
        else
        {
            if (cameraHolder != null)
            {
                Camera cam = cameraHolder.GetComponentInChildren<Camera>();
                if (cam != null) cam.enabled = false;
            }
            networkPosition = transform.position;
            networkRotation = transform.rotation;
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 15f);
            return;
        }

        if (isKillCamPlaying) return;

        wasGrounded = isGrounded;
        HandleMovement();

        if (!wasGrounded && isGrounded)
            photonView.RPC("RPC_PlayLand", RpcTarget.All);

        HandleMouseLook();
        HandleCrouch();
        HandleJump();
        UpdateSpread();
        HandleFootstep();

        if (cameraHolder != null)
        {
            float targetCamY = isCrouching ? crouchCamY : standCamY;
            Vector3 camPos = cameraHolder.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchSmoothSpeed);
            cameraHolder.localPosition = camPos;
        }
    }

    void HandleMovement()
    {
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
        }

        Vector3 move = transform.right * x + transform.forward * z;
        move = move.normalized;

        isRunning = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && z > 0 && !isCrouching && isGrounded && move.magnitude > 0;

        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : moveSpeed);
        cc.Move(move * speed * Time.deltaTime);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        isGrounded = cc.isGrounded;
    }

    void HandleMouseLook()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
            mouseY = mouseDelta.y * mouseSensitivity * 0.1f;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCrouch()
    {
        bool wantCrouch = Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
        if (wantCrouch == isCrouching) return;

        isCrouching = wantCrouch;

        cc.height = isCrouching ? crouchHeight : standHeight;
        cc.center = new Vector3(0, cc.height / 2f, 0);

        photonView.RPC("RPC_SetCrouch", RpcTarget.Others, isCrouching);
    }

    [PunRPC]
    void RPC_SetCrouch(bool crouching)
    {
        isCrouching = crouching;
        cc.height = crouching ? crouchHeight : standHeight;
        cc.center = new Vector3(0, cc.height / 2f, 0);
    }

    void HandleJump()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }

    void HandleFootstep()
    {
        bool isMoving = false;
        if (Keyboard.current != null)
        {
            isMoving = Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
                       Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed;
        }
        bool shouldPlayFootstep = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && isMoving && isGrounded;

        if (!shouldPlayFootstep)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer += Time.deltaTime;
        if (footstepTimer >= runFootstepInterval)
        {
            footstepTimer = 0f;
            photonView.RPC("RPC_PlayFootstep", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_PlayFootstep()
    {
        if (runFootstepClips == null || runFootstepClips.Length == 0) return;
        AudioClip clip = runFootstepClips[Random.Range(0, runFootstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }

    [PunRPC]
    void RPC_PlayLand()
    {
        if (landClip == null) return;
        audioSource.PlayOneShot(landClip);
    }

    void UpdateSpread()
    {
        float speed = cc != null ? new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude : 0f;
        
        bool inputMoving = false;
        if (Keyboard.current != null)
        {
            inputMoving = Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed ||
                          Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed;
        }

        float targetSpread = 0f;

        if (!isGrounded)
        {
            // Jumping/Airborne: Massive spread
            targetSpread = maxSpread * 1.5f;
        }
        else if (isCrouching)
        {
            // Crouching: extremely accurate, small spread if moving
            targetSpread = inputMoving ? maxSpread * 0.2f : 0f;
        }
        else if (isRunning && inputMoving)
        {
            // Running/Sprinting: High spread
            targetSpread = maxSpread * 1.0f;
        }
        else if (inputMoving)
        {
            // Walking: Moderate spread
            targetSpread = maxSpread * 0.5f;
        }
        else
        {
            // Standing still or perfect stop
            targetSpread = 0f;
        }

        // Braking / Counter-strafing detection:
        // If grounded and horizontal speed is extremely low (meaning we successfully stopped or counter-strafed)
        bool isBraked = isGrounded && (speed < 0.25f || !inputMoving);

        if (isBraked)
        {
            // Recover instantly to target spread (usually 0) when stationary or perfectly braked
            currentSpread = Mathf.MoveTowards(currentSpread, targetSpread, spreadRecoverySpeed * Time.deltaTime * 8f);
        }
        else
        {
            // Slower recovery or expansion when actively moving/jumping
            currentSpread = Mathf.MoveTowards(currentSpread, targetSpread, spreadRecoverySpeed * Time.deltaTime * 2.5f);
        }
    }

    public float GetCurrentSpread() => currentSpread;
    public bool IsCrouching() => isCrouching;
    public bool IsRunning() => isRunning;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(isCrouching);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            bool crouching = (bool)stream.ReceiveNext();

            if (crouching != isCrouching)
            {
                isCrouching = crouching;
                cc.height = crouching ? crouchHeight : standHeight;
                cc.center = new Vector3(0, cc.height / 2f, 0);
            }
        }
    }

    public void OnDied()
    {
        enabled = false;
    }
}
}