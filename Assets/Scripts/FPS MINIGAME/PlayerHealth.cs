using UnityEngine;
using Photon.Pun;

namespace FPSMinigame
{
public class PlayerHealth : MonoBehaviourPun
{
    [Header("리스폰 설정")]
    public float respawnTime = 3f;

    private Transform spawnPoint1;
    private Transform spawnPoint2;
    private bool isDead = false;

    public bool IsDead => isDead;

    void Start()
    {
        var sp1Obj = GameObject.Find("SpawnPoint_1");
        var sp2Obj = GameObject.Find("SpawnPoint_2");
        if (sp1Obj != null) spawnPoint1 = sp1Obj.transform;
        if (sp2Obj != null) spawnPoint2 = sp2Obj.transform;

        if (photonView.IsMine)
        {
            MoveToSpawnPoint();
        }
    }

    public void SetDead()
    {
        isDead = true;
    }

    [PunRPC]
    void OnHeadShot()
    {
        if (isDead) return;
        isDead = true;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            // Reset locomotion parameters on all clients to guarantee clean knockdown transition
            anim.SetFloat("Speed", 0f);
            anim.SetBool("IsGrounded", true);
            anim.SetBool("IsCrouching", false);
            anim.SetFloat("Turn", 0f);
            
            anim.SetTrigger("Knockdown");
        }

        // Disable PlayerAnimator to stop sending updates that override/fight with Knockdown transition
        PlayerAnimator animScript = GetComponent<PlayerAnimator>();
        if (animScript != null) animScript.enabled = false;

        // Disable PhotonAnimatorView on all clients during death so synced stream parameters (like Speed > 0.1)
        // do not instantly override and force the remote animator out of the Knockdown state.
        var pav = GetComponent<Photon.Pun.PhotonAnimatorView>();
        if (pav != null) pav.enabled = false;

        if (photonView.IsMine)
        {
            MG01Manager.Instance?.AddOpponentScore();

            KillCam killCam = GetComponent<KillCam>();
            if (killCam != null)
                StartCoroutine(killCam.PlayKillCam());

            StartCoroutine(Respawn());
        }
        else
        {
            MG01Manager.Instance?.AddMyScore();
        }

        FPSController fps = GetComponent<FPSController>();
        if (fps != null) fps.enabled = false;
    }

    public void ForceRespawn()
    {
        MoveToSpawnPoint();
        isDead = false;

        FPSController fps = GetComponent<FPSController>();
        if (fps != null) fps.enabled = true;

        PlayerAnimator animScript = GetComponent<PlayerAnimator>();
        if (animScript != null) animScript.enabled = true;

        var pav = GetComponent<Photon.Pun.PhotonAnimatorView>();
        if (pav != null) pav.enabled = true;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
        }
    }

    void MoveToSpawnPoint()
    {
        Transform spawnPoint = PhotonNetwork.IsMasterClient
            ? spawnPoint1
            : spawnPoint2;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        else
        {
            // Safe fallback
            transform.position = new Vector3(PhotonNetwork.IsMasterClient ? -5f : 5f, 2f, 0f);
            transform.rotation = Quaternion.identity;
        }

        if (cc != null) cc.enabled = true;
    }

    System.Collections.IEnumerator Respawn()
    {
        yield return new WaitForSecondsRealtime(respawnTime);

        MoveToSpawnPoint();
        isDead = false;

        FPSController fps = GetComponent<FPSController>();
        if (fps != null) fps.enabled = true;

        MG01Manager.Instance?.RespawnBoth();

        photonView.RPC("OnRespawned", RpcTarget.All);
    }

    [PunRPC]
    void OnRespawned()
    {
        isDead = false;
        FPSController fps = GetComponent<FPSController>();
        if (fps != null) fps.enabled = true;

        PlayerAnimator animScript = GetComponent<PlayerAnimator>();
        if (animScript != null) animScript.enabled = true;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
        }
    }
}
}