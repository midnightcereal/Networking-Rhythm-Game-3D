using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public Button leaveButton;
    public Button startGameButton;

    private void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
        leaveButton.onClick.AddListener(LobbyManager.Instance.LeaveLobby);
        startGameButton.onClick.AddListener(StartGame);

        hostButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        leaveButton.gameObject.SetActive(false);
        startGameButton.interactable = false; //only host
    }

    private void StartHost()
    {
        if (!NetworkManager.Singleton.StartHost()) return;

        startGameButton.interactable = true;
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(true);
        //Force host UI rebuild so "Host" appears immediately
        LobbyManager.Instance?.RebuildLobbyUI();
    }

    private void StartClient()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = "127.0.0.1";
        transport.ConnectionData.Port = 7777;

        if (!NetworkManager.Singleton.StartClient()) return;

        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(true);
        startGameButton.gameObject.SetActive(false);
    }

    private void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}