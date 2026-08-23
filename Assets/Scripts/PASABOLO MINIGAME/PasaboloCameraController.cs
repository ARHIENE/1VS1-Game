using System.Collections;
using UnityEngine;

// 동적 카메라: Aim(조준, 공 근접 추적) -> Flight(발사 후 핀 추적) -> Result(테이블 전체 조망) 3단계 전환.
// 프로젝트 전체가 Cinemachine을 쓰지 않고 손수 Lerp/Slerp로 카메라를 다루므로(CameraFollow/GomokuSeatedCamera)
// 동일한 방식을 따른다. PasaboloTurnManager의 OnShotFired/OnShotSettled 이벤트로 상태를 전환하며,
// 이 이벤트는 RpcTarget.All로 전파되므로 양쪽 클라이언트가 동시에 같은 시점 전환을 겪는다.
public class PasaboloCameraController : MonoBehaviour
{
    enum CamState { Idle, Aim, Flight, Result }

    [Header("References")]
    public PasaboloLaunchController launchController;
    public PasaboloTableSpawner tableSpawner;

    [Header("Aim (공 기준 오프셋)")]
    public Vector3 aimOffset = new Vector3(0f, 1.2f, -2.5f);
    public float aimFollowSpeed = 6f;

    [Header("Flight (핀 평균 위치 기준 오프셋)")]
    public Vector3 flightOffset = new Vector3(0f, 3f, -4f);
    public float flightFollowSpeed = 4f;

    [Header("Result (테이블 크기 비율 기반 조망)")]
    public float resultHeightRatio = 0.5f;
    public float resultBackRatio = 0.6f;
    public float blendDuration = 0.6f;

    private CamState state = CamState.Idle;
    private Coroutine blendRoutine;

    void Start()
    {
        if (launchController == null) launchController = FindAnyObjectByType<PasaboloLaunchController>();
        if (tableSpawner == null) tableSpawner = FindAnyObjectByType<PasaboloTableSpawner>();

        if (PasaboloTurnManager.Instance != null)
        {
            PasaboloTurnManager.Instance.OnShotFired += HandleShotFired;
            PasaboloTurnManager.Instance.OnShotSettled += HandleShotSettled;
        }

        SetResultView(instant: true);
    }

    void OnDestroy()
    {
        if (PasaboloTurnManager.Instance != null)
        {
            PasaboloTurnManager.Instance.OnShotFired -= HandleShotFired;
            PasaboloTurnManager.Instance.OnShotSettled -= HandleShotSettled;
        }
    }

    void Update()
    {
        bool isMyTurn = PasaboloTurnManager.Instance != null && PasaboloTurnManager.Instance.IsLocalTurn();
        bool dragging = launchController != null && launchController.IsDragging;

        if (isMyTurn && dragging && state != CamState.Aim)
        {
            state = CamState.Aim;
        }
        else if (isMyTurn && !dragging && state == CamState.Aim)
        {
            // 드래그를 풀었지만 아직 발사 전(취소) — 전체 조망으로 복귀
            SetResultView(instant: false);
        }

        if (state == CamState.Aim) UpdateAim();
        else if (state == CamState.Flight) UpdateFlight();
    }

    void UpdateAim()
    {
        if (launchController == null || launchController.ballTransform == null) return;

        Vector3 pull = launchController.CurrentPullVector;
        Vector3 aimDir = pull.sqrMagnitude > 0.01f ? pull.normalized : Vector3.forward;
        Vector3 rotatedOffset = Quaternion.LookRotation(-aimDir, Vector3.up) * aimOffset;

        Vector3 targetPos = launchController.ballTransform.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * aimFollowSpeed);
        transform.LookAt(launchController.ballTransform.position + Vector3.up * 0.3f);
    }

    void HandleShotFired()
    {
        state = CamState.Flight;
    }

    void UpdateFlight()
    {
        if (tableSpawner == null || tableSpawner.pins == null || tableSpawner.pins.Length == 0) return;

        Vector3 avg = Vector3.zero;
        int count = 0;
        foreach (Transform pin in tableSpawner.pins)
        {
            if (pin == null) continue;
            avg += pin.position;
            count++;
        }
        if (count == 0) return;
        avg /= count;

        Vector3 targetPos = avg + flightOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * flightFollowSpeed);
        transform.LookAt(avg);
    }

    void HandleShotSettled()
    {
        SetResultView(instant: false);
    }

    void SetResultView(bool instant)
    {
        state = CamState.Result;

        Vector3 center = tableSpawner != null ? tableSpawner.TableCenter : Vector3.zero;
        float length = tableSpawner != null ? tableSpawner.tableLength : 20f;
        Vector3 targetPos = center + new Vector3(0f, length * resultHeightRatio, -length * resultBackRatio);
        Quaternion targetRot = Quaternion.LookRotation((center - targetPos).normalized, Vector3.up);

        if (blendRoutine != null) StopCoroutine(blendRoutine);

        if (instant)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
        else
        {
            blendRoutine = StartCoroutine(BlendTo(targetPos, targetRot));
        }
    }

    IEnumerator BlendTo(Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float t = 0f;

        while (t < blendDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / blendDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, k);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
    }
}
