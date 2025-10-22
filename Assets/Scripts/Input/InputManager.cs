using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode[] laneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

    [Header("Lane References")]
    public Transform[] laneSpawnPoints;

    [Header("Hit Settings")]
    public Transform hitLine;
    public float hitTolerance = 0.15f;

    private bool[] laneHolding;

    private void Awake()
    {
        laneHolding = new bool[laneKeys.Length];
    }

    private void Update()
    {
        for (int lane = 0; lane < laneKeys.Length; lane++)
        {
            if (Input.GetKeyDown(laneKeys[lane]))
            {
                laneHolding[lane] = true;
                bool hitSomething = CheckHit(lane);

                //Lose combo if spamming
                if (!hitSomething)
                    ComboManager.Instance.ResetCombo();
            }

            if (Input.GetKeyUp(laneKeys[lane]))
            {
                laneHolding[lane] = false;
                ReleaseHold(lane);
            }
        }
    }

    private bool CheckHit(int lane)
    {
        if (lane < 0 || lane >= laneSpawnPoints.Length) return false;

        Note[] notes = FindObjectsOfType<Note>();
        Note closest = null;
        float closestDist = float.MaxValue;

        foreach (var note in notes)
        {
            //Only notes in this lane
            if (Mathf.Abs(note.transform.position.x - laneSpawnPoints[lane].position.x) > 0.5f)
                continue;

            float dist = Mathf.Abs(note.transform.position.y - hitLine.position.y);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = note;
            }
        }

        if (closest != null && closestDist <= hitTolerance)
        {
            closest.Hit();
            Debug.Log($"Hit lane {lane} ({(closest.isHold ? "Hold" : "Tap")})");
            return true;
        }

        return false;
    }

    private void ReleaseHold(int lane)
    {
        if (lane < 0 || lane >= laneSpawnPoints.Length) return;

        Note[] notes = FindObjectsOfType<Note>();
        foreach (var note in notes)
        {
            if (Mathf.Abs(note.transform.position.x - laneSpawnPoints[lane].position.x) > 0.5f)
                continue;

            if (note.isHold)
                note.ReleaseHold();
        }
    }
}