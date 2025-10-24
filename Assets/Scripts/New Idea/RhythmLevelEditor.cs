using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class RhythmLevelEditor : MonoBehaviour
{
    public AudioSource audioSource;
    public RhythmLevel currentLevel;
    public NoteSpawner spawner;
    public Transform notesParent;

    private bool isRecording = false;

    void Update()
    {
        //Toggle editor mode
        if (Input.GetKeyDown(KeyCode.E))
        {
            isRecording = !isRecording;

            //Stop audio
            audioSource.Stop();
            audioSource.time = 0f;

            DeleteAllNotesInScene();

            //Stop spawner when editing
            if (spawner != null)
                spawner.enabled = !isRecording;

            Debug.Log("Editor mode: " + (isRecording ? "ON" : "OFF"));
        }

        if (!isRecording) return;

        //Record note
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentLevel.notes.Add(new RhythmNote { time = audioSource.time });
            Debug.Log("Note added at " + audioSource.time);
        }

        //Play/pause song
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (audioSource.isPlaying)
                audioSource.Pause();
            else
                audioSource.Play();
        }

        //Delete all notes in current level JSON
        if (Input.GetKeyDown(KeyCode.P))
        {
            currentLevel.notes.Clear();
            DeleteAllNotesInScene();
            Debug.Log("All notes deleted. Total notes: " + currentLevel.notes.Count);
        }

        //Save level
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveLevel(currentLevel);
        }
    }

    void SaveLevel(RhythmLevel level)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"levelName\": \"{level.levelName}\",");
        sb.AppendLine($"  \"bpm\": {level.bpm},");
        sb.AppendLine($"  \"songFileName\": \"{level.songFileName}\",");
        sb.AppendLine("  \"notes\": [");

        for (int i = 0; i < level.notes.Count; i++)
        {
            sb.Append($"    {{ \"time\": {level.notes[i].time:F3} }}");
            if (i < level.notes.Count - 1) sb.Append(",");
            sb.AppendLine();
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");

        string path = Path.Combine(Application.streamingAssetsPath, "Levels", level.levelName + ".json");

        //Ensure folder exists
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        File.WriteAllText(path, sb.ToString());
        Debug.Log("Level saved to StreamingAssets: " + path);
    }

    void DeleteAllNotesInScene()
    {
        //Delete all OrbitingNote objects in the scene
        OrbitingNote[] existingNotes = FindObjectsOfType<OrbitingNote>();
        foreach (var note in existingNotes)
        {
            Destroy(note.gameObject);
        }
    }
}