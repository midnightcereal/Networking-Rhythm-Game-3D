using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles song selection in the lobby and synchronizes it across clients.
/// The host can set the selected song, clients receive the selection automatically.
/// </summary>
public class SongSelectionManager : NetworkBehaviour
{
    public static SongSelectionManager Instance;

    [Header("Selected Song Data")]
    // Stores the name of the selected song JSON
    public NetworkVariable<FixedString128Bytes> selectedSong = new NetworkVariable<FixedString128Bytes>(
        new FixedString128Bytes("kitsune"), // default song
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // only host can write
    );

    public NetworkVariable<int> selectedSongIndex = new NetworkVariable<int>(0);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Subscribe to changes to update lobby UI if needed
        selectedSong.OnValueChanged += OnSongChanged;
    }

    private void OnDestroy()
    {
        selectedSong.OnValueChanged -= OnSongChanged;
    }

    public void SetSelectedSong(int index)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        selectedSongIndex.Value = index;
    }

    public int GetSelectedSongIndex()
    {
        return selectedSongIndex.Value;
    }

    private void OnSongChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        Debug.Log($"Selected song changed: {newValue.ToString()}");
    }

    /// <summary>
    /// Called by host to change the song selection in the lobby.
    /// </summary>
    /// <param name="songJsonName"></param>
    public void SetSelectedSong(string songJsonName)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Only the host can set the selected song!");
            return;
        }

        if (string.IsNullOrEmpty(songJsonName)) return;

        selectedSong.Value = new FixedString128Bytes(songJsonName);
    }

    /// <summary>
    /// Get the currently selected song (any client can call this)
    /// </summary>
    /// <returns>JSON name of selected song</returns>
    public string GetSelectedSong()
    {
        return selectedSong.Value.ToString();
    }

    /// <summary>
    /// Helper for GameSceneManager to load the correct song JSON
    /// </summary>
    public void ApplySelectedSongToGame()
    {
        SongManager songManager = FindObjectOfType<SongManager>();
        if (songManager == null)
        {
            Debug.LogError("No SongManager found in Game scene!");
            return;
        }

        string songJson = GetSelectedSong();
        if (!string.IsNullOrEmpty(songJson))
        {
            songManager.songJsonFile = songJson;
            Debug.Log($"Loaded selected song '{songJson}' into SongManager.");
        }
    }
}