using System.Collections;
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
    public float speedMultiplier;
    public string audioFile;
    public int colourChangeBeats;
    public string[] availableColours;
    public EditorNoteData[] notes;
}


public class EditorSongManager : MonoBehaviour
{
    [Header("Editor Tools")]
    public bool dragToolActive = false;
    private bool snapSelectedNotes = true;
    private bool isSelecting = false;
    private bool isDraggingNotes = false;
    private Vector3 lastMouseWorldPos;
    private Vector2 selectionStart;
    private readonly List<EditorNote> selectedNotes = new List<EditorNote>();

    [Header("Selection UI")]
    public RectTransform selectionBox;
    private Vector2 selectionBoxStart;
    private Vector2 selectionBoxEnd;

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
    [SerializeField] private Transform trackContainer;
    public float scrollSpeed = 5f;
    [SerializeField] private float cameraReturnSpeed = 5f;
    float defaultCameraY = 0f;
    private Coroutine cameraMoveRoutine;

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

    [Header("Playback Bar")]
    public GameObject playStartBarPrefab;
    private GameObject currentPlayStartBar = null;
    private float? playStartY = null;
    private float storedBarY = 0f;

    private void Start()
    {
        defaultCameraY = Camera.main.transform.position.y;
        SpawnSegments();
        UpdateSegmentHeight();
        UpdateSegmentTexts();
    }

    public void SpawnSegments()
    {
        ClearAllSegments();
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

    public void ClearAllSegments()
    {
        // Destroy all runtime segment objects
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

        // Optional: find any lingering TMP labels in the scene and remove them
#if UNITY_EDITOR
        TMPro.TextMeshProUGUI[] tmpLabels = FindObjectsOfType<TMPro.TextMeshProUGUI>();
        foreach (var t in tmpLabels)
        {
            if (!runtimeSegments.Contains(t.transform.root.gameObject))
                DestroyImmediate(t.gameObject);
        }
#endif
    }

    private void Update()
    {
        HandleMouseInput();
        HandleKeyShortcuts();
        HandleScroll();
        HandleSubdivisionHotkeys();
        UpdateSegmentScroll();
        UpdateSegmentRebuild();
    }

    #region Inputs
    private void HandleMouseInput()
    {
        Vector3 mousePos = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            if (isSelecting)
            {
                isSelecting = false;
                if (selectionBox != null)
                    selectionBox.gameObject.SetActive(false);
            }

            if (isDraggingNotes)
            {
                foreach (var n in selectedNotes)
                {
                    n.isDragging = false;

                    //Update time based on new Y position so it stays there
                    n.time = n.transform.position.y * speedMultiplier;

                    //Reset colour back to white when drag ends
                    Renderer rend = n.GetComponent<Renderer>();
                    if (rend != null)
                        rend.sharedMaterial.color = Color.white;
                }

                isDraggingNotes = false;
            }
        }

        //Place start bar (middle click)
        if (Input.GetMouseButtonDown(2))
        {
            PlacePlayStartBar();
        }

        //Delete note (right click)
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

        //Drag tool active
        if (dragToolActive)
        {
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            //Start selection box
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    EditorNote note = hit.collider.GetComponent<EditorNote>();
                    if (note != null && selectedNotes.Contains(note))
                    {
                        //Drag selected notes
                        isDraggingNotes = true;
                        lastMouseWorldPos = GetMouseWorldPosition(mousePos);

                        foreach (var n in selectedNotes)
                            n.isDragging = true;
                    }
                    else
                    {
                        //Start a new selection box
                        isSelecting = true;
                        selectionStart = mousePos;
                        if (selectionBox != null)
                            selectionBox.gameObject.SetActive(true);
                    }
                }
                else
                {
                    //Clicked empty space -> start selection box
                    isSelecting = true;
                    selectionStart = mousePos;
                    if (selectionBox != null)
                        selectionBox.gameObject.SetActive(true);
                }
            }

