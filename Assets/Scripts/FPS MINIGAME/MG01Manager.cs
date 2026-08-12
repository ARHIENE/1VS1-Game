using Photon.Pun;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

namespace FPSMinigame
{
public class MG01Manager : MonoBehaviourPunCallbacks
{
    public static MG01Manager Instance;

    [Header("라운드 설정")]
    public int roundsToWin = 3;
    public int maxRounds = 5;

    [Header("현재 점수")]
    public int myScore = 0;
    public int opponentScore = 0;
    public int currentRound = 0;

    private bool minigameEnded = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddMyScore()
    {
        if (minigameEnded) return;
        myScore++;
        currentRound++;
        UpdateScoreUI();
        photonView.RPC("RPC_SyncScore", RpcTarget.Others, myScore, opponentScore, currentRound);
        CheckRoundEnd();
    }

    public void AddOpponentScore()
    {
        if (minigameEnded) return;
        opponentScore++;
        currentRound++;
        UpdateScoreUI();
        photonView.RPC("RPC_SyncScore", RpcTarget.Others, opponentScore, myScore, currentRound);
        CheckRoundEnd();
    }

    [PunRPC]
    void RPC_SyncScore(int myS, int opponentS, int round)
    {
        myScore = myS;
        opponentScore = opponentS;
        currentRound = round;
        UpdateScoreUI();
        CheckRoundEnd();
    }

    void CheckRoundEnd()
    {
        if (minigameEnded) return;

        bool roundEnded = false;
        if (myScore >= roundsToWin) roundEnded = true;
        else if (opponentScore >= roundsToWin) roundEnded = true;
        else if (currentRound >= maxRounds) roundEnded = true;

        if (roundEnded && PhotonNetwork.IsMasterClient)
        {
            minigameEnded = true;
            bool iWin = myScore >= roundsToWin ||
                (currentRound >= maxRounds && myScore > opponentScore);
            photonView.RPC("OnMinigameResult", RpcTarget.All, iWin);
        }
    }

    [PunRPC]
    void OnMinigameResult(bool iWon)
    {
        minigameEnded = true;
        if (GameManager.Instance != null)
        {
            if (iWon)
            {
                Debug.Log("이 미니게임 승리!");
                GameManager.Instance.AddMyWin();
            }
            else
            {
                Debug.Log("이 미니게임 패배!");
                GameManager.Instance.AddOpponentWin();
            }
            Invoke("GoNextMinigame", 4f);
        }
        else
        {
            Debug.Log(iWon ? "승리!" : "패배!");
            Invoke("GoBackToHub", 4f);
        }
    }

    void GoNextMinigame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextMinigame();
        }
    }

    void GoBackToHub()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public void RespawnBoth()
    {
        Debug.Log("RespawnBoth 호출!");
        // Others만! 맞은 사람은 이미 Respawn 코루틴에서 이동함
        photonView.RPC("RPC_RespawnAll", RpcTarget.Others);
    }

    [PunRPC]
    void RPC_RespawnAll()
    {
        Debug.Log("RPC_RespawnAll 실행!");
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        Debug.Log($"찾은 플레이어 수: {players.Length}");

        foreach (PlayerHealth player in players)
        {
            Debug.Log($"플레이어: {player.name} IsMine: {player.photonView.IsMine}");
            if (player.photonView.IsMine)
            {
                Debug.Log("ForceRespawn 호출!");
                player.ForceRespawn();
            }
        }
    }

    void UpdateScoreUI()
    {
        ScoreUI scoreUI = FindFirstObjectByType<ScoreUI>();
        if (scoreUI != null)
            scoreUI.UpdateScore();
    }

    public string GetScoreText()
    {
        return $"Me: {myScore}  You: {opponentScore}  Round: {currentRound}/{maxRounds}";
    }
}
}