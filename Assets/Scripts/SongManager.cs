using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class NoteData
{
    public float time;           //Time in seconds relative to song
    public int lane;             //0-3
    public string type;          //"tap" or "hold"
    public float holdDuration;   //seconds
}

[System.Serializable]
public class SongData
{
    public string songName;
    public float bpm;
    public string audioFile;
    public NoteData[] notes;

    //Camera colour changes
    public string[] availableColours;
    public int colourChangeBeats = 4;
}

public class SongManager : MonoBehaviour
{
    [Header("Lanes")]
    public Transform[] laneSpawnPoints;
    public Transform hitLine;

    [Header("BPM Scaling")]
    public float baseBPM = 120f;
    public float baseNoteSpeed = 5f;
    public float visualScale = 1f;

    [Header("Notes")]
    public GameObject notePrefab;

    [Header("Song JSON")]
    public string songJsonFile = "Songs/TestSong";

    private List<NoteData> notes;
    private float noteSpeed;
    private AudioSource audioSource;
    private SongData songData;
    private CameraColourManager colourManager;

    private void Start()
    {
        LoadSong();
        SetupAudio();
        SetupColourSystem();
        StartCoroutine(SpawnNotes());
    }

    private void LoadSong()
    {
        TextAsset file = Resources.Load<TextAsset>(songJsonFile);
        if (file == null)
        {
            Debug.LogError("Song JSON not found: " + songJsonFile);
            return;
        }

        songData = JsonUtility.FromJson<SongData>(file.text);
        notes = new List<NoteData>(songData.notes);

        //Scale note speed relative to BPM
        float bpmFactor = songData.bpm / baseBPM;
        noteSpeed = baseNoteSpeed * bpmFactor * visualScale;
    }

    private void SetupAudio()
    {
        AudioClip clip = Resources.Load<AudioClip>(songData.audioFile);
        if (clip == null)
        {
            Debug.LogError("Audio file not found in Resources: " + songData.audioFile);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void SetupColourSystem()
    {
        colourManager = Camera.main.GetComponent<CameraColourManager>();
        if (colourManager && songData.availableColours != null && songData.availableColours.Length > 0)
        {
            colourManager.Initialize(songData.availableColours, songData.bpm, songData.colourChangeBeats, audioSource);
            Debug.Log("Set up camera colours succesfully");
        }
        else
        {
            Debug.LogError("Error setting up camera colours");
        }
    }

    private IEnumerator SpawnNotes()
    {
        float lastSpawnTime = 0f;

        foreach (var note in notes)
        {
            //Time the note should spawn (note.time - travelTime)
            float travelDistance = Mathf.Abs(laneSpawnPoints[note.lane].position.y - hitLine.position.y);
            float travelTime = travelDistance / noteSpeed;
            float spawnTime = note.time - travelTime;

            //Delay relative to last spawn
            float delay = spawnTime - lastSpawnTime;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            SpawnNote(note);
            lastSpawnTime = spawnTime;
        }
    }

    private void SpawnNote(NoteData data)
    {
        if (data.lane < 0 || data.lane >= laneSpawnPoints.Length) return;

        Transform spawnPoint = laneSpawnPoints[data.lane];
        Vector3 spawnPos = spawnPoint.position;

        GameObject noteObj = Instantiate(notePrefab, spawnPos, Quaternion.identity);

        Note noteScript = noteObj.GetComponent<Note>();
        noteScript.speed = noteSpeed;
        noteScript.hitLine = hitLine;
        noteScript.isHold = data.type == "hold";
        noteScript.holdDuration = data.holdDuration;
    }
}