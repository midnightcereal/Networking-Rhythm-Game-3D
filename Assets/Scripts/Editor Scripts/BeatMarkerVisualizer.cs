using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BeatMarkerVisualizer : MonoBehaviour
{
    public float speedMultiplier = 1f;
    public float laneX = 5f;
    public GameObject markerPrefab;

    private List<GameObject> activeMarkers = new List<GameObject>();
    private Dictionary<GameObject, float> markerTimes = new Dictionary<GameObject, float>();
    private string loadPath;

    private void Start()
    {
        loadPath = Path.Combine(Application.dataPath, "Resources/Songs/beat_markers.json");
    }

    private void Update()
    {
        if (EditorSongManager.Instance == null || EditorSongManager.Instance.audioSource == null)
            return;

        //Press Space to (re)load and visualize
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadAndDisplayBeats();
        }

        //Press J to delete beat markers JSON and reset
        if (Input.GetKeyDown(KeyCode.J))
        {
            DeleteBeatMarkers();
        }

        UpdateMarkerPositions();
    }

    private void LoadAndDisplayBeats()
    {
        speedMultiplier = EditorSongManager.Instance.speedMultiplier;

        if (!File.Exists(loadPath))
        {
            Debug.LogWarning($"[BeatMarkerVisualizer] No beat marker file found: {loadPath}");
            return;
        }

        string json = File.ReadAllText(loadPath);
        BeatMarkerData data = JsonUtility.FromJson<BeatMarkerData>(json);
        if (data == null || data.beatTimes == null || data.beatTimes.Length == 0)
        {
            Debug.LogWarning("[BeatMarkerVisualizer] No beat data found in JSON!");
            return;
        }

        //Clear previous
        foreach (var obj in activeMarkers)
            if (obj != null) DestroyImmediate(obj);
        activeMarkers.Clear();
        markerTimes.Clear();

        float songTime = 0f;
        if (EditorSongManager.Instance.audioSource != null)
            songTime = EditorSongManager.Instance.audioSource.time;

        //Spawn beat markers with correct Y position even if not playing
        foreach (float beatTime in data.beatTimes)
        {
            float y = 12.37f + (beatTime / speedMultiplier) - (songTime / speedMultiplier);
            Vector3 pos = new Vector3(laneX, y, 9.5f);


            GameObject marker = Instantiate(markerPrefab, pos, Quaternion.identity, transform);
            marker.name = $"BeatMarker_{beatTime:F2}";

            activeMarkers.Add(marker);
            markerTimes[marker] = beatTime;
        }

        Debug.Log($"[BeatMarkerVisualizer] Spawned {data.beatTimes.Length} beat markers");
    }

    private void DeleteBeatMarkers()
    {
        //Delete the JSON file if it exists
        if (File.Exists(loadPath))
        {
            File.Delete(loadPath);
            Debug.Log("[BeatMarkerVisualizer] Deleted beat_markers.json");
        }
        else
        {
            Debug.LogWarning("[BeatMarkerVisualizer] No beat_markers.json found to delete");
        }

        //Clear any currently spawned markers
        foreach (var obj in activeMarkers)
            if (obj != null) DestroyImmediate(obj);

        activeMarkers.Clear();
        markerTimes.Clear();
    }

    private void UpdateMarkerPositions()
    {
        if (activeMarkers.Count == 0) return;

        //Only move markers if song is playing from camera
        if (!EditorSongManager.Instance.isPlayingFromCamera)
            return;

        float songTime = EditorSongManager.Instance.audioSource.time;
        float trackStartY = EditorSongManager.Instance.trackStartY;
        float speedMultiplier = EditorSongManager.Instance.speedMultiplier;

        foreach (var marker in activeMarkers)
        {
            if (marker == null) continue;

            float beatTime = markerTimes[marker];

            // Match note scroll logic
            float y = trackStartY + (beatTime / speedMultiplier) - (songTime / speedMultiplier);

            marker.transform.position = new Vector3(laneX, y, 9.5f);
        }
    }

    public void ResetMarkersPosition()
    {
        if (activeMarkers.Count == 0) return;

        float trackStartY = EditorSongManager.Instance.trackStartY;
        float speedMultiplier = EditorSongManager.Instance.speedMultiplier;

        foreach (var marker in activeMarkers)
        {
            if (marker == null) continue;

            float beatTime = markerTimes[marker];

            float y = trackStartY + (beatTime / speedMultiplier);
            marker.transform.position = new Vector3(laneX, y, 9.5f);
        }
    }

    /// <summary>
    /// Accessed by editor song manager for beat snapping
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetActiveMarkers()
    {
        return activeMarkers;
    }
}