using UnityEngine;
using Photon.Pun;
using TMPro;

public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        // Increase network update rates for smoother movement
        PhotonNetwork.SendRate = 60; 
        PhotonNetwork.SerializationRate = 45; 
        
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        statusText.text = "Connecting to Photon...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        // Set a random nickname if none is set
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
        }
        
        statusText.text = $"Connected as {PhotonNetwork.NickName}! Loading Lobby...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }
}
