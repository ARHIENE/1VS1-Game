using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;

public enum PasaboloGameState { Playing, Finished }

// 턴/라운드 진행 + 점수 누적 관리. 실제 물리는 PasaboloPhysicsController(마스터 권위)가 담당하고,
// 이 매니저는 GomokuTurnManager처럼 발사 요청을 RPC로 중계하고 결과를 받아 점수/턴을 갱신한다.
public class PasaboloTurnManager : MonoBehaviourPun
{
    public static PasaboloTurnManager Instance { get; private set; }

    [Header("References")]
    public PasaboloPhysicsController physicsController;
    public PasaboloTableSpawner tableSpawner;

    [Header("Rules")]
    [Tooltip("한 플레이어당 턴(발사) 횟수. 스펙상 3~5 사이 조정 가능")]
    public int totalRounds = 3;

    [Header("UI (선택, 비워두면 무시)")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI scoreText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    public int CurrentPlayer { get; private set; } = 1;
    public PasaboloGameState State { get; private set; } = PasaboloGameState.Playing;

    // 로컬 클라이언트가 조작하는 플레이어 번호. 오프라인(미접속)이면 핫싯 모드로 취급
    public int LocalPlayer { get; private set; } = 1;

    public int ScoreP1 { get; private set; }
    public int ScoreP2 { get; private set; }
    private int turnsP1;
    private int turnsP2;

    // 발사/정지 이벤트 — PasaboloCameraController가 구독해 Aim/Flight/Result 시점 전환에 사용
    public event System.Action OnShotFired;
    public event System.Action OnShotSettled;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (physicsController == null) physicsController = FindAnyObjectByType<PasaboloPhysicsController>();
        if (tableSpawner == null) tableSpawner = FindAnyObjectByType<PasaboloTableSpawner>();

        LocalPlayer = (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) ? 2 : 1;
    }

    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        UpdateTurnUI();
    }

    public bool IsLocalTurn()
    {
        if (State != PasaboloGameState.Playing) return false;
        if (!PhotonNetwork.IsConnected) return true;
        return CurrentPlayer == LocalPlayer;
    }

    // PasaboloLaunchController가 슬링샷 릴리즈 시 호출
    public void RequestLaunch(Vector3 force)
    {
        if (!IsLocalTurn()) return;

        int shooter = CurrentPlayer;

        if (PhotonNetwork.IsConnected)
        {
            // 카메라 전환(Aim->Flight)은 양쪽 모두 필요하므로 RpcTarget.All로 브로드캐스트하되,
            // 실제 AddForce는 RPC_Launch 내부에서 마스터/오프라인일 때만 실행
            photonView.RPC(nameof(RPC_Launch), RpcTarget.All, force, shooter);
        }
        else
        {
            RPC_Launch(force, shooter);
        }
    }

    [PunRPC]
    void RPC_Launch(Vector3 force, int shooter)
    {
        OnShotFired?.Invoke();

        // 실시간 물리를 양쪽이 각각 재현하면 프레임/타이밍 차이로 결과가 어긋날 수 있어
        // 마스터(또는 오프라인 핫싯)만 실제 시뮬레이션 권위를 갖는다
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
        physicsController?.Launch(force, shooter);
    }

    // PasaboloPhysicsController가 정지 감지 후 호출(마스터/오프라인에서만)
    public void ReportTurnResult(int shooter, int turnScore, Vector3[] finalPinPositions)
    {
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC(nameof(RPC_TurnResult), RpcTarget.All, shooter, turnScore, finalPinPositions);
        }
        else
        {
            RPC_TurnResult(shooter, turnScore, finalPinPositions);
        }
    }

    [PunRPC]
    void RPC_TurnResult(int shooter, int turnScore, Vector3[] finalPinPositions)
    {
        if (State != PasaboloGameState.Playing) return;

        if (shooter == 1) { ScoreP1 += turnScore; turnsP1++; }
        else { ScoreP2 += turnScore; turnsP2++; }

        // 마스터가 계산한 최종 핀 위치로 하드 스냅(PhotonTransformView 드리프트 보정) 후 다음 턴 위치로 리셋
        tableSpawner?.SnapPinsToPositions(finalPinPositions);
        tableSpawner?.ResetForNextTurn();

        OnShotSettled?.Invoke();

        if (turnsP1 >= totalRounds && turnsP2 >= totalRounds)
        {
            State = PasaboloGameState.Finished;
            string msg = ScoreP1 == ScoreP2 ? "무승부"
                : (ScoreP1 > ScoreP2 ? "플레이어 1 승리!" : "플레이어 2 승리!");
            ShowResult(msg);
            return;
        }

        CurrentPlayer = (shooter == 1) ? 2 : 1;
        UpdateTurnUI();
    }

    string PlayerLabel(int player) => player == 1 ? "플레이어 1" : "플레이어 2";

    void UpdateTurnUI()
    {
        if (turnText != null)
        {
            bool mine = IsLocalTurn();
            turnText.text = $"{PlayerLabel(CurrentPlayer)} 차례 ({turnsP1}/{totalRounds} - {turnsP2}/{totalRounds})" + (mine ? " (내 턴)" : "");
        }

        if (scoreText != null)
        {
            scoreText.text = $"P1 {ScoreP1} : {ScoreP2} P2";
        }
    }

    void ShowResult(string message)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
        UpdateTurnUI();
    }

    public void ReturnToHub()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene("GameScene");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}
