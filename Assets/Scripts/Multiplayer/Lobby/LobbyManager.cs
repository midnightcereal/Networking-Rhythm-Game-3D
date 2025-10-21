using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("UI References")]
    public Transform playerListParent;
    public GameObject playerNameTextPrefab;
    public TextMeshProUGUI playerCountText;

    [Header("Settings")]
    public int maxPlayers = 2;

    //Track all locally spawned NetworkPlayers
    private readonly List<NetworkPlayer> spawnedPlayers = new();

    private void Awake()
    {
        Instance = this;

        //Subscribe to client connect/disconnect
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

    #region Client Connect/Disconnect

    private void OnClientConnected(ulong clientId)
    {
        //if (!NetworkManager.Singleton.IsServer) return;

        ////Check max players
        //if (NetworkManager.Singleton.ConnectedClients.Count > maxPlayers)
        //{
        //    Debug.LogWarning("Max players reached! Rejecting connection.");
        //    NetworkManager.Singleton.DisconnectClient(clientId);
        //    return;
        //}

        ////Spawn NetworkPlayer for the joining client
        //var playerObj = Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab);
        //playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        //Clean up UI when a player leaves
        RebuildLobbyUI();
        UpdatePlayerCount();
    }

    #endregion

    #region Spawn/Despawn Handling

    /// <summary>
    /// Called by NetworkPlayer when it spawns locally
    /// </summary>
    /// <param name="player"></param>
    public void PlayerSpawned(NetworkPlayer player)
    {
        if (!spawnedPlayers.Contains(player))
            spawnedPlayers.Add(player);

        RebuildLobbyUI();
        UpdatePlayerCount();
    }

    /// <summary>
    /// Called by NetworkPlayer when it despawns
    /// </summary>
    /// <param name="player"></param>
    public void PlayerDespawned(NetworkPlayer player)
    {
        if (spawnedPlayers.Contains(player))
            spawnedPlayers.Remove(player);

        RebuildLobbyUI();
        UpdatePlayerCount();
    }

    /// <summary>
    /// ClientRpc to tell clients to leave
    /// </summary>
    [ClientRpc]
    private void DisconnectClientsClientRpc()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            //Cleanly shut down network client
            NetworkManager.Singleton.Shutdown();

            //Reload lobby scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    #endregion

    #region UI

    public void RebuildLobbyUI()
    {
        //Clear old UI
        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        //Create new UI for each spawned player
        foreach (var player in spawnedPlayers)
        {
            GameObject textObj = Instantiate(playerNameTextPrefab, playerListParent);
            var textComponent = textObj.GetComponent<Text>();
            if (textComponent != null)
            {
                //textComponent.text = player.PlayerName.Value.ToString();
            }
        }
    }

    /// <summary>
    /// Update the current player count text
    /// </summary>
    public void UpdatePlayerCount()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {spawnedPlayers.Count}/{maxPlayers}";
        }
    }

    public void LeaveLobby()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            //Notify all clients to disconnect
            DisconnectClientsClientRpc();

            //Stop the host
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby"); //Reload lobby for host
        }
        else
        {
            //Client leaving normally
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    }

    #endregion
}