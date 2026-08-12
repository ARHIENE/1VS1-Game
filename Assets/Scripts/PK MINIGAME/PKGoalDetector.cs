using UnityEngine;
using Photon.Pun;

// 슛 결과 판정 담당 (물리 충돌이 아닌 실제 궤적 종착점 기준)
// 실제 슛 궤적의 골라인 통과 지점 → 1~6번 구역 계산 → 골키퍼 선택 구역과 비교
public class PKGoalDetector : MonoBehaviourPun
{
    [Header("References")]
    public PKGameManager gameManager;

    private bool goalDecided = false;

    public void ResetDecision() => goalDecided = false;

    // realTrajectory: 오차가 적용된 최종 궤적, gkZone: 골키퍼가 선택한 구역(1~6)
    public void EvaluateShot(Vector3[] realTrajectory, int gkZone)
    {
        if (goalDecided) return;
        if (!PhotonNetwork.IsMasterClient) return;

        if (realTrajectory == null || realTrajectory.Length == 0)
        {
            Decide(false, "미스");
            return;
        }

        Vector3 endPoint   = realTrajectory[realTrajectory.Length - 1];
        Vector3 goalCenter = gameManager.goalCenter;
        float   halfWidth  = gameManager.goalHalfWidth;
        float   height     = gameManager.goalHeight;

        if (!PKGoalZones.IsInsideGoal(endPoint, goalCenter, halfWidth, height))
        {
            Decide(false, "빗나감");
            return;
        }

        int  shotZone = PKGoalZones.GetZoneFromPosition(endPoint, goalCenter, halfWidth, height);
        bool blocked  = shotZone == gkZone;

        Decide(!blocked, blocked ? "선방!" : "골!");
    }

    public void DeclareNoGoal(string reason)
    {
        if (goalDecided) return;
        Decide(false, reason);
    }

    void Decide(bool isGoal, string reason)
    {
        goalDecided = true;
        photonView.RPC(nameof(RPC_Result), RpcTarget.All, isGoal, reason);
    }

    [PunRPC]
    void RPC_Result(bool isGoal, string reason)
    {
        gameManager?.OnGoalResult(isGoal, reason);
    }
}
