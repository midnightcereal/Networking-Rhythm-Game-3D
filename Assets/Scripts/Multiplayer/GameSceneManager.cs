using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : NetworkBehaviour
{
    public static GameSceneManager Instance;

    [Header("Local Rhythm Prefab")]
    public GameObject localRhythmPrefab;

    private HashSet<ulong> playersReady = new HashSet<ulong>();
    private GameObject localRhythmInstance;

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
        if (sceneName != "Game") return;

        Debug.Log($"Client {clientId} loaded Game scene");

        //Spawn local rhythm setup for local player
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SpawnLocalRhythmSetup();
        }

        //Track ready players on the host
        playersReady.Add(clientId);

        if (NetworkManager.Singleton.IsHost)
        {
            if (playersReady.Count == NetworkManager.Singleton.ConnectedClientsIds.Count)
            {
                Debug.Log("All players loaded. Starting game!");
                StartGameClientRpc();
            }
        }
    }

    private void SpawnLocalRhythmSetup()
    {
        if (localRhythmPrefab == null)
        {
            Debug.LogError("Local rhythm prefab not assigned in GameSceneManager!");
            return;
        }

        if (localRhythmInstance != null)
        {
            Destroy(localRhythmInstance);
        }

        localRhythmInstance = Instantiate(localRhythmPrefab);
        Debug.Log("Spawned local rhythm setup for local client");
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

        Debug.Log("Starting song intro!");

        SongManager songManager = FindObjectOfType<SongManager>();
        SongIntroUI introUI = FindObjectOfType<SongIntroUI>();

        if (songManager == null)
        {
            Debug.LogError("No SongManager found in scene!");
            yield break;
        }

        var songData = songManager.GetSongData();

        if (introUI != null && songData != null)
        {
            string songTitle = songData.songName;
            string artist = songData.artist;
            int difficulty = songData.difficulty;

            yield return introUI.PlayIntroSequence(songTitle, artist, difficulty);
        }
        else
        {
            Debug.LogWarning("No SongIntroUI found or SongData missing, skipping intro.");
        }

        songManager.BeginSong();
    }
}