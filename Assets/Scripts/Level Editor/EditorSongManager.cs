using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class EditorNoteData
{
    public float positionY;
    public int lane;       // 0-3
    public string type;    // "tap" or "hold"
    public float holdDuration;
}

[System.Serializable]
public class EditorSongData
{
    public string songName;
    public float bpm;
    public float speedMultiplier = 0.1f;
    public string audioFile;
    public EditorNoteData[] notes;
}

public class EditorSongManager : MonoBehaviour
{
    [Header("Editor Settings")]
    public GameObject notePrefab;
    public float speedMultiplier = 0.1f;
    public float laneZ = 9.5f;

    [Header("Track Start Offset")]
    [Tooltip("Where the first beat (segment 0) begins in world space.")]
    public float trackStartY = 2.5f;

    [Header("Timing & Sync Settings")]
    public float bpm = 120f;
    public int beatSubdivision = 4; // 4 = quarter, 8 = eighth, etc.
    public float segmentHeight = 1f; // auto-updated
    //Rebuild variables
    private int lastSegmentCount = -1;
    private float lastBpm = -1f;
    private int lastSubdivision = -1;

    [Header("Grid Visuals")]
    [Range(4f, 20f)]
    public float gridVisualScale = 4f; //increases the spacing between beats visually
    public GameObject segmentPrefab;
    public List<GameObject> runtimeSegments = new List<GameObject>();

    [Header("Camera & Input")]
    public float scrollSpeed = 5f;

    [Header("Lanes")]
    public float[] laneXPositions = new float[] { -9.2f, -2.88f, 2.88f, 9.22f };
    public int segmentCount = 20;
    public Color segmentColour = Color.green;

    [Header("Hold Notes")]
    public float holdDurationEditor = 1f;

    [HideInInspector] public List<EditorNote> notes = new List<EditorNote>();
    [HideInInspector] public bool placingTap = false;
    [HideInInspector] public bool placingHold = false;
    [HideInInspector] public bool deleteMode = false;

    [HideInInspector] public EditorNote currentHoldNote = null;
    [HideInInspector] public AudioSource audioSource;
    private string songName = "No Song Loaded";

    private void Start()
    {
        SpawnSegments();
        UpdateSegmentHeight();
        UpdateSegmentTexts();
    }

