using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BeatMarkerRecorder : MonoBehaviour
{
    private AudioSource audioSource;
    private List<float> beatTimes = new List<float>();
    private bool audioReady = false;
    private string savePath;

    private void Start()
    {
        savePath = Path.Combine(Application.dataPath, "Resources/Songs/beat_markers.json");
        Debug.Log($"[BeatMarkerRecorder] Save path: {savePath}");
    }

    private void Update()
    {
        if (!audioReady)
        {
            audioSource = FindObjectOfType<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                audioReady = true;
                Debug.Log("[BeatMarkerRecorder] Found AudioSource at runtime");
            }
            return;
        }

        if (!audioSource.isPlaying)
            return;

        //Press J to mark beat
        if (Input.GetKeyDown(KeyCode.J))
        {
            float currentTime = audioSource.time;
            beatTimes.Add(currentTime);
            Debug.Log($"[BeatMarkerRecorder] Marked beat at {currentTime:F3}s");
        }

        //Press Space to save
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SaveBeatMarkers();
        }
    }

    private void SaveBeatMarkers()
    {
        if (beatTimes.Count == 0)
        {
            Debug.LogWarning("[BeatMarkerRecorder] No beats recorded to save!");
            return;
        }

        BeatMarkerData data = new BeatMarkerData { beatTimes = beatTimes.ToArray() };
        string json = JsonUtility.ToJson(data, true);
        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
        File.WriteAllText(savePath, json);

        Debug.Log($"[BeatMarkerRecorder] Saved {beatTimes.Count} beat markers to {savePath}");
    }
}