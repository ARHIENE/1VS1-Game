using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace FPSMinigame
{
public class KillCam : MonoBehaviourPun
{
    [Header("카메라 설정")]
    public Camera deathCam;
    public Camera faceCam;

    [Header("연출 설정")]
    public float slowMotionScale = 0.2f;
    public float deathCamTime = 1.5f;
    public float faceCamTime = 1.5f;

    private Camera mainCam;
    private Transform killerTransform;
    private Transform victimTransform;

    void Start()
    {
        Transform cameraHolder = transform.Find("CameraHolder");
        if (cameraHolder != null)
            mainCam = cameraHolder.GetComponentInChildren<Camera>();

        if (deathCam != null)
        {
            deathCam.enabled = false;
            deathCam.gameObject.SetActive(false);
        }
        if (faceCam != null)
        {
            faceCam.enabled = false;
            faceCam.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    public void SetKillerRPC(Vector3 pos, Quaternion rot)
    {
        if (killerTransform != null)
            Destroy(killerTransform.gameObject);

        GameObject temp = new GameObject("KillerPos");
        temp.transform.position = pos;
        temp.transform.rotation = rot;
        killerTransform = temp.transform;
    }

    public IEnumerator PlayKillCam()
    {
        if (!photonView.IsMine) yield break;

        FPSController fps = GetComponent<FPSController>();
        if (fps != null) fps.isKillCamPlaying = true;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (mainCam != null) mainCam.enabled = false;

        if (deathCam != null)
        {
            deathCam.enabled = true;
            deathCam.gameObject.SetActive(true);
            deathCam.transform.position = transform.position
                + (-transform.forward * 2f) + Vector3.up * 1.5f;
            deathCam.transform.LookAt(transform.position + Vector3.up * 1.5f);
        }

        yield return new WaitForSecondsRealtime(deathCamTime);

        if (deathCam != null)
        {
            deathCam.enabled = false;
            deathCam.gameObject.SetActive(false);
        }

        if (faceCam != null && killerTransform != null)
        {
            faceCam.enabled = true;
            faceCam.gameObject.SetActive(true);
            faceCam.transform.position = killerTransform.position
                + killerTransform.forward * 1f + Vector3.up * 1.7f;
            faceCam.transform.LookAt(killerTransform.position + Vector3.up * 1.7f);
        }

        yield return new WaitForSecondsRealtime(faceCamTime);

        EndKillCam();
    }

    public IEnumerator PlayKillerCam(Transform victim)
    {
        if (!photonView.IsMine) yield break;

        victimTransform = victim;

        FPSController fps = GetComponent<FPSController>();
        if (fps != null) fps.isKillCamPlaying = true;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (mainCam != null) mainCam.enabled = false;

        if (deathCam != null)
        {
            deathCam.enabled = true;
            deathCam.gameObject.SetActive(true);
            deathCam.transform.position = victimTransform.position
                + (-victimTransform.forward * 2f) + Vector3.up * 1.5f;
            deathCam.transform.LookAt(victimTransform.position + Vector3.up * 1.5f);
        }

        yield return new WaitForSecondsRealtime(deathCamTime);

        if (deathCam != null)
        {
            deathCam.enabled = false;
            deathCam.gameObject.SetActive(false);
        }

        if (faceCam != null)
        {
            faceCam.enabled = true;
            faceCam.gameObject.SetActive(true);
            faceCam.transform.position = transform.position
                + transform.forward * 1f + Vector3.up * 1.7f;
            faceCam.transform.LookAt(transform.position + Vector3.up * 1.7f);
        }

        yield return new WaitForSecondsRealtime(faceCamTime);

        EndKillCam();
    }

    void EndKillCam()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (deathCam != null)
        {
            deathCam.enabled = false;
            deathCam.gameObject.SetActive(false);
        }
        if (faceCam != null)
        {
            faceCam.enabled = false;
            faceCam.gameObject.SetActive(false);
        }
        if (mainCam != null) mainCam.enabled = true;

        FPSController fps = GetComponent<FPSController>();
        if (fps != null) fps.isKillCamPlaying = false;

        if (killerTransform != null)
            Destroy(killerTransform.gameObject);
    }
}
}