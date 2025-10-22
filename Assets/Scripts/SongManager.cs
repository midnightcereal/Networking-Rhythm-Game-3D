using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoteData
{
    public float positionY;
    public int lane;
    public string type; // "tap" or "hold"
    public float holdDuration;
}

[System.Serializable]
public class SongData
{
    public string songName;
    public float bpm;
    public float speedMultiplier = 0.1f; // editor multiplier
    public string audioFile;
    public NoteData[] notes;
}

public class SongManager : MonoBehaviour
{
    [Header("Notes & Lanes")]
    public GameObject notePrefab;
    public float laneZ = 9.5f;
    public float[] laneXPositions = new float[] { -9.2f, -2.88f, 2.88f, 9.22f };
    public Transform hitLine;

    [Header("Audio & Timing")]
    public string songJsonFile = "TestSong"; // from Resources/Songs
    public AudioSource audioSource;

    [Header("Editor Settings")]
    public float segmentHeight = 1f; // must match EditorSongManager
    public float bpm = 120f;
    public int beatSubdivision = 4; // must match EditorSongManager
    public float speedMultiplier = 0.1f; // must match EditorSongManager

    private SongData songData;
    private List<GameObject> spawnedNotes = new List<GameObject>();

    private void Start()
    {
        LoadSong();
        SpawnAllNotes();
        PlayAudio();
    }

    private void LoadSong()
    {
        TextAsset file = Resources.Load<TextAsset>($"Songs/{songJsonFile}");
        if (file == null)
        {
            Debug.LogError($"Song JSON not found: {songJsonFile}");
            return;
        }

        songData = JsonUtility.FromJson<SongData>(file.text);

        // override editor values if JSON has them
        bpm = songData.bpm;
        speedMultiplier = songData.speedMultiplier;

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (!string.IsNullOrEmpty(songData.audioFile))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Songs/{songData.audioFile}");
            if (clip) audioSource.clip = clip;
            else Debug.LogWarning($"Audio file '{songData.audioFile}' not found!");
        }
    }

    private void SpawnAllNotes()
    {
        if (notePrefab == null || songData == null || songData.notes == null) return;

        // calculate falling speed exactly like in editor
        float noteSpeed = (60f / bpm) / (beatSubdivision / 4f) / segmentHeight * speedMultiplier;
        // or simpler: speed = segmentHeight / (time per segment)
        // here we just use the same speedMultiplier as editor for consistency

        float manualOffset = 1f; // offset in seconds to fix the sync between editor and game scenes

        foreach (var n in songData.notes)
        {
            Vector3 pos = new Vector3(laneXPositions[n.lane], n.positionY, laneZ);
            GameObject noteObj = Instantiate(notePrefab, pos, Quaternion.identity);
            spawnedNotes.Add(noteObj);

            Note noteScript = noteObj.GetComponent<Note>();
            if (noteScript != null)
            {
                noteScript.isHold = n.type == "hold";
                noteScript.holdDuration = n.holdDuration;
                noteScript.hitLine = hitLine;
                noteScript.time = (n.positionY * speedMultiplier) - manualOffset; // match editor-style timing
                noteScript.speedMultiplier = speedMultiplier;    // pass the multiplier
            }
        }
    }

    private void PlayAudio()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.time = 0f;
            audioSource.Play();
        }
    }
}