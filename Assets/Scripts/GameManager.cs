using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    
    [Header("MiniGame UI")]
    public GameObject selectionPanel;
    public Button miniGame1Button;
    public Button miniGame2Button;

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError($"[GameManager] Player Prefab is null!");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (PhotonNetwork.LocalPlayer.TagObject != null) return; // Already spawned

        Debug.Log($"[GameManager] Spawning player {PhotonNetwork.LocalPlayer.NickName}");
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 2f, Random.Range(-3f, 3f));
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
        PhotonNetwork.LocalPlayer.TagObject = player;

        // UI is handled by MiniGameSelector persistent listeners
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    // Removed LoadMiniGame as it is handled by MiniGameSelector component
}
