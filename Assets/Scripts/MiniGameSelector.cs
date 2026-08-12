using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class MiniGameSelector : MonoBehaviour
{
    private bool isLoading = false;

    public void LoadMiniGame(string sceneName)
    {
        if (isLoading) return;

        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            isLoading = true;
            Debug.Log($"[MiniGameSelector] Loading Level: {sceneName}");
            PhotonNetwork.LoadLevel(sceneName);
        }
        else if (!PhotonNetwork.IsConnected)
        {
            isLoading = true;
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.Log("Only the Master Client can start mini-games.");
        }
    }
}
