using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public Toggle epilepsyToggle;
    public Slider volumeSlider;

    public Button hostButton;
    public Button joinButton;
    public Button leaveButton;
    public Button startGameButton;
    public Button quitButton;

    public TextMeshProUGUI coinsText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
        leaveButton.onClick.AddListener(LobbyManager.Instance.LeaveLobby);
        startGameButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(QuitGame);

        hostButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        leaveButton.gameObject.SetActive(false);
        startGameButton.interactable = false; //only host

        //Load saved epilepsy mode
        bool savedValue = PlayerPrefs.GetInt("EpilepsySafeMode", 0) == 1;
        epilepsyToggle.isOn = savedValue;
        epilepsyToggle.onValueChanged.AddListener(OnEpilepsyToggleChanged);

        //Load saved volume
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (coinsText != null && CoinManager.Instance != null)
        {
            Debug.Log("Set coins text");
            coinsText.text = $"Coins: {CoinManager.Instance.GetCurrentCoins()}";
        }
    }

    private void OnEpilepsyToggleChanged(bool value)
    {
        PlayerPrefs.SetInt("EpilepsySafeMode", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();

        //Apply volume to listener and now playing audio
        AudioListener.volume = value;
    }

    private void StartHost()
    {
        if (!NetworkManager.Singleton.StartHost()) return;

        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(true);
        //Force host UI rebuild so "Host" appears immediately
        LobbyManager.Instance?.RebuildLobbyUI();

        UpdateStartButton();
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

        int currentPlayers = FindObjectsOfType<NetworkPlayer>().Length;
        if(currentPlayers != 2)
        {
            //startGameButton.interactable = false;
            return;
        }

        //startGameButton.interactable = true;
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        UpdateStartButton();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        UpdateStartButton();
    }

    private void UpdateStartButton()
    {
        if (startGameButton == null) return;

        int currentPlayers = FindObjectsOfType<NetworkPlayer>().Length;
        bool canStart = NetworkManager.Singleton.IsHost && currentPlayers == 2;
        startGameButton.interactable = canStart;
    }
}