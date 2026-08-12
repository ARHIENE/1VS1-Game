using System.Collections;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class PKBall : MonoBehaviourPun
{
    [Header("Path Follow Settings")]
    public float travelSpeed       = 16f;  // 궤적 길이에 따른 이동 속도(m/s)
    public float minTravelDuration = 0.6f;
    public float maxTravelDuration = 1.6f;

    private Rigidbody rb;
    private bool hasBeenKicked = false;
    public bool HasBeenKicked => hasBeenKicked;

    private Coroutine pathRoutine;
    public bool IsMoving => pathRoutine != null;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (photonView != null)
        {
            photonView.OwnershipTransfer = OwnershipOption.Takeover;
        }
    }

    // 오차가 적용된 실제 궤적을 따라 공을 이동시킴
    public void KickAlongPath(Vector3[] path)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
        photonView.RPC(nameof(RPC_KickAlongPath), RpcTarget.All, path);
    }

    [PunRPC]
    void RPC_KickAlongPath(Vector3[] path)
    {
        hasBeenKicked = true;
        if (pathRoutine != null) StopCoroutine(pathRoutine);
        pathRoutine = StartCoroutine(FollowPath(path));
    }

    IEnumerator FollowPath(Vector3[] path)
    {
        if (path == null || path.Length < 2)
        {
            pathRoutine = null;
            yield break;
        }

        float length = 0f;
        for (int i = 1; i < path.Length; i++)
            length += Vector3.Distance(path[i - 1], path[i]);

        float duration = Mathf.Clamp(length / Mathf.Max(travelSpeed, 0.01f), minTravelDuration, maxTravelDuration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = SampleAlongPath(path, t);
            yield return null;
        }

        transform.position = path[path.Length - 1];
        pathRoutine = null;
    }

    Vector3 SampleAlongPath(Vector3[] path, float t)
    {
        float target = t * (path.Length - 1);
        int   index  = Mathf.Clamp(Mathf.FloorToInt(target), 0, path.Length - 2);
        float localT = target - index;
        return Vector3.Lerp(path[index], path[index + 1], localT);
    }

    public void ResetBall(Vector3 position)
    {
        if (pathRoutine != null) { StopCoroutine(pathRoutine); pathRoutine = null; }
        photonView.RPC(nameof(RPC_ResetBall), RpcTarget.All, position);
    }

    [PunRPC]
    void RPC_ResetBall(Vector3 position)
    {
        hasBeenKicked = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = position;
    }
}
