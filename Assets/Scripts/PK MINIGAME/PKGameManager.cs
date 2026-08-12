using System.Collections;
using UnityEngine;
using Photon.Pun;

public class PKGameManager : MonoBehaviourPun
{
    public static PKGameManager Instance { get; private set; }

    [Header("References")]
    public PKBall            ball;
    public PKGoalDetector    goalDetector;
    public PKKicker          kicker;
    public PKGoalkeeper      goalkeeper;
    public PKGoalkeeperCursor gkCursor;
    public PKUIManager       uiManager;
    public Transform         ballSpawnPoint;

    [Header("Goal Zones (2행 3열, 1~6)")]
    public Vector3 goalCenter    = new Vector3(0f, 1.22f, 10f);
    public float   goalHalfWidth = 3.66f;
    public float   goalHeight    = 2.44f;

    [Header("Rules")]
    public int   roundsPerTeam      = 5;
    public float turnTimeLimit      = 10f;
    public float ballResultTimeout  = 5f;
    public float resultDisplayTime  = 2.5f;

    [Header("Camera (역할 무관 고정 시점)")]
    public Vector3 cameraPosition    = new Vector3(0f, 2.8f, -7.5f);
    public Vector3 cameraEulerAngles = new Vector3(18f, 0f, 0f);

    private int  scoreA = 0, scoreB = 0;
    private int  kicksA = 0, kicksB = 0;

    private bool isTeamATurn   = true;
    private bool inSuddenDeath = false;
    private int  sdPair        = 0;
    private bool kickExecuted  = false;

    private bool kickerReady = false;
    private bool goalkeeperReady = false;
    private Vector3[] lockedRealTrajectory = null;
    private int        lockedGkZone = 0;

    private Coroutine resultWaitCoroutine;

    public enum Team { A, B }
    public Team localTeam = Team.A;

    public bool IsLocalKicker =>
        (isTeamATurn && localTeam == Team.A) ||
        (!isTeamATurn && localTeam == Team.B);

    public bool IsKickExecuted => kickExecuted;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterLocalPlayer(PKKicker localKicker, PKGoalkeeper localGoalkeeper)
    {
        kicker = localKicker;
        goalkeeper = localGoalkeeper;
    }

    public bool IsTeamATurn => isTeamATurn;

    public PKGoalkeeper GetDefendingGoalkeeper()
    {
        PKGoalkeeper[] gks = FindObjectsByType<PKGoalkeeper>(FindObjectsSortMode.None);
        if (gks.Length == 0) return goalkeeper;
        if (gks.Length == 1) return gks[0];

        foreach (var gk in gks)
        {
            if (gk.photonView == null || gk.photonView.Owner == null) continue;
            bool isMaster = gk.photonView.Owner.IsMasterClient;
            if (isTeamATurn)
            {
                if (!isMaster) return gk;
            }
            else
            {
                if (isMaster) return gk;
            }
        }
        return goalkeeper != null ? goalkeeper : gks[0];
    }

    public PKKicker GetActiveKicker()
    {
        PKKicker[] kickers = FindObjectsByType<PKKicker>(FindObjectsSortMode.None);
        if (kickers.Length == 0) return kicker;
        if (kickers.Length == 1) return kickers[0];

        foreach (var k in kickers)
        {
            if (k.photonView == null || k.photonView.Owner == null) continue;
            bool isMaster = k.photonView.Owner.IsMasterClient;
            if (isTeamATurn)
            {
                if (isMaster) return k;
            }
            else
            {
                if (!isMaster) return k;
            }
        }
        return kicker != null ? kicker : kickers[0];
    }

