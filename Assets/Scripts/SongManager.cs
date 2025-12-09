using System.Collections;
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
    public string artist;
    public int difficulty; // 1–5
    public float bpm;
    public float speedMultiplier = 0.1f; //editor multiplier
    public string audioFile;
    //Camera Colours
    public int colourChangeBeats = 1;
    public string[] availableColours;

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
    public float manualOffset = 1.0f;
    public float preRollSeconds = 3f;
    public string songJsonFile = "TestSong"; //from Resources/Songs
    public AudioSource audioSource;

    [Header("Editor Settings")]
    public float segmentHeight = 1f;
    public float bpm = 120f;
    public int beatSubdivision = 4;
    public float speedMultiplier = 0.1f;

    [Header("References")]
    public CameraColourManager cameraColourManager;

    private SongData songData;
    private List<GameObject> spawnedNotes = new List<GameObject>();
    private bool songEnded = false;

    private void Start()
    {
        //Set the song to be loaded to the one host selected in the lobby scene
        SongSelectionManager.Instance?.ApplySelectedSongToGame();
        LoadSong();

        //Apply saved volume
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (audioSource != null)
            audioSource.volume = savedVolume;
    }

    public void BeginSong(float additionalDelay)
    {
        SpawnAllNotes();
        StartCoroutine(StartAudioWithDelay(additionalDelay));

        if (cameraColourManager != null && songData.availableColours != null)
        {
            cameraColourManager.epilepsySafeMode = PlayerPrefs.GetInt("EpilepsySafeMode", 0) == 1;

            //Initialize the flashing colours from the song json
            cameraColourManager.Initialize(songData.availableColours,songData.bpm,songData.colourChangeBeats,audioSource);
        }
    }

    private void LoadSong()
    {
        //Get song file
        TextAsset file = Resources.Load<TextAsset>($"Songs/{songJsonFile}");
        if (file == null)
        {
            Debug.LogError($"Song JSON not found: {songJsonFile}");
            return;
        }

        //Get song data
        songData = JsonUtility.FromJson<SongData>(file.text);

        //Override editor values if JSON has them
        bpm = songData.bpm;
        speedMultiplier = songData.speedMultiplier;

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (!string.IsNullOrEmpty(songData.audioFile))
        {
            //Load audio file
            AudioClip clip = Resources.Load<AudioClip>($"Songs/{songData.audioFile}");
            if (clip) audioSource.clip = clip;
            else Debug.LogWarning($"Audio file '{songData.audioFile}' not found!");
        }
    }

    public SongData GetSongData()
    {
        return songData;
    }

    private void SpawnAllNotes()
    {
        if (notePrefab == null || songData == null || songData.notes == null) return;

        foreach (var n in songData.notes)
        {
            //Spawn the note above the hit line so it starts falling immediately
            float spawnY = hitLine.position.y + preRollSeconds * speedMultiplier;
            Vector3 pos = new Vector3(laneXPositions[n.lane], spawnY, laneZ);
            GameObject noteObj = Instantiate(notePrefab, pos, Quaternion.identity);
            spawnedNotes.Add(noteObj);

            Note noteScript = noteObj.GetComponent<Note>();
            if (noteScript != null)
            {
                noteScript.isHold = n.type == "hold";
                noteScript.holdDuration = n.holdDuration;
                noteScript.hitLine = hitLine;

                //Time logic for syncing
                noteScript.time = (n.positionY * speedMultiplier);
                noteScript.speedMultiplier = speedMultiplier;
            }
        }
    }

    private IEnumerator StartAudioWithDelay(float additionalDelay)
    {
        //Wait for intro to finish and then play audio
        float totalDelay = Mathf.Abs(preRollSeconds) + additionalDelay;
        yield return new WaitForSeconds(totalDelay);

        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }
}