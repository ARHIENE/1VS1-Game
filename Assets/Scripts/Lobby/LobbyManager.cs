using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Menu UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;

    [Header("Room UI")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TMP_Text roomInfoText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    [Header("Status UI")]
    [SerializeField] private TMP_Text networkStatusText;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        menuPanel.SetActive(true);
        roomPanel.SetActive(false);

        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
        startGameButton.onClick.AddListener(StartGame);
        leaveButton.onClick.AddListener(LeaveRoom);

        if (!PhotonNetwork.IsConnected)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LauncherScene");
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsConnected && networkStatusText != null)
        {
            string region = PhotonNetwork.CloudRegion;
            int ping = PhotonNetwork.GetPing();
            networkStatusText.text = $"Region: {region} | Ping: {ping}ms";
        }
    }

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        menuPanel.SetActive(false);
        roomPanel.SetActive(true);
        UpdateRoomUI();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        UpdateRoomUI();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            roomInfoText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}\nPlayers: {PhotonNetwork.CurrentRoom.PlayerCount}";
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        menuPanel.SetActive(true);
        roomPanel.SetActive(false);
    }
}
