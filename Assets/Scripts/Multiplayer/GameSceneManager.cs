using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : NetworkBehaviour
{
    public static GameSceneManager Instance;

    private HashSet<ulong> playersReady = new HashSet<ulong>();

    private void Awake()
    {
        Instance = this;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnClientLoadedScene;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnClientLoadedScene;
    }

    private void OnClientLoadedScene(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (sceneName == "Game")
        {
            playersReady.Add(clientId);
            Debug.Log($"Client {clientId} loaded Game Scene");

            //Check if all connected players are ready
            if (NetworkManager.Singleton.IsHost)
            {
                if (playersReady.Count == NetworkManager.Singleton.ConnectedClientsIds.Count)
                {
                    Debug.Log("All players loaded. Starting game!");
                    StartGameClientRpc();
                }
            }
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("Game started for everyone!");
        double startTime = NetworkManager.ServerTime.Time + 2.0;
        StartCoroutine(CountdownStart(startTime));
    }

    private IEnumerator CountdownStart(double startTime)
    {
        double remaining = startTime - NetworkManager.ServerTime.Time;

        if (remaining > 0)
            yield return new WaitForSeconds((float)remaining);

        Debug.Log("Start!");

        SongManager songManager = FindObjectOfType<SongManager>();
        if (songManager != null)
            songManager.BeginSong();
        else
            Debug.LogError("No SongManager found in scene!");
    }
}