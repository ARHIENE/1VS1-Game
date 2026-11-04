using UnityEngine;
using Photon.Pun;

// 마스터 클라이언트(또는 오프라인 핫싯)만 실제 물리(AddForce, 충돌)를 시뮬레이션하고,
// 공/핀이 모두 멈추면 TurnManager를 통해 결과를 RPC로 전파한다.
// 비마스터 클라이언트의 공/핀 Rigidbody는 isKinematic으로 전환해 PhotonTransformView가
// 전달하는 위치만 그대로 반영한다 — 이 프로젝트에 실시간 물리 동기화 전례가 없어
// (오목/PK 모두 결정론적 RPC 또는 결과-재생 방식만 사용) 검증된 PhotonTransformView만 재사용해 새로 도입.
public class PasaboloPhysicsController : MonoBehaviour
{
    [Header("References")]
    public PasaboloTableSpawner tableSpawner;
    public PasaboloTurnManager turnManager;

    [Header("Rest Detection")]
    public float restVelocityThreshold = 0.05f;
    public float restCheckDuration = 0.5f;

    [Header("Rayas 가중치 (거리 임계값은 tableSpawner.rayaDistances 재사용)")]
    public int[] rayaWeights = { 1, 2, 3 };

    bool IsAuthority => !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

    private Rigidbody ballBody;
    private Rigidbody[] pinBodies;
    private bool simulating;
    private float restTimer;
    private int pendingShooter;

    void Start()
    {
        if (tableSpawner == null) tableSpawner = FindAnyObjectByType<PasaboloTableSpawner>();
        if (turnManager == null) turnManager = PasaboloTurnManager.Instance;

        ballBody = tableSpawner != null && tableSpawner.ball != null ? tableSpawner.ball.GetComponent<Rigidbody>() : null;

        int pinCount = tableSpawner != null && tableSpawner.pins != null ? tableSpawner.pins.Length : 0;
        pinBodies = new Rigidbody[pinCount];
        for (int i = 0; i < pinCount; i++)
        {
            pinBodies[i] = tableSpawner.pins[i] != null ? tableSpawner.pins[i].GetComponent<Rigidbody>() : null;
        }

        if (ballBody != null) ballBody.isKinematic = !IsAuthority;
        foreach (var pb in pinBodies)
        {
            if (pb != null) pb.isKinematic = !IsAuthority;
        }
    }

    // TurnManager.RPC_Launch가 마스터(또는 오프라인)에서만 호출
    public void Launch(Vector3 force, int shooter)
    {
        if (!IsAuthority || ballBody == null) return;

        pendingShooter = shooter;
        simulating = true;
        restTimer = 0f;
        ballBody.linearVelocity = Vector3.zero;
        ballBody.AddForce(force, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        if (!IsAuthority || !simulating) return;

        if (AllAtRest())
        {
            restTimer += Time.fixedDeltaTime;
            if (restTimer >= restCheckDuration)
            {
                simulating = false;
                ResolveTurn();
            }
        }
        else
        {
            restTimer = 0f;
        }
    }

    bool AllAtRest()
    {
        float sqrThreshold = restVelocityThreshold * restVelocityThreshold;

        if (ballBody != null && ballBody.linearVelocity.sqrMagnitude > sqrThreshold) return false;
        foreach (var pb in pinBodies)
        {
            if (pb != null && pb.linearVelocity.sqrMagnitude > sqrThreshold) return false;
        }
        return true;
    }

    void ResolveTurn()
    {
        int totalScore = 0;
        Vector3[] finalPositions = new Vector3[pinBodies.Length];

        for (int i = 0; i < pinBodies.Length; i++)
        {
            if (pinBodies[i] == null) continue;

            finalPositions[i] = pinBodies[i].position;
            float distance = Vector3.Distance(pinBodies[i].position, tableSpawner.PinStartPositions[i]);
            totalScore += PasaboloRayasScorer.ComputeScore(distance, tableSpawner.rayaDistances, rayaWeights);
        }

        turnManager?.ReportTurnResult(pendingShooter, totalScore, finalPositions);
    }
}
