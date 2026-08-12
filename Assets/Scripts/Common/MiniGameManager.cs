using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MiniGameManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    [SerializeField] private Button returnToHubButton;

    private GameObject titleObject;
    private bool isPaused = false;

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError($"[MiniGameManager] Player Prefab is null in scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}!");
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }

        // Cache Title
        titleObject = GameObject.Find("Title");

        if (returnToHubButton == null)
        {
            GameObject btnGO = GameObject.Find("ReturnButton");
            if (btnGO != null) returnToHubButton = btnGO.GetComponent<Button>();
        }

        if (returnToHubButton != null)
        {
            returnToHubButton.onClick.AddListener(ReturnToHub);
        }

        // Hide pause UI initially so it doesn't block the screen during gameplay
        SetPauseUIActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        SetPauseUIActive(isPaused);

        // Toggle cursor state based on pause menu visibility
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName == "MiniGame1")
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void SetPauseUIActive(bool active)
    {
        if (titleObject != null)
        {
            titleObject.SetActive(active);
        }

        if (returnToHubButton != null)
        {
            // Only show Return to Hub button to the MasterClient when paused
            returnToHubButton.gameObject.SetActive(active && PhotonNetwork.IsMasterClient);
        }
    }

    private void SpawnPlayer()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Vector3 spawnPos = Vector3.up * 51f; // Default height for mini-games

        if (sceneName == "MiniGame_LavaRiver")
        {
            // Spawn on left or right cliff based on actor number
            float side = (PhotonNetwork.LocalPlayer.ActorNumber % 2 == 0) ? 1f : -1f;
            spawnPos = new Vector3(140f * side, 75.5f, Random.Range(-5f, 5f));
        }
        else
        {
            spawnPos = new Vector3(Random.Range(-5f, 5f), 2f, Random.Range(-5f, 5f));
        }

        Debug.Log($"[MiniGameManager] Spawning player {PhotonNetwork.LocalPlayer.NickName} at {spawnPos}");
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }

    public void ReturnToHub()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[MiniGameManager] Master Client returning to Hub");
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}
