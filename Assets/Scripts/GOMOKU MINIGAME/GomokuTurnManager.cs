using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;

public enum GomokuGameState { Playing, Win, Draw }

// 턴 순서 관리 + 게임 상태 관리 + Grid/WinChecker 호출
// 배치는 결정론적 로직이므로, RPC로 (x,y,player)만 동기화하면 양쪽 클라이언트가 동일한 결과에 도달한다
public class GomokuTurnManager : MonoBehaviourPun
{
    public static GomokuTurnManager Instance { get; private set; }

    [Header("References")]
    public GomokuStoneSpawner stoneSpawner;
    public GomokuVisualHighlighter highlighter;

    [Header("UI (선택, 비워두면 무시)")]
    public TextMeshProUGUI turnText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    public GomokuGrid Grid { get; private set; }
    public int CurrentPlayer { get; private set; } = 1;
    public GomokuGameState State { get; private set; } = GomokuGameState.Playing;

    // 로컬 클라이언트가 조작하는 플레이어 번호. 오프라인(미접속)이면 한 화면에서 번갈아 두는 핫싯 모드로 취급
    public int LocalPlayer { get; private set; } = 1;

    // 상대방 아바타 머리 회전 동기화 (pitch, yaw) — GomokuSeatedCamera가 구독해서 상대 아바타 머리에 반영
    public event System.Action<float, float> OnOpponentHeadLook;

    private float headSyncTimer;
    private const float HeadSyncInterval = 0.05f; // 초당 약 20회

    // 보드 회전 상태(0~3, 90도 단위). 현재 턴인 로컬 플레이어만 돌릴 수 있음
    private int boardRotationSteps = 0;

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

        Grid = new GomokuGrid();

        if (stoneSpawner == null) stoneSpawner = FindAnyObjectByType<GomokuStoneSpawner>();
        if (highlighter == null) highlighter = FindAnyObjectByType<GomokuVisualHighlighter>();

        // Awake에서 확정: 다른 스크립트(GomokuSeatedCamera 등)가 자신의 Start()에서
        // Unity의 Awake-전체완료-후-Start 순서 보장을 믿고 안전하게 참조할 수 있어야 함
        LocalPlayer = (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) ? 2 : 1;
    }

    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        UpdateTurnUI();
    }

    public bool IsLocalTurn()
    {
        if (State != GomokuGameState.Playing) return false;
        if (!PhotonNetwork.IsConnected) return true;
        return CurrentPlayer == LocalPlayer;
    }

    // GomokuInputHandler가 (x,y) 기둥 클릭 시 호출
    public void RequestPlace(int x, int y)
    {
        if (!IsLocalTurn()) return;
        if (!Grid.CanPlace(x, y)) return;

        int player = PhotonNetwork.IsConnected ? LocalPlayer : CurrentPlayer;

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC(nameof(RPC_PlaceStone), RpcTarget.All, x, y, player);
        }
        else
        {
            RPC_PlaceStone(x, y, player);
        }
    }

    [PunRPC]
    void RPC_PlaceStone(int x, int y, int player)
    {
        if (State != GomokuGameState.Playing) return;
        if (player != CurrentPlayer) return; // 턴 순서가 어긋난 요청 방어

        if (!Grid.TryPlace(x, y, player, out Vector3Int placed)) return;

        stoneSpawner?.SpawnStone(placed, player);

        if (GomokuWinChecker.CheckWin(Grid, placed, player, out List<Vector3Int> winLine))
        {
            State = GomokuGameState.Win;
            highlighter?.HighlightWinLine(winLine);
            ShowResult($"{PlayerLabel(player)} 승리!");
            return;
        }

        if (Grid.IsFull())
        {
            State = GomokuGameState.Draw;
            ShowResult("무승부");
            return;
        }

        CurrentPlayer = (player == 1) ? 2 : 1;
        UpdateTurnUI();
    }

    // GomokuSeatedCamera가 매 프레임 자신의 pitch/yaw를 보고 → 접속 중일 때만, 너무 잦지 않게(HeadSyncInterval) RPC로 상대에게 전파
    public void ReportLocalHeadLook(float pitch, float yaw)
    {
        if (!PhotonNetwork.IsConnected) return;

        headSyncTimer += Time.deltaTime;
        if (headSyncTimer < HeadSyncInterval) return;
        headSyncTimer = 0f;

        photonView.RPC(nameof(RPC_UpdateHeadLook), RpcTarget.Others, pitch, yaw);
    }

    [PunRPC]
    void RPC_UpdateHeadLook(float pitch, float yaw)
    {
        OnOpponentHeadLook?.Invoke(pitch, yaw);
    }

    // GomokuInputHandler가 Q/E 입력 시 호출. direction: -1(왼쪽/반시계) 또는 +1(오른쪽/시계)
    public void RequestRotateBoard(int direction)
    {
        if (!IsLocalTurn()) return;

        int newSteps = ((boardRotationSteps + direction) % 4 + 4) % 4;

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC(nameof(RPC_RotateBoard), RpcTarget.All, newSteps);
        }
        else
        {
            RPC_RotateBoard(newSteps);
        }
    }

    [PunRPC]
    void RPC_RotateBoard(int steps)
    {
        boardRotationSteps = steps;
        stoneSpawner?.SetBoardRotation(steps);
    }

    string PlayerLabel(int player) => player == 1 ? "플레이어 1" : "플레이어 2";

    void UpdateTurnUI()
    {
        if (turnText == null) return;
        bool mine = IsLocalTurn();
        turnText.text = $"{PlayerLabel(CurrentPlayer)} 차례" + (mine ? " (내 턴)" : "");
    }

    void ShowResult(string message)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
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
