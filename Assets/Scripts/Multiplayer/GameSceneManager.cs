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
        if (sceneName == "GameScene")
        {
            playersReady.Add(clientId);
            Debug.Log($"Client {clientId} loaded GameScene");

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
        StartCoroutine(CountdownStart());
    }

    private IEnumerator CountdownStart()
    {
        int count = 3;
        while (count > 0)
        {
            Debug.Log(count);
            yield return new WaitForSeconds(1f);
            count--;
        }
        Debug.Log("Go!");
        //Start song playback & rhythm logic here
    }
}