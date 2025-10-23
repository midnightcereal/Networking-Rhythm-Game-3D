using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorAutoPlayer : MonoBehaviour
{
    [Header("Auto Player Settings")]
    public bool play = false;
    public Transform hitLine;
    public float hitTolerance = 0.05f;

    [HideInInspector] public float speedMultiplier = 0.1f; // from loaded JSON
    private List<EditorNote> notes = new List<EditorNote>();
    private static HashSet<EditorNote> activeHoldNotes = new HashSet<EditorNote>();

    public static bool IsPlayingNote(EditorNote note) => activeHoldNotes.Contains(note);

    private void Update()
    {
        if (!play) return;

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
                note.gameObject.SetActive(false);
                notes.RemoveAt(i);
                i--;
            }

            // HOLD NOTES
            else if (note.isHold && !note.isHit && note.transform.position.y <= hitY + hitTolerance)
            {
                note.isHit = true;
                note.transform.position = new Vector3(note.transform.position.x, hitY, note.transform.position.z);
                note.StartHoldPulse();

                activeHoldNotes.Add(note);
                StartCoroutine(HoldRoutine(note));
            }
        }
    }

    private IEnumerator HoldRoutine(EditorNote note)
    {
        float songStartTime = EditorSongManager.Instance.audioSource.time;
        float hitTime = note.time;
        float holdEndTime = hitTime + note.holdDuration; // when the hold should complete

        while (EditorSongManager.Instance.audioSource.time < holdEndTime)
        {
            if (note != null)
            {
                float remainingDuration = Mathf.Max(0f, holdEndTime - EditorSongManager.Instance.audioSource.time);

                // Set hold line scale based on remaining time
                float lineLength = remainingDuration / speedMultiplier;
                note.UpdateHoldVisual(lineLength);

                // Lock note Y at hit line
                note.transform.position = new Vector3(note.transform.position.x, hitLine.position.y, note.transform.position.z);
            }

            yield return null;
        }

        if (note != null)
        {
            note.StopHoldPulse();
            note.gameObject.SetActive(false);
            activeHoldNotes.Remove(note);
        }
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