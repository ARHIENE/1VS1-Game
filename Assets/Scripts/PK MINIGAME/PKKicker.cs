using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// 키커 플레이어 오브젝트에 붙임
// 동작: 마우스를 누른 채 이동 → 공에서 골대까지 이어지는 궤적을 직접 그림
//        마우스를 떼면 궤적 확정 → 파워 게이지 단계로 전환 → 클릭으로 게이지 정지 시 실제 슛 확정
public class PKKicker : MonoBehaviourPun
{
    enum Phase { Idle, Drawing, GaugeRunning, Done }

    [Header("References")]
    public PKBall        ball;
    public PKPowerGauge  powerGauge;
    public LineRenderer  trajectoryLine;   // 궤적 미리보기
    public Camera        mainCamera;

    [Header("Ball World Position")]
    public Transform     ballTransform;   // 공 오브젝트 Transform

    [Header("Drawing")]
    public float maxDrawDuration  = 1.4f;  // 궤적을 그리는 데 배분되는 시간(공→골대 진행에 사용)
    public float startClickRadius = 80f;   // 공 근처 클릭 인식 반경(픽셀)

    [Header("Shot Error")]
    public float maxMissDeviation = 4.5f;  // 정확도 0(최악)일 때 적용되는 최대 오차(월드 단위)

    private Phase phase = Phase.Idle;
    private List<Vector3> drawnPoints;
    private float drawStartTime;

    private bool isMyTurn  = false;
    private bool hasKicked = false;
    public  bool HasKicked => hasKicked;

    private Coroutine timerRoutine;

    Vector3 GoalCenter    => PKGameManager.Instance != null ? PKGameManager.Instance.goalCenter    : new Vector3(0f, 1.22f, 10f);
    float   GoalHalfWidth => PKGameManager.Instance != null ? PKGameManager.Instance.goalHalfWidth : 3.66f;
    float   GoalHeight    => PKGameManager.Instance != null ? PKGameManager.Instance.goalHeight    : 2.44f;
    float   TurnTimeLimit => PKGameManager.Instance != null ? PKGameManager.Instance.turnTimeLimit : 10f;

