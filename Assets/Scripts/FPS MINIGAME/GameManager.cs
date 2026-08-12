using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace FPSMinigame
{
    public class GameManager : MonoBehaviour
    {
    public static GameManager Instance;

    [Header("전체 게임 승리 횟수")]
    public int myWins = 0;
    public int opponentWins = 0;

    private int currentGameIndex = 0;

    private string[] minigameScenes = {
        "MG_01",
        "MG_02",
        "MG_03",
        "MG_04",
        "MG_05"
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void AddMyWin()
    {
        myWins++;
        Debug.Log($"내 승리! myWins: {myWins}");
    }

    public void AddOpponentWin()
    {
        opponentWins++;
        Debug.Log($"상대 승리! opponentWins: {opponentWins}");
    }

    public void LoadNextMinigame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (currentGameIndex < minigameScenes.Length)
        {
            PhotonNetwork.LoadLevel(minigameScenes[currentGameIndex]);
            currentGameIndex++;
        }
        else
        {
            PhotonNetwork.LoadLevel("Result");
        }
    }

    public string GetWinsText()
    {
        return $"Me: {myWins}  You: {opponentWins}";
    }

    public void LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
    }
}
}