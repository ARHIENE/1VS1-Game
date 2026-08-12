using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PlayerSetup : MonoBehaviourPun
{
    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        bool isGameScene = sceneName == "GameScene";
        bool isAOSScene = sceneName == "MiniGame_LavaRiver";
        bool isFPSScene = sceneName == "MiniGame1";
        bool isPKScene = sceneName == "MiniGame2";

        // Scene-specific scaling (massive scale for AOS feel)
        if (isAOSScene)
        {
            transform.localScale = new Vector3(25f, 25f, 25f);
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        // Fix animator on start
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.SetBool("IsGrounded", true);
            animator.SetFloat("Speed", 0f);
        }

        // Enable/Disable AOSController based on scene & ownership
        var aos = GetComponent<AOSController>();
        if (aos != null)
        {
            aos.enabled = photonView.IsMine ? isAOSScene : false;
            if (isAOSScene)
            {
                // Assign different fireballs for everyone to ensure RPC works
                bool isPlayer1 = photonView.Owner.ActorNumber % 2 != 0;
                string fireballName = isPlayer1 ? "Fireball_Orange" : "Fireball_Blue";
                aos.fireballPrefab = Resources.Load<GameObject>(fireballName);
            }
        }

        // Setup FPS components
        var fps = GetComponent<FPSMinigame.FPSController>();
        var gun = GetComponent<FPSMinigame.GunController>();
        var health = GetComponent<FPSMinigame.PlayerHealth>();
        var killCam = GetComponent<FPSMinigame.KillCam>();
        var anim = GetComponent<FPSMinigame.PlayerAnimator>();

        if (fps != null) fps.enabled = isFPSScene && photonView.IsMine;
        if (gun != null) gun.enabled = isFPSScene && photonView.IsMine;
        if (health != null) health.enabled = isFPSScene;
        if (killCam != null) killCam.enabled = isFPSScene;
        if (anim != null) anim.enabled = isFPSScene;

        // Setup PK components
        var kicker = GetComponent<PKKicker>();
        var gk = GetComponent<PKGoalkeeper>();
        if (kicker != null) kicker.enabled = isPKScene && photonView.IsMine;
        if (gk != null) gk.enabled = isPKScene && photonView.IsMine;

        if (isPKScene && photonView.IsMine)
        {
            if (PKGameManager.Instance != null)
            {
                PKGameManager.Instance.RegisterLocalPlayer(kicker, gk);
            }
        }

        // Toggle local player body mesh visibility for first-person camera
        if (isFPSScene && photonView.IsMine)
        {
            Transform meshTrans = transform.Find("HumanM_BodyMesh");
            if (meshTrans != null)
            {
                meshTrans.gameObject.layer = LayerMask.NameToLayer("ThirdPersonOnly");
            }
        }
        else
        {
            Transform meshTrans = transform.Find("HumanM_BodyMesh");
            if (meshTrans != null)
            {
                meshTrans.gameObject.layer = LayerMask.NameToLayer("Default");
            }
        }

        if (photonView.IsMine)
        {
            if (GetComponent<PlayerController>() != null) GetComponent<PlayerController>().enabled = isGameScene;
            if (GetComponent<CharacterController>() != null) GetComponent<CharacterController>().enabled = isGameScene || isFPSScene;
            
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) 
            {
                agent.enabled = isAOSScene;
                if (isAOSScene)
                {
                    agent.radius = 6.0f; // Adjusted for massive 25x scale
                    agent.height = 20f;
                    agent.speed = 120f; // Fast base speed
                    agent.acceleration = 5000f; // Instant acceleration
                    agent.angularSpeed = 5000f; // Instant rotation
                    agent.autoBraking = false; // Disable slowing down
                }
            }
            
            // Camera Setup
            if (isAOSScene) SetupAOSCamera();
            if (isFPSScene)
            {
                SetupFPSCamera();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                // RESTORE: Turn off FPS cameras and restore the default scene camera for non-FPS scenes
                DisableFPSCamerasLocal();
            }
        }
        else
        {
            // IMPORTANT: Remote players must NOT have movement controllers active
            if (GetComponent<PlayerController>() != null) GetComponent<PlayerController>().enabled = false;
            
            var remoteAos = GetComponent<AOSController>();
            if (remoteAos != null) remoteAos.enabled = false;

            var remoteAgent = GetComponent<NavMeshAgent>();
            if (remoteAgent != null) remoteAgent.enabled = false;

            var remoteCc = GetComponent<CharacterController>();
            if (remoteCc != null) remoteCc.enabled = false;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) 
            {
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
            }

            // Disable child cameras on remote clients to prevent camera overlap / overlay issues
            DisableRemoteCameras();
        }
    }

    void DisableFPSCamerasLocal()
    {
        var fpsCamTrans = transform.Find("CameraHolder/FPSCamera");
        if (fpsCamTrans != null)
        {
            var cam = fpsCamTrans.GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
            
            var listener = fpsCamTrans.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        var deathCamTrans = transform.Find("DeathCam");
        if (deathCamTrans != null)
        {
            var cam = deathCamTrans.GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
        }

        var faceCamTrans = transform.Find("FaceCam");
        if (faceCamTrans != null)
        {
            var cam = faceCamTrans.GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
        }

        // Re-enable the scene's Main Camera
        GameObject sceneCam = GameObject.Find("Main Camera");
        if (sceneCam != null)
        {
            var cam = sceneCam.GetComponent<Camera>();
            if (cam != null) cam.enabled = true;
            
            var listener = sceneCam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }
    }

    void SetupAOSCamera()
    {
        GameObject camGO = GameObject.Find("Main Camera");
        if (camGO != null)
        {
            CameraFollow normFollow = camGO.GetComponent<CameraFollow>();
            if (normFollow != null) Destroy(normFollow);

            AOSCameraFollow follow = camGO.GetComponent<AOSCameraFollow>();
            if (follow == null) follow = camGO.AddComponent<AOSCameraFollow>();
            follow.target = transform;
        }
    }

    void SetupFPSCamera()
    {
        // For FPS, the local player's child camera 'FPSCamera' is the active camera.
        // We disable the main scene camera to let the FPS camera render.
        GameObject sceneCam = GameObject.Find("Main Camera");
        if (sceneCam != null)
        {
            sceneCam.GetComponent<Camera>().enabled = false;
            var listener = sceneCam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        var fpsCamTrans = transform.Find("CameraHolder/FPSCamera");
        if (fpsCamTrans != null)
        {
            var cam = fpsCamTrans.GetComponent<Camera>();
            if (cam != null)
            {
                cam.enabled = true;
                
                int layer = LayerMask.NameToLayer("ThirdPersonOnly");
                if (layer == -1) layer = 8; // Fallback to index 8
                
                // Hide local player's own body from first-person view
                cam.cullingMask &= ~(1 << layer);
                
                Debug.Log($"[PlayerSetup] Configured FPSCamera cullingMask to exclude layer {layer}. Current cullingMask: {cam.cullingMask}");
            }
            
            var listener = fpsCamTrans.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }
    }

    void DisableRemoteCameras()
    {
        var fpsCamTrans = transform.Find("CameraHolder/FPSCamera");
        if (fpsCamTrans != null)
        {
            var cam = fpsCamTrans.GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
            
            var listener = fpsCamTrans.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        var deathCamTrans = transform.Find("DeathCam");
        if (deathCamTrans != null)
        {
            var cam = deathCamTrans.GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
        }

        var faceCamTrans = transform.Find("FaceCam");
        if (faceCamTrans != null)
        {
            var cam = faceCamTrans.GetComponent<Camera>();
            if (cam != null) cam.enabled = false;
        }
    }
}
