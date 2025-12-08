using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SongInfo
{
    public string songName;
    public string artist;
    public int difficulty;
    public AudioClip previewClip;
    public string sceneToLoad;
    public Sprite coverArt;
}

public class SongMenuGrid : MonoBehaviour
{
    public static SongMenuGrid Instance;

    public Transform buttonParent;
    public GameObject songButtonPrefab;
    public GameObject nowPlayingTextPrefab;

    [Header("Grid Settings")]
    public Vector2 gridSize = new Vector2(2, 2);      //2x2 grid
    public Vector2 cellSize = new Vector2(300, 160);  //Button size
    public Vector2 spacing = new Vector2(40, 40);     //Gap between buttons
    public Vector2 startOffset = new Vector2(340, 0);

    public AudioSource previewAudioSource;
    public List<SongInfo> songs = new List<SongInfo>();
    private SongInfo selectedSong;
    private GameObject currentIndicator;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PopulateGrid();
        //if (songs.Count > 0) SelectSong(songs[0]);
    }

    private void PopulateGrid()
    {
        //Clear old buttons
        foreach (Transform child in buttonParent) Destroy(child.gameObject);
        currentIndicator = null;

        int maxSongs = (int)(gridSize.x * gridSize.y);
        for (int i = 0; i < Mathf.Min(songs.Count, maxSongs); i++)
        {
            SongInfo song = songs[i];

            //Calculate grid position
            int col = i % (int)gridSize.x;
            int row = i / (int)gridSize.x;

            Vector2 position = startOffset;
            position.x += col * (cellSize.x + spacing.x);
            position.y -= row * (cellSize.y + spacing.y);

            GameObject btnObj = Instantiate(songButtonPrefab, buttonParent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();

            //Middle right anchoring
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = cellSize;

            Image cover = btnObj.transform.Find("Cover Image")?.GetComponent<Image>();
            TextMeshProUGUI title = btnObj.transform.Find("Title Text")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI artist = btnObj.transform.Find("Artist Text")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI diff = btnObj.transform.Find("Difficulty Text")?.GetComponent<TextMeshProUGUI>();

            if (cover && song.coverArt) cover.sprite = song.coverArt;
            if (title) title.text = song.songName;
            if (artist) artist.text = song.artist;
            if (diff) diff.text = $"{song.difficulty}/5";

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectSong(song));
        }
    }

    public void SelectSong(SongInfo song)
    {
        if (selectedSong == song) return;

        selectedSong = song;

        if (currentIndicator != null) Destroy(currentIndicator);

        //Find and mark clicked button
        foreach (Transform child in buttonParent)
        {
            TextMeshProUGUI titleText = child.Find("Title Text")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null && titleText.text == song.songName)
            {
                currentIndicator = Instantiate(nowPlayingTextPrefab, child);
                RectTransform rt = currentIndicator.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, cellSize.y / 2 + 30);
                break;
            }
        }

        //Play preview
        if (previewAudioSource && previewAudioSource.clip != song.previewClip)
        {
            previewAudioSource.Stop();
            previewAudioSource.clip = song.previewClip;
            previewAudioSource.Play();
        }

        //Notify SongSelectionManager (host only)
        if (NetworkManager.Singleton.IsHost && SongSelectionManager.Instance != null)
        {
            SongSelectionManager.Instance?.SetSelectedSong(song.songName);
        }
    }

    public SongInfo GetSelectedSong() => selectedSong;
}