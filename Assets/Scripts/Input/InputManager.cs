using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode[] laneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

    [Header("Lane References")]
    public Transform[] laneSpawnPoints;

    [Header("Hit Settings")]
    public Transform hitLine;
    public float hitTolerance = 0.5f; // world units

    // Track hold state per lane
    private bool[] laneHolding;

    private void Awake()
    {
        laneHolding = new bool[laneKeys.Length];
    }

    private void Update()
    {
        for (int lane = 0; lane < laneKeys.Length; lane++)
        {
            // Key pressed
            if (Input.GetKeyDown(laneKeys[lane]))
            {
                laneHolding[lane] = true;
                CheckHit(lane, true);
            }

            // Key released
            if (Input.GetKeyUp(laneKeys[lane]))
            {
                laneHolding[lane] = false;
                ReleaseHold(lane);
            }
        }
    }

    private void CheckHit(int lane, bool holdingKey)
    {
        if (lane < 0 || lane >= laneSpawnPoints.Length) return;

        Note[] laneNotes = FindObjectsOfType<Note>();
        Note closest = null;
        float closestDist = float.MaxValue;

        foreach (var note in laneNotes)
        {
            // Only notes in this lane
            if (Mathf.Abs(note.transform.position.x - laneSpawnPoints[lane].position.x) > 0.5f) continue;

            float dist = Mathf.Abs(note.transform.position.y - hitLine.position.y);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = note;
            }
        }

        if (closest != null && closestDist <= hitTolerance)
        {
            closest.Hit(holdingKey);
            Debug.Log($"Hit lane {lane} ({(closest.isHold ? "Hold" : "Tap")})");
        }
    }

    private void ReleaseHold(int lane)
    {
        if (lane < 0 || lane >= laneSpawnPoints.Length) return;

        Note[] laneNotes = FindObjectsOfType<Note>();

        foreach (var note in laneNotes)
        {
            // Only notes in this lane
            if (Mathf.Abs(note.transform.position.x - laneSpawnPoints[lane].position.x) > 0.5f) continue;

            if (note.isHold)
            {
                note.ReleaseHold(); // stops scaling and triggers miss if released early
                Debug.Log("Stopped holding | Missed");
            }
        }
    }
}