    public bool IsMyTurn
    {
        get => isMyTurn;
        set
        {
            isMyTurn  = value;
            hasKicked = false;
            phase     = Phase.Idle;
            drawnPoints = null;
            ClearTrajectory();

            if (powerGauge != null)
            {
                powerGauge.OnStopped -= HandleGaugeStopped;
                powerGauge.ResetGauge();
            }

            if (timerRoutine != null) { StopCoroutine(timerRoutine); timerRoutine = null; }
            if (value) timerRoutine = StartCoroutine(TurnTimer());
        }
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            if (ball == null) ball = FindAnyObjectByType<PKBall>();
            if (ballTransform == null && ball != null) ballTransform = ball.transform;
            if (mainCamera == null) mainCamera = Camera.main;
            if (powerGauge == null) powerGauge = FindAnyObjectByType<PKPowerGauge>();

            if (trajectoryLine == null)
            {
                trajectoryLine = GetComponent<LineRenderer>();
                if (trajectoryLine == null)
                {
                    trajectoryLine = gameObject.AddComponent<LineRenderer>();
                }

                trajectoryLine.startWidth = 0.05f;
                trajectoryLine.endWidth = 0.05f;
                trajectoryLine.positionCount = 0;
                trajectoryLine.useWorldSpace = true;

                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    trajectoryLine.material = new Material(shader);
                }
                trajectoryLine.startColor = Color.yellow;
                trajectoryLine.endColor = Color.red;
            }
        }
    }

    void Update()
    {
        if (!photonView.IsMine || !isMyTurn || hasKicked) return;

        if (phase == Phase.Idle) HandleDrawStart();
        else if (phase == Phase.Drawing) HandleDrawing();
    }

    // ─────────────────────────────────────────────
    //  드래그 시작: 공 화면 위치 근처에서 눌러야 함
    // ─────────────────────────────────────────────
    void HandleDrawStart()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 ballScreen = mainCamera.WorldToScreenPoint(ballTransform.position);
        Vector2 mouse      = Input.mousePosition;
        if (Vector2.Distance(mouse, ballScreen) > startClickRadius) return;

        phase = Phase.Drawing;
        drawnPoints = new List<Vector3> { ballTransform.position };
        drawStartTime = Time.time;
        ClearTrajectory();
    }

    // ─────────────────────────────────────────────
    //  드래그 중: 현재 마우스 위치를 3D 궤적 포인트로 계속 추가
    // ─────────────────────────────────────────────
    void HandleDrawing()
    {
        if (Input.GetMouseButton(0))
        {
            float t = Mathf.Clamp01((Time.time - drawStartTime) / maxDrawDuration);
            float z = Mathf.Lerp(ballTransform.position.z, GoalCenter.z, t);

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.z) > 0.0001f)
            {
                float rt = (z - ray.origin.z) / ray.direction.z;
                if (rt > 0f)
                {
                    Vector3 worldPt = ray.origin + ray.direction * rt;
                    worldPt.x = Mathf.Clamp(worldPt.x, GoalCenter.x - GoalHalfWidth * 1.6f, GoalCenter.x + GoalHalfWidth * 1.6f);
                    worldPt.y = Mathf.Clamp(worldPt.y, 0.05f, GoalCenter.y + GoalHeight * 1.6f);

                    drawnPoints.Add(worldPt);
                    DrawTrajectoryPreview(drawnPoints);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishDrawing();
        }
    }

    void FinishDrawing()
    {
        if (drawnPoints.Count < 2)
        {
            drawnPoints.Add(new Vector3(GoalCenter.x, GoalCenter.y, GoalCenter.z));
        }
        else
        {
            Vector3 last = drawnPoints[drawnPoints.Count - 1];
            if (Mathf.Abs(last.z - GoalCenter.z) > 0.05f)
            {
                Vector3 prev = drawnPoints[drawnPoints.Count - 2];
                Vector3 dir  = last - prev;
                if (Mathf.Abs(dir.z) > 0.0001f)
                {
                    float tExtra = (GoalCenter.z - prev.z) / dir.z;
                    drawnPoints.Add(prev + dir * tExtra);
                }
                else
                {
                    drawnPoints.Add(new Vector3(last.x, last.y, GoalCenter.z));
                }
            }
        }

        phase = Phase.GaugeRunning;

        if (powerGauge != null)
        {
            powerGauge.ResetGauge();
            powerGauge.OnStopped += HandleGaugeStopped;
            powerGauge.StartGauge();
        }
        else
        {
            HandleGaugeStopped(0.5f); // 파워 게이지가 없으면 Perfect로 간주하고 즉시 발사
        }
    }

    void HandleGaugeStopped(float gaugeValue)
    {
        if (powerGauge != null) powerGauge.OnStopped -= HandleGaugeStopped;

        float error = powerGauge != null
            ? powerGauge.GetAccuracyError()
            : PKTrajectoryUtil.GetAccuracyError(gaugeValue, 0.08f);

        Vector3[] realTrajectory = PKTrajectoryUtil.ApplyError(drawnPoints, error, maxMissDeviation);
        FinalizeKick(realTrajectory);
    }

    void FinalizeKick(Vector3[] realTrajectory)
    {
        phase = Phase.Done;
        hasKicked = true;
        ClearTrajectory();

        if (timerRoutine != null) { StopCoroutine(timerRoutine); timerRoutine = null; }

        if (PKGameManager.Instance != null)
        {
            PKGameManager.Instance.SetKickerReady(realTrajectory);
        }
        else if (ball != null)
        {
            ball.KickAlongPath(realTrajectory);
        }
    }

    // 제한시간(10초) 초과 시 자동으로 약한 기본 슛을 골대 중앙으로 실행
    public void ForceWeakKick()
    {
        if (hasKicked) return;

        if (powerGauge != null)
        {
            powerGauge.OnStopped -= HandleGaugeStopped;
            powerGauge.StopGauge();
        }

        Vector3[] weakPath =
        {
            ballTransform != null ? ballTransform.position : GoalCenter,
            new Vector3(GoalCenter.x, GoalCenter.y, GoalCenter.z)
        };
        FinalizeKick(weakPath);
    }

    IEnumerator TurnTimer()
    {
        float remaining = TurnTimeLimit;
        while (remaining > 0f && !hasKicked)
        {
            PKUIManager.Instance?.UpdateTimer(remaining);
            yield return null;
            remaining -= Time.deltaTime;
        }

        if (!hasKicked)
        {
            PKUIManager.Instance?.UpdateTimer(0f);
            ForceWeakKick();
        }
    }

    // ─────────────────────────────────────────────
    //  궤적 미리보기
    // ─────────────────────────────────────────────
    void DrawTrajectoryPreview(List<Vector3> pts)
    {
        if (trajectoryLine == null) return;
        trajectoryLine.positionCount = pts.Count;
        trajectoryLine.SetPositions(pts.ToArray());
        trajectoryLine.enabled = true;
    }

    void ClearTrajectory()
    {
        if (trajectoryLine == null) return;
        trajectoryLine.positionCount = 0;
        trajectoryLine.enabled = false;
    }
}
