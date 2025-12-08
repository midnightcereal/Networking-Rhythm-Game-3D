using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NowPlayingManager : NetworkBehaviour
{
    public static NowPlayingManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI songNameText;
    public TextMeshProUGUI elapsedTimeText;
    public Button skipButton;
    public Button pauseButton;
    public Button restartButton;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Songs Queue")]
    public List<SongInfo> songQueue = new List<SongInfo>();

    private int currentIndex = 0;
    private bool isPaused = false;
    private bool muteNowPlaying = false; //for host when selecting a song

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        skipButton.onClick.AddListener(SkipSong);
        pauseButton.onClick.AddListener(TogglePause);
        restartButton.onClick.AddListener(RestartSong);

        if (songQueue.Count > 0)
        {
            PlaySongAtIndex(0);
        }
    }

    private void PlaySongAtIndex(int index)
    {
        if (index < 0 || index >= songQueue.Count) return;

        currentIndex = index;
        SongInfo song = songQueue[index];

        if (audioSource != null && !muteNowPlaying)
        {
            audioSource.clip = song.previewClip;
            audioSource.Play();
        }

        if (songNameText != null)
            songNameText.text = song.songName;

        //Show elapsed 00:00 / total song time
        if (audioSource.clip != null)
        {
            TimeSpan total = TimeSpan.FromSeconds(audioSource.clip.length);
            elapsedTimeText.text = $"00:00 / {total.Minutes:D2}:{total.Seconds:D2}";
        }

        isPaused = false;
    }

    private void Update()
    {
        if (!audioSource || songQueue.Count == 0 || isPaused) return;

        if (audioSource.clip != null)
        {
            TimeSpan elapsed = TimeSpan.FromSeconds(audioSource.time);
            TimeSpan total = TimeSpan.FromSeconds(audioSource.clip.length);
            elapsedTimeText.text = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2} / {total.Minutes:D2}:{total.Seconds:D2}";
        }

        //Autoplay next song when finished
        if (!audioSource.isPlaying && !isPaused && audioSource.clip != null)
        {
            PlayNextSong();
        }
    }

    private void PlayNextSong()
    {
        int nextIndex = (currentIndex + 1) % songQueue.Count;
        PlaySongAtIndex(nextIndex);
    }

    private void SkipSong()
    {
        PlayNextSong();
    }

    private void TogglePause()
    {
        if (!audioSource) return;

        if (isPaused)
        {
            audioSource.UnPause();
            isPaused = false;
        }
        else
        {
            audioSource.Pause();
            isPaused = true;
        }
    }

    private void RestartSong()
    {
        if (!audioSource) return;

        audioSource.Stop();
        audioSource.Play();
        isPaused = false;
    }

    ///<summary>Called by the lobby when a song is selected. Only mute for the host</summary>
    public void OnLobbySongSelected()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            isPaused = true;
            muteNowPlaying = true;
            songNameText.text = "MUTED WHILE PREVIEWING";
            elapsedTimeText.text = "00:00";
            if (audioSource != null) audioSource.Stop();
        }
    }
}
