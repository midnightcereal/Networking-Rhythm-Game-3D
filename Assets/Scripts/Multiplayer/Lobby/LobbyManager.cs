using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("UI References")]
    public Transform playerListParent;
    public GameObject playerNameTextPrefab;
    public TextMeshProUGUI playerCountText;

    [Header("Settings")]
    public int maxPlayers = 2;

    private readonly List<NetworkPlayer> localPlayers = new();
    private readonly List<GameObject> playerUIObjects = new(); //track instantiated UI objects

    private bool hasLocalPlayerSpawned = false;

    private void Awake()
    {
        Instance = this;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        //Only run this check after local player has spawned
        if (!hasLocalPlayerSpawned)
        {
            NetworkPlayer localPlayer = FindLocalPlayer();
            if (localPlayer != null)
                hasLocalPlayerSpawned = true;
            else
                return; //wait until local player exists
        }

        //Count all NetworkPlayer prefabs in the scene
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();

        if (NetworkManager.Singleton.IsHost)
        {
            if (players.Length == 1)
            {
                RebuildLobbyUI();
            }
        }
        else
        {
            //If no players exist, host left or lobby ended
            if (players.Length == 0)
            {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Lobby");
            }
        }
    }

    private NetworkPlayer FindLocalPlayer()
    {
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (var p in players)
        {
            if (p.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                return p;
        }
        return null;
    }

    #region Network Callbacks

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            RebuildLobbyUI();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            RebuildLobbyUI();
        }
    }

    #endregion

    #region Player Tracking

    public void PlayerSpawned(NetworkPlayer player)
    {
        if (!localPlayers.Contains(player))
            localPlayers.Add(player);

        RebuildLobbyUI();
    }

    public void PlayerDespawned(NetworkPlayer player)
    {
        if (localPlayers.Contains(player))
            localPlayers.Remove(player);

        RebuildLobbyUI();
    }

    #endregion

    #region UI Handling

    public void RebuildLobbyUI()
    {
        //Destroy old UI
        foreach (var uiObj in playerUIObjects)
            if (uiObj != null)
                Destroy(uiObj);
        playerUIObjects.Clear();

        //Update player list
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        localPlayers.Clear();
        localPlayers.AddRange(players);

        foreach (var p in localPlayers)
        {
            GameObject textObj = Instantiate(playerNameTextPrefab, playerListParent);
            playerUIObjects.Add(textObj);

            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string display = p.DisplayName.Value.ToString();
                if (p.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                    display += " (You)";
                textComponent.text = display;
            }
        }

        UpdatePlayerCount();
    }

    public void UpdatePlayerCount()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {localPlayers.Count}/{maxPlayers}";
        }
    }

    #endregion

    #region Leave Lobby

    public void LeaveLobby()
    {
        //Clear UI immediately
        foreach (var uiObj in playerUIObjects)
            if (uiObj != null)
                Destroy(uiObj);
        playerUIObjects.Clear();

        playerCountText.text = "";

        if (NetworkManager.Singleton.IsHost)
        {
            //Host shuts down server -> clients detect 0 players
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
        else
        {
            //Client leaves
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    }

    #endregion
}