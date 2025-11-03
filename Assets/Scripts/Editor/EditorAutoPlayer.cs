using System.Collections.Generic;
using UnityEngine;

public class EditorAutoPlayer : MonoBehaviour
{
    [Header("Auto Player Settings")]
    public bool play = false;
    public Transform hitLine;
    public float hitTolerance = 0.05f;

    [HideInInspector] public float speedMultiplier = 0.1f;
    private List<EditorNote> notes = new List<EditorNote>();
    private static HashSet<EditorNote> activeHoldNotes = new HashSet<EditorNote>();

    public static bool IsPlayingNote(EditorNote note) => activeHoldNotes.Contains(note);

    private void Update()
    {
        if (!play) return;
        if (EditorSongManager.Instance.audioSource == null) return;
        if (!EditorSongManager.Instance.isPlayingFromCamera) return;

        float hitY = hitLine.position.y;
        float songTime = EditorSongManager.Instance.audioSource.time;

        for (int i = 0; i < notes.Count; i++)
        {
            var note = notes[i];
            if (note == null) continue;

            // Move unhit notes
            if (!note.isHit && !IsPlayingNote(note))
            {
                float y = (note.time - songTime) / speedMultiplier;
                note.transform.position = new Vector3(note.transform.position.x, y, note.transform.position.z);
            }

            // TAP NOTES
            if (!note.isHold && !note.isHit && note.transform.position.y <= hitY + hitTolerance)
            {
                note.isHit = true;
                note.transform.position = new Vector3(note.transform.position.x, hitY, note.transform.position.z);
                note.PlayTapPop();
            }

            // HOLD NOTES
            else if (note.isHold && !note.isHit && note.transform.position.y <= hitY + hitTolerance)
            {
                note.isHit = true;
                note.transform.position = new Vector3(note.transform.position.x, hitY, note.transform.position.z);
                note.StartHoldPulse();
                activeHoldNotes.Add(note);
            }
        }
    }

    public void ResetAllNotes()
    {
        // Stop all active hold coroutines first
        StopAllHolds();

        // Reset all notes to original positions
        foreach (var note in notes)
        {
            if (note != null)
                note.ResetNote();
        }

        activeHoldNotes.Clear();
        Debug.Log("All notes reset.");
    }

    public void StopAllHolds()
    {
        foreach (var note in activeHoldNotes)
        {
            if (note != null)
            {
                note.StopHoldPulse();
                note.gameObject.SetActive(true);
            }
        }

        activeHoldNotes.Clear();
    }

    public void RefreshNotes()
    {
        notes.Clear();
        EditorNote[] allNotes = FindObjectsOfType<EditorNote>();
        foreach (var n in allNotes)
        {
            n.ResetNote();
            notes.Add(n);
        }

        SetSpeedMultiplier(EditorSongManager.Instance.speedMultiplier);
        Debug.Log("Refreshed autoplayer's notes.");
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }
}