using System.Runtime.CompilerServices;
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

    [Header("Upgrade Menu")]
    public GameObject upgradeMenuPanel;
    public Button openUpgradeButton;
    public Button backButton;
    public Button[] abilityUpgradeButtons = new Button[3]; //0=Regen, 1=Blur, 2=Hitline
    public TextMeshProUGUI[] abilityDescriptionTexts = new TextMeshProUGUI[3];

    [Header("Player Name Input")]
    public TMP_InputField playerNameInput;
    private const string PLAYER_NAME_KEY = "PlayerCustomName";

    [Header("Fullscreen Button")]
    public Toggle fullscreenToggle;
    //public TextMeshProUGUI fullscreenToggleText;
    private const string FULLSCREEN_KEY = "FullscreenEnabled";

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

        //Load saved fullscreen mode
        if (fullscreenToggle != null)
        {
            //Load saved preference (default = true / fullscreen)
            bool savedFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;
            Screen.fullScreen = savedFullscreen;
            fullscreenToggle.isOn = savedFullscreen;

            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        }

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

        if (openUpgradeButton != null)
            openUpgradeButton.onClick.AddListener(OpenUpgradeMenu);

        if (backButton != null)
            backButton.onClick.AddListener(CloseUpgradeMenu);

        for (int i = 0; i < 3; i++)
        {
            int index = i; // capture for lambda
            if (abilityUpgradeButtons[i] != null)
                abilityUpgradeButtons[i].onClick.AddListener(() => UpgradeAbility(index));
        }

        if (coinsText != null && CoinManager.Instance != null)
        {
            Debug.Log("Set coins text");
            coinsText.text = $"Coins: {CoinManager.Instance.GetCurrentCoins()}";
        }

        if (upgradeMenuPanel != null)
            upgradeMenuPanel.SetActive(false);

        //Load and setup player name input
        string savedName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
        if (playerNameInput != null)
        {
            playerNameInput.text = savedName;
            playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
        }
    }

    private void OnPlayerNameChanged(string newName)
    {
        //Save
        PlayerPrefs.SetString(PLAYER_NAME_KEY, newName);
        PlayerPrefs.Save();

        //Update the local NetworkPlayer
        if (NetworkPlayer.LocalPlayerInstance != null)
        {
            NetworkPlayer.LocalPlayerInstance.SetDisplayName(newName);
        }
    }

    public string GetCurrentPlayerName()
    {
        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
            return playerNameInput.text.Trim();

        return PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
    }

    private void OnEpilepsyToggleChanged(bool value)
    {
        PlayerPrefs.SetInt("EpilepsySafeMode", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnFullscreenToggleChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        //UpdateFullscreenToggleText();
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

        //Hide upgrade button when entering MP lobby
        LobbyManager.Instance?.EnterMultiplayerLobby();
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

        //Hide upgrade button when entering MP lobby
        LobbyManager.Instance?.EnterMultiplayerLobby();
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

    //UPGRADE MENU
    public void OpenUpgradeMenu()
    {
        if (upgradeMenuPanel == null) return;

        //Hide main lobby UI
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);
        if (epilepsyToggle != null) epilepsyToggle.gameObject.SetActive(false);
        //if (volumeSlider != null) volumeSlider.gameObject.SetActive(false);

        upgradeMenuPanel.SetActive(true);
        RefreshUpgradeUI();
    }

    public void CloseUpgradeMenu()
    {
        if (upgradeMenuPanel == null) return;

        upgradeMenuPanel.SetActive(false);

        //Show main lobby UI again
        hostButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        leaveButton.gameObject.SetActive(NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient);
        startGameButton.gameObject.SetActive(true);
        quitButton.gameObject.SetActive(true);
        if (epilepsyToggle != null) epilepsyToggle.gameObject.SetActive(true);
        //if (volumeSlider != null) volumeSlider.gameObject.SetActive(true);

        //Update coins in case they were spent
        if (coinsText != null && CoinManager.Instance != null)
            coinsText.text = $"Coins: {CoinManager.Instance.GetCurrentCoins()}";
    }

    public void SetUpgradeButtonActive(bool active)
    {
        if (openUpgradeButton != null)
            openUpgradeButton.gameObject.SetActive(active);
    }

    private void RefreshUpgradeUI()
    {
        if (AbilityUpgradeManager.Instance == null) return;

        for (int i = 0; i < 3; i++)
        {
            //Update description text
            if (abilityDescriptionTexts[i] != null)
                abilityDescriptionTexts[i].text = AbilityUpgradeManager.Instance.GetUpgradeDescription(i);

            //Enable/disable button
            if (abilityUpgradeButtons[i] != null)
                abilityUpgradeButtons[i].interactable = AbilityUpgradeManager.Instance.CanUpgrade(i);
        }

        //Update coins display
        if (coinsText != null && CoinManager.Instance != null)
            coinsText.text = $"Coins: {CoinManager.Instance.GetCurrentCoins()}";
    }

    private void UpgradeAbility(int index)
    {
        if (AbilityUpgradeManager.Instance == null) return;

        AbilityUpgradeManager.Instance.UpgradeAbility(index);
        RefreshUpgradeUI();
    }
}