    public void SpawnSegments()
    {
        // Fully clean old segments first
        foreach (var s in runtimeSegments)
        {
            if (s != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(s);
                else
                    Destroy(s);
#else
            Destroy(s);
#endif
            }
        }
        runtimeSegments.Clear();

        if (laneXPositions == null || laneXPositions.Length == 0) return;

        // Recalculate segmentHeight so visuals are in sync
        UpdateSegmentHeight(); // keeps your formula centralized

        for (int seg = 0; seg < segmentCount; seg++)
        {
            float y = trackStartY + seg * segmentHeight;
            // Seconds for this segment (matches OnDrawGizmos)
            float seconds = (y - trackStartY) * speedMultiplier;

            for (int i = 0; i < laneXPositions.Length; i++)
            {
                Vector3 pos = new Vector3(laneXPositions[i], y, laneZ - 0.03f);
                GameObject segObj = Instantiate(segmentPrefab, pos, Quaternion.Euler(-90f, 0f, 0f));
                segObj.name = $"Segment_{i}_{seg}";
                segObj.transform.localScale = new Vector3(0.4f, 0.1f, 0.2f);

                // Update text (if prefab has Canvas -> TMP text)
                Transform canvas = segObj.transform.Find("Canvas");
                if (canvas != null)
                {
                    var text = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null)
                        text.text = $"{seconds:F2}s";
                }

                runtimeSegments.Add(segObj);
            }
        }
    }

    private void Update()
    {
        HandleMouseInput();
        HandleScroll();
        HandleSubdivisionHotkeys();
        UpdateSegmentScroll();
        UpdateSegmentRebuild();
    }


    private void HandleMouseInput()
    {
        Vector3 mousePos = Input.mousePosition;

        //Delete mode
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                EditorNote note = hit.collider.GetComponent<EditorNote>();
                if (note != null)
                {
                    notes.Remove(note);
                    DestroyImmediate(note.gameObject);
                }
            }
            return;
        }

        //Place notes
        if ((placingTap || placingHold) && Input.GetMouseButtonDown(0))
        {
            if (placingTap) PlaceNoteOnTrack(mousePos, false);
            else if (placingHold)
            {
                currentHoldNote = PlaceNoteOnTrack(mousePos, true);
                if (currentHoldNote != null) currentHoldNote.isDragging = true;
            }
        }

        // Drag hold note
        if (currentHoldNote != null && currentHoldNote.isDragging && Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, laneZ));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);

                // Snap drag position to the top of the next segment above the note
                float noteY = currentHoldNote.transform.position.y; // note bottom
                int currentSegment = Mathf.FloorToInt((noteY - trackStartY) / segmentHeight);
                float nextSegmentTop = trackStartY + (currentSegment + 1) * segmentHeight;

                // Clamp drag to be at least one segment
                float snappedY = Mathf.Max(nextSegmentTop, hit.y);

                // Compute hold line length
                float length = snappedY - noteY;
                currentHoldNote.holdDuration = length * speedMultiplier;
                currentHoldNote.UpdateHoldVisual(length);
            }
        }
    }









    //    // Release hold
    //    if (currentHoldNote != null && Input.GetMouseButtonUp(0))
    //    {
    //        currentHoldNote.isDragging = false;
    //        currentHoldNote = null;
    //    }
    //}

        #region Track Movement
    private void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
            Camera.main.transform.position += Vector3.up * scroll * scrollSpeed;
    }

    private void HandleSubdivisionHotkeys()
    {
        //Increase subdivision
        if (Input.GetKeyDown(KeyCode.Period))
        {
            beatSubdivision *= 2;
            if (beatSubdivision > 64) beatSubdivision = 64;
            UpdateSegmentHeight();
            UpdateSegmentTexts();
        }

        //Decrease subdivision
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            beatSubdivision /= 2;
            if (beatSubdivision < 1) beatSubdivision = 1;
            UpdateSegmentHeight();
            UpdateSegmentTexts();
        }
    }

    private void UpdateSegmentHeight()
    {
        segmentHeight = ((60f / bpm) * speedMultiplier / (beatSubdivision / 4f)) * gridVisualScale;
        Debug.Log($"Segment Height Updated: {segmentHeight} (Subdivision: 1/{beatSubdivision})");
    }

    ///<summary>
    ///Updates all segment markers and their displayed seconds when BPM, segment height or trackStartY changes.
    ///</summary>
    public void UpdateSegmentTexts()
    {
#if UNITY_EDITOR
        if (laneXPositions == null || laneXPositions.Length == 0) return;

        // Recalc visual spacing if needed
        UpdateSegmentHeight();

        for (int seg = 0; seg < segmentCount; seg++)
        {
            float y = trackStartY + seg * segmentHeight;
            float seconds = (y - trackStartY) * speedMultiplier;

            for (int i = 0; i < laneXPositions.Length; i++)
            {
                string name = $"Segment_{i}_{seg}";
                GameObject segObj = GameObject.Find(name);
                if (segObj == null) continue;

                segObj.transform.position = new Vector3(laneXPositions[i], y, laneZ - 0.01f);

                Transform canvas = segObj.transform.Find("Canvas");
                if (canvas != null)
                {
                    var text = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null)
                        text.text = $"{seconds:F2}s";
                }
            }
        }
