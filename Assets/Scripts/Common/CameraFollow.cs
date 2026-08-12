using UnityEngine;
using Photon.Pun;

public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 5, -7);
    public float smoothSpeed = 0.125f;
    private Transform target;

    private void LateUpdate()
    {
        if (target == null)
        {
            FindLocalPlayer();
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    private void FindLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                target = player.transform;
                break;
            }
        }
    }
}
