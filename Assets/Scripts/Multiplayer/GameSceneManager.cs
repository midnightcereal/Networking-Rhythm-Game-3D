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

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnClientLoadedScene;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientLoadedScene(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (sceneName != "Game") return;

        //Debug.Log($"Client {clientId} loaded Game scene");

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
            Debug.LogError("Local rhythm prefab not assigned!");
            return;
        }

        if (localRhythmInstance != null)
            Destroy(localRhythmInstance);

        //Locally spawn the rhythm track prefab for each player (contains input manager/ combo manager/ UI elements etc.)
        localRhythmInstance = Instantiate(localRhythmPrefab);
        //Debug.Log("Spawned local rhythm setup for local client");
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        //Start game for all players
        Debug.Log("Game started for everyone!");
        double startTime = NetworkManager.ServerTime.Time + 1.0;
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

        //Retrieve song info
        var songData = songManager.GetSongData();


        if (MissEffect.Instance != null)
            MissEffect.Instance.ResetGameOverState();

        if (introUI != null && songData != null)
        {
            //Start the song intro sequence - display artist/ difficulty then countdown
            yield return introUI.PlayIntroSequence(songData.songName, songData.artist, songData.difficulty);
        }

        //Since adding song selector client's audio is 0.2 seconds out of sync for some reason
        if (NetworkManager.Singleton.IsHost)
        {
            songManager.BeginSong(0.2f);
        }
        else
        {
            songManager.BeginSong(0f);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        //if (playersReady.Count < 2) return;

        Debug.Log($"[GameSceneManager] Player {clientId} disconnected mid-game!");

        bool isHostLeaving = (clientId == NetworkManager.Singleton.LocalClientId); // Only true on the host itself

        int remainingSide;

        if (IsHost)
        {
            //Host is still alive -> a client left
            int disconnectedSide = GameplayUI.Instance.GetPlayerSide(clientId);
            remainingSide = 1 - disconnectedSide;

            EndGameDueToDisconnectClientRpc(remainingSide);
        }
        else
        {
            //This is the remaining CLIENT -> the HOST left
            remainingSide = GameplayUI.Instance.GetPlayerSide(NetworkManager.Singleton.LocalClientId);

            PerformGameEndDueToDisconnect(remainingSide);
        }
    }

    [ClientRpc]
    private void EndGameDueToDisconnectClientRpc(int winnerSide)
    {
        PerformGameEndDueToDisconnect(winnerSide);
    }

    private void PerformGameEndDueToDisconnect(int winnerSide)
    {
        Debug.Log($"[GameSceneManager] Ending game - Winner Side: {winnerSide}");

        //Stop song
        SongManager songManager = FindObjectOfType<SongManager>();
        if (songManager?.audioSource != null)
            songManager.audioSource.Stop();

        //Destroy local rhythm track
        if (localRhythmInstance != null)
        {
            var sm = localRhythmInstance.GetComponentInChildren<SongManager>();
            sm.gameObject.SetActive(false);

            var cm = localRhythmInstance.GetComponentInChildren<ComboManager>();
            cm.gameObject.SetActive(false);

            var noteTracks = GameObject.FindWithTag("NoteTracks");
            noteTracks.SetActive(false);

            //localRhythmInstance = null;
        }

        //Show correct forfeit results
        if (ResultsManager.Instance != null)
            ResultsManager.Instance.ShowForfeitResults(winnerSide);

        Time.timeScale = 0f;
    }
}