            //Update selection box
            if (isSelecting && Input.GetMouseButton(0))
            {
                UpdateSelectionBox(mousePos);
                SelectNotesWithinBox(selectionStart, mousePos);
            }

            //Drag selected notes
            if (isDraggingNotes && selectedNotes.Count > 0 && Input.GetMouseButton(0))
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition(mousePos);
                Vector3 delta = mouseWorldPos - lastMouseWorldPos;

                foreach (var n in selectedNotes)
                {
                    Vector3 newPos = n.transform.position + delta;

                    //Snap if shift is NOT held
                    if (!shiftHeld)
                        newPos = SnapToGrid(newPos);

                    n.transform.position = newPos;
                }

                lastMouseWorldPos = mouseWorldPos;
            }

            //End selection or drag
            if (Input.GetMouseButtonUp(0))
            {
                if (isSelecting)
                {
                    isSelecting = false;
                    if (selectionBox != null)
                        selectionBox.gameObject.SetActive(false);
                }

                if (isDraggingNotes)
                {
                    foreach (var n in selectedNotes)
                        n.isDragging = false;

                    isDraggingNotes = false;
                }
            }

            return;
        }

        //Placement mode (tap or hold)
        if ((placingTap || placingHold) && Input.GetMouseButtonDown(0))
        {
            if (placingTap)
            {
                PlaceNoteOnTrack(mousePos, false);
            }
            else if (placingHold)
            {
                //Start placing hold note
                currentHoldNote = PlaceNoteOnTrack(mousePos, true);
                if (currentHoldNote != null)
                    currentHoldNote.isDragging = true; //enable drag to draw
            }
        }

        //Dragging hold note to set length
        if (currentHoldNote != null && currentHoldNote.isDragging && Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition(mousePos);
            float startY = currentHoldNote.transform.position.y;
            float dragY = mouseWorldPos.y;

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            //Snap if shift is NOT held
            if (!shiftHeld)
            {
                int startSegment = Mathf.FloorToInt((startY - trackStartY) / segmentHeight);
                int endSegment = Mathf.CeilToInt((dragY - trackStartY) / segmentHeight);
                dragY = trackStartY + Mathf.Max(startSegment + 1, endSegment) * segmentHeight;
            }

            float holdLength = Mathf.Max(0.01f, dragY - startY);
            currentHoldNote.holdDuration = holdLength * speedMultiplier;
            currentHoldNote.UpdateHoldVisual(holdLength);
        }

        //Release hold note
        if (currentHoldNote != null && Input.GetMouseButtonUp(0))
        {
            currentHoldNote.isDragging = false;
            currentHoldNote = null;
        }
    }

    private void HandleKeyShortcuts()
    {
        // --- Toggle note placement modes ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            placingTap = true;
            placingHold = false;
            dragToolActive = false;
            Debug.Log("Tap placement mode active");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            placingTap = false;
            placingHold = true;
            dragToolActive = false;
            Debug.Log("Hold placement mode active");
        }

        // --- Toggle drag tool ---
        if (Input.GetKeyDown(KeyCode.M))
        {
            dragToolActive = !dragToolActive;
            placingTap = false;
            placingHold = false;

            if (dragToolActive)
                Debug.Log("Drag tool activated");
            else
                Debug.Log("Drag tool deactivated");

            // Clear current selection when disabling
            if (!dragToolActive)
                selectedNotes.Clear();
        }

        // --- Deselect all ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            selectedNotes.Clear();
            dragToolActive = false;
            placingTap = false;
            placingHold = false;
            Debug.Log("All tools cleared");
        }
    }
    #endregion

    #region Shortcut Functions
    private Vector3 SnapToGrid(Vector3 pos)
    {
        //Snap X to nearest lane
        float closestX = laneXPositions[0];
        float minDist = Mathf.Abs(pos.x - closestX);
        for (int i = 1; i < laneXPositions.Length; i++)
        {
            float d = Mathf.Abs(pos.x - laneXPositions[i]);
            if (d < minDist)
            {
                minDist = d;
                closestX = laneXPositions[i];
            }
        }

        //Snap Y to nearest segment
        float relativeY = pos.y - trackStartY;
        int nearestSegment = Mathf.RoundToInt(relativeY / segmentHeight);
        float snappedY = trackStartY + nearestSegment * segmentHeight;

        return new Vector3(closestX, snappedY, laneZ);
    }

    private Vector3 GetMouseWorldPosition(Vector3 mousePos)
    {
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, laneZ));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);
        return Vector3.zero;
    }

    private void UpdateSelectionBox(Vector3 currentMousePos)
    {
        Vector2 start = selectionStart;
        Vector2 end = currentMousePos;
        Vector2 center = (start + end) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(start.x - end.x), Mathf.Abs(start.y - end.y));

        selectionBox.position = center;
        selectionBox.sizeDelta = size;
    }
    private void SelectNotesWithinBox(Vector2 start, Vector2 end)
    {
        Rect rect = new(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            Mathf.Abs(end.x - start.x),
            Mathf.Abs(end.y - start.y)
        );

        selectedNotes.Clear();

        foreach (var note in notes)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(note.transform.position);
            if (rect.Contains(screenPos))
                selectedNotes.Add(note);
        }

        HighlightSelectedNotes();
    }

    private void HighlightSelectedNotes()
    {
        foreach (var note in FindObjectsOfType<EditorNote>())
        {
            var rend = note.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = selectedNotes.Contains(note) ? Color.yellow : Color.white;
        }
    }
    #endregion

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

        //Move play bar
        if (currentPlayStartBar != null)
        {
            currentPlayStartBar.transform.position = new Vector3(0f, storedBarY - scrollOffset, laneZ - 0.5f);
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

    private void PlacePlayStartBar()
    {
        if (playStartBarPrefab == null)
        {
            Debug.LogWarning("No playStartBarPrefab assigned!");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, laneZ));

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);

            //Remove any existing bar
            if (currentPlayStartBar != null)
                DestroyImmediate(currentPlayStartBar);

            //Spawn new bar at hit.y
            currentPlayStartBar = Instantiate(playStartBarPrefab);
            currentPlayStartBar.transform.position = new Vector3(0f, hit.y, laneZ - 0.5f);
            currentPlayStartBar.transform.localScale = new Vector3(25f, 0.1f, 0.5f);
            currentPlayStartBar.name = "PlayStartBar";

            storedBarY = hit.y;
            playStartY = hit.y;

            Debug.Log($"Set Play Start Bar at Y={playStartY:F2}");
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

        // Find nearest segment
        int nearestSegment = Mathf.RoundToInt((hit.y - trackStartY) / segmentHeight);

        // Check for existing note in the same lane & segment
        foreach (var n in notes)
        {
            if (n == null) continue;
            int nSegment = Mathf.RoundToInt((n.transform.position.y - trackStartY) / segmentHeight);
            if (nSegment == nearestSegment && n.lane == laneIndex)
            {
                Debug.Log("Segment already has a note on this lane!");
                return null;
            }
        }

        // Snap Y unless shift is held
        if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            hit.y = trackStartY + nearestSegment * segmentHeight;
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
        else
        {
            Debug.Log("Retrieved note script");
        }

        noteScript.isHold = hold;
        noteScript.lane = laneIndex;
        noteScript.time = (hit.y * speedMultiplier) + audioSource.time;

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

        // Preserve existing JSON fields if they exist
        EditorSongData existingData = null;
        if (File.Exists(path))
        {
            string existingJson = File.ReadAllText(path);
            existingData = JsonUtility.FromJson<EditorSongData>(existingJson);
        }

        //Collect note data
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

        //Create new song data
        EditorSongData songData = new EditorSongData
        {
            songName = defaultSongName,
            bpm = bpmInput,
            speedMultiplier = speedMultiplier,
            audioFile = defaultAudioFile,
            notes = noteDataList.ToArray()
        };

        //Preserve existing colour fields if available
        if (existingData != null)
        {
            songData.colourChangeBeats = existingData.colourChangeBeats;
            songData.availableColours = existingData.availableColours;
        }

        //Write to file
        File.WriteAllText(path, JsonUtility.ToJson(songData, true));
        Debug.Log($"Saved song JSON: {path}");
    }

    public void LoadJSON()
    {
        string path = EditorUtility.OpenFilePanel("Load JSON", "Assets/Resources/Songs", "json");
        if (string.IsNullOrEmpty(path)) return;

        //Ensure AudioSource exists
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                Debug.Log("Created new AudioSource on Editor Manager");
            }
        }

        //Read JSON
        string jsonText = File.ReadAllText(path);
        EditorSongData songData = JsonUtility.FromJson<EditorSongData>(jsonText);
        if (songData == null)
        {
            Debug.LogError("Failed to parse JSON file!");
            return;
        }

        //Apply basic fields
        songName = songData.songName;
        bpm = songData.bpm;
        speedMultiplier = songData.speedMultiplier;

        UpdateSegmentHeight();
        UpdateSegmentTexts();

        //Load audio
        if (!string.IsNullOrEmpty(songData.audioFile))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Songs/{songData.audioFile}");
            if (clip != null)
            {
                audioSource.clip = clip;
                Debug.Log($"Loaded audio clip: {songData.audioFile}");
            }
            else
            {
                Debug.LogWarning($"Audio file '{songData.audioFile}' not found in Resources/Songs/");
            }
        }

        //Clear old notes
        foreach (var n in notes)
            if (n != null) DestroyImmediate(n.gameObject);
        notes.Clear();

        //Instantiate loaded notes
        foreach (var nData in songData.notes)
        {
            Vector3 pos = new Vector3(laneXPositions[nData.lane], nData.positionY, laneZ);
            EditorNote note = Instantiate(notePrefab, pos, Quaternion.identity).GetComponent<EditorNote>();
            note.isHold = nData.type == "hold";
            note.lane = nData.lane;
            note.time = nData.positionY * speedMultiplier;
            note.holdDuration = nData.holdDuration;
            if (note.isHold)
                note.UpdateHoldVisual(nData.holdDuration / speedMultiplier);
            notes.Add(note);
        }

        Debug.Log($"Loaded song: {songData.songName} | Colours preserved: {(songData.availableColours != null ? songData.availableColours.Length : 0)}");
    }

    #endregion

    #region Playback
    public void PlayFromCamera()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource || audioSource.clip == null) return;

        float startTime = 0f;

        if (playStartY.HasValue)
        {
            startTime = Mathf.Max(0f, (playStartY.Value - trackStartY) * speedMultiplier);
        }
        else
        {
            //Fallback to camera position
            float camY = Camera.main.transform.position.y;
            startTime = Mathf.Max(0f, (camY - trackStartY) * speedMultiplier);
        }

        startTime = Mathf.Clamp(startTime, 0f, audioSource.clip.length);
        audioSource.time = startTime;
        audioSource.Play();

        //Move camera to default (hit bar area) when starting
        Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, trackStartY, Camera.main.transform.position.z);

        Debug.Log($"> Playing from {startTime:F2}s (bar Y={playStartY})");
    }

    private IEnumerator DelayedPlay(float startTime, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.time = startTime;
        audioSource.Play();
    }

    private IEnumerator MoveCameraToDefault()
    {
        Transform cam = Camera.main.transform;
        Vector3 startPos = cam.position;
        Vector3 targetPos = new Vector3(startPos.x, defaultCameraY, startPos.z);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * cameraReturnSpeed;
            cam.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
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

        //Remove play start bar and reset custom start
        if (currentPlayStartBar != null)
        {
            DestroyImmediate(currentPlayStartBar);
            currentPlayStartBar = null;
        }
        playStartY = null;

        Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, trackStartY, Camera.main.transform.position.z);
        Camera.main.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        ResetSegmentTextPositions();

        Debug.Log("> Restarted track and removed play start bar.");
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