#endif
    }

    // ---------- HANDLE RUNTIME REBUILD when segmentCount / bpm / subdivision changes ----------
    private void UpdateSegmentRebuild()
    {
        // Only trigger rebuild if something changed
        if (lastSegmentCount != segmentCount || Mathf.Abs(lastBpm - bpm) > 0.001f || lastSubdivision != beatSubdivision)
        {
            // Clean & respawn
            SpawnSegments();

            // Update trackers
            lastSegmentCount = segmentCount;
            lastBpm = bpm;
            lastSubdivision = beatSubdivision;
        }
    }

    ///<summary>
    ///Moves segment labels downward in real time as the song plays so they align with the hitbar as time passes.
    ///</summary>
    private void UpdateSegmentScroll()
    {
        if (audioSource == null || audioSource.clip == null) return;
        if (!audioSource.isPlaying) return;

        float songTime = audioSource.time;              // seconds into the song
        float scrollOffset = songTime / speedMultiplier; // convert to world Y units

        // For each segment compute baseY and then shift by scrollOffset
        for (int seg = 0; seg < segmentCount; seg++)
        {
            float baseY = trackStartY + seg * segmentHeight;
            float adjustedY = baseY - scrollOffset;
            float seconds = (baseY - trackStartY) * speedMultiplier; // stable seconds label

            for (int i = 0; i < laneXPositions.Length; i++)
            {
                string name = $"Segment_{i}_{seg}";
                GameObject segObj = GameObject.Find(name);
                if (segObj == null) continue;

                segObj.transform.position = new Vector3(laneXPositions[i], adjustedY, laneZ - 0.1f);

                Transform canvas = segObj.transform.Find("Canvas");
                if (canvas != null)
                {
                    var text = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null)
                        text.text = $"{seconds:F2}s";
                }
            }
        }
    }

    ///<summary>
    ///Resets all segment labels and markers to their original Y positions at the start of the track.
    ///</summary>
    private void ResetSegmentTextPositions()
    {
        for (int seg = 0; seg < segmentCount; seg++)
        {
            float baseY = trackStartY + seg * segmentHeight;
            float seconds = (baseY - trackStartY) * speedMultiplier;

            for (int i = 0; i < laneXPositions.Length; i++)
            {
                string name = $"Segment_{i}_{seg}";
                GameObject segObj = GameObject.Find(name);
                if (segObj == null) continue;

                segObj.transform.position = new Vector3(laneXPositions[i], baseY, laneZ - 0.1f);

                Transform canvas = segObj.transform.Find("Canvas");
                if (canvas != null)
                {
                    var text = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null)
                        text.text = $"{seconds:F2}s";
                }
            }
        }
    }

    #endregion

    private EditorNote PlaceNoteOnTrack(Vector3 mouseScreenPos, bool hold)
    {
        // Make sure the prefab is assigned
        if (notePrefab == null)
        {
            Debug.LogError("Note Prefab not assigned in EditorSongManager!");
            return null;
        }

        // Make sure the camera exists
        if (Camera.main == null)
        {
            Debug.LogError("No MainCamera found! Tag your editor camera as 'MainCamera'.");
            return null;
        }

        // Create a ray to the track plane
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, laneZ));

        if (!plane.Raycast(ray, out float enter))
        {
            Debug.LogWarning("Ray didn't hit the placement plane.");
            return null;
        }

        Vector3 hit = ray.GetPoint(enter);

        // Find nearest lane X
        int laneIndex = 0;
        float closestX = Mathf.Abs(hit.x - laneXPositions[0]);
        for (int i = 1; i < laneXPositions.Length; i++)
        {
            float dist = Mathf.Abs(hit.x - laneXPositions[i]);
            if (dist < closestX)
            {
                closestX = dist;
                laneIndex = i;
            }
        }

        hit.x = laneXPositions[laneIndex];
        hit.z = laneZ;

        // Snap Y to nearest segment
        int nearestSegment = Mathf.RoundToInt((hit.y - trackStartY) / segmentHeight);
        hit.y = trackStartY + nearestSegment * segmentHeight;


        // Prevent overlapping notes
        foreach (var n in notes)
        {
            if (n == null) continue;
            int nSegment = Mathf.RoundToInt(n.transform.position.y / segmentHeight);
            if (nSegment == nearestSegment && n.lane == laneIndex)
            {
                Debug.Log("Segment already has a note on this lane!");
                return null;
            }
        }

        // Instantiate note
        GameObject noteObj = Instantiate(notePrefab, hit, Quaternion.identity);
        if (noteObj == null)
        {
            Debug.LogError("Failed to instantiate notePrefab.");
            return null;
        }

        EditorNote noteScript = noteObj.GetComponent<EditorNote>();
        if (noteScript == null)
        {
            Debug.LogError("The notePrefab is missing the 'EditorNote' script!");
            return null;
        }

        noteScript.isHold = hold;
        noteScript.lane = laneIndex;
        noteScript.time = hit.y * speedMultiplier;

        if (hold)
        {
            float lengthSegments = Mathf.Max(1, Mathf.Round(holdDurationEditor / segmentHeight));
            noteScript.holdDuration = lengthSegments * segmentHeight * speedMultiplier;
            noteScript.UpdateHoldVisual(lengthSegments * segmentHeight);
        }

        notes.Add(noteScript);
        return noteScript;
    }

    #region JSON
    public void SaveJSON(string defaultSongName, float bpmInput, string defaultAudioFile)
    {
        string path = EditorUtility.SaveFilePanel("Save JSON", "Assets/Resources/Songs", defaultSongName + ".json", "json");
        if (string.IsNullOrEmpty(path)) return;

        List<EditorNoteData> noteDataList = new List<EditorNoteData>();
        foreach (var n in notes)
        {
            noteDataList.Add(new EditorNoteData
            {
                positionY = n.transform.position.y,
                lane = n.lane,
                type = n.isHold ? "hold" : "tap",
                holdDuration = n.isHold ? n.holdDuration : 0f
            });
        }

        EditorSongData songData = new EditorSongData
        {
            songName = defaultSongName,
            bpm = bpmInput,
            speedMultiplier = speedMultiplier,
            audioFile = defaultAudioFile,
            notes = noteDataList.ToArray()
        };

        File.WriteAllText(path, JsonUtility.ToJson(songData, true));
        Debug.Log("Saved song JSON: " + path);
    }

    public void LoadJSON()
    {
        string path = EditorUtility.OpenFilePanel("Load JSON", "Assets/Resources/Songs", "json");
        if (string.IsNullOrEmpty(path)) return;

        string jsonText = File.ReadAllText(path);
        EditorSongData songData = JsonUtility.FromJson<EditorSongData>(jsonText);

        bpm = songData.bpm;
        songName = songData.songName;
        speedMultiplier = songData.speedMultiplier;
        UpdateSegmentHeight();
        UpdateSegmentTexts();

        //Ensure we have an AudioSource
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();

        //Try to load the clip from Resources/Songs/{audioFile}
        if (!string.IsNullOrEmpty(songData.audioFile))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Songs/{songData.audioFile}");
            if (clip)
            {
                audioSource.clip = clip;
                Debug.Log($"Loaded audio clip: {songData.audioFile}");
            }
            else
            {
                Debug.LogWarning($"Audio file '{songData.audioFile}' not found in Resources/Songs/");
            }
        }

        foreach (var n in notes)
            if (n != null) DestroyImmediate(n.gameObject);
        notes.Clear();

        foreach (var nData in songData.notes)
        {
            Vector3 pos = new Vector3(laneXPositions[nData.lane], nData.positionY, laneZ);
            EditorNote note = Instantiate(notePrefab, pos, Quaternion.identity).GetComponent<EditorNote>();
            note.isHold = nData.type == "hold";
            note.lane = nData.lane;
            note.time = nData.positionY * speedMultiplier;
            note.holdDuration = nData.holdDuration;
            if (note.isHold) note.UpdateHoldVisual(nData.holdDuration / speedMultiplier);
            notes.Add(note);
        }

        Debug.Log($"Loaded song: {songName} | BPM: {bpm} | Multiplier: {speedMultiplier}");
    }
    #endregion

    #region Playback
    public void PlayFromCamera()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();

        if (audioSource.clip == null)
        {
            Debug.LogWarning("Assign audio clip first.");
            return;
        }

        //Start playback from camera position
        float startTime = (Camera.main.transform.position.y - trackStartY) * speedMultiplier;
        startTime = Mathf.Clamp(startTime, 0f, audioSource.clip.length);

        audioSource.time = startTime;
        audioSource.Play();

        Debug.Log($"> Playing from {startTime:F2}s");
    }

    public void PauseAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    public void ResetTrack()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }

        Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, trackStartY, Camera.main.transform.position.z);
        Camera.main.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        ResetSegmentTextPositions();
    }

    #endregion

    #region Debug

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(10, 10, 400, 30), $"Song: {songName}", style);
        GUI.Label(new Rect(10, 30, 400, 30), $"BPM: {bpm}", style);
        GUI.Label(new Rect(10, 50, 400, 30), $"Subdivision: 1/{beatSubdivision}", style);
        GUI.Label(new Rect(10, 70, 400, 30), $"Segment Height: {segmentHeight:F4}", style);
        GUI.Label(new Rect(10, 90, 400, 30), $"Track Start Y: {trackStartY:F2}", style);
    }

    private void OnDrawGizmos()
    {
        if (laneXPositions == null || laneXPositions.Length == 0) return;

        // Recalculate segment height (in case subdivision or bpm changed)
        //UpdateSegmentHeight();

        Gizmos.color = segmentColour;

        // Draw segments starting from trackStartY
        for (int seg = 0; seg < segmentCount; seg++)
        {
            float y = trackStartY + seg * segmentHeight;

            // Draw spheres along each lane
            for (int i = 0; i < laneXPositions.Length; i++)
            {
                Gizmos.DrawSphere(new Vector3(laneXPositions[i], y, laneZ), 0.1f);
            }

        #if UNITY_EDITOR
            // Draw time label (in seconds) to the right of the lanes
            float timeAtSegment = (y - trackStartY) * speedMultiplier;
            UnityEditor.Handles.Label(
                new Vector3(laneXPositions[laneXPositions.Length - 1] + 1.5f, y, laneZ),
                $"{timeAtSegment:F2}s"
            );
        #endif
        }
    }

    #endregion
}