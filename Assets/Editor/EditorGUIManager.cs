using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EditorSongManager))]
public class EditorGUIManager : Editor
{
    private EditorSongManager manager;
    private bool deleteMode = false;
    private string songNameField = "TestSong";

    private void OnEnable()
    {
        manager = (EditorSongManager)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Editor Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Load JSON"))
            manager.LoadJSON();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Song Name:", GUILayout.Width(70));
        songNameField = EditorGUILayout.TextField(songNameField);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Save JSON"))
        {
            manager.SaveJSON(songNameField, 178f, "digicore_mine");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Playback Controls", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play From Camera"))
        {
            if (EditorSongManager.Instance.audioSource == null) return;

            EditorSongManager.Instance.isPlayingFromCamera = true;
            manager.PlayFromCamera();
        }
        if (GUILayout.Button("Pause")) manager.PauseAudio();
        if (GUILayout.Button("Restart"))
        {
            manager.ResetTrack();
            EditorAutoPlayer autoPlayer = FindObjectOfType<EditorAutoPlayer>();
            if (autoPlayer != null)
            {
                EditorSongManager.Instance.isPlayingFromCamera = false;

                autoPlayer.ResetAllNotes();
                autoPlayer.RefreshNotes();
            }

            BeatMarkerVisualizer markerVisualiser = FindObjectOfType<BeatMarkerVisualizer>();
            if (markerVisualiser != null)
            {
                markerVisualiser.ResetMarkersPosition();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Note Placement", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        bool tapToggle = GUILayout.Toggle(manager.placingTap, "Tap Note", "Button");
        if (tapToggle != manager.placingTap)
        {
            manager.placingTap = tapToggle;
            if (tapToggle) { manager.placingHold = false; deleteMode = false; }
        }

        bool holdToggle = GUILayout.Toggle(manager.placingHold, "Hold Note", "Button");
        if (holdToggle != manager.placingHold)
        {
            manager.placingHold = holdToggle;
            if (holdToggle) { manager.placingTap = false; deleteMode = false; }
        }
        EditorGUILayout.EndHorizontal();

        if (manager.placingHold)
        {
            manager.holdDurationEditor = EditorGUILayout.FloatField("Hold Duration (s)", manager.holdDurationEditor);
        }

        EditorGUILayout.Space();
        bool deleteToggle = GUILayout.Toggle(deleteMode, "Delete Mode", "Button");
        if (deleteToggle != deleteMode)
        {
            deleteMode = deleteToggle;
            if (deleteMode)
            {
                manager.placingTap = false;
                manager.placingHold = false;
            }
        }
    }
}