    void Start()
    {
        localTeam = (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) ? Team.B : Team.A;
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.5f);
        BeginTurn();
    }

    void BeginTurn()
    {
        kickExecuted    = false;
        kickerReady     = false;
        goalkeeperReady = false;
        lockedRealTrajectory = null;
        lockedGkZone    = 0;

        ball.ResetBall(ballSpawnPoint != null ? ballSpawnPoint.position : new Vector3(0f, 0.11f, -1.0f));
        if (goalDetector != null) goalDetector.ResetDecision();

        // Dynamically find local player references if not registered yet
        if (kicker == null || goalkeeper == null)
        {
            PKKicker[] kickers = FindObjectsByType<PKKicker>(FindObjectsSortMode.None);
            foreach (var k in kickers)
            {
                if (k.photonView == null || k.photonView.IsMine)
                {
                    kicker = k;
                    goalkeeper = k.GetComponent<PKGoalkeeper>();
                    break;
                }
            }
        }

        bool localIsKicker = IsLocalKicker;

        // Takeover ownership of the ball to make sure our physics/RPCs work authoritatively
        if (localIsKicker && PhotonNetwork.IsConnected && ball != null && ball.photonView != null)
        {
            ball.photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }

        // --- 카메라: 역할과 무관하게 항상 공격자 뒤에서 골대를 보는 고정 시점 ---
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow != null) camFollow.enabled = false;

            mainCam.transform.position = cameraPosition;
            mainCam.transform.rotation = Quaternion.Euler(cameraEulerAngles);
        }

        // Position ALL player GameObjects according to their current turn roles
        PKKicker curKicker = GetActiveKicker();
        PKKicker[] allKickers = FindObjectsByType<PKKicker>(FindObjectsSortMode.None);
        foreach (var k in allKickers)
        {
            CharacterController cc = k.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (k == curKicker)
            {
                k.transform.position = new Vector3(0f, 0f, -3.0f);
                k.transform.rotation = Quaternion.identity; // Facing +z (at goal)
            }
            else
            {
                k.transform.position = new Vector3(0f, 0f, 10.0f);
                k.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Facing -z (at kicker)
            }
        }

        if (kicker != null) kicker.IsMyTurn    = localIsKicker;
        if (goalkeeper != null) goalkeeper.IsMyTurn = !localIsKicker;

        // 골키퍼 구역 선택 UI: 골키퍼 턴인 로컬 플레이어에게만 표시, 공격자에게는 절대 비노출
        gkCursor?.SetActive(!localIsKicker);

        uiManager?.UpdateTurnUI(localIsKicker, isTeamATurn,
            scoreA, scoreB, kicksA, kicksB, inSuddenDeath, sdPair);
    }

    public void SetKickerReady(Vector3[] realTrajectory)
    {
        if (uiManager != null)
        {
            uiManager.ShowWaitingForOpponent(true);
        }
        photonView.RPC(nameof(RPC_SetKickerReady), RpcTarget.All, realTrajectory);
    }

    [PunRPC]
    void RPC_SetKickerReady(Vector3[] realTrajectory)
    {
        kickerReady = true;
        lockedRealTrajectory = realTrajectory;
        CheckAndExecutePK();
    }

    public void SetGoalkeeperReady(int zone)
    {
        if (uiManager != null)
        {
            uiManager.ShowWaitingForOpponent(false);
        }
        photonView.RPC(nameof(RPC_SetGoalkeeperReady), RpcTarget.All, zone);
    }

    [PunRPC]
    void RPC_SetGoalkeeperReady(int zone)
    {
        goalkeeperReady = true;
        lockedGkZone = zone;
        CheckAndExecutePK();
    }

    void CheckAndExecutePK()
    {
        if (kickerReady && goalkeeperReady)
        {
            kickExecuted = true;
            gkCursor?.SetActive(false);

            if (resultWaitCoroutine != null) StopCoroutine(resultWaitCoroutine);
            resultWaitCoroutine = StartCoroutine(ExecutePKRoutine());
        }
    }

    IEnumerator ExecutePKRoutine()
    {
        // 1. Play run-up and kick animation on the active kicker
        PKKicker activeKicker = GetActiveKicker();
        if (activeKicker != null)
        {
            Animator kickerAnim = activeKicker.GetComponent<Animator>();
            if (kickerAnim == null) kickerAnim = activeKicker.GetComponentInChildren<Animator>();
            if (kickerAnim != null)
            {
                kickerAnim.SetTrigger("SoccerKick");
            }
        }

        // Wait for run-up (around 0.35s is perfect for mixamo penalty kick animation contact point)
        yield return new WaitForSeconds(0.35f);

        // 2. Launch the ball along the resolved (오차 적용된) trajectory
        if (ball != null)
        {
            ball.KickAlongPath(lockedRealTrajectory);
        }

        // 3. Make goalkeeper dive with a very slight reaction delay (0.1s is very realistic!)
        yield return new WaitForSeconds(0.1f);
        PKGoalkeeper activeGk = GetDefendingGoalkeeper();
        if (activeGk != null)
        {
            activeGk.ExecuteDive(lockedGkZone);
        }

        // 4. Wait for the ball to finish traveling its path (with a safety timeout)
        float elapsed = 0f;
        while (ball != null && ball.IsMoving && elapsed < ballResultTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 5. Evaluate result from the resolved trajectory endpoint vs goalkeeper's chosen zone
        goalDetector.EvaluateShot(lockedRealTrajectory, lockedGkZone);
    }

    public void OnGoalResult(bool isGoal, string reason)
    {
        if (resultWaitCoroutine != null) StopCoroutine(resultWaitCoroutine);

        if (isGoal) { if (isTeamATurn) scoreA++; else scoreB++; }
        if (isTeamATurn) kicksA++; else kicksB++;

        uiManager?.ShowResult(isGoal, reason, scoreA, scoreB);
        StartCoroutine(AfterResult());
    }

    IEnumerator AfterResult()
    {
        yield return new WaitForSeconds(resultDisplayTime);
        if (!inSuddenDeath && CheckEarlyEnd()) yield break;
        if (CheckMatchEnd()) yield break;
        isTeamATurn = !isTeamATurn;
        BeginTurn();
    }

    bool CheckEarlyEnd()
    {
        int remA = roundsPerTeam - kicksA;
        int remB = roundsPerTeam - kicksB;
        bool aWon = scoreA > scoreB + remB;
        bool bWon = scoreB > scoreA + remA;
        if (!aWon && !bWon) return false;
        StartCoroutine(EndMatch(aWon ? Team.A : Team.B, "조기 종료"));
        return true;
    }

    bool CheckMatchEnd()
    {
        if (!inSuddenDeath)
        {
            if (kicksA < roundsPerTeam || kicksB < roundsPerTeam) return false;
            if (scoreA != scoreB)
            {
                StartCoroutine(EndMatch(scoreA > scoreB ? Team.A : Team.B, "정규전"));
                return true;
            }
            inSuddenDeath = true; sdPair = 0; isTeamATurn = true;
            uiManager?.ShowSuddenDeath();
            BeginTurn();
            return true;
        }
        else
        {
            int sdA = kicksA - roundsPerTeam;
            int sdB = kicksB - roundsPerTeam;
            if (sdA <= sdPair || sdB <= sdPair) return false;
            if (scoreA != scoreB)
            {
                StartCoroutine(EndMatch(scoreA > scoreB ? Team.A : Team.B, "서든데스"));
                return true;
            }
            sdPair++; isTeamATurn = true;
            BeginTurn();
            return true;
        }
    }

    IEnumerator EndMatch(Team winner, string reason)
    {
        yield return new WaitForSeconds(0.5f);
        uiManager?.ShowMatchResult(winner == localTeam, scoreA, scoreB, reason);
